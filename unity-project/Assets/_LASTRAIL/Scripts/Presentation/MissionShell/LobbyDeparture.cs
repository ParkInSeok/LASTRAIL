using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyCommandDeck
    {
        bool departureDialog;
        Coroutine departureExpiry;
        DateTimeOffset? scheduledDepartureExpiry;
        void CancelDepartureExpiry()
        {
            if(departureExpiry!=null)StopCoroutine(departureExpiry);
            departureExpiry=null;scheduledDepartureExpiry=null;
        }
        void RefreshDepartureClock()
        {
            CancelDepartureExpiry();
            if(isActiveAndEnabled)UpdateDepartureDialog();
        }
        void ScheduleDepartureExpiry()
        {
            var expiry=localDeparture!=null?localDeparture.QuoteExpiresAt:accountState?.Departure?.ExpiresAt;
            if(!Application.isPlaying||!isActiveAndEnabled||!expiry.HasValue||expiry<=DateTimeOffset.UtcNow){CancelDepartureExpiry();return;}
            if(departureExpiry!=null&&scheduledDepartureExpiry==expiry)return;
            CancelDepartureExpiry();scheduledDepartureExpiry=expiry;
            departureExpiry=StartCoroutine(WaitForDepartureExpiry(expiry.Value));
        }
        IEnumerator WaitForDepartureExpiry(DateTimeOffset expiry)
        {
            do
            {
                yield return new WaitForSecondsRealtime((float)Math.Max(.01,(expiry-DateTimeOffset.UtcNow).TotalSeconds));
            }while(expiry>DateTimeOffset.UtcNow);
            departureExpiry=null;scheduledDepartureExpiry=null;
            UpdateDepartureDialog();
        }
        bool operationsDialog;
        Func<string,Func<bool>,Task> prepareOrdinaryCombat;
        bool ordinaryPreparationBusy;
        enum PreparationDisplay { Idle, Preparing, Ready, Failed, Uncertain, Unavailable }
        PreparationDisplay ordinaryDisplay;
        string ordinaryDisplayRun,ordinaryDisplayMessage;
        long ordinaryPreparationBinding;
        public void BindOrdinaryPreparation(Func<string,Task> prepare)
        { BindOrdinaryPreparationWithOwner(prepare==null?null:(Func<string,Func<bool>,Task>)((run,current)=>prepare(run))); }
        public void BindOrdinaryPreparationWithOwner(Func<string,Func<bool>,Task> prepare)
        { ordinaryPreparationBinding++;prepareOrdinaryCombat=prepare;ordinaryDisplay=PreparationDisplay.Idle;ordinaryDisplayMessage=null;UpdateDepartureRecoveryDialog(); }
        async void PrepareOrdinaryFromRecovery()
        {
            if(!isActiveAndEnabled||ordinaryPreparationBusy||ordinaryDisplay==PreparationDisplay.Unavailable||prepareOrdinaryCombat==null||!departureRecoveryDialog||dialog==null)return;
            var target=departureRecoveryId;var surface=dialog;var state=accountState;var prepare=prepareOrdinaryCombat;long binding=ordinaryPreparationBinding;
            ordinaryPreparationBusy=true;ordinaryDisplay=PreparationDisplay.Preparing;ordinaryDisplayMessage=null;UpdateDepartureRecoveryDialog();
            bool CurrentEntry()=>this!=null&&isActiveAndEnabled&&surface==dialog&&departureRecoveryDialog&&state==accountState&&binding==ordinaryPreparationBinding;
            try{
                await prepare(target,CurrentEntry);
                if(this!=null&&isActiveAndEnabled&&surface==dialog&&departureRecoveryDialog&&state==accountState&&binding==ordinaryPreparationBinding)
                    {ordinaryDisplay=PreparationDisplay.Ready;ordinaryDisplayMessage="전투 준비를 마쳤습니다.";}
            }catch(Exception error){
                UnityEngine.Debug.LogWarning("ordinary_preparation_failed: "+error.GetType().Name);
                if(CurrentEntry()){
                    if(error is OrdinaryNodeStartUnavailableException unavailable&&unavailable.RunResultAvailable)OpenRunLifecycle(target,true);
                    else if(error is OrdinaryNodeStartUnavailableException problem){
                        ordinaryDisplay=problem.Failure==OrdinaryPreparationFailure.OutcomeUnknown?PreparationDisplay.Uncertain:PreparationDisplay.Unavailable;
                        ordinaryDisplayMessage=ordinaryDisplay==PreparationDisplay.Unavailable?"현재 전투 정보를 불러올 수 없습니다.\n"+problem.Message:problem.Message;
                    }else{ordinaryDisplay=PreparationDisplay.Failed;ordinaryDisplayMessage="전투 준비를 완료하지 못했습니다. 원래 요청과 전투 시작 상태는 보존됩니다.";}
                }
            }finally{
                ordinaryPreparationBusy=false;
                if(!CurrentEntry()&&ordinaryDisplay==PreparationDisplay.Preparing)ordinaryDisplay=PreparationDisplay.Idle;
                if(this!=null&&isActiveAndEnabled)UpdateDepartureRecoveryDialog();
            }
        }
        readonly System.Collections.Generic.Dictionary<string,Button> operationListButtons=new System.Collections.Generic.Dictionary<string,Button>();
        bool CanSelectOperation(string id)=>(accountState==null||operationProgressReady)&&missions?.Any(x=>x.MissionId==id&&x.IsServerListedUnlockedForDisplay)==true;
        string OperationAvailabilityNotice=>accountState==null?null:!operationProgressReady?"진행 정보 확인 필요":CanSelectOperation(departureOperation)?null:"잠김 · 진행 조건 확인 필요";
        void UpdateOperationList()
        {
            if(!operationsDialog||dialog==null)return;
            foreach(var row in missions??Array.Empty<MissionShellMissionRowState>())
                if(operationListButtons.TryGetValue(row.MissionId,out var button))
                {
                    button.interactable=CanSelectOperation(row.MissionId);
                    button.GetComponentInChildren<TMP_Text>().text=row.DisplayLabel+(accountState!=null&&accountState.StoryArchive.DisplayState==null?" · 확인 필요":row.IsServerListedUnlockedForDisplay?"":" · 잠김");
                }
            dialog.Find("DialogBody").GetComponent<TMP_Text>().text=operationProgressReady?"":"진행 정보 확인 필요";
        }
        DeparturePreparationView preparationView;
        AccountStateSnapshot departureSupportOwner;
        string[] departureSupportIds=Array.Empty<string>();
        int departureReadGeneration;
        string departureSupportNotice="선택 확인 필요";
        string departureOperation,departureLabel;
        int departureRisk;
        bool departureRecoveryDialog;
        string departureRecoveryId;
        bool startLookupDialog;
        public void OpenOperations()
        {
            accountState?.SynchronizeSession();
            var selection=accountState?.RunSelection;
            if(selection?.Kind==LobbyRunSelectionKind.Conflict){OpenRunRecordSelection(selection);return;}
            if(selection?.RunId!=null){OpenRunLifecycle(selection.RunId,true);return;}
            if(accountState?.HasDepartureLookupRequest==true){OpenStartLookup();return;}
            if(selection?.Kind==LobbyRunSelectionKind.AwaitingConfirmation){OpenRunRecordSelection(selection);return;}
            OpenOperationList();
        }
        void OpenRunRecordSelection(LobbyRunSelection selection,int page=0)
        {
            var owner=accountState;
            ShowInfo("작전 기록 확인",selection.Notice);
            Place(dialog.Find("DialogBody").GetComponent<UnityEngine.RectTransform>(),48,145,650,85);
            bool Current()=>this!=null&&isActiveAndEnabled&&ReferenceEquals(accountState,owner)&&ReferenceEquals(owner?.RunSelection,selection);
            if(owner?.HasDepartureLookupRequest==true)Button("CheckOriginalDeparture",dialog,60,235,630,55,"원래 출격 요청 확인",()=>{if(Current())OpenStartLookup();});
            const int perPage=4;int offset=page*perPage;
            for(int i=offset;i<Math.Min(offset+perPage,selection.KnownRuns.Count);i++){
                string id=selection.KnownRuns[i];int number=i+1;
                Button("InspectRunRecord"+number,dialog,60,300+(i-offset)*65,630,55,"작전 기록 "+number+" 확인",()=>{if(Current())OpenRunLifecycle(id,true);});
            }
            if(page>0)Button("PreviousRunRecords",dialog,60,570,300,55,"이전 기록",()=>{if(Current())OpenRunRecordSelection(selection,page-1);});
            if(offset+perPage<selection.KnownRuns.Count)Button("NextRunRecords",dialog,390,570,300,55,"다음 기록",()=>{if(Current())OpenRunRecordSelection(selection,page+1);});
        }
        void OpenOperationList()
        {
            ShowInfo("작전 선택","");
            operationsDialog=true;
            var rows=missions?.Where(x=>DepartureContract.Operations.Contains(x.MissionId)).Take(7).ToArray();
            if(rows==null||rows.Length==0){dialog.Find("DialogBody").GetComponent<TMP_Text>().text="작전 정보를 불러오지 못했습니다.";return;}
            for(int i=0;i<rows.Length;i++){
                var row=rows[i];operationListButtons[row.MissionId]=Button("SelectDeparture"+i,dialog,50,140+i*65,650,58,row.DisplayLabel,()=>{if(CanSelectOperation(row.MissionId))OpenDepartureQuote(row.MissionId,row.DisplayLabel);});
            }
            UpdateOperationList();
        }
        public void OpenStartLookup()
        {
            ShowInfo("출발 결과 확인","");startLookupDialog=true;
            Button("ReadOriginalRun",dialog,60,470,630,65,"출발 기록 보기",()=>{var id=accountState?.KnownDepartureRunId;if(id!=null)OpenDepartureRecovery(id);});
            Button("LookupDepartureStart",dialog,60,555,630,70,"처리 결과 확인",()=>ReadStartLookup());
            UpdateStartLookupDialog();ReadStartLookup();
        }
        async void ReadStartLookup(){if(accountState!=null)await accountState.LookupDepartureStartAsync();}
        void UpdateStartLookupDialog()
        {
            if(!startLookupDialog||dialog==null)return;
            dialog.Find("DialogBody").GetComponent<TMP_Text>().text=accountState?.DepartureLookupNotice??"출발 요청을 확인할 수 없습니다.";
            dialog.Find("LookupDepartureStart").GetComponent<Button>().interactable=accountState?.CanRetryDepartureLookup==true;
            dialog.Find("LookupDepartureStart").GetComponentInChildren<TMP_Text>().text=accountState?.DepartureReceiptNeedsSave==true?"확정 응답 저장 다시 시도":"처리 결과 확인";
            dialog.Find("ReadOriginalRun").GetComponent<Button>().interactable=accountState?.KnownDepartureRunId!=null&&!accountState.DepartureLookupLoading;
        }
        public void OpenDepartureRecovery(string runId)
        {
            if(ordinaryDisplayRun!=runId){ordinaryDisplayRun=runId;ordinaryDisplay=PreparationDisplay.Idle;ordinaryDisplayMessage=null;}
            ShowInfo("출발 기록 확인","");departureRecoveryDialog=true;departureRecoveryId=runId;
            Button("PrepareOrdinaryCombat",dialog,60,385,630,65,"전투 다시 시작",PrepareOrdinaryFromRecovery);
            Button("RunLifecycle",dialog,60,470,630,65,"전투·보상 기록",()=>OpenRunLifecycle(runId));
            Button("RefreshDepartureRecovery",dialog,60,555,630,70,"다시 확인",()=>ReadDepartureRecovery());
            Button("OrdinaryPreparationSupport",dialog,60,640,630,65,"문의하기",()=>{CloseDialog();support?.Invoke();});
            dialog.sizeDelta=new UnityEngine.Vector2(750,840);Place(dialog.Find("CloseLobbyDialog").GetComponent<UnityEngine.RectTransform>(),60,745,630,70);
            dialog.Find("CloseLobbyDialog").GetComponentInChildren<TMP_Text>().text="로비로";
            EvaluateLayout();UpdateDepartureRecoveryDialog();EnsureDepartureRecovery();
        }
        async void EnsureDepartureRecovery(){if(accountState!=null)await accountState.EnsureDepartureRecoveryAsync(departureRecoveryId);}
        async void ReadDepartureRecovery(){if(accountState!=null)await accountState.RefreshDepartureRecoveryAsync(departureRecoveryId);}
        void UpdateDepartureRecoveryDialog()
        {
            if(!departureRecoveryDialog||dialog==null)return;
            dialog.Find("DialogBody").GetComponent<TMP_Text>().text=ordinaryPreparationBusy?(accountState?.HasUnconfirmedOrdinaryResult(departureRecoveryId)==true?"전투 결과 확인 중":"전투 준비 중"):ordinaryDisplayMessage??(accountState?.DepartureRecoveryLoading==true||accountState?.DepartureRecoveryError!=null?accountState.DepartureRecoveryNotice:"이 전투를 처음부터 다시 시작합니다.\n체력은 전투 시작 시점으로 돌아갑니다.");
            dialog.Find("RefreshDepartureRecovery").GetComponent<Button>().interactable=accountState!=null&&!accountState.DepartureRecoveryLoading&&!ordinaryPreparationBusy;
            var prepare=dialog.Find("PrepareOrdinaryCombat").GetComponent<Button>();
            prepare.interactable=prepareOrdinaryCombat!=null&&!ordinaryPreparationBusy&&ordinaryDisplay!=PreparationDisplay.Unavailable&&accountState?.DepartureRecoveryLoading!=true;
            prepare.GetComponentInChildren<TMP_Text>().text=ordinaryDisplay==PreparationDisplay.Uncertain?"원요청 확인":ordinaryDisplay==PreparationDisplay.Failed||ordinaryDisplay==PreparationDisplay.Ready?"다시 준비":"전투 다시 시작";
            var help=dialog.Find("OrdinaryPreparationSupport").GetComponent<Button>();help.gameObject.SetActive(ordinaryDisplay==PreparationDisplay.Unavailable);help.interactable=support!=null;
        }
        void OpenDepartureQuote(string id,string label,int selectedRisk=0)=>OpenResourceDialog(new[]{"EntryUi/secondary","NoncombatPreparation/"},()=>OpenDepartureQuotePrepared(id,label,selectedRisk));
        void OpenDepartureQuotePrepared(string id,string label,int selectedRisk)
        {
            if(localDeparture?.LockedOperation!=null){id=localDeparture.LockedOperation;label=missions?.FirstOrDefault(x=>x.MissionId==id)?.DisplayLabel??id;selectedRisk=Array.IndexOf(DepartureContract.Risks,localDeparture.LockedRisk);if(selectedRisk<0)return;}
            departureOperation=id;departureLabel=label;departureRisk=selectedRisk;
            ShowInfo("출발 준비","");
            dialog.parent.GetComponent<Image>().color=new UnityEngine.Color(.013f,.020f,.024f,1);
            foreach(UnityEngine.Transform child in dialog)child.gameObject.SetActive(false);
            dialog.sizeDelta=new UnityEngine.Vector2(941,1672);
            localDeparture?.InvalidateQuote();
            DeparturePreparationView created=null;var boundOwner=accountState;
            Action CurrentView(Action action)=>()=>{if(this!=null&&isActiveAndEnabled&&departureDialog&&created!=null&&created==preparationView&&boundOwner==accountState)action();};
            preparationView=created=DeparturePreparationView.Create(dialog,font,frameSkin,CurrentView(ClosePreparation),CurrentView(OpenOperations),
                CurrentView(()=>OpenPreparationPage(LobbyPage.Formation)),CurrentView(()=>OpenPreparationPage(LobbyPage.Train)),CurrentView(OpenPreparationSupport),
                CurrentView(ChangeDepartureRisk),CurrentView(PreviewDeparture),CurrentView(ConfirmDeparture),CurrentView(OpenDepartureGuide),CurrentView(RefreshDeparture));
            preparationView.BindDepartureIntent(CaptureDepartureIntent);
            preparationView.BindBlockActions(ResolveDepartureBlock);
            preparationView.BindBlockAvailability(CanResolveDepartureBlock);
            departureDialog=true;EvaluateLayout();RenderPreparationSelections();UpdateDepartureDialog();ReadDepartureForDisplay(true);
        }
        void ClosePreparation(){if(localDeparture?.CanCancelUnsentStart==true&&!localDeparture.CancelUnsentStart())return;CloseDialog();}
        Func<bool> CaptureDepartureIntent()
        {
            accountState?.SynchronizeSession();
            var view=preparationView;var owner=accountState;var flow=localDeparture;
            var account=owner?.Account;var loadout=owner?.TrainLoadout;var ultimate=owner?.Ultimate;var quote=owner?.Departure;
            var pending=owner?.Pending;var operation=departureOperation;var risk=departureRisk;
            return ()=>{
                accountState?.SynchronizeSession();
                return this!=null&&isActiveAndEnabled&&departureDialog&&view!=null&&view==preparationView&&owner==accountState&&flow==localDeparture&&
                    ReferenceEquals(account,owner?.Account)&&ReferenceEquals(loadout,owner?.TrainLoadout)&&ReferenceEquals(ultimate,owner?.Ultimate)&&ReferenceEquals(quote,owner?.Departure)&&
                    ReferenceEquals(pending,owner?.Pending)&&operation==departureOperation&&risk==departureRisk;
            };
        }
        void ChangeDepartureRisk(){departureRisk=(departureRisk+1)%DepartureContract.Risks.Length;localDeparture?.InvalidateQuote();UpdateDepartureDialog();}
        async void PreviewDeparture(){if(accountState==null||!CanSelectOperation(departureOperation)||!accountState.CanPreviewDeparture)return;if(localDeparture!=null)await localDeparture.PreviewAsync(departureOperation,DepartureContract.Risks[departureRisk],accountState.Account.FormationRevision,accountState.TrainLoadout.Revision,accountState.Ultimate.Revision);else await accountState.PreviewDepartureAsync(departureOperation,DepartureContract.Risks[departureRisk]);}
        async void ConfirmDeparture(){accountState?.SynchronizeSession();if(localDeparture!=null){if(localDeparture.CanRetryEntry){localDeparture.RetryEntry();return;}if(!localDeparture.CanRetryStart&&(CurrentDepartureBlocks().HasBlocks||!CanSelectOperation(departureOperation)||CurrentLocalDepartureCost()==null))return;await localDeparture.StartAsync();return;}if(accountState==null)return;if(accountState.Pending?.Kind.StartsWith("departure-")==true)await accountState.RetryPendingAsync();else if(!CurrentDepartureBlocks().HasBlocks&&CanSelectOperation(departureOperation))await accountState.ConfirmDepartureAsync();}
        string CurrentLocalDepartureCost(){var state=accountState;return localDeparture!=null&&state?.AccountStatus==AccountStateReadStatus.Ready&&state.TrainLoadoutReady&&state.UltimateReady&&state.Account!=null&&state.TrainLoadout!=null&&state.Ultimate!=null?localDeparture.ReadGuideFuelCost(departureOperation,DepartureContract.Risks[departureRisk],state.Account.FormationRevision,state.TrainLoadout.Revision,state.Ultimate.Revision):null;}
        Action PreparationReturn(){var id=departureOperation;var label=departureLabel;var risk=departureRisk;return ()=>{if(this!=null&&isActiveAndEnabled)OpenDepartureQuote(id,label,risk);};}
        void OpenPreparationPage(LobbyPage page){var back=PreparationReturn();OpenPage(page,back);}
        void OpenPreparationSupport(){if(localCommandClient==null)return;var back=PreparationReturn();CloseDialog();LastRail.Battle.Ce03SupportSelectionPanel.Open(transform,localCommandClient,back);}
        void RefreshDeparture()=>ReadDepartureForDisplay(false);
        async void ReadDepartureForDisplay(bool reuseShared)
        {
            var owner=accountState;var view=preparationView;if(owner==null)return;int request=++departureReadGeneration;
            await System.Threading.Tasks.Task.WhenAll(reuseShared?owner.EnsureSharedAsync():owner.RefreshSharedAsync(),reuseShared?owner.EnsureTrainLoadoutAsync():owner.RefreshTrainLoadoutAsync(),reuseShared?owner.EnsureUltimateAsync():owner.RefreshUltimateAsync());
            if(this==null||!isActiveAndEnabled||preparationView!=view||view==null||owner!=accountState||localCommandClient==null)return;
            var snapshot=owner.Account;var client=localCommandClient;
            bool Current()=>this!=null&&isActiveAndEnabled&&request==departureReadGeneration&&preparationView==view&&view!=null&&owner==accountState&&client==localCommandClient&&ReferenceEquals(owner.Account,snapshot)&&owner.AccountStatus==AccountStateReadStatus.Ready;
            departureSupportNotice="불러오는 중";RenderPreparationSelections();
            try{var data=await client.ReadSupportAsync();if(!Current())return;departureSupportIds=((JArray)data["selection"]["slots"]).Select(x=>(string)x["skillId"]).ToArray();departureSupportOwner=snapshot;departureSupportNotice="미선택";}
            catch(Exception){if(!Current())return;departureSupportIds=Array.Empty<string>();departureSupportOwner=null;departureSupportNotice="불러오지 못함";}
            if(Current()){RenderPreparationSelections();UpdateDepartureDialog();}
        }
        void UpdateDepartureDialog()
        {
            if(!departureDialog||dialog==null||preparationView==null){CancelDepartureExpiry();return;}var state=accountState;
            ScheduleDepartureExpiry();
            var blocks=CurrentDepartureBlocks();preparationView.RenderBlocks(blocks);
            if(localDeparture!=null){
                var cost=CurrentLocalDepartureCost();
                preparationView.Render(departureLabel,DepartureContract.Risks[departureRisk],blocks.PrimaryNotice??localDeparture.Notice,PreparationBalances(),localDeparture.HasConfirmedStart?"출격 확정됨":cost==null?"비용 확인 필요":"연료 "+cost,
                    localDeparture.CanChangeSelection,localDeparture.CanChangeSelection&&CanSelectOperation(departureOperation)&&state?.CanPreviewDeparture==true&&!localDeparture.Busy,!blocks.HasBlocks&&CanSelectOperation(departureOperation)&&cost!=null||localDeparture.CanRetryStart||localDeparture.CanRetryEntry,localDeparture.CanChangeSelection,localCommandClient!=null&&localDeparture.CanChangeSelection,localDeparture.NeedsReceiptSave?"확정 응답 저장 다시 시도":localDeparture.CanRetryEntry?"전투 자료 다시 준비":localDeparture.EntryStatus==LastRail.Battle.Ce03EntryStatus.UpdateRequired?"업데이트 필요":localDeparture.CanRetryStart?"원래 출발 요청 확인":"출격");return;
            }
            var quote=state?.Departure;
            bool matches=quote!=null&&quote.OperationId==departureOperation&&quote.Risk==DepartureContract.Risks[departureRisk];
            bool recovery=state?.Pending?.Kind.StartsWith("departure-")==true;
            var text=state?.ActionNotice;if(string.IsNullOrEmpty(text))text=state?.AccountStatus==AccountStateReadStatus.Ready?"편성과 열차 배치를 저장한 뒤 비용을 확인해 주세요.":state?.AccountNotice??"계정 정보를 확인해 주세요.";
            if(!recovery)text=OperationAvailabilityNotice??text;
            text=blocks.PrimaryNotice??text;
            var costText=matches&&state.DepartureSelectionNotice==null&&quote.ExpiresAt>DateTimeOffset.UtcNow?"연료 "+quote.FuelCost+" / 보유 "+quote.AvailableFuel:"비용 확인 필요";
            preparationView.Render(departureLabel,DepartureContract.Risks[departureRisk],text,PreparationBalances(),costText,state!=null&&!state.Busy&&state.Pending==null,CanSelectOperation(departureOperation)&&state?.CanPreviewDeparture==true,
                state!=null&&!state.Busy&&(recovery?state.CanRetryDeparture:!blocks.HasBlocks&&CanSelectOperation(departureOperation)&&matches&&state.CanConfirmDeparture),state!=null&&!state.Busy&&state.Pending==null,localCommandClient!=null,recovery?"원래 출발 요청 확인":"출격");
        }
        string[] PreparationBalances()=>new[]{accountState?.BalanceText(0)??"—",accountState?.BalanceText(1)??"—",accountState?.BalanceText(2)??"—"};
        void RenderPreparationSelections()
        {
            if(preparationView==null)return;
            var state=accountState;var account=state?.AccountStatus==AccountStateReadStatus.Ready?state.Account:null;
            for(int i=0;i<5;i++){
                var id=account?.ActiveOperators.FirstOrDefault(x=>x.Number("slot")==i)?.Text("operatorId");
                var core=id==null?null:account.TrainingCoreSlots.FirstOrDefault(x=>x.Text("operatorId")==id);
                preparationView.Member(i,id,core==null?null:"훈련 Lv."+core.Number("trainedLevel"),id!=null&&state.UltimateReady&&state.Ultimate?.OperatorId==id);
            }
            var load=state?.TrainLoadoutReady==true?state.TrainLoadout:null;
            preparationView.Carriage(0,load?.Train==null?"기관차 확인 필요":"기관차 · Lv."+(long)load.Train["level"],load?.Train==null?null:"CombatSecondFinal/train_locomotive-v03");
            var assigned=load?.Loadout?["carriages"] as JArray;var ownedCars=load?.Carriages;
            for(int i=0;i<4;i++){
                var id=(string)assigned?.FirstOrDefault(x=>(int)x["slot"]==i)?["carriageId"];
                var row=ownedCars?.FirstOrDefault(x=>(string)x["carriageId"]==id);string type=(string)row?["type"];
                string label=load==null?"편성 확인 필요":load.SlotCount.HasValue&&i>=load.SlotCount.Value?"잠김":type==null?"미배치":type=="COMBAT"?"전투칸":type=="GUN"?"포격칸":type=="CARGO"?"화물칸":type=="DEFENSE"?"방어칸":"의무칸";
                string resource=type=="CARGO"?"train_carriage_cargo-v03":type==null?null:"train_carriage_armored-v03";
                preparationView.Carriage(i+1,label,resource==null?null:"CombatSecondFinal/"+resource,load?.SlotCount.HasValue==true&&i>=load.SlotCount.Value,type);
            }
            var supportCurrent=account!=null&&ReferenceEquals(account,departureSupportOwner);
            for(int i=0;i<3;i++){var id=supportCurrent&&i<departureSupportIds.Length?departureSupportIds[i]:null;preparationView.Support(i,id,id==null?(localCommandClient==null?"연결 확인 필요":departureSupportNotice):GuideSupportName(id));}
        }
    }
}

