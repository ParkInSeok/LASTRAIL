using System;
using System.Linq;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class FourAgentV03EffectsTests {
  static FourAgentV03Catalog Catalog(){var clips=new JArray();foreach(var id in new[]{"op_kyle","op_reina","op_eve","op_sera"})for(int d=1;d<=4;d++)foreach(var motion in new[]{"idle","attack"})clips.Add(new JObject{["actorId"]=id,["direction"]=d,["motion"]=motion,["durationMs"]=2400,["scale"]=.1f,["anchor"]=new JArray(500,1000),["frames"]=new JArray(new JObject{["path"]="fixture",["durationMs"]=2400,["x"]=0,["y"]=0,["w"]=100,["h"]=100})});return FourAgentV03Catalog.Restore(new JObject{["version"]=FourAgentV03Catalog.Version,["clips"]=clips});}
  static JArray Draw(string id,long age,long continuous=0,bool fx=true,bool smoke=false,bool target=true,bool attack=true){var type=typeof(FourAgentV03Catalog).Assembly.GetType("LastRail.Presentation.Battle.FourAgentV03Effects");Assert.That(type,Is.Not.Null,"v03 independent effect adapter is required");var method=type.GetMethod("Append");Assert.That(method,Is.Not.Null);var rows=new JArray();method.Invoke(null,new object[]{rows,id,1,age,continuous,attack,fx,smoke,Vector2.zero,target?(Vector2?)new Vector2(-200,0):null,Catalog(),new Func<string,Vector2>(_=>new Vector2(1200,1600))});return rows;}
  static int Count(JArray rows,string role)=>rows.Count(x=>(string)x["role"]==role);
  [Test] public void EveBeamUsesFinalAttackClock(){Assert.That(Count(Draw("op_eve",1299),"beam"),Is.Zero);Assert.That(Count(Draw("op_eve",1300),"beam"),Is.GreaterThan(0));Assert.That(Count(Draw("op_eve",1420),"beam"),Is.Zero);}
  [Test] public void SeraBeamHasOwnEnvelope(){Assert.That(Count(Draw("op_sera",1080),"beam"),Is.Zero);Assert.That(Count(Draw("op_sera",1100),"beam"),Is.GreaterThan(0));Assert.That(Count(Draw("op_sera",1380),"beam"),Is.Zero);}
  [TestCase("op_eve")][TestCase("op_sera")] public void MissingTargetNeverInventsBeam(string id){Assert.That(Count(Draw(id,1300,target:false),"beam"),Is.Zero);}
  [Test] public void SeraHasThreeIndependentCrystalsAndNoDuplicateActor(){var rows=Draw("op_sera",1100);Assert.That(Count(rows,"crystal"),Is.EqualTo(3));Assert.That(Count(rows,"actor"),Is.Zero);}
  [TestCase("op_eve","drone",4)][TestCase("op_sera","crystal",3)] public void EffectsOffRetainsBodies(string id,string role,int n){var rows=Draw(id,1300,fx:false);Assert.That(Count(rows,role),Is.EqualTo(n));Assert.That(Count(rows,"beam"),Is.Zero);Assert.That(Count(rows,"charge"),Is.Zero);}
  [Test] public void SmokeUsesContinuousClockAndSurvivesAttackTransition(){var idle=Draw("op_eve",0,777,smoke:true,attack:false).Where(x=>(string)x["role"]=="smoke").ToArray();var attack=Draw("op_eve",1300,777,smoke:true).Where(x=>(string)x["role"]=="smoke").ToArray();Assert.That(idle.Length,Is.EqualTo(18));Assert.That(JToken.DeepEquals(new JArray(idle),new JArray(attack)),Is.True);var later=Draw("op_eve",1300,1000,smoke:true).Where(x=>(string)x["role"]=="smoke");Assert.That(JToken.DeepEquals(new JArray(attack),new JArray(later)),Is.False);}
 }
}
