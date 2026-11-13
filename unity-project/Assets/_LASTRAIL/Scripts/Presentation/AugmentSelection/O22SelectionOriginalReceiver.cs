using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.AugmentSelection
{
    // Bind Commit to the existing controller. START hash is supplied by the
    // retained original-mode owner, never extracted from the arriving packet.
    public sealed class O22SelectionOriginalReceiver
    {
        readonly CombatO22ApplicationStore store;
        readonly AugmentSelectionScope scope;
        readonly string startHash;
        readonly Func<bool> ownerCurrent;
        readonly Action<CombatO22ApplicationOriginal> applyCommitted;
        public O22SelectionOriginalReceiver(CombatO22ApplicationStore store,AugmentSelectionScope scope,string retainedStartHash,Func<bool> ownerCurrent)
            :this(store,scope,retainedStartHash,ownerCurrent,null){}
        public O22SelectionOriginalReceiver(CombatO22ApplicationStore store,AugmentSelectionScope scope,string retainedStartHash,Func<bool> ownerCurrent,Action<CombatO22ApplicationOriginal> applyCommitted)
        {this.store=store??throw new ArgumentNullException(nameof(store));this.scope=scope??throw new ArgumentNullException(nameof(scope));startHash=retainedStartHash;this.ownerCurrent=ownerCurrent??throw new ArgumentNullException(nameof(ownerCurrent));this.applyCommitted=applyCommitted;}
        public void Commit(AugmentSelectionScope requestScope,AugmentSelectionSnapshot selection,string json)
        {
            if(!ownerCurrent()||requestScope==null||!ReferenceEquals(requestScope.Owner,scope.Owner)||requestScope.RunId!=scope.RunId||requestScope.EventId!=scope.EventId)throw new InvalidOperationException("Selection original owner changed");
            var original=CombatO22ApplicationOriginal.Read(json,scope.Owner.AccountId,scope.RunId,scope.EventId,scope.Owner.Environment,startHash);
            if(selection==null||!JToken.DeepEquals(JObject.Parse(original.SelectionJson),JObject.Parse(selection.Json)))throw new FormatException("Original receipt snapshot mismatch");
            store.Commit(original);
            if(!ownerCurrent())throw new InvalidOperationException("Original committed under previous owner; recover in its original scope");
            // Failure leaves the selection command pending. Retry reuses the same
            // acquisition and the native owner's idempotent application command.
            applyCommitted?.Invoke(original);
        }
    }
}
