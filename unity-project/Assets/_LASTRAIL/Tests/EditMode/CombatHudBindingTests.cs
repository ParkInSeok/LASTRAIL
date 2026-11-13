using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatHudBindingTests
    {
        [Test] public void SelectedCommanderAndTrainControlsUseTheirActualAbilityAndShareBusyState()
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab"));
            try{
                var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();driver.Advance(500000*40,40);
                var hud=go.AddComponent<CombatLiveHudBinding>();hud.Bind(driver);hud.Bind(driver);
                Button Find(string name)=>Array.Find(go.GetComponentsInChildren<Transform>(true),t=>t.name==name)?.GetComponent<Button>();
                var commander=Find("LiveCommanderSkill");Assert.NotNull(commander);Assert.IsFalse(commander.interactable);
                for(int i=0;i<800&&!commander.interactable;i++){driver.Advance(500000,1);hud.Refresh();}Assert.IsTrue(commander.interactable);
                var expected=(string)driver.HudState["ultimate"]["actor"];commander.onClick.Invoke();
                var commands=(JArray)JObject.Parse(driver.StateJson)["commands"];Assert.AreEqual(expected+"/"+(string)driver.HudState["ultimate"]["ability"],(string)commands[0]["skill"]);
                var barrier=Find("LiveTrainSkill_barrier");Assert.NotNull(barrier);barrier.onClick.Invoke();
                Assert.IsFalse(Find("LiveTrainSkill_emergency_repair").interactable);Assert.IsFalse(Find("LiveTrainSkill_support_fire").interactable);
                Assert.AreEqual(2,((JArray)JObject.Parse(driver.StateJson)["commands"]).Count);
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        static BasicCombatSimulation Fresh(){var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-local-producer/draft.json"));d["ownedSelection"]["accountId"]="a";return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);}
        [Test] public void ExistingPrefabButtonsSendExactlyOneCommandAndDisableWhileBusy()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab");var go=UnityEngine.Object.Instantiate(prefab);
            try{
                int saves=0;var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>{saves++;return r+1;},()=>true);driver.Open();driver.Advance(500000*12,12);
                var root=go.AddComponent<BattleSceneRoot>();root.BindCombat(driver,go);driver.Advance(500000*12,12);
                var hud=go.GetComponent<CombatLiveHudBinding>();hud.Bind(driver);hud.Bind(driver);
                var button=Array.Find(go.GetComponentsInChildren<Transform>(true),x=>x.name=="SkillIconSlot_0").GetComponent<Button>();
                Assert.IsTrue(button.interactable);var before=saves;button.onClick.Invoke();Assert.AreEqual(before+1,saves);Assert.IsFalse(button.interactable);
                var state=JObject.Parse(driver.StateJson);Assert.AreEqual(1,((JArray)state["commands"]).Count);
                driver.SetPaused(true);hud.Refresh();Assert.IsFalse(button.interactable);
                Assert.AreEqual(5,((JArray)driver.HudState["skills"]).Count);
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
