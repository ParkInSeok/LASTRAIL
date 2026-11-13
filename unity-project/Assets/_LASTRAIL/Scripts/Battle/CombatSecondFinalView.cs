using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 Slider branchLever,trainThrottle;TMP_Text routeDescription,throttleDescription;
 void BuildSecondFinalConsole(){
  // Dim only the physical console backing; children retain readable state colors.
  root.Find("CommandConsole").GetComponent<RawImage>().color=new Color(.38f,.43f,.46f);
  var cabinGroup=Rect("CabinContent",root,0,389,720,447);cabinGroup.localScale=new Vector3(1,367f/447,1);cabinLayer.transform.SetParent(cabinGroup,false);cabinLayer.rectTransform.anchoredPosition=Vector2.zero;foreach(var glass in windows){glass.transform.SetParent(cabinGroup,false);glass.rectTransform.anchoredPosition+=new Vector2(0,389);}actorLayer.SetParent(cabinGroup,false);actorLayer.anchoredPosition=new Vector2(0,389);foreach(var card in operatorCards)((RectTransform)card.Button.transform.parent).anchoredPosition+=new Vector2(0,80);
  var title=root.Find("Title").GetComponent<TMP_Text>();title.gameObject.SetActive(true);title.fontSize=29;title.characterSpacing=1;title.rectTransform.anchoredPosition=new Vector2(12,-10);title.rectTransform.sizeDelta=new Vector2(200,40);
  wave.fontSize=18;wave.rectTransform.anchoredPosition=new Vector2(224,-8);wave.rectTransform.sizeDelta=new Vector2(150,30);waveTrack.rectTransform.anchoredPosition=new Vector2(224,-44);waveTrack.rectTransform.sizeDelta=new Vector2(150,8);
  if(ultimateAutoButton!=null)ultimateAutoButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(386,-11);
  PositionHeaderControl(speed,517,99);PositionHeaderControl(pause,628,80);
  var arrows=speed.transform.Find("SpeedChevrons");if(arrows!=null){((RectTransform)arrows).anchoredPosition=new Vector2(3,-3);((RectTransform)arrows).sizeDelta=new Vector2(24,38);}
  foreach(var c in controls.FindAll(v=>v.Group=="manuals")){
   int i=c.Index;var r=c.Button.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(458,-(1008+i*76));r.sizeDelta=new Vector2(238,66);
   var well=(RectTransform)c.Button.transform.Find("PressWell");well.anchoredPosition=new Vector2(2,-2);well.sizeDelta=new Vector2(234,62);well.GetComponent<Image>().color=new Color(.02f,.06f,.07f,.65f);
   var frame=Rect("SupportHousing",c.Button.transform,0,0,238,66).gameObject.AddComponent<CombatConceptPanel>();frame.Metal=true;frame.raycastTarget=false;frame.transform.SetAsFirstSibling();
   c.AbilityIcon.rectTransform.anchoredPosition=new Vector2(4,-3);c.AbilityIcon.rectTransform.sizeDelta=new Vector2(60,60);
   c.Text.rectTransform.anchoredPosition=new Vector2(70,-6);c.Text.rectTransform.sizeDelta=new Vector2(158,54);c.Text.fontSize=20;
   c.Wipe.rectTransform.anchoredPosition=new Vector2(10,-9);c.Wipe.rectTransform.sizeDelta=new Vector2(48,48);
   c.CooldownText.rectTransform.anchoredPosition=new Vector2(10,-9);c.CooldownText.rectTransform.sizeDelta=new Vector2(48,48);c.CooldownText.fontSize=20;
   c.Rim.rectTransform.anchoredPosition=new Vector2(5,-4);c.Rim.rectTransform.sizeDelta=new Vector2(58,58);
   c.Button.GetComponent<CombatCommandPress>().Configure(well.GetComponent<Image>(),()=>presentationClock());
  }
  var left=Rect("BranchHousing",root,24,1003,275,235).gameObject.AddComponent<CombatConceptPanel>();left.Metal=true;left.raycastTarget=false;
  var middle=Rect("ThrottleHousing",root,310,1003,134,235).gameObject.AddComponent<CombatConceptPanel>();middle.Metal=true;middle.raycastTarget=false;
  Text("BranchTitle",root,"분기 · 증강",36,1008,250,26,19);
  branchLever=RailSlider("BranchLever",46,1122,231,70,false);((CombatBranchLever)branchLever).Commit=ConfirmRailRoute;
  routeDescription=Text("RouteDescription",root,"분기 선택 없음",37,1085,248,35,14);
  Text("LeverHint",root,"◀             ▶",45,1193,234,29,20);
  Text("ThrottleTitle",root,"운행 속도",316,1008,122,26,18);
  trainThrottle=RailSlider("TrainThrottle",350,1050,40,148,true);
  for(int i=0;i<7;i++)Box("ThrottleTick"+i,root,401,1057+i*22,i%3==0?22:13,2,new Color(.44f,.58f,.59f));
  throttleDescription=Text("ThrottleDescription",root,"순항",316,1205,122,26,14);
  branchLever.onValueChanged.AddListener(v=>PreviewRailRoute(Mathf.RoundToInt(v)));
  trainThrottle.onValueChanged.AddListener(v=>ChangeRailThrottle(v));
  branchLever.interactable=false;trainThrottle.interactable=false;
  ApplySecondFinalArt();
  ApplyTitleBar();
  ApplyExteriorQuality();
  ApplyHullQuality();
  (GetComponent<CombatSafeArea>()??gameObject.AddComponent<CombatSafeArea>()).Bind(root,GetComponent<Canvas>());
 }
 void PositionHeaderControl(Button b,float x,float width){var r=b.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(x,-10);r.sizeDelta=new Vector2(width,44);var housing=(RectTransform)b.transform.Find("ControlHousing");housing.sizeDelta=new Vector2(width,44);var t=b.GetComponentInChildren<TMP_Text>();t.rectTransform.anchoredPosition=new Vector2(b==speed?27:3,-3);t.rectTransform.sizeDelta=new Vector2(b==speed?width-30:width-6,38);t.fontSize=20;}
 Slider RailSlider(string name,float x,float y,float w,float h,bool vertical){var r=Rect(name,root,x,y,w,h);Box("Track",r,vertical?w*.4f:0,vertical?0:h*.44f,vertical?w*.2f:w,vertical?h:h*.12f,new Color(.08f,.19f,.21f));Slider slider=vertical?r.gameObject.AddComponent<CombatPointerSlider>():r.gameObject.AddComponent<CombatBranchLever>();slider.direction=vertical?Slider.Direction.BottomToTop:Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;slider.wholeNumbers=!vertical;var area=Rect("HandleArea",r,vertical?0:55,vertical?37:0,vertical?w:w-110,vertical?h-74:h);var handle=Box("Handle",area,0,0,vertical?48:34,vertical?24:70,new Color(.48f,.59f,.62f));handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;handle.raycastTarget=true;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;return slider;}
 void PreviewRailRoute(int index){if(!CommandsAvailable)return;try{driver.PreviewRailRoute(index);Refresh();}catch{notice.text="경로 선택 기록을 확인해 주세요";}}
 void ConfirmRailRoute(){if(!CommandsAvailable)return;var rail=driver.HudState["railJourney"];var offer=rail?["offer"] as Newtonsoft.Json.Linq.JObject;if(offer==null)return;try{driver.ConfirmRailRoute((string)offer["id"],(string)offer["options"][(int)rail["previewIndex"]]["id"]);Refresh();}catch{notice.text="경로 확정 기록을 확인해 주세요";}}
 void ChangeRailThrottle(float value){if(!CommandsAvailable)return;var state=driver.HudState;var rail=state["railJourney"]??state["ordinaryRail"];if(rail?["minimumBps"]==null)return;try{driver.SetRailThrottle(Mathf.RoundToInt(Mathf.Lerp((int)rail["minimumBps"],(int)rail["maximumBps"],value)));Refresh();}catch{notice.text="운행 설정 기록을 확인해 주세요";}}
 void RefreshRailControls(Newtonsoft.Json.Linq.JObject state){
  if(trainThrottle==null)return;var rail=(state["railJourney"]??state["ordinaryRail"]) as Newtonsoft.Json.Linq.JObject;bool active=rail!=null&&(state["ordinaryRail"]==null||(bool?)state["fullCombatReady"]==true)&&!driver.RecoveryRequired&&!(bool)state["paused"]&&(string)state["outcome"]=="Running";
  trainThrottle.interactable=active;var offer=rail?["offer"] as Newtonsoft.Json.Linq.JObject;branchLever.interactable=active&&offer!=null;
  if(rail==null)return;
  int low=(int)rail["minimumBps"],high=(int)rail["maximumBps"],value=(int)rail["throttleBps"];trainThrottle.SetValueWithoutNotify(Mathf.InverseLerp(low,high,value));throttleDescription.text=(value<10000?"저속":value==10000?"순항":"고속")+" "+(value/10000f).ToString("0.00")+"×";
  if((string)state["outcome"]!="Running")routeDescription.text=(string)state["outcome"]=="Victory"?"작전 완료":"작전 종료";
  else if(offer!=null){branchLever.maxValue=((Newtonsoft.Json.Linq.JArray)offer["options"]).Count-1;int index=(int)rail["previewIndex"];branchLever.SetValueWithoutNotify(index);routeDescription.text=(string)offer["options"][index]["label"]+"\n"+(string)offer["options"][index]["description"];}
  else routeDescription.text=(string)rail["phase"]=="Recovering"?"선로 전환 중":"다음 분기 대기";
 }
}
}

