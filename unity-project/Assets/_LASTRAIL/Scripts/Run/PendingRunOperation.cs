using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Submissions;
using Newtonsoft.Json.Linq;

namespace LastRail.Run
{
    public enum PendingRunKind { Start, Finish, Claim, InitializeOrdinary, CompleteOrdinaryNode, TravelCommand, MapArrival, ConfirmDestination }

    public sealed class PendingRunOperation
    {
        internal const int MaximumEncodedResponse = 65536;
        internal const int MaximumStartResponse = 131072;
        public string ResponseVersion { get; }
        public long? OrdinaryCheckpointRevision { get; }
        internal int ReservedResponseCharacters => ResponseVersion == RunCompleteClaimReceipt.Version
            ? RunCompleteClaimReceipt.MaximumEncodedCharacters : Kind == PendingRunKind.Start ? MaximumStartResponse : MaximumEncodedResponse;
        public PendingRunKind Kind { get; }
        public string AccountId { get; }
        public string TargetId { get; }
        public string IdempotencyKey { get; }
        public string BodyJson { get; }
        public string MapArrivalBindingJson { get; }
        public AppBuildSnapshot Build { get; }
        public DateTimeOffset ComposedAt { get; }
        public string AcceptedResponseJson { get; }
        public string EndpointPath => Kind == PendingRunKind.ConfirmDestination ? "/v1/runs/"+Uri.EscapeDataString(TargetId)+"/operation-map/destination" : Kind == PendingRunKind.MapArrival ? "/v1/runs/"+Uri.EscapeDataString(TargetId)+"/b-route/transition" : Kind == PendingRunKind.TravelCommand ? "/v1/runs/"+Uri.EscapeDataString(TargetId)+"/b-arrival-reports" : Kind == PendingRunKind.CompleteOrdinaryNode ? "/v1/runs/"+Uri.EscapeDataString(TargetId)+"/operation-map/completion" : Kind == PendingRunKind.InitializeOrdinary ? "/v1/runs/"+TargetId+"/ordinary-initialization" : Kind == PendingRunKind.Start ? "/v1/runs/start" : Kind == PendingRunKind.Claim ? "/v1/runs/"+TargetId+"/reward-preview/"+GameApiJson.String(Object(BodyJson),"rewardPreviewId")+"/claim" : "/v1/runs/" + TargetId + "/finish";
        internal PendingRunOperation(PendingRunKind kind, string account, string target, string key, string body,
            AppBuildSnapshot build, DateTimeOffset composedAt, string acceptedResponse = null)
            : this(kind,account,target,key,body,build,composedAt,acceptedResponse,null) { }
        internal PendingRunOperation(PendingRunKind kind, string account, string target, string key, string body,
            AppBuildSnapshot build, DateTimeOffset composedAt, string acceptedResponse, string responseVersion)
            :this(kind,account,target,key,body,build,composedAt,acceptedResponse,responseVersion,null) { }
        internal PendingRunOperation(PendingRunKind kind, string account, string target, string key, string body,
            AppBuildSnapshot build, DateTimeOffset composedAt, string acceptedResponse, string responseVersion,long? ordinaryCheckpointRevision)
            :this(kind,account,target,key,body,build,composedAt,acceptedResponse,responseVersion,ordinaryCheckpointRevision,null) { }
        internal PendingRunOperation(PendingRunKind kind, string account, string target, string key, string body,
            AppBuildSnapshot build, DateTimeOffset composedAt, string acceptedResponse, string responseVersion,long? ordinaryCheckpointRevision,string mapArrivalBindingJson)
        {
            if((kind==PendingRunKind.MapArrival)!=(mapArrivalBindingJson!=null))throw new ArgumentException("Map arrival requires its original route binding.");
            MapArrivalBindingJson=mapArrivalBindingJson;
            if(ordinaryCheckpointRevision.HasValue&&(kind!=PendingRunKind.InitializeOrdinary||ordinaryCheckpointRevision<0||ordinaryCheckpointRevision>0&&acceptedResponse==null))throw new ArgumentException("Invalid ordinary checkpoint revision.");
            OrdinaryCheckpointRevision=ordinaryCheckpointRevision;
            if(responseVersion!=null&&(kind!=PendingRunKind.Claim||responseVersion!=RunCompleteClaimReceipt.Version))
                throw new ArgumentException("Unsupported run response version.");
            ResponseVersion=responseVersion;
            if (!Enum.IsDefined(typeof(PendingRunKind), kind) || !PendingReceipt.Identifier(account, 1)
                || !PendingReceipt.Identifier(target, 1) || !RunRequestKey.IsSupported(key,kind==PendingRunKind.ConfirmDestination||kind==PendingRunKind.MapArrival||kind==PendingRunKind.TravelCommand||kind==PendingRunKind.InitializeOrdinary||kind==PendingRunKind.CompleteOrdinaryNode?1:16,kind==PendingRunKind.TravelCommand?200:128)
                || (kind==PendingRunKind.ConfirmDestination||kind==PendingRunKind.MapArrival||kind==PendingRunKind.TravelCommand||kind==PendingRunKind.InitializeOrdinary||kind==PendingRunKind.CompleteOrdinaryNode)&&key.Contains(" "))
                throw new ArgumentException("Invalid run operation identity.");
            Kind = kind; AccountId = account; TargetId = target; IdempotencyKey = key;
            BodyJson = body; Build = build ?? new AppBuildSnapshot(null, null); ComposedAt = composedAt.ToUniversalTime();
            ValidateBody();
            if (acceptedResponse != null) ValidateAccepted(acceptedResponse);
            AcceptedResponseJson = acceptedResponse;
        }
        public static PendingRunOperation Start(string account, RunStartRequest request, AppBuildSnapshot build, DateTimeOffset composedAt)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return new PendingRunOperation(PendingRunKind.Start, account, request.OperationPreviewId, request.IdempotencyKey, request.BodyJson, build, composedAt);
        }
        public static PendingRunOperation Travel(string account,string run,BTravelCommand command,AppBuildSnapshot build,DateTimeOffset composedAt)
        {
            if(command==null)throw new ArgumentNullException(nameof(command));
            return new PendingRunOperation(PendingRunKind.TravelCommand,account,run,command.RequestId,command.BodyJson,build,composedAt);
        }
        public static PendingRunOperation ConfirmDestination(string account,BOperationDestinationIntent intent,AppBuildSnapshot build,DateTimeOffset composedAt)
        {
            if(intent==null||intent.AccountId!=account)throw new ArgumentException("Original destination intent required.");
            return new PendingRunOperation(PendingRunKind.ConfirmDestination,account,intent.RunId,intent.CommandId,intent.Body().ToString(Newtonsoft.Json.Formatting.None),build,composedAt);
        }
        public BOperationDestinationIntent ReadDestinationIntent()
        {
            if(Kind!=PendingRunKind.ConfirmDestination)throw new InvalidOperationException("Not a destination confirmation.");
            var body=Object(BodyJson);LastRail.Battle.BDirectAttackInput.Fields(body,"expectedRevision","edgeId");
            return new BOperationDestinationIntent(AccountId,TargetId,IdempotencyKey,LastRail.Battle.BDirectAttackInput.Nat(body,"expectedRevision",1),LastRail.Battle.BDirectAttackInput.Text(body,"edgeId"));
        }
        public static PendingRunOperation ArriveMap(string account,BRouteCommand command,AppBuildSnapshot build,DateTimeOffset composedAt)
        {
            if(command==null||command.IsWave||command.Kind!="ARRIVE"||command.Binding.AccountId!=account)throw new ArgumentException("Original map arrival required.");
            return new PendingRunOperation(PendingRunKind.MapArrival,account,command.Binding.RunId,command.EventId,command.Capture().ToString(Newtonsoft.Json.Formatting.None),build,composedAt,null,null,null,command.Binding.Capture().ToString(Newtonsoft.Json.Formatting.None));
        }
        public BRouteCommand ReadMapArrivalCommand()
        {
            if(Kind!=PendingRunKind.MapArrival)throw new InvalidOperationException("Not a map arrival.");
            var packet=Object(MapArrivalBindingJson);var binding=BRouteBinding.Read(MapArrivalBindingJson,(string)packet["packetHash"],AccountId,TargetId);
            var body=Object(BodyJson);LastRail.Battle.BDirectAttackInput.Fields(body,"version","command");
            if((string)body["version"]!=BRouteBinding.Version)throw new FormatException("Map arrival version differs.");
            var command=BRouteCommand.FromOriginal(binding,body["command"] as JObject);
            if(command.Kind!="ARRIVE"||command.EventId!=IdempotencyKey)throw new FormatException("Map arrival identity differs.");return command;
        }
        internal bool ReplacesConfirmedTravel(PendingRunOperation previous)=>(Kind==PendingRunKind.TravelCommand||Kind==PendingRunKind.MapArrival||Kind==PendingRunKind.ConfirmDestination)
            &&previous?.Kind==Kind&&previous.AccountId==AccountId&&previous.TargetId==TargetId&&previous.AcceptedResponseJson!=null
            &&(Kind!=PendingRunKind.ConfirmDestination||ReadDestinationIntent().ExpectedRevision>previous.ReadDestinationIntent().ExpectedRevision);
        public static PendingRunOperation Finish(string account, RunResultSubmitRequest request, AppBuildSnapshot build, DateTimeOffset composedAt)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return new PendingRunOperation(PendingRunKind.Finish, account, request.RunId, request.IdempotencyKey, request.BodyJson, build, composedAt);
        }
        public static PendingRunOperation Claim(string account, RunRewardClaimRequest request, AppBuildSnapshot build, DateTimeOffset composedAt)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            return new PendingRunOperation(PendingRunKind.Claim,account,request.RunId,request.IdempotencyKey,request.BodyJson,build,composedAt);
        }
        public PendingRunOperation Confirmed(string acceptedResponseJson)
        {
            ValidateAccepted(acceptedResponseJson);
            // Existing character-bound receipts remain recoverable; new START writes also honor raw UTF-8.
            if(Kind==PendingRunKind.Start&&AcceptedResponseJson==null&&Encoding.UTF8.GetByteCount(acceptedResponseJson)>MaximumStartResponse)
                throw new FormatException("START response exceeds its UTF-8 budget.");
            return new PendingRunOperation(Kind, AccountId, TargetId, IdempotencyKey, BodyJson, Build, ComposedAt,
                AcceptedResponseJson ?? acceptedResponseJson, ResponseVersion,OrdinaryCheckpointRevision,MapArrivalBindingJson);
        }
        internal PendingRunOperation WithOrdinaryCheckpoint(long revision){
            if(Kind!=PendingRunKind.InitializeOrdinary||AcceptedResponseJson==null||!OrdinaryCheckpointRevision.HasValue||revision<1||OrdinaryCheckpointRevision>0&&OrdinaryCheckpointRevision!=revision)throw new InvalidOperationException("Original first checkpoint transition required.");
            return new PendingRunOperation(Kind,AccountId,TargetId,IdempotencyKey,BodyJson,Build,ComposedAt,AcceptedResponseJson,ResponseVersion,revision);
        }
        public static PendingRunOperation CompleteClaim(string account,RunRewardClaimRequest request,AppBuildSnapshot build,DateTimeOffset composedAt)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            return new PendingRunOperation(PendingRunKind.Claim,account,request.RunId,request.IdempotencyKey,request.BodyJson,build,composedAt,null,RunCompleteClaimReceipt.Version);
        }
        internal bool SameRequest(PendingRunOperation value) => value != null && Kind == value.Kind && AccountId == value.AccountId
            && TargetId == value.TargetId && IdempotencyKey == value.IdempotencyKey && BodyJson == value.BodyJson
            && ResponseVersion == value.ResponseVersion && MapArrivalBindingJson==value.MapArrivalBindingJson && Build.AppVersion == value.Build.AppVersion && Build.AppVersionCode == value.Build.AppVersionCode && ComposedAt == value.ComposedAt;
        private void ValidateBody()
        {
            if(Kind==PendingRunKind.ConfirmDestination){ReadDestinationIntent();return;}
            if(Kind==PendingRunKind.MapArrival){ReadMapArrivalCommand();return;}
            if(Kind==PendingRunKind.TravelCommand){if(BTravelCommand.Read(BodyJson).RequestId!=IdempotencyKey)throw new FormatException("Travel request identity differs.");return;}
            var root = Object(BodyJson);
            if(Kind==PendingRunKind.CompleteOrdinaryNode){OrdinaryNodeCompletion.ValidateBody(BodyJson);return;}
            if(Kind==PendingRunKind.InitializeOrdinary){if(BodyJson!="{}")throw new FormatException("Initialization requires the original empty body.");return;}
            if(Kind==PendingRunKind.Claim){if(root.Count!=2||GameApiJson.String(root,"runId")!=TargetId||!PendingReceipt.Identifier(GameApiJson.String(root,"rewardPreviewId"),1))throw new FormatException("Invalid reward claim body.");return;}
            if (Kind == PendingRunKind.Start)
            {
                if (root.Count != 1 || GameApiJson.String(root, "operationPreviewId") != TargetId) throw new FormatException("Invalid run start body.");
                return;
            }
            var allowed = new[] { "runId", "encounterType", "currentStage", "currentWaveNumber", "completedWaveCount", "outcome",
                "raidResult", "startAugmentPackageRef", "startTacticPackageRef", "runtimeStateJson", "tacticSelections", "clientCompletedAt" };
            if (root.Properties().Any(p => !allowed.Contains(p.Name)) || GameApiJson.String(root, "runId") != TargetId
                || GameApiJson.String(root, "currentStage") != "Complete" || !(root["tacticSelections"] is JArray)
                || root["currentWaveNumber"]?.Type != JTokenType.Integer || root["completedWaveCount"]?.Type != JTokenType.Integer)
                throw new FormatException("Invalid run finish body.");
            var encounter = GameApiJson.String(root, "encounterType"); var outcome = GameApiJson.String(root, "outcome");
            if (encounter != "ORDINARY_15_WAVE" && encounter != "RAID_BOSS" || outcome != "VICTORY" && outcome != "FAILURE" && outcome != "ABANDONED")
                throw new FormatException("Invalid run finish classification.");
            ReadTime(root, "clientCompletedAt");
            var runtime = Object(GameApiJson.String(root, "runtimeStateJson"));
            if (GameApiJson.String(runtime, "runId") != TargetId) throw new FormatException("Run runtime identity mismatch.");
        }
        private void ValidateAccepted(string response)
        {
            if (response == null || response.Length > ReservedResponseCharacters || new JValue(response).ToString(Newtonsoft.Json.Formatting.None).Length > ReservedResponseCharacters)
                throw new FormatException("Run response exceeds its reserved storage budget.");
            if(Kind==PendingRunKind.ConfirmDestination){BOperationDestinationReceipt.Read(response,ReadDestinationIntent());return;}
            if(Kind==PendingRunKind.MapArrival){var result=BRouteCommandResult.Read(200,response,ReadMapArrivalCommand());if(!result.Committed)throw new FormatException("Map arrival unconfirmed.");return;}
            if(Kind==PendingRunKind.TravelCommand){BTravelReceipt.Read(response,BTravelCommand.Read(BodyJson)).RequireScope(AccountId,TargetId);return;}
            if(ResponseVersion==RunCompleteClaimReceipt.Version){
                var complete=RunCompleteClaimReceipt.Read(response,AccountId,TargetId,GameApiJson.String(Object(BodyJson),"rewardPreviewId"),IdempotencyKey);
                CheckSensitiveFields(complete.Capture());return;
            }
            var root = Object(response);
            if(Kind==PendingRunKind.CompleteOrdinaryNode){OrdinaryNodeCompletion.ValidateResponse(response,BodyJson,IdempotencyKey);return;}
            if(Kind==PendingRunKind.InitializeOrdinary){OrdinaryInitializationReceipt.ValidateEnvelope(root,AccountId,TargetId);return;}
            if(Kind==PendingRunKind.Claim){
                if(root["version"]!=null){RunCompleteClaimReceipt.Read(response,AccountId,TargetId,GameApiJson.String(Object(BodyJson),"rewardPreviewId"),IdempotencyKey);return;}
                var allowed=new[]{"runId","rewardPreviewId","rewardClaimId","status","serverClaimedAt","balanceVersion","rewardLines","errorCode","message"};
                if(root.Properties().Any(p=>!allowed.Contains(p.Name))||GameApiJson.String(root,"runId")!=TargetId||GameApiJson.String(root,"rewardPreviewId")!=GameApiJson.String(Object(BodyJson),"rewardPreviewId")||!PendingReceipt.Identifier(GameApiJson.String(root,"rewardClaimId"),1))throw new FormatException("Invalid reward receipt identity.");
                var status=GameApiJson.String(root,"status");if(status!="CLAIMED"&&status!="DUPLICATE_REPLAY")throw new FormatException("Unconfirmed claim.");
                ReadTime(root,"serverClaimedAt");GameApiJson.String(root,"balanceVersion");
                var lines=root["rewardLines"];if(lines!=null&&!(lines is JArray)||status=="CLAIMED"&&!(lines is JArray))throw new FormatException("Invalid reward receipt lines.");
                if(lines is JArray array)foreach(var line in array){if(!(line is JObject row)||row["quantity"]?.Type!=JTokenType.Integer||(long)row["quantity"]<0||!PendingReceipt.Identifier(GameApiJson.String(row,"id"),1))throw new FormatException("Invalid reward line.");GameApiJson.String(row,"kind");GameApiJson.String(row,"source");}
                var code=GameApiJson.String(root,"errorCode",true);if(code!=null&&!(status=="DUPLICATE_REPLAY"&&code=="duplicate_replay"))throw new FormatException("Unexpected receipt error.");return;
            }
            var common = new[] { "accepted", "runId", "balanceVersion", "message", "errorCode" };
            var fields = Kind == PendingRunKind.Start
                ? new[] { "startStatus", "operationPreviewId", "operationId", "actId", "regionId", "encounterType", "riskLevel", "risk",
                    "contentVersion", "ordinary", "raid", "raidId", "bossId", "bossPhaseRefs", "allowedOperatorSlots",
                    "startAugmentPackageRef", "startTacticPackageRef", "runVersionSnapshot", "serverStartedAt" }
                : new[] { "resultStatus", "rewardClaimId", "rewardPreviewId", "serverReceivedAt", "staminaRefund", "runStartVersionSnapshot", "runFinishVersionSnapshot" };
            if (root.Properties().Any(p => !common.Contains(p.Name) && !fields.Contains(p.Name))) throw new FormatException("Unexpected run response field.");
            if (!PendingReceipt.Identifier(GameApiJson.String(root, "runId"), 1)) throw new FormatException("Invalid accepted run reference.");
            GameApiJson.String(root, "balanceVersion");
            if (Kind == PendingRunKind.Start)
            {
                if (!GameApiJson.Boolean(root, "accepted") || GameApiJson.String(root, "startStatus") != "STARTED"
                    || GameApiJson.String(root, "operationPreviewId") != TargetId) throw new FormatException("Run start was not accepted.");
                ReadTime(root, "serverStartedAt"); Snapshot(root, "runVersionSnapshot", "server_accepted_run_start");
            }
            else
            {
                var status = GameApiJson.String(root, "resultStatus"); var accepted = GameApiJson.Boolean(root, "accepted");
                if (GameApiJson.String(root, "runId") != TargetId || !(status == "ACCEPTED" && accepted || status == "DUPLICATE_REPLAY" && !accepted)
                    || !PendingReceipt.Identifier(GameApiJson.String(root, "rewardPreviewId"), 1)) throw new FormatException("Run finish was not accepted.");
                ReadTime(root, "serverReceivedAt"); Snapshot(root, "runStartVersionSnapshot", "server_accepted_run_start");
                Snapshot(root, "runFinishVersionSnapshot", "server_accepted_run_finish");
            }
            var message = GameApiJson.String(root, "message", true);
            var error = GameApiJson.String(root, "errorCode", true);
            if (message != null && message != "Run finish was already accepted." || error != null && error != "duplicate_replay")
                throw new FormatException("Unexpected accepted response message.");
        }
        private static void Snapshot(JObject root, string name, string source)
        {
            if (root[name] == null || root[name].Type == JTokenType.Null) return;
            if (!(root[name] is JObject value) || GameApiJson.String(value, "source") != source) throw new FormatException("Invalid server run version snapshot.");
        }
        private static JObject Object(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 262144) throw new FormatException("Invalid run JSON size.");
            var root = GameApiJson.ReadObject(json);
            CheckSensitiveFields(root);
            return root;
        }
        private static void CheckSensitiveFields(JObject root)
        {
            var forbidden = new[] { "authorization", "accesstoken", "refreshtoken", "password", "idtoken", "secret", "token" };
            if (root.Descendants().OfType<JProperty>().Any(p => forbidden.Contains(p.Name.ToLowerInvariant())))
                throw new FormatException("Sensitive fields are not run data.");
        }
        internal static DateTimeOffset ReadTime(JObject root, string field) => DateTimeOffset.ParseExact(GameApiJson.String(root, field),
            new[] { "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'" }, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
    public interface IRunOperationJournal
    {
        Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account);
        Task<bool> PutAsync(PendingRunOperation operation);
        Task<bool> ConfirmAsync(PendingRunOperation operation, string responseJson);
        Task<bool> RemoveConfirmedAsync(string account, PendingRunKind kind, string key);
    }
    public interface IOrdinaryCheckpointJournal {
        Task<bool> ConfirmFirstCheckpointAsync(PendingRunOperation operation,long revision);
    }
    public interface ITravelConflictJournal {
        Task<bool> ReplaceRejectedTravelAsync(PendingRunOperation rejected,PendingRunOperation recovery,string conflictJson);
    }
}
