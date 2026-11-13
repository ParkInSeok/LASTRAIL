using System;
using UnityEngine.EventSystems;
namespace LastRail.Battle
{
    public enum Ce03BattleEntry { Presented, Joined, Result, KnownLifecycle }

    // Compose once above recovery and Continue for one account/run generation.
    // The external lifecycle owner supplies its existing permission; no GET here.
    public sealed class Ce03PlayerRunOwner : IDisposable
    {
        readonly Ce03OriginalStart original;
        readonly CombatPlayerAssetBinding binding;
        readonly CombatCheckpointStore checkpoints;
        readonly Ce03LocalClient client;
        readonly Func<bool> ownerCurrent,combatAllowed;
        Ce03PlayerBattleController controller;
        bool disposed,opening,resultPublished;
        bool Current=>!disposed&&ownerCurrent();
        public Ce03PlayerRunOwner(Ce03OriginalStart original,CombatPlayerAssetBinding binding,CombatCheckpointStore checkpoints,Ce03LocalClient client,Func<bool> ownerCurrent,Func<bool> combatAllowed)
        {
            this.original=original??throw new ArgumentNullException(nameof(original));
            this.binding=binding??throw new ArgumentNullException(nameof(binding));
            this.checkpoints=checkpoints??throw new ArgumentNullException(nameof(checkpoints));
            this.client=client??throw new ArgumentNullException(nameof(client));
            this.ownerCurrent=ownerCurrent??throw new ArgumentNullException(nameof(ownerCurrent));
            this.combatAllowed=combatAllowed??throw new ArgumentNullException(nameof(combatAllowed));
        }
        public Ce03PlayerBattleController Resolve(Ce03OriginalStart requested)
        {
            if(!Current||opening)throw new InvalidOperationException("Current run owner unavailable.");
            if(requested==null||!ReferenceEquals(requested,original)&&requested.Json!=original.Json)throw new InvalidOperationException("Another original START cannot join this run.");
            if(!combatAllowed()){controller?.ClosePresentation();return null;}
            if(controller!=null){if(controller.RecoveryRequired&&!controller.HasPresentation){checkpoints.RequireDurableLocalStart(original);controller.Restore();}return controller;}
            opening=true;
            try{
                // A cleared/legacy marker is continuation, never permission to
                // reconstruct a missing checkpoint from an old STARTED receipt.
                if(client.AwaitingCheckpointKey!=original.Key)checkpoints.RequireDurableLocalStart(original);
                var next=new Ce03PlayerBattleController(original,binding,checkpoints,()=>Current&&combatAllowed(),client);
                if(!Current||!combatAllowed()){next.Dispose();throw new InvalidOperationException("Run owner changed during entry.");}
                controller=next;return next;
            }finally{opening=false;}
        }
        public Ce03BattleEntry Present(Ce03OriginalStart requested,CombatPlayerScene scene,EventSystem input,Action<CombatSceneResult> completed,Action<Exception> failed)
        {
            var active=Resolve(requested);if(active==null)return Ce03BattleEntry.KnownLifecycle;
            var terminal=active.SavedResult;
            if(terminal!=null){if(!resultPublished){resultPublished=true;completed?.Invoke(terminal);}return Ce03BattleEntry.Result;}
            if(active.HasPresentation)return Ce03BattleEntry.Joined;
            active.Present(scene,input,result=>{resultPublished=true;completed?.Invoke(result);},failed);
            return Ce03BattleEntry.Presented;
        }
        public void Dispose(){if(disposed)return;disposed=true;controller?.Dispose();controller=null;}
    }
}
