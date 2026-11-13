using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03LocalDepartureTests {
 [TestCase(false)] [TestCase(true)] public async Task InvalidQuoteDoesNotReuseSuccessfulPreparationNotice(bool expired){
  var wire=new Wire{Expired=expired};using var flow=new Ce03LocalDeparture(Client(wire),p=>true,(s,p)=>{},o=>Assert.Fail("No entry"));
  await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);if(!expired)flow.InvalidateQuote();
  int requests=wire.Requests.Count;var root=new UnityEngine.GameObject("InvalidQuoteBlock",typeof(UnityEngine.RectTransform));
  try{var deck=root.AddComponent<LastRail.Presentation.MissionShell.LobbyCommandDeck>();deck.BindLocalDeparture(flow);
   var read=typeof(LastRail.Presentation.MissionShell.LobbyCommandDeck).GetMethod("CurrentDepartureBlocks",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   var blocks=(LastRail.Presentation.MissionShell.DepartureBlockState)read.Invoke(deck,null);
   Assert.AreEqual("현재 선택의 출격 비용을 확인해 주세요.",System.Linq.Enumerable.Single(blocks.Reasons,r=>r.Code=="quote").Message);
   Assert.AreEqual(requests,wire.Requests.Count);Assert.IsFalse(flow.CanStart);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public async Task UnverifiedPrepareReasonRemainsVisibleInAllDepartureBlocks(){
  var wire=new Wire();using var flow=new Ce03LocalDeparture(Client(wire),p=>false,(s,p)=>{},o=>Assert.Fail("No entry"));
  await flow.PreviewAsync("opn_01_last_departure","I",1,1,1);
  var root=new UnityEngine.GameObject("UnverifiedBlock",typeof(UnityEngine.RectTransform));
  try{var deck=root.AddComponent<LastRail.Presentation.MissionShell.LobbyCommandDeck>();deck.BindLocalDeparture(flow);
   var read=typeof(LastRail.Presentation.MissionShell.LobbyCommandDeck).GetMethod("CurrentDepartureBlocks",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   var blocks=(LastRail.Presentation.MissionShell.DepartureBlockState)read.Invoke(deck,null);
   Assert.IsTrue(System.Linq.Enumerable.Any(blocks.Reasons,r=>r.Message==flow.Notice),"The existing owner failure notice must not be replaced with a generic cost prompt.");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 sealed class Wire:IRunApiHttpTransport {
  public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
  public bool LoseQuote;public bool Zulu=true;public bool Expired;
  public TaskCompletionSource<RunApiHttpResponse> DelayedPreview;
  public RunApiHttpResponse PreviewReply;
  public readonly JObject Prepare=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/self-check-1Ys9pJ/prepare-r6.json"));
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);JObject body;
   if(r.EndpointPath=="/__local/ce03/support-loadout")body=new JObject{["selection"]=Prepare["commandContract"]["support"].DeepClone()};
   else if(r.EndpointPath=="/__local/ce03/prepare")body=(JObject)Prepare.DeepClone();
   else if(r.EndpointPath=="/v1/operations/preview"){if(LoseQuote)return Task.FromResult(RunApiHttpResponse.NetworkError("Synthetic lost response"));body=new JObject{["accepted"]=true,["operationPreviewId"]="fixture_quote",["expiresAt"]=(Zulu?DateTime.UtcNow.AddMinutes(Expired?-2:2).ToString("O"):DateTimeOffset.UtcNow.AddMinutes(Expired?-2:2).ToString("O")),["quote"]=new JObject{["fuelCost"]=1}};}
   else return Task.FromResult(RunApiHttpResponse.NetworkError("Unexpected START"));
   var reply=RunApiHttpResponse.Success(200,body.ToString());
   if(r.EndpointPath=="/v1/operations/preview"&&DelayedPreview!=null){PreviewReply=reply;return DelayedPreview.Task;}
   return Task.FromResult(reply);
  }
 }
 static Ce03LocalClient Client(Wire w)=>new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>{});
 [Test] public async Task DelayedPreviewLocksNativeOperationChange()
 {
  var w=new Wire{DelayedPreview=new TaskCompletionSource<RunApiHttpResponse>()};var flow=new Ce03LocalDeparture(Client(w),p=>true,(s,p)=>{},o=>Assert.Fail("No start"));
  var root=new UnityEngine.GameObject("DelayedPreparation",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));var safe=new UnityEngine.GameObject("SafeArea",typeof(UnityEngine.RectTransform));safe.transform.SetParent(root.transform,false);
  try{
   var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
   var deck=LastRail.Presentation.MissionShell.LobbyCommandDeck.Attach(root,font,"QA",0,new[]{new LastRail.Presentation.MissionShell.MissionShellMissionRowState("opn_01_last_departure","QA",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindLocalDeparture(flow);deck.OpenOperations();
   UnityEngine.UI.Button Find(string name)=>System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name==name);
   Find("SelectDeparture0").onClick.Invoke();var original=root.GetComponentInChildren<LastRail.Presentation.MissionShell.DeparturePreparationView>();
   var pending=flow.PreviewAsync("opn_01_last_departure","I",1,1,1);Assert.IsTrue(flow.Busy);
   Assert.IsFalse(Find("DepartureOperation").interactable);Find("DepartureOperation").onClick.Invoke();Assert.AreSame(original,root.GetComponentInChildren<LastRail.Presentation.MissionShell.DeparturePreparationView>());
   w.DelayedPreview.SetResult(w.PreviewReply);await pending;Assert.IsTrue(Find("DepartureOperation").interactable);
   Assert.IsFalse(Find("ConfirmDeparture").interactable,"Without matching current account revisions a local quote cannot start from this view.");
  }finally{w.DelayedPreview.TrySetResult(w.PreviewReply);UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public async Task GuideCostIsReadOnlyAndRejectsChangedOrInvalidatedSelection(){var w=new Wire();var f=new Ce03LocalDeparture(Client(w),p=>true,(s,p)=>{},o=>Assert.Fail("Guide START"));await f.PreviewAsync("opn_01_last_departure","I",1,1,1);int count=w.Requests.Count;Assert.AreEqual("1",f.ReadGuideFuelCost("opn_01_last_departure","I",1,1,1));Assert.IsNull(f.ReadGuideFuelCost("opn_01_last_departure","II",1,1,1));Assert.IsNull(f.ReadGuideFuelCost("opn_01_last_departure","I",2,1,1));f.InvalidateQuote();Assert.IsNull(f.ReadGuideFuelCost("opn_01_last_departure","I",1,1,1));Assert.AreEqual(count,w.Requests.Count);}
 [Test] public async Task UnverifiedPrepareIsPersistedButCannotQuoteOrStart(){var w=new Wire();int saved=0;var f=new Ce03LocalDeparture(Client(w),p=>false,(s,p)=>saved++,o=>Assert.Fail("Unverified entry"));await f.PreviewAsync("opn_01_last_departure","I",1,1,1);Assert.AreEqual(1,saved);Assert.IsTrue(f.HasPrepare);Assert.IsFalse(f.CanStart);await f.StartAsync();Assert.AreEqual(2,w.Requests.Count);}
 [TestCase(true)] [TestCase(false)] public async Task ChangedSelectionInvalidatesQuoteAndVerificationIsRechecked(bool zulu){var w=new Wire{Zulu=zulu};bool verified=true;var f=new Ce03LocalDeparture(Client(w),p=>verified,(s,p)=>{},o=>{});await f.PreviewAsync("opn_01_last_departure","I",1,1,1);Assert.IsTrue(f.CanStart);verified=false;Assert.IsFalse(f.CanStart);verified=true;f.InvalidateQuote();Assert.IsFalse(f.CanStart);}
 [Test] public async Task LostQuoteReplaysOriginalBeforeAllowingFreshSelection(){var w=new Wire{LoseQuote=true};var c=Client(w);var f=new Ce03LocalDeparture(c,p=>true,(s,p)=>{},o=>{});await f.PreviewAsync("opn_01_last_departure","I",1,1,1);Assert.AreEqual("quote",c.PendingKind);Assert.IsFalse(f.CanChangeSelection);var original=w.Requests[2];w.LoseQuote=false;await f.PreviewAsync("opn_01_last_departure","II",1,1,1);var replay=w.Requests[3];Assert.AreEqual(original.BodyJson,replay.BodyJson);Assert.AreEqual(original.Headers[1].Value,replay.Headers[1].Value);Assert.IsFalse(c.HasPending);Assert.IsFalse(f.CanStart);}
 [Test] public void ActualDepartureDialogConstructsBeforeFlowChangedRefresh(){var w=new Wire();var flow=new Ce03LocalDeparture(Client(w),p=>false,(s,p)=>{},o=>{});var root=new UnityEngine.GameObject("LocalDepartureUiTest",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));var safe=new UnityEngine.GameObject("SafeArea",typeof(UnityEngine.RectTransform));safe.transform.SetParent(root.transform,false);try{var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LastRail.Presentation.MissionShell.LobbyCommandDeck.Attach(root,font,"QA",0,new[]{new LastRail.Presentation.MissionShell.MissionShellMissionRowState("opn_01_last_departure","QA",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},()=>{},()=>{});deck.BindLocalDeparture(flow);deck.OpenOperations();System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="SelectDeparture0").onClick.Invoke();Assert.IsNotNull(System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="DepartureRisk"));Assert.IsFalse(System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="ConfirmDeparture").interactable);deck.BindLocalDeparture(null);}finally{UnityEngine.Object.DestroyImmediate(root);}}

 [TestCase(true)] [TestCase(false)] public async Task QuoteDateAndJournalReopenPreserveOriginalText(bool zulu){var w=new Wire{Zulu=zulu};var c=Client(w);var quote=await c.QuoteAsync(new JObject{["operationId"]="opn_01_last_departure",["risk"]="I",["expectedFormationRevision"]=1,["expectedLoadoutRevision"]=1,["expectedUltimateRevision"]=1});Assert.AreEqual(JTokenType.String,quote["expiresAt"].Type);var saved=c.JournalJson;var restored=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"a",w,new GameApiAccessToken("lst1.test"),raw=>{},saved);Assert.AreEqual(saved,restored.JournalJson);}

 [TestCase(true)] [TestCase(false)] public async Task ExpiredQuoteCannotStart(bool zulu){var w=new Wire{Zulu=zulu,Expired=true};var f=new Ce03LocalDeparture(Client(w),p=>true,(s,p)=>{},o=>Assert.Fail("Expired START"));await f.PreviewAsync("opn_01_last_departure","I",1,1,1);Assert.IsFalse(f.CanStart);await f.StartAsync();Assert.AreEqual(3,w.Requests.Count);}

}
