using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Narrative
{
    public enum StoryArchiveReadStatus { NotLoaded, Loading, Ready, Unavailable, AuthRequired }

    // Owns only the current account's story-progress read. No reward, run-start,
    // playback or operator-discovery state is derived here.
    public sealed class StoryArchiveReadModel : IDisposable
    {
        readonly IRunApiHttpTransport wire;readonly DataLifecycleTrace trace;
        readonly Func<GameApiAuthSessionSnapshot> session;
        readonly Func<long> generation;
        GameApiAuthSessionSnapshot owner;
        long ownerGeneration=long.MinValue;
        int request;
        bool disposed;
        Task readTask;

        public StoryProgressReadState State { get; private set; }
        // Keep verified source/version for recovery while withholding account display
        // after authentication rejection, including an unsuccessful retry.
        bool displayAuthenticationRequired;
        public StoryProgressReadState DisplayState=>displayAuthenticationRequired?null:State;
        public ConfirmedStoryProgress ConfirmedProgress { get; private set; }

        StoryProgressProjection projection;
        internal bool TryPrepareClaim(JObject progress, out ConfirmedStoryProgress next)
        {
            next=ConfirmedProgress;
            if(Status!=StoryArchiveReadStatus.Ready||State==null||projection==null||next==null)return false;
            try {
                RunCompleteClaimReceipt.ValidateProgress(progress);
                long version=(long)progress["mutationSequence"];string hash=(string)progress["contentHash"];
                if(version<next.MutationSequence)return true;
                if(version==next.MutationSequence)return hash==next.ContentHash;
                var display=Parse(projection.Project(progress).ToString());
                next=new ConfirmedStoryProgress(version,hash,display,(JObject)progress["state"]);return true;
            }catch(FormatException){return false;}
            catch(ArgumentException){return false;}
        }
        internal void CommitClaim(ConfirmedStoryProgress next){request++;readTask=null;ConfirmedProgress=next;State=next.Display;}
        internal void NotifyClaim(){Changed?.Invoke();}
        public StoryArchiveReadStatus Status { get; private set; }
        public string ErrorCode { get; private set; }
        public event Action Changed;

        public StoryArchiveReadModel(IRunApiHttpTransport wire,Func<GameApiAuthSessionSnapshot> session,Func<long> generation,DataLifecycleTrace trace=null)
        {
            this.trace=trace;this.wire=wire??throw new ArgumentNullException(nameof(wire));
            this.session=session??throw new ArgumentNullException(nameof(session));
            this.generation=generation??throw new ArgumentNullException(nameof(generation));
        }

        public void SynchronizeSession()
        {
            if(disposed)return;
            var next=session();long stamp=generation();
            if(ReferenceEquals(owner,next)&&ownerGeneration==stamp)return;
            bool retain=owner!=null&&next!=null&&owner.AccountId==next.AccountId&&owner.AccountStatus=="ACTIVE"&&next.AccountStatus=="ACTIVE"&&ownerGeneration==stamp&&(State!=null||ConfirmedProgress!=null);
            readTask=null;
            if(retain){owner=next;request++;if(Status==StoryArchiveReadStatus.Loading)Status=StoryArchiveReadStatus.Unavailable;return;}
            trace?.Record(LifecycleData.Story,LifecycleEvent.ReferenceReleased,LifecycleReason.SessionChanged,stamp);owner=next;ownerGeneration=stamp;request++;State=null;ConfirmedProgress=null;projection=null;ErrorCode=null;
            displayAuthenticationRequired=next==null;
            Status=next==null?StoryArchiveReadStatus.AuthRequired:StoryArchiveReadStatus.NotLoaded;
            Changed?.Invoke();
        }

        public Task EnsureAsync(){SynchronizeSession();if(disposed)return Task.CompletedTask;if(Status==StoryArchiveReadStatus.Ready){trace?.Record(LifecycleData.Story,LifecycleEvent.CacheHit,LifecycleReason.Entry,ownerGeneration);return Task.CompletedTask;}return RefreshAsync();}
        public void SupplyValidated(GameApiAuthSessionSnapshot original,long stamp,StoryProgressReadState state){
            SynchronizeSession();if(disposed||state==null||!ReferenceEquals(original,session())||stamp!=generation())return;
            if(ConfirmedProgress!=null)return;
            request++;readTask=null;State=state;projection=null;ErrorCode=null;displayAuthenticationRequired=false;Status=StoryArchiveReadStatus.Ready;Changed?.Invoke();
        }
        public Task RefreshAsync(){
            SynchronizeSession();if(disposed)return Task.CompletedTask;if(readTask!=null&&!readTask.IsCompleted){trace?.Record(LifecycleData.Story,LifecycleEvent.Joined,LifecycleReason.Entry,ownerGeneration);return readTask;}trace?.Record(LifecycleData.Story,LifecycleEvent.Requested,LifecycleReason.ExplicitRefresh,ownerGeneration);
            var completion=new TaskCompletionSource<bool>();readTask=completion.Task;Complete();return completion.Task;
            async void Complete(){try{await ReadAsync();completion.TrySetResult(true);}catch(Exception e){completion.TrySetException(e);}}
        }
        async Task ReadAsync()
        {
            if(disposed)return;
            SynchronizeSession();var original=owner;long stamp=ownerGeneration;int read=++request;
            if(original==null)return;
            bool Current()=>!disposed&&read==request&&ReferenceEquals(original,session())&&stamp==generation();
            ErrorCode=null;Status=StoryArchiveReadStatus.Loading;Changed?.Invoke();
            try
            {
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(
                    new RunApiHttpRequest("GET","/v1/story-progress",null,new[]{new RunApiHttpHeader("X-Story-Progress-Version",StoryProgressProjection.ObservationVersion)}),original.AccessToken));
                if(!Current())return;
                if(response.IsNetworkError){Fail("network_error");return;}
                if(response.StatusCode==401||response.StatusCode==403){Fail("auth_required",StoryArchiveReadStatus.AuthRequired);return;}
                if(response.StatusCode!=200){Fail("read_unavailable");return;}
                try{AcceptObservation(response.BodyJson,original.AccountId);displayAuthenticationRequired=false;Status=StoryArchiveReadStatus.Ready;}
                catch(Exception){Fail("invalid_response");}
            }
            catch(Exception){if(Current())Fail("read_failed");}
            finally{if(Current())Changed?.Invoke();}
        }

        void Fail(string code,StoryArchiveReadStatus status=StoryArchiveReadStatus.Unavailable)
        {ErrorCode=code;Status=status;if(status==StoryArchiveReadStatus.AuthRequired)displayAuthenticationRequired=true;}

        void AcceptObservation(string json,string accountId)
        {
            var display=Parse(json);var root=GameApiJson.ReadObject(json);
            if(root["version"]==null&&root["sourceOwner"]==null&&root["projectionDefinition"]==null){
                if(ConfirmedProgress!=null)throw new FormatException("Unversioned progress cannot replace observed state");State=display;return;
            }
            if((string)root["version"]!=StoryProgressProjection.ObservationVersion||!(root["sourceOwner"] is JObject source)||
                (string)source["accountId"]!=accountId||!(root["projectionDefinition"] is JObject definition))throw new FormatException("Progress observation identity");
            var progress=(JObject)source.DeepClone();progress.Remove("accountId");RunCompleteClaimReceipt.ValidateProgress(progress);
            var incoming=new StoryProgressProjection(definition);var projected=incoming.Project(progress);incoming.ValidateDisplay(root,projected);
            long version=(long)progress["mutationSequence"];string hash=(string)progress["contentHash"];
            if(ConfirmedProgress!=null){
                if(version==ConfirmedProgress.MutationSequence&&hash!=ConfirmedProgress.ContentHash)throw new FormatException("Progress version conflict");
                if(projection.SameScope(incoming)&&!projection.SameDefinition(incoming))throw new FormatException("Progress definition conflict");
                if(version<ConfirmedProgress.MutationSequence)return;
            }
            projection=incoming;State=display;ConfirmedProgress=new ConfirmedStoryProgress(version,hash,display,(JObject)progress["state"]);
        }

        static StoryProgressReadState Parse(string json)
        {
            if(string.IsNullOrEmpty(json)||json.Length>131072)throw new FormatException("Story read size");
            var root=GameApiJson.ReadObject(json);
            if(!GameApiJson.Boolean(root,"accepted")||Text(root,"actId")!="act_01_wasteland_railway")throw new FormatException("Story scope");
            var progress=GameApiJson.Object(root,"storyProgress");var narrative=GameApiJson.Object(root,"narrative");
            if(progress["order"]?.Type!=JTokenType.Integer)throw new FormatException("Story order");
            long order=(long)progress["order"];if(order<0||order>1000)throw new FormatException("Story order range");
            var version=Text(root,"narrativeVersion");if(version!=Text(narrative,"narrativeVersion"))throw new FormatException("Narrative version mismatch");
            var response=new StoryProgressReadResponseDto(Text(root,"actId"),Optional(progress,"highestStoryProgressId"),Optional(progress,"highestClearedOperationId"),(int)order,
                Ids(root,"clearedOperationIds"),Ids(root,"unlockedOperations"),version,Text(narrative,"contentStatus"),Text(narrative,"localizationStatus"),Text(narrative,"fixtureStatus"),Text(narrative,"missingContentStatus"));
            return StoryProgressReadState.FromResponse(response);
        }
        static string Text(JObject row,string key)
        {
            if(row[key]?.Type!=JTokenType.String)throw new FormatException("Story text field");
            var value=(string)row[key];if(string.IsNullOrWhiteSpace(value)||value.Length>128)throw new FormatException("Story text range");return value;
        }
        static string Optional(JObject row,string key)
        {if(row[key]==null)throw new FormatException("Missing story field");return row[key].Type==JTokenType.Null?null:Text(row,key);}
        static IReadOnlyList<string> Ids(JObject row,string key)
        {
            if(!(row[key] is JArray values)||values.Count>64)throw new FormatException("Story list");
            var ids=new List<string>();foreach(var token in values){if(token.Type!=JTokenType.String)throw new FormatException("Story id");var id=(string)token;if(string.IsNullOrWhiteSpace(id)||id.Length>128)throw new FormatException("Story id range");ids.Add(id);}
            if(ids.Distinct(StringComparer.Ordinal).Count()!=ids.Count)throw new FormatException("Duplicate story id");return ids.AsReadOnly();
        }

        public void Dispose(){if(disposed)return;trace?.Record(LifecycleData.Story,LifecycleEvent.ReferenceReleased,LifecycleReason.OwnerDisposed,ownerGeneration);disposed=true;request++;readTask=null;owner=null;State=null;ConfirmedProgress=null;projection=null;Changed=null;}
    }
    public sealed class ConfirmedStoryProgress
    {
        public long MutationSequence { get; }
        public string ContentHash { get; }
        readonly JObject sourceState;
        internal StoryProgressReadState Display { get; }
        public JObject CaptureSourceState()=>(JObject)sourceState.DeepClone();
        internal ConfirmedStoryProgress(long sequence,string hash,StoryProgressReadState display,JObject state){MutationSequence=sequence;ContentHash=hash;Display=display;sourceState=(JObject)state.DeepClone();}
    }
}
