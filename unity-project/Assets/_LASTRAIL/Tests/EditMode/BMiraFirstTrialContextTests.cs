#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class BMiraFirstTrialContextTests {
  static string Path=>"D:/Project/LASTRAIL/QA/MiraFirstStatic-20261015/NativeSource/server-packets.json";
  static JObject Entry(string name="grown")=>(JObject)JObject.Parse(File.ReadAllText(Path))["packets"][name];
  static BMiraFirstTestSource Source(string name="grown"){
   var e=Entry(name);var p=(JObject)e["packet"];var m=(JObject)e["originalMira"];
   return BMiraFirstTestSource.Read(p,p,(string)p["packetHash"],(string)p["accountId"],(string)p["runId"],m,(string)m["packetHash"],(string)e["departurePin"]["pinHash"]);
  }
  internal static JObject Policy()=>new JObject{["profile"]=BMiraFirstTestSource.Profile,["status"]="NOT_APPLICABLE",["policyVersion"]="mira-first-test-critical-na-20261015"};
  static Type Type(){var t=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.BMiraFirstTrialContext");Assert.That(t,Is.Not.Null,"Explicit critical-NA first-trial context missing.");return t;}
  static object Invoke(string method,params object[] args){try{return Type().GetMethod(method).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  static object Read(BMiraFirstTestSource s=null,JObject policy=null)=>Invoke("Read",s??Source(),policy??Policy());
  static JObject Capture(object c)=>(JObject)c.GetType().GetMethod("Capture").Invoke(c,null);
  [Test]public void ApprovedCriticalNaIsSeparateFromUnmodifiedOriginalServerPacket(){
   var source=Source();var before=source.Capture();var c=Read(source);var p=Capture(c);
   Assert.That((string)p["critical"]["status"],Is.EqualTo("NOT_APPLICABLE"));
   Assert.That(p["critical"]["chance"],Is.Null);Assert.That(p["critical"]["rolled"],Is.Null);
   Assert.That(JToken.DeepEquals(before,source.Capture()),Is.True);Assert.That(source.StartReady,Is.False);
   Assert.That((string)p["firstSourceHash"],Is.EqualTo(source.Hash));Assert.That((string)p["accountId"],Is.EqualTo(source.AccountId));Assert.That((string)p["runId"],Is.EqualTo(source.RunId));
  }
  [TestCase("profile","other")][TestCase("status","NON_CRITICAL")][TestCase("status","ROLLED_FAILURE")][TestCase("policyVersion","old")]
  public void PolicyCannotBeInferredOrExpanded(string field,string value){var p=Policy();p[field]=value;Assert.Throws<ArgumentException>(()=>Read(policy:p));}
  [Test]public void ChanceZeroCannotBeSmuggledIntoNotApplicablePolicy(){var p=Policy();p["chance"]=0;Assert.Throws<ArgumentException>(()=>Read(policy:p));}
  [Test]public void BeforeFlightContextRestoresExactlyFromItsOriginalPolicyAndSource(){
   var c=Read();var p=Capture(c);var r=Invoke("Restore",p,BDirectAttackInput.Digest(p),Source(),Policy());Assert.That(JToken.DeepEquals(p,Capture(r)),Is.True);
  }
  [Test]public void OldVersionPolicyOrDifferentRunNeverReceiveNewPolicyDuringRestore(){
   var p=Capture(Read());var old=(JObject)p.DeepClone();old.Remove("critical");
   Assert.Throws<ArgumentException>(()=>Invoke("Restore",old,BDirectAttackInput.Digest(old),Source(),Policy()));
   p["critical"]["policyVersion"]="old";Assert.Throws<ArgumentException>(()=>Invoke("Restore",p,BDirectAttackInput.Digest(p),Source(),Policy()));
   p=Capture(Read());Assert.Throws<ArgumentException>(()=>Invoke("Restore",p,BDirectAttackInput.Digest(p),Source("base"),Policy()));
  }
  [Test]public void CallerCannotMutateCapturedPolicyOrFrozenSource(){
   var c=Read();var p=Capture(c);p["critical"]["status"]="CHANGED";p["firstSourceHash"]="CHANGED";
   Assert.That((string)Capture(c)["critical"]["status"],Is.EqualTo("NOT_APPLICABLE"));Assert.That((string)Capture(c)["firstSourceHash"],Is.Not.EqualTo("CHANGED"));
  }
 }
}
#endif