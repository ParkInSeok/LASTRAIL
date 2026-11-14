using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Battle
{
    // Converts HUD intents to the existing driver commands; the driver remains authoritative.
    public sealed class ApprovedCombatInputController:IDisposable
    {
        readonly CombatSceneDriver driver;
        Func<bool> available;
        public ApprovedCombatInputController(CombatSceneDriver driver,Func<bool> available)
        {this.driver=driver??throw new ArgumentNullException(nameof(driver));this.available=available??throw new ArgumentNullException(nameof(available));}
        bool Current=>available!=null&&available()&&!driver.IsRetired&&!driver.RecoveryRequired;
        public void CycleSpeed(){if(!Current)return;int value=(int)driver.ReadDisplayState()["speedHalfUnits"];driver.SetSpeed(value==2?3:value==3?4:2);}
        public void ToggleUltimateAuto(){if(Current)driver.SetUltimateAuto(!(bool)driver.ReadDisplayState()["ultimateAuto"]);}
        public void Use(string group,int index)
        {
            if(!Current)return;var state=driver.ReadDisplayState();
            if(group!="skills"&&group!="manuals"&&group!="ultimate")return;
            var rows=state[group] as JArray;if(group!="ultimate"&&(rows==null||index<0||index>=rows.Count))return;
            Request(group=="ultimate"?state[group]:rows[index]);
        }
        public void UseGeneral(string actor){if(Current)Request(((JArray)driver.ReadDisplayState()["skills"]).Single(r=>(string)r["actor"]==actor));}
        public void UseUltimate(string actor,string ability){if(Current)Request(((JArray)driver.ReadDisplayState()["ultimates"]).Single(r=>(string)r["actor"]==actor&&(string)r["ability"]==ability));}
        public string UseBound(string group,string actor,string ability)
        {
            if(!Current)return null;var state=driver.ReadDisplayState();
            if(group=="ultimate")return Request(state[group]);
            if(group!="skills"&&group!="manuals"&&group!="ultimates")return null;
            return Request(((JArray)state[group]).Single(r=>(string)r["actor"]==actor&&(string)r["ability"]==ability));
        }
        string Request(JToken row){if(row==null||!(bool)row["enabled"])return null;return driver.RequestSkill(Guid.NewGuid().ToString("N"),(string)row["actor"],(string)row["ability"]);}
        public void FocusEnemy(string id){if(Current)driver.SetEnemyFocus(id);}
        public void PreviewRoute(int index){if(Current)driver.PreviewRailRoute(index);}
        public void ConfirmRoute()
        {
            if(!Current)return;var rail=driver.ReadDisplayState()["railJourney"];var offer=rail?["offer"] as JObject;if(offer==null)return;
            driver.ConfirmRailRoute((string)offer["id"],(string)offer["options"][(int)rail["previewIndex"]]["id"]);
        }
        public void ChangeThrottle(float value)
        {
            if(!Current)return;var state=driver.ReadDisplayState();var rail=state["railJourney"]??state["ordinaryRail"];if(rail?["minimumBps"]==null)return;
            driver.SetRailThrottle(Mathf.RoundToInt(Mathf.Lerp((int)rail["minimumBps"],(int)rail["maximumBps"],value)));
        }
        public void Dispose(){available=null;}
    }
}
