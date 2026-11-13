using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 readonly List<RectTransform> consist=new List<RectTransform>();
 readonly Dictionary<long,RectTransform> incomingShells=new Dictionary<long,RectTransform>();
 RectTransform railWorld;CombatRailTrack track,routeMap;TMP_Text routeMapLabel;
 RectTransform TrainPiece(string id,string artName,Rect bounds,float x,float width,string label){
  var texture=SecondArt(artName);if(texture==null)throw new System.InvalidOperationException("Missing selected train art "+artName);
  float height=width*bounds.height/bounds.width;
  var piece=Rect(id,railWorld,x,230-height,width,height);
  var shadow=Rect("WheelGround",piece,2,height-3,width-4,9).gameObject.AddComponent<CombatGroundShadow>();shadow.raycastTarget=false;shadow.color=new Color(0,0,0,.7f);
  float sourceWidth=artName.Contains("cargo")?1918:1774,sourceHeight=artName.Contains("cargo")?820:887;
  var art=Raw("Body",piece,texture,0,0,width,height);art.uvRect=new Rect(bounds.x/sourceWidth,1-(bounds.y+bounds.height)/sourceHeight,bounds.width/sourceWidth,bounds.height/sourceHeight);
  art.color=Color.white;
  // Selected identity and type remain in the model and piece name; no debug labels on the body.
  return piece;
 }
 void BuildRailWorld(JObject rail,JObject state){
  railWorld=Rect("SelectedRailConsist",exteriorViewport,0,0,720,273);railWorld.SetSiblingIndex(trainLayer.transform.GetSiblingIndex()+1);
  track=Rect("ActualRouteTrack",railWorld,0,0,720,273).gameObject.AddComponent<CombatRailTrack>();track.raycastTarget=false;
  consist.Add(TrainPiece("Locomotive","train_locomotive-v03",new Rect(20,140,1734,567),548,144,null));
  foreach(var c in CombatRailWorldLayout.BuildConsist((JArray)rail["carriages"])){
   string type=(string)c["type"];bool cargo=type=="Cargo";
   string label=type=="Combat"?"전투":type=="Gun"?"포격":type=="Defense"?"방어":type=="Medical"?"의무":"화물";
   consist.Add(TrainPiece((string)c["id"],cargo?"train_carriage_cargo-v03":"train_carriage_armored-v03",cargo?new Rect(32,112,1854,606):new Rect(16,191,1742,484),(float)c["x"],120,label));
  }
  int gun=0;var gunSlots=((JArray)rail["carriages"]).Where(c=>(string)c["type"]=="Gun").Select(c=>(int)c["slot"]+1).ToArray();
  foreach(var m in mounts){var actor=((JArray)state["actors"]).FirstOrDefault(a=>(string)a["id"]==m.ActorId);int node=(bool?)actor?["train"]==true?0:consist.FindIndex(piece=>piece.name==m.ActorId);if(node<0){m.Image.gameObject.SetActive(false);continue;}m.Image.transform.SetParent(consist[node],false);m.Image.rectTransform.localScale=Vector3.one*.48f;m.Home=new Vector2(consist[node].sizeDelta.x*.43f,-8);}
  trainLayer.gameObject.SetActive(false);distantRail.gameObject.SetActive(false);trainDamage.transform.SetParent(consist[0],false);trainDamage.rectTransform.anchoredPosition=Vector2.zero;trainDamage.rectTransform.localScale=new Vector3(consist[0].sizeDelta.x/625,consist[0].sizeDelta.y/290,1);
  routeMap=Rect("RouteMap",root,66,1040,180,42).gameObject.AddComponent<CombatRailTrack>();routeMap.Map=true;routeMap.raycastTarget=false;
  routeMapLabel=Text("RouteMapLabel",root,"본선",37,1238,248,18,12);routeMapLabel.gameObject.SetActive(false);
  AddConsistAtmosphere();
 }
 void RefreshRailWorld(JObject state,float travelTime){
  var rail=state["railJourney"] as JObject;if(rail==null||((JArray)rail["carriages"]).Count<2)return;
  if(railWorld==null)BuildRailWorld(rail,state);
  long distance=(long)rail["distanceMm"];var marks=(JArray)rail["routeDistancesMm"];long junction=marks.Count==0?distance:(long)marks.Last;int side=(int)rail["routeSide"];
  track.SetRoute(distance,junction,side,false,0);routeMap.SetRoute(distance,junction,side,(string)rail["phase"]=="Selecting",(int)rail["previewIndex"]);
  var choices=(JArray)rail["confirmed"];routeMapLabel.text=(string)rail["phase"]=="Selecting"?"분기 대기 · 레버로 선택":choices.Count==0?"본선":side<0?"상단 선로 운행":"하단 선로 운행";routeMapLabel.gameObject.name=choices.Count==0?"RouteMapLabel":"RouteMap_"+(string)choices.Last["routeId"];
  for(int i=0;i<consist.Count;i++){var node=consist[i];node.anchoredPosition=new Vector2(i==0?548:428-(i-1)*120,-(230-node.sizeDelta.y+CombatRailWorldLayout.RouteOffsetAt(distance,junction,(620-(i==0?620:488-(i-1)*120))/120f,side))+Mathf.Sin(travelTime*15-i)*.35f);}
  // Existing enemy actors keep their targets/HP. The view places their pursuit behind the right-facing consist.
  foreach(var actor in (JArray)state["actors"]){if(!units.TryGetValue((string)actor["id"],out var unit)||!unit.Vehicle)continue;
   if((bool?)actor["artillery"]==true){RefreshArtilleryUnit(state,actor,unit,distance);continue;}
   float depth=unit.Root.localScale.x;float x=unit.Root.anchoredPosition.x+85*depth;unit.Root.anchoredPosition=new Vector2(720-x-85*depth,unit.Root.anchoredPosition.y);unit.Art.uvRect=new Rect(1,0,-1,1);
   unit.Hp.rectTransform.sizeDelta=new Vector2(Mathf.Max(105,46/depth),7/depth);unit.Hp.color=new Color(1,.46f,.30f);
  }
  RefreshIncomingShells(state);
  RefreshConsistAtmosphere(state,travelTime);
 }
 void RefreshArtilleryUnit(JObject state,JToken actor,Unit unit,long distance){
  var row=((JArray)state["railArtillery"]["encounters"]).First(e=>(string)e["id"]==(string)actor["id"]);
  float x=CombatRailWorldLayout.ArtilleryX(distance,(long)row["startDistanceMm"],(long)row["exposureDistanceMm"]);
  unit.Art.texture=SecondArt("artillery_emplacement-v01");unit.Art.uvRect=new Rect(22f/1536,109f/1024,1500f/1536,727f/1024);unit.Art.rectTransform.sizeDelta=new Vector2(124,60);unit.Art.rectTransform.anchoredPosition=Vector2.zero;
  unit.Root.localScale=Vector3.one;unit.Root.anchoredPosition=new Vector2(x,-174);unit.ProjectedGround=174;unit.Hp.rectTransform.anchoredPosition=new Vector2(8,9);unit.Hp.rectTransform.sizeDelta=new Vector2(108,8);unit.Visibility.alpha=1;unit.Text.text="포대";unit.Text.rectTransform.anchoredPosition=new Vector2(0,-61);unit.Text.rectTransform.sizeDelta=new Vector2(124,19);unit.Text.fontSize=12;
  if(unit.Art.GetComponent<Button>()==null){unit.Art.raycastTarget=true;var button=unit.Art.gameObject.AddComponent<CombatInputButton>();button.targetGraphic=unit.Art;string id=(string)actor["id"];button.onClick.AddListener(()=>{if(!CommandsAvailable)return;if(ConsumeInformationWorldClick())return;driver.SetEnemyFocus(id);Refresh();});}
  var ledge=unit.Root.Find("RockLedge");if(ledge==null){var texture=SecondArt("artillery_ledge-v01");if(texture!=null){var image=Raw("RockLedge",unit.Root,texture,-16,54,156,156*449f/2147);image.uvRect=new Rect(21f/2172,48f/724,2147f/2172,449f/724);image.color=new Color(.61f,.69f,.79f);image.transform.SetAsFirstSibling();}}
  unit.Art.GetComponent<Button>().interactable=!driver.RecoveryRequired&&!(bool)state["paused"]&&(int)actor["hp"]>0;
 }
 void RefreshIncomingShells(JObject state){
  if(state["railArtillery"] is not JObject artillery)return;var live=new HashSet<long>();
  foreach(var shot in (JArray)artillery["projectiles"]){long id=(long)shot["sequence"];live.Add(id);if(!incomingShells.TryGetValue(id,out var shell)){shell=Box("IncomingShell_"+id,exteriorViewport,0,0,5,5,new Color(1,.6f,.24f)).rectTransform;incomingShells.Add(id,shell);}
   float progress=Mathf.Clamp01(((long)state["tick"]-(long)shot["launchedTick"])/(float)((long)shot["dueTick"]-(long)shot["launchedTick"]));
   var row=((JArray)artillery["encounters"]).First(e=>(string)e["id"]==(string)shot["source"]);float origin=CombatRailWorldLayout.ArtilleryX((long)state["railJourney"]["distanceMm"],(long)row["startDistanceMm"],(long)row["exposureDistanceMm"])+12;
   shell.anchoredPosition=new Vector2(Mathf.Lerp(origin,620,progress),-Mathf.Lerp(125,208,progress)+Mathf.Sin(progress*Mathf.PI)*34);
  }
  foreach(var id in incomingShells.Keys.Where(id=>!live.Contains(id)).ToArray()){DisposeVisual(incomingShells[id].gameObject);incomingShells.Remove(id);}
 }
}
}

