using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Auth;
using LastRail.Presentation.AugmentSelection;
using UnityEngine;
using TMPro;
using System.Reflection;
using System.Threading.Tasks;
namespace LastRail.Tests.EditMode {
 public class BRouteArrivalResponseTests {
  // Synthetic AUGMENT route envelopes; never evidence of published/admitted support.
  internal static BRouteCommand Command(){var p=(JObject)BRouteConsumerTests.Sample()["pendingBinding"];p["pendingNode"]["eventKind"]="AUGMENT";p.Remove("packetHash");p["packetHash"]=LastRail.Battle.BDirectAttackInput.Digest(p).Substring(7);var b=BRouteConsumerTests.Read(p);return BRouteCommand.FromOriginal(b,BRouteConsumerTests.Arrival(b));}
  static JObject Active(){var p=Command().Binding.Capture();p["currentNode"]=p["pendingNode"].DeepClone();p["pendingNode"]=null;p["pendingEdgeId"]=null;p["phase"]="ACTIVE";p["mapRevision"]=Packet()["receipt"]["revision"].DeepClone();p.Remove("packetHash");p["packetHash"]=LastRail.Battle.BDirectAttackInput.Digest(p).Substring(7);return p;}
  internal static JObject Packet()=>new JObject{["version"]=BRouteBinding.Version,["committed"]=true,["receipt"]=BRouteConsumerTests.Sample()["localAccepted"]["arrival"].DeepClone()};
  internal static JObject Grant(BRouteCommand c)=>new JObject{["runId"]=c.Binding.RunId,["eventId"]="grant",["stageId"]=c.Binding.NativeNode.NodeId,["catalogVersion"]=CombatO22ApplicationOriginal.CatalogVersion,["grantRevision"]=0,["state"]="SELECTING",["slots"]=new JArray(new JObject{["slotIndex"]=0,["candidateId"]="O22",["freeRemaining"]=3,["extraRemaining"]=1}),["selectedCandidateId"]=null};
  [Test]public void ArrivalSuccessCarriesGrantWithoutAnotherSelectionRead(){var c=Command();var p=Packet();p["augment"]=Grant(c);var r=BRouteCommandResult.Read(200,p.ToString(),c);Assert.IsTrue(r.Committed);Assert.IsTrue(JToken.DeepEquals(p,r.Capture()));}
  [Test]public async System.Threading.Tasks.Task IssuanceFailureStillCommitsArrivalAndFencesOldMap(){var c=Command();var p=Packet();p["augmentPending"]=true;p["errorCode"]="augment_catalog_source_required";var r=BRouteCommandResult.Read(503,p.ToString(),c);Assert.IsTrue(r.Committed);using(var owner=new BRouteSessionController(c.Binding.AccountId,c.Binding.RunId)){await owner.RefreshAsync(()=>System.Threading.Tasks.Task.FromResult(c.Binding));await owner.SendAsync(c,()=>System.Threading.Tasks.Task.FromResult(r));Assert.IsTrue((bool)owner.Capture()["routeRefreshRequired"]);}}
  [Test]public void NoAwardCarriesItsLaterCompletionRevision(){var c=Command();var p=Packet();var receipt=(JObject)p["receipt"].DeepClone();receipt["eventId"]="node:no-award";receipt["revision"]=(long)receipt["revision"]+1;receipt["state"]["phase"]="READY";receipt["verificationId"]="augment-no-award:"+CombatO22ApplicationOriginal.CatalogVersion;p["augment"]=new JObject{["outcome"]="NO_AWARD",["catalogVersion"]=CombatO22ApplicationOriginal.CatalogVersion,["mapReceipt"]=receipt};var r=BRouteCommandResult.Read(200,p.ToString(),c);var prop=typeof(BRouteCommandResult).GetProperty("RequiredMapRevision");Assert.NotNull(prop);Assert.AreEqual((long)receipt["revision"],prop.GetValue(r));}
  [Test]public void CrossNodeGrantAndMixedPendingPayloadAreRejected(){var c=Command();var p=Packet();p["augment"]=Grant(c);p["augment"]["stageId"]="another-node";Assert.Catch(()=>BRouteCommandResult.Read(200,p.ToString(),c));p["augment"]=Grant(c);p["augmentPending"]=true;p["errorCode"]="failure";Assert.Catch(()=>BRouteCommandResult.Read(503,p.ToString(),c));}
  sealed class ArrivalWire:IGameApiAuthenticatedHttpTransport {
   internal int Posts,Reads;internal bool Pending=true,Confirmed;internal string OriginalBody;internal Action OnPost;
   public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){
    if(request.Method=="GET"){Reads++;var b=Active();return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,b.ToString(),new[]{new RunApiHttpHeader("ETag",(string)b["packetHash"])})));}
    Posts++;if(OriginalBody==null)OriginalBody=request.BodyJson;else Assert.AreEqual(OriginalBody,request.BodyJson,"Recovery must use the same arrival request");var p=Packet();if(Pending){p["augmentPending"]=true;p["errorCode"]="augment_catalog_source_required";}else {p["augment"]=Grant(Command());if(Confirmed){p["augment"]["state"]="CONFIRMED";p["augment"]["selectedCandidateId"]="O22";p["augment"]["grantRevision"]=1;}}OnPost?.Invoke();return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(Pending?503:200,p.ToString())));
   }
  }
  [Test]public async Task SceneEntryRecoversOriginalArrivalAndOpensHostWithoutSelectionGet(){
   var method=typeof(BRouteAugmentScreen).GetMethod("ArriveAsync");Assert.NotNull(method,"Production scene entry caller missing");
   var cmd=Command();var wire=new ArrivalWire();var root=new GameObject("Arrival host",typeof(RectTransform),typeof(Canvas));
   try{using(var c=new BRouteSessionController(cmd.Binding.AccountId,cmd.Binding.RunId)){
    await c.RefreshAsync(()=>Task.FromResult(cmd.Binding));var host=root.AddComponent<BRouteAugmentScreen>();var auth=new GameApiAuthSessionSnapshot(cmd.Binding.AccountId,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
    var crypto=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(global::CombatO22StoreTests).GetNestedType("TestCrypto",BindingFlags.NonPublic),true);string path="D:/Project/LASTRAIL/LocalTemp/arrival-scene-"+Guid.NewGuid().ToString("N");
    var store=new CombatO22ApplicationStore(path+".original",()=>null,_=>{});var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    var args=new object[]{c,cmd,new AugmentSelectionOwner(cmd.Binding.AccountId,"TEST"),wire,new Func<GameApiAuthSessionSnapshot>(()=>auth),new Func<bool>(()=>true),crypto,path,store,new Func<string,string,AugmentCardPresentation>((v,id)=>new AugmentCardPresentation(id,"시험","시험 정의",null)),font};
    wire.OnPost=()=>root.SetActive(false);try{await (Task)method.Invoke(host,args);Assert.Fail("Closed owner must reject the response");}catch(OperationCanceledException){}
    Assert.AreEqual(JTokenType.Null,c.Capture()["lastResult"].Type,"Closed screen must not publish the old response into the borrowed owner");root.SetActive(true);wire.OnPost=null;wire.Posts=0;
    await (Task)method.Invoke(host,args);Assert.IsNull(host.View);Assert.AreEqual(0,wire.Reads);
    wire.Pending=false;await (Task)method.Invoke(host,args);Assert.NotNull(host.View);Assert.AreEqual(2,wire.Posts);Assert.AreEqual(1,wire.Reads);
    host.Close();await (Task)method.Invoke(host,args);Assert.NotNull(host.View);Assert.AreEqual(2,wire.Posts);Assert.AreEqual(1,wire.Reads,"Retained route and grant must be reused on reentry");
   }}finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test]public async Task ConfirmedArrivalWithLostSelectResponseKeepsOriginalRequestReachable(){
   var cmd=Command();var wire=new ArrivalWire{Pending=false,Confirmed=true};var root=new GameObject("Recovery host",typeof(RectTransform),typeof(Canvas));
   try{using(var c=new BRouteSessionController(cmd.Binding.AccountId,cmd.Binding.RunId)){
    await c.RefreshAsync(()=>Task.FromResult(cmd.Binding));var host=root.AddComponent<BRouteAugmentScreen>();var auth=new GameApiAuthSessionSnapshot(cmd.Binding.AccountId,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
    var crypto=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(global::CombatO22StoreTests).GetNestedType("TestCrypto",BindingFlags.NonPublic),true);string path="D:/Project/LASTRAIL/LocalTemp/arrival-select-recovery-"+Guid.NewGuid().ToString("N");var owner=new AugmentSelectionOwner(cmd.Binding.AccountId,"TEST");var scope=new AugmentSelectionScope(owner,cmd.Binding.RunId,"grant");var pending=AugmentSelectionCommand.Create(scope,"SELECT",0,null,AugmentSelectionSnapshot.Parse(Grant(cmd).ToString()));var journal=new AugmentSelectionFileJournal(scope,crypto,path);journal.SaveNew(scope.StorageKey,pending.Serialized);
    var store=new CombatO22ApplicationStore(path+".original",()=>null,_=>{});var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    await host.ArriveAsync(c,cmd,owner,wire,()=>auth,()=>true,crypto,path,store,(v,id)=>new AugmentCardPresentation(id,"시험","시험 정의",null),font);
    Assert.NotNull(host.View);Assert.AreEqual(pending.Serialized,journal.Load(scope.StorageKey));Assert.AreEqual(1,wire.Posts);
   }}finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
