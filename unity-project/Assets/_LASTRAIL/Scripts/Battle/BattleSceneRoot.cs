using LastRail.Core.Simulation;
using UnityEngine;
using System.Linq;

namespace LastRail.Battle
{
    public sealed class BattleSceneRoot : MonoBehaviour
    {
        public static event System.Action CompositionChanged;
        private void OnEnable(){CompositionChanged?.Invoke();}
        private void OnDisable(){CompositionChanged?.Invoke();}
        private BattleSession session;
        private bool preparing;
        private Component[] releasing = new Component[0];

        public BattleSession Session => session;
        public CombatSceneDriver Combat { get; private set; }
        public string RecoveryNotice { get; private set; }
        public void BindCombat(CombatSceneDriver driver)
        {
            if(driver==null)throw new System.ArgumentNullException(nameof(driver));
            if(releasing.Any(c=>c!=null))throw new System.InvalidOperationException("Previous presentation cleanup has not completed.");
            preparing=true;Combat=null;
            try{driver.Open();Combat=driver;RecoveryNotice=null;preparing=false;}
            catch{RecoveryNotice="전투 화면을 준비하지 못했습니다. 다시 시도해 주세요.";throw;}
        }
        public void BindCombat(CombatSceneDriver driver, GameObject existingHud)
        {
            if(existingHud==null)throw new System.ArgumentNullException(nameof(existingHud));
            if(driver==null)throw new System.ArgumentNullException(nameof(driver));
            if(releasing.Any(c=>c!=null))throw new System.InvalidOperationException("Previous presentation cleanup has not completed.");
            preparing=true;Combat=null;
            var priorComponents=existingHud.GetComponentsInChildren<Component>(true);
            var priorTransforms=existingHud.GetComponentsInChildren<Transform>(true);
            var priorChildren=existingHud.transform.Cast<Transform>().Select(t=>(Child:t,Active:t.gameObject.activeSelf)).ToArray();
            CombatLiveHudBinding attemptedBinding=null;
            try{
            existingHud.GetComponent<CombatLiveHudBinding>()?.Unbind();
            existingHud.GetComponent<ApprovedCombatView>()?.SetCommandAvailability(()=>!preparing&&Combat==driver);
            driver.Open();
            var state=driver.HudState;
            var legacy=state["ultimate"] as Newtonsoft.Json.Linq.JObject;
            bool useApproved=state["ultimates"] is Newtonsoft.Json.Linq.JArray || (bool?)legacy?["chargeMode"]==true;
            var art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");
            var binding=existingHud.GetComponent<CombatLiveHudBinding>();
            if(art!=null&&useApproved){
                var view=existingHud.GetComponent<ApprovedCombatView>();
                if(view==null){foreach(Transform child in existingHud.transform)child.gameObject.SetActive(false);view=existingHud.AddComponent<ApprovedCombatView>();}
                view.SetCommandAvailability(()=>!preparing&&Combat==driver);
                view.BindOrRefresh(driver,art);
                if(binding!=null){binding.Unbind();binding.enabled=false;}
            }else{
                binding=binding??existingHud.AddComponent<CombatLiveHudBinding>();attemptedBinding=binding;binding.enabled=true;binding.Bind(driver);
            }
            Combat=driver;RecoveryNotice=null;preparing=false;
            }catch{
                attemptedBinding?.Unbind();
                var added=existingHud.GetComponentsInChildren<Component>(true).Where(c=>!priorComponents.Contains(c)).ToArray();
                releasing=added;
                foreach(var view in added.OfType<ApprovedCombatView>()){view.enabled=false;view.ReleaseOwnedPresentationResources();}
                foreach(var child in existingHud.GetComponentsInChildren<Transform>(true).Where(c=>!priorTransforms.Contains(c)&&priorTransforms.Contains(c.parent)).ToArray()){
                    child.gameObject.SetActive(false);child.SetParent(null,false);Release(child.gameObject);
                }
                foreach(var component in added.Reverse())if(component!=null&&!(component is Transform)){if(component is Behaviour behaviour)behaviour.enabled=false;if(priorTransforms.Contains(component.transform))Release(component);}
                foreach(var child in priorChildren)if(child.Child!=null)child.Child.gameObject.SetActive(child.Active);
                RecoveryNotice="전투 화면을 준비하지 못했습니다. 다시 시도해 주세요.";throw;
            }
        }
        static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}

        private void Awake()
        {
            session = new BattleSession(new SimulationClock());
        }

        private void Update()
        {
            if(preparing)return;
            if(Combat==null){session.Tick(Time.unscaledDeltaTime);return;}
            if(Combat.RecoveryRequired)return;
            try{Combat.Advance((long)System.Math.Round(Time.unscaledDeltaTime*System.TimeSpan.TicksPerSecond),8);}
            catch(System.Exception){RecoveryNotice="전투 기록을 저장하지 못했습니다. 복구 후 계속할 수 있습니다.";}
        }
        private void OnApplicationPause(bool paused)
        {
            // Returning from the background does not automatically resume gameplay.
            if(!paused||Combat==null||Combat.RecoveryRequired)return;
            try{Combat.SetPaused(true);}catch(System.Exception){RecoveryNotice="전투 기록 저장을 다시 확인해야 합니다.";}
        }
    }
}
