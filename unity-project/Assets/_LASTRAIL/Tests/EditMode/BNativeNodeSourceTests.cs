#if UNITY_EDITOR
using LastRail.Battle;
using LastRail.Run;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class BNativeNodeSourceTests {
  internal static JObject First()=> (JObject)JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-native-node-sample-20261005.json"))["first"];
  static BNativeNodeSourceSnapshot Read(JObject p)=>BNativeNodeSourceSnapshot.Read(p.ToString(),(string)p["packetHash"],"a","LOCAL_B12_RUN");
  static void Repin(JObject p){p.Remove("packetHash");p["packetHash"]=BDirectAttackInput.Digest(p).Substring(7);}
  [Test] public void SuppliedRouteRawBoardingAndDefinitionsStayExactlyOriginal(){var p=First();var s=Read(p);Assert.IsTrue(JToken.DeepEquals(p,s.Capture()));Assert.IsTrue(JToken.DeepEquals(s.Route.NativeNode.Capture(),s.Boarding.OriginalSource.Capture()));Assert.AreEqual("n1-0",s.Route.NativeNode.NodeId);Assert.IsFalse(s.AcceptedTimingSupported);Assert.Throws<InvalidOperationException>(()=>s.RequireAcceptedTiming());}
  [TestCase("accountId")][TestCase("runId")][TestCase("nodeId")][TestCase("nativeContextId")][TestCase("mapRevision")][TestCase("seedHash")]
  public void ExchangedOuterIdentityCannotAuthorizeInnerOriginalNode(string key){var p=First();p[key]=key=="mapRevision"?(JToken)new JValue(999):new JValue("foreign");Repin(p);Assert.Throws<ArgumentException>(()=>Read(p));}
  [Test] public void RawNodeSwapMissingBoardingAndInventedTimingAreRejected(){foreach(string field in new[]{"originalNodeSource","boardingSource","interceptionAcceptedTiming"}){var p=First();p[field]=new JObject();Repin(p);Assert.Throws<ArgumentException>(()=>Read(p));}}
  [Test] public void AuthenticatedNativeBundleNeedsOriginalStrictIntake(){Assert.NotNull(typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Run.BNativeNodeSourceSnapshot"));}
 }
}
#endif
