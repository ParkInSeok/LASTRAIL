using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public enum AdRuntimePolicyStatus { Unknown, Checking, Disabled, Enabled }
    // Ephemeral observation only. A true read never replaces the server's fresh start authorization.
    public sealed class AdRuntimePolicyService:IDisposable
    {
        readonly IRunApiHttpTransport wire;
        readonly Func<GameApiAuthSessionSnapshot> session;
        readonly Func<long> generation;
        long request,observedGeneration;
        GameApiAuthSessionSnapshot observedOwner;
        bool disposed;bool? observedEnabled;
        AdRuntimePolicyStatus status;
        public AdRuntimePolicyStatus Status=>!disposed&&ReferenceEquals(observedOwner,session())&&observedGeneration==generation()?status:AdRuntimePolicyStatus.Unknown;
        public long? Revision {get;private set;}
        public string Environment {get;private set;}
        public event Action Changed;
        public AdRuntimePolicyService(IRunApiHttpTransport wire,Func<GameApiAuthSessionSnapshot> session,Func<long> generation)
        {this.wire=wire;this.session=session;this.generation=generation;}
        bool Current(GameApiAuthSessionSnapshot owner,long stamp,long serial)=>!disposed&&serial==request&&ReferenceEquals(owner,session())&&stamp==generation();
        public async Task RefreshAsync()
        {
            if(disposed)return;var owner=session();long stamp=generation(),serial=++request;
            if(!ReferenceEquals(observedOwner,owner)||observedGeneration!=stamp){Revision=null;Environment=null;observedEnabled=null;}
            observedOwner=owner;observedGeneration=stamp;status=AdRuntimePolicyStatus.Unknown;
            if(owner==null||owner.AccountStatus!="ACTIVE"||owner.AccessTokenExpiresAt<=DateTime.UtcNow){Changed?.Invoke();return;}
            status=AdRuntimePolicyStatus.Checking;Changed?.Invoke();
            try
            {
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/runtime-policy",null,null),owner.AccessToken));
                if(!Current(owner,stamp,serial))return;
                if(response.IsNetworkError||response.StatusCode!=200)throw new FormatException("Runtime policy unavailable");
                var root=GameApiJson.ReadObject(response.BodyJson);
                if(!GameApiJson.Boolean(root,"accepted")||GameApiJson.String(root,"contractVersion")!="runtime-policy-v1")throw new FormatException("Runtime policy version");
                string environment=GameApiJson.String(root,"environment"),state=GameApiJson.String(root,"policyStatus");
                if(string.IsNullOrWhiteSpace(environment))throw new FormatException("Policy environment");
                Number(root,"serverTime");
                if(Environment!=null&&Environment!=environment)throw new FormatException("Policy environment changed");
                if(state=="UNKNOWN")
                {
                    if(root["adsEnabled"]?.Type!=JTokenType.Null||root["runtimePolicyRevision"]?.Type!=JTokenType.Null)throw new FormatException("Unknown policy shape");
                    status=AdRuntimePolicyStatus.Unknown;return;
                }
                if(state!="CONFIGURED")throw new FormatException("Policy status");
                long revision=Number(root,"runtimePolicyRevision");bool enabled=GameApiJson.Boolean(root,"adsEnabled");
                if(revision<1||Revision.HasValue&&revision<Revision.Value)throw new FormatException("Policy revision");
                if(Revision==revision&&observedEnabled.HasValue&&observedEnabled.Value!=enabled)throw new FormatException("Policy revision changed facts");
                Environment=environment;Revision=revision;observedEnabled=enabled;status=enabled?AdRuntimePolicyStatus.Enabled:AdRuntimePolicyStatus.Disabled;
            }
            catch(Exception){if(Current(owner,stamp,serial))status=AdRuntimePolicyStatus.Unknown;}
            finally{if(Current(owner,stamp,serial)){if(status==AdRuntimePolicyStatus.Checking)status=AdRuntimePolicyStatus.Unknown;Changed?.Invoke();}}
        }
        static long Number(JObject root,string key)
        {if(root[key]?.Type!=JTokenType.Integer)throw new FormatException(key);long value=root[key].Value<long>();if(value<0||value>9007199254740991L)throw new FormatException(key);return value;}
        public void Invalidate(){request++;status=AdRuntimePolicyStatus.Unknown;if(!disposed)Changed?.Invoke();}
        public void Reset(){request++;status=AdRuntimePolicyStatus.Unknown;Revision=null;Environment=null;observedOwner=null;observedEnabled=null;}
        public void Dispose(){disposed=true;Reset();Changed=null;}
    }
}
