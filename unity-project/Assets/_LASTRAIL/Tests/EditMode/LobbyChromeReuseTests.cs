using System.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyChromeReuseTests
{
    [Test] public void EquipmentEffectRefreshRetainsFieldsAndCloseDispatchesOnce()
    {
        var root=new GameObject("EquipmentEffectReuse",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
        try{
            var pages=LobbyDestinationPages.Create(root.transform,null,()=>{});pages.Open(LobbyPage.Operators,true);
            var frame=new Newtonsoft.Json.Linq.JObject{["version"]="equipment.multishot-display-frame.v1.20261016",["currentNotice"]="first"};pages.PresentEquipmentMultishot(frame);
            var panel=pages.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="EquipmentMultishotDetails");var label=panel.Find("currentNotice");int children=panel.childCount;
            frame["currentNotice"]="second";pages.PresentEquipmentMultishot(frame);Assert.AreSame(label,panel.Find("currentNotice"));Assert.AreEqual("second",label.GetComponent<TMPro.TMP_Text>().text);Assert.AreEqual(children,panel.childCount);
            int closed=0;pages.BindEquipmentMultishotClose(()=>closed++);var button=panel.Find("CloseEquipmentMultishot").GetComponent<Button>();button.onClick.Invoke();button.onClick.Invoke();Assert.AreEqual(1,closed);
        }finally{Object.DestroyImmediate(root);}
    }    [Test]
    public void NavigationAndHeaderSurvivePageChangesAndReentry()
    {
        var root=new GameObject("ChromeReuse",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
        try{
            var pages=LobbyDestinationPages.Create(root.transform,null,()=>{});
            pages.Open(LobbyPage.Operators,true);
            var chrome=pages.Controls.Find("PageChrome");Assert.NotNull(chrome);
            var rail=chrome.Find(LobbyNavigationRail.ObjectName);
            var back=chrome.Find("Back").GetComponent<Button>();
            pages.Open(LobbyPage.Shop,true);
            Assert.AreSame(chrome,pages.Controls.Find("PageChrome"));Assert.AreSame(rail,chrome.Find(LobbyNavigationRail.ObjectName));
            Assert.IsTrue(rail.Find("PageNav4/BlueInset").gameObject.activeSelf);
            Assert.IsFalse(rail.Find("PageNav1/BlueInset").gameObject.activeSelf);
            back.onClick.Invoke();Assert.IsFalse(pages.IsOpen);
            back.onClick.Invoke();Assert.IsFalse(pages.IsOpen);
            pages.Open(LobbyPage.Operators,true);Assert.AreSame(chrome,pages.Controls.Find("PageChrome"));
            back.onClick.Invoke();Assert.IsFalse(pages.IsOpen);
        }finally{Object.DestroyImmediate(root);}
    }
}
