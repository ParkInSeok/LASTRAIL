using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class CombatRequirements
    {
        readonly JObject data;
        public string Version=>(string)data["version"];
        public bool CanStart=>false; // This version specifies BLOCKED only. No client ready override.
        public int? RiskLevel=>(int?)data["riskLevel"];
        public int? OperatorSlotCount=>(int?)data["operatorSlotCount"];
        public bool? FirstClearEligible=>(bool?)data["firstClearEligible"];
        public string UltimateOperatorId=>(string)data["ultimateOperatorId"];
        public long? UltimateSelectionRevision=>(long?)data["ultimateSelectionRevision"];
        public string[] BlockingReasons=>((JArray)data["blockingReasons"]).Values<string>().ToArray();
        public JObject RequiredContent=>(JObject)data["requiredContent"].DeepClone();
        public CombatRequirements(JObject value)
        {
            data=(JObject)value.DeepClone();
            var version=GameApiJson.String(data,"version");if((version!="run-combat-requirements-v1"&&version!="run-combat-requirements-v2"&&version!="run-combat-requirements-v3")||GameApiJson.String(data,"status")!="BLOCKED"||GameApiJson.Boolean(data,"canStart"))throw new FormatException();
            var reasons=RecruitmentJson.Array(data,"blockingReasons");
            var expected=new[]{"enemy_faction_binding_missing","wave_plan_binding_missing","reward_table_binding_missing","combat_seed_contract_missing","engine_output_profile_unapproved","ultimate_selection_contract_missing","effective_stats_profile_unapproved"};
            if(version!="run-combat-requirements-v1") {
                var id=RecruitmentJson.NullableString(data,"ultimateOperatorId");if(id!=null)RecruitmentJson.Id(data,"ultimateOperatorId");
                if(data["ultimateSelectionRevision"]?.Type!=JTokenType.Null)UltimateSelectionSnapshot.RevisionOf(data,"ultimateSelectionRevision");
                if(id!=null&&data["ultimateSelectionRevision"]?.Type!=JTokenType.Integer)throw new FormatException();
                expected=expected.Where(x=>x!="ultimate_selection_contract_missing").Concat(id==null?new[]{"ultimate_selection_required"}:Array.Empty<string>()).ToArray();
            }else Null(data,"ultimateOperatorId");
            if(version=="run-combat-requirements-v3")expected=ValidateDiagnostics(expected);
            if(reasons.Count!=expected.Length||reasons.Any(t=>t.Type!=JTokenType.String)||!reasons.Values<string>().OrderBy(x=>x).SequenceEqual(expected.OrderBy(x=>x)))throw new FormatException();
            var tier=RecruitmentJson.NullableString(data,"riskTier");
            if(tier==null){Null(data,"riskLevel");}else if(!DepartureContract.Risks.Contains(tier)||AccountStateSnapshot.Number(data,"riskLevel")!=Array.IndexOf(DepartureContract.Risks,tier)+1)throw new FormatException();
            if(data["operatorSlotCount"]?.Type!=JTokenType.Null&&AccountStateSnapshot.Number(data,"operatorSlotCount")!=5)throw new FormatException();
            if(data["firstClearEligible"]?.Type!=JTokenType.Null)GameApiJson.Boolean(data,"firstClearEligible");
            foreach(var field in new[]{"enemyFactionId","wavePlanId","rewardTableId","seed","engineOutput"})Null(data,field);
            var content=GameApiJson.Object(data,"requiredContent");foreach(var field in new[]{"operationId","contentVersion","startAugmentPackageRef","startTacticPackageRef","encounterType","raidId","bossId"})RecruitmentJson.NullableString(content,field);
            if(RecruitmentJson.Array(content,"bossPhaseRefs").Any(t=>t.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)t)))throw new FormatException();
            Null(content,"bindingVersion");Null(content,"catalogHash");
        }
        // These are diagnostic requirements, never an executable combat profile.
        string[] ValidateDiagnostics(string[] expected)
        {
            var binding=GameApiJson.Object(data,"policyBinding");
            if(GameApiJson.String(binding,"version")!="combat.policy-binding.v1"||GameApiJson.Boolean(binding,"canExecute"))throw new FormatException();
            var status=GameApiJson.String(binding,"status");
            if(status!="HISTORICAL"&&status!="INVALID"&&status!="EXECUTION_UNAVAILABLE")throw new FormatException();
            var policy=RecruitmentJson.NullableString(binding,"policyVersion");
            var effects=Strings(binding,"requiredEffectKinds");var blocked=Strings(binding,"blockingReasons");
            var operators=RecruitmentJson.Array(binding,"operators");
            if(status=="EXECUTION_UNAVAILABLE"){
                if(string.IsNullOrWhiteSpace(policy)||operators.Count!=5||effects.Length==0||!blocked.OrderBy(x=>x).SequenceEqual(effects.Select(x=>"combat_effect_handler_missing:"+x).OrderBy(x=>x)))throw new FormatException();
                var ids=new System.Collections.Generic.HashSet<string>();var slots=new System.Collections.Generic.HashSet<long>();
                var actualEffects=new System.Collections.Generic.HashSet<string>();
                foreach(var token in operators){
                    var row=token as JObject;if(row==null)throw new FormatException();
                    var id=RecruitmentJson.Id(row,"operatorId");var slot=AccountStateSnapshot.Number(row,"slot");
                    if(!ids.Add(id)||!slots.Add(slot)||slot>=5||GameApiJson.String(row,"role")!="OPERATOR"||RecruitmentJson.Id(GameApiJson.Object(row,"passive"),"operatorId")!=id)throw new FormatException();
                    var abilities=RecruitmentJson.Array(row,"abilities");if(abilities.Count!=2)throw new FormatException();
                    var abilityIds=new System.Collections.Generic.HashSet<string>();
                    foreach(var ability in abilities){var a=ability as JObject;if(a==null)throw new FormatException();var aid=GameApiJson.String(a,"id");if((aid!=id+".active"&&aid!=id+".ultimate")||!abilityIds.Add(aid))throw new FormatException();actualEffects.Add(GameApiJson.String(a,"effect"));}
                }
                if(!actualEffects.SetEquals(effects))throw new FormatException();
            }else if(operators.Count!=0||effects.Length!=0||(status=="HISTORICAL"&&(policy!=null||blocked.Length!=0))||(status=="INVALID"&&blocked.Length==0))throw new FormatException();
            var authority=GameApiJson.Object(data,"authority");var authorityStatus=GameApiJson.String(authority,"status");
            if(authorityStatus=="PINNED")ValidateAuthority(GameApiJson.Object(authority,"contract"));
            else {if(authorityStatus!="HISTORICAL"&&authorityStatus!="INVALID")throw new FormatException();Null(authority,"contract");}
            return expected.Where(x=>x!="ultimate_selection_required"||status=="HISTORICAL").Concat(blocked).Concat(authorityStatus=="INVALID"?new[]{"combat_authority_contract_invalid"}:Array.Empty<string>()).ToArray();
        }
        static string[] Strings(JObject value,string name)
        {
            var array=RecruitmentJson.Array(value,name);if(array.Any(x=>x.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)x)))throw new FormatException();
            var result=array.Values<string>().ToArray();if(result.Distinct().Count()!=result.Length)throw new FormatException();return result;
        }
        internal static void ValidateAuthority(JObject value)
        {
            var mode=GameApiJson.String(value,"mode");bool world=mode=="WORLD_BOSS_SOLO"||mode=="WORLD_BOSS_COOP";
            if(!world&&mode!="ORDINARY_SOLO"&&mode!="ORDINARY_COOP")throw new FormatException();
            if(value.Count!=7||GameApiJson.String(value,"version")!="combat.authority-split.v1"||GameApiJson.String(value,"combatOwner")!=(world?"SERVER":mode=="ORDINARY_COOP"?"UNITY_HOST":"UNITY_LOCAL")||GameApiJson.String(value,"combatClock")!=(world?"SERVER_COMBAT":mode=="ORDINARY_COOP"?"HOST_LOGICAL":"LOCAL_LOGICAL")||GameApiJson.String(value,"transactionOwner")!="SERVER"||GameApiJson.String(value,"resultVerificationOwner")!="SERVER"||GameApiJson.Boolean(value,"clientReportProvesCombat"))throw new FormatException();
        }
        internal void ValidateAdmission(JObject admission)
        {
            var version=GameApiJson.String(admission,"version");
            var modern=version=="departure-v3"||version=="departure-v4";
            if(Version!=(modern?"run-combat-requirements-v3":"run-combat-requirements-v2"))throw new FormatException();
            var choice=GameApiJson.Object(admission,"ultimateSelection");
            if(UltimateOperatorId!=(string)choice["operatorId"]||UltimateSelectionRevision!=(long)choice["revision"])throw new FormatException();
            if(!modern)return;
            var authority=GameApiJson.Object(data,"authority");
            if(admission.ContainsKey("combatAuthority")){
                if((string)authority["status"]!="PINNED"||!JToken.DeepEquals(authority["contract"],admission["combatAuthority"]))throw new FormatException();
            }else if((string)authority["status"]!="HISTORICAL")throw new FormatException();
            var binding=GameApiJson.Object(data,"policyBinding");
            if(version=="departure-v4"){
                var policy=GameApiJson.Object(admission,"combatPolicyRequirements");
                if((string)binding["status"]=="HISTORICAL"||((string)binding["policyVersion"]!=null&&(string)binding["policyVersion"]!=GameApiJson.String(policy,"version")))throw new FormatException();
                if((string)binding["status"]=="EXECUTION_UNAVAILABLE")foreach(var row in (JArray)binding["operators"]){
                    if(!((JArray)admission["formation"]["operators"]).Any(x=>JToken.DeepEquals(x["slot"],row["slot"])&&JToken.DeepEquals(x["operatorId"],row["operatorId"])))throw new FormatException();
                }
            }else if((string)binding["status"]!="HISTORICAL")throw new FormatException();
        }
        static void Null(JObject r,string name){if(r[name]?.Type!=JTokenType.Null)throw new FormatException();}
    }
    public sealed class DepartureRecoverySnapshot
    {
        readonly JObject data;
        public string RunId=>(string)data["runId"];
        public string RunStatus=>(string)data["runStatus"];
        public bool Available=>(string)data["recoveryStatus"]=="AVAILABLE";
        public JObject StartResponse=>data["startResponse"] is JObject r?(JObject)r.DeepClone():null;
        public DepartureRecoverySnapshot(string json,string expectedRun)
        {
            data=GameApiJson.ReadObject(json);if(!GameApiJson.Boolean(data,"accepted")||RecruitmentJson.Id(data,"runId")!=expectedRun)throw new FormatException();GameApiJson.String(data,"runStatus");
            var status=GameApiJson.String(data,"recoveryStatus");
            if(status=="AVAILABLE"){
                var original=GameApiJson.Object(data,"startResponse");if(!GameApiJson.Boolean(original,"accepted")||GameApiJson.String(original,"startStatus")!="STARTED"||GameApiJson.String(original,"runId")!=expectedRun)throw new FormatException();
                // Preserve old responses verbatim; do not require or add today's fields.
            }else if(status!="SNAPSHOT_UNAVAILABLE"||data["startResponse"]?.Type!=JTokenType.Null)throw new FormatException();
        }
    }
    public sealed partial class AccountScreenRuntime
    {
        long departureRecoveryRequest;
        public Func<string> OriginalRunId {get;set;}
        public string KnownDepartureRunId=>RunSelection.RunId;
        public DepartureRecoverySnapshot DepartureRecovery {get;private set;}
        public bool DepartureRecoveryLoading {get;private set;}
        public string DepartureRecoveryError {get;private set;}
        public string DepartureRecoveryNotice {get;private set;}="";
        string recoveryTarget;
        void ResetDepartureRecovery(){departureRecoveryRequest++;DepartureRecovery=null;DepartureRecoveryLoading=false;DepartureRecoveryError=null;DepartureRecoveryNotice="";recoveryTarget=null;}
        public Task EnsureDepartureRecoveryAsync(string runId){
            SynchronizeSession();
            if(!Current(owner,ownerGeneration))return Task.CompletedTask;
            if(DepartureRecoveryLoading&&recoveryTarget==runId||DepartureRecovery?.RunId==runId&&DepartureRecovery.Available&&DepartureRecoveryError==null)return Task.CompletedTask;
            if(Pending!=null&&Pending.Account==owner.AccountId&&Pending.Kind=="departure-start"&&Pending.StartResultJson!=null){
                var original=new DepartureStartResult(200,Pending.StartResultJson,Pending.Target);
                if(original.RunId==runId){DepartureRecoveryNotice="보관된 출발 기록을 사용합니다.";return Task.CompletedTask;}
            }
            return RefreshDepartureRecoveryAsync(runId);
        }
        public async Task RefreshDepartureRecoveryAsync(string runId)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(!Current(original,stamp))return;
            var id=RecruitmentJson.Id(new JObject{["id"]=runId},"id");var request=++departureRecoveryRequest;
            if(recoveryTarget!=id)DepartureRecovery=null;recoveryTarget=id;DepartureRecoveryLoading=true;DepartureRecoveryError=null;DepartureRecoveryNotice="출발 기록을 확인하고 있습니다.";Changed?.Invoke();
            try{
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString(id)+"/start-state",null,null),original.AccessToken));
                if(!Current(original,stamp)||request!=departureRecoveryRequest)return;
                if(!response.IsNetworkError&&response.StatusCode==200){
                    DepartureRecovery=new DepartureRecoverySnapshot(response.BodyJson,id);
                    DepartureRecoveryNotice=DepartureRecovery.Available?"출발 기록을 확인했습니다. 전투 시작 상태를 확인한 뒤 다시 준비합니다.":"출발 기록을 복원하지 못했습니다. 잠시 후 다시 확인해 주세요.";
                }else{
                    string error=null;try{error=GameApiJson.String(GameApiJson.ReadObject(response.BodyJson),"errorCode");}catch{}
                    DepartureRecoveryError=error??(response.IsNetworkError?"network_error":"http_"+response.StatusCode);
                    DepartureRecoveryNotice=response.StatusCode==401?"로그인 상태를 다시 확인해 주세요.":response.StatusCode==404?"이 계정의 출발 기록을 찾지 못했습니다.":"출발 기록을 불러오지 못했습니다. 다시 확인해 주세요.";
                }
            }catch{if(!Current(original,stamp)||request!=departureRecoveryRequest)return;DepartureRecoveryError="invalid_response";DepartureRecoveryNotice="출발 기록을 확인하지 못했습니다. 다시 확인해 주세요.";}
            if(Current(original,stamp)&&request==departureRecoveryRequest){DepartureRecoveryLoading=false;Changed?.Invoke();}
        }
    }
}

