using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Auth;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyCommandDeck {
 bool rouletteView;
 void BuildOfflineAdEntry(){
  rouletteView=false;dialog.sizeDelta=new Vector2(750,880);
  Place(dialog.Find("CloseLobbyDialog").GetComponent<RectTransform>(),60,765,630,70);
  Button("OpenOfflineRoulette",dialog,60,645,630,80,"보급 룰렛",OpenOfflineRoulette);
 }
 async void OpenOfflineRoulette(){
  var owner=accountState;if(owner==null||owner.Busy||!RewardEntryDisplayState.Offline(owner).Visible)return;
  bool use=owner.AdSkip.Snapshot?.Owned==true&&owner.AdIntent?.Status!="CLAIMED"&&owner.AdIntent?.Status!="EXPIRED"&&owner.AdIntent?.Status!="SUPPLY_CONSUMED";
  if(!await OpenResourceDialogAsync(RouletteResources,()=>OpenOfflineSupplyPrepared(true)))return;
  if(owner!=accountState||!isActiveAndEnabled||!rouletteView)return;
  if(use)await owner.UseAdSkipOfflineAsync();
 }
 SharedRoulettePopup offlineRoulettePopup;
 void BuildOfflineAd(){
  dialog.sizeDelta=new Vector2(1600,900);dialog.GetComponent<Image>().color=Color.clear;
  foreach(Transform child in dialog)child.gameObject.SetActive(false);
  offlineRoulettePopup=SharedRoulettePopup.Create(dialog,font,"Ad");
  offlineRoulettePopup.Bind(ActOfflineRoulette,RecheckOfflineRoulette,null,()=>OpenOfflineSupply(false));
 }
 async void RecheckOfflineRoulette(){if(accountState!=null)await accountState.RecheckAdAsync();}
 async void ActOfflineRoulette(){
  if(accountState==null)return;
  if(accountState.AdIntent?.Status=="CLAIMED"||accountState.AdIntent?.Status=="EXPIRED"||accountState.AdIntent?.Status=="SUPPLY_CONSUMED"){
   accountState.AcknowledgeAdResult();OpenOfflineSupply(false);return;
  }
  if(accountState.Busy)return;
  if(accountState.Pending?.Kind=="reward-approval")await accountState.RetryPendingAsync();
  else if(accountState.CanSpinAd)await accountState.SpinAdAsync();
  else if(accountState.AdSkip.Snapshot?.Owned==true)await accountState.UseAdSkipOfflineAsync();
  else if(accountState.AdSkip.Snapshot==null){if(accountState.HasPendingAd)await accountState.RecheckAdAsync();else await accountState.RefreshAdSkipAsync();}
  else if(accountState.Pending?.Kind=="ad-intent"&&accountState.AdIntent?.Status=="AD_PENDING"&&accountState.AdProviderAvailable&&accountState.RuntimeAds.Status==AdRuntimePolicyStatus.Enabled)await accountState.PresentAdAsync();
  else if(accountState.HasPendingAd)await accountState.RecheckAdAsync();else await accountState.BeginAdAsync();
 }
 void UpdateOfflineAd(){
  if(!rouletteView||offlineRoulettePopup==null)return;
  var state=accountState;var intent=state?.AdIntent;var result=intent?.Result;
  bool terminal=intent?.Status=="CLAIMED"||intent?.Status=="EXPIRED"||intent?.Status=="SUPPLY_CONSUMED";
  bool paid=intent?.Status=="CLAIMED"||intent?.Status=="SUPPLY_CONSUMED";
  string label=terminal?"확인":state?.CanSpinAd==true?"룰렛 돌리기":state?.Pending?.Kind=="ad-intent"&&state?.AdProviderAvailable==true?"광고 시청":state?.HasPendingAd==true?"다시 확인":"광고 시청";
  bool owned=state?.AdSkip.Snapshot?.Owned==true,unknown=state?.AdSkip.Snapshot==null;
  if(!terminal&&state?.CanSpinAd!=true){if(state?.Pending?.Kind=="reward-approval")label="사용 결과 재확인";else if(owned)label="광고 없이 룰렛";else if(unknown)label=state?.HasPendingAd==true?"다시 확인":"이용권 다시 확인";}
  if(!terminal&&state?.CanSpinAd!=true&&!owned&&state?.HasPendingAd==true&&state.RuntimeAds.Status!=AdRuntimePolicyStatus.Enabled)label="다시 확인";
  string notice=state?.AdSkipUse.Notice;
  if(string.IsNullOrEmpty(notice))notice=unknown&&state?.HasPendingAd!=true?state?.AdSkip.Notice:state?.AdNotice??"";
  if(state?.Busy!=true&&intent==null&&state?.AdSkip.Snapshot?.Status=="REVOKED"&&string.IsNullOrEmpty(state.AdSkipUse.Notice))notice="이용권 상태가 변경되었습니다. 기본 보급은 받을 수 있습니다.";
  if(result!=null||state?.CanSpinAd==true&&!state.Busy)notice="";else if(intent?.Status=="SUPPLY_CONSUMED")notice="기본 보급을 이미 수령했습니다.";
  else if(string.IsNullOrEmpty(notice))notice=state?.Busy==true?"결과 확인 중…":intent?.Status=="AD_PENDING"?"광고 확인 중…":state?.AdProviderAvailable!=true?"현재 광고를 이용할 수 없습니다. 기본 보상은 받을 수 있습니다.":"";
  var rows=new Newtonsoft.Json.Linq.JArray();if(state?.Offline!=null)foreach(var reward in state.Offline.RewardLines)rows.Add(new Newtonsoft.Json.Linq.JObject{["id"]=reward.RewardCode,["kind"]=reward.Kind,["source"]=reward.Source,["quantity"]=reward.Quantity});
  if(intent?.BaseLines!=null)rows=intent.BaseLines;
  offlineRoulettePopup.ConfigureProbability(intent==null?"보급 정보를 먼저 확인해 주세요.":"가능 배수  "+string.Join(" · ",intent.Multipliers.Select(x=>x+"배"))+"\n\n"+intent.ProbabilityText,intent?.Multipliers);
  bool separateRefresh=state?.HasPendingAd==true&&!terminal&&label!="다시 확인";
  offlineRoulettePopup.Render(result,rows,notice,label,state!=null&&(terminal||state.HasPendingAd||state.Pending?.Kind=="reward-approval"||state.Pending==null&&(owned||unknown||state.AdProviderAvailable)),state?.Busy==true,paid,separateRefresh,false);
 }
 [RequireComponent(typeof(CanvasRenderer))] public sealed class RouletteDiscGraphic:MaskableGraphic {
  protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();var r=rectTransform.rect;var center=r.center;mesh.AddVert(center,color,new Vector2(.5f,.5f));for(int i=0;i<=96;i++){float a=i*Mathf.PI*2/96;mesh.AddVert(center+new Vector2(Mathf.Cos(a)*r.width*.44f,Mathf.Sin(a)*r.height*.44f),new Color(color.r*.5f,color.g*.5f,color.b*.5f,color.a),Vector2.zero);if(i>0)mesh.AddTriangle(0,i,i+1);}}
 }
 static string CurrencyLabel(string id)=>id=="cur_credits"?"크레딧":id=="cur_machine_parts"?"기계 부품":id=="cur_tactical_data"?"전술 데이터":id=="cur_operator_exp"?"요원 경험치":id;
 }
 public sealed class OfflineAdResultMotion:MonoBehaviour {
  RectTransform ring;TMP_Text label;string receipt;float began;bool moving,sawWaiting;
  public void Bind(RectTransform value,TMP_Text text){ring=value;label=text;}
  public void Present(Newtonsoft.Json.Linq.JObject result){
   if(label==null)return;if(result==null){sawWaiting=true;receipt=null;moving=false;label.text="—";if(ring!=null)ring.localRotation=Quaternion.identity;return;}
   var id=(string)result["rewardTransactionId"]??(string)result["rewardClaimId"];label.text=(int)result["multiplier"]+"배";if(receipt==id)return;
   receipt=id;began=Time.unscaledTime;moving=Application.isPlaying&&sawWaiting;sawWaiting=false;
  }
  void Update(){if(!moving||ring==null)return;float t=Mathf.Clamp01((Time.unscaledTime-began)/1.2f);ring.localRotation=Quaternion.Euler(0,0,720*(1-Mathf.Pow(1-t,3)));if(t>=1){moving=false;ring.localRotation=Quaternion.identity;}}
  void OnDisable(){moving=false;if(ring!=null)ring.localRotation=Quaternion.identity;}
 }
}


