using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
public sealed class OrdinaryRoofPresentationTests {
 [TestCase(false)][TestCase(true)] public void OriginalPhysicalAnchorsIgnoreRowOrderAndRejectForeignPlacement(bool suppliedArt){
  const string path="Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/";
  var assets=ScriptableObject.CreateInstance<OrdinaryRoofPresentationAssets>();
  var art=new Texture2D(8,4);
  if(suppliedArt){assets.CarBodies=new[]{art,art,art};assets.CarBounds=new[]{new Rect(0,0,8,4),new Rect(0,0,8,4),new Rect(0,0,8,4)};}
  assets.Prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path+"OrdinaryRoofSpawnV01.prefab");assets.Definition=AssetDatabase.LoadAssetAtPath<TextAsset>(path+"definition.json");
  string before=File.ReadAllText(path+"OrdinaryRoofSpawnV01.prefab");
  var source=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json"));
  var original=OrdinaryFiveStartInput.Read((JObject)source["originalStart"],(string)source["accountId"],(string)source["runId"]);
  var model=OrdinaryFiveCombatFactory.FromInitialization(original,source["initializationResponse"].ToString(),500000).Create();
  string owner=model.CaptureLocalState();var hud=model.ReadHudState();RenderTexture texture=null;
  try{
   using(var roof=new OrdinaryRoofPresentation(assets,(JObject)hud["ordinaryRail"])){
    texture=roof.Texture;Assert.AreEqual(5,roof.AnchorCount);
    var ground=roof.ProjectRoofRelativeToFront(-16000,0);
    Assert.True(ground.HasValue);
    Assert.That(Vector2.Distance(ground.Value,roof.ProjectRelativeToFront(-16000,3212.5,0)),Is.LessThan(.0001f));
    Assert.Less(ground.Value.y,roof.ProjectRelativeToFront(-16000,4113,0).y,"Fire starts on the roof, not at a target body center");
    Assert.IsNull(roof.ProjectRoofRelativeToFront(-12500,0),"The gap between cars is not a roof");
    if(suppliedArt){
     var origin=roof.ProjectRelativeToFront(-16000,3213,0);
     var right=roof.ProjectRelativeToFront(-15000,3213,0);
     var up=roof.ProjectRelativeToFront(-16000,4213,0);
     // The isolated stage sits at x=10000; float projection precision is below 0.03 output pixels.
     Assert.AreEqual((right.x-origin.x)*texture.width,(up.y-origin.y)*texture.height,.03,"Pre-authored 2D art must not receive a second vertical perspective compression.");
    }
    if(suppliedArt){var skins=Resources.FindObjectsOfTypeAll<SpriteRenderer>().Where(r=>r.name=="ApprovedCarBody"&&r.sprite.texture==art).ToArray();Assert.AreEqual(3,skins.Length);foreach(var skin in skins)Assert.AreEqual(12,skin.sprite.bounds.size.x,.0001);}
    var actors=hud["actors"].Where(a=>!(bool)a["train"]).ToArray();var points=actors.ToDictionary(a=>(string)a["id"],roof.Project);
    Assert.AreEqual(5,points.Values.Distinct().Count());
    foreach(var actor in actors.Reverse())Assert.AreEqual(points[(string)actor["id"]],roof.Project(actor));
    var wrong=(JObject)actors[0].DeepClone();wrong["physicalInstanceId"]="foreign";Assert.Throws<InvalidOperationException>(()=>roof.Project(wrong));
    wrong=(JObject)actors[0].DeepClone();wrong["localXmm"]=1;Assert.Throws<InvalidOperationException>(()=>roof.Project(wrong));
    var boarding=new JArray(Enumerable.Range(0,3).Select(i=>new JObject{["id"]="boarding-test-"+i,["enemy"]=true,["boarding"]=true,["slot"]=1,["hp"]=100,["hitHeightMm"]=1800,["hitRadiusMm"]=300,["footRelativeToFrontMm"]=new JArray(-18000+i*1000,3213,0),["tauntedBy"]="op_kyle"}));
    Assert.DoesNotThrow(()=>roof.RefreshEnemies(boarding));
    var bodies=UnityEngine.Resources.FindObjectsOfTypeAll<MeshRenderer>().Where(r=>r.name.StartsWith("LocalAuthoredBoardingCapsule:boarding-test-")).ToArray();Assert.AreEqual(3,bodies.Length);
    foreach(var body in bodies){Assert.AreEqual(1,body.sharedMaterial.color.r,.0001);Assert.AreEqual(.75f,body.sharedMaterial.color.g,.0001);Assert.AreEqual(.12f,body.sharedMaterial.color.b,.0001);}
    var stateField=typeof(OrdinaryRoofPresentation).GetField("enemyState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    var visibleState=stateField.GetValue(roof);boarding[0]["basicNextOpportunityInTicks"]=12;boarding[0]["hp"]=99;roof.RefreshEnemies(boarding);
    Assert.AreEqual(visibleState,stateField.GetValue(roof),"Cooldown and nonlethal HP changes must not invalidate the roof render.");
    boarding[0]["footRelativeToFrontMm"][0]=-19000;roof.RefreshEnemies(boarding);Assert.AreNotEqual(visibleState,stateField.GetValue(roof));
    boarding[0]["hp"]=0;roof.RefreshEnemies(boarding);Assert.IsFalse(bodies.Single(b=>b.name.EndsWith("0")).gameObject.activeSelf);
    roof.RefreshEnemies(new JArray());Assert.IsTrue(bodies.All(b=>!b.gameObject.activeSelf));
   }
   Assert.IsTrue(texture==null);Assert.AreEqual(before,File.ReadAllText(path+"OrdinaryRoofSpawnV01.prefab"));Assert.AreEqual(owner,model.CaptureLocalState());
  }finally{UnityEngine.Object.DestroyImmediate(assets);UnityEngine.Object.DestroyImmediate(art);}
 }
}
