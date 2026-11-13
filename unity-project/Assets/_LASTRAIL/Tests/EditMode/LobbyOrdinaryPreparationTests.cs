using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LobbyOrdinaryPreparationTests
    {
        GameObject root;
        LobbyCommandDeck deck;
        [SetUp] public void SetUp()
        {
            root=new GameObject("OrdinaryPreparationUi",typeof(RectTransform),typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);
        }
        [TearDown] public void TearDown(){UnityEngine.Object.DestroyImmediate(root);}
        Button Prepare=>root.GetComponentsInChildren<Button>().Single(b=>b.name=="PrepareOrdinaryCombat");
        TMP_Text Body=>root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="DialogBody");
        [Test] public void RestartLabelExplainsOriginalStartHealth(){deck.OpenDepartureRecovery("run_a");Assert.AreEqual("전투 다시 시작",Prepare.GetComponentInChildren<TMP_Text>().text);Assert.That(Body.text,Does.Contain("체력은 전투 시작 시점"));}
        [Test] public async Task UnavailableOriginStaysVisibleAndCannotRetryEveryFrame(){
            int calls=0;deck.BindOrdinaryPreparation(id=>{calls++;return Task.FromException(new OrdinaryNodeStartUnavailableException());});deck.OpenDepartureRecovery("run_a");Prepare.onClick.Invoke();await Task.Yield();
            deck.EvaluateLayout();Assert.That(Body.text,Does.Contain("현재 전투 정보를 불러올 수 없습니다"));Assert.IsFalse(Prepare.interactable);
            Prepare.onClick.Invoke();Assert.AreEqual(1,calls);
        }
        [Test] public async Task OneInputDelegatesOriginalRunOnceWhilePending()
        {
            int calls=0;string run=null;var pending=new TaskCompletionSource<bool>();
            deck.BindOrdinaryPreparation(id=>{calls++;run=id;return pending.Task;});deck.OpenDepartureRecovery("original-run");
            Prepare.onClick.Invoke();Prepare.onClick.Invoke();Assert.AreEqual(1,calls);Assert.AreEqual("original-run",run);Assert.IsFalse(Prepare.interactable);
            pending.SetResult(true);await Task.Yield();Assert.IsTrue(Prepare.interactable);Assert.That(Body.text,Does.Contain("준비를 마쳤"));
        }
        [Test] public async Task RebindingDoesNotInvokeOldDelegateOrPublishItsResult()
        {
            var pending=new TaskCompletionSource<bool>();int old=0,current=0;
            deck.BindOrdinaryPreparation(id=>{old++;return pending.Task;});deck.OpenDepartureRecovery("run_a");Prepare.onClick.Invoke();
            deck.BindOrdinaryPreparation(id=>{current++;return Task.CompletedTask;});string before=Body.text;
            pending.SetResult(true);await Task.Yield();Assert.That(Body.text,Does.Not.Contain("준비를 마쳤"));Assert.IsTrue(Prepare.interactable);
            deck.OpenDepartureRecovery("run_b");Prepare.onClick.Invoke();await Task.Yield();Assert.AreEqual(1,old);Assert.AreEqual(1,current);
        }
        [Test] public async Task ClosedViewNeverReceivesLatePreparationResult()
        {
            var pending=new TaskCompletionSource<bool>();deck.BindOrdinaryPreparation(id=>pending.Task);deck.OpenDepartureRecovery("run_a");Prepare.onClick.Invoke();
            deck.CloseDialog();pending.SetResult(true);await Task.Yield();Assert.IsFalse(deck.IsDialogOpen);
        }
        [Test] public void UnbindingDisablesAndRemovesPreparationCallback()
        {
            int calls=0;deck.BindOrdinaryPreparation(id=>{calls++;return Task.CompletedTask;});deck.OpenDepartureRecovery("run_a");
            deck.BindOrdinaryPreparation(null);Assert.IsFalse(Prepare.interactable);Prepare.onClick.Invoke();Assert.AreEqual(0,calls);
        }
        [Test] public void DisabledViewCannotInvokePreparation()
        {
            int calls=0;deck.BindOrdinaryPreparation(id=>{calls++;return Task.CompletedTask;});deck.OpenDepartureRecovery("run_a");
            var button=Prepare;root.SetActive(false);button.onClick.Invoke();Assert.AreEqual(0,calls);
        }
        [Test] public async Task ClosingRequestingScreenRevokesLatePresentationPermission()
        {
            var pending=new TaskCompletionSource<bool>();int presentations=0;Func<bool> permission=null;
            deck.BindOrdinaryPreparationWithOwner(async(id,current)=>{permission=current;await pending.Task;if(current())presentations++;});
            deck.OpenDepartureRecovery("run_a");Prepare.onClick.Invoke();Assert.IsTrue(permission());
            deck.CloseDialog();Assert.IsFalse(permission());pending.SetResult(true);await Task.Yield();Assert.AreEqual(0,presentations);
        }
        [Test] public async Task ConfirmedRunResultUsesExistingResultScreen()
        {
            deck.BindOrdinaryPreparation(id=>Task.FromException(new OrdinaryNodeStartUnavailableException("confirmed",true)));
            deck.OpenDepartureRecovery("run_a");Prepare.onClick.Invoke();await Task.Yield();
            Assert.IsTrue(root.GetComponentsInChildren<Button>().Any(b=>b.name=="RecoverRunFinish"));
            Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(b=>b.name=="PrepareOrdinaryCombat"));
        }
        [Test] public async Task UncertainOutcomeUsesOriginalRequestActionAndPersistsAcrossDisplayRefresh(){
            deck.BindOrdinaryPreparation(id=>Task.FromException(new OrdinaryNodeStartUnavailableException("요청 결과를 확인하지 못했습니다.",false,OrdinaryPreparationFailure.OutcomeUnknown)));
            deck.OpenDepartureRecovery("run_a");Prepare.onClick.Invoke();await Task.Yield();deck.EvaluateLayout();
            Assert.That(Body.text,Does.Contain("요청 결과"));Assert.AreEqual("원요청 확인",Prepare.GetComponentInChildren<TMP_Text>().text);Assert.IsTrue(Prepare.interactable);
        }
    }
}
