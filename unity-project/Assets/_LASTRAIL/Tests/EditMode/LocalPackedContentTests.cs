using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class LocalPackedContentTests {
  [Test] public void MissingBuiltSettingsNeverUseProjectAssets(){
   Assert.ThrowsAsync<InvalidOperationException>(async()=>await EditorProjectContentBridge.InitializeBuiltAsync(Path.GetFullPath("Library/LocalPackedContent/missing/runtime-output/settings.json"),CancellationToken.None));
  }
  [UnityTest] public IEnumerator BuiltOnlyDownloadCacheAndRelease(){
   yield return new EnterPlayMode();var task=Verify();while(!task.IsCompleted)yield return null;var error=task.Exception;yield return new ExitPlayMode();if(error!=null)throw error;
  }
  static async Task Verify(){
   Assert.IsNull(PreparedContent.EditorAsset,"Play mode must not use the authoring adapter.");
   var prior=Caching.currentCacheForWriting;var directory=Path.GetFullPath("../LocalTemp/local-packed-cache-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);var cache=Caching.AddCache(directory);Caching.currentCacheForWriting=cache;
   var policy=new DownloadAccessPolicy();policy.SetNetwork(ContentNetwork.Wifi);
   using var backend=new AddressableContentBackend(null,true,policy,source:ContentResourceSource.EditorLocal);
   var gate=typeof(ClientContentGate).GetMethod("Approved",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"test","test","https://unused.invalid/catalog.bin","sha256:"+new string('0',64),false});
   typeof(AddressableContentBackend).GetField("approvedGate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(backend,gate);
   using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(8));
   try{
    using(var publicOwner=PreparedContent.Install(backend)){await PublicEntryTexture.IsCachedAsync(timeout.Token);}
    await backend.InitializeAsync(timeout.Token);await backend.UpdateCatalogAsync(timeout.Token);
    var bytes=await backend.RequiredBytesAsync(timeout.Token);Assert.GreaterOrEqual(bytes,0);Debug.Log("LOCAL_PACKED_TEST bytes="+bytes);
    var key=RemoteContentCatalog.Find("TrainFortress/train-overview").Address;
    var size=Addressables.GetDownloadSizeAsync(key);await size.Task;try{if(size.Result>0)Assert.Throws<InvalidOperationException>(()=>backend.ResolveApprovedTexture(key));}finally{Addressables.Release(size);}
    policy.SetPaused(true);using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();var pending=backend.DownloadAsync((a,b)=>{},cancelled.Token);bool stopped=false;try{await pending;}catch(OperationCanceledException){stopped=true;}Assert.IsTrue(stopped);}policy.SetPaused(false);
    await backend.DownloadAsync((a,b)=>{},timeout.Token);Assert.AreEqual(0,await backend.RequiredBytesAsync(timeout.Token));Debug.Log("LOCAL_PACKED_TEST cache ready");
    using(var resources=PreparedContent.Install(backend)){
     for(int repeat=0;repeat<2;repeat++){
      using(var portraits=await PreparedContent.PrepareAsync(new[]{"FormationPlan/portrait-reina","FormationPlan/portrait-kyle","FormationPlan/portrait-eve","FormationPlan/portrait-sera","FormationPlan/portrait-nyx"},timeout.Token)){
       foreach(var id in new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"}){
        var face=LastRail.Presentation.MissionShell.FormationPlanView.Portrait(id);Assert.NotNull(face,id);Assert.Greater(face.width,100,id);
       }
      }
      Assert.Throws<InvalidOperationException>(()=>LastRail.Presentation.MissionShell.FormationPlanView.Portrait("op_nyx"));
     }
     var scope=await PreparedContent.PrepareAsync(new[]{"TrainFortress/"},timeout.Token);var train=PreparedContent.Load<Texture2D>("TrainFortress/train-overview");Assert.NotNull(train);Assert.Greater(train.width,1000);scope.Dispose();
     Assert.Throws<InvalidOperationException>(()=>PreparedContent.Load<Texture2D>("TrainFortress/train-overview"));
     var again=await PreparedContent.PrepareAsync(new[]{"TrainFortress/"},timeout.Token);Assert.NotNull(PreparedContent.Load<Texture2D>("TrainFortress/train-overview"));again.Dispose();
    }
    // A cached reopen must succeed without a running origin server.
    Type.GetType("LastRail.Editor.LocalPackedContentServer, LastRail.Editor").GetMethod("Stop").Invoke(null,null);
    await backend.DownloadAsync((a,b)=>{},timeout.Token);Assert.AreEqual(0,await backend.RequiredBytesAsync(timeout.Token));Debug.Log("LOCAL_PACKED_TEST cache ready");
    var handle=Addressables.LoadAssetAsync<Texture2D>(backend.ResolveApprovedTexture(key));await handle.Task;Assert.NotNull(handle.Result);Addressables.Release(handle);Assert.IsFalse(handle.IsValid());
    File.WriteAllText(Path.GetFullPath("../QA/ResourceIntake-20261110/connection/train-fortress-v01/local-packed-result.json"),"{\"initialMissingBytes\":"+bytes+",\"cachedRemainingBytes\":0,\"originStoppedCacheLoad\":true,\"cancelReentryRelease\":true}");
   }finally{Caching.currentCacheForWriting=prior;Caching.RemoveCache(cache);}
  }
 }
}
