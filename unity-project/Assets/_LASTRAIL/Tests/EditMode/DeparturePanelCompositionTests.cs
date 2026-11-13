using System.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DeparturePanelCompositionTests
{
    [Test]
    public void CarriageStateAndSupportIdentityCanChangeWithoutStaleDecorationsOrResizeDrift()
    {
        var root=new GameObject("PanelContract",typeof(RectTransform),typeof(Canvas));
        try
        {
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=DeparturePreparationView.Create(root.transform,font,null,null,null,null,null,null,null,null,null,null,null);
            T Find<T>(string name)where T:Component=>view.GetComponentsInChildren<T>(true).Single(x=>x.name==name);
            view.Carriage(1,"전투칸","CombatSecondFinal/train_carriage_armored-v03",false,"COMBAT");
            Assert.IsTrue(Find<DeparturePanelGraphic>("PreparationCarriageRole1").enabled);
            Assert.IsFalse(Find<DeparturePanelGraphic>("PreparationCarriageLock1").enabled);
            view.Carriage(1,"잠김",null,true,null);
            Assert.IsFalse(Find<DeparturePanelGraphic>("PreparationCarriageRole1").enabled);
            Assert.IsTrue(Find<DeparturePanelGraphic>("PreparationCarriageLock1").enabled);
            Assert.IsFalse(Find<TMP_Text>("PreparationCarriageLabel1").enabled);
            view.Carriage(1,"미배치",null,false,null);
            Assert.IsFalse(Find<DeparturePanelGraphic>("PreparationCarriageLock1").enabled);
            Assert.AreEqual("미배치",Find<TMP_Text>("PreparationCarriageLabel1").text);
            Assert.IsTrue(Find<TMP_Text>("PreparationCarriageLabel1").enabled);
            Assert.IsFalse(view.GetComponentsInChildren<TMP_Text>().Any(x=>x.name=="PreparationCarriageNumber0"));
            view.Support(0,"barrier","방어막");view.SetViewportHeight(900);
            var icon=Find<RawImage>("PreparationSupport0");var compact=icon.rectTransform.anchoredPosition;var size=icon.rectTransform.sizeDelta;
            view.SetViewportHeight(1080);var tall=icon.rectTransform.anchoredPosition;
            for(int i=0;i<5;i++)view.Support(0,"barrier","방어막");
            Assert.AreEqual(tall,icon.rectTransform.anchoredPosition);Assert.AreEqual(size,icon.rectTransform.sizeDelta);
            view.SetViewportHeight(900);Assert.AreEqual(compact,icon.rectTransform.anchoredPosition);
            view.Support(0,null,"불러오는 중");Assert.IsNull(icon.texture);Assert.IsFalse(icon.enabled);
            view.Support(0,"emergency_repair","긴급 수리");Assert.AreEqual("support_repair-v01",icon.texture.name);
            var repairedPosition=icon.rectTransform.anchoredPosition;var repairedSize=icon.rectTransform.sizeDelta;
            view.SetViewportHeight(900);view.SetViewportHeight(1080);
            Assert.AreEqual(repairedPosition,icon.rectTransform.anchoredPosition,"Changing the texture must not be undone by the next layout pass.");
            Assert.AreEqual(repairedSize,icon.rectTransform.sizeDelta);
            Assert.AreEqual(icon.texture.width*icon.uvRect.width/(icon.texture.height*icon.uvRect.height),repairedSize.x/repairedSize.y,.001f,"Replacement art keeps its own aspect.");
            var support=Find<RectTransform>("SupportPanel");
            Assert.GreaterOrEqual(repairedPosition.x,support.anchoredPosition.x);
            Assert.LessOrEqual(repairedPosition.x+repairedSize.x*icon.rectTransform.localScale.x,support.anchoredPosition.x+support.rect.width*support.localScale.x);
            Assert.GreaterOrEqual(-repairedPosition.y,-support.anchoredPosition.y);
            Assert.LessOrEqual(-repairedPosition.y+repairedSize.y*icon.rectTransform.localScale.y,-support.anchoredPosition.y+support.rect.height*support.localScale.y);
            Assert.IsTrue(view.GetComponentsInChildren<DeparturePanelGraphic>().All(x=>!x.raycastTarget));
        }
        finally{Object.DestroyImmediate(root);}
    }
}
