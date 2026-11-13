using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.AugmentSelection
{
    // The host must replace this owner on login/logout/session-generation changes.
    // This is a binding identity, never an authentication credential.
    public sealed class AugmentSelectionOwner
    {
        public string AccountId { get; }
        public string Environment { get; }
        public AugmentSelectionOwner(string accountId,string environment)
        {AccountId=Check(accountId);Environment=Check(environment);}
        internal static string Check(string value)
        {if(string.IsNullOrWhiteSpace(value)||value.Length>200)throw new ArgumentException("Selection identity");return value;}
    }
    public sealed class AugmentSelectionScope
    {
        public AugmentSelectionOwner Owner { get; }
        public string RunId { get; }
        public string EventId { get; }
        public string StorageKey { get; }
        public AugmentSelectionScope(AugmentSelectionOwner owner,string runId,string eventId)
        {
            Owner=owner??throw new ArgumentNullException(nameof(owner));RunId=AugmentSelectionOwner.Check(runId);EventId=AugmentSelectionOwner.Check(eventId);
            StorageKey=new JArray(owner.AccountId,owner.Environment,RunId,EventId).ToString(Formatting.None);
        }
    }
    // SELECT has a public adapter; read/reroll contracts remain host-specific.
    // Implementations authenticate the captured owner; they must never read a newer global login.
    public interface IAugmentSelectionPort
    {
        Task<string> ReadAsync(AugmentSelectionScope scope,CancellationToken cancellation);
        Task<string> ExecuteAsync(AugmentSelectionScope scope,AugmentSelectionCommand command,CancellationToken cancellation);
    }
    // A production adapter must durably commit before returning and provide compare-and-swap.
    // No production persistence adapter is installed by this isolated component.
    public interface IAugmentSelectionJournal
    {
        string Load(string scopeKey);
        void SaveNew(string scopeKey,string serializedCommand);
        void Complete(string scopeKey,string expectedSerializedCommand);
    }
    public sealed class AugmentSelectionRejectedException:Exception
    {
        // Only a definitive server rejection may use this type. Timeouts/malformed replies are ambiguous.
        public bool RequiresSuppliedSnapshot { get; }
        public AugmentSelectionRejectedException(string code):this(code,false){}
        public AugmentSelectionRejectedException(string code,bool requiresSuppliedSnapshot):base(code){RequiresSuppliedSnapshot=requiresSuppliedSnapshot;}
    }
}
