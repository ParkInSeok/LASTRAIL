using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using LastRail.Battle;
public class CombatMultipartRigTests {
 [Test] public void AuthoredAnglesKeepRestOrientationAndReturnAfterSkill(){
  var data=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/rig-exact-union-v01.json"));data["attackAngles"]=new JObject{["upper_arm"]=-35};data["skillAngles"]=new JObject{["upper_arm"]=-80};((JObject)data["parts"][4])["restAngle"]=10;
  var texture=new Texture2D(1536,1024);var go=new GameObject("authored-poses",typeof(RectTransform));
  try{var rig=go.AddComponent<CombatMultipartRig>();rig.Configure(texture,data);var arm=go.transform.Find("torso/upper_arm");rig.SetExtension(1);Assert.Less(Quaternion.Angle(arm.localRotation,Quaternion.Euler(0,0,25)),.001f);rig.SetGuard(1);Assert.Less(Quaternion.Angle(arm.localRotation,Quaternion.Euler(0,0,70)),.001f);rig.SetExtension(0);rig.SetGuard(0);Assert.Less(Quaternion.Angle(arm.localRotation,Quaternion.Euler(0,0,-10)),.001f);}
  finally{Object.DestroyImmediate(go);Object.DestroyImmediate(texture);}
 }
 [Test] public void ExactUnionRigKeepsSixPartsWithOverNinetyPercentFewerTriangles(){
  var data=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/rig-exact-union-v01.json"));var texture=new Texture2D(1536,1024);var go=new GameObject("optimized",typeof(RectTransform));
  try{var rig=go.AddComponent<CombatMultipartRig>();rig.Configure(texture,data);Assert.AreEqual(6,go.GetComponentsInChildren<CombatClippedPart>().Length);Assert.AreEqual(2766,rig.TriangleCount);Assert.Less(rig.TriangleCount,28517*.1f);var foot=go.transform.Find("lower").position;rig.SetExtension(1);rig.SetGuard(1);Assert.AreEqual(foot,go.transform.Find("lower").position);}
  finally{Object.DestroyImmediate(go);Object.DestroyImmediate(texture);}
 }
 [Test] public void OriginalPixelUvsSurviveImporterTextureResize(){
  var data=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/rig-diagnostic-v01.json"));var texture=new Texture2D(2048,1024);var go=new GameObject("resized-source",typeof(RectTransform));
  try{var row=data["parts"][0];var part=go.AddComponent<CombatClippedPart>();part.Configure(texture,row,new Vector2(1536,1024));
   using(var mesh=new VertexHelper()){typeof(CombatClippedPart).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Invoke(part,new object[]{mesh});var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,0);float sourceX=vertex.position.x/(float)row["scale"]+(float)row["pivot"][0];Assert.AreEqual(sourceX/1536,vertex.uv0.x,.000001f);}
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(texture);}
 }
 [Test] public void SeparateArmHierarchyMovesWithoutMovingLowerBodyOrScalingChildSockets(){
  var data=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/UI/Diagnostics/KyleParts/rig-diagnostic-v01.json"));var texture=new Texture2D(1536,1024);var go=new GameObject("parts-diagnostic",typeof(RectTransform));
  try{var rig=go.AddComponent<CombatMultipartRig>();rig.Configure(texture,data);Assert.AreEqual(6,go.GetComponentsInChildren<CombatClippedPart>().Length);Assert.Greater(rig.TriangleCount,0);
   var lower=go.transform.Find("lower");var forearm=go.transform.Find("torso/upper_arm/forearm_knife");var foot=lower.TransformPoint(new Vector3(-50,-220,0));var arm=forearm.position;
   rig.SetExtension(1);Assert.AreEqual(foot,lower.TransformPoint(new Vector3(-50,-220,0)));Assert.Greater(Vector3.Distance(arm,forearm.position),1);Assert.AreEqual(Vector3.one,forearm.localScale);
   rig.SetExtension(0);Assert.Less(Vector3.Distance(arm,forearm.position),.001f);
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(texture);}
 }
}
