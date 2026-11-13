using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.AugmentSelection
{
    // SELECT and explicitly enabled FREE reroll. No implicit read or effect application.
    public sealed class AugmentSelectHttpPort:IAugmentSelectionPort
    {
        public const string Version="combat.augment-select.v1.20261019";
        public const string FreeRerollVersion="combat.augment-reroll.v1.20261021";
        readonly bool freeRerolls;
        readonly IGameApiAuthenticatedHttpTransport transport;
        readonly AugmentSelectionOwner owner;
        readonly Func<GameApiAuthSessionSnapshot> session;
        readonly Func<bool> ownerCurrent;
        public AugmentSelectHttpPort(IGameApiAuthenticatedHttpTransport transport,AugmentSelectionOwner owner,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent)
            :this(transport,owner,session,ownerCurrent,false){}
        public AugmentSelectHttpPort(IGameApiAuthenticatedHttpTransport transport,AugmentSelectionOwner owner,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,bool freeRerolls)
        {this.transport=transport??throw new ArgumentNullException(nameof(transport));this.owner=owner??throw new ArgumentNullException(nameof(owner));this.session=session??throw new ArgumentNullException(nameof(session));this.ownerCurrent=ownerCurrent??throw new ArgumentNullException(nameof(ownerCurrent));this.freeRerolls=freeRerolls;}
        public Task<string> ReadAsync(AugmentSelectionScope scope,CancellationToken cancellation)=>throw new NotSupportedException("Public augment read contract unavailable; supply the original node snapshot");
        public async Task<string> ExecuteAsync(AugmentSelectionScope scope,AugmentSelectionCommand command,CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if(scope==null||!ReferenceEquals(scope.Owner,owner)||!ownerCurrent())throw new OperationCanceledException("Selection owner changed");
            bool reroll=command?.Operation=="REROLL"&&command.Mode=="FREE"&&freeRerolls;
            if(command==null||command.Operation!="SELECT"&&!reroll)throw new NotSupportedException("Only SELECT and explicitly enabled FREE reroll are supported");
            if(command.Before.RunId!=scope.RunId||command.Before.EventId!=scope.EventId||command.Key.Length>128)throw new ArgumentException("Original selection request scope");
            foreach(char c in command.Key)if(c<0x21||c>0x7e)throw new ArgumentException("Original request key format");
            var auth=session();if(auth==null||auth.AccountId!=owner.AccountId||auth.AccountStatus!="ACTIVE")throw new OperationCanceledException("Selection authentication changed");
            string version=reroll?FreeRerollVersion:Version;
            var body=new JObject{["version"]=version,["command"]=SelectionJson.Object(command.BodyJson)["command"]};
            var request=new RunApiHttpRequest("POST","/v1/runs/"+Uri.EscapeDataString(scope.RunId)+"/augment-grants/"+Uri.EscapeDataString(scope.EventId)+(reroll?"/reroll":"/select"),body.ToString(Formatting.None),new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("Idempotency-Key",command.Key)});
            var response=await transport.SendAsync(request,auth.AccessToken);
            cancellation.ThrowIfCancellationRequested();if(!ownerCurrent()||!ReferenceEquals(session(),auth))throw new OperationCanceledException("Selection response belongs to previous session");
            if(response==null||response.CanRetryNetwork||response.RequiresAuthRefreshOrGuestLogin||response.Response==null)throw new IOException("Selection response requires original-key recovery");
            var raw=response.Response;
            if(raw.StatusCode==409&&raw.BodyJson!=null&&raw.BodyJson.Length<=4096)
            {
                var rejection=SelectionJson.Object(raw.BodyJson);SelectionJson.Keys(rejection,"accepted","errorCode");
                string code=rejection["errorCode"]?.Type==JTokenType.String?(string)rejection["errorCode"]:null;
                if(rejection["accepted"]?.Type==JTokenType.Boolean&&!(bool)rejection["accepted"]&&
                   (code=="augment_selection_revision_conflict"||code=="augment_selection_candidate_mismatch"||code=="augment_selection_selection_closed"))
                    throw new AugmentSelectionRejectedException(code,true);
                // request_conflict can refer to another body bound to this key; retain recovery evidence.
            }
            if(raw.StatusCode!=200||raw.BodyJson==null||raw.BodyJson.Length>2*1024*1024)throw new IOException("Selection commit not confirmed");
            var envelope=SelectionJson.Object(raw.BodyJson);SelectionJson.Keys(envelope,reroll?new[]{"version","committed","result","targetReadout"}:new[]{"version","committed","result"});
            if((string)envelope["version"]!=version||envelope["committed"]?.Type!=JTokenType.Boolean||!(bool)envelope["committed"]||envelope["result"] is not JObject result)throw new FormatException("Selection response envelope");
            if(reroll){
                var readout=envelope["targetReadout"] as JObject??throw new FormatException("Reroll readout missing");
                if((string)readout["version"]!="augment.target-readout.v1.20261021")throw new FormatException("Reroll readout version");
                foreach(string field in new[]{"runId","eventId","catalogVersion","grantRevision"})if(!JToken.DeepEquals(readout[field],result["snapshot"]?[field]))throw new FormatException("Reroll display revision differs");
                // The host retains its pinned catalog resolver. This adjunct is
                // not another selection owner and does not trigger a new GET.
            }
            return result.ToString(Formatting.None);
        }
    }
}
