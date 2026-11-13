using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Measured publication input only. This test neither registers a definition nor approves a START pin.
public sealed class OrdinaryEnemyBodyPublicationTests {
 const string Folder="Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/";
 const string Prefab=Folder+"OrdinaryEnemyTrainV01.prefab",ScenePath=Folder+"OrdinaryEnemyTrainV01.unity";
 static string Absolute(string asset)=>Path.Combine(Path.GetDirectoryName(Application.dataPath),asset);
 static string Hash(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static object Measure(string method,params object[] args){try{return typeof(BPhysicalSceneSource).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 [Test] public void ExistingApprovedEnemyBodyExportsMeasuredRegistryInputWithoutWeaponsOrPinApproval(){
  string prefabHash=Hash(File.ReadAllBytes(Absolute(Prefab)));
  Assert.AreEqual("5844aaa8e930bc2b97c0fd831fa0acddf2b7cf60900b1585670c5f2f5e992d02",prefabHash,"Existing server body source hash must match before publication preparation.");
  string sceneHash=Hash(File.ReadAllBytes(Absolute(ScenePath)));
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
  GameObject loaded=null;
  try{
   var frame=scene.GetRootGameObjects().Single(g=>g.name=="OrdinaryEnemyTrainV01").transform;
   loaded=PrefabUtility.LoadPrefabContents(Prefab);
   var body=frame.Find("BodyCollider").GetComponent<BoxCollider>();
   var shape=(JObject)Measure("MeasureShape",frame,body,1000d);
   var prefabShape=(JObject)Measure("MeasureShape",loaded.transform,loaded.transform.Find("BodyCollider").GetComponent<BoxCollider>(),1000d);
   Assert.IsTrue(JToken.DeepEquals(shape,prefabShape),"Saved scene and original prefab must have identical measured geometry.");
   Assert.IsTrue(JToken.DeepEquals(shape["sizeMm"],new JArray(12000,3200,3000)));
   Assert.IsTrue(JToken.DeepEquals(shape["centerMm"],new JArray(0,1600,0)));
   shape["id"]="BodyCollider";shape["role"]="OCCLUDER";
   var anchors=new JArray();
   foreach(string name in new[]{"GunnerFoot_0","GunnerFoot_1"}){
    var point=(JArray)Measure("MeasureAnchor",frame,frame.Find(name),1000d);
    Assert.IsTrue(JToken.DeepEquals(point,(JArray)Measure("MeasureAnchor",loaded.transform,loaded.transform.Find(name),1000d)));
    anchors.Add(new JObject{["id"]=name,["role"]="FOOT",["positionMm"]=point});
   }
   var definition=new JObject{["version"]="ordinary.native-geometry-definition.v1.20261031",["definitionId"]="ordinary-enemy-body-measured-v1-20261102",["units"]="MILLIMETERS",["millimetersPerUnit"]=1000,["axis"]="X_FORWARD_Y_UP_Z_SIDE",["coordinateSpace"]="BOUND_FRAME_LOCAL",
    ["source"]=new JObject{["scenePath"]=ScenePath,["sceneHash"]=sceneHash},
    ["entities"]=new JArray(new JObject{["authoringId"]="enemy_train_first_battle",["role"]="TRAIN",["definitionRef"]="enemy_train_first_battle",["framePath"]=frame.name,["assetPath"]=Prefab,["assetHash"]=prefabHash,["anchors"]=anchors,["shapes"]=new JArray(shape)})};
   var bytes=new UTF8Encoding(false).GetBytes(definition.ToString(Formatting.None));
   const string output="D:/Project/LASTRAIL/QA/CommanderS1-20260922/NativeOffscreen845/ordinary-enemy-body-definition-unpublished.json";
   Directory.CreateDirectory(Path.GetDirectoryName(output));
   if(File.Exists(output))CollectionAssert.AreEqual(bytes,File.ReadAllBytes(output),"Preserve a previously exported version; changed measurements require a new publication artifact.");
   else File.WriteAllBytes(output,bytes);
   Assert.AreEqual(sceneHash,Hash(File.ReadAllBytes(Absolute(ScenePath))));Assert.AreEqual(prefabHash,Hash(File.ReadAllBytes(Absolute(Prefab))));
   TestContext.WriteLine(new JObject{["path"]=output,["definitionHash"]=Hash(bytes),["sceneHash"]=sceneHash,["assetHash"]=prefabHash,["measuredEntities"]=1,["measuredFootAnchors"]=2,["registered"]=false,["pinApproved"]=false,["executionVerified"]=false,["weaponGeometryIncluded"]=false}.ToString(Formatting.None));
  }finally{if(loaded!=null)PrefabUtility.UnloadPrefabContents(loaded);EditorSceneManager.CloseScene(scene,true);}
 }
}
