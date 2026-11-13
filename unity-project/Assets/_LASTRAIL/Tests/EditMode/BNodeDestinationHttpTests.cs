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
 public sealed class BNodeDestinationHttpTests {
  static IEnumerator Wait(Task task){var end=DateTime.UtcNow.AddSeconds(30);while(!task.IsCompleted){Assert.Less(DateTime.UtcNow,end);yield return null;}}
  [UnityTest] public IEnumerator OriginalPublicDestinationOnlyPreservesControllerCommandAndRejectsLateResponse(){
   var path=Environment.GetEnvironmentVariable("LASTRAIL_NODE_CONNECTION");if(string.IsNullOrEmpty(path))Assert.Ignore("Explicit isolated original destination server required");var cfg=JObject.Parse(File.ReadAllText(path));var run=(string)cfg["runId"];var token=new GameApiAccessToken((string)cfg["token"]);
   using(var a=new UnityWebRequestRunApiHttpAdapter((string)cfg["origin"],10))using(var late=new UnityWebRequestRunApiHttpAdapter((string)cfg["delayedOrigin"],10))using(var c=new BNodeSessionController("a",run)){
    var t=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(a));var client=new BOperationDestinationClient(t);int calls=0;c.Bind(_=>calls++);
    var get=t.SendAsync(new RunApiHttpRequest("GET","/v1/runs/"+run+"/operation-map",null,null),token);yield return Wait(get);Assert.AreEqual(200,get.Result.Response.StatusCode);var status=JObject.Parse(get.Result.Response.BodyJson);Assert.AreEqual("operation.map-status.v1",(string)status["version"]);Assert.AreEqual(run,(string)status["runId"]);
    var i=new BOperationDestinationIntent("a",run,"UNITY_NODE_DESTINATION",(long)status["revision"],(string)cfg["edgeId"]);var original=i.Capture();
    var task=c.ConfirmDestinationAsync(i,()=>client.ConfirmAsync(i,token),c.Revision);yield return Wait(task);Assert.IsTrue(task.GetAwaiter().GetResult());Assert.AreEqual(1,calls);
    var replay=client.ConfirmAsync(i,token);yield return Wait(replay);var receipt=replay.GetAwaiter().GetResult();Assert.IsTrue(JToken.DeepEquals(c.Capture()["destination"],receipt.Capture()));Assert.IsFalse(receipt.CanProveArrival);Assert.IsTrue(JToken.DeepEquals(original,i.Capture()));
    var auth=client.ConfirmAsync(i,new GameApiAccessToken("lst1.invalid-node"));yield return Wait(auth);Assert.IsTrue(auth.IsFaulted);StringAssert.Contains("AuthRequired",auth.Exception.InnerException.Message);
    var stale=new BOperationDestinationIntent("a",run,"new-stale",i.ExpectedRevision,i.EdgeId);var error=client.ConfirmAsync(stale,token);yield return Wait(error);Assert.IsTrue(error.IsFaulted);StringAssert.Contains("409",error.Exception.InnerException.Message);
    var conflict=new BOperationDestinationIntent("a",run,i.CommandId,i.ExpectedRevision,"n1-0:n2-1");error=client.ConfirmAsync(conflict,token);yield return Wait(error);Assert.IsTrue(error.IsFaulted);StringAssert.Contains("409",error.Exception.InnerException.Message);
    var dc=new BOperationDestinationClient(new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(late)));task=c.ConfirmDestinationAsync(i,()=>dc.ConfirmAsync(i,token),c.Revision);Assert.AreEqual(1,late.InFlightCount);c.Unbind();yield return Wait(task);Assert.IsFalse(task.GetAwaiter().GetResult());Assert.AreEqual(1,calls);Assert.Throws<InvalidOperationException>(()=>c.RequireAcceptedArrivalOrCompletion());Assert.AreEqual(0,a.InFlightCount);Assert.AreEqual(0,late.InFlightCount);
    File.WriteAllText("D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNodeController-20261005/actual-destination-http.json",new JObject{["evidence"]="ACTUAL_UNITY_HTTP_ORIGINAL_GAME_DESTINATION_ROUTE",["initialStatus"]=status,["intent"]=i.Capture(),["receipt"]=receipt.Capture(),["controller"]=c.Capture(),["callbacks"]=calls,["latePublication"]=false,["acceptedArrival"]=false,["nativeNodeBindingInThisHttpTest"]=false,["inFlightAtEnd"]=0}.ToString());
   }
  }
 }
}
#endif
