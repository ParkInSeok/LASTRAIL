using System;
using System.Linq;
using UnityEngine.EventSystems;

namespace LastRail.Battle
{
    // Borrows the prepared combat owner; owns only the presentation lease and callbacks.
    public sealed class OrdinaryPlayerBattleController : IDisposable
    {
        readonly OrdinaryFiveStartInput original;
        readonly CombatSceneDriver driver;
        CombatPlayerAssetBinding binding;
        readonly Func<bool> current;
        readonly Func<bool> foreground;
        CombatPlayerScene scene;
        bool disposed,retired;
        public bool IsCurrent => !disposed && !retired && !driver.IsRetired && current();
        public bool HasPresentation => scene != null && scene.IsOpen;
        public OrdinaryPlayerBattleController(OrdinaryFiveStartInput original,CombatSceneDriver driver,CombatPlayerAssetBinding binding,Func<bool> current,Func<bool> foreground=null)
        {
            this.original=original??throw new ArgumentNullException(nameof(original));
            this.driver=driver??throw new ArgumentNullException(nameof(driver));
            this.binding=binding??throw new ArgumentNullException(nameof(binding));
            this.current=current??throw new ArgumentNullException(nameof(current));
            this.foreground=foreground;
        }
        public void Present(CombatPlayerScene target,EventSystem input,Action leave,Action<CombatSceneResult> completed,Action<Exception> failed)
        {
            if(!IsCurrent||HasPresentation)throw new InvalidOperationException("Ordinary presentation owner is unavailable or already open.");
            if(target==null)throw new ArgumentNullException(nameof(target));
            if(driver.RecoveryRequired)driver.Open();
            driver.CaptureCommittedState();
            var lease=binding.Acquire(binding.ManifestHash,original.Actors.OrderBy(a=>a.Slot).Select(a=>a.OperatorId).ToArray());
            scene=target;
            try{
                target.Bind(driver,lease,input,()=>IsCurrent,
                    result=>{if(!IsCurrent)return;RetireDriver();scene=null;completed?.Invoke(result);},
                    error=>{if(!IsCurrent)return;RetireDriver();scene=null;failed?.Invoke(error);},foreground);
                target.Closed+=OnSceneClosed;
                target.View.GetComponent<CombatPauseController>()?.BindNavigation(null,()=>{if(!IsCurrent||driver.Outcome!=BasicCombatOutcome.Running)return;ClosePresentation();leave?.Invoke();});
            }catch{RetireDriver();scene=null;lease.Dispose();throw;}
        }
        void OnSceneClosed(){RetireDriver();scene=null;}
        void RetireDriver(){retired=true;if(scene!=null)scene.Closed-=OnSceneClosed;if(driver.IsNodeInstance)driver.Retire();}
        public void ClosePresentation(){RetireDriver();var previous=scene;scene=null;if(previous!=null)previous.Close();}
        public void Dispose(){if(disposed)return;disposed=true;try{ClosePresentation();}finally{binding=null;}}
    }
}
