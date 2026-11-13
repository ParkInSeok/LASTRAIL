using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Submissions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Run
{
    public sealed class EncryptedRunOperationJournal : IRunOperationJournal,IOrdinaryCheckpointJournal,ITravelConflictJournal
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private const int MaximumJournalCharacters = 3 * 1024 * 1024;
        private readonly IAndroidKeystoreNativeStoragePort storage;
        private readonly string accountScope;
        public EncryptedRunOperationJournal(IAndroidKeystoreNativeStoragePort storage)
            : this(storage, null) { }
        internal EncryptedRunOperationJournal(IAndroidKeystoreNativeStoragePort storage, string accountScope)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            if (accountScope != null && !PendingReceipt.Identifier(accountScope, 1)) throw new ArgumentException("Invalid journal scope.");
            this.accountScope = accountScope;
        }
        public async Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)
        {
            if (!PendingReceipt.Identifier(account, 1) || accountScope != null && account != accountScope) throw new ArgumentException("Invalid run account.");
            await Gate.WaitAsync();
            try { return Array.AsReadOnly(Load().Where(r => r.AccountId == account).ToArray()); }
            finally { Gate.Release(); }
        }
        public async Task<bool> PutAsync(PendingRunOperation operation)
        {
            if (operation == null || operation.AcceptedResponseJson != null || accountScope != null && operation.AccountId != accountScope) return false;
            await Gate.WaitAsync();
            try
            {
                var rows = Load(); var original = rows.FirstOrDefault(r => Identity(r, operation.AccountId, operation.Kind, operation.IdempotencyKey));
                if (original != null) { if (!original.SameRequest(operation)) return false; }
                else
                {
                    // Keep only the latest travel receipt or its next pending command, in one durable write.
                    rows.RemoveAll(r=>operation.ReplacesConfirmedTravel(r));
                    if (rows.Count >= 32 || rows.Any(r => r.AccountId == operation.AccountId && r.Kind == operation.Kind && r.TargetId == operation.TargetId)) return false;
                    rows.Add(operation);
                }
                return Save(rows);
            }
            catch { return false; }
            finally { Gate.Release(); }
        }
        public async Task<bool> ConfirmAsync(PendingRunOperation operation, string responseJson)
        {
            if (operation == null || accountScope != null && operation.AccountId != accountScope) return false;
            await Gate.WaitAsync();
            try
            {
                var rows = Load(); var index = rows.FindIndex(r => Identity(r, operation.AccountId, operation.Kind, operation.IdempotencyKey));
                if (index < 0 || !rows[index].SameRequest(operation)) return false;
                rows[index] = rows[index].Confirmed(responseJson);
                return Save(rows);
            }
            catch { return false; }
            finally { Gate.Release(); }
        }
        public async Task<bool> ReplaceRejectedTravelAsync(PendingRunOperation rejected,PendingRunOperation recovery,string conflictJson)
        {
            if(rejected?.Kind!=PendingRunKind.TravelCommand||recovery?.Kind!=PendingRunKind.TravelCommand||rejected.AccountId!=recovery.AccountId||rejected.TargetId!=recovery.TargetId||rejected.IdempotencyKey==recovery.IdempotencyKey||recovery.AcceptedResponseJson!=null||accountScope!=null&&accountScope!=rejected.AccountId)return false;
            await Gate.WaitAsync();
            try{
                var original=BTravelCommand.Read(rejected.BodyJson);var current=BTravelReceipt.ReadConflictCurrent(conflictJson,original);current?.RequireScope(rejected.AccountId,rejected.TargetId);
                var next=BTravelCommand.Read(recovery.BodyJson);
                if(next.Kind!="RECOVER"||next.ExpectedRevision!=(current?.Revision??0)||next.DestinationEventId!=original.DestinationEventId||next.NodeId!=original.NodeId||next.MapRevision!=original.MapRevision)return false;
                var rows=Load();var index=rows.FindIndex(r=>Identity(r,rejected.AccountId,rejected.Kind,rejected.IdempotencyKey));
                if(index<0||!rows[index].SameRequest(rejected)||rows[index].AcceptedResponseJson!=null)return false;
                rows[index]=recovery;return Save(rows);
            }catch{return false;}
            finally{Gate.Release();}
        }
        public async Task<bool> ConfirmFirstCheckpointAsync(PendingRunOperation operation,long revision)
        {
            if(operation==null||operation.Kind!=PendingRunKind.InitializeOrdinary||accountScope!=null&&operation.AccountId!=accountScope)return false;
            await Gate.WaitAsync();
            try{
                var rows=Load();var index=rows.FindIndex(r=>Identity(r,operation.AccountId,operation.Kind,operation.IdempotencyKey));
                if(index<0||!rows[index].SameRequest(operation)||rows[index].AcceptedResponseJson!=operation.AcceptedResponseJson)return false;
                var next=rows[index].WithOrdinaryCheckpoint(revision);
                if(rows[index].OrdinaryCheckpointRevision==revision)return true;
                rows[index]=next;return Save(rows);
            }catch{return false;}finally{Gate.Release();}
        }
        public async Task<bool> RemoveConfirmedAsync(string account, PendingRunKind kind, string key)
        {
            // shortcut: retain initialization proof until verified ordinary terminal cleanup is connected.
            if(kind==PendingRunKind.InitializeOrdinary)return false;
            if (!PendingReceipt.Identifier(account, 1) || accountScope != null && account != accountScope || !RunRequestKey.IsSupported(key,kind==PendingRunKind.ConfirmDestination||kind==PendingRunKind.InitializeOrdinary||kind==PendingRunKind.CompleteOrdinaryNode?1:16) || (kind==PendingRunKind.ConfirmDestination||kind==PendingRunKind.InitializeOrdinary||kind==PendingRunKind.CompleteOrdinaryNode)&&key.Contains(" ") || !Enum.IsDefined(typeof(PendingRunKind), kind)) return false;
            await Gate.WaitAsync();
            try
            {
                var rows = Load(); var record = rows.FirstOrDefault(r => Identity(r, account, kind, key));
                if (record != null && record.AcceptedResponseJson == null) return false;
                rows.RemoveAll(r => Identity(r, account, kind, key)); return Save(rows);
            }
            catch { return false; }
            finally { Gate.Release(); }
        }
        private void Available()
        {
            if (!storage.IsAndroidRuntime || !storage.IsKeystoreAvailable || storage.UsedPlayerPrefsFallback)
                throw new InvalidOperationException("Secure run storage is unavailable.");
        }
        // Used only in a private staging directory before atomic publication.
        // It cannot replace a published snapshot or rewrite a legacy journal.
        internal async Task<bool> CreateScopedSnapshotAsync(IReadOnlyList<PendingRunOperation> records)
        {
            if (accountScope == null || records == null || records.Count > 32 || records.Any(r => r == null || r.AccountId != accountScope)) return false;
            await Gate.WaitAsync();
            try
            {
                Available();
                if (storage.Load() != null) return false;
                if (records.GroupBy(r => new { r.Kind, r.IdempotencyKey }).Any(g => g.Count() != 1)
                    || records.GroupBy(r => new { r.Kind, r.TargetId }).Any(g => g.Count() != 1)) return false;
                return Save(records.ToList());
            }
            finally { Gate.Release(); }
        }
        private List<PendingRunOperation> Load()
        {
            Available(); var encrypted = storage.Load();
            if (encrypted == null) return new List<PendingRunOperation>();
            var text = storage.Decrypt(encrypted);
            if (text == null || text.Length > MaximumJournalCharacters) throw new FormatException("Invalid run journal size.");
            JObject root;
            using (var reader = new JsonTextReader(new StringReader(text)) { DateParseHandling = DateParseHandling.None, MaxDepth = 32 })
            {
                root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
                if (reader.Read() || root.Descendants().Any(token => token.Type == JTokenType.Comment))
                    throw new FormatException("Invalid run journal content.");
            }
            Fields(root, accountScope == null ? new[] { "schema", "records" } : new[] { "schema", "accountId", "records" });
            if (root["schema"]?.Type != JTokenType.Integer || (accountScope == null ? ((int)root["schema"] != 1 && (int)root["schema"] != 2)
                : ((int)root["schema"] != 3 || GameApiJson.String(root, "accountId") != accountScope)) || !(root["records"] is JArray records) || records.Count > 32)
                throw new FormatException("Invalid run journal schema.");
            var rows = new List<PendingRunOperation>();
            foreach (var token in records)
            {
                if (!(token is JObject item)) throw new FormatException("Invalid run journal entry.");
                var fields=new[]{"kind", "accountId", "targetId", "idempotencyKey", "bodyJson", "appVersion", "appVersionCode", "composedAt", "acceptedResponseJson"};
                var expected=(int)root["schema"]>=2?fields.Concat(new[]{"responseVersion"}):fields.AsEnumerable();
                if(item.Property("ordinaryCheckpointRevision")!=null)expected=expected.Concat(new[]{"ordinaryCheckpointRevision"});
                if(item.Property("mapArrivalBindingJson")!=null)expected=expected.Concat(new[]{"mapArrivalBindingJson"});Fields(item,expected.ToArray());
                var name = GameApiJson.String(item, "kind");
                if (!Enum.TryParse(name, out PendingRunKind kind) || kind.ToString() != name) throw new FormatException("Invalid run operation kind.");
                var version = GameApiJson.String(item, "appVersion", true); var codeText = GameApiJson.String(item, "appVersionCode", true);
                long? code = null;
                if (codeText != null)
                {
                    if (!long.TryParse(codeText, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0
                        || value.ToString(CultureInfo.InvariantCulture) != codeText) throw new FormatException("Invalid composed run build.");
                    code = value;
                }
                var build = new AppBuildSnapshot(version, code);
                if (build.AppVersion != version) throw new FormatException("Invalid composed version label.");
                var op = new PendingRunOperation(kind, GameApiJson.String(item, "accountId"), GameApiJson.String(item, "targetId"),
                    GameApiJson.String(item, "idempotencyKey"), GameApiJson.String(item, "bodyJson"), build,
                    PendingRunOperation.ReadTime(item, "composedAt"), GameApiJson.String(item, "acceptedResponseJson", true),
                    (int)root["schema"]>=2?GameApiJson.String(item,"responseVersion",true):null,
                    item["ordinaryCheckpointRevision"]==null?null:item["ordinaryCheckpointRevision"].Type==JTokenType.Integer?(long?)item["ordinaryCheckpointRevision"]:throw new FormatException("Invalid first checkpoint marker."),GameApiJson.String(item,"mapArrivalBindingJson",true));
                if (accountScope != null && op.AccountId != accountScope) throw new FormatException("Run record belongs to another account.");
                if (rows.Any(r => r.AccountId == op.AccountId && r.Kind == op.Kind && (r.IdempotencyKey == op.IdempotencyKey || r.TargetId == op.TargetId)))
                    throw new FormatException("Duplicate run operation identity.");
                rows.Add(op);
            }
            return rows;
        }
        private bool Save(List<PendingRunOperation> rows)
        {
            Available();bool versioned=accountScope!=null||rows.Any(r=>r.ResponseVersion!=null);
            var records=new JArray(rows.Select(r=>{var entry=Encode(r);if(r.MapArrivalBindingJson!=null)entry["mapArrivalBindingJson"]=r.MapArrivalBindingJson;if(versioned)entry["responseVersion"]=r.ResponseVersion;if(r.OrdinaryCheckpointRevision.HasValue)entry["ordinaryCheckpointRevision"]=r.OrdinaryCheckpointRevision.Value;return entry;}));
            var root = new JObject { ["schema"] = accountScope!=null?3:versioned?2:1, ["records"] = records };
            if (accountScope != null) root["accountId"] = accountScope;
            var text = root.ToString(Formatting.None);
            // Reserve confirmation space before accepting any pending operation.
            var reserved = rows.Where(r => r.AcceptedResponseJson == null).Sum(r=>r.ReservedResponseCharacters);
            if (text.Length + reserved > MaximumJournalCharacters) return false;
            var encrypted = storage.Encrypt(text); return encrypted != null && storage.Save(encrypted);
        }
        private static JObject Encode(PendingRunOperation op) => new JObject
        {
            ["kind"] = op.Kind.ToString(), ["accountId"] = op.AccountId, ["targetId"] = op.TargetId,
            ["idempotencyKey"] = op.IdempotencyKey, ["bodyJson"] = op.BodyJson, ["appVersion"] = op.Build.AppVersion,
            ["appVersionCode"] = op.Build.AppVersionCode?.ToString(CultureInfo.InvariantCulture),
            ["composedAt"] = op.ComposedAt.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
            ["acceptedResponseJson"] = op.AcceptedResponseJson
        };
        private static bool Identity(PendingRunOperation op, string account, PendingRunKind kind, string key) =>
            op.AccountId == account && op.Kind == kind && op.IdempotencyKey == key;
        private static void Fields(JObject value, params string[] fields)
        { if (value.Count != fields.Length || value.Properties().Any(p => !fields.Contains(p.Name))) throw new FormatException("Unexpected run journal fields."); }
    }
}
