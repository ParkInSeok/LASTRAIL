using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace LastRail.Battle
{
    // Binds existing prefab controls. No simulation clock, seed or HTTP sender here.
    public sealed class CombatLiveHudBinding : MonoBehaviour
    {
        sealed class Binding {internal Button Button;internal TMP_Text Label;internal UnityAction Click;internal int Slot;internal string Group="skills",Caption="",Actor,Ability;}
        readonly List<Binding> bindings=new List<Binding>();
        CombatSceneDriver driver; TMP_Text wave,notice;Button autoButton;Graphic autoThumb;UnityAction autoClick;
        public string LastCommandStatus { get; private set; }
        Transform Named(string name)=>GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name==name);
        public void Bind(CombatSceneDriver value)
        {
            Unbind();driver=value??throw new ArgumentNullException(nameof(value));
            var state=driver.ReadDisplayState()??throw new InvalidOperationException("Open combat before binding HUD.");
            bool individual=state["ultimates"] is JArray;bool generalAuto=(bool?)state["generalAuto"]??false;
            foreach(var old in GetComponentsInChildren<Transform>(true))if(old.name.StartsWith("LiveUltimate_")||old.name=="LiveCommanderSkill")old.gameObject.SetActive(false);
            var skills=(JArray)state["skills"];if(skills.Count!=5)throw new InvalidOperationException("Five active skills required.");
            wave=Named("WaveCounterText")?.GetComponent<TMP_Text>();notice=Named("RefundHintText")?.GetComponent<TMP_Text>();
            foreach(var name in new[]{"UnitRefsText","CueRefsText"}){var demo=Named(name);if(demo!=null)demo.gameObject.SetActive(false);}
            for(int i=0;i<5;i++)
            {
                var target=Named("SkillIconSlot_"+i)??throw new InvalidOperationException("Missing skill slot.");
                target.SetParent(Named("SafeArea")??transform,false);var slotRect=(RectTransform)target;slotRect.anchorMin=slotRect.anchorMax=new Vector2((i+.5f)/5f,0);slotRect.pivot=new Vector2(.5f,.5f);slotRect.anchoredPosition=new Vector2(0,170);slotRect.sizeDelta=new Vector2(80,80);
                var button=target.GetComponent<Button>()??target.gameObject.AddComponent<Button>();
                button.targetGraphic=target.GetComponent<Image>();if(button.targetGraphic!=null)button.targetGraphic.raycastTarget=true;
                var label=target.Find("LiveSkillState")?.GetComponent<TextMeshProUGUI>();
                if(label==null){var g=new GameObject("LiveSkillState",typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(target,false);label=g.GetComponent<TextMeshProUGUI>();var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;label.alignment=TextAlignmentOptions.Center;label.fontSize=20;label.raycastTarget=false;if(wave!=null)label.font=wave.font;}
                var binding=new Binding{Button=button,Label=label,Slot=i,Group=individual&&generalAuto?"ultimates":"skills"};var bound=Row(state,binding);binding.Actor=(string)bound["actor"];binding.Ability=(string)bound["ability"];binding.Click=()=>Click(binding);button.onClick.AddListener(binding.Click);bindings.Add(binding);
            }
            if((!(state["ultimate"] is JObject)&&!(state["ultimates"] is JArray))||!(state["manuals"] is JArray manuals)||manuals.Count!=3)throw new InvalidOperationException("Selected commander and three train skills required.");
            if(!(state["ultimates"] is JArray))AddControl("LiveCommanderSkill","분대장 스킬","ultimate",0,0);
            if(individual){if(!generalAuto)for(int i=0;i<5;i++){var row=state["ultimates"][i];AddControl("LiveUltimate_"+(string)row["actor"],"궁극기","ultimates",i,i,5,260,70);}BindAuto();}
            for(int i=0;i<manuals.Count;i++){var id=(string)manuals[i]["ability"];var caption=id=="barrier"?"방어막":id=="emergency_repair"?"긴급 수리":id=="support_fire"?"지원 사격":throw new InvalidOperationException("Unknown train skill label.");AddControl("LiveTrainSkill_"+id,caption,"manuals",i,i+1);}
            if(isActiveAndEnabled)driver.StateChanged+=OwnerChanged;Refresh();
        }
        JToken Row(JObject state,Binding binding)=>binding.Group=="ultimate"?state["ultimate"]:binding.Actor==null?state[binding.Group][binding.Slot]:((JArray)state[binding.Group]).Single(r=>(string)r["actor"]==binding.Actor&&(string)r["ability"]==binding.Ability);
        void AddControl(string name,string caption,string group,int index,int column,int columns=4,float y=18,float height=88)
        {
            var safe=Named("SafeArea")??transform;var target=Named(name);
            if(target==null){var g=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));g.transform.SetParent(safe,false);target=g.transform;var rect=(RectTransform)target;rect.anchorMin=new Vector2(column/(float)columns,0);rect.anchorMax=new Vector2((column+1)/(float)columns,0);rect.pivot=new Vector2(.5f,0);rect.offsetMin=new Vector2(14,y);rect.offsetMax=new Vector2(-14,y+height);g.GetComponent<Image>().color=new Color(.055f,.14f,.19f,.96f);}
            target.gameObject.SetActive(true);var button=target.GetComponent<Button>();button.targetGraphic=target.GetComponent<Image>();
            var label=target.GetComponentInChildren<TextMeshProUGUI>();if(label==null){var g=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(target,false);label=g.GetComponent<TextMeshProUGUI>();var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(5,4);rect.offsetMax=new Vector2(-5,-4);label.alignment=TextAlignmentOptions.Center;label.fontSize=23;label.raycastTarget=false;if(wave!=null)label.font=wave.font;}
            var b=new Binding{Button=button,Label=label,Slot=index,Group=group,Caption=caption};var bound=Row(driver.ReadDisplayState(),b);b.Actor=(string)bound["actor"];b.Ability=(string)bound["ability"];b.Click=()=>Click(b);button.onClick.AddListener(b.Click);bindings.Add(b);
        }
        void BindAuto(){
            var target=Named("LiveUltimateAuto");if(target==null){var go=new GameObject("LiveUltimateAuto",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(Named("SafeArea")??transform,false);target=go.transform;var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);rect.anchoredPosition=new Vector2(-20,-20);rect.sizeDelta=new Vector2(132,44);go.GetComponent<Image>().color=new Color(.035f,.08f,.1f);var label=new GameObject("AutoLabel",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();label.transform.SetParent(target,false);label.font=wave!=null?wave.font:TMP_Settings.defaultFontAsset;label.text="AUTO";label.fontSize=18;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(10,0);label.rectTransform.offsetMax=new Vector2(-38,0);var thumb=new GameObject("AutoThumb",typeof(RectTransform)).AddComponent<CombatAutoThumb>();thumb.transform.SetParent(target,false);thumb.raycastTarget=false;thumb.rectTransform.anchorMin=thumb.rectTransform.anchorMax=thumb.rectTransform.pivot=new Vector2(0,.5f);thumb.rectTransform.sizeDelta=new Vector2(18,18);}
            target.gameObject.SetActive(true);autoButton=target.GetComponent<Button>();autoButton.targetGraphic=target.GetComponent<Image>();autoThumb=target.Find("AutoThumb").GetComponent<Graphic>();autoClick=()=>{if(driver==null||!autoButton.interactable)return;driver.SetUltimateAuto(!(bool)driver.ReadDisplayState()["ultimateAuto"]);};autoButton.onClick.AddListener(autoClick);
        }
        void Click(Binding binding)
        {
            if(driver==null||!binding.Button.interactable)return;
            var row=(JObject)Row(driver.ReadDisplayState(),binding);
            try{LastCommandStatus=driver.RequestSkill(Guid.NewGuid().ToString("N"),(string)row["actor"],(string)row["ability"]);}
            catch(Exception){LastCommandStatus="RECOVERY_REQUIRED";}
        }
        public void Refresh()
        {
            if(driver==null)return;var state=driver.ReadDisplayState();if(state==null)return;
            if(wave!=null)wave.text="웨이브 "+state["wave"]+"/"+state["totalWaves"];
            if(notice!=null)notice.text=driver.RecoveryRequired?"전투 기록 복구가 필요합니다.":(bool)state["paused"]?"일시 정지":"열차 "+state["trainHp"]+" / "+state["trainMaxHp"];
            if(autoButton!=null){bool on=(bool?)state["ultimateAuto"]??false;autoButton.interactable=!driver.RecoveryRequired&&!(bool)state["paused"]&&(string)state["outcome"]=="Running";autoThumb.rectTransform.anchoredPosition=new Vector2(103,0);autoThumb.color=on?new Color(.2f,.85f,1):new Color(.16f,.21f,.23f);}
            foreach(var b in bindings){var s=Row(state,b);b.Button.interactable=!driver.RecoveryRequired&&(bool)s["enabled"];var status=(string)s["status"];var hint=status=="DOWN"?"회복 중":status=="CASTING"?"사용 중":status=="QUEUED"?"대기 중":status=="NO_TARGET"?"대상 없음":status=="RECOVERY"||status=="ATTACKING"?"행동 중":status=="PAUSED"?"일시 정지":status=="TERMINAL"?"전투 종료":CombatCooldownText.Format((long?)s["cooldownMs"]);b.Label.text=b.Caption+(b.Caption!=""&&hint!=""?"\n":"")+hint;}
        }
        void OwnerChanged(){if(driver.IsRetired)Unbind();else Refresh();}
        void OnEnable(){if(driver!=null){driver.StateChanged-=OwnerChanged;driver.StateChanged+=OwnerChanged;OwnerChanged();}}
        void OnDisable(){if(driver!=null)driver.StateChanged-=OwnerChanged;}
        public void Unbind(){if(driver!=null)driver.StateChanged-=OwnerChanged;if(autoButton!=null){autoButton.onClick.RemoveListener(autoClick);autoButton.interactable=false;autoButton.gameObject.SetActive(false);}autoButton=null;autoThumb=null;autoClick=null;foreach(var b in bindings){if(b.Button!=null){b.Button.onClick.RemoveListener(b.Click);b.Button.interactable=false;}}bindings.Clear();driver=null;}
        void OnDestroy()=>Unbind();
    }
}
