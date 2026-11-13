using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryLiveCombatSourcesTests {
 static string Hash(string bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(bytes))).Replace("-","").ToLowerInvariant();}
 static JObject Read(string bytes,string hash){try{return (JObject)typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryLiveCombatSources").GetMethod("ReadDefinition",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{bytes,hash});}catch(TargetInvocationException e){throw e.InnerException;}}
 // Deliberate parser fixture, not real actor measurements or gameplay evidence.
 static JObject Definition()=>new JObject{
  ["version"]="ordinary.native-geometry-definition.v1.20261031",["definitionId"]="TEST_ONLY",["units"]="MILLIMETERS",["millimetersPerUnit"]=1000,["axis"]="X_FORWARD_Y_UP_Z_SIDE",["coordinateSpace"]="BOUND_FRAME_LOCAL",
  ["source"]=new JObject{["scope"]="LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION",["obstacle"]=new JObject{["initialFront"]=58000},["liveLayout"]=new JObject{["body"]=new JObject{["originMm"]=new JArray(68000,0,0),["relativeToInitialFrontMm"]=new JArray(10000,0,0),["basis"]=new JArray(new JArray(1,0,0),new JArray(0,1,0),new JArray(0,0,1))}}},
  ["entities"]=new JArray(new JObject{["authoringId"]="body",["role"]="WORLD",["definitionRef"]="body",["framePath"]="test/body",["assetPath"]="Assets/TestOnly.prefab",["assetHash"]=new string('a',64),["anchors"]=new JArray(),["shapes"]=new JArray(new JObject{["id"]="box",["role"]="OCCLUDER",["kind"]="BOX",["centerMm"]=new JArray(0,0,0),["sizeMm"]=new JArray(1000,1000,1000)})})};
 [Test] public void ExactPinnedGeometryRetainsExplicitProvenanceAndMeasuredOffsets(){var d=Definition();string bytes=d.ToString();var read=Read(bytes,Hash(bytes));Assert.IsTrue(JToken.DeepEquals(d,read));read["source"]["liveLayout"]["body"]["originMm"][0]=0;Assert.AreEqual(68000,(long)Read(bytes,Hash(bytes))["source"]["liveLayout"]["body"]["originMm"][0]);}
 [Test] public void ChangedRawBytesCannotReuseIndependentDefinitionPin(){string bytes=Definition().ToString();Assert.Throws<ArgumentException>(()=>Read(bytes+" ",Hash(bytes)));Assert.Throws<ArgumentException>(()=>Read(bytes,null));}
 [Test] public void InconsistentFrameAndUnsupportedRotationAreRejected(){var d=Definition();d["source"]["liveLayout"]["body"]["relativeToInitialFrontMm"][0]=999;string bytes=d.ToString();Assert.Throws<ArgumentException>(()=>Read(bytes,Hash(bytes)));d=Definition();d["source"]["liveLayout"]["body"]["basis"][0][0]=0.5;bytes=d.ToString();Assert.Throws<ArgumentException>(()=>Read(bytes,Hash(bytes)));}
 [Test] public void NegativeDimensionsAndUnmeasuredFramesAreRejected(){var d=Definition();d["entities"][0]["shapes"][0]["sizeMm"][0]=-1;string bytes=d.ToString();Assert.Throws<ArgumentException>(()=>Read(bytes,Hash(bytes)));d=Definition();((JObject)d["source"]["liveLayout"]).Remove("body");bytes=d.ToString();Assert.Throws<ArgumentException>(()=>Read(bytes,Hash(bytes)));}
 [Test] public void AnchorHitRoleCannotBeSubstitutedForRegisteredShapeRole(){var d=Definition();d["entities"][0]["shapes"][0]["role"]="HIT";string bytes=d.ToString();Assert.Throws<ArgumentException>(()=>Read(bytes,Hash(bytes)));}
}
