using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace LastRail.Tests.EditMode
{
 public sealed class LobbyHostLifetimeTests
 {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  sealed class Wire:IRunApiHttpTransport
  {
   public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
   public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply=r=>Task.FromResult(RunApiHttpResponse.NetworkError("offline fixture"));
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Reply(request);}
  }
  sealed class Journal:IAccountActionJournal,IAccountAdActionJournal
  {
   public PendingAccountAction Row;public PendingAccountAction Read(string id)=>Row;
   public bool Save(PendingAccountAction row){Row=row;return true;}public bool Remove(PendingAccountAction row){Row=null;return true;}
   public bool ReplaceAd(PendingAccountAction old,PendingAccountAction next){Row=next;return true;}
  }
  static object Field(LobbyCommandDeck host,string name)=>typeof(LobbyCommandDeck).GetField(name,Private).GetValue(host);
  static void Set(LobbyCommandDeck host,string name,object value)=>typeof(LobbyCommandDeck).GetField(name,Private).SetValue(host,value);
  static void Call(LobbyCommandDeck host,string name,params object[] args)=>typeof(LobbyCommandDeck).GetMethod(name,Private).Invoke(host,args);
  static LobbyCommandDeck Create(GameObject root)
  {
   ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
   var rect=(RectTransform)safe.transform;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
   var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
   return LobbyCommandDeck.Attach(root,font,"QA",0,Array.Empty<MissionShellMissionRowState>(),null,null);
  }
  static async Task Drain(){for(int i=0;i<4;i++)await Task.Yield();}
  [Test] public void SameRuntimeSessionChangeClosesExistingDialog()
  {
   var wire=new Wire();var session=SupportInquiryTests.Session();long generation=1;using var account=new AccountScreenRuntime(wire,null,()=>session,()=>generation);account.SynchronizeSession();
   var root=new GameObject("HostSession",typeof(RectTransform),typeof(Canvas));
   try{var host=Create(root);host.BindAccount(account);host.OpenDepartureRecovery("run_old");Assert.IsTrue(host.IsDialogOpen);
    session=SupportInquiryTests.Session("other");generation++;account.SynchronizeSession();Assert.IsFalse(host.IsDialogOpen);Assert.IsNull(Field(host,"dialog"));
   }finally{Object.DestroyImmediate(root);}
  }
  [Test] public async Task PreparationContinuationRejectsSessionChangeBeforeRuntimeNotification()
  {
   var wire=new Wire();var session=SupportInquiryTests.Session();long generation=1;using var account=new AccountScreenRuntime(wire,null,()=>session,()=>generation);account.SynchronizeSession();
   var root=new GameObject("HostPreparation",typeof(RectTransform),typeof(Canvas));var pending=new TaskCompletionSource<bool>();Func<bool> entry=null;
   try{var host=Create(root);host.BindAccount(account);host.BindOrdinaryPreparationWithOwner((run,current)=>{Assert.AreEqual("run_old",run);entry=current;return pending.Task;});host.OpenDepartureRecovery("run_old");
    root.GetComponentsInChildren<Button>().Single(b=>b.name=="PrepareOrdinaryCombat").onClick.Invoke();Assert.IsNotNull(entry);Assert.IsTrue(entry());
    session=SupportInquiryTests.Session("other");generation++;Assert.IsFalse(entry(),"Session owner predicate must invalidate before Changed closes the dialog.");
    pending.SetResult(true);await Drain();Assert.AreNotEqual("Ready",Field(host,"ordinaryDisplay").ToString());Assert.IsFalse((bool)Field(host,"ordinaryPreparationBusy"));
   }finally{pending.TrySetResult(true);Object.DestroyImmediate(root);}
  }
  [Test] public async Task ClosingOfflineDialogBlocksDelayedRecoveryRead()
  {
   var gate=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=r=>r.EndpointPath=="/v1/ad-skip"?gate.Task:Task.FromResult(RunApiHttpResponse.NetworkError("offline fixture"))};
   var session=SupportInquiryTests.Session();var journal=new Journal{Row=new PendingAccountAction(session.AccountId,"ad-create","",0,"original_key")};using var account=new AccountScreenRuntime(wire,journal,()=>session,()=>1);account.SynchronizeSession();
   var root=new GameObject("HostOffline",typeof(RectTransform),typeof(Canvas));
   try{var host=Create(root);Set(host,"accountState",account);Call(host,"ShowInfo","운행 보급","",null);Set(host,"offlineDialog",true);Call(host,"RefreshOffline");
    Assert.IsTrue(account.HasPendingAd);Assert.IsTrue(wire.Requests.Any(r=>r.EndpointPath=="/v1/ad-skip"));host.CloseDialog();int before=wire.Requests.Count;
    gate.SetResult(RunApiHttpResponse.NetworkError("offline fixture"));await Drain();Assert.AreEqual(before,wire.Requests.Count);Assert.IsFalse(wire.Requests.Any(r=>r.EndpointPath.StartsWith("/v1/offline-rewards/ad-intents")));
   }finally{gate.TrySetResult(RunApiHttpResponse.NetworkError("closed fixture"));Object.DestroyImmediate(root);}
  }
  [Test] public async Task OlderDepartureRefreshCannotOverwriteSupportNoticeOrStartAnotherRead()
  {
   var first=new TaskCompletionSource<RunApiHttpResponse>();int accounts=0;var wire=new Wire{Reply=r=>r.EndpointPath=="/v1/account/state"?(++accounts==1?first.Task:Task.FromResult(RunApiHttpResponse.Success(200,AccountStateReadTests.Json()))):r.EndpointPath=="/__local/ce03/support-loadout"?Task.FromResult(RunApiHttpResponse.Success(200,"{\"selection\":{\"revision\":1,\"slots\":[]}}")):Task.FromResult(RunApiHttpResponse.NetworkError("offline fixture"))};
   var session=SupportInquiryTests.Session();using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);account.SynchronizeSession();var root=new GameObject("HostDepartureReads",typeof(RectTransform),typeof(Canvas));
   try{var host=Create(root);Set(host,"accountState",account);Set(host,"localCommandClient",new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),session.AccountId,wire,new GameApiAccessToken("lst1.fixture"),raw=>{}));
    Call(host,"OpenDepartureQuotePrepared","opn_01_last_departure","QA",0);Call(host,"ReadDepartureForDisplay",false);await Drain();
    Assert.AreEqual(1,accounts,"Shared account owner coalesces overlapping reads.");Assert.AreEqual(0,wire.Requests.Count(r=>r.EndpointPath=="/__local/ce03/support-loadout"));
    first.SetResult(RunApiHttpResponse.Success(200,AccountStateReadTests.Json()));await Drain();Assert.AreEqual(1,wire.Requests.Count(r=>r.EndpointPath=="/__local/ce03/support-loadout"),"Only the newest host continuation may query support.");Assert.AreEqual("미선택",Field(host,"departureSupportNotice"));
   }finally{first.TrySetResult(RunApiHttpResponse.NetworkError("closed fixture"));Object.DestroyImmediate(root);}
  }
 }
}
