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
 public sealed class EquipmentSafeTransitionHttpTests {
  const string Qa="D:/Project/LASTRAIL/QA/CombatBMigration-20261004/SafeTransitionConsumer-20261005/";
  static IEnumerator Wait(Task task){var end=DateTime.UtcNow.AddSeconds(30);while(!task.IsCompleted){Assert.Less(DateTime.UtcNow,end);yield return null;}}
  [UnityTest] public IEnumerator ActualOriginalTcpDiagnosticPreservesRetryAndRejectsAuthErrorsAndLatePublication(){
   var path=Environment.GetEnvironmentVariable("LASTRAIL_SAFE_CONNECTION");if(string.IsNullOrEmpty(path))Assert.Ignore("Explicit original isolated TCP harness required");var config=JObject.Parse(File.ReadAllText(path));var token=new GameApiAccessToken((string)config["token"]);var run=(string)config["runId"];
   using(var a=new UnityWebRequestRunApiHttpAdapter((string)config["origin"],10))using(var delayed=new UnityWebRequestRunApiHttpAdapter((string)config["delayedOrigin"],10)){
    var t=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(a));var client=new EquipmentSafeTransitionHttpClient(t);
    var ct=client.CaptureAsync("a",run,token);yield return Wait(ct);var c=ct.GetAwaiter().GetResult();var i=c.CreateIntent("UNITY_DIAGNOSTIC","ENTER","BETWEEN_NODES");var originalRequest=i.Capture();
    var task=client.VerifyAsync(i,token);yield return Wait(task);var r=task.GetAwaiter().GetResult();Assert.IsFalse(r.CanAuthorizeMaintenance);Assert.IsFalse((bool)r.Capture()["accepted"]);
    task=client.VerifyAsync(i,token);yield return Wait(task);Assert.IsTrue(JToken.DeepEquals(r.Capture(),task.GetAwaiter().GetResult().Capture()));Assert.IsTrue(JToken.DeepEquals(originalRequest,i.Capture()));
    ct=client.CaptureAsync("a",run,token);yield return Wait(ct);Assert.AreEqual(c.Hash,ct.GetAwaiter().GetResult().Hash);
    var bad=client.CaptureAsync("a",run,new GameApiAccessToken("lst1.invalid-safe-transition"));yield return Wait(bad);Assert.IsTrue(bad.IsFaulted);StringAssert.Contains("AuthRequired",bad.Exception.InnerException.Message);
    var observations=new JArray();foreach(var mode in new[]{"version","key","query","paused","changed-command"}){
     var body=i.Capture();if(mode=="paused")body["paused"]=true;if(mode=="changed-command")body["reason"]="SAFE_EVENT";
     var request=new RunApiHttpRequest("POST","/v1/runs/"+run+"/equipment-safe-transition/verify"+(mode=="query"?"?bad=1":""),body.ToString(),new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("X-Equipment-Safe-Transition-Version",mode=="version"?"old":EquipmentSafeTransitionContext.Version),new RunApiHttpHeader("Idempotency-Key",mode=="key"?"wrong":i.CommandId)});
     var raw=t.SendAsync(request,token);yield return Wait(raw);var response=raw.GetAwaiter().GetResult().Response;Assert.AreEqual(mode=="version"||mode=="changed-command"?409:400,response.StatusCode);observations.Add(new JObject{["case"]=mode,["status"]=response.StatusCode,["body"]=JObject.Parse(response.BodyJson)});
    }
    using(var o=new EquipmentSafeTransitionOwner()){int calls=0;o.Bind("a",run,_=>calls++);var first=o.PublishAsync(i,()=>Task.FromResult(r));yield return Wait(first);Assert.IsTrue(first.Result);var dc=new EquipmentSafeTransitionHttpClient(new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(delayed)));var late=o.PublishAsync(i,()=>dc.VerifyAsync(i,token));Assert.AreEqual(1,delayed.InFlightCount);o.Unbind();yield return Wait(late);Assert.IsFalse(late.GetAwaiter().GetResult());Assert.AreEqual(1,calls);}
    Assert.AreEqual(0,a.InFlightCount);Assert.AreEqual(0,delayed.InFlightCount);
    File.WriteAllText(Qa+"actual-unity-transition.json",new JObject{["evidence"]="ACTUAL_UNITY_WEBREQUEST_ORIGINAL_AUTH_SQL_LOOPBACK",["context"]=c.Capture(),["request"]=i.Capture(),["receipt"]=r.Capture(),["checks"]=observations,["exactReplay"]=true,["latePublication"]=false,["canAuthorizeMaintenance"]=false,["inFlightAtEnd"]=0}.ToString());
   }
  }
 }
}
#endif
