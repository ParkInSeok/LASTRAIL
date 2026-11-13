#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BRouteHttpTests {
  static IEnumerator Wait(Task t){var end=DateTime.UtcNow.AddSeconds(30);while(!t.IsCompleted){Assert.Less(DateTime.UtcNow,end);yield return null;}}
  [UnityTest] public IEnumerator OriginalBindingAndActualMutationWireKeepLocalEvidenceAndOperatingRefusalDistinct(){
   var path=Environment.GetEnvironmentVariable("LASTRAIL_B_ROUTE_CONNECTION");if(string.IsNullOrEmpty(path))Assert.Ignore("Explicit isolated original route fixture required");var cfg=JObject.Parse(File.ReadAllText(path));var run=(string)cfg["runId"];var token=new GameApiAccessToken((string)cfg["token"]);
   using(var a=new UnityWebRequestRunApiHttpAdapter((string)cfg["origin"],10))using(var accepted=new UnityWebRequestRunApiHttpAdapter((string)cfg["acceptedOrigin"],10))using(var late=new UnityWebRequestRunApiHttpAdapter((string)cfg["delayedOrigin"],10))using(var c=new BRouteSessionController("a",run)){
    var transport=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(a));var client=new BRouteHttpClient(transport);var local=new BRouteHttpClient(new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(accepted)));int callbacks=0;c.Bind(_=>callbacks++);
    var refresh=c.RefreshAsync(()=>client.ReadAsync("a",run,token));yield return Wait(refresh);Assert.IsTrue(refresh.GetAwaiter().GetResult());Assert.IsNull(c.Route.NativeNode);Assert.AreEqual(1,c.Route.MapRevision);
    var intent=new BOperationDestinationIntent("a",run,"select",1,"entry:n1-0");var destination=new BOperationDestinationClient(transport);var choose=c.ConfirmDestinationAsync(intent,()=>destination.ConfirmAsync(intent,token));yield return Wait(choose);Assert.IsTrue(choose.GetAwaiter().GetResult());Assert.IsTrue((bool)c.Capture()["routeRefreshRequired"]);
    refresh=c.RefreshAsync(()=>client.ReadAsync("a",run,token));yield return Wait(refresh);Assert.IsTrue(refresh.GetAwaiter().GetResult());Assert.AreEqual("n1-0",c.Route.NativeNode.NodeId);Assert.AreNotEqual(run,c.Route.NativeNode.NativeContextId);Assert.AreEqual(JTokenType.Null,c.Capture()["nodeSession"]["runtime"].Type);
    var auth=client.ReadAsync("a",run,new GameApiAccessToken("lst1.invalid-route"));yield return Wait(auth);Assert.IsTrue(auth.IsFaulted);StringAssert.Contains("AuthRequired",auth.Exception.InnerException.Message);
    var stale=client.ReadAsync("a",run,token,new string('0',64));yield return Wait(stale);Assert.IsTrue(stale.IsFaulted);StringAssert.Contains("409",stale.Exception.InnerException.Message);
    var cmd=BRouteCommand.FromOriginal(c.Route,BRouteConsumerTests.Arrival(c.Route));var refused=c.SendAsync(cmd,()=>client.SendAsync(cmd,token));yield return Wait(refused);Assert.IsTrue(refused.GetAwaiter().GetResult());Assert.AreEqual(409,(int)c.Capture()["lastResultStatus"]);Assert.IsFalse((bool)c.Capture()["routeRefreshRequired"]);
    var commit=c.SendAsync(cmd,()=>local.SendAsync(cmd,token));yield return Wait(commit);Assert.IsTrue(commit.GetAwaiter().GetResult());Assert.IsTrue((bool)c.Capture()["routeRefreshRequired"]);var replay=c.SendAsync(cmd,()=>local.SendAsync(cmd,token));yield return Wait(replay);Assert.IsTrue(replay.GetAwaiter().GetResult());
    refresh=c.RefreshAsync(()=>client.ReadAsync("a",run,token));yield return Wait(refresh);Assert.IsTrue(refresh.GetAwaiter().GetResult());Assert.AreEqual(3,c.Route.MapRevision);
    for(int wave=1;wave<=2;wave++){var wc=BRouteCommand.FromOriginal(c.Route,new JObject{["eventId"]="wave"+wave,["nodeId"]="n1-0",["waveOrdinal"]=wave,["expectedMapRevision"]=3,["evidenceRef"]="LOCAL_ACCEPTED_ORIGINAL_REF"},true);var defaultWave=c.SendAsync(wc,()=>client.SendAsync(wc,token));yield return Wait(defaultWave);Assert.IsTrue(defaultWave.GetAwaiter().GetResult());Assert.AreEqual(409,(int)c.Capture()["lastResultStatus"]);commit=c.SendAsync(wc,()=>local.SendAsync(wc,token));yield return Wait(commit);Assert.IsTrue(commit.GetAwaiter().GetResult());Assert.AreEqual(200,(int)c.Capture()["lastResultStatus"]);}
    cmd=BRouteCommand.FromOriginal(c.Route,new JObject{["kind"]="COMPLETE",["eventId"]="COMPLETE_LOCAL",["expectedRevision"]=3,["nodeId"]="n1-0",["evidenceRef"]="LOCAL_ACCEPTED_ORIGINAL_REF"});refused=c.SendAsync(cmd,()=>client.SendAsync(cmd,token));yield return Wait(refused);Assert.IsTrue(refused.GetAwaiter().GetResult());Assert.AreEqual(409,(int)c.Capture()["lastResultStatus"]);commit=c.SendAsync(cmd,()=>local.SendAsync(cmd,token));yield return Wait(commit);Assert.IsTrue(commit.GetAwaiter().GetResult());refresh=c.RefreshAsync(()=>client.ReadAsync("a",run,token));yield return Wait(refresh);Assert.IsTrue(refresh.GetAwaiter().GetResult());Assert.AreEqual(4,c.Route.MapRevision);
    cmd=BRouteCommand.FromOriginal(c.Route,new JObject{["kind"]="RELEASE_HOLD",["eventId"]="RELEASE_LOCAL",["expectedRevision"]=4,["holdId"]="LOCAL_REWARD_HOLD",["evidenceRef"]="LOCAL_ACCEPTED_ORIGINAL_REF"});refused=c.SendAsync(cmd,()=>client.SendAsync(cmd,token));yield return Wait(refused);Assert.IsTrue(refused.GetAwaiter().GetResult());Assert.AreEqual(409,(int)c.Capture()["lastResultStatus"]);commit=c.SendAsync(cmd,()=>local.SendAsync(cmd,token));yield return Wait(commit);Assert.IsTrue(commit.GetAwaiter().GetResult());refresh=c.RefreshAsync(()=>client.ReadAsync("a",run,token));yield return Wait(refresh);Assert.IsTrue(refresh.GetAwaiter().GetResult());Assert.AreEqual(5,c.Route.MapRevision);Assert.IsFalse((bool)c.Capture()["routeRefreshRequired"]);
    var delayed=new BRouteHttpClient(new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(late)));refresh=c.RefreshAsync(()=>delayed.ReadAsync("a",run,token));Assert.AreEqual(1,late.InFlightCount);int before=callbacks;c.Unbind();yield return Wait(refresh);Assert.IsFalse(refresh.GetAwaiter().GetResult());Assert.AreEqual(before,callbacks);Assert.AreEqual(0,a.InFlightCount);Assert.AreEqual(0,accepted.InFlightCount);Assert.AreEqual(0,late.InFlightCount);
    File.WriteAllText("D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BRouteConsumer-20261005/actual-route-http.json",new JObject{["evidence"]="ACTUAL_UNITY_HTTP_ORIGINAL_ROUTES_LOCAL_SQL_EVIDENCE_OWNER",["controller"]=c.Capture(),["callbacks"]=callbacks,["latePublication"]=false,["operatingEvidenceProvider"]=false,["nativeRuntimeAttached"]=false,["inFlightAtEnd"]=0}.ToString());
   }
  }
 }
}
#endif
