#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Diagnostics;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class LocalDevelopmentDiagnosticsPathTests {
  static string Resolve(string original,string explicitPath){
   var type=typeof(LocalClientErrorCapture).Assembly.GetType("LastRail.Diagnostics.LocalDevelopmentDiagnosticsPath");
   Assert.That(type,Is.Not.Null,"Explicit D development diagnostics path adapter missing");
   try{return (string)type.GetMethod("Resolve").Invoke(null,new object[]{original,explicitPath});}
   catch(System.Reflection.TargetInvocationException error){throw error.InnerException;}
  }
  [Test]public void NoLocalOverridePreservesExistingDeviceStorage(){string original="D:/Project/LASTRAIL/QA/OriginalDeviceData";Assert.That(Resolve(original,null),Is.EqualTo(Path.Combine(original,"diagnostics","safe-client-errors-v1.json")));}
  [Test]public void ExplicitDevelopmentOutputStaysWithinDProjectQa(){var path=Resolve("C:/unused-original","D:/Project/LASTRAIL/QA/ExplicitDevDiagnostic/diagnostics");Assert.That(path.Replace('\\','/'),Is.EqualTo("D:/Project/LASTRAIL/QA/ExplicitDevDiagnostic/diagnostics/safe-client-errors-v1.json"));}
  [TestCase("C:/wrong")][TestCase("relative/path")][TestCase("D:/OtherProject/log")][TestCase("D:/Project/LASTRAIL/QA/../../outside")][TestCase("")]
  public void InvalidExplicitOverrideCannotSilentlyFallBackToC(string value){Assert.Throws<ArgumentException>(()=>Resolve("C:/unused-original",value));}
 }
}
#endif