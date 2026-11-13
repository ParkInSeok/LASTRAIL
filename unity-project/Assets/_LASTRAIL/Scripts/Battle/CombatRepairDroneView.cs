using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed class CombatRepairDroneView:MonoBehaviour {
 RectTransform root,body,upper,forearm,hatch,door,panel,cabinAnchor;RawImage spark;CombatRepairWeldContact contact;CombatRigAtlasCache.Lease cache;CombatRepairDroneTimeline timeline;
 readonly Dictionary<string,RectTransform> joints=new Dictionary<string,RectTransform>();
 readonly List<CombatRepairRotorBlur> rotors=new List<CombatRepairRotorBlur>();
 readonly Vector2 shoulder=new Vector2(-50,-102),arm=new Vector2(-96.88f,8.12f),tool=new Vector2(-112.56f,-28.28f);
 public JObject Frame{get;private set;}public float ContactError{get;private set;}
 static RectTransform Rect(string name,Transform parent,Vector2 p,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=p;r.sizeDelta=size;return r;}
 public bool Bind(RectTransform cabin,RectTransform actors,JObject hud){var source=LastRail.Resources.PreparedContent.Load<Texture2D>("CombatPresentation/RepairDrone/parts-source-v01");var metadata=LastRail.Resources.PreparedContent.Load<TextAsset>("CombatPresentation/RepairDrone/rig-v01");if(source==null||metadata==null)return false;cache=CombatRigAtlasCache.Acquire(source,JObject.Parse(metadata.text));timeline=new CombatRepairDroneTimeline(hud);cabinAnchor=cabin;
  root=Rect("FriendlyRepairDrone",actors,Vector2.zero,new Vector2(720,447));root.SetAsFirstSibling();root.position=cabin.TransformPoint(Vector3.zero);panel=Rect("CentralRepairContact",cabin,new Vector2(407,-140),Vector2.zero);body=Rect("DroneRoot",root,Vector2.zero,Vector2.zero);body.localScale=Vector3.one*.16f;
  foreach(var p in (JArray)JObject.Parse(metadata.text)["parts"]){string id=(string)p["id"],parent=(string)p["parent"];var a=p["anchorRigUnits"];var joint=Rect(id,id=="hatch"?root:parent==null?body:joints[parent],new Vector2((float)a[0],-(float)a[1]),Vector2.zero);joints[id]=joint;
   var crop=p["sourceRectTopLeftPx"];var pivot=p["pivotSourceTopLeftPx"];float scale=(float)p["sourceScale"];var r=Rect("MaskedArt",joint,new Vector2(((float)crop[0]-(float)pivot[0])*scale,((float)pivot[1]-(float)crop[1])*scale),new Vector2((float)crop[2]*scale,(float)crop[3]*scale));var image=r.gameObject.AddComponent<RawImage>();image.texture=cache.Texture;image.material=cache.Material;image.uvRect=new Rect((float)crop[0]/1536,1-((float)crop[1]+(float)crop[3])/1024,(float)crop[2]/1536,(float)crop[3]/1024);image.raycastTarget=false;}
  upper=joints["upper_arm"];forearm=joints["forearm_tool"];hatch=joints["hatch"];hatch.anchoredPosition=new Vector2(85,-82);hatch.localScale=Vector3.one*.115f;
  var bay=Rect("DarkCompartment",hatch,new Vector2(39,88),new Vector2(180,200)).gameObject.AddComponent<Image>();bay.color=new Color(.005f,.012f,.015f);bay.raycastTarget=false;door=Rect("InnerDoorFace",hatch,new Vector2(39,88),new Vector2(180,200));var doorImage=door.gameObject.AddComponent<RawImage>();doorImage.texture=cache.Texture;doorImage.material=cache.Material;doorImage.uvRect=new Rect(1210/1536f,1-865/1024f,180/1536f,200/1024f);doorImage.raycastTarget=false;
  var rSpark=Rect("ActualWeldSpark",root,Vector2.zero,new Vector2(24,24));rSpark.pivot=new Vector2(.5f,.5f);spark=rSpark.gameObject.AddComponent<RawImage>();spark.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("CombatPresentation/RepairDrone/weld-fx-source-v01");spark.material=LastRail.Resources.PreparedContent.Load<Material>("CombatPresentation/SupportMissile/additive");spark.uvRect=new Rect(1/3f,.5f,1/3f,.5f);spark.raycastTarget=false;
  foreach(var id in new[]{"rotor_left","rotor_right"}){var blur=Rect("RotorBladeBlur",joints[id],new Vector2(id=="rotor_left"?-113.85f:105.6f,14.85f),new Vector2(180,75));blur.pivot=new Vector2(.5f,.5f);var graphic=blur.gameObject.AddComponent<CombatRepairRotorBlur>();graphic.raycastTarget=false;rotors.Add(graphic);}
  contact=Rect("WeldContactLight",root,Vector2.zero,new Vector2(720,447)).gameObject.AddComponent<CombatRepairWeldContact>();contact.raycastTarget=false;
  root.gameObject.SetActive(false);return true;
 }
 static float Ease(float p)=>p*p*(3-2*p);
 static Vector2 Rotate(Vector2 v,float a){float c=Mathf.Cos(a),s=Mathf.Sin(a);return new Vector2(v.x*c-v.y*s,v.x*s+v.y*c);}
 public void Refresh(JObject hud){if(root==null)return;Frame=timeline.Read(hud);root.gameObject.SetActive((bool)Frame["active"]);if(!(bool)Frame["active"])return;root.position=cabinAnchor.TransformPoint(Vector3.zero);float travel=Ease((float)Frame["travel"]),back=Ease((float)Frame["return"]),t=(float)Frame["time"];
  var departure=new Vector2(100,-85);var hover=new Vector2(445,-125);body.anchoredPosition=Vector2.Lerp(Vector2.Lerp(departure,hover,travel),departure,back)+new Vector2(0,Mathf.Sin(t*9)*.7f*(1-back));body.localScale=Vector3.one*(.16f*Mathf.Lerp(.35f,1,Mathf.Min(travel*3,1))*(1-back*.7f));
  var target=(Vector2)body.InverseTransformPoint(panel.position);bool welding=(bool)Frame["weld"];if(!welding)target=shoulder+new Vector2(-135,-60);var d=target-shoulder;float len=Mathf.Clamp(d.magnitude,Mathf.Abs(arm.magnitude-tool.magnitude)+.01f,arm.magnitude+tool.magnitude-.01f);float phi=Mathf.Atan2(d.y,d.x),bend=Mathf.Acos(Mathf.Clamp((arm.sqrMagnitude+len*len-tool.sqrMagnitude)/(2*arm.magnitude*len),-1,1));float angle=phi+bend-Mathf.Atan2(arm.y,arm.x);upper.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);var elbow=shoulder+Rotate(arm,angle);var finalDirection=target-elbow;forearm.localRotation=Quaternion.Euler(0,0,(Mathf.Atan2(finalDirection.y,finalDirection.x)-angle-Mathf.Atan2(tool.y,tool.x))*Mathf.Rad2Deg);
  var tip=forearm.TransformPoint(tool);ContactError=Vector3.Distance(tip,panel.position);spark.gameObject.SetActive(welding&&ContactError<1);spark.rectTransform.position=tip;contact.gameObject.SetActive(welding&&ContactError<1);contact.SetContact(tip,forearm.position,t);spark.color=new Color(1,1,1,.6f+.4f*Mathf.Sin(t*43)*Mathf.Sin(t*43));door.localScale=new Vector3(Mathf.Lerp(1,.08f,Mathf.Min(travel*5,1)*(1-back)),1,1);foreach(var rotor in rotors)rotor.SetTime(t);
 }
 void OnDestroy(){if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);if(panel!=null)UnityEngine.Object.DestroyImmediate(panel.gameObject);CombatRigAtlasCache.Release(cache);}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatRepairWeldContact:MaskableGraphic {
 Vector2 tip,elbow;float time;
 public void SetContact(Vector3 point,Vector3 joint,float clock){tip=rectTransform.InverseTransformPoint(point);elbow=rectTransform.InverseTransformPoint(joint);time=clock;SetVerticesDirty();}
 static void Line(VertexHelper m,Vector2 a,Vector2 b,float width,Color tint){var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=m.currentVertCount;m.AddVert(a-n,tint,Vector2.zero);m.AddVert(a+n,tint,Vector2.zero);m.AddVert(b+n,tint,Vector2.zero);m.AddVert(b-n,tint,Vector2.zero);m.AddTriangle(i,i+1,i+2);m.AddTriangle(i,i+2,i+3);}
 protected override void OnPopulateMesh(VertexHelper m){m.Clear();float flicker=.8f+.2f*Mathf.Sin(time*43)*Mathf.Sin(time*43);Line(m,Vector2.Lerp(tip,elbow,.12f),elbow,1.2f,new Color(.55f,.85f,1,.38f));for(int ray=0;ray<8;ray++){float a=ray*Mathf.PI/4+.13f;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float length=(ray%2==0?8:5)*flicker;Line(m,tip,tip+d*length,2.5f,new Color(1,.58f,.16f,.45f));Line(m,tip,tip+d*length,.8f,new Color(1,.95f,.72f,.95f));float travel=Mathf.Repeat(time*4+ray*.173f,1);Line(m,tip+d*(8+travel*8),tip+d*(10+travel*8),.8f,new Color(1,.68f,.23f,(1-travel)*.8f));}Line(m,tip-Vector2.right*2,tip+Vector2.right*2,3,new Color(1,1,.95f,1));}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatRepairRotorBlur:MaskableGraphic {
 float time;public void SetTime(float value){time=value;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper m){m.Clear();var r=rectTransform.rect;for(int blade=0;blade<4;blade++)for(int trail=0;trail<6;trail++){float angle=time*37+blade*Mathf.PI*.5f-trail*.12f;var p=new Vector2(Mathf.Cos(angle)*r.width*.45f,Mathf.Sin(angle)*r.height*.43f);var n=new Vector2(-p.y,p.x).normalized*4;var tint=new Color(.5f,.85f,.88f,.25f*(1-trail/6f));int i=m.currentVertCount;m.AddVert(p*.14f-n,tint,Vector2.zero);m.AddVert(p*.14f+n,tint,Vector2.zero);m.AddVert(p+n,tint,Vector2.zero);m.AddVert(p-n,tint,Vector2.zero);m.AddTriangle(i,i+1,i+2);m.AddTriangle(i,i+2,i+3);}}
}
public sealed partial class ApprovedCombatView {
 CombatRepairDroneView repairDrone;
 void BindRepairDrone(JObject state){repairDrone=gameObject.AddComponent<CombatRepairDroneView>();if(!repairDrone.Bind(cabinLayer.rectTransform,actorLayer,state)){UnityEngine.Object.DestroyImmediate(repairDrone);repairDrone=null;}}
 void RefreshRepairDrone(JObject state){repairDrone?.Refresh(state);}
}
}
