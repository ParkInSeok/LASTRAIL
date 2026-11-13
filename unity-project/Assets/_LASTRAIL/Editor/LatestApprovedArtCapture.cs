#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class LatestApprovedArtCapture {
 const string Qa="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/";
 const int Width=1320,Height=1500,CellWidth=440,RowHeight=300;
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly Type UnitType=typeof(ApprovedCombatView).GetNestedType("Unit",BindingFlags.NonPublic);
 static readonly MethodInfo Apply=typeof(ApprovedCombatView).GetMethod("ApplyApprovedIdle",Private);
 static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){
  var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
  r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
 }
 static void Set(object unit,string field,object value)=>UnitType.GetField(field).SetValue(unit,value);
 static T Get<T>(object unit,string field)=>(T)UnitType.GetField(field).GetValue(unit);
 static object Unit(Transform parent,string id,string visual,bool right,float x,float y,Texture2D texture){
  var u=Activator.CreateInstance(UnitType,true);var body=Rect(id,parent,x,y,150,155);
  var art=Rect("Source",body,0,0,150,155).gameObject.AddComponent<RawImage>();art.texture=texture;art.raycastTarget=false;
  var hp=Rect("Hp",body,0,0,1,1).gameObject.AddComponent<CombatMeter>();hp.gameObject.SetActive(false);
  Set(u,"Root",body);Set(u,"Art",art);Set(u,"Hp",hp);Set(u,"Visual",visual);Set(u,"FacesRight",right);return u;
 }
 static void Render(ApprovedCombatView view,object u,double age,JObject state,JArray casts){
  Check((bool)Apply.Invoke(view,new object[]{u,age,state,(long)age,casts,1d}),"Approved source consumer refused input");
  var art=Get<RawImage>(u,"Art");var size=art.rectTransform.sizeDelta;
  var position=art.rectTransform.anchoredPosition;
  Check(art.texture!=null&&float.IsFinite(size.x)&&float.IsFinite(size.y)&&size.x>0&&size.y>0&&float.IsFinite(position.x)&&float.IsFinite(position.y),"Missing texture or invalid registered rectangle");
  Check(art.rectTransform.localScale==Vector3.one&&art.uvRect==new UnityEngine.Rect(0,0,1,1),"Unexpected mirroring/cropping");
 }
 static void Label(Transform parent,string value,float x,float y){var t=Rect("Label",parent,x,y,CellWidth-12,34).gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=14;t.color=Color.white;t.text=value;t.raycastTarget=false;}
 public static void Run(){
  Directory.CreateDirectory(Qa+"visual");var records=new JArray();GameObject canvasGo=null,cameraGo=null;RenderTexture target=null;var previous=RenderTexture.active;
  try{
   var config=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>("Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset");Check(config!=null&&config.OperatorFrames!=null,"Current runtime config missing");config.OperatorFrames.Validate();
   var clips=(JArray)JObject.Parse(File.ReadAllText(Qa+"characters.json"))["clips"];
   cameraGo=new GameObject("ApprovedArtCaptureCamera",typeof(Camera));var camera=cameraGo.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.09f,.11f);camera.cullingMask=1<<31;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
   target=new RenderTexture(Width,Height,24);camera.targetTexture=target;
   for(int offset=0;offset<clips.Count;offset+=5){
    canvasGo=new GameObject("ApprovedArtCapture",typeof(RectTransform),typeof(Canvas));var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    var view=canvasGo.AddComponent<ApprovedCombatView>();typeof(ApprovedCombatView).GetField("assets",Private).SetValue(view,config);var units=(IDictionary)typeof(ApprovedCombatView).GetField("units",Private).GetValue(view);
    for(int row=0;row<5&&offset+row<clips.Count;row++){
     var clip=clips[offset+row];string id=(string)clip["operatorId"],role=(string)clip["role"];bool right=(string)clip["direction"]=="right";var entry=config.OperatorFrames.Find(id,right);Check(entry!=null&&entry.FacesRight==right,"Requested direction absent: "+id);
     var frames=role=="idle"?entry.IdleFrames:role=="basic"?entry.BasicFrames:role=="active"?entry.ActiveFrames:entry.UltimateFrames;
     var durations=role=="idle"?entry.DurationsMs:role=="basic"?entry.BasicDurationsMs:role=="active"?entry.ActiveDurationsMs:entry.UltimateDurationsMs;
     Check(frames.Length==clip["frames"].Count()&&frames.Length>0,"Registry clip count differs: "+id+"/"+role);
     int total=durations.Sum();int[] indices={0,frames.Length/2,frames.Length-1};
     for(int col=0;col<3;col++){
      int index=indices[col];double age=durations.Take(index).Sum()+Math.Max(0,durations[index]/2);float x=col*CellWidth,y=row*RowHeight;string actorId="sample-"+(offset+row)+"-"+col,targetId=actorId+"-target";
      var u=Unit(canvasGo.transform,actorId,id,right,x+(right?60:230),y+70,entry.IdleFrames[0]);var recipient=Unit(canvasGo.transform,targetId,"op_kyle",!right,x+(right?230:60),y+85,config.OperatorFrames.Find("op_kyle").IdleFrames[0]);
      Get<RawImage>(recipient,"Art").color=new Color(.55f,.55f,.55f,.5f);Get<RectTransform>(recipient,"Root").localScale=Vector3.one*.7f;units.Add(actorId,u);units.Add(targetId,recipient);
      var state=new JObject{["id"]=actorId,["hp"]=100,["basicStatus"]=role=="basic"?"PREPARING":"IDLE",["attackTarget"]=targetId,["basicStartedAt"]=0,["basicCommitAt"]=durations.Take(entry.BasicFireFrame).Sum(),["recoverAt"]=total};
      var casts=new JArray();if(role=="active"||role=="ultimate")casts.Add(new JObject{["id"]=actorId+"-cast",["actor"]=actorId,["ability"]=id+"."+role,["status"]="APPLIED",["due"]=0,["windupTicks"]=0,["target"]=targetId});
      Render(view,u,age,state,casts);var art=Get<RawImage>(u,"Art");Check(art.texture==frames[index],"Consumer chose different source exposure: "+id+"/"+role+"/"+index);
      var effect=Get<RawImage>(u,"ActionEffect");bool effectVisible=effect!=null&&effect.gameObject.activeSelf;var sera=Get<CombatSeraCrystalView>(u,"SeraCrystals");
      if(role=="active"||role=="ultimate"){
       Render(view,u,total+10000,state,casts);Check(Get<RawImage>(u,"ActionEffect")==null||!Get<RawImage>(u,"ActionEffect").gameObject.activeSelf,"Effect survived end");
       casts[0]["status"]="CANCELLED";Render(view,u,age,state,casts);Check(entry.IdleFrames.Contains(art.texture),"Cancelled action still displayed");Check(Get<RawImage>(u,"ActionEffect")==null||!Get<RawImage>(u,"ActionEffect").gameObject.activeSelf,"Effect survived cancel");casts[0]["status"]="APPLIED";
      }
      state["hp"]=0;Render(view,u,age,state,casts);Check(entry.IdleFrames.Contains(art.texture),"Dead actor retained action");Check(sera==null||!sera.Visible,"Dead actor retained Sera FX");state["hp"]=100;Render(view,u,age,state,casts);
      Label(canvasGo.transform,id+" "+(right?"R":"L")+" "+role+" f"+index+" @"+age+"ms",x+8,y+8);
      records.Add(new JObject{["operatorId"]=id,["direction"]=right?"right":"left",["role"]=role,["sourceFrame"]=index,["elapsedMs"]=age,["asset"]=AssetDatabase.GetAssetPath(art.texture),["page"]=offset/5,["row"]=row,["column"]=col,["bodyHeight"]=155,["width"]=art.rectTransform.sizeDelta.x,["height"]=art.rectTransform.sizeDelta.y,["independentEffectVisible"]=effectVisible,["seraFxVisible"]=sera!=null&&sera.Visible,["sourceAndLifecycleAssertions"]=true});
     }
    }
    foreach(var child in canvasGo.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
    Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);
    try{pixels.ReadPixels(new UnityEngine.Rect(0,0,Width,Height),0,0);pixels.Apply();File.WriteAllBytes(Qa+"visual/approved-clips-page-"+(offset/5)+".png",pixels.EncodeToPNG());}finally{Object.DestroyImmediate(pixels);}
    Object.DestroyImmediate(canvasGo);canvasGo=null;
   }
   File.WriteAllText(Qa+"visual/capture.json",new JObject{["scope"]="Actual ApplyApprovedIdle consumer with local presentation-only input; sampled exposures, not full-game or temporal playback validation",["clipCount"]=clips.Count,["samples"]=records,["result"]="PASS"}.ToString());
  }catch(Exception e){File.WriteAllText(Qa+"visual/failure.txt",e.ToString());throw;}
  finally{RenderTexture.active=previous;if(canvasGo!=null)Object.DestroyImmediate(canvasGo);if(cameraGo!=null)Object.DestroyImmediate(cameraGo);if(target!=null){target.Release();Object.DestroyImmediate(target);}}
 }
}
#endif
