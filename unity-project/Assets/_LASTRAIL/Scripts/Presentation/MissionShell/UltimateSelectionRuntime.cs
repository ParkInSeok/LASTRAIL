using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class UltimateSelectionSnapshot
    {
        public const string Rules="ultimate-selection-v1";
        public string OperatorId {get;}
        public long Revision {get;}
        public long FormationRevision {get;}
        public string Status {get;}
        public bool ChangeLocked {get;}
        public UltimateSelectionSnapshot(string json,bool receipt=false)
        {
            var r=GameApiJson.ReadObject(json);
            if(r.Count!=7||!GameApiJson.Boolean(r,"accepted")||GameApiJson.String(r,"rulesVersion")!=Rules)throw new FormatException();
            OperatorId=RecruitmentJson.NullableString(r,"operatorId");if(OperatorId!=null)RecruitmentJson.Id(r,"operatorId");
            Revision=RevisionOf(r,"revision");FormationRevision=RevisionOf(r,"formationRevision");Status=GameApiJson.String(r,"selectionStatus");
            if(receipt){if(!GameApiJson.Boolean(r,"refreshRequired")||Status!="SELECTED"&&Status!="UNSELECTED")throw new FormatException();}
            else ChangeLocked=GameApiJson.Boolean(r,"changeLocked");
            if(!new[]{"FORMATION_REQUIRED","OWNERSHIP_INVALID","UNSELECTED","SELECTED"}.Contains(Status)||
                Status=="SELECTED"&&OperatorId==null||Status=="UNSELECTED"&&OperatorId!=null||
                Status=="FORMATION_REQUIRED"&&(OperatorId!=null||Revision!=0||FormationRevision!=0)||Status!="FORMATION_REQUIRED"&&FormationRevision==0)throw new FormatException();
        }
        internal static long RevisionOf(JObject r,string name){var n=AccountStateSnapshot.Number(r,name);if(n>=9007199254740991L)throw new FormatException();return n;}
        public static JObject ValidateRequest(string target,string json)
        {
            if(target!="active"||json==null||json.Length>1500)throw new FormatException();var r=GameApiJson.ReadObject(json);
            if(r.Count!=4||GameApiJson.String(r,"rulesVersion")!=Rules)throw new FormatException();
            var id=RecruitmentJson.NullableString(r,"operatorId");if(id!=null)RecruitmentJson.Id(r,"operatorId");
            RevisionOf(r,"expectedRevision");RevisionOf(r,"expectedFormationRevision");return r;
        }
    }
    public sealed partial class AccountScreenRuntime
    {
        long ultimateRequest;
        bool ultimateDirty;
        public UltimateSelectionSnapshot Ultimate {get;private set;}
        public bool UltimateReady {get;private set;}
        public bool UltimateLoading {get;private set;}
        public string UltimateDraft {get;private set;}
        public string UltimateNotice {get;private set;}="";
        void ResetUltimate(){ultimateRead=null;ultimateRequest++;Ultimate=null;UltimateReady=false;UltimateLoading=false;UltimateDraft=null;ultimateDirty=false;UltimateNotice="";}
        void RotateUltimateCredentials()
        {
            ultimateRead=null;ultimateRequest++;UltimateLoading=false;
        }
        public bool CanEditUltimate=>!Busy&&Pending==null&&UltimateReady&&Ultimate!=null&&!Ultimate.ChangeLocked&&
            (Ultimate.Status=="SELECTED"||Ultimate.Status=="UNSELECTED")&&AccountStatus==AccountStateReadStatus.Ready&&
            Account?.FormationRevision==Ultimate.FormationRevision&&Account.ActiveOperators.Count==5;
        bool EligibleUltimate(string id)=>id==null||Account!=null&&Account.ActiveOperators.Any(o=>o.Text("operatorId")==id)&&Account.Operators.Any(o=>o.Text("operatorId")==id);
        public bool SelectUltimate(string id)
        {SynchronizeSession();if(!CanEditUltimate||!EligibleUltimate(id))return false;UltimateDraft=id;ultimateDirty=true;Changed?.Invoke();return true;}
        Task ultimateRead;long ultimateReadRevision;
        public Task EnsureUltimateAsync()=>EnsureRead(()=>UltimateReady&&Ultimate!=null,RefreshUltimateAsync,ref ultimateRead);
        public Task RefreshUltimateAsync(){SynchronizeSession();if(ultimateReadRevision!=ultimateRequest)ultimateRead=null;if(ultimateRead==null||ultimateRead.IsCompleted)ultimateReadRevision=ultimateRequest+1;return ShareRead(ref ultimateRead,ReadUltimateAsync);}
        async Task ReadUltimateAsync()
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++ultimateRequest;if(!Current(original,stamp))return;
            UltimateReady=false;UltimateLoading=true;Changed?.Invoke();
            try{
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/formation/ultimate",null,null),original.AccessToken));
                if(!Current(original,stamp)||request!=ultimateRequest)return;
                if(reply.IsNetworkError||reply.StatusCode!=200)throw new InvalidOperationException();
                var snapshot=new UltimateSelectionSnapshot(reply.BodyJson);
                if(Ultimate==null||Ultimate.Revision!=snapshot.Revision||Ultimate.OperatorId!=snapshot.OperatorId||Ultimate.FormationRevision!=snapshot.FormationRevision)Departure=null;
                Ultimate=snapshot;UltimateReady=true;
                if(Pending?.Kind=="ultimate-selection"){UltimateDraft=(string)UltimateSelectionSnapshot.ValidateRequest(Pending.Target,Pending.Payload)["operatorId"];ultimateDirty=true;}
                else if(!ultimateDirty)UltimateDraft=snapshot.OperatorId;
                UltimateNotice=snapshot.ChangeLocked?"진행 중인 작전을 마친 뒤 변경할 수 있습니다.":snapshot.Status=="FORMATION_REQUIRED"?"먼저 편성을 활성화해 주세요.":snapshot.Status=="OWNERSHIP_INVALID"?"활성 편성의 보유 대원을 확인해 주세요.":snapshot.OperatorId==null?"분대를 이끌 대원을 선택해 주세요.":"현재 분대장: "+OperatorVisualCatalog.Label(snapshot.OperatorId);
            }catch{if(!Current(original,stamp)||request!=ultimateRequest)return;UltimateNotice="분대장 선택을 불러오지 못했습니다. 다시 확인해 주세요.";}
            finally{if(Current(original,stamp)&&request==ultimateRequest){UltimateLoading=false;Changed?.Invoke();}}
        }
        public Task SaveUltimateAsync()
        {
            SynchronizeSession();if(!CanEditUltimate||!EligibleUltimate(UltimateDraft))return Task.CompletedTask;
            var body=new JObject{["rulesVersion"]=UltimateSelectionSnapshot.Rules,["operatorId"]=UltimateDraft==null?JValue.CreateNull():new JValue(UltimateDraft),["expectedRevision"]=Ultimate.Revision,["expectedFormationRevision"]=Ultimate.FormationRevision};
            return SubmitUltimateAsync(body.ToString());
        }
        async Task SubmitUltimateAsync(string payload)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(Busy||!Current(original,stamp))return;
            writing=true;ActionErrorCode=null;ActionNotice="분대장 선택을 확인하고 있습니다.";Changed?.Invoke();var refresh=false;
            try{
                var body=UltimateSelectionSnapshot.ValidateRequest("active",payload);if(journal==null)throw new InvalidOperationException();
                var action=journal.Read(original.AccountId);
                if(action!=null&&(action.Kind!="ultimate-selection"||action.Payload!=payload)){Pending=action;ActionNotice="이전 요청 결과를 먼저 확인해 주세요.";return;}
                if(action==null){action=new PendingAccountAction(original.AccountId,"ultimate-selection","active",0,Guid.NewGuid().ToString("N"),payload);if(!journal.Save(action))throw new InvalidOperationException();}
                Pending=action;UltimateDraft=(string)body["operatorId"];ultimateDirty=true;UltimateReady=false;ultimateRequest++;UltimateLoading=false;
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/formation/ultimate",payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),original.AccessToken));
                if(!Current(original,stamp))return;
                if(!response.IsNetworkError&&response.StatusCode==200){
                    var receipt=new UltimateSelectionSnapshot(response.BodyJson,true);
                    var expected=UltimateSelectionSnapshot.RevisionOf(body,"expectedRevision");
                    if(receipt.OperatorId!=(string)body["operatorId"]||receipt.FormationRevision!=UltimateSelectionSnapshot.RevisionOf(body,"expectedFormationRevision")||receipt.Revision<expected||receipt.Revision>expected+1)throw new FormatException();
                    if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;ultimateDirty=false;Departure=null;refresh=true;ActionNotice="분대장 선택을 저장했습니다.";
                }else if(!response.IsNetworkError&&(response.StatusCode==400||response.StatusCode==403||response.StatusCode==409)){
                    var error=GameApiJson.ReadObject(response.BodyJson);if(GameApiJson.Boolean(error,"accepted"))throw new FormatException();ActionErrorCode=GameApiJson.String(error,"errorCode");
                    if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;Departure=null;refresh=true;ActionNotice="선택은 유지했습니다. 최신 상태를 확인한 뒤 다시 저장해 주세요.";
                }else ActionNotice="저장 결과 확인이 필요합니다. 같은 요청으로 재확인해 주세요.";
            }catch{if(Current(original,stamp))ActionNotice="저장 결과를 확인하지 못했습니다. 같은 요청으로 재확인해 주세요.";}
            finally{writing=false;if(!disposed)Changed?.Invoke();}
            if(refresh&&Current(original,stamp))await Task.WhenAll(RefreshAccountAsync(),RefreshUltimateAsync());
        }
    }
}

