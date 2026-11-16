using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Resources;
using LastRail.Run;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace LastRail.Tests.EditMode
{
 public sealed class LobbyResourceSessionLifetimeTests
 {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  sealed class Wire:IRunApiHttpTransport
  {
   public int Requests;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests++;return Task.FromResult(RunApiHttpResponse.NetworkError("offline fixture"));}
  }
  [TestCase(false)][TestCase(true)] public async Task SameRuntimeSilentSessionChangeRejectsDelayedAssetsAndReleasesLeases(bool page)
  {
   var installed=typeof(PreparedContent).GetField("current",BindingFlags.Static|BindingFlags.NonPublic);var previous=installed.GetValue(null);
   var pending=new Dictionary<string,TaskCompletionSource<PreparedContent.AssetLease>>();int loads=0,releases=0,builds=0;
   using var resources=new PreparedContent.Session(key=>{loads++;Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Object>(RemoteContentCatalog.AssetPath(key)),key);var completion=new TaskCompletionSource<PreparedContent.AssetLease>();pending.Add(key,completion);return completion.Task;});
   var wire=new Wire();var auth=SupportInquiryTests.Session();long generation=1;using var account=new AccountScreenRuntime(wire,null,()=>auth,()=>generation);account.SynchronizeSession();
   var root=new GameObject("ResourceSessionLifetime",typeof(RectTransform),typeof(Canvas));Task opening=null;LobbyDestinationPages pages=null;LobbyCommandDeck host=null;
   try
   {
    var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    // Host chrome is borrowed before the isolated tracked asset session is installed.
    if(page){pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.BindAccount(account);}
    else{((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;host=LobbyCommandDeck.Attach(root,font,"QA",0,Array.Empty<MissionShellMissionRowState>(),null,null);host.BindAccount(account);}
    int reads=wire.Requests;installed.SetValue(null,resources);
    opening=page?(Task)typeof(LobbyDestinationPages).GetMethod("OpenPreparedPageAsync",Private).Invoke(pages,new object[]{LobbyPage.Formation,false}):(Task)typeof(LobbyCommandDeck).GetMethod("OpenResourceDialogAsync",Private).Invoke(host,new object[]{new[]{"EntryUi/primary"},(Action)(()=>builds++)});
    Assert.Greater(pending.Count,0);Assert.IsFalse(opening.IsCompleted);
    auth=SupportInquiryTests.Session("other");generation++;
    // Deliberately do not synchronize or rebind: the await continuation itself owns this guard.
    foreach(var pair in pending)pair.Value.SetResult(new PreparedContent.AssetLease(AssetDatabase.LoadAssetAtPath<Object>(RemoteContentCatalog.AssetPath(pair.Key)),()=>releases++));
    await opening;
    if(page){Assert.IsFalse(pages.IsOpen);Assert.AreEqual(LobbyPage.Operators,pages.CurrentPage,"No page builder may run for the former session.");}
    else{Assert.AreEqual(0,builds);Assert.IsFalse(await (Task<bool>)opening);Assert.IsFalse(host.IsDialogOpen);}
    Assert.AreEqual(reads,wire.Requests);Assert.AreEqual(loads,releases,"Every lease acquired by the rejected opening must be returned.");
   }
   finally{try{foreach(var pair in pending)if(!pair.Value.Task.IsCompleted)pair.Value.TrySetResult(new PreparedContent.AssetLease(AssetDatabase.LoadAssetAtPath<Object>(RemoteContentCatalog.AssetPath(pair.Key)),()=>releases++));if(opening!=null)await opening;}finally{installed.SetValue(null,previous);Object.DestroyImmediate(root);}}
  }
 }
}
