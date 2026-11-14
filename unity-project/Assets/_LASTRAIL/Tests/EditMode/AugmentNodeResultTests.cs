using System;
using System.Collections.Generic;
using LastRail.Presentation.AugmentSelection;
using NUnit.Framework;
public class AugmentNodeResultTests
{
    static AugmentCardPresentation Card=>new AugmentCardPresentation("흡혈 회로","증강","설명",null);
    [UnityEngine.TestTools.UnityTest]public System.Collections.IEnumerator OpenModalDisablesUnderlyingNavigationAndRestoresFocusOnClose()
    {
        yield return new UnityEngine.TestTools.EnterPlayMode();
        var root=new UnityEngine.GameObject("ResultHost",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
        var eventObject=new UnityEngine.GameObject("ResultEvents",typeof(UnityEngine.EventSystems.EventSystem));
        UnityEngine.EventSystems.EventSystem.current=eventObject.GetComponent<UnityEngine.EventSystems.EventSystem>();
        try{
            var hostButtonObject=new UnityEngine.GameObject("Claim",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Button));hostButtonObject.transform.SetParent(root.transform,false);var host=hostButtonObject.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(hostButtonObject);
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentNodeResultView.Create(root.transform,font);
            view.Bind(new AugmentNodeResult("r","n",1,new[]{new AugmentNodeResultEntry("O22",Card,AugmentRecordCoverage.None,null,null)}));
            UnityEngine.UI.Button Find(string name)=>System.Linq.Enumerable.Single(view.GetComponentsInChildren<UnityEngine.UI.Button>(true),b=>b.name==name);
            Find("AugmentEffectsEntry").onClick.Invoke();Assert.IsFalse(host.IsInteractable());Assert.AreEqual("AugmentResultClose",UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);
            Find("AugmentResultCard0").onClick.Invoke();Assert.IsFalse(Find("AugmentResultCard0").IsInteractable());
            Find("DetailClose").onClick.Invoke();Assert.IsTrue(Find("AugmentResultCard0").IsInteractable());Assert.IsFalse(host.IsInteractable());
            Find("AugmentResultClose").onClick.Invoke();Assert.IsTrue(host.IsInteractable());Assert.AreEqual(hostButtonObject,UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject);
            Find("AugmentEffectsEntry").onClick.Invoke();yield return null;
            Assert.IsFalse(host.IsInteractable(),"Same-frame close/reopen must survive deferred destruction");
            Find("AugmentResultClose").onClick.Invoke();
        }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(eventObject);}
        yield return new UnityEngine.TestTools.ExitPlayMode();
    }
    [Test]public void ResultViewHidesWithoutOwnedAugmentsAndClearsOnUnbind()
    {
        var type=typeof(AugmentNodeResult).Assembly.GetType("LastRail.Presentation.AugmentSelection.AugmentNodeResultView");Assert.IsNotNull(type);
        var root=new UnityEngine.GameObject("NodeResult",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
        try{
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=(UnityEngine.Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,font});
            void Bind(AugmentNodeResult r)=>type.GetMethod("Bind").Invoke(view,new object[]{r});
            Bind(new AugmentNodeResult("r","n",0,Array.Empty<AugmentNodeResultEntry>()));
            UnityEngine.UI.Button Find(string name)=>System.Linq.Enumerable.Single(view.GetComponentsInChildren<UnityEngine.UI.Button>(true),b=>b.name==name);
            Assert.IsFalse(Find("AugmentEffectsEntry").gameObject.activeSelf);
            Bind(new AugmentNodeResult("r","n",1,new[]{new AugmentNodeResultEntry("O22",Card,AugmentRecordCoverage.None,null,null)}));Find("AugmentEffectsEntry").onClick.Invoke();
            Find("AugmentResultCard0").onClick.Invoke();Assert.IsFalse(System.Linq.Enumerable.Any(view.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="DetailSelect"));
            var retained=Find("AugmentResultCard0");
            Bind(new AugmentNodeResult("r","n2",2,new[]{new AugmentNodeResultEntry("O22",new AugmentCardPresentation("교체된 효과","증강","새 설명",null),AugmentRecordCoverage.None,null,null)}));
            Assert.AreSame(retained,Find("AugmentResultCard0"));
            Find("AugmentEffectsEntry").onClick.Invoke();retained.onClick.Invoke();
            Assert.IsTrue(System.Linq.Enumerable.Any(view.GetComponentsInChildren<TMPro.TMP_Text>(),t=>t.text=="교체된 효과"));
            type.GetMethod("Unbind").Invoke(view,null);Assert.IsFalse(Find("AugmentEffectsEntry").gameObject.activeSelf);
            retained.onClick.Invoke();Assert.IsFalse(retained.gameObject.activeInHierarchy);
            Assert.IsFalse(System.Linq.Enumerable.Any(view.GetComponentsInChildren<TMPro.TMP_Text>(true),t=>t.text=="기록 없음"));
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test]public void ZeroMissingAndPartialRecordsRemainDifferent()
    {
        var zero=new AugmentNodeResultEntry("O22",Card,AugmentRecordCoverage.Complete,null,new[]{new AugmentResultMetric("실제 회복 HP",0)});
        StringAssert.Contains("발동결과 없음",zero.Summary);StringAssert.Contains("0",zero.Summary);
        var missing=new AugmentNodeResultEntry("O22",Card,AugmentRecordCoverage.None,null,null);Assert.AreEqual("기록 없음",missing.Summary);
        var partial=new AugmentNodeResultEntry("O22",Card,AugmentRecordCoverage.Partial,"재접속 이후 기록",new[]{new AugmentResultMetric("실제 회복 HP",12)});StringAssert.Contains("재접속 이후 기록",partial.Summary);
        Assert.Throws<ArgumentException>(()=>new AugmentNodeResultEntry("O22",Card,AugmentRecordCoverage.Partial,null,zero.Metrics));
    }
    [Test]public void SnapshotFreezesOwnedOrderAndKeepsDifferentUnitsSeparate()
    {
        var metrics=new List<AugmentResultMetric>{new AugmentResultMetric("보호막 생성",100),new AugmentResultMetric("실제 흡수",50)};
        var entry=new AugmentNodeResultEntry("O02",Card,AugmentRecordCoverage.Complete,null,metrics);metrics.Clear();Assert.AreEqual(2,entry.Metrics.Count);
        var list=new List<AugmentNodeResultEntry>{entry,new AugmentNodeResultEntry("O15",Card,AugmentRecordCoverage.Complete,null,new[]{new AugmentResultMetric("공격에 소비한 보호막",20)})};
        var result=new AugmentNodeResult("run","node",42,list);list.Reverse();Assert.AreEqual("O02",result.Entries[0].Id);Assert.AreEqual(42,result.FinalSequence);
        StringAssert.Contains("100",entry.Summary);StringAssert.Contains("50",entry.Summary);StringAssert.DoesNotContain("170",entry.Summary);
        Assert.Throws<ArgumentException>(()=>new AugmentNodeResult("run","node",42,new[]{entry,entry}));
    }
}
