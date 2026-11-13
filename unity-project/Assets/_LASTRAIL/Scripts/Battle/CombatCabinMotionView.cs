using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 float captureFigureScale=-1;
 public float CabinFigureScale=>Mathf.Clamp(captureFigureScale>0?captureFigureScale:assets.CabinFigureScale,.75f,1);
 bool HasApprovedIdle(string id)=>assets!=null&&assets.OperatorFrames!=null&&assets.OperatorFrames.Find(id)?.HasIdle==true;
 public bool HasOperatorRigTarget(string id)=>units.ContainsKey(id)&&!HasApprovedIdle(id);
 CombatMultipartRig captureKyleRig;
 readonly Dictionary<string,CombatMultipartRig> captureRigs=new Dictionary<string,CombatMultipartRig>();
 readonly Dictionary<string,float> captureRigHpY=new Dictionary<string,float>();
 readonly HashSet<string> downRigActors=new HashSet<string>();
 bool captureRigMotion;
 public void EnableCaptureRigMotion(){captureRigMotion=true;}
 public float CaptureRigExtension=>captureKyleRig==null?0:captureKyleRig.Extension;
 public float CaptureRigGuard=>captureKyleRig==null?0:captureKyleRig.Guard;
 public double CaptureRigBuildMs=>captureRigs.Values.Sum(r=>r.BuildMilliseconds);
 public double CaptureRigPoseUs=>captureRigs.Values.Sum(r=>r.MeanPoseMicroseconds);
 public void SetCaptureCabinSpread(){
  var positions=CombatFormationLayout.Positions(driver.HudState);
  foreach(var unit in units.Values){if(unit.Enemy||unit.Vehicle)continue;unit.Home=positions[unit.Visual];}
  Refresh();
 }
 void RefreshCaptureRig(Unit unit,float windup,float seconds,int hp){
  if(HasApprovedIdle(unit.Visual))return;
  if(!captureRigs.TryGetValue(unit.Visual,out var rig)||!captureRigMotion)return;
  if(hp<=0&&downRigActors.Add(unit.Visual))rig.BeginDown();if(hp>0)downRigActors.Remove(unit.Visual);
  float release=Mathf.Clamp01((unit.AttackUntil-seconds)/.22f);
  rig.SetExtension(hp<=0?0:release>0?Mathf.SmoothStep(0,1,release):Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1,windup)));
  float guard=0;foreach(var cast in (Newtonsoft.Json.Linq.JArray)driver.HudState["casts"])if((string)cast["actor"]==unit.Visual){
   float due=(long)cast["due"]*.05f;if((string)cast["status"]=="CASTING"){float duration=(long)cast["windupTicks"]*.05f;guard=Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-(due-duration))/Mathf.Min(.14f,Mathf.Max(.05f,duration))));}else if((string)cast["status"]=="APPLIED")guard=Mathf.Max(guard,Mathf.SmoothStep(0,1,Mathf.Clamp01((due+.14f-seconds)/.14f)));
  }rig.SetGuard(hp>0?guard:0);
  var actor=((Newtonsoft.Json.Linq.JArray)driver.HudState["actors"]).Single(a=>(string)a["id"]==unit.Visual);
  float down=0;if(hp<=0){var hits=((Newtonsoft.Json.Linq.JArray)driver.HudState["hits"]).Where(h=>(string)h["Target"]==unit.Visual&&(int)h["HpLoss"]>0);float hitAt=hits.Select(h=>(long)h["Tick"]*.05f).DefaultIfEmpty(seconds-.35f).Max();down=Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-hitAt)/.35f));}
  else if((long)actor["recoverAt"]>0)down=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-(long)actor["recoverAt"]*.05f)/.30f));
  rig.SetDown(down,hp>0);rig.SetTint(unit.Art.color);if(captureRigHpY.TryGetValue(unit.Visual,out float hpY))unit.Hp.rectTransform.anchoredPosition=new Vector2(22,hpY);
 }
 public void SetCaptureKyleRig(Texture2D texture,Newtonsoft.Json.Linq.JObject data,float extension){if(HasApprovedIdle("op_kyle"))return;SetCaptureOperatorRig("op_kyle",texture,data,false);captureKyleRig.SetExtension(extension);}
 public void SetCaptureOperatorRig(string id,Texture2D texture,Newtonsoft.Json.Linq.JObject data,bool fitHealthBar=true){
  if(HasApprovedIdle(id))return;
  if(captureRigs.ContainsKey(id))throw new System.InvalidOperationException("Duplicate diagnostic rig");
  if(id!="op_kyle"&&(data["rigHeight"]==null||data["solePoint"]==null||data["attackAngles"]==null))throw new System.ArgumentException("Source-authored rig extent and poses required for "+id);
  var unit=units[id];unit.Art.gameObject.SetActive(false);var node=Rect(id+"PartsDiagnostic",unit.Root,0,0,0,0);
  float height=(float?)data["rigHeight"]??430;var sole=data["solePoint"];float footX=sole==null?220:(float)sole[0],footY=sole==null?475:(float)sole[1];float scale=155f/height;
  node.anchoredPosition=new Vector2(75-footX*scale,-160+footY*scale);node.localScale=Vector3.one*scale;
  var rig=node.gameObject.AddComponent<CombatMultipartRig>();rig.Configure(texture,data,assets.OperatorRigs!=null||System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-rigAlphaCache")>=0);captureRigs.Add(id,rig);captureKyleRig=rig;
  if(fitHealthBar){float top=float.NegativeInfinity;var corners=new Vector3[4];
   for(int pose=0;pose<3;pose++){rig.SetExtension(pose==1?1:0);rig.SetGuard(pose==2?1:0);foreach(var part in rig.GetComponentsInChildren<CombatClippedPart>()){part.rectTransform.GetWorldCorners(corners);foreach(var point in corners)top=Mathf.Max(top,unit.Root.InverseTransformPoint(point).y);}}
   captureRigHpY[id]=top+10;rig.SetExtension(0);rig.SetGuard(0);
  }
 }
 public Newtonsoft.Json.Linq.JArray CaptureRigStates()=>new Newtonsoft.Json.Linq.JArray(captureRigs.Select(p=>new Newtonsoft.Json.Linq.JObject{["actor"]=p.Key,["extension"]=p.Value.Extension,["guard"]=p.Value.Guard,["down"]=p.Value.Down,["tint"]=new Newtonsoft.Json.Linq.JArray(p.Value.Tint.r,p.Value.Tint.g,p.Value.Tint.b,p.Value.Tint.a),["hasSkillPose"]=p.Value.HasSkillPose,["triangles"]=p.Value.TriangleCount}));
 public int CaptureRigTriangles=>captureRigs.Values.Sum(r=>r.TriangleCount);
 public void SetCaptureFigureScale(float scale){if(float.IsNaN(scale)||scale<.75f||scale>1)throw new System.ArgumentOutOfRangeException(nameof(scale));captureFigureScale=scale;Refresh();}
 void ScaleCabinUnit(Unit unit){
  if(unit.Vehicle)return;float scale=CabinFigureScale;
  // Keep the original support anchor at local(75,-160), including the ground shadow and HP panel.
  float aspect=unit.Root.parent.parent.name=="CabinContent"?447f/367:1;unit.Root.localScale=new Vector3(scale,scale*aspect,scale);
  unit.Root.anchoredPosition+=new Vector2(75*(1-scale),-160*(1-scale*aspect));
 }
 static float SupportX(string id,int cell){
  float[] x=id=="op_kyle"?new[]{.484375f,.393651f,.476526f,.508333f,.103509f}:id=="op_reina"?new[]{.463867f,.421875f,.53125f,.479492f,.462891f}:id=="op_eve"?new[]{.487305f,.442383f,.40918f,.5f,.490234f}:id=="op_sera"?new[]{.523438f,.460938f,.56543f,.546875f,.62207f}:id=="op_mira"?new[]{.372381f,.584821f,.402466f,.777273f,.584821f}:new[]{.50293f,.543945f,.474609f,.546875f,.481445f};return x[Mathf.Clamp(cell,0,4)];
 }
 void CabinPoseLean(Unit unit,int cell,float angle){
  if(unit.Vehicle||HasApprovedIdle(unit.Visual))return;var rect=unit.Art.rectTransform;
  var support=new Vector2(rect.sizeDelta.x*SupportX(unit.Visual,cell)*rect.localScale.x,-rect.sizeDelta.y*assets.FootBottom(unit.Visual,cell));
  rect.anchoredPosition=CombatCabinPoseGeometry.AnchorForRotation(rect.anchoredPosition,support,angle);rect.localRotation=Quaternion.Euler(0,0,angle);
  var bar=unit.Hp.rectTransform;bar.anchoredPosition=new Vector2(22,-(unit.Visual=="op_sera"?17.2f:unit.Visual=="op_mira"?8.4f:2));
 }
}
public static class CombatCabinPoseGeometry {
 public static Vector2 AnchorForRotation(Vector2 baseline,Vector2 support,float degrees){float a=degrees*Mathf.Deg2Rad;var turned=new Vector2(support.x*Mathf.Cos(a)-support.y*Mathf.Sin(a),support.x*Mathf.Sin(a)+support.y*Mathf.Cos(a));return baseline+support-turned;}
}
}
