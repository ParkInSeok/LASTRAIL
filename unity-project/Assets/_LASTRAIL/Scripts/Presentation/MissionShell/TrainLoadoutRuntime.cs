using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed partial class AccountScreenRuntime
    {
        long trainLoadoutRequest;string[] trainDraft=new string[4];string trainDraftId;bool trainDraftDirty;
        public TrainLoadoutSnapshot TrainLoadout {get;private set;}public TrainLoadoutReceipt LastTrainLoadoutReceipt {get;private set;}
        public bool TrainLoadoutReady {get;private set;}public bool TrainLoadoutLoading {get;private set;}public string TrainLoadoutNotice {get;private set;}="";
        public string[] TrainDraft=>(string[])trainDraft.Clone();public bool TrainDraftDirty=>trainDraftDirty;
        void ResetTrainLoadout(){trainLoadoutRead=null;trainLoadoutRequest++;TrainLoadout=null;TrainLoadoutReady=false;TrainLoadoutLoading=false;LastTrainLoadoutReceipt=null;TrainLoadoutNotice="";trainDraft=new string[4];trainDraftId=null;trainDraftDirty=false;}
        void RotateTrainLoadoutCredentials()
        {
            // Invalidate borrowed requests, not this account's confirmed selection or draft.
            trainLoadoutRead=null;trainLoadoutRequest++;TrainLoadoutLoading=false;
        }
        Task trainLoadoutRead;long trainLoadoutReadRevision;
        public Task EnsureTrainLoadoutAsync()=>EnsureRead(()=>TrainLoadoutReady&&TrainLoadout!=null,RefreshTrainLoadoutAsync,ref trainLoadoutRead);
        public Task RefreshTrainLoadoutAsync(){SynchronizeSession();if(trainLoadoutReadRevision!=trainLoadoutRequest)trainLoadoutRead=null;if(trainLoadoutRead==null||trainLoadoutRead.IsCompleted)trainLoadoutReadRevision=trainLoadoutRequest+1;return ShareRead(ref trainLoadoutRead,ReadTrainLoadoutAsync);}
        async Task ReadTrainLoadoutAsync()
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++trainLoadoutRequest;if(!Current(original,stamp))return;TrainLoadoutReady=false;TrainLoadoutLoading=true;Changed?.Invoke();
            try{
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/trains/loadout",null,null),original.AccessToken));if(!Current(original,stamp)||request!=trainLoadoutRequest)return;
                if(reply.IsNetworkError||reply.StatusCode!=200)throw new InvalidOperationException("read_failed");var snapshot=new TrainLoadoutSnapshot(reply.BodyJson);TrainLoadout=snapshot;TrainLoadoutReady=true;
                TrainLoadoutNotice=TrainLoadoutProblem(snapshot.ProgressError??snapshot.LoadoutError??(snapshot.OpenRun?"train_loadout_open_run":null));
                if(Pending?.Kind=="train-loadout"){var r=TrainLoadoutWrite.Validate(Pending.Target,Pending.Payload);r.Remove("expectedRevision");trainDraft=TrainLoadoutWrite.ValidateSelection(r);trainDraftId=Pending.Target;trainDraftDirty=true;}
                else if(!trainDraftDirty){var saved=snapshot.Loadout;trainDraft=saved==null?new string[4]:TrainLoadoutWrite.ValidateSelection(saved);trainDraftId=saved==null?(string)snapshot.Train?["trainId"]:(string)saved["trainId"];}
            }catch{if(!Current(original,stamp)||request!=trainLoadoutRequest)return;TrainLoadoutNotice="장착 정보를 불러오지 못했습니다. 이전 정보는 유지됩니다.";}
            finally{if(Current(original,stamp)&&request==trainLoadoutRequest){TrainLoadoutLoading=false;Changed?.Invoke();}}
        }
        public bool CanEditTrain=>!Busy&&Pending==null&&TrainLoadoutReady&&TrainLoadout?.Train!=null&&TrainLoadout.SlotCount!=null&&TrainLoadout.ProgressError==null&&!TrainLoadout.OpenRun;
        public bool SelectTrainCarriage(int slot,string id)
        {
            if(!CanEditTrain||slot<0||slot>=TrainLoadout.SlotCount||!TrainLoadout.Carriages.OfType<JObject>().Any(c=>(string)c["carriageId"]==id&&(bool)c["installable"]))return false;
            var old=Array.IndexOf(trainDraft,id);if(old>=0)trainDraft[old]=trainDraft[slot];trainDraft[slot]=id;trainDraftId=(string)TrainLoadout.Train["trainId"];trainDraftDirty=true;Changed?.Invoke();return true;
        }
        public void ClearTrainDraft(){if(!CanEditTrain)return;trainDraft=new string[4];trainDraftId=(string)TrainLoadout.Train["trainId"];trainDraftDirty=true;Changed?.Invoke();}
        public bool CanSaveTrainLoadout
        {
            get{
                if(!CanEditTrain||trainDraftId!=(string)TrainLoadout.Train["trainId"])return false;var count=TrainLoadout.SlotCount.Value;
                if(trainDraft.Take(count).Any(s=>s==null)||trainDraft.Skip(count).Any(s=>s!=null)||trainDraft.Take(count).Distinct().Count()!=count)return false;
                var rows=TrainLoadout.Carriages.OfType<JObject>().Where(c=>trainDraft.Take(count).Contains((string)c["carriageId"])).ToArray();if(rows.Length!=count||rows.Any(c=>!(bool)c["installable"]))return false;
                var limits=TrainLoadout.Limits;foreach(var type in TrainLoadoutWrite.Types){var n=rows.Count(c=>(string)c["type"]==type);if(n<(long)limits[type]["min"]||n>(long)limits[type]["max"])return false;}return true;
            }
        }
        public Task SaveTrainLoadoutAsync()
        {
            if(!CanSaveTrainLoadout)return Task.CompletedTask;var r=TrainLoadoutWrite.Selection(trainDraftId,trainDraft,TrainLoadout.SlotCount.Value);r["expectedRevision"]=TrainLoadout.Revision;
            return SubmitAsync("train-loadout",trainDraftId,0,r.ToString());
        }
        public static string TrainLoadoutProblem(string code)=>code==null?"":code=="story_locked"?"작전 진행 후 해금":code=="medical_unlock_unapproved"?"의무실 장착은 준비 중":code=="carriage_definition_unsupported"?"지원하지 않는 차량":code=="turret_installation_undefined"?"포탑 장착은 준비 중":code=="train_loadout_open_run"?"진행 중인 작전을 마친 후 변경할 수 있습니다.":code=="story_progress_invalid"||code=="story_progress_inconsistent"?"작전 진행 정보 확인이 필요합니다.":code=="train_loadout_schema_unsupported"?"저장된 장착 정보 확인이 필요합니다.":"장착 조건 확인 필요 · "+code;
    }
}
