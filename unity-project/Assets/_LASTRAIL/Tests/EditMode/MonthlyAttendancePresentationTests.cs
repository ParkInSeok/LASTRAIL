using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class MonthlyAttendancePresentationTests
    {
        sealed class Registry : IMonthlyAttendanceRegistry { public string CurrentActivityId => "monthly-view-fixture"; }
        sealed class Wire : IRunApiHttpTransport
        {
            public int Reads, Writes; public bool Unavailable,Paid;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.Method!="GET"){Writes++;throw new InvalidOperationException("Read-only attendance presentation fixture");}
                Reads++;var data=Calendar();if(Paid){data["recovery"]["recoverableCount"]=1;data["recovery"]["quote"]=new JObject{["walletPolicyVersion"]="qa_wallet",["currencyId"]="crystal",["sufficientFunds"]=true,["freeSpent"]=65,["paidSpent"]=35};}return Task.FromResult(RunApiHttpResponse.Success(Unavailable?503:200,Unavailable?"{}":data.ToString()));
            }
        }
        // Synthetic contract-valid display input, not a preserved server response or a reward grant.
        public static JObject Calendar()
        {
            var cells=new JArray();
            for(int i=1;i<=28;i++)cells.Add(new JObject{["cell"]=i,["rewards"]=new JArray(new JObject{["kind"]="CURRENCY",["id"]="cur_credits",["quantity"]=1000}),["unavailableReason"]=null,
                ["receipt"]=i<=3?new JObject{["cell"]=i,["source"]="NATURAL",["dayIndex"]=i-1,["claimId"]="synthetic-"+i}:null});
            return new JObject{["accepted"]=true,["version"]="monthly-attendance-v1",["activityId"]="monthly-view-fixture",["policyVersion"]="monthly-view-fixture",["cycleId"]="2026-09",["revision"]=3,
                ["serverTime"]=new DateTimeOffset(2026,9,3,0,0,0,TimeSpan.Zero).ToUnixTimeMilliseconds(),["startAt"]=new DateTimeOffset(2026,8,31,19,0,0,TimeSpan.Zero).ToUnixTimeMilliseconds(),["endAt"]=new DateTimeOffset(2026,9,30,19,0,0,TimeSpan.Zero).ToUnixTimeMilliseconds(),
                ["dayIndex"]=2,["active"]=true,["todayAttended"]=true,["completedCells"]=3,["complete"]=false,["unavailableReason"]=null,["cells"]=cells,
                ["recovery"]=new JObject{["price"]=100,["maxPerCycle"]=7,["used"]=0,["todayAttendanceRequired"]=false,["recoverableCount"]=0,["quote"]=null}};
        }
        [Test] public async Task DedicatedCalendarUsesVerifiedCellsAndClearsStaleReadWithoutGranting()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1,new Registry());
            var root=new GameObject("AttendancePresentation",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var r=(RectTransform)safe.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenMonthlyAttendance();
                var view=root.GetComponentInChildren<MonthlyAttendanceView>();Assert.IsNotNull(view);Assert.That(state.MonthlyReady,Is.True,state.MonthlyError);
                Assert.That(view.CurrentState.Cells.Count,Is.EqualTo(28));Assert.That(view.CurrentState.Cells.Count(c=>c.Received),Is.EqualTo(3));Assert.That(view.CurrentState.CountText,Does.Contain("3 / 28"));
                Assert.IsFalse(view.GetComponentsInChildren<Button>().Single(b=>b.name=="RecoverMonthly").interactable);Assert.That(wire.Writes,Is.Zero);
                wire.Unavailable=true;await state.RefreshMonthlyAttendanceAsync();Assert.IsNotNull(state.MonthlyAttendance,"Owner retains stale snapshot for recovery diagnostics.");
                Assert.That(view.CurrentState.Cells.Count,Is.Zero,"Stale calendar must not appear current after a failed read.");Assert.IsFalse(view.CurrentState.CanRecover);
                int before=wire.Reads;view.GetComponentsInChildren<Button>().Single(b=>b.name=="RefreshMonthly").onClick.Invoke();Assert.That(wire.Reads-before,Is.EqualTo(1));
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="CloseMonthly").onClick.Invoke();Assert.IsFalse(deck.IsDialogOpen);Assert.That(wire.Writes,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void ReplacingAccountClosesPaidConfirmationWithoutSubmitting()
        {
            var wire=new Wire{Paid=true};var session=SupportInquiryTests.Session();using var a=new AccountScreenRuntime(wire,null,()=>session,()=>1,new Registry());using var b=new AccountScreenRuntime(wire,null,()=>session,()=>1,new Registry());
            var root=new GameObject("AttendanceOwner",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var r=(RectTransform)safe.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(a);deck.OpenMonthlyAttendance();
                root.GetComponentsInChildren<Button>().Single(x=>x.name=="RecoverMonthly").onClick.Invoke();Assert.That(root.GetComponentsInChildren<Button>().Any(x=>x.name=="ConfirmPaidAttendance"),Is.True);deck.BindAccount(b);
                Assert.That(deck.IsDialogOpen,Is.False,"Paid confirmation belongs to the old account runtime and must close on replacement.");Assert.That(wire.Writes,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void RebindingReplacesOnlyOwnedCallbacksAndUnbindStopsDispatch()
        {
            var root=new GameObject("AttendanceBindings",typeof(RectTransform));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=MonthlyAttendanceView.Create(root.transform,font);
                int old=0,current=0,external=0;view.Bind(()=>old++,()=>{},()=>{});view.Bind(()=>current++,()=>{},()=>{});
                view.Render(new MonthlyAttendanceDisplayState(null));var close=view.GetComponentsInChildren<Button>().Single(b=>b.name=="CloseMonthly");close.onClick.AddListener(()=>external++);
                close.onClick.Invoke();Assert.That(old,Is.Zero);Assert.That(current,Is.EqualTo(1));Assert.That(external,Is.EqualTo(1));view.Unbind();close.onClick.Invoke();Assert.That(current,Is.EqualTo(1));Assert.That(external,Is.EqualTo(2));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
