#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 // QA View borrows read-only native callbacks. No HP, clock or command ownership.
 internal static class BOrdinaryGeometrySceneQa {
  internal static string OutputFolder=>Probe.OutputFolder;
  internal static Probe Open(BDirectAttackBasicLoop owner,BOrdinaryGeometryBinding geometry)=>new Probe(owner,geometry);
  internal sealed class Probe:IDisposable {
      internal static string RunLabel {
    get { var label=Environment.GetEnvironmentVariable("LASTRAIL_GEOMETRY_SCENE_QA_RUN");if(string.IsNullOrEmpty(label))return null;
     if(label.Length>64||label.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'))throw new ArgumentException("Invalid scene QA run label");return label; }
   }
   internal static string OutputFolder=>RunLabel==null?"D:/Project/LASTRAIL/QA/CombatBMigration-20261004/OrdinaryGeometry/successor-native-v02":"D:/Project/LASTRAIL/QA/CombatBMigration-20261004/"+RunLabel;
   static string AssetFolder=>RunLabel==null?"Assets/_LASTRAIL/QA/CombatBGeometry":"Assets/_LASTRAIL/QA/CombatBGeometry/"+RunLabel;
   string Folder=>OutputFolder;
   Func<JObject> observe;Func<int> hp;Func<long> nativeTick;
   readonly UnityEngine.SceneManagement.Scene prior,scene;
   readonly Dictionary<string,GameObject> groups=new Dictionary<string,GameObject>();
   readonly Dictionary<string,GameObject> markers=new Dictionary<string,GameObject>();
   readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
   readonly Camera camera;readonly TextMesh caption;readonly RenderTexture target;bool disposed;
   static Vector3 Point(JToken p)=>new Vector3((float)p[0]/1000f,(float)p[1]/1000f,(float)p[2]/1000f);
   internal Probe(BDirectAttackBasicLoop owner,BOrdinaryGeometryBinding geometry){
    if(owner==null||geometry==null)throw new ArgumentNullException();
    Assert.AreEqual(geometry.Hash,(string)owner.Capture()["spatialSource"]["geometryHash"]);
    observe=owner.Capture;nativeTick=()=>owner.Tick;hp=()=>owner.Hp("op_kyle");Directory.CreateDirectory(Folder);Directory.CreateDirectory(AssetFolder);prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    if(string.IsNullOrEmpty(prior.path))Assert.IsTrue(EditorSceneManager.SaveScene(prior,AssetFolder+"/qa-runner-origin-v02.unity"));scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    foreach(string name in new[]{"body","roof","paths","actors","routes","projectiles","labels"})groups.Add(name,new GameObject("independent_"+name));
    var asset=geometry.Definition.Capture();int count=((JArray)geometry.Capture()["instances"]).Count;
    for(int slot=0;slot<count;slot++){
     foreach(string group in new[]{"body","roof"}){string lo=group=="body"?"bodyMinMm":"roofMinMm",hi=group=="body"?"bodyMaxMm":"roofMaxMm";var a=Point(geometry.WorldPoint(slot,(JArray)asset[lo]));var b=Point(geometry.WorldPoint(slot,(JArray)asset[hi]));Box(group+"_"+slot,group,(a+b)*.5f,new Vector3(Mathf.Abs(a.x-b.x),Mathf.Max(.03f,Mathf.Abs(a.y-b.y)),Mathf.Abs(a.z-b.z)),group=="body"?new Color(.12f,.15f,.2f):new Color(.3f,.36f,.44f));}
     foreach(string side in new[]{"left","right"})foreach(string path in new[]{"approachMm","climbMm"})Line(slot+"_"+side+"_"+path,"paths",((JArray)asset[side][path]).Select(p=>Point(geometry.WorldPoint(slot,(JArray)p))).ToArray(),side=="left"?Color.cyan:Color.magenta);
    }
    foreach(var bridge in (JArray)geometry.Capture()["connections"])Line("bridge_"+(string)bridge["fromSlot"],"paths",((JArray)bridge["bridgeMm"]).Select(Point).ToArray(),Color.white);
    camera=new GameObject("native_owner_QA_camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=22;camera.transform.position=new Vector3(10,32,-32);camera.transform.LookAt(new Vector3(0,1,0));camera.backgroundColor=new Color(.04f,.05f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;target=new RenderTexture(1280,720,24);camera.targetTexture=target;
    caption=new GameObject("dynamic_native_status").AddComponent<TextMesh>();caption.transform.SetParent(groups["labels"].transform);caption.font=UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");caption.GetComponent<Renderer>().sharedMaterial=caption.font.material;caption.anchor=TextAnchor.UpperLeft;caption.fontSize=48;caption.characterSize=.25f;caption.color=Color.white;caption.transform.position=new Vector3(-22,7,0);caption.transform.rotation=camera.transform.rotation;
   }
   Material Material(string key,Color color){if(materials.TryGetValue(key,out var m))return m;m=new Material(Shader.Find("Unlit/Color")){color=color};materials.Add(key,m);return m;}
   GameObject Box(string id,string group,Vector3 point,Vector3 scale,Color color){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.name=id;o.transform.SetParent(groups[group].transform);o.transform.position=point;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=Material(group=="actors"?(color==Color.green?"operator":"enemy"):group,color);return o;}
   void Line(string name,string group,Vector3[] points,Color color){var o=new GameObject(name);o.transform.SetParent(groups[group].transform);var line=o.AddComponent<LineRenderer>();line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=.08f;line.sharedMaterial=Material(group=="routes"?"route":name,color);}
   void Marker(string id,string group,Vector3 point,Color color){if(!markers.TryGetValue(id,out var o)){o=Box(id,group,point,group=="actors"?new Vector3(.4f,.7f,.4f):new Vector3(.5f,.5f,.5f),color);markers.Add(id,o);}o.SetActive(true);o.transform.position=point;}
   internal void Show(string stage,bool save){
    if(disposed)throw new ObjectDisposedException(nameof(Probe));var snapshot=observe();var space=snapshot["spatialSource"];long tick=(long)space["tick"];Assert.AreEqual(nativeTick(),tick);
    foreach(var marker in markers.Values)marker.SetActive(false);
    foreach(Transform route in groups["routes"].transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(route.gameObject);
    foreach(var op in space["operators"])Marker((string)op["id"]+"_native","actors",Point(op["positionMm"])+Vector3.up*.35f,Color.green);
    foreach(var enemy in space["positions"].Where(p=>(string)p["phase"]!="Dead")){string id=(string)enemy["id"];Marker(id+"_native","actors",Point(enemy["positionMm"])+Vector3.up*.35f,Color.red);markers[id+"_native"].name=id+"_"+(string)enemy["phase"]+"_g"+(long)enemy["generation"];if(((JArray)enemy["routeMm"]).Count>1)Line("route_"+id,"routes",((JArray)enemy["routeMm"]).Select(Point).ToArray(),Color.yellow);}
    var shots=(JArray)snapshot["gunnerIntercept"]["projectiles"];foreach(var shot in shots.Where(p=>(string)p["terminal"]=="FLYING")){Assert.NotNull(shot["sourcePointMm"],"Consume immutable launch endpoints");long begin=(long)shot["launchTick"],end=(long)shot["contactTick"];float t=Mathf.Clamp01((float)(tick-begin)/(end-begin));Marker((string)shot["id"],"projectiles",Vector3.Lerp(Point(shot["sourcePointMm"]),Point(shot["targetPointMm"]),t),new Color(1,.6f,.1f));}
    caption.text="TEST MODEL / LIVE NATIVE OWNER\n"+stage+"  tick "+tick+"  revision "+(long)space["revision"]+"\nKyle HP "+hp()+"   Flying "+shots.Count(p=>(string)p["terminal"]=="FLYING")+" / terminal "+shots.Count(p=>(string)p["terminal"]!="FLYING")+"\n"+string.Join("  ",space["positions"].Where(p=>(string)p["phase"]!="Dead").GroupBy(p=>(string)p["phase"]).Select(g=>g.Key+":"+g.Count()));
    PlaceCaption();AssertFramed();if(save){Capture(stage);File.WriteAllText(Folder+"/native-"+stage+".json",snapshot.ToString());if(stage=="flight-mid")SaveScene();}
   }
   void PlaceCaption(){caption.transform.position=camera.ViewportToWorldPoint(new Vector3(.04f,.94f,20));caption.transform.rotation=camera.transform.rotation;}
   void AssertFramed(){foreach(var renderer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled)){var bounds=renderer.localBounds;foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){var v=camera.WorldToViewportPoint(renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));Assert.IsTrue(v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1&&v.z>0,renderer.name+" clipped by QA camera");}}}
   void Capture(string name){var before=RenderTexture.active;Texture2D png=null;try{camera.Render();RenderTexture.active=target;png=new Texture2D(1280,720,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();File.WriteAllBytes(Folder+"/scene-"+name+".png",png.EncodeToPNG());}finally{RenderTexture.active=before;if(png!=null)UnityEngine.Object.DestroyImmediate(png);}}
   internal void AuditIndependentVisibility(){if(disposed)throw new ObjectDisposedException(nameof(Probe));var before=observe();foreach(var pair in groups){pair.Value.SetActive(false);Capture("hide-"+pair.Key);pair.Value.SetActive(true);}foreach(var group in groups)group.Value.SetActive(group.Key=="paths"||group.Key=="labels");Capture("isolated-paths");foreach(var group in groups)group.Value.SetActive(true);var savedPosition=camera.transform.position;var savedRotation=camera.transform.rotation;camera.transform.position=new Vector3(-10,32,32);camera.transform.LookAt(new Vector3(0,1,0));PlaceCaption();AssertFramed();Capture("opposite-side");camera.transform.position=savedPosition;camera.transform.rotation=savedRotation;PlaceCaption();var roof=groups["roof"].GetComponentInChildren<Renderer>();var previous=roof.sharedMaterial;roof.sharedMaterial=Material("replacement",Color.blue);Capture("replace-roof");roof.sharedMaterial=previous;Assert.IsTrue(JToken.DeepEquals(before,observe()));File.WriteAllText(Folder+"/independent-visibility.json",new JObject{["nativeHash"]=BDirectAttackInput.Digest(before),["groups"]=new JArray(groups.Keys),["hideReplaceChangesNativeState"]=false}.ToString());}
   void SaveScene(){foreach(var pair in materials){string path=AssetFolder+"/qa-v02-"+pair.Key.Replace(':','-')+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing==null)AssetDatabase.CreateAsset(pair.Value,path);else{existing.CopyPropertiesFromMaterial(pair.Value);foreach(var renderer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).Where(r=>r.sharedMaterial==pair.Value))renderer.sharedMaterial=existing;}}camera.targetTexture=null;Assert.IsTrue(EditorSceneManager.SaveScene(scene,AssetFolder+"/ordinary-b-native-qa-v02.unity"));camera.targetTexture=target;}
   public void Dispose(){if(disposed)return;disposed=true;observe=null;hp=null;nativeTick=null;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.SceneManagement.SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);foreach(var m in materials.Values)if(!AssetDatabase.Contains(m))UnityEngine.Object.DestroyImmediate(m);}
  }
 }
}
#endif