#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {public sealed class BSameRunSourceHttpTests {
 const string Qa="D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BSameRunSource-20261006/";
 static IEnumerator Wait(Task t){var limit=DateTime.UtcNow.AddSeconds(60);while(!t.IsCompleted){Assert.Less(DateTime.UtcNow,limit);yield return null;}}
 [UnityTest,Timeout(240000)] public IEnumerator OriginalSameRunBundleHttpRestoreReauthAndUnbindLifetime(){
  var file=Environment.GetEnvironmentVariable("LASTRAIL_SAME_RUN_SOURCE_CONNECTION");if(string.IsNullOrEmpty(file))Assert.Ignore("Explicit disposable original GAME/SQL fixture required");var connection=JObject.Parse(File.ReadAllText(file));string run=(string)connection["runId"];var token=new GameApiAccessToken((string)connection["token"]);
  using(var adapter=new UnityWebRequestRunApiHttpAdapter((string)connection["origin"],30))using(var c=new BRouteSessionController("a",run)){
   var raw=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter));var client=new BDepartureSourceBundleHttpClient(raw);
   var before=c.Capture();var early=c.ReadDepartureSourceBundleAsync(client,token);yield return Wait(early);Assert.IsTrue(early.GetAwaiter().GetResult());Assert.IsNull(c.DepartureSourceBundle.NativeNode);Assert.That(c.DepartureSourceBundle.MissingSources,Does.Contain("SELECTED_COMBAT_NODE"));Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));var assignmentHash=c.DepartureAssignment.PacketHash;
   var choose=raw.SendAsync(new RunApiHttpRequest("GET","/__qa_choose",null,null),token);yield return Wait(choose);Assert.AreEqual(200,choose.GetAwaiter().GetResult().Response.StatusCode);
   var selected=c.ReadDepartureSourceBundleAsync(client,token);yield return Wait(selected);Assert.IsTrue(selected.GetAwaiter().GetResult());var source=c.DepartureSourceBundle;Assert.NotNull(source.NativeNode);Assert.AreEqual(assignmentHash,c.DepartureAssignment.PacketHash);Assert.AreSame(source.Assignment,c.DepartureAssignment);c.PrepareDepartureFormation().RequireSource(source.Assignment);
   var bind=c.RefreshAsync(()=>Task.FromResult(source.NativeNode.Route));yield return Wait(bind);Assert.IsTrue(bind.GetAwaiter().GetResult());Assert.IsNull(c.Capture()["nodeSession"]["runtime"] as JObject);var savedController=c.Capture();var savedSource=source.Capture();
   var remove=raw.SendAsync(new RunApiHttpRequest("GET","/__qa_remove_current_sources",null,null),token);yield return Wait(remove);Assert.AreEqual(200,remove.GetAwaiter().GetResult().Response.StatusCode);var same=client.ReadAsync("a",run,token,source.PacketHash);yield return Wait(same);Assert.IsTrue(JToken.DeepEquals(savedSource,same.GetAwaiter().GetResult().Capture()));
   var errors=new JArray();var stale=client.ReadAsync("a",run,token,new string('0',64));yield return Wait(stale);var staleError=stale.Exception.GetBaseException() as BResultReportHttpException;Assert.NotNull(staleError);Assert.AreEqual(409,staleError.StatusCode);errors.Add(new JObject{["status"]=409,["code"]=staleError.ErrorCode});
   foreach(var test in new[]{("version",409),("query",400)}){var path="/v1/runs/"+run+"/b-departure-source"+(test.Item1=="query"?"?contextId=fake":"");var headers=new[]{new RunApiHttpHeader("X-B-Departure-Source-Version",test.Item1=="version"?"old":BDepartureSourceBundle.Version)};var send=raw.SendAsync(new RunApiHttpRequest("GET",path,null,headers),token);yield return Wait(send);Assert.AreEqual(test.Item2,send.GetAwaiter().GetResult().Response.StatusCode);errors.Add(new JObject{["case"]=test.Item1,["status"]=test.Item2});}
   var wrong=client.ReadAsync("foreign",run,token);yield return Wait(wrong);Assert.IsInstanceOf<ArgumentException>(wrong.Exception.GetBaseException());
   var revoke=raw.SendAsync(new RunApiHttpRequest("GET","/__qa_revoke",null,null),token);yield return Wait(revoke);Assert.AreEqual(200,revoke.GetAwaiter().GetResult().Response.StatusCode);var denied=client.ReadAsync("a",run,token);yield return Wait(denied);var auth=denied.Exception.GetBaseException() as BResultReportHttpException;Assert.NotNull(auth);Assert.AreEqual(401,auth.StatusCode);Assert.IsTrue(auth.RequiresAuthentication);errors.Add(new JObject{["status"]=401,["code"]=auth.ErrorCode});
   var reauth=raw.SendAsync(new RunApiHttpRequest("GET","/__qa_reauthenticate",null,null),token);yield return Wait(reauth);Assert.AreEqual(200,reauth.GetAwaiter().GetResult().Response.StatusCode);token=new GameApiAccessToken((string)JObject.Parse(File.ReadAllText(file))["token"]);var rechecked=client.ReadAsync("a",run,token,source.PacketHash);yield return Wait(rechecked);Assert.IsTrue(JToken.DeepEquals(savedSource,rechecked.GetAwaiter().GetResult().Capture()));
   using(var restored=BRouteSessionController.Restore(savedController,BDirectAttackInput.Digest(savedController),null,"a",run)){
    var frozen=BDepartureSourceBundle.Read(savedSource.ToString(),source.PacketHash,"a",run);var load=restored.ReadDepartureSourceBundleAsync(_=>Task.FromResult(frozen));yield return Wait(load);Assert.IsTrue(load.GetAwaiter().GetResult());Assert.IsTrue(JToken.DeepEquals(savedController,restored.Capture()));restored.PrepareDepartureFormation().RequireSource(restored.DepartureSourceBundle.Assignment);
    int callbacks=0;restored.Bind(_=>callbacks++);var delay=raw.SendAsync(new RunApiHttpRequest("GET","/__qa_delay_next",null,null),token);yield return Wait(delay);Assert.AreEqual(200,delay.GetAwaiter().GetResult().Response.StatusCode);var canceled=restored.ReadDepartureSourceBundleAsync(client,token,source.PacketHash);restored.Unbind();yield return Wait(canceled);Assert.IsFalse(canceled.GetAwaiter().GetResult());Assert.AreEqual(0,callbacks);Assert.AreEqual(source.PacketHash,restored.DepartureSourceBundle.PacketHash);
    var retry=restored.ReadDepartureSourceBundleAsync(client,token,source.PacketHash);yield return Wait(retry);Assert.IsTrue(retry.GetAwaiter().GetResult());Assert.IsTrue(JToken.DeepEquals(savedSource,restored.DepartureSourceBundle.Capture()));Assert.IsTrue(JToken.DeepEquals(savedController,restored.Capture()));
   }
   File.WriteAllText(Qa+"actual-http-same-run-source.json",new JObject{["packet"]=savedSource,["controller"]=savedController,["beforeSelectionNullNode"]=true,["sourceDeletedRestoreUnchanged"]=true,["sameRunAssignmentNodeContextProfile"]=true,["reAuthenticationNewLocalSession"]=true,["controllerAndSourceRestore"]=true,["unboundReadNotPublishedOrNotified"]=true,["sameRequestAfterCancellation"]=true,["errors"]=errors,["accountMismatchRejected"]=true,["runtimeFactoryAttached"]=false,["operatingStart"]=false,["source"]= "FRESH_ORIGINAL_PERSISTED_BUILDER_WITH_SYNTHETIC_ACCOUNT_SEED_AND_LOCAL_PROFILE"}.ToString());
  }
 }
}}
#endif
