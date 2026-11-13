using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class BPhysicalSceneSourceTests {
  // Explicit measurement fixture, not published battlefield dimensions.
  [Test] public void MeasuresActualColliderAndAnchorsReusesUnchangedSourceAndOwnsRevision(){
   var type=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.BPhysicalSceneSource");
   Assert.NotNull(type,"Actual scene measurement owner is missing");
   var root=new GameObject("measurement frame");var body=new GameObject("actual collision box");var marker=new GameObject("hit anchor");
   try{
    body.transform.SetParent(root.transform,false);marker.transform.SetParent(body.transform,false);
    var box=body.AddComponent<BoxCollider>();box.size=new Vector3(2,1,4);box.center=new Vector3(0,.5f,0);marker.transform.localPosition=new Vector3(0,1,1);
    var owner=root.AddComponent(type);
    type.GetMethod("Bind").Invoke(owner,new object[]{"fixture-geometry","fixture-explicit-unit",1000d,root.transform,box,new[]{"hit"},new[]{marker.transform}});
    JObject Capture()=>(JObject)type.GetMethod("Capture").Invoke(owner,null);
    var first=Capture();Assert.AreEqual(1,(long)first["sourceRevision"]);Assert.AreEqual(new JArray(0,1000,1000).ToString(),first["anchors"][0]["positionMm"].ToString());
    Assert.AreEqual(8,((JArray)first["bodyCornersMm"]).Count);Assert.AreEqual(-1000,(long)first["bodyCornersMm"][0][0]);
    var immutable=Capture();first["anchors"][0]["positionMm"][0]=999999;
    Assert.IsTrue(JToken.DeepEquals(immutable,Capture()),"Caller mutation cannot alter the retained original");
    root.transform.position=new Vector3(30,40,50);Assert.IsTrue(JToken.DeepEquals(immutable,Capture()),"Rendering/world origin change does not change frame-relative geometry");
    marker.transform.localPosition=new Vector3(0,1,2);var changed=Capture();Assert.AreEqual(2,(long)changed["sourceRevision"]);Assert.AreEqual(2000,(long)changed["anchors"][0]["positionMm"][2]);
    Assert.AreNotEqual((string)immutable["packetHash"],(string)changed["packetHash"]);
    UnityEngine.Object.DestroyImmediate(marker);Assert.Throws<TargetInvocationException>(()=>Capture(),"Missing source must not return a stale successful capture");
   }finally{UnityEngine.Object.DestroyImmediate(root);if(body)UnityEngine.Object.DestroyImmediate(body);if(marker)UnityEngine.Object.DestroyImmediate(marker);}
  }
  [Test] public void RejectsMissingUnitContractAndAnAnchorOutsideItsFrame(){
   var type=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.BPhysicalSceneSource");Assert.NotNull(type);
   var root=new GameObject("frame");var outside=new GameObject("unrelated");
   try{var box=root.AddComponent<BoxCollider>();var owner=root.AddComponent(type);var bind=type.GetMethod("Bind");
    Assert.Throws<TargetInvocationException>(()=>bind.Invoke(owner,new object[]{"fixture","",1000d,root.transform,box,new[]{"hit"},new[]{root.transform}}));
    Assert.Throws<TargetInvocationException>(()=>bind.Invoke(owner,new object[]{"fixture","explicit-unit",0d,root.transform,box,new[]{"hit"},new[]{root.transform}}));
    Assert.Throws<TargetInvocationException>(()=>bind.Invoke(owner,new object[]{"fixture","explicit-unit",1000d,root.transform,box,new[]{"hit"},new[]{outside.transform}}));
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(outside);}
  }
  [Test] public void CapturesNestedRotationScaleAndProtectsBorrowedBindings(){
   var root=new GameObject("frame");var body=new GameObject("nested body");var marker=new GameObject("anchor");
   try{
    body.transform.SetParent(root.transform,false);marker.transform.SetParent(body.transform,false);
    body.transform.localPosition=new Vector3(10,0,0);body.transform.localRotation=Quaternion.Euler(0,90,0);body.transform.localScale=new Vector3(2,1,3);
    var box=body.AddComponent<BoxCollider>();box.size=new Vector3(2,2,2);marker.transform.localPosition=new Vector3(0,0,1);
    var names=new[]{"hit"};var references=new[]{marker.transform};
    var owner=root.AddComponent<BPhysicalSceneSource>();owner.Bind("fixture","fixture-unit",1000,root.transform,box,names,references);
    names[0]="changed";references[0]=root.transform;
    var first=owner.Capture();Assert.AreEqual("hit",(string)first["anchors"][0]["id"]);
    Assert.AreEqual(new JArray(13000,0,0).ToString(),first["anchors"][0]["positionMm"].ToString());
    Assert.AreEqual(new JArray(7000,-1000,2000).ToString(),first["bodyCornersMm"][0].ToString());
    root.transform.position=new Vector3(10000000,10000000,10000000);root.transform.localScale=Vector3.one*2;
    Assert.IsTrue(JToken.DeepEquals(first,owner.Capture()),"Frame-local source is independent of display origin and scale");
    Assert.Throws<InvalidOperationException>(()=>owner.Bind("new","fixture-unit",1000,root.transform,box,new[]{"hit"},new[]{marker.transform}));
    body.transform.localScale=new Vector3(0,1,1);Assert.Throws<InvalidOperationException>(()=>owner.Capture());
    body.transform.localScale=new Vector3(2,1,3);Assert.IsTrue(JToken.DeepEquals(first,owner.Capture()),"Failed capture must not publish a revision");
    marker.transform.localPosition=new Vector3(0,0,2000000);Assert.Throws<InvalidOperationException>(()=>owner.Capture());
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public void NormalizedChangesAndReparentingDoNotPublishStaleOrSpuriousRevisions(){
   var root=new GameObject("frame");var outside=new GameObject("outside");var marker=new GameObject("anchor");
   try{
    marker.transform.SetParent(root.transform,false);
    var box=root.AddComponent<BoxCollider>();var owner=root.AddComponent<BPhysicalSceneSource>();
    owner.Bind("fixture","fixture-unit",1000,root.transform,box,new[]{"hit"},new[]{marker.transform});
    var first=owner.Capture();marker.transform.localPosition=new Vector3(.0001f,0,0);
    Assert.IsTrue(JToken.DeepEquals(first,owner.Capture()),"Submillimeter changes with the same normalized coordinates retain revision");
    marker.transform.SetParent(outside.transform,false);Assert.Throws<InvalidOperationException>(()=>owner.Capture());
    marker.transform.SetParent(root.transform,false);Assert.IsTrue(JToken.DeepEquals(first,owner.Capture()));
    UnityEngine.Object.DestroyImmediate(box);Assert.Throws<InvalidOperationException>(()=>owner.Capture());
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(outside);if(marker)UnityEngine.Object.DestroyImmediate(marker);}
  }
  [Test] public void ExportPreservesOriginalCanonicalBytesForServerReader(){
   var root=new GameObject("explicit interop measurement fixture");
   try{
    var box=root.AddComponent<BoxCollider>();box.size=new Vector3(2,1,4);box.center=new Vector3(0,.5f,0);
    var owner=root.AddComponent<BPhysicalSceneSource>();owner.Bind("interop-fixture","explicit-interop-unit",1000d,root.transform,box,new[]{"origin"},new[]{root.transform});
    var method=typeof(BPhysicalSceneSource).GetMethod("CaptureExport");Assert.NotNull(method,"Exact original canonical byte export missing");
    var export=(JObject)method.Invoke(owner,null);var packet=(JObject)export["packet"];
    string geometry=(string)export["geometryCanonicalJson"],canonical=(string)export["packetCanonicalJson"];
    string RawHash(string text){using(var sha=System.Security.Cryptography.SHA256.Create())return "sha256:"+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
    Assert.AreEqual((string)packet["geometryHash"],RawHash(geometry));
    Assert.AreEqual((string)packet["packetHash"],RawHash(canonical));
    StringAssert.Contains("\"millimetersPerUnit\":1000.0",geometry,"Preserve Newtonsoft floating-point representation");
    var geometryBody=(JObject)packet.DeepClone();geometryBody.Remove("packetHash");geometryBody.Remove("geometryHash");geometryBody.Remove("sourceRevision");
    Assert.IsTrue(JToken.DeepEquals(geometryBody,JObject.Parse(geometry)));
    var packetBody=(JObject)packet.DeepClone();packetBody.Remove("packetHash");
    Assert.IsTrue(JToken.DeepEquals(packetBody,JObject.Parse(canonical)));
    var exportPath=Environment.GetEnvironmentVariable("LASTRAIL_MEASUREMENT_EXPORT");
    if(!string.IsNullOrEmpty(exportPath)){
     var full=System.IO.Path.GetFullPath(exportPath);
     Assert.IsTrue(full.StartsWith("D:\\Project\\LASTRAIL\\",StringComparison.OrdinalIgnoreCase));
     System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
     using(var stream=new System.IO.FileStream(full,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write))
     using(var writer=new System.IO.StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(export.ToString(Newtonsoft.Json.Formatting.None));
     TestContext.Out.WriteLine("Actual Unity measurement export: "+full);
    }
    packet["bodyCornersMm"][0][0]=999999;
    Assert.AreEqual(-1000,(long)owner.Capture()["bodyCornersMm"][0][0]);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
