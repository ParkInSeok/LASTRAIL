using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class DepartureQuote
    {
        readonly JObject data;
        public string Id=>(string)data["operationPreviewId"];
        public string OperationId=>(string)data["operationId"];
        public string Risk=>(string)data["risk"];
        public long FuelCost=>(long)data["quote"]["fuelCost"];
        public long AvailableFuel=>(long)data["quote"]["availableFuel"];
        public DateTimeOffset ExpiresAt {get;}
        public JObject Admission=>(JObject)data["admission"].DeepClone();
        public CombatRequirements Combat {get;} public CombatRewardReference RewardReference {get;}
        public bool CurrentVersion=>new[]{"departure-v2","departure-v3","departure-v4"}.Contains((string)data["admission"]["version"]);
        public bool? FirstClearEligible=>(bool?)data["firstClearEligible"];
        public string SelectionChangeNotice(long formation,long loadout,long ultimate)
        {
            var a=data["admission"];int changed=(formation!=(long)a["formationRevision"]?1:0)|(loadout!=(long)a["loadoutRevision"]?2:0)|(ultimate!=((long?)a["ultimateSelection"]?["revision"]??0)?4:0);
            return changed switch {
                0=>null,1=>"대원 편성이 변경되었습니다. 비용을 다시 확인해 주세요.",2=>"열차 배치가 변경되었습니다. 비용을 다시 확인해 주세요.",
                3=>"대원 편성·열차 배치가 변경되었습니다. 비용을 다시 확인해 주세요.",4=>"궁극기 선택이 변경되었습니다. 비용을 다시 확인해 주세요.",
                5=>"대원 편성·궁극기 선택이 변경되었습니다. 비용을 다시 확인해 주세요.",6=>"열차 배치·궁극기 선택이 변경되었습니다. 비용을 다시 확인해 주세요.",
                _=>"대원 편성·열차 배치·궁극기 선택이 변경되었습니다. 비용을 다시 확인해 주세요."};
        }
        public DepartureQuote(string json,string requestJson)
        {
            data=GameApiJson.ReadObject(json);var request=GameApiJson.ReadObject(requestJson);
            if(!GameApiJson.Boolean(data,"accepted")||GameApiJson.String(data,"operationId")!=GameApiJson.String(request,"operationId")||GameApiJson.String(data,"risk")!=GameApiJson.String(request,"risk"))throw new FormatException();
            RecruitmentJson.Id(data,"operationPreviewId");RecruitmentJson.Utc(data,"expiresAt");ExpiresAt=DateTimeOffset.Parse(GameApiJson.String(data,"expiresAt"),System.Globalization.CultureInfo.InvariantCulture);
            var admission=DepartureContract.Admission(data);
            if(CurrentVersion){var choice=GameApiJson.Object(admission,"ultimateSelection");if(UltimateSelectionSnapshot.RevisionOf(choice,"revision")!=UltimateSelectionSnapshot.RevisionOf(request,"expectedUltimateRevision"))throw new FormatException();}
            foreach(var field in new[]{"formationRevision","loadoutRevision"})
                if(AccountStateSnapshot.Number(data,field)!=AccountStateSnapshot.Number(request,field=="formationRevision"?"expectedFormationRevision":"expectedLoadoutRevision")||AccountStateSnapshot.Number(data,field)!=AccountStateSnapshot.Number(admission,field))throw new FormatException();
            var quote=GameApiJson.Object(data,"quote");if(AccountStateSnapshot.Number(quote,"fuelCost")!=AccountStateSnapshot.Number(admission,"fuelCost"))throw new FormatException();
            AccountStateSnapshot.Number(quote,"availableFuel");GameApiJson.String(quote,"balanceVersion");RewardReference=CombatRewardReference.Optional(data,GameApiJson.String(quote,"balanceVersion"));
            if(AccountStateSnapshot.Number(data,"allowedOperatorSlots")!=5)throw new FormatException();
            // 4ced349 immutable receipts replay without these later additions.
            // Validate their original quote, acknowledge it, then require a fresh quote.
            if(!data.ContainsKey("combatContract")&&!data.ContainsKey("firstClearEligible"))return;
            Combat=new CombatRequirements(GameApiJson.Object(data,"combatContract"));
            if(CurrentVersion)Combat.ValidateAdmission(admission);
            if(Combat.FirstClearEligible!=GameApiJson.Boolean(data,"firstClearEligible")||Combat.RiskLevel!=Array.IndexOf(DepartureContract.Risks,Risk)+1||Combat.OperatorSlotCount!=5)throw new FormatException();
            var content=Combat.RequiredContent;foreach(var field in new[]{"operationId","contentVersion","startAugmentPackageRef","startTacticPackageRef","encounterType","raidId","bossId","bossPhaseRefs"})if(!JToken.DeepEquals(content[field],data[field]))throw new FormatException();
        }
    }
    public static class DepartureContract
    {
        public static string RejectionNotice(JObject response,string preview)=>DepartureRejection.Read(response,preview).Notice;
        public static readonly string[] Operations={"opn_01_last_departure","opn_02_first_signal","opn_03_water_stop","opn_04_scrapstar_show","opn_05_night_boarders","opn_06_broken_line","raid_raider_king_engine"};
        public static readonly string[] Risks={"I","II","III","IV","V"};
        public static JObject Validate(string kind,string target,string payload)
        {
            if(payload==null||payload.Length>3000)throw new FormatException();var r=GameApiJson.ReadObject(payload);
            if(kind=="departure-preview"){
                if((r.Count!=4&&r.Count!=5)||!Operations.Contains(target)||GameApiJson.String(r,"operationId")!=target||!Risks.Contains(GameApiJson.String(r,"risk")))throw new FormatException();
                if(r.Count==5)UltimateSelectionSnapshot.RevisionOf(r,"expectedUltimateRevision"); // Four-field original journals remain recoverable.
                foreach(var field in new[]{"expectedFormationRevision","expectedLoadoutRevision"})if(AccountStateSnapshot.Number(r,field)<1)throw new FormatException();
            }else if(kind=="departure-start"){
                if(r.Count!=1||RecruitmentJson.Id(r,"operationPreviewId")!=target)throw new FormatException();
            }else throw new FormatException();return r;
        }
        internal static JObject Admission(JObject root)
        {
            var a=GameApiJson.Object(root,"admission");var version=GameApiJson.String(a,"version");if(!new[]{"departure-v1","departure-v2","departure-v3","departure-v4"}.Contains(version)||AccountStateSnapshot.Number(a,"fuelCost")<1||AccountStateSnapshot.Number(a,"formationRevision")<1||AccountStateSnapshot.Number(a,"loadoutRevision")<1)throw new FormatException();
            var f=GameApiJson.Object(a,"formation");var operators=RecruitmentJson.Array(f,"operators");if(AccountStateSnapshot.Number(f,"schemaVersion")!=1||operators.Count!=5)throw new FormatException();
            var ids=new System.Collections.Generic.HashSet<string>();var slots=new System.Collections.Generic.HashSet<long>();
            foreach(var t in operators){var row=(JObject)t;var slot=AccountStateSnapshot.Number(row,"slot");if(slot>=5||!slots.Add(slot)||!ids.Add(RecruitmentJson.Id(row,"operatorId")))throw new FormatException();}
            if(version!="departure-v1") {var choice=GameApiJson.Object(a,"ultimateSelection");if(choice.Count!=3||GameApiJson.String(choice,"rulesVersion")!=UltimateSelectionSnapshot.Rules)throw new FormatException();UltimateSelectionSnapshot.RevisionOf(choice,"revision");var id=RecruitmentJson.NullableString(choice,"operatorId");if(id!=null&&!ids.Contains(id))throw new FormatException();}
            if(a.ContainsKey("combatAuthority"))CombatRequirements.ValidateAuthority(GameApiJson.Object(a,"combatAuthority"));
            if(version=="departure-v4"){var policy=GameApiJson.Object(a,"combatPolicyRequirements");GameApiJson.String(policy,"version");if(GameApiJson.String(policy,"status")!="REQUIRED_NOT_EXECUTABLE")throw new FormatException();}
            TrainLoadoutWrite.ValidateSelection(GameApiJson.Object(a,"loadout"));
            var owned=GameApiJson.Object(a,"ownedState");RecruitmentJson.Array(owned,"operators");RecruitmentJson.Array(owned,"trainingCoreSlots");GameApiJson.Object(owned,"train");RecruitmentJson.Array(owned,"carriages");
            return a;
        }
        public static JObject Started(string json,string preview)
        {
            var r=GameApiJson.ReadObject(json);if(!GameApiJson.Boolean(r,"accepted")||GameApiJson.String(r,"startStatus")!="STARTED"||GameApiJson.String(r,"operationPreviewId")!=preview||!Operations.Contains(GameApiJson.String(r,"operationId")))throw new FormatException();
            RecruitmentJson.Id(r,"runId");GameApiJson.String(r,"balanceVersion");RecruitmentJson.Utc(r,"serverStartedAt");Admission(r);return r;
        }
    }
    public sealed partial class AccountScreenRuntime
    {
        public DepartureQuote Departure {get;private set;}
        public string DepartureConditionNotice {get;private set;}
        JObject departureStarted;
        LastRail.Battle.OrdinaryFiveStartInput ordinaryDepartureInput;
        PendingAccountAction ordinaryPendingSource;
        LastRail.Battle.OrdinaryFiveStartInput ordinaryPendingInput;
        // Reuse the selected run's retained response; this is input acquisition, not admission or activation.
        public LastRail.Battle.OrdinaryFiveStartInput ReadOrdinaryCombatInput(string runId)
        {
            SynchronizeSession();if(!Current(owner,ownerGeneration))throw new InvalidOperationException("Account session unavailable.");
            var selection=RunSelection;if(selection.Kind!=LobbyRunSelectionKind.Recovery||selection.RunId!=runId)throw new InvalidOperationException("Run selection requires recovery review.");
            LastRail.Battle.OrdinaryFiveStartInput selected=null;
            void Accept(LastRail.Battle.OrdinaryFiveStartInput input){
                if(selected!=null&&(selected.OriginHash!=input.OriginHash||selected.KitHash!=input.KitHash||selected.Basic?.BasicHash!=input.Basic?.BasicHash||selected.Relocation?.RelocationHash!=input.Relocation?.RelocationHash||selected.FirstBattle?.SourceHash!=input.FirstBattle?.SourceHash||selected.Train?.SourceHash!=input.Train?.SourceHash))throw new InvalidOperationException("Retained original START pins conflict.");
                selected=selected??input;
            }
            if(Lifecycle?.RunId==runId)Accept(Lifecycle.ReadOrdinaryInput(owner.AccountId));
            if(DepartureStartResult?.RunId==runId){
                if(DepartureStartResult.RunStatus!="ACTIVE")throw new InvalidOperationException("Run is not active.");
                Accept(DepartureStartResult.ReadOrdinaryInput(owner.AccountId));
            }
            if((string)departureStarted?["runId"]==runId)Accept(ordinaryDepartureInput??(ordinaryDepartureInput=LastRail.Battle.OrdinaryFiveStartInput.Read(departureStarted,owner.AccountId,runId)));
            if(Pending?.Account==owner.AccountId&&Pending.Kind=="departure-start"&&Pending.StartResultJson!=null){
                if(!ReferenceEquals(ordinaryPendingSource,Pending)){
                    var receipt=new DepartureStartResult(200,Pending.StartResultJson,Pending.Target);
                    if(receipt.RunId!=runId||receipt.RunStatus!="ACTIVE")throw new InvalidOperationException("Retained run is not active.");
                    var input=receipt.ReadOrdinaryInput(owner.AccountId);ordinaryPendingInput=input;ordinaryPendingSource=Pending;
                }
                Accept(ordinaryPendingInput);
            }else{ordinaryPendingSource=null;ordinaryPendingInput=null;}
            return selected??throw new InvalidOperationException("Original START unavailable.");
        }
        public JObject DepartureStarted=>departureStarted==null?null:(JObject)departureStarted.DeepClone();
        public async Task<PendingRunOperation> InitializeOrdinaryCombatAsync(string runId)
        {
            SynchronizeSession();if(Busy||runRecovery==null||runRecovery.Busy)return null;
            var original=ReadOrdinaryCombatInput(runId);var session=owner;var stamp=ownerGeneration;
            bool StillRecovering(){if(!Current(session,stamp))return false;var selection=RunSelection;return selection.Kind==LobbyRunSelectionKind.Recovery&&selection.RunId==runId;}
            await runRecovery.RestoreAsync();
            if(!StillRecovering())return null;
            var saved=runRecovery.Operations.SingleOrDefault(o=>o.Kind==PendingRunKind.InitializeOrdinary&&o.TargetId==runId);
            var result=await runRecovery.InitializeOrdinaryAsync(original,saved?.IdempotencyKey??Guid.NewGuid().ToString("N"));
            return StillRecovering()?result:null;
        }
        // Bound by the combat owner only when a durable battle consumer is available.
        public Func<bool> BattleEntryReady {get;set;}
        public Func<bool> ExistingRunRecoveryClear {get;set;}
        public bool CanRetryDeparture=>!Busy&&Pending?.Kind=="departure-preview";
        public bool CanPreviewDeparture=>!Busy&&Pending==null&&AccountStatus==AccountStateReadStatus.Ready&&Account?.ActiveOperators.Count==5&&Account.FormationRevision>0&&TrainLoadoutReady&&TrainLoadout?.Loadout!=null&&TrainLoadout.Revision>0&&!TrainLoadout.OpenRun&&TrainLoadout.ProgressError==null&&TrainLoadout.LoadoutError==null&&UltimateReady&&Ultimate!=null&&!Ultimate.ChangeLocked&&Ultimate.FormationRevision==Account.FormationRevision&&(Ultimate.Status=="SELECTED"||Ultimate.Status=="UNSELECTED");
        public string DepartureSelectionNotice=>Departure==null?null:AccountStatus!=AccountStateReadStatus.Ready||Account==null||!TrainLoadoutReady||TrainLoadout==null||!UltimateReady||Ultimate==null?"현재 편성 정보를 확인한 뒤 비용을 다시 확인해 주세요.":Departure.SelectionChangeNotice(Account.FormationRevision,TrainLoadout.Revision,Ultimate.Revision);
        public bool CanConfirmDeparture=>CanPreviewDeparture&&DepartureSelectionNotice==null&&Departure?.Combat.CanStart==true&&Departure.ExpiresAt>DateTimeOffset.UtcNow&&Departure.AvailableFuel>=Departure.FuelCost&&BattleEntryReady?.Invoke()==true&&ExistingRunRecoveryClear?.Invoke()==true;
        void ResetDeparture(bool retainConfirmedReceipt=false)
        {
            ResetOrdinaryPreparation();
            var retainedLifecycle=retainConfirmedReceipt?Lifecycle:null;bool retainedVerified=retainConfirmedReceipt&&lifecycleVerified;
            var retainedPreview=retainedLifecycle!=null?RunRewardPreview:null;
            var retainedReceipt=retainedLifecycle!=null?verifiedRunReceipt:null;
            var retainedNotice=retainedLifecycle!=null?RunResultNotice:"";
            ResetRunSelection();ResetLifecycle();Lifecycle=retainedLifecycle;lifecycleVerified=retainedVerified;
            RunRewardPreview=retainedPreview;verifiedRunReceipt=retainedReceipt;RunResultNotice=retainedNotice;
            if(Lifecycle!=null)LifecycleNotice=Lifecycle.Notice;
            Departure=null;departureStarted=null;ordinaryDepartureInput=null;ordinaryPendingSource=null;ordinaryPendingInput=null;DepartureConditionNotice=null;ResetDepartureRecovery();ResetDepartureLookup(retainConfirmedReceipt);
        }
        public Task PreviewDepartureAsync(string operationId,string risk)
        {
            SynchronizeSession();if(!CanPreviewDeparture||!DepartureContract.Operations.Contains(operationId)||!DepartureContract.Risks.Contains(risk))return Task.CompletedTask;
            return SubmitDepartureAsync("departure-preview",operationId,new JObject{["operationId"]=operationId,["risk"]=risk,["expectedFormationRevision"]=Account.FormationRevision,["expectedLoadoutRevision"]=TrainLoadout.Revision,["expectedUltimateRevision"]=Ultimate.Revision}.ToString());
        }
        public Task ConfirmDepartureAsync()
        {
            SynchronizeSession();if(!CanConfirmDeparture)return Task.CompletedTask;
            return SubmitDepartureAsync("departure-start",Departure.Id,new JObject{["operationPreviewId"]=Departure.Id}.ToString());
        }
        async Task SubmitDepartureAsync(string kind,string target,string payload)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(writing||!Current(original,stamp))return;
            if(kind=="departure-start"&&(Departure?.Combat.CanStart!=true||BattleEntryReady?.Invoke()!=true||ExistingRunRecoveryClear?.Invoke()!=true)){
                ActionNotice="전투 준비가 완료되면 원래 출발 요청을 확인할 수 있습니다.";Changed?.Invoke();return;
            }
            writing=true;ActionErrorCode=null;DepartureConditionNotice=null;ActionNotice="출발 정보를 확인하고 있습니다.";Changed?.Invoke();bool refresh=false;DepartureRefresh rejectedRefresh=DepartureRefresh.None;
            try{
                if(journal==null)throw new InvalidOperationException();var action=journal.Read(original.AccountId);
                if(action!=null&&(action.Kind!=kind||action.Target!=target||action.Payload!=payload)){Pending=action;ActionNotice="이전 요청부터 확인해 주세요.";return;}
                if(action==null){action=new PendingAccountAction(original.AccountId,kind,target,0,Guid.NewGuid().ToString("N"),payload);if(!journal.Save(action))throw new InvalidOperationException();}
                Pending=action;DepartureContract.Validate(kind,target,payload);
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST",kind=="departure-preview"?"/v1/operations/preview":"/v1/runs/start",action.Payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),original.AccessToken));
                if(!Current(original,stamp))return;
                if(response.IsNetworkError||response.StatusCode>=500){ActionNotice="결과 확인이 필요합니다. 같은 요청으로 재확인해 주세요.";return;}
                var r=GameApiJson.ReadObject(response.BodyJson);
                if(response.StatusCode==200&&GameApiJson.Boolean(r,"accepted")){
                    if(kind=="departure-preview"){
                        var quote=new DepartureQuote(response.BodyJson,payload);
                        if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;Departure=quote.Combat==null||!quote.CurrentVersion?null:quote;ActionNotice=Departure==null?"이전 견적을 확인했습니다. 새 출발 비용을 확인해 주세요.":!Departure.Combat.CanStart?"현재 버전에서는 출격할 수 없습니다.":"출발 비용을 확인해 주세요.";
                    }else{
                        var started=DepartureContract.Started(response.BodyJson,target);
                        if(Departure!=null&&Departure.Id==target&&(!JToken.DeepEquals(Departure.Admission,started["admission"])||(string)started["operationId"]!=Departure.OperationId))throw new FormatException();
                        departureStarted=started;ordinaryDepartureInput=null;Departure=null;refresh=true;
                        // Retain the exact request until the battle owner durably consumes this run.
                        // On restart the server replays its entire pinned STARTED response.
                        ActionNotice="출발이 확정되었습니다. 전투 연결을 확인하고 있습니다.";
                    }
                }else if((response.StatusCode==400||response.StatusCode==403||response.StatusCode==404||response.StatusCode==409)&&!GameApiJson.Boolean(r,"accepted")&&(!r.ContainsKey("startStatus")||(string)r["startStatus"]=="REJECTED")){
                    var error=GameApiJson.String(r,"errorCode");var rejection=DepartureRejection.Read(r,target);
                    if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;Departure=null;ActionErrorCode=error;rejectedRefresh=rejection.Refresh;InvalidateRejectedDeparture(rejectedRefresh);
                    ActionNotice=DepartureConditionNotice=rejection.Notice;
                }else ActionNotice="출발 결과를 확인하지 못했습니다. 같은 요청으로 재확인해 주세요.";
            }catch{if(Current(original,stamp))ActionNotice="출발 결과 또는 저장 상태를 확인하지 못했습니다. 원래 요청을 보존합니다.";}
            finally{writing=false;if(Current(original,stamp))Changed?.Invoke();}
            if(refresh&&Current(original,stamp))await Task.WhenAll(RefreshSharedAsync(),RefreshTrainLoadoutAsync(),RefreshUltimateAsync());
            else if(Current(original,stamp))await RefreshRejectedDepartureAsync(rejectedRefresh);
        }
        void InvalidateRejectedDeparture(DepartureRefresh refresh)
        {
            // Invalidate earlier in-flight reads before notifying; retain snapshots as stale display data.
            if((refresh&DepartureRefresh.Account)!=0){accountRequest++;AccountStatus=AccountStateReadStatus.NotLoaded;}
            if((refresh&DepartureRefresh.Train)!=0){trainLoadoutRequest++;TrainLoadoutReady=false;}
            if((refresh&DepartureRefresh.Ultimate)!=0){ultimateRequest++;UltimateReady=false;}
        }
        Task RefreshRejectedDepartureAsync(DepartureRefresh refresh)=>Task.WhenAll(
            (refresh&DepartureRefresh.Account)!=0?RefreshAccountAsync():Task.CompletedTask,
            (refresh&DepartureRefresh.Train)!=0?RefreshTrainLoadoutAsync():Task.CompletedTask,
            (refresh&DepartureRefresh.Ultimate)!=0?RefreshUltimateAsync():Task.CompletedTask);
    }
}

