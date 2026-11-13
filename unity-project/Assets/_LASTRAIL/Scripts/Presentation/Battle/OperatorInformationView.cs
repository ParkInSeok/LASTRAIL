using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.Battle {
 // Independent dynamic slots/text/icons. Layout is a review candidate, not final approved art.
 public sealed class OperatorInformationView : MonoBehaviour {
  RectTransform summary,detail;TMP_Text title,notice,description,health,shield;readonly Button[] slots=new Button[3];readonly TMP_Text[] labels=new TMP_Text[3];readonly RawImage[] icons=new RawImage[3];
  AugmentSelection.AugmentModalInputScope detailInput;GameObject detailFocus;
  Action<int> open;Action close;Action back;TMP_FontAsset font;Func<string,Texture> resolveIcon;OperatorInformationFrame last;bool suppressed;
  public bool DetailVisible=>detail!=null&&detail.gameObject.activeSelf;
  public void Configure(Transform parent,TMP_FontAsset suppliedFont,Func<string,Texture> iconResolver=null){
   if(summary!=null)throw new InvalidOperationException("Information view already configured.");font=suppliedFont;resolveIcon=iconResolver;
   summary=Panel("OperatorInformation",parent,40,689,640,166);title=Text("Title",summary,12,6,610,25,20);notice=Text("SourceAndRange",summary,12,117,610,45,16);
   health=Text("Health",summary,12,33,350,24,18);shield=Text("Shield",summary,370,33,240,24,18);
   for(int i=0;i<3;i++){int slot=i;slots[i]=Button("EquippedSlot"+i,summary,12+i*207,61,198,52,()=>open?.Invoke(slot));icons[i]=Rect("Icon",slots[i].transform,5,5,42,42).gameObject.AddComponent<RawImage>();icons[i].raycastTarget=false;labels[i]=Text("Label",slots[i].transform,52,4,140,46,16);}
   detail=Panel("OperatorEquipmentDetail",parent,40,389,640,295);description=Text("Description",detail,14,12,610,220,18);
   var closeButton=Button("CloseEquipmentDetail",detail,450,240,175,42,()=>close?.Invoke());detailFocus=closeButton.gameObject;Text("Caption",closeButton.transform,4,4,165,32,19).text="닫기";
   var backButton=Button("CloseOperatorInformation",summary,550,5,78,27,()=>back?.Invoke());Text("Caption",backButton.transform,4,1,70,24,16).text="닫기";
   summary.gameObject.SetActive(false);detail.gameObject.SetActive(false);
  }
  public void Bind(Action<int> openDetail,Action closeDetail,Action goBack=null){open=openDetail;close=closeDetail;back=goBack;}
  public void Unbind(){open=null;close=null;back=null;last=null;if(summary!=null)summary.gameObject.SetActive(false);SetDetailVisible(false);}
  public void SetSuppressed(bool value){if(suppressed==value)return;suppressed=value;if(last!=null)Present(last);}
  public void Present(OperatorInformationFrame frame){
   last=frame;if(summary==null)return;bool visible=frame.Visible&&frame.Status=="READY"&&frame.Snapshot!=null&&!suppressed;summary.gameObject.SetActive(visible);if(!visible){SetDetailVisible(false);return;}
   title.text="요원 정보";
   bool ready=frame.Status=="READY";var snapshot=ready?frame.Snapshot:null;
   health.text=snapshot.Health==null?"HP 정보 미확인":"HP "+snapshot.Health.CurrentHp+" / "+snapshot.Health.MaxHp;
   shield.text=snapshot.Health?.Shield is int value?"보호막 "+value:"보호막 미확인";
   notice.text=snapshot.RangeNotice+"\n"+snapshot.SourceNotice;
   for(int i=0;i<3;i++){
    var item=snapshot?.Slot(i);bool known=snapshot!=null&&snapshot.EquipmentKnown;
    slots[i].interactable=ready&&known&&item!=null;
    labels[i].text=!known?"장착 정보 미확인":item==null?"빈 슬롯":(item.Name??"이름 미확인")+" ★"+item.Star;
    var texture=item?.IconKey==null?null:resolveIcon?.Invoke(item.IconKey);icons[i].texture=texture;icons[i].gameObject.SetActive(texture!=null);
   }
   if(ready&&frame.DetailSlot>=0){var item=snapshot.Slot(frame.DetailSlot);description.text=(item.Name??"이름 미확인")+" ★"+item.Star+"\n"+item.Description;}SetDetailVisible(ready&&frame.DetailSlot>=0);
  }
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h){var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.035f,.065f,.08f,.98f);image.raycastTarget=true;return r;}
  static Button Button(string name,Transform parent,float x,float y,float w,float h,Action click){var r=Panel(name,parent,x,y,w,h);var b=r.gameObject.AddComponent<LastRail.Battle.CombatInputButton>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(()=>click());return b;}
  TMP_Text Text(string name,Transform parent,float x,float y,float w,float h,float size){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=Color.white;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
  void SetDetailVisible(bool value){if(detail==null)return;if(!value){detailInput?.Dispose();detailInput=null;detail.gameObject.SetActive(false);return;}detail.gameObject.SetActive(true);if(detailInput==null)detailInput=new AugmentSelection.AugmentModalInputScope(detail,detail.parent,detailFocus);}
  void OnDisable()=>Unbind();
 }
}
