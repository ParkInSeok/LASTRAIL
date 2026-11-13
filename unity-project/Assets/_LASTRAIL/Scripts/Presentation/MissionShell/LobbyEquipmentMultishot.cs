using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.MissionShell {
 public sealed partial class LobbyDestinationPages {
  RectTransform equipmentMultishotPanel;
  Action equipmentMultishotClosed;
  readonly List<(GameObject child,bool active)> equipmentHiddenChildren=new List<(GameObject,bool)>();
  public void BindEquipmentMultishotClose(Action closed)=>equipmentMultishotClosed=closed;
  // Existing common popup/art composition. Only independent display fields and
  // the original close callback; no new placement confirmation or write flow.
  public void PresentEquipmentMultishot(JObject frame){
   if(!IsOpen||!isActiveAndEnabled||content==null)return;
   if(frame==null||((string)frame["version"]!="equipment.multishot-display-frame.v1.20261016"&&(string)frame["version"]!=EquipmentMultishotController.FrameVersion))throw new ArgumentException("Equipment display frame required");
   if(equipmentMultishotPanel==null){foreach(Transform child in content)equipmentHiddenChildren.Add((child.gameObject,child.gameObject.activeSelf));equipmentMultishotPanel=CoreShield("EquipmentMultishotDetails");}
   else{for(int i=equipmentMultishotPanel.childCount-1;i>=0;i--){var child=equipmentMultishotPanel.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}}
   var preview=frame["preview"] as JObject;
   var approved=LastRail.Resources.PreparedContent.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");
   void ItemFrame(string name,float top,float height){
    if(approved==null||approved.ItemFrame==null)return;
    var row=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();row.SetParent(equipmentMultishotPanel,false);
    row.anchorMin=row.anchorMax=row.pivot=new Vector2(0,1);row.anchoredPosition=new Vector2(40,-top);row.sizeDelta=new Vector2(861,height);
    ApprovedUiConnections.Frame(row,"Border",approved.ItemFrame,70f/3);
   }
   Text("EquipmentEffectTitle",equipmentMultishotPanel,preview==null?"장비 효과":"장착 미리보기",48,205,845,75,40,true);
   Text("PlacementPreviewNotice",equipmentMultishotPanel,(string)frame["previewNotice"]??"",48,280,845,24,20);
   void Section(string key,float y,JObject data,string fallback,string notice){
    Text(key+"Basis",equipmentMultishotPanel,data==null?fallback:(string)data["basisLabel"],48,y,845,48,30);
    Text(key+"Summary",equipmentMultishotPanel,data==null?"효과 상태 확인 전":(string)data["summary"],48,y+50,845,44,28);
    if(data!=null){var slots=(JArray)data["slots"];for(int i=0;i<slots.Count;i++){var row=slots[i];float top=y+105+i*105;
     ItemFrame(key+"ItemFrame"+i,top-6,94);
     Text(key+"Slot"+i,equipmentMultishotPanel,(string)row["slotLabel"],48,top,130,40,25);
     Text(key+"CurrentStar"+i,equipmentMultishotPanel,(string)row["currentStar"],180,top,200,40,25);
     Text(key+"AppliedStar"+i,equipmentMultishotPanel,(string)row["appliedStar"],390,top,230,40,25);
     Text(key+"Coefficient"+i,equipmentMultishotPanel,(string)row["coefficient"],625,top,268,40,25);
     Text(key+"Status"+i,equipmentMultishotPanel,(string)row["statusLabel"],180,top+42,713,38,23);
    }}Text(key+"Notice",equipmentMultishotPanel,notice??"",48,y+425,845,40,23);
   }
   if(preview!=null){
    Text("PreviewStatus",equipmentMultishotPanel,(string)preview["statusLabel"],48,280,845,25,21);
    if((string)preview["status"]=="PREVIEW"){
     Section("previewBefore",305,preview["before"] as JObject,"장착 전","");Section("previewAfter",790,preview["after"] as JObject,"장착 후 예상","");
     Text("PreviewBeforeStats",equipmentMultishotPanel,"기본 스탯 · 장착 전",48,1258,425,30,21);
     Text("PreviewAfterStats",equipmentMultishotPanel,"기본 스탯 · 장착 후 예상",500,1258,393,30,21);
     Text("PreviewBeforeHp",equipmentMultishotPanel,(string)preview["beforeStats"]["hp"],48,1292,425,32,23);
     Text("PreviewAfterHp",equipmentMultishotPanel,(string)preview["afterStats"]["hp"],500,1292,393,32,23);
     Text("PreviewBeforeAttack",equipmentMultishotPanel,(string)preview["beforeStats"]["attack"],48,1328,245,34,23);
     Text("PreviewBeforeDefense",equipmentMultishotPanel,(string)preview["beforeStats"]["defense"],300,1328,180,34,23);
     Text("PreviewAfterAttack",equipmentMultishotPanel,(string)preview["afterStats"]["attack"],500,1328,215,34,23);
     Text("PreviewAfterDefense",equipmentMultishotPanel,(string)preview["afterStats"]["defense"],720,1328,173,34,23);
    }else Text("PreviewCommittedNotice",equipmentMultishotPanel,"기존 장착 요청의 처리 결과를 확인했습니다.",48,400,845,70,27);
   }else{
    Section("current",305,frame["current"] as JObject,"현재 장착",(string)frame["currentNotice"]);Section("departure",790,frame["departure"] as JObject,"출발 시 적용",(string)frame["departureNotice"]);
    if(frame["stored"] is JObject stored){ItemFrame("StoredItemFrame",1269,90);var row=stored["slots"][0];Text("StoredCardStatus",equipmentMultishotPanel,(string)stored["basisLabel"]+" · "+(string)row["statusLabel"],48,1275,845,44,23);Text("StoredCurrentStar",equipmentMultishotPanel,(string)row["currentStar"],48,1320,240,36,23);Text("StoredAppliedStar",equipmentMultishotPanel,(string)row["appliedStar"],305,1320,275,36,23);Text("StoredCoefficient",equipmentMultishotPanel,(string)row["coefficient"],596,1320,297,36,23);}
    Text("StoredCardNotice",equipmentMultishotPanel,(string)frame["storedNotice"],48,1360,845,29,22);
   }
   detailBack=ReleaseEquipmentMultishot;
   Button("CloseEquipmentMultishot",equipmentMultishotPanel,48,1390,845,90,"닫기",ReleaseEquipmentMultishot);
  }
  public void ClearEquipmentMultishot(){equipmentMultishotClosed=null;if(equipmentMultishotPanel!=null){ReleaseDetailInput();var owned=equipmentMultishotPanel.gameObject;equipmentMultishotPanel=null;owned.SetActive(false);if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}foreach(var saved in equipmentHiddenChildren)if(saved.child!=null)saved.child.SetActive(saved.active);equipmentHiddenChildren.Clear();}
  void ReleaseEquipmentMultishot(){var callback=equipmentMultishotClosed;equipmentMultishotClosed=null;ClearEquipmentMultishot();callback?.Invoke();}
 }
}
