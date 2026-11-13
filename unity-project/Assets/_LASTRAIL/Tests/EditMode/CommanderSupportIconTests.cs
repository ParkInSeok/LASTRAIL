using System.IO;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommanderSupportIconTests
{
    [Test]
    public void PreparationAndCombatBorrowTheSameApprovedSupportTexturesWithoutChangingButtonGeometry()
    {
        var p=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-general-active-auto-hM29XX/prepared.json"));
        var contract=new Ce03CommandContract((JObject)p["commandContract"]);
        var driver=new CombatSceneDriver(()=>contract.CreateLocalTest(p,(string)p["ownedSelection"]["accountId"],contract.Revision,1),()=>null,(s,r)=>r+1,()=>true);driver.Open();
        var battleRoot=new GameObject("IconCombat",typeof(RectTransform),typeof(Canvas));var prepRoot=new GameObject("IconPreparation",typeof(RectTransform),typeof(Canvas));
        try
        {
            var battle=battleRoot.AddComponent<ApprovedCombatView>();battle.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var prep=DeparturePreparationView.Create(prepRoot.transform,font,null,null,null,null,null,null,null,null,null,null,null);
            for(int i=0;i<3;i++)
            {
                string id=(string)driver.HudState["manuals"][i]["ability"];
                string name=id=="support_fire"?"support_fire-v01":id=="barrier"?"support_barrier-v01":"support_repair-v01";
                prep.Support(i,id,id);
                var expected=LastRail.Resources.PreparedContent.Load<Texture2D>("CommanderSupportIcons/"+name);Assert.IsNotNull(expected);
                var prepared=prep.transform.Find("PreparationSupport"+i).GetComponent<RawImage>();
                var command=(RectTransform)battleRoot.transform.Find("ApprovedBattle/Command"+i);var actual=command.Find("SkillIcon").GetComponent<RawImage>();
                Assert.AreSame(expected,prepared.texture,"Preparation must follow the approved combat identity.");Assert.AreSame(expected,actual.texture,"Both screens must borrow the same asset instance.");
                Assert.AreEqual(prepared.uvRect,actual.uvRect);
                Assert.AreEqual(new Vector2(238,66),command.sizeDelta);Assert.AreEqual(new Vector2(458,-(1008+i*76)),command.anchoredPosition);
                Assert.That(actual.rectTransform.sizeDelta.x,Is.LessThanOrEqualTo(60.001f));Assert.That(actual.rectTransform.sizeDelta.y,Is.LessThanOrEqualTo(60.001f));
                Assert.IsFalse(actual.raycastTarget);var position=actual.rectTransform.anchoredPosition;var size=actual.rectTransform.sizeDelta;
                battle.Refresh();battle.Refresh();Assert.AreEqual(position,actual.rectTransform.anchoredPosition);Assert.AreEqual(size,actual.rectTransform.sizeDelta);
            }
        }
        finally{Object.DestroyImmediate(battleRoot);Object.DestroyImmediate(prepRoot);}
    }
}
