#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class ApprovedFeedbackPlayerTests {
  static Type ProbeType { get {var t=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Presentation.Battle.ApprovedFeedbackPlayerProbe");Assert.That(t,Is.Not.Null,"Explicit native feedback Player probe missing");return t;} }
  [Test]public void PlayerProbeRejectsMissingPinBeforeCreatingSceneObjects(){
   var t=ProbeType;var go=new GameObject("probe-invalid");var asset=new TextAsset("{}");
   try{var probe=go.AddComponent(t);var e=Assert.Throws<TargetInvocationException>(()=>t.GetMethod("Initialize").Invoke(probe,new object[]{asset,"wrong"}));Assert.That(e.InnerException,Is.TypeOf<ArgumentException>());Assert.That(go.transform.childCount,Is.Zero);}
   finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(asset);}
  }
  [Test]public void PlayerProbeReusesOriginalMiraSourceInsteadOfFabricatedInputs(){
   var t=ProbeType;var source=RoofMiraLocalOwnerTests.SourceEnvelope();var asset=new TextAsset(source.ToString());var go=new GameObject("probe-source");
   try{var p=go.AddComponent(t);t.GetMethod("Initialize").Invoke(p,new object[]{asset,BDirectAttackInput.Digest(source)});Assert.That((string)t.GetProperty("SourceHash").GetValue(p),Is.EqualTo(BDirectAttackInput.Digest(source)));Assert.That(go.transform.childCount,Is.Zero);}
   finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(asset);}
  }
  [Test]public void ActualNativeGpuContactLabelsReadOnlyOriginalReceipt(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_APPROVED_FEEDBACK_GPU_OUTPUT")??"D:/Project/LASTRAIL/QA/MiraFirstLive-20261015/ApprovedFeedbackGpu01";
   string old=Environment.GetEnvironmentVariable("LASTRAIL_MIRA_FEEDBACK_QA_OUTPUT");Environment.SetEnvironmentVariable("LASTRAIL_MIRA_FEEDBACK_QA_OUTPUT",output);
   try{
    new MiraFirstNativeFeedbackAcceptanceTests().ActualAcceptedNodePrimaryContactsAndNativeViewGpuCapture();
    var report=JObject.Parse(File.ReadAllText(output+"/acceptance.json"));var contact=((JArray)report["acceptedPulses"]).Single(p=>(string)p["kind"]=="CONTACT");
    Assert.That((string)contact["labels"]["hpText"],Is.EqualTo("33"));Assert.That((string)contact["labels"]["shieldText"],Is.EqualTo("10"));
    Assert.That((string)contact["labels"]["criticalState"],Is.EqualTo("NOT_APPLICABLE"));Assert.That((long)contact["targetGeneration"],Is.GreaterThanOrEqualTo(1));Assert.That(contact["pointMm"],Is.Not.Null);
   }finally{Environment.SetEnvironmentVariable("LASTRAIL_MIRA_FEEDBACK_QA_OUTPUT",old);}
  }
 }
}
#endif
