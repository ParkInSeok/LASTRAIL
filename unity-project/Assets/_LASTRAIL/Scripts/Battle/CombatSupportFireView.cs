using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Rendering only. Missile trajectories converge on one real reserved/receipt
// target; three visual projectiles never cause three simulation hits.
public sealed class CombatSupportFireView:MonoBehaviour {
 RectTransform train,ghost,mount,barrels,fxRoot,hatchRoot;
 RawImage leftHatch,rightHatch;readonly RawImage[] missiles=new RawImage[3],exhaust=new RawImage[3],ignition=new RawImage[3],impacts=new RawImage[3],haze=new RawImage[3];
 readonly RawImage[,] trails=new RawImage[3,8];
 readonly RawImage[] hitCore=new RawImage[3];
 CombatRigAtlasCache.Lease cache;CombatSupportFireAssets assets;CombatSupportFireTimeline timeline;
 Vector2[] sockets;Vector2 lastTarget;string targetId;bool hasTarget;
 public JObject Frame{get;private set;}
 public JArray FlightPoints{get;private set;}=new JArray();
 public int ImpactReceipts=>received.Count;readonly HashSet<string> received=new HashSet<string>();
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 RawImage Image(string name,Transform parent,Texture texture,Material material,Vector4 crop,Vector2 pivot){var r=Rect(name,parent,crop.x-pivot.x,crop.y-pivot.y,crop.z,crop.w);var image=r.gameObject.AddComponent<RawImage>();image.texture=texture;image.material=material;image.raycastTarget=false;image.uvRect=new Rect(crop.x/1536,1-(crop.y+crop.w)/1024,crop.z/1536,crop.w/1024);return image;}
 RawImage Fx(string name,string item){var metadata=JObject.Parse(assets.FxMetadata.text);var row=((JArray)metadata["items"]).Single(x=>(string)x["id"]==item);var p=row["sourceRectTopLeftPx"];var raw=Image(name,fxRoot,assets.Effects,assets.Additive,new Vector4((float)p[0],(float)p[1],(float)p[2],(float)p[3]),Vector2.zero);raw.rectTransform.pivot=new Vector2(.5f,.5f);raw.gameObject.SetActive(false);return raw;}
 public void Bind(RectTransform trainLayer,RectTransform exteriorActors,CombatSupportFireAssets data,JObject hud){
  train=trainLayer;assets=data;timeline=new CombatSupportFireTimeline(hud);cache=CombatRigAtlasCache.Acquire(data.Hardware,JObject.Parse(data.Masks.text));
  ghost=Rect("SupportLauncherBehindRoof",train.parent,0,0,625,290);ghost.SetSiblingIndex(train.GetSiblingIndex());mount=Rect("Elevator",ghost,0,0,0,0);mount.localScale=Vector3.one*data.NativeScale;
  Image("LauncherBase",mount,cache.Texture,cache.Material,new Vector4(35,290,500,210),new Vector2(280,458));barrels=Rect("RecoilingBarrels",mount,0,0,0,0);Image("Barrels",barrels,cache.Texture,cache.Material,new Vector4(35,30,500,284),new Vector2(280,458));
  hatchRoot=Rect("SupportRoofHatch",train,0,0,0,0);var left=Rect("LeftHinge",hatchRoot,-40,0,0,0);var right=Rect("RightHinge",hatchRoot,40,0,0,0);
  leftHatch=Image("HatchLeaf",left,cache.Texture,cache.Material,new Vector4(580,190,380,280),new Vector2(720,431));rightHatch=Image("HatchLeaf",right,cache.Texture,cache.Material,new Vector4(580,190,380,280),new Vector2(720,431));
  fxRoot=Rect("SupportMissileEffects",exteriorActors,0,0,720,1280);
  var meta=JObject.Parse(data.HardwareMetadata.text);sockets=((JArray)meta["launcherSocketsSourceTopLeftPx"]).Select(p=>new Vector2((float)p[0]-280,458-(float)p[1])).ToArray();
  for(int i=0;i<3;i++){
   missiles[i]=Image("Missile"+i,fxRoot,cache.Texture,cache.Material,new Vector4(1045,210,465,140),Vector2.zero);missiles[i].rectTransform.pivot=new Vector2((1491-1045)/465f,1-(273-210)/140f);missiles[i].rectTransform.sizeDelta=new Vector2(24,24*140f/465);
   exhaust[i]=Fx("Exhaust"+i,"exhaust");exhaust[i].rectTransform.pivot=new Vector2(48/512f,1-245/512f);ignition[i]=Fx("Ignition"+i,"ignition");impacts[i]=Fx("Impact"+i,"impact_fire");haze[i]=Fx("Haze"+i,"impact_haze");for(int j=0;j<8;j++)trails[i,j]=Fx("Trail"+i+"_"+j,"trail_haze");hitCore[i]=Fx("WhiteHotCore"+i,"ignition");
  }
 }
 static Vector2 Curve(Vector2 a,Vector2 b,float t){var p=a+new Vector2(90,31);var q=b+new Vector2(-45,70);float u=1-t;return u*u*u*a+3*u*u*t*p+3*u*t*t*q+t*t*t*b;}
 static void Place(RawImage image,Vector2 p,float width,float height,float angle,float alpha){image.gameObject.SetActive(alpha>0);image.rectTransform.anchoredPosition=p;image.rectTransform.sizeDelta=new Vector2(width,height);image.rectTransform.localRotation=Quaternion.Euler(0,0,angle);image.color=new Color(1,1,1,Mathf.Clamp01(alpha));}
 public void Refresh(JObject hud,Func<string,Vector3?> targetWorld,bool simplified){
  Frame=timeline.Read(hud,simplified);bool active=(bool)Frame["active"];ghost.gameObject.SetActive(active);hatchRoot.gameObject.SetActive(active);fxRoot.gameObject.SetActive(active);FlightPoints=new JArray();if(!active)return;
  // Actor depth sorting runs every frame. Resolved flashes must be above the
  // recipient artwork, otherwise its opaque sprite erases the impact core.
  fxRoot.SetAsLastSibling();
  ghost.anchoredPosition=train.anchoredPosition;ghost.localRotation=train.localRotation;ghost.localScale=train.localScale;
  var roof=new Vector2(train.rect.width*assets.RoofAnchor.x,-train.rect.height*assets.RoofAnchor.y);float rise=(float)Frame["raise"];
  mount.anchoredPosition=roof-new Vector2(0,180*(1-rise));hatchRoot.anchoredPosition=roof;float open=Mathf.Max(.04f,(float)Frame["hatch"]);
  leftHatch.transform.parent.localScale=new Vector3(-.23f,.23f*open,1);rightHatch.transform.parent.localScale=new Vector3(.23f,.23f*open,1);barrels.anchoredPosition=new Vector2(-2.5f*(float)Frame["recoil"],0);
  string id=(string)Frame["target"];if(id!=targetId){targetId=id;hasTarget=false;}var world=id==null?null:targetWorld(id);if(world.HasValue){lastTarget=fxRoot.InverseTransformPoint(world.Value);hasTarget=true;}
  bool impact=(bool)Frame["impact"]&&hasTarget;if(impact)received.Add((string)Frame["command"]);
  for(int i=0;i<3;i++){
   Vector2 start=fxRoot.InverseTransformPoint(barrels.TransformPoint(sockets[i]));float p=(float)Frame["rockets"][i];bool fly=hasTarget&&p>=0;var point=fly?Curve(start,lastTarget,p):start;var tangent=fly?(Curve(start,lastTarget,Mathf.Min(1,p+.001f))-Curve(start,lastTarget,Mathf.Max(0,p-.001f))).normalized:Vector2.right;float angle=Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg;
   Place(missiles[i],point,24,24*140f/465,angle,fly?1:0);Place(exhaust[i],point-tangent*(425f/465*24),36,18,angle+180,fly?.85f:0);Place(ignition[i],start,22,22,0,fly&&p<.3f?(1-p/.3f)*.8f:0);
   float age=(float)Frame["impactAge"]-i*.06f,fade=impact&&age>=0?Mathf.Clamp01((.55f-age)/.41f):0;float size=(simplified?60:110)*(.7f+.3f*Mathf.Clamp01(age/.08f));var offset=simplified?Vector2.zero:new Vector2((i-1)*9,i==1?7:0);
   float shown=simplified&&i>0?0:1;Place(impacts[i],lastTarget+offset,size,size,0,fade*shown);Place(haze[i],lastTarget+offset+new Vector2(-age*8,age*18),size*1.1f,size*1.1f,0,fade*.35f*shown);
   for(int j=0;j<8;j++){float t=(fly?p:1)-(j+1)*.045f;float opacity=!simplified&&hasTarget&&t>0?(fly?.6f:impact?.6f*Mathf.Clamp01(1-(float)Frame["impactAge"]/.3f):0)*(1-j/9f):0;var tail=Curve(start,lastTarget,Mathf.Clamp01(t));var direction=Curve(start,lastTarget,Mathf.Clamp01(t+.01f))-tail;Place(trails[i,j],tail,35+j*.7f,20+j*.6f,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg,opacity);}
   float hot=impact&&age>=0?Mathf.Clamp01((.28f-age)/.16f)*shown:0;float core=simplified?30:55;Place(hitCore[i],lastTarget+offset,core,core,0,hot);
   FlightPoints.Add(new JObject{["index"]=i,["visible"]=fly,["progress"]=p,["source"]=new JArray(start.x,start.y),["position"]=new JArray(point.x,point.y),["target"]=new JArray(lastTarget.x,lastTarget.y)});
  }
 }
 static void ReleaseObject(GameObject go){if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
 void OnDestroy(){if(ghost!=null)ReleaseObject(ghost.gameObject);if(hatchRoot!=null)ReleaseObject(hatchRoot.gameObject);if(fxRoot!=null)ReleaseObject(fxRoot.gameObject);CombatRigAtlasCache.Release(cache);}
}
}
