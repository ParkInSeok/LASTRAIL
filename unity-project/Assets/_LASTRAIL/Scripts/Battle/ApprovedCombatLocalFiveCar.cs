using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
 public sealed partial class ApprovedCombatView {
  Texture2D[] localCarArt;
  RectTransform localCarField;
  RawImage[] localCars;
  readonly System.Collections.Generic.Dictionary<string,Image> localPartyMarkers=new System.Collections.Generic.Dictionary<string,Image>();
  TMPro.TMP_Text localPartyStatus;
  float localFocus=-38500,localScale=.017f;
  // Explicit local preview only: caller owns these textures and their candidate/approval manifest.
  public void ConfigureLocalFiveCarField(Texture2D[] engineToRear){
   if(root!=null||engineToRear==null||engineToRear.Length!=6||engineToRear.Any(t=>t==null))throw new ArgumentException("Six independent local car textures before Bind required.");
   localCarArt=(Texture2D[])engineToRear.Clone();
  }
  Vector2 LocalCarPoint(double x,double y,double z)=>new Vector2(800+(float)(x-localFocus)*localScale,-490+(float)y*localScale+(float)z*localScale*.4f);
  void RefreshLocalFiveCarField(JObject state){
   if(localCarArt==null||landscapeLayout==null)return;
   if(((JArray)state["ordinaryRail"]?["physicalInstanceIds"])?.Count!=6)throw new InvalidOperationException("Local six-car art requires six native physical hosts.");
   if(localCarField==null){
    localCarField=Rect("LocalSixCarField",root,0,0,1600,900);localCarField.SetSiblingIndex(actorLayer.GetSiblingIndex());
    trainLayer.gameObject.SetActive(false);trainDamage.gameObject.SetActive(false);contactShadow.gameObject.SetActive(false);contactDust.gameObject.SetActive(false);
    foreach(var mount in mounts)mount.Image.gameObject.SetActive(false);
    BindOrdinaryEnvironment(assets.OrdinaryFieldBackground,assets.OrdinaryFieldTrack);
    localCars=new RawImage[6];for(int i=5;i>=0;i--)localCars[i]=Raw("PhysicalCar_"+i,localCarField,localCarArt[i],0,0,1,1);
    localPartyStatus=Text("LocalPartyReviewStatus",root,"",30,80,1500,45,18);
   }
   var party=state["localParty"] as JObject;
   bool moving=!string.IsNullOrEmpty((string)party?["phase"])&&(string)party["phase"]!="Complete"&&(string)party["phase"]!="Cancelled";
   bool overview=moving||string.IsNullOrEmpty((string)party?["phase"]);
   var actors=((JArray)state["actors"]).Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]&&a["footRelativeToFrontMm"] is JArray).ToArray();
   float targetFocus=overview?-38500:actors.Length==5?(float)actors.Average(a=>(double)a["footRelativeToFrontMm"][0]):-38500;
   float targetScale=overview?.017f:.045f;
   localFocus=Mathf.Lerp(localFocus,targetFocus,1-Mathf.Exp(-Time.unscaledDeltaTime*3));localScale=Mathf.Lerp(localScale,targetScale,1-Mathf.Exp(-Time.unscaledDeltaTime*3));
   for(int i=0;i<6;i++){
    var point=LocalCarPoint(-6000-i*13000,3213,0);float width=12000*localScale,height=width*localCarArt[i].height/localCarArt[i].width;
    var r=localCars[i].rectTransform;r.anchoredPosition=new Vector2(point.x-width/2,point.y+height*.13f);r.sizeDelta=new Vector2(width,height);
   }
   localPartyStatus.text="독립 로컬 검수 · 기관차 + 5객차 · 이동 2m/s(검토값)"+(party==null?"":" · "+(string)party["phase"])+ (moving?" · 이동 동작 원화 미등록: 위치 표식 표시":"");
  }
  bool PlaceLocalFiveCarActor(JToken row,Unit unit,int pose){
   if(localCarField==null||unit.Vehicle||!(row["footRelativeToFrontMm"] is JArray foot))return false;
   var p=LocalCarPoint((double)foot[0],(double)foot[1],(double)foot[2]);
   bool moving=(bool?)row["partyMoving"]==true;
   if(!(bool)row["enemy"]){
    string id=(string)row["id"];
    if(!localPartyMarkers.TryGetValue(id,out var marker)){
     marker=Box("LocalMovementMarker_"+id,localCarField,0,0,20,20,new Color(.08f,.25f,.3f,.95f));
     var number=Text("FormationNumber",marker.transform,((int)row["slot"]+1).ToString(),0,0,20,20,14);number.alignment=TMPro.TextAlignmentOptions.Center;localPartyMarkers.Add(id,marker);
    }
    marker.rectTransform.anchoredPosition=new Vector2(p.x-10,p.y+10);marker.gameObject.SetActive(moving);
   }
   unit.Root.localScale=Vector3.one*(localScale/.045f*.6f);
   var destination=localCarField.TransformPoint(new Vector3(p.x,p.y,0));
   var sole=unit.Root.TransformPoint(new Vector2(75,-160));unit.Root.position+=destination-sole;unit.Shadow.gameObject.SetActive(false);
   if(moving){unit.Art.enabled=false;if(unit.Emblem!=null)unit.Emblem.enabled=false;unit.Text.text="이동 "+((int)row["slot"]+1);}
   else{unit.Art.enabled=true;if(unit.Emblem!=null)unit.Emblem.enabled=true;}
   return true;
  }
 }
}
