#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using LastRail.Qa;
using System.Linq;
namespace LastRail.Tests.EditMode {
 public sealed class MapOverlapV2Tests {
  [Test] public void QaAssemblyIsExcludedFromPlayerCompilation(){Assert.That(UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player).Select(a=>a.name),Does.Not.Contain("LastRail.MapGrayboxFixture.Qa"));}
  [Test] public void InvalidDomainsAreRejectedBeforeGuards(){Assert.Throws<ArgumentOutOfRangeException>(()=>MapOverlapV2.Evaluate(0,0,1,10,2,4,1,2));Assert.Throws<ArgumentOutOfRangeException>(()=>MapOverlapV2.Evaluate(1,2,1,10,2,4,1,2));Assert.Throws<ArgumentOutOfRangeException>(()=>MapOverlapV2.Evaluate(1,0,1,0,2,4,1,2,false,false,true));Assert.Throws<ArgumentOutOfRangeException>(()=>MapOverlapV2.Evaluate(1,0,1,10,2,2,1,2));Assert.Throws<ArgumentOutOfRangeException>(()=>MapOverlapV2.Evaluate(1,0,1,10,2,4,2,2));}
  [Test] public void SeparateCandidateMatchesFifteenPinnedExpectations(){
   var type=Type.GetType("LastRail.Qa.MapOverlapV2, LastRail.MapGrayboxFixture.Qa");Assert.That(type,Is.Not.Null,"Separate v2 candidate is required; never modify observed-v1");
   var report=(JObject)type.GetMethod("Verify").Invoke(null,new object[]{null});Assert.That((int)report["cases"],Is.EqualTo(15));Assert.That((string)report["result"],Is.EqualTo("PASS"));
  }
 }
}
#endif
