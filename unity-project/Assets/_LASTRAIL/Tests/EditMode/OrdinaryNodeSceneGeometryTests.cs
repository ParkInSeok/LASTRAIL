using System;
using System.Collections.Generic;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

// Explicit test registration and real Unity components; not approval of product candidate geometry.
public sealed class OrdinaryNodeSceneGeometryTests {
 static object Call(object owner,string name,params object[] args){try{return owner.GetType().GetMethod(name).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 sealed class Fixture:IDisposable {
  internal readonly GameObject Root=new GameObject("test-native-basis");internal Transform Frame,Muzzle;internal BoxCollider Box;
  internal OrdinaryFiveStartInput Start;internal OrdinaryNodeStart Node;internal OrdinaryNodeAttempt Attempt;internal string Bytes;internal object Owner;internal bool Current=true;internal long Generation=1;internal Action OnGeneration;
  internal Fixture(bool create=true,bool extraEntity=false){
   Frame=new GameObject("actor").transform;Frame.SetParent(Root.transform,false);Muzzle=new GameObject("muzzle").transform;Muzzle.SetParent(Frame,false);Muzzle.localPosition=new Vector3(.5f,1.25f,0);
   Box=Frame.gameObject.AddComponent<BoxCollider>();Box.center=new Vector3(0,1,0);Box.size=new Vector3(1,2,1);
   var definition=new JObject{["version"]="ordinary.native-geometry-definition.v1.20261031",["definitionId"]="TEST_ONLY_SCENE_GEOMETRY",["units"]="MILLIMETERS",["millimetersPerUnit"]=1000,["axis"]="X_FORWARD_Y_UP_Z_SIDE",["coordinateSpace"]="BOUND_FRAME_LOCAL",["source"]=new JObject{["scenePath"]="Assets/TestOnly.unity",["sceneHash"]=new string('a',64)},["entities"]=new JArray(new JObject{["authoringId"]="reina-authoring",["role"]="OPERATOR",["definitionRef"]="op_reina",["framePath"]="actor",["assetPath"]="Assets/TestOnly.prefab",["assetHash"]=new string('b',64),["anchors"]=new JArray(new JObject{["id"]="muzzle",["role"]="EMISSION",["abilityId"]="op_reina.ultimate",["positionMm"]=new JArray(500,1250,0)}),["shapes"]=new JArray(new JObject{["id"]="hit",["role"]="HIT_VOLUME",["kind"]="BOX",["centerMm"]=new JArray(0,1000,0),["sizeMm"]=new JArray(1000,2000,1000)})})};
   if(extraEntity){var extra=(JObject)definition["entities"][0].DeepClone();extra["authoringId"]="required-world-solid";extra["role"]="WORLD";((JArray)definition["entities"]).Add(extra);}
   Bytes=definition.ToString(Newtonsoft.Json.Formatting.None);using var sha=System.Security.Cryptography.SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Bytes))).Replace("-","").ToLowerInvariant();
   var reference=new JObject{["definitionId"]=definition["definitionId"].DeepClone(),["version"]=definition["version"].DeepClone(),["definitionHash"]=hash};var f=OrdinaryNodeStartWireTests.Fixture(reference);var entry=f.Response["receipt"]["nodeStart"];
   entry["snapshot"]["runtime"]["spatial"]["definitionBinding"]=new JObject{["version"]="ordinary.node-geometry-binding.v1.20261031",["accountId"]=entry["accountId"].DeepClone(),["runId"]=entry["runId"].DeepClone(),["nodeId"]=entry["nodeId"].DeepClone(),["entryMapRevision"]=entry["entryMapRevision"].DeepClone(),["originGeneration"]=1,["definitionRef"]=reference};entry["snapshotHash"]=BDirectAttackInput.Digest(entry["snapshot"]).Substring(7);
   Start=f.Start;Node=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;Node.RequireOriginal(f.Start);Attempt=OrdinaryNodeAttempt.FromEntry(Node);if(create){Create();Bind();}
  }
  internal void Create(){var type=typeof(BPhysicalSceneSource).Assembly.GetType("LastRail.Battle.OrdinaryNodeSceneGeometry");Assert.NotNull(type,"Missing current-node native scene measurement adapter");try{Owner=Activator.CreateInstance(type,new object[]{Start,Node,Attempt,Bytes,Root.transform,(Func<bool>)(()=>Current)});}catch(TargetInvocationException e){throw e.InnerException;}}
  internal void Bind()=>Call(Owner,"BindEntity","op_reina","reina-authoring",Frame,new Dictionary<string,Transform>{{"muzzle",Muzzle}},new Dictionary<string,Collider>{{"hit",Box}},(Func<long>)(()=>{OnGeneration?.Invoke();return Generation;}));
  internal JObject Capture(long tick=0)=>(JObject)Call(Owner,"Capture",tick);
  public void Dispose(){if(Owner is IDisposable d)d.Dispose();UnityEngine.Object.DestroyImmediate(Root);}
 }
 [Test] public void RealComponentsProduceOriginalAttemptBoundMeasuredGeometry(){using var f=new Fixture();f.Frame.localPosition=new Vector3(3,0,0);var s=f.Capture();Assert.AreEqual(f.Node.NodeId,(string)s["nodeId"]);Assert.AreEqual(f.Attempt.Generation,(long)s["attemptGeneration"]);var actor=s["entities"][0];Assert.AreEqual(3000,(long)actor["frame"]["originMm"][0]);Assert.AreEqual(500,(long)actor["anchors"][0]["positionMm"][0]);Assert.AreEqual(2000,(long)actor["shapes"][0]["sizeMm"][1]);}
 [Test] public void PinnedBytesAndActualShapeMustBothMatch(){using var changed=new Fixture(false);changed.Bytes+=" ";Assert.Throws<ArgumentException>(changed.Create);using var shape=new Fixture();shape.Box.size=Vector3.one;Assert.Throws<InvalidOperationException>(()=>shape.Capture());using var muzzle=new Fixture();muzzle.Muzzle.localPosition+=Vector3.right;Assert.Throws<InvalidOperationException>(()=>muzzle.Capture());}
 [Test] public void SameTickCannotChangePoseAndCapturedDataCannotMutateSource(){using var f=new Fixture();var first=f.Capture();first["entities"][0]["anchors"][0]["positionMm"][0]=9999;Assert.AreEqual(500,(long)f.Capture()["entities"][0]["anchors"][0]["positionMm"][0]);f.Frame.localPosition=Vector3.right;Assert.Throws<InvalidOperationException>(()=>f.Capture());Assert.AreEqual(1000,(long)f.Capture(1)["entities"][0]["frame"]["originMm"][0]);}
 [Test] public void CurrentGenerationIsReadWithoutInventingAnIndependentLifeOwner(){using var f=new Fixture();f.Capture();f.Generation=2;Assert.AreEqual(2,(long)f.Capture(1)["entities"][0]["generation"]);f.Generation=0;Assert.Throws<InvalidOperationException>(()=>f.Capture(2));}
 [Test] public void RetiredAttemptDestroyedReferencesAndDuplicateBindingsFailClosed(){using var f=new Fixture();Assert.Throws<InvalidOperationException>(f.Bind);f.Current=false;Assert.Throws<InvalidOperationException>(()=>f.Capture());f.Current=true;UnityEngine.Object.DestroyImmediate(f.Muzzle.gameObject);Assert.Throws<InvalidOperationException>(()=>f.Capture());((IDisposable)f.Owner).Dispose();Assert.Throws<ObjectDisposedException>(()=>f.Capture());}
 [Test] public void ScaledBasisCannotSilentlyChangeTheRegisteredMillimeterContract(){using var f=new Fixture();f.Root.transform.localScale=Vector3.one*2;Assert.Throws<InvalidOperationException>(()=>f.Capture());}
 [Test] public void EntireDestroyedEntityReportsAbsenceInsteadOfReusingItsLastPositionAsCurrent(){using var f=new Fixture();var prior=f.Capture();UnityEngine.Object.DestroyImmediate(f.Frame.gameObject);var absent=f.Capture(1)["entities"][0];Assert.IsFalse((bool)absent["present"]);Assert.IsFalse((bool)absent["active"]);Assert.IsTrue(JToken.DeepEquals(prior["entities"][0]["frame"],absent["frame"]));}
 [Test] public void UnboundRegisteredGeometryCannotBecomeAnImplicitEmptyOccluderSet(){using var f=new Fixture(extraEntity:true);Assert.Throws<InvalidOperationException>(()=>f.Capture());}
 [Test] public void MovingTheCoordinateBasisRequiresAnExplicitNewContext(){using var f=new Fixture();f.Capture();f.Root.transform.position=Vector3.right;Assert.Throws<InvalidOperationException>(()=>f.Capture(1));}
 [Test] public void SameTickRecheckCannotReturnAnAttemptRetiredDuringNativeMeasurement(){using var f=new Fixture();f.Capture();f.OnGeneration=()=>f.Current=false;Assert.Throws<InvalidOperationException>(()=>f.Capture());}
}
