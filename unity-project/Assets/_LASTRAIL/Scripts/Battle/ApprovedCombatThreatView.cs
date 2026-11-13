using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.AugmentSelection;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 RectTransform threatPanel,threatDetails,threatContent;TMP_Text threatSummary,threatRows,threatDetailRows;Button threatMore,threatClose;
 AugmentModalInputScope threatInput;long threatRevision=-1;
 readonly CombatArtilleryOutcomeFeedback artilleryOutcome=new CombatArtilleryOutcomeFeedback();
 void BuildThreatDisplay(){
  threatPanel=Box("ThreatSummary",root,480,110,640,122,new Color(.035f,.065f,.08f,.92f)).rectTransform;
  threatSummary=Text("Title",threatPanel,"",10,4,620,25,19);
  threatRows=Text("Warnings",threatPanel,"",10,32,620,48,18);threatRows.alignment=TextAlignmentOptions.TopLeft;
  threatMore=Button("ThreatDetailsOpen","위험 목록",480,194,640,36,()=>SetThreatDetails(true));
  threatDetails=Box("ThreatDetails",root,480,140,640,440,new Color(.035f,.065f,.08f,.98f)).rectTransform;threatDetails.GetComponent<Image>().raycastTarget=true;
  Text("Title",threatDetails,"현재 주의 사건 · 대응 시간 정보 없음",14,12,610,34,22);
  var viewport=Box("Viewport",threatDetails,14,58,610,310,new Color(0,0,0,.1f)).rectTransform;viewport.gameObject.AddComponent<RectMask2D>();viewport.GetComponent<Image>().raycastTarget=true;
  threatContent=Rect("Content",viewport,0,0,610,310);threatDetailRows=Text("Rows",threatContent,"",0,0,610,310,20);threatDetailRows.alignment=TextAlignmentOptions.TopLeft;
  var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=threatContent;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
  threatClose=Button("ThreatDetailsClose","닫기",0,0,180,42,()=>SetThreatDetails(false));threatClose.transform.SetParent(threatDetails,false);((RectTransform)threatClose.transform).anchoredPosition=new Vector2(440,-385);
  threatDetails.gameObject.SetActive(false);threatPanel.gameObject.SetActive(false);threatMore.gameObject.SetActive(false);
 }
 void SetThreatDetails(bool visible){
  if(threatDetails==null)return;
  if(!visible){threatInput?.Dispose();threatInput=null;threatDetails.gameObject.SetActive(false);return;}
  if(threatPanel==null||!threatPanel.gameObject.activeSelf||!CommandsAvailable||FullCutinVisible)return;
  threatDetails.gameObject.SetActive(true);threatDetails.SetAsLastSibling();if(threatInput==null)threatInput=new AugmentModalInputScope(threatDetails,root,threatClose.gameObject);
 }
 void RefreshThreatDisplay(Newtonsoft.Json.Linq.JObject state){
  if(threatPanel==null)return;
  threatMore.interactable=!FullCutinVisible;
  if(driver.RecoveryRequired){SetThreatDetails(false);threatPanel.gameObject.SetActive(false);threatMore.gameObject.SetActive(false);threatRevision=-1;return;}
  if(!driver.TryGetCommittedRevision(out var revision)||revision==threatRevision)return;threatRevision=revision;
  var projection=CombatThreatProjection.Read(state);var rows=projection.Warnings;bool visible=rows.Count>0;
  var outcome=artilleryOutcome.Observe(state,visible);
  if(!visible)SetThreatDetails(false);threatPanel.gameObject.SetActive(visible||outcome!=null);threatMore.gameObject.SetActive(visible);
  if(!visible){threatSummary.text=outcome??"";threatRows.text="";return;}
  threatSummary.text=projection.Primary?.Label??"지속 주의 · 대응 시간 정보 없음";
  threatRows.text=string.Join("\n",rows.Take(2).Select(w=>"• "+w.Label));threatMore.GetComponentInChildren<TMP_Text>().text=rows.Count>2?"전체 "+rows.Count+"건 보기 · 나머지 "+(rows.Count-2)+"건":"전체 "+rows.Count+"건 보기";
  threatDetailRows.text=string.Join("\n",rows.Select(w=>"• "+w.Label));float height=Mathf.Max(310,rows.Count*30);threatContent.sizeDelta=new Vector2(610,height);threatDetailRows.rectTransform.sizeDelta=new Vector2(610,height);
 }
}
}
