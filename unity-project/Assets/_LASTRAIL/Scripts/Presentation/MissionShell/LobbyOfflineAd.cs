using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Auth;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyCommandDeck {
 bool rouletteView;OfflineRouletteScreenController offlineRouletteController;
 void ReleaseOfflineRouletteBinding(){offlineRouletteController?.Dispose();offlineRouletteController=null;offlineRoulettePopup?.Unbind();offlineRoulettePopup=null;rouletteView=false;}
 void BuildOfflineAdEntry(){
  rouletteView=false;dialog.sizeDelta=new Vector2(750,880);
  Place(dialog.Find("CloseLobbyDialog").GetComponent<RectTransform>(),60,765,630,70);
  Button("OpenOfflineRoulette",dialog,60,645,630,80,"보급 룰렛",OpenOfflineRoulette);
 }
 async void OpenOfflineRoulette(){
  var owner=accountState;if(owner==null||owner.Busy||!RewardEntryDisplayState.Offline(owner).Visible)return;
  var currentSession=owner.CaptureScreenSession();
  bool use=owner.AdSkip.Snapshot?.Owned==true&&owner.AdIntent?.Status!="CLAIMED"&&owner.AdIntent?.Status!="EXPIRED"&&owner.AdIntent?.Status!="SUPPLY_CONSUMED";
  if(!await OpenResourceDialogAsync(RouletteResources,()=>OpenOfflineSupplyPrepared(true)))return;
  if(owner!=accountState||!currentSession()||!isActiveAndEnabled||!rouletteView)return;
  if(offlineRouletteController!=null)await offlineRouletteController.EnterAsync(use);
 }
 SharedRoulettePopup offlineRoulettePopup;
 void BuildOfflineAd(){
  dialog.sizeDelta=new Vector2(1600,900);dialog.GetComponent<Image>().color=Color.clear;
  foreach(Transform child in dialog)child.gameObject.SetActive(false);
  var popup=offlineRoulettePopup=SharedRoulettePopup.Create(dialog,font,"Ad");var owner=accountState;var ownedDialog=dialog;
  if(owner==null)return;
  void Render(OfflineRouletteScreenState state){popup.ConfigureProbability(state.Probability,state.Multipliers);popup.Render(state.Result,state.Lines,state.Notice,state.Label,state.Enabled,state.Busy,state.Paid,state.SeparateRefresh,false);}
  var controller=offlineRouletteController=new OfflineRouletteScreenController(owner,Render,()=>OpenOfflineSupply(false),()=>this!=null&&isActiveAndEnabled&&rouletteView&&dialog==ownedDialog&&accountState==owner&&offlineRoulettePopup==popup);
  popup.Bind(async()=>await controller.ActAsync(),async()=>await controller.RecheckAsync(),null,controller.Back);
 }
 void UpdateOfflineAd(){if(rouletteView)offlineRouletteController?.Render();}
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


