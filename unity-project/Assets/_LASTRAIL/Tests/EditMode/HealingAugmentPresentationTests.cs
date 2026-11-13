using System;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class HealingAugmentPresentationTests
{
    const string Version="augment.utility-definitions.v7.20261019";
    static JObject Lifesteal(int bps=1200)=>JObject.Parse("{id:'O22',name:'흡혈 회로',rules:{damageToHealBps:"+bps+",eligible:'ALL_CONFIRMED_ENEMY_HP_DAMAGE_ATTRIBUTED_TO_THIS_OPERATOR',addedRecoveryCap:null,internalCooldownMs:0}}");
    [Test]public void KnownScopeAndTriggerDoNotInventResolvedActorsOrOwnedCombinations(){
        var card=HealingAugmentPresentation.FromPinned(Version,Lifesteal(),null);
        StringAssert.Contains("요원",card.Type);StringAssert.Contains("적용 대상",card.Details);StringAssert.Contains("발동 조건",card.Details);
        StringAssert.Contains("피해가 귀속된 요원 자신",card.Details);StringAssert.Contains("함께 사용할 때",card.Details);
        StringAssert.DoesNotContain("대상 없음",card.Details);StringAssert.DoesNotContain("현재 보유",card.Details);
    }
    [Test]public void UsesPinnedValuesWithoutMutatingSourceOrRewritingOldVersions()
    {
        var source=Lifesteal(1250);string before=source.ToString();var card=HealingAugmentPresentation.FromPinned(Version,source,null);
        StringAssert.Contains("12.5%",card.Description);StringAssert.Contains("드론",card.Details);StringAssert.Contains("시간·횟수별 회복 상한",card.Details);Assert.AreEqual(before,source.ToString());
        Assert.IsNull(HealingAugmentPresentation.FromPinned("historical",source,null));
    }
    [Test]public void MissingOrIncompatibleDefinitionCannotAcquireInventedCopy()
    {
        var d=Lifesteal();((JObject)d["rules"]).Remove("damageToHealBps");Assert.Throws<ArgumentException>(()=>HealingAugmentPresentation.FromPinned(Version,d,null));
        d=Lifesteal();d["rules"]["addedRecoveryCap"]=800;Assert.Throws<ArgumentException>(()=>HealingAugmentPresentation.FromPinned(Version,d,null));
        Assert.IsNull(HealingAugmentPresentation.FromPinned(Version,JObject.Parse("{id:'O15'}"),null));
    }
    [Test]public void BackflowSeparatesImmediateHealingParentCapAndCombinedValues()
    {
        var definition=JObject.Parse("{id:'O23',name:'생명 역류',rules:{excessToDamageBps:6000,parentCasterAttackCapBps:15000,projectileProfile:{speedMmPerSecond:36000,maximumRangeMm:24000}}}");
        var composition=JObject.Parse("{o02AndBackflowAllocationBps:[5000,5000],o02ConversionBps:5000,backflowConversionBps:6000}");
        var card=HealingAugmentPresentation.FromPinned(Version,definition,composition);
        StringAssert.Contains("60%",card.Description);StringAssert.Contains("150%",card.Details);StringAssert.Contains("25%",card.Details);StringAssert.Contains("30%",card.Details);
        StringAssert.Contains("치료 요원의 원치유",card.Details);StringAssert.Contains("36m/s",card.Details);StringAssert.Contains("420",card.Examples);StringAssert.Contains("175",card.Examples);StringAssert.Contains("210",card.Examples);
    }
    [Test]public void ExamplesStartFoldedAndToggleDoesNotSelect()
    {
        var root=new UnityEngine.GameObject("CopyDetail",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
        try{
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=AugmentSelectionView.Create(root.transform,font);int selected=0;view.Bind(_=>selected++,(_,__)=>{},()=>{});
            view.Render(AugmentSelectionSnapshot.Parse(LastRail.Tests.EditMode.AugmentSelectionContractTests.Initial),false,false,"",(_,__)=>new AugmentCardPresentation("생명 역류","증강","요약",null,"기본 상세","예시 420"));
            var buttons=view.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            System.Linq.Enumerable.Single(buttons,b=>b.name=="Details0").onClick.Invoke();
            var body=System.Linq.Enumerable.Single(view.GetComponentsInChildren<TMPro.TMP_Text>(true),t=>t.name=="DetailBody");Assert.AreEqual("기본 상세",body.text);
            var toggle=System.Linq.Enumerable.FirstOrDefault(buttons,b=>b.name=="DetailExamples");Assert.IsNotNull(toggle);
            toggle.onClick.Invoke();StringAssert.Contains("예시 420",body.text);Assert.AreEqual(0,selected);
            System.Linq.Enumerable.Single(buttons,b=>b.name=="DetailClose").onClick.Invoke();System.Linq.Enumerable.Single(buttons,b=>b.name=="Details0").onClick.Invoke();Assert.AreEqual("기본 상세",body.text);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
