using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
 public sealed partial class ApprovedCombatView {
  // Approved D frames the train on the left of a 1600 x 900 display.
  const float OrdinaryFieldScale = .6f;
  const float OrdinaryWheelLine = 490f;
  OrdinaryRoofPresentation ordinaryRoof;
  RawImage ordinaryRoofSurface;
  readonly Dictionary<string,RawImage> ordinaryBasicShotVisuals=new Dictionary<string,RawImage>(StringComparer.Ordinal);
  RectTransform BindOrdinaryEnvironment(Texture2D refinery,Texture2D viaduct){
   if(ordinaryRoofSurface==null&&localCarArt==null)throw new InvalidOperationException("Bind the physical roof before its field.");
   if(refinery==null||viaduct==null||refinery.width!=1672||refinery.height!=941||viaduct.width!=1672||viaduct.height!=941)
    throw new InvalidOperationException("Complete registered 848 environment layers required.");
   const float railPixel=480;
   var size=CombatLandscapeDLayout.DesignSize;
   float scale=Mathf.Max(size.x/refinery.width, OrdinaryWheelLine/railPixel, (size.y-OrdinaryWheelLine)/(refinery.height-railPixel));
   float w=size.x/(refinery.width*scale), h=size.y/(refinery.height*scale);
   var uv=new Rect((1-w)/2,1-h-(railPixel-OrdinaryWheelLine/scale)/refinery.height,w,h);
   var field=Rect("OrdinaryEnvironment",root,0,0,size.x,size.y);
   field.SetSiblingIndex(exteriorViewport.GetSiblingIndex());
   Raw("OrdinaryFieldBackground",field,refinery,0,0,size.x,size.y).uvRect=uv;
   Raw("OrdinaryFieldTrack",field,viaduct,0,0,size.x,size.y).uvRect=uv;
   mapLayer.gameObject.SetActive(false);
   RefreshCanyonVisibility(false);RefreshPursuitVisibility(false);
   travelGround.gameObject.SetActive(false);travelForeground.gameObject.SetActive(false);distantRail.gameObject.SetActive(false);
   return field;
  }
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
  internal void UseComparisonField(Texture2D refinery, Texture2D viaduct, Texture2D enemyEngine){
   var field=BindOrdinaryEnvironment(refinery,viaduct);
   const float railPixel=480;
   var size=CombatLandscapeDLayout.DesignSize;
   float scale=size.x/(refinery.width*field.GetChild(0).GetComponent<RawImage>().uvRect.width);
   // The emplacement follows the shelf crop. Its visible alpha box, in source pixels, sets its size.
   float gunHeight=102*scale, gunWidth=gunHeight*1499/725;
   float gunX=size.x/2+(1285-refinery.width/2f)*scale;
   float gunBottom=OrdinaryWheelLine+(337-railPixel)*scale;
   var emplacement=SecondArt("artillery_emplacement-v01");
   if(!emplacement)throw new InvalidOperationException("Comparison emplacement texture is missing.");
   Raw("ComparisonEmplacement",field,emplacement,gunX-gunWidth/2,gunBottom-gunHeight,gunWidth,gunHeight).uvRect=new Rect(23f/1536,110f/1024,1499f/1536,725f/1024);
   field.Find("ComparisonEmplacement").SetSiblingIndex(1);
   // Show only the inward nose on each edge of the playable field, sharing one texture.
   void Engine(bool right,float exposed){
    float cover=Mathf.Max(size.x/refinery.width,size.y/refinery.height);
    float width=exposed*cover,height=132*cover;
    float nose=1744f/enemyEngine.width;
    float sample=exposed*656/(132*enemyEngine.width);
    Raw(right?"ComparisonRightEngine":"ComparisonLeftEngine",field,enemyEngine,right?size.x-width:0,OrdinaryWheelLine-height,width,height).uvRect=
     new Rect(right?nose:nose-sample,116f/enemyEngine.height,right?-sample:sample,656f/enemyEngine.height);
   }
   Engine(false,163);Engine(true,124);
   // Visible body bounds in the roof texture, and D shield clearances as fractions of visible body width.
   var hull=ordinaryRoofSurface.rectTransform;
   float originalHullWidth=1520*OrdinaryFieldScale;
   float bodyWidth=originalHullWidth*(502f/729.6f);
   float bodyLeft=originalHullWidth*(114f/729.6f);
   float wheelY=-(1-ordinaryRoof.WheelViewportY)*hull.rect.height;
   electromagnetic.BindComparisonHull(hull,new Rect(bodyLeft-bodyWidth*27/397,
     wheelY+bodyWidth*21/397,bodyWidth*432/397,bodyWidth*47/397));
  }
#endif
  void EnsureOrdinaryRoof(JObject state){
   if(localCarArt!=null){RefreshLocalFiveCarField(state);return;}
   if(ordinaryRoof!=null||landscapeLayout==null||!(state["ordinaryRail"] is JObject rail))return;
   ordinaryRoof=new OrdinaryRoofPresentation(assets.OrdinaryRoof??LastRail.Resources.PreparedContent.Load<OrdinaryRoofPresentationAssets>("CombatPresentation/OrdinaryRoof"),rail);
   var texture = ordinaryRoof.Texture;
   float displayHeight = texture.height * OrdinaryFieldScale;
   float top = OrdinaryWheelLine - (1f - ordinaryRoof.WheelViewportY) * displayHeight;
   ordinaryRoofSurface = Raw("OrdinaryPhysicalRoof", root, texture, 80, top, texture.width * OrdinaryFieldScale, displayHeight);
   ordinaryRoofSurface.transform.SetSiblingIndex(actorLayer.GetSiblingIndex());
   distantRail.PlaceAcrossSideView(root, OrdinaryWheelLine, ordinaryRoofSurface.transform);
   contactShadow.gameObject.SetActive(false);
   contactDust.gameObject.SetActive(false);
   trainLayer.gameObject.SetActive(false);trainDamage.gameObject.SetActive(false);
   foreach(var mount in mounts)mount.Image.gameObject.SetActive(false);
   if(assets.OrdinaryFieldBackground!=null||assets.OrdinaryFieldTrack!=null)
    BindOrdinaryEnvironment(assets.OrdinaryFieldBackground,assets.OrdinaryFieldTrack);
  }
  bool PlaceOrdinaryRoofActor(JToken row,Unit unit,int pose){
   if(localCarArt!=null)return PlaceLocalFiveCarActor(row,unit,pose);
   if(ordinaryRoof==null||unit.Vehicle||unit.Enemy&&row["footRelativeToFrontMm"]==null)return false;
   var foot=row["footRelativeToFrontMm"] as JArray;var point=foot==null?ordinaryRoof.Project(row):ordinaryRoof.ProjectRelativeToFront((double)foot[0],(double)foot[1],(double)foot[2]);var rect=ordinaryRoofSurface.rectTransform;
   var destination=rect.TransformPoint(new Vector2(point.x*rect.rect.width,-(1-point.y)*rect.rect.height));
   if(unit.Enemy){
    unit.Root.localScale=Vector3.one;unit.Root.position=destination;unit.Shadow.gameObject.SetActive(false);unit.Art.enabled=false;
    var head=ordinaryRoof.ProjectRelativeToFront((double)foot[0],(double)foot[1]+((long?)row["hitHeightMm"]??ordinaryRoof.GunnerHeightMm((int)row["slot"])),(double)foot[2]);
    Vector3 headWorld=rect.TransformPoint(new Vector2(head.x*rect.rect.width,-(1-head.y)*rect.rect.height));float height=unit.Root.InverseTransformPoint(headWorld).y;
    unit.Hp.rectTransform.anchoredPosition=new Vector2(-14,height+10);unit.Hp.rectTransform.sizeDelta=new Vector2(28,7);
    if(unit.Fallback!=null){unit.Fallback.text=(bool?)row["boarding"]==true?((string)row["tauntedBy"]=="op_kyle"?"도발":"승차병"):(int)row["slot"]==0?"사수 · 로컬 판정체":"";unit.Fallback.fontSize=12;unit.Fallback.rectTransform.anchoredPosition=new Vector2(-40,height+30);unit.Fallback.rectTransform.sizeDelta=new Vector2(130,20);}
    return true;
   }
   unit.Root.localScale=Vector3.one*(.7f * OrdinaryFieldScale);
   Vector3 sole;
   if(HasApprovedIdle(unit.Visual)||captureRigs.ContainsKey(unit.Visual))sole=unit.Root.TransformPoint(new Vector2(75,-160));
   else{
    if(assets.ActorAtlas(unit.Visual)==null)throw new InvalidOperationException("Ordinary roof actor requires its inspected sole metadata.");
    var art=unit.Art.rectTransform;sole=art.TransformPoint(new Vector2(art.rect.width*SupportX(unit.Visual,pose),-art.rect.height*assets.FootBottom(unit.Visual,pose)));
   }
   unit.Root.position+=destination-sole;
   unit.Shadow.gameObject.SetActive(false);
   return true;
  }
  RectTransform ordinaryGunnerShotLayer;
  sealed class GunnerShotVisual { internal Image Mark;internal TMPro.TMP_Text Status; }
  readonly Dictionary<string,GunnerShotVisual> ordinaryGunnerShotVisuals=new Dictionary<string,GunnerShotVisual>(StringComparer.Ordinal);
  Vector2 OrdinaryShotPoint(double x,double y,double z,double front){var uv=ordinaryRoof.ProjectRelativeToFront(x-front,y,z);var r=ordinaryRoofSurface.rectTransform;return ordinaryGunnerShotLayer.InverseTransformPoint(r.TransformPoint(new Vector2(uv.x*r.rect.width,-(1-uv.y)*r.rect.height)));}
  void RefreshOrdinaryRoofEnemies(JObject state){
   if(ordinaryRoof==null)return;ordinaryRoof.RefreshEnemies((JArray)state["actors"]);
   var shots=state["ordinaryGunnerProjectiles"] as JArray;
   if(ordinaryGunnerShotLayer==null){ordinaryGunnerShotLayer=Rect("ActualGunnerProjectiles",root,0,0,root.rect.width,root.rect.height);ordinaryGunnerShotLayer.SetSiblingIndex(actorLayer.GetSiblingIndex()+1);}
   var live=new HashSet<string>(StringComparer.Ordinal);long tick=(long)state["tick"];double front=(double)state["ordinaryRail"]["frontArcMm"];
   if(shots!=null)foreach(JObject shot in shots){
    string status=(string)shot["status"];long launch=(long)shot["launchTick"],contact=(long)shot["contactTick"];
    // Original gunner policy fixes the launch-world endpoints and arrival tick. Never chase a moving target visually.
    bool flying=status=="FLYING";if(!flying&&(status!="HIT"||tick-contact>Math.Ceiling(2000000d/(long)state["stepTimeSpanTicks"])))continue;
    string id=(string)shot["projectileId"];live.Add(id);
    if(!ordinaryGunnerShotVisuals.TryGetValue(id,out var visual)){visual=new GunnerShotVisual{Mark=Box("OriginalShot",ordinaryGunnerShotLayer,0,0,12,4,new Color(1,.85f,.35f)),Status=Text("OriginalArrival",ordinaryGunnerShotLayer,"",0,0,100,20,12)};visual.Mark.rectTransform.pivot=new Vector2(.5f,.5f);ordinaryGunnerShotVisuals.Add(id,visual);}
    var start=(JArray)shot["muzzleMm"];var end=(JArray)shot["targetPointMm"];if(contact<=launch)throw new InvalidOperationException("Original projectile arrival must follow launch.");
    double t=flying?(double)(tick-launch)/(contact-launch):1;var xyz=Enumerable.Range(0,3).Select(i=>(double)start[i]+((double)end[i]-(double)start[i])*t).ToArray();
    var point=OrdinaryShotPoint(xyz[0],xyz[1],xyz[2],front);var a=OrdinaryShotPoint((double)start[0],(double)start[1],(double)start[2],front);var b=OrdinaryShotPoint((double)end[0],(double)end[1],(double)end[2],front);
    bool visible=point.x>=0&&point.x<=root.rect.width&&point.y<=0&&point.y>=-root.rect.height;
    visual.Mark.gameObject.SetActive(visible);visual.Mark.rectTransform.anchoredPosition=point;visual.Mark.rectTransform.sizeDelta=flying?new Vector2(12,4):new Vector2(9,9);visual.Mark.rectTransform.localRotation=Quaternion.Euler(0,0,flying?Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg:45);
    visual.Status.gameObject.SetActive(visible&&!flying);visual.Status.rectTransform.anchoredPosition=point+new Vector2(-50,27);visual.Status.text="고정 도착";
   }
   foreach(string id in ordinaryGunnerShotVisuals.Keys.Where(id=>!live.Contains(id)).ToArray()){var visual=ordinaryGunnerShotVisuals[id];DisposeVisual(visual.Mark.gameObject);DisposeVisual(visual.Status.gameObject);ordinaryGunnerShotVisuals.Remove(id);}
   var basicLive=new HashSet<string>(StringComparer.Ordinal);
   if(state["ordinaryBasicProjectiles"] is JArray basics)foreach(var shot in basics){
    var texture=assets.OperatorFrames?.Find((string)shot["actorId"])?.BasicProjectile;if(texture==null)continue;
    string id=(string)shot["projectileId"];basicLive.Add(id);if(!ordinaryBasicShotVisuals.TryGetValue(id,out var mark)){mark=Raw("ApprovedBasicProjectile",ordinaryGunnerShotLayer,texture,0,0,48,48f*texture.height/texture.width);mark.rectTransform.pivot=new Vector2(.5f,.5f);mark.raycastTarget=false;ordinaryBasicShotVisuals.Add(id,mark);}
    Vector2 Project(JToken p)=>OrdinaryShotPoint((double)p[0],(double)p[1],(double)p[2],front);
    var point=Project(shot["pointMm"]);var direction=Project(shot["anchorMm"])-Project(shot["originMm"]);mark.rectTransform.anchoredPosition=point;mark.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);mark.gameObject.SetActive(point.x>=0&&point.x<=root.rect.width&&point.y<=0&&point.y>=-root.rect.height);
   }
   foreach(var id in ordinaryBasicShotVisuals.Keys.Where(id=>!basicLive.Contains(id)).ToArray()){DisposeVisual(ordinaryBasicShotVisuals[id].gameObject);ordinaryBasicShotVisuals.Remove(id);}
  }
  void ReleaseOrdinaryRoof(){ordinaryRoof?.Dispose();ordinaryRoof=null;if(ordinaryRoofSurface!=null)ordinaryRoofSurface.texture=null;}
 }
}
