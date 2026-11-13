using System.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DepartureSceneCompositionTests
{
    [Test]
    public void ResizingPreservesLandscapeColumnsSceneRegistrationAndCallbacks()
    {
        const float compactHeight=900,tallHeight=1080;
        var root=new GameObject("SceneContract",typeof(RectTransform),typeof(Canvas));
        ((RectTransform)root.transform).sizeDelta=new Vector2(1600,compactHeight);
        int backs=0;
        try
        {
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=DeparturePreparationView.Create(root.transform,font,null,()=>backs++,null,null,null,null,null,null,null,null,null);
            view.SetViewportHeight(compactHeight);
            var driver=view.GetComponentsInChildren<RawImage>().SingleOrDefault(x=>x.name=="PreparationDriver");
            Assert.IsNotNull(driver,"Actual preparation must connect the supplied independent driver, not only QA injection.");
            var environment=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="PreparationEnvironment");
            Assert.AreSame(environment.transform.parent,driver.transform.parent);
            var approved=Resources.Load<LastRail.Presentation.ApprovedUiConnections>("ApprovedUi/Connections20261110");
            Assert.AreSame(approved.DepartureCabin,environment.texture);Assert.AreSame(approved.Pilot,driver.texture);
            Assert.AreEqual(new Rect(0,0,1,1),driver.uvRect);
            var viewport=(RectTransform)environment.transform.parent;
            Assert.AreEqual(viewport.rect.size,environment.rectTransform.sizeDelta);
            Assert.That(driver.rectTransform.sizeDelta.x/driver.rectTransform.sizeDelta.y,Is.EqualTo((float)driver.texture.width/driver.texture.height).Within(.001));
            Assert.That(-driver.rectTransform.anchoredPosition.y+driver.rectTransform.sizeDelta.y,Is.EqualTo(viewport.rect.height).Within(.001));
            Assert.LessOrEqual(driver.rectTransform.sizeDelta.x,viewport.rect.width*.52f+.001f);
            driver.gameObject.SetActive(false);Assert.IsTrue(environment.enabled);driver.gameObject.SetActive(true);
            Assert.IsNotNull(driver.GetComponentInParent<RectMask2D>());
            Assert.IsFalse(driver.raycastTarget);Assert.IsFalse(environment.raycastTarget);Assert.IsNotNull(driver.texture);
            var driverPosition=driver.rectTransform.anchoredPosition;var driverSize=driver.rectTransform.sizeDelta;var sceneSize=environment.rectTransform.sizeDelta;var uv=environment.uvRect;
            RectTransform Rect(string name)=>view.GetComponentsInChildren<RectTransform>().Single(x=>x.name==name);
            var operation=Rect("OperationPanel");var formation=Rect("FormationPanel");var fuel=Rect("FuelPanel");var start=Rect("ConfirmDeparture");var guide=Rect("DepartureGuideEntry");
            var operationPosition=operation.anchoredPosition;var formPosition=formation.anchoredPosition;var fuelPosition=fuel.anchoredPosition;var startPosition=start.anchoredPosition;var startSize=start.sizeDelta;var guidePosition=guide.anchoredPosition;
            Assert.Greater(fuel.anchoredPosition.x,formation.anchoredPosition.x+formation.rect.width*formation.localScale.x,"Fuel/start column must sit beside the formation, not beneath a portrait screen.");
            foreach(var section in new[]{operation,formation,fuel,start,guide})
            {
                Assert.GreaterOrEqual(section.anchoredPosition.x,0);
                Assert.GreaterOrEqual(-section.anchoredPosition.y,0);
                Assert.LessOrEqual(section.anchoredPosition.x+section.rect.width*section.localScale.x,1600);
                Assert.LessOrEqual(-section.anchoredPosition.y+section.rect.height*section.localScale.y,900);
            }
            int children=view.GetComponentsInChildren<Transform>().Length;
            view.SetViewportHeight(tallHeight);view.SetViewportHeight(tallHeight);
            Assert.AreEqual(driverPosition,driver.rectTransform.anchoredPosition);Assert.AreEqual(driverSize,driver.rectTransform.sizeDelta);
            Assert.AreEqual(sceneSize,environment.rectTransform.sizeDelta);Assert.AreEqual(uv,environment.uvRect);
            Assert.AreEqual(operationPosition,operation.anchoredPosition);Assert.AreEqual(formPosition,formation.anchoredPosition);
            Assert.AreEqual(fuelPosition,fuel.anchoredPosition);
            Assert.AreEqual(startPosition,start.anchoredPosition);
            Assert.AreEqual(startSize,start.sizeDelta);Assert.AreEqual(guidePosition,guide.anchoredPosition);
            Assert.AreEqual(children,view.GetComponentsInChildren<Transform>().Length,"Rerender must not append scene layers.");
            view.SetViewportHeight(compactHeight);
            Assert.AreEqual(formPosition,formation.anchoredPosition);Assert.AreEqual(fuelPosition,fuel.anchoredPosition);Assert.AreEqual(startPosition,start.anchoredPosition);
            var back=Rect("BackDeparture").GetComponent<Button>();back.onClick.Invoke();Assert.AreEqual(1,backs);
            view.gameObject.SetActive(false);back.onClick.Invoke();Assert.AreEqual(1,backs,"Inactive view must not invoke the owner's callback.");
        }
        finally{Object.DestroyImmediate(root);}
    }
}
