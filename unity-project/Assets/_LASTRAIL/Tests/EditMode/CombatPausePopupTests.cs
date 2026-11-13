using System;
using System.Collections;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
public sealed class CombatPausePopupTests
{
    [Test] public void NavigationRebindReplacesCallbacksAndDisableUnbindsHost()
    {
        var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("PauseNavigation",typeof(RectTransform),typeof(Canvas));
        try{
            go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var controller=go.GetComponent<CombatPauseController>();int old=0,current=0,lobby=0;
            controller.BindNavigation(()=>old++,()=>old++);controller.BindNavigation(()=>current++,()=>lobby++);controller.Open();
            Button Find(string name)=>go.GetComponentsInChildren<Button>(true).Single(x=>x.name==name);
            Find("PauseSettings").onClick.Invoke();Assert.AreEqual(0,old);Assert.AreEqual(1,current);Assert.AreEqual(0,lobby);Assert.IsTrue((bool)driver.HudState["paused"]);
            Find("PauseLobby").onClick.Invoke();Assert.AreEqual(0,lobby);Find("PauseLobby").onClick.Invoke();Assert.AreEqual(1,lobby);Assert.IsTrue((bool)driver.HudState["paused"]);
            controller.Close();Find("PauseSettings").onClick.Invoke();Assert.AreEqual(1,current);
            controller.Open();controller.UnbindNavigation();Assert.IsFalse(Find("PauseSettings").interactable);Assert.IsFalse(Find("PauseLobby").interactable);
            controller.Close();Assert.IsTrue((bool)driver.HudState["paused"]);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void LobbyRequiresConfirmationCancelResumesAndConfirmedIntentIsConsumedOnce()
    {
        var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("LeaveIntent",typeof(RectTransform),typeof(Canvas));
        try{
            go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var c=go.GetComponent<CombatPauseController>();int calls=0;c.BindNavigation(null,()=>calls++);
            Button Find(string n)=>go.GetComponentsInChildren<Button>(true).Single(b=>b.name==n);
            c.Open();string paused=driver.StateJson;Find("PauseLobby").onClick.Invoke();Assert.AreEqual(0,calls);Assert.AreEqual(paused,driver.StateJson);Assert.IsTrue(go.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("시작 당시 상태")));
            Find("PauseContinue").onClick.Invoke();Assert.IsFalse(c.IsOpen);Assert.IsFalse((bool)driver.HudState["paused"]);Assert.AreEqual(0,calls);
            c.Open();Find("PauseLobby").onClick.Invoke();c.RequestBack();Assert.IsFalse(c.IsOpen);Assert.IsFalse((bool)driver.HudState["paused"]);Assert.AreEqual(0,calls);
            c.Open();Find("PauseLobby").onClick.Invoke();Find("PauseLobby").onClick.Invoke();Find("PauseLobby").onClick.Invoke();Assert.AreEqual(1,calls);Assert.IsTrue((bool)driver.HudState["paused"]);
            c.UnbindNavigation();Find("PauseLobby").onClick.Invoke();Assert.AreEqual(1,calls);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void AbandonIsUnavailableWithoutHostAndDoesNotMasqueradeAsLobby()
    {
        var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("AbandonIntent",typeof(RectTransform),typeof(Canvas));
        try{
            go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var c=go.GetComponent<CombatPauseController>();int leaves=0,abandons=0;c.BindNavigation(null,()=>leaves++);c.Open();
            Button Find(string n)=>go.GetComponentsInChildren<Button>(true).Single(b=>b.name==n);
            Assert.IsFalse(Find("PauseAbandon").interactable);Find("PauseAbandon").onClick.Invoke();Assert.AreEqual(0,leaves);
            c.BindNavigation(null,()=>leaves++,()=>abandons++);string before=driver.StateJson;
            Find("PauseAbandon").onClick.Invoke();Assert.AreEqual(0,abandons);Find("PauseAbandon").onClick.Invoke();Find("PauseAbandon").onClick.Invoke();
            Assert.AreEqual(1,abandons);Assert.AreEqual(0,leaves);Assert.AreEqual(before,driver.StateJson,"Menu delegates intent; it cannot remove a run or fabricate a result.");
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [UnityTest] public IEnumerator PlayModeDisableClearsNavigationAndDoesNotResume()
    {
        yield return new EnterPlayMode();
        var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("PausePlayLifecycle",typeof(RectTransform),typeof(Canvas));
        try{
            go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var controller=go.GetComponent<CombatPauseController>();int navigation=0;controller.BindNavigation(()=>navigation++,()=>navigation++);controller.Open();
            var stale=go.GetComponentsInChildren<Button>().Single(x=>x.name=="PauseLobby");go.SetActive(false);stale.onClick.Invoke();Assert.AreEqual(0,navigation);Assert.IsTrue((bool)driver.HudState["paused"]);Assert.IsFalse(controller.IsOpen);
            go.SetActive(true);controller.Open();Assert.IsFalse(go.GetComponentsInChildren<Button>().Single(x=>x.name=="PauseSettings").interactable);Assert.IsFalse(go.GetComponentsInChildren<Button>().Single(x=>x.name=="PauseLobby").interactable);Assert.IsTrue((bool)driver.HudState["paused"]);
            UnityEngine.Object.DestroyImmediate(controller);Assert.IsNull(go.GetComponentInChildren<CombatPauseView>());
        }finally{UnityEngine.Object.DestroyImmediate(go);}
        yield return new ExitPlayMode();
    }
    [Test] public void HeaderOpensModalAndOnlyContinueResumesPersistedPause()
    {
        int saves=0;var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>{saves++;return r+1;},()=>true);driver.Open();
        var go=new GameObject("PauseTest",typeof(RectTransform),typeof(Canvas));
        try{
            var view=go.AddComponent<ApprovedCombatView>();var art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");view.Bind(driver,art);view.BindOrRefresh(driver,art);view.BindOrRefresh(driver,art);
            Button Find(string name)=>go.GetComponentsInChildren<Button>(true).Single(x=>x.name==name);
            int before=saves;Find("Pause").onClick.Invoke();Assert.IsTrue((bool)driver.HudState["paused"]);Assert.AreEqual(before+1,saves);
            Assert.IsTrue(go.GetComponentsInChildren<Button>().Any(x=>x.name=="PauseContinue"),"Header must expose central pause popup");
            Assert.IsFalse(Find("PauseSettings").interactable);Assert.IsFalse(Find("PauseLobby").interactable);
            var stale=Find("PauseContinue");Find("PauseClose").onClick.Invoke();Assert.IsFalse((bool)driver.HudState["paused"],"User close resumes the same in-memory battle");
            before=saves;stale.onClick.Invoke();Assert.AreEqual(before,saves,"Closed popup cannot resume");
            Find("Pause").onClick.Invoke();Assert.AreEqual(before+1,saves,"Reopening pauses the current battle");before=saves;
            Find("PauseContinue").onClick.Invoke();Assert.IsFalse((bool)driver.HudState["paused"]);Assert.AreEqual(before+1,saves);
            Find("Pause").onClick.Invoke();before=saves;var disabled=Find("PauseContinue");go.SetActive(false);disabled.onClick.Invoke();Assert.AreEqual(before,saves);Assert.IsTrue((bool)driver.HudState["paused"]);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void FailedResumeKeepsPauseAndExposesRecoveryInsteadOfFalseSuccess()
    {
        bool fail=false;var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>{if(fail)throw new InvalidOperationException("QA save failure");return r+1;},()=>true);driver.Open();
        var go=new GameObject("PauseFailure",typeof(RectTransform),typeof(Canvas));
        try{
            go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
            go.GetComponentsInChildren<Button>().Single(x=>x.name=="Pause").onClick.Invoke();
            var resume=go.GetComponentsInChildren<Button>().SingleOrDefault(x=>x.name=="PauseContinue");Assert.IsNotNull(resume);
            fail=true;resume.onClick.Invoke();Assert.IsTrue(driver.RecoveryRequired);Assert.IsTrue((bool)driver.HudState["paused"]);Assert.IsFalse(resume.interactable);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
