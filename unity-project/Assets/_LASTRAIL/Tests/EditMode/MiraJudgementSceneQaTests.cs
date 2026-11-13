#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastRail.Tests.EditMode {
 // QA-only transparent wire overlay. Owns its camera/lines, never authority state.
 public sealed class MiraJudgementSceneQaTests {
  const string Folder="Assets/_LASTRAIL/QA/MiraJudgementV1";
  const string Output="D:/Project/LASTRAIL/QA/MiraGeometry-20261015/NativeScene";
  readonly List<Material> materials=new List<Material>();
  static Vector3 P(JToken a)=>new Vector3((float)a[0]/1000f,(float)a[1]/1000f,(float)a[2]/1000f);
  static JObject Data(string n)=>JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"_LASTRAIL/Data/Combat/"+n+".json")));
  Material ColorMaterial(string id,Color color){
   string path=Folder+"/qa-"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null){var shader=Shader.Find("Unlit/Color");Assert.That(shader,Is.Not.Null);m=new Material(shader){color=color};AssetDatabase.CreateAsset(m,path);}
   Assert.That(m.color,Is.EqualTo(color));materials.Add(m);return m;
  }
  void Line(Transform parent,string id,IEnumerable<Vector3> vertices,Material material,bool loop=false,float width=.025f){
   var go=new GameObject(id);go.transform.SetParent(parent,false);var l=go.AddComponent<LineRenderer>();l.useWorldSpace=false;l.positionCount=vertices.Count();l.SetPositions(vertices.ToArray());l.loop=loop;l.widthMultiplier=width;l.sharedMaterial=material;
  }
  void Box(Transform parent,string id,Vector3 center,Vector3 size,Material m){
   var half=size*.5f;
   foreach(int axis in new[]{0,1,2})foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1}){
    var lo=new Vector3();var hi=new Vector3();lo[axis]=-half[axis];hi[axis]=half[axis];lo[(axis+1)%3]=hi[(axis+1)%3]=a*half[(axis+1)%3];lo[(axis+2)%3]=hi[(axis+2)%3]=b*half[(axis+2)%3];
    Line(parent,id+"-edge-"+axis+"-"+a+"-"+b,new[]{center+lo,center+hi},m);
   }
  }
  void Capsule(Transform parent,JToken row,Material m){
   var foot=P(row["feetMm"]);float r=(float)row["radiusMm"]/1000f,h=(float)row["heightMm"]/1000f;string id=(string)row["id"];
   foreach(float y in new[]{r,h-r})Line(parent,id+"-ring-"+y,Enumerable.Range(0,48).Select(i=>foot+new Vector3(Mathf.Cos(i*Mathf.PI/24)*r,y,Mathf.Sin(i*Mathf.PI/24)*r)),m,true);
   foreach(float a in new[]{0,Mathf.PI/2,Mathf.PI,3*Mathf.PI/2}){
    var side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*r;
    Line(parent,id+"-side-"+a,new[]{foot+side+Vector3.up*r,foot+side+Vector3.up*(h-r)},m);
   }
   // Orthogonal semicircle arcs show the actual hemispheres, not a rectangle proxy.
   foreach(bool upper in new[]{false,true})foreach(bool xPlane in new[]{false,true}){
    var c=foot+Vector3.up*(upper?h-r:r);
    Line(parent,id+"-cap-"+upper+"-"+xPlane,Enumerable.Range(0,25).Select(i=>{
     float a=i*Mathf.PI/24;float side=Mathf.Cos(a)*r;float y=Mathf.Sin(a)*r*(upper?1:-1);
     return c+new Vector3(xPlane?side:0,y,xPlane?0:side);
    }),m);
   }
  }
  byte[] Render(Camera camera,RenderTexture rt,string name){
   var old=RenderTexture.active;Texture2D png=null;
   try{camera.Render();RenderTexture.active=rt;png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);png.Apply();var bytes=png.EncodeToPNG();File.WriteAllBytes(Output+"/"+name+".png",bytes);return bytes;}
   finally{RenderTexture.active=old;if(png!=null)UnityEngine.Object.DestroyImmediate(png);}
  }
  public static void ExportIsolated(){
   var args=Environment.GetCommandLineArgs();
   if(!Application.isBatchMode || !args.Contains("-executeMethod") || args.Contains("-runTests"))
    throw new InvalidOperationException("Exporter requires its own dedicated batch executeMethod process, never a test runner or user editor.");
   new MiraJudgementSceneQaTests().BuildOwnedScene();
  }
  void BuildOwnedScene(){
   Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null),"Run this QA with -WithGraphics.");
   Directory.CreateDirectory(Folder);Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   RenderTexture rt=null;Camera camera=null;
   try{
    var p=Data("MiraJudgementProfileV1");var l=Data("MiraJudgementLayoutV1");
    var a=MiraJudgementSceneAdapter.Create(p,BDirectAttackInput.Digest(p),l,BDirectAttackInput.Digest(l));a.AddApprovedTestBox("FIRST_TEST_APPROVED_BOX",4000,3000);
    var before=a.Observe(0,1);var snapshot=a.CaptureNativeState(0,1);
    string prefabPath=Folder+"/MiraApprovedJudgementV1.prefab";
    PrefabUtility.SaveAsPrefabAsset(a.gameObject,prefabPath,out bool saved);Assert.That(saved,Is.True);
    var reloaded=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),scene);
    try{Assert.That(JToken.DeepEquals(reloaded.GetComponent<MiraJudgementSceneAdapter>().CaptureNativeState(0,1),snapshot),Is.True);}
    finally{UnityEngine.Object.DestroyImmediate(reloaded);}
    var overlay=new GameObject("QA_WireOverlayOnly_NOT_ART_OR_GAMEPLAY");var cyan=ColorMaterial("body",Color.cyan);var yellow=ColorMaterial("muzzle-box",Color.yellow);var green=ColorMaterial("scout",Color.green);var orange=ColorMaterial("gunner",new Color(1,.5f,0));var white=ColorMaterial("roof",Color.white);var magenta=ColorMaterial("path",Color.magenta);
    foreach(var box in (JArray)before["blockers"])Box(overlay.transform,(string)box["id"],P(box["centerMm"]),P(box["sizeMm"]),(string)box["id"]=="FIRST_TEST_APPROVED_BOX"?yellow:cyan);
    foreach(var row in (JArray)l["cars"])Box(overlay.transform,"roof-"+(string)row["id"],P(row["centerMm"])+new Vector3(0,2.4f,0),new Vector3(11.6f,0,2.8f),white);
    foreach(var t in (JArray)before["targets"])Capsule(overlay.transform,t,(string)t["definitionId"]=="enemy_raider_scout"?green:orange);
    var muzzle=P(before["source"]["muzzleMm"]);Box(overlay.transform,"judgement-muzzle",muzzle,Vector3.one*.12f,yellow);
    Line(overlay.transform,"feet-to-muzzle",new[]{P(before["source"]["feetMm"]),muzzle},yellow);
    Line(overlay.transform,"frozen-ray-reference",new[]{muzzle,P(before["targets"][0]["anchorMm"])},magenta);
    camera=new GameObject("QA_camera_NOT_GAMEPLAY").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=10;camera.transform.position=new Vector3(15,17,-16);camera.transform.LookAt(new Vector3(0,2,3));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.055f);rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;
    var text=new GameObject("QA_dynamic_caption");text.transform.SetParent(overlay.transform,false);var caption=text.AddComponent<TextMesh>();caption.font=UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");caption.GetComponent<Renderer>().sharedMaterial=caption.font.material;caption.fontSize=48;caption.characterSize=.08f;caption.anchor=TextAnchor.UpperLeft;caption.color=Color.white;caption.text="APPROVED MIRA JUDGEMENT MODEL / NOT FINAL ART\nNative colliders + frozen muzzle ray / HP & fire binding incomplete\nCyan: carriage  White: roof  Green/Orange: target capsule  Yellow: muzzle/test box";caption.transform.position=camera.ViewportToWorldPoint(new Vector3(.03f,.95f,12));caption.transform.rotation=camera.transform.rotation;
    foreach(var line in overlay.GetComponentsInChildren<LineRenderer>())for(int i=0;i<line.positionCount;i++){var v=camera.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(i)));Assert.That(v.z,Is.GreaterThan(0));Assert.That(v.x,Is.InRange(0f,1f),line.name);Assert.That(v.y,Is.InRange(0f,1f),line.name);}
    var on=Render(camera,rt,"approved-overlay-on");overlay.SetActive(false);var off=Render(camera,rt,"approved-overlay-off");
    Assert.That(on.SequenceEqual(off),Is.False);Assert.That(JToken.DeepEquals(a.Observe(0,1),before),Is.True);
    overlay.SetActive(true);camera.targetTexture=null;Assert.That(EditorSceneManager.SaveScene(scene,Folder+"/MiraApprovedJudgementV1.unity"),Is.True);camera.targetTexture=rt;
    var opened=EditorSceneManager.OpenScene(Folder+"/MiraApprovedJudgementV1.unity",OpenSceneMode.Single);
    var loadedAdapter=opened.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MiraJudgementSceneAdapter>(true)).Single();
    Assert.That(JToken.DeepEquals(loadedAdapter.CaptureNativeState(0,1),snapshot),Is.True,"Saved scene reload must preserve native state.");
    File.WriteAllText(Output+"/native-state.json",snapshot.ToString());
    File.WriteAllText(Output+"/scene-check.json",new JObject{["scene"]=Folder+"/MiraApprovedJudgementV1.unity",["prefab"]=prefabPath,["observationHash"]=BDirectAttackInput.Digest(before),["nativeSnapshotHash"]=BDirectAttackInput.Digest(snapshot),["width"]=1920,["height"]=1080,["graphics"]=SystemInfo.graphicsDeviceType.ToString(),["gpu"]=SystemInfo.graphicsDeviceName,["prefabReloadEqual"]=true,["sceneReloadEqual"]=true,["overlayChangesAuthority"]=false,["scope"]="ISOLATED_APPROVED_JUDGEMENT_NOT_NATIVE_HP_OR_OPERATING_SCENE"}.ToString());
   }
   finally{if(camera!=null)camera.targetTexture=null;if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}materials.Clear();}
  }
 }
}
#endif