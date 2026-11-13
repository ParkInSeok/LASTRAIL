using System;
using UnityEngine.EventSystems;
namespace LastRail.Battle
{
    // One confirmed local run. Presentation may be replaced; the original and checkpoint owner persist.
    public sealed class Ce03PlayerBattleController : IDisposable
    {
        readonly Ce03OriginalStart original;
        readonly CombatPlayerAssetBinding binding;
        readonly CombatSceneDriver driver;
        readonly Func<bool> ownerCurrent;
        CombatPlayerScene presentation;
        bool disposed;
        public bool HasPresentation=>presentation!=null&&presentation.IsOpen;
        public bool RecoveryRequired=>driver.RecoveryRequired;
        public CombatSceneResult SavedResult=>driver.Outcome==BasicCombatOutcome.Running?null:new CombatSceneResult(driver.Outcome,driver.Tick);
        public Ce03PlayerBattleController(Ce03OriginalStart original,CombatPlayerAssetBinding binding,CombatCheckpointStore checkpoints,Func<bool> ownerCurrent,Ce03LocalClient departureClient=null)
        {
            this.original=original??throw new ArgumentNullException(nameof(original));this.binding=binding??throw new ArgumentNullException(nameof(binding));
            this.ownerCurrent=ownerCurrent??throw new ArgumentNullException(nameof(ownerCurrent));if(checkpoints==null)throw new ArgumentNullException(nameof(checkpoints));
            var factory=original.CreateVerifiedPlayerFactory(binding);
            driver=new CombatSceneDriver(factory,()=>checkpoints.RestoreLocalStartWithRevision(original,factory),(model,revision)=>checkpoints.SaveLocalStart(original,model,revision),()=>!disposed&&ownerCurrent());
            driver.Open();
            if(!ownerCurrent())throw new InvalidOperationException("Run owner changed before checkpoint handoff.");
            departureClient?.CompleteCheckpointHandoff(original,checkpoints);
        }
        public void Present(CombatPlayerScene scene,EventSystem input,Action<CombatSceneResult> completed,Action<Exception> failed)
        {
            if(disposed||!ownerCurrent())throw new InvalidOperationException("Run owner is no longer current.");
            if(HasPresentation)throw new InvalidOperationException("Close the existing run presentation first.");
            presentation=null;
            if(scene==null)throw new ArgumentNullException(nameof(scene));
            var prepared=original.Prepare();var formation=Ce03PlayerAssetContract.Validate(prepared["playerAssetBinding"] as Newtonsoft.Json.Linq.JObject,prepared["ownedSelection"] as Newtonsoft.Json.Linq.JObject);
            var lease=binding.Acquire(Ce03PlayerAssetContract.ManifestHash,formation);presentation=scene;
            try{scene.Bind(driver,lease,input,()=>!disposed&&ownerCurrent(),result=>{presentation=null;completed?.Invoke(result);},error=>{presentation=null;if(failed!=null)failed(error);else UnityEngine.Debug.LogException(error);});}
            catch{presentation=null;lease.Dispose();throw;}
        }
        public void ClosePresentation(){var previous=presentation;presentation=null;if(previous!=null)previous.Close();}
        public void Restore(){if(disposed||HasPresentation)throw new InvalidOperationException("Close the scene before restoring the retained run.");presentation=null;driver.Open();}
        public void Dispose(){if(disposed)return;disposed=true;ClosePresentation();}
    }
}

