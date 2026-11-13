using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;

namespace LastRail.Run
{
    public sealed class DurableRunClient : IDisposable
    {
        private readonly IGameApiAuthenticatedHttpTransport transport;
        private readonly IRunOperationJournal journal;
        private readonly Func<GameApiAuthSessionSnapshot> currentSession;
        private readonly Func<long> generation;
        private readonly Func<AppBuildSnapshot> capture;
        private readonly Func<DateTimeOffset> now;
        private IReadOnlyList<PendingRunOperation> operations = Array.Empty<PendingRunOperation>();
        private string publishedAccount;
        private long publishedGeneration;
        // Validated wire responses awaiting durable storage, never published as completion.
        // Bounded by the journal's 32 pending records and scoped to one account generation.
        private readonly List<PendingRunOperation> awaitingStorage = new List<PendingRunOperation>();
        private string storageAccount;
        private long storageGeneration;
        private bool disposed;
        private TaskCompletionSource<bool> idle;
        public IReadOnlyList<PendingRunOperation> Operations => Current(publishedAccount, publishedGeneration)
            ? operations : Array.Empty<PendingRunOperation>();
        public bool Busy { get; private set; }
        public string Notice { get; private set; } = "";
        private string lastErrorCode;
        private string errorAccount;
        private long errorGeneration;
        private string travelConflictJson;
        private string travelConflictKey;
        public string LastTravelConflictJson=>Current(errorAccount,errorGeneration)?travelConflictJson:null;
        public string LastErrorCode => Current(errorAccount,errorGeneration) ? lastErrorCode : null;

        public DurableRunClient(IGameApiAuthenticatedHttpTransport transport, IRunOperationJournal journal,
            Func<GameApiAuthSessionSnapshot> currentSession, Func<long> generation,
            Func<AppBuildSnapshot> capture = null, Func<DateTimeOffset> now = null)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.currentSession = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
            this.generation = generation ?? throw new ArgumentNullException(nameof(generation));
            this.capture = capture ?? AppBuildSnapshot.CaptureInstalled;
            this.now = now ?? (() => DateTimeOffset.UtcNow);
        }

        public async Task RestoreAsync()
        {
            var owner = currentSession();
            if (disposed || owner == null) return;
            var account = owner.AccountId; var origin = generation();
            while (Busy)
            {
                await idle.Task;
                if (!Current(account, origin)) return;
            }
            if (!Current(account, origin)) return;
            Begin();
            try
            {
                var rows = await ReadAsync(account);
                if (!Current(account, origin)) return;
                Publish(rows, account, origin); Notice = "";
            }
            catch
            {
                if (Current(account, origin))
                {
                    operations = Array.Empty<PendingRunOperation>();
                    Notice = "Run recovery storage could not be read.";
                }
                throw;
            }
            finally { End(); }
        }

        public Task<PendingRunOperation> StartAsync(RunStartRequest request) => request == null
            ? Task.FromResult<PendingRunOperation>(null)
            : SubmitAsync(PendingRunKind.Start, request.OperationPreviewId, request.IdempotencyKey, request.BodyJson,
                account => PendingRunOperation.Start(account, request, capture(), now()), null);

        public Task<PendingRunOperation> FinishAsync(RunResultSubmitRequest request) => request == null
            ? Task.FromResult<PendingRunOperation>(null)
            : SubmitAsync(PendingRunKind.Finish, request.RunId, request.IdempotencyKey, request.BodyJson,
                account => PendingRunOperation.Finish(account, request, capture(), now()), null);

        public Task<PendingRunOperation> ClaimAsync(RunRewardClaimRequest request) => request == null
            ? Task.FromResult<PendingRunOperation>(null)
            : SubmitAsync(PendingRunKind.Claim,request.RunId,request.IdempotencyKey,request.BodyJson,
                account=>PendingRunOperation.Claim(account,request,capture(),now()),null);

        // Explicit opt-in; existing callers and pending legacy operations stay on
        // their original contract. The journal reserves space before any POST.
        public Task<PendingRunOperation> ClaimCompleteAsync(RunRewardClaimRequest request) => request == null
            ? Task.FromResult<PendingRunOperation>(null)
            : SubmitAsync(PendingRunKind.Claim,request.RunId,request.IdempotencyKey,request.BodyJson,
                account=>PendingRunOperation.CompleteClaim(account,request,capture(),now()),null,RunCompleteClaimReceipt.Version);

        public Task<PendingRunOperation> CompleteOrdinaryNodeAsync(OrdinaryNodeCompletion original)
        {
            if(original==null)throw new ArgumentNullException(nameof(original));
            if(currentSession()?.AccountId!=original.AccountId)return Task.FromResult<PendingRunOperation>(null);
            return SubmitAsync(PendingRunKind.CompleteOrdinaryNode,original.RunId,original.Key,original.BodyJson,
                account=>account==original.AccountId?new PendingRunOperation(PendingRunKind.CompleteOrdinaryNode,account,original.RunId,original.Key,original.BodyJson,capture(),now()):throw new InvalidOperationException("Original account changed."),null);
        }
        public Task<PendingRunOperation> InitializeOrdinaryAsync(LastRail.Battle.OrdinaryFiveStartInput original,string key)
        {
            if(original==null)throw new ArgumentNullException(nameof(original));
            if(currentSession()?.AccountId!=original.AccountId)return Task.FromResult<PendingRunOperation>(null);
            return SubmitAsync(PendingRunKind.InitializeOrdinary,original.RunId,key,"{}",
                account=>account==original.AccountId?new PendingRunOperation(PendingRunKind.InitializeOrdinary,account,original.RunId,key,"{}",capture(),now(),null,null,0):throw new InvalidOperationException("Original account changed."),
                null,null,json=>OrdinaryInitializationReceipt.Validate(json,original));
        }
        // Initialization recovery requires the retained START again; never confirm against a different source.
        public Task<PendingRunOperation> RetryAsync(PendingRunOperation operation) => operation == null||operation.Kind==PendingRunKind.InitializeOrdinary
            ? Task.FromResult<PendingRunOperation>(null)
            : SubmitAsync(operation.Kind, operation.TargetId, operation.IdempotencyKey, operation.BodyJson, null, operation,operation.ResponseVersion);

        public Task<PendingRunOperation> TravelAsync(string runId,BTravelCommand command)
        {
            if(command==null)throw new ArgumentNullException(nameof(command));
            return SubmitAsync(PendingRunKind.TravelCommand,runId,command.RequestId,command.BodyJson,
                account=>PendingRunOperation.Travel(account,runId,command,capture(),now()),null);
        }
        public Task<PendingRunOperation> ConfirmDestinationAsync(BOperationDestinationIntent intent)
        {
            if(intent==null)throw new ArgumentNullException(nameof(intent));
            if(currentSession()?.AccountId!=intent.AccountId)return Task.FromResult<PendingRunOperation>(null);
            return SubmitAsync(PendingRunKind.ConfirmDestination,intent.RunId,intent.CommandId,intent.Body().ToString(Newtonsoft.Json.Formatting.None),
                account=>PendingRunOperation.ConfirmDestination(account,intent,capture(),now()),null);
        }
        public Task<PendingRunOperation> ArriveMapAsync(BRouteCommand command)
        {
            if(command==null||command.IsWave||command.Kind!="ARRIVE")throw new ArgumentException("Map arrival command required.");
            if(currentSession()?.AccountId!=command.Binding.AccountId)return Task.FromResult<PendingRunOperation>(null);
            return SubmitAsync(PendingRunKind.MapArrival,command.Binding.RunId,command.EventId,command.Capture().ToString(Newtonsoft.Json.Formatting.None),
                account=>PendingRunOperation.ArriveMap(account,command,capture(),now()),null,mapBinding:command.Binding.Capture().ToString(Newtonsoft.Json.Formatting.None));
        }
        public async Task<PendingRunOperation> RecoverTravelConflictAsync(PendingRunOperation rejected,string newKey)
        {
            var owner=currentSession();if(disposed||Busy||owner==null||rejected?.Kind!=PendingRunKind.TravelCommand||rejected.AccountId!=owner.AccountId||journal is not ITravelConflictJournal conflicts||LastTravelConflictJson==null||travelConflictKey!=rejected.IdempotencyKey)return null;
            string account=owner.AccountId;long stamp=generation();var conflict=LastTravelConflictJson;PendingRunOperation recovery=null;Begin();
            try{
                var original=BTravelCommand.Read(rejected.BodyJson);var latest=BTravelReceipt.ReadConflictCurrent(conflict,original);latest?.RequireScope(account,rejected.TargetId);
                var body=Newtonsoft.Json.Linq.JObject.Parse(original.BodyJson);body.Remove("bps");body.Remove("value");body["requestId"]=newKey;body["kind"]="RECOVER";body["expectedRevision"]=latest?.Revision??0;
                recovery=PendingRunOperation.Travel(account,rejected.TargetId,BTravelCommand.Read(body.ToString(Newtonsoft.Json.Formatting.None)),capture(),now());
                if(!Current(account,stamp)||!await conflicts.ReplaceRejectedTravelAsync(rejected,recovery,conflict)||!Current(account,stamp))return null;
                var rows=await ReadAsync(account);if(!Current(account,stamp))return null;Publish(rows,account,stamp);
            }catch{if(Current(account,stamp))Notice="Travel conflict recovery could not be saved. The original request is retained.";return null;}
            finally{End();}
            return Current(account,stamp)?await RetryAsync(recovery):null;
        }

        private async Task<PendingRunOperation> SubmitAsync(PendingRunKind kind, string target, string key, string body,
            Func<string, PendingRunOperation> compose, PendingRunOperation supplied,string responseVersion=null,Action<string> validateResponse=null,string mapBinding=null)
        {
            var owner = currentSession();
            if (disposed || Busy || owner == null || (supplied != null && supplied.AccountId != owner.AccountId)) return null;
            var account = owner.AccountId; var origin = generation();
            lastErrorCode = null;
            travelConflictJson=null;travelConflictKey=null;
            Begin();
            try
            {
                var rows = await ReadAsync(account);
                if (!Current(account, origin)) return null;
                Publish(rows, account, origin);
                if(kind==PendingRunKind.InitializeOrdinary&&OrdinaryContinuationBlocked(rows,account,target))return null;
                var operation = rows.FirstOrDefault(r => r.Kind == kind && r.IdempotencyKey == key);
                if (operation != null)
                {
                    if (operation.TargetId != target || operation.BodyJson != body || operation.ResponseVersion!=responseVersion || kind==PendingRunKind.MapArrival&&supplied==null&&operation.MapArrivalBindingJson!=mapBinding || (supplied != null && !operation.SameRequest(supplied)))
                    { Notice = "The saved run request differs from this request."; return null; }
                }
                else
                {
                    if (compose == null || rows.Any(r => r.Kind == kind && r.TargetId == target && !((kind==PendingRunKind.TravelCommand||kind==PendingRunKind.MapArrival||kind==PendingRunKind.ConfirmDestination)&&r.AcceptedResponseJson!=null)))
                    { Notice = "Use the original saved run operation."; return null; }
                    operation = compose(account);
                    if (!Current(account, origin)) return null;
                    var saved = await journal.PutAsync(operation);
                    if (!Current(account, origin)) return null;
                    if (!saved) { Notice = "Run request could not be saved. Nothing was sent."; return null; }
                    rows.RemoveAll(r=>operation.ReplacesConfirmedTravel(r));rows.Add(operation); Publish(rows, account, origin);
                }
                // A supplied record is never proof of confirmation; only the journal's record is used.
                if (operation.AcceptedResponseJson != null)
                {
                    validateResponse?.Invoke(operation.AcceptedResponseJson);
                    operation.Confirmed(operation.AcceptedResponseJson);
                    Notice = ""; return operation;
                }
                var buffered = awaitingStorage.FirstOrDefault(r => r.SameRequest(operation));
                string json;
                if (buffered != null) json = buffered.AcceptedResponseJson;
                else
                {
                var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("Idempotency-Key",operation.IdempotencyKey)};
                if(kind==PendingRunKind.CompleteOrdinaryNode||kind==PendingRunKind.ConfirmDestination)headers.Add(new RunApiHttpHeader("Content-Type","application/json"));
                if(kind==PendingRunKind.TravelCommand){headers.Add(new RunApiHttpHeader("Content-Type","application/json"));headers.Add(new RunApiHttpHeader("X-B-Arrival-Version",BTravelCommand.Version));}
                if(kind==PendingRunKind.MapArrival){headers.Add(new RunApiHttpHeader("Content-Type","application/json"));headers.Add(new RunApiHttpHeader("X-B-Route-Version",BRouteBinding.Version));}
                if(operation.ResponseVersion!=null)headers.Add(new RunApiHttpHeader("X-Run-Claim-Version",operation.ResponseVersion));
                var request = new RunApiHttpRequest("POST", operation.EndpointPath, operation.BodyJson,headers);
                if (!Current(account, origin)) return null;
                var response = await transport.SendAsync(request, currentSession().AccessToken);
                if (!Current(account, origin)) return null;
                if (response == null || response.Outcome != GameApiAuthenticatedHttpOutcome.Succeeded
                    || response.Response == null || response.Response.StatusCode != 200)
                {
                    if((kind==PendingRunKind.CompleteOrdinaryNode||kind==PendingRunKind.TravelCommand||kind==PendingRunKind.MapArrival||kind==PendingRunKind.ConfirmDestination)&&response?.Outcome==GameApiAuthenticatedHttpOutcome.Succeeded&&response.Response!=null){
                        lastErrorCode=RunApiErrorJsonAdapter.OptionalString(response.Response.BodyJson,"errorCode");errorAccount=account;errorGeneration=origin;
                        if(kind==PendingRunKind.TravelCommand&&response.Response.StatusCode==409&&lastErrorCode=="combat_b_travel_revision_conflict"){
                            BTravelReceipt.ReadConflictCurrent(response.Response.BodyJson,BTravelCommand.Read(operation.BodyJson));
                            travelConflictJson=response.Response.BodyJson;
                            travelConflictKey=operation.IdempotencyKey;
                        }
                    }
                    if(kind==PendingRunKind.Finish && response?.Outcome==GameApiAuthenticatedHttpOutcome.Succeeded && response.Response!=null){
                        var error=GameApiJson.ReadObject(response.Response.BodyJson);
                        if(!GameApiJson.Boolean(error,"accepted")){
                            var code=GameApiJson.String(error,"errorCode",true);
                            if(response.Response.StatusCode==409&&code=="run_finish_request_conflict"||response.Response.StatusCode==503&&code=="run_finish_receipt_unavailable"){
                                lastErrorCode=code;errorAccount=account;errorGeneration=origin;
                            }
                        }
                    }
                    Notice = "Run delivery is unconfirmed. The original request is retained."; return null;
                }
                json = response.Response.BodyJson;
                validateResponse?.Invoke(json);
                awaitingStorage.Add(operation.Confirmed(json));
                }
                validateResponse?.Invoke(json);
                var durable = await journal.ConfirmAsync(operation, json);
                if (!Current(account, origin)) return null;
                if (!durable) { Notice = "Run response could not be saved. Recovery is still pending."; return null; }
                // Another client may have confirmed first; only the stored response is canonical.
                var canonicalRows = await ReadAsync(account);
                if (!Current(account, origin)) return null;
                var canonical = canonicalRows.FirstOrDefault(r => r.Kind == operation.Kind
                    && r.IdempotencyKey == operation.IdempotencyKey);
                if (canonical == null || !canonical.SameRequest(operation) || canonical.AcceptedResponseJson == null)
                { Notice = "The durable run response could not be recovered."; return null; }
                canonical.Confirmed(canonical.AcceptedResponseJson);
                validateResponse?.Invoke(canonical.AcceptedResponseJson);
                Publish(canonicalRows, account, origin);
                if(kind==PendingRunKind.InitializeOrdinary&&OrdinaryContinuationBlocked(canonicalRows,account,target))return null;
                Notice = "";
                return canonical;
            }
            catch
            {
                if (Current(account, origin)) Notice = "Run recovery could not complete. Saved operations are retained.";
                return null;
            }
            finally { End(); }
        }

        public async Task<PendingRunOperation> CommitFirstOrdinaryCheckpointAsync(LastRail.Battle.OrdinaryFiveStartInput original,string key,Func<PendingRunOperation,Task<long>> saveInitial)
        {
            var owner=currentSession();if(disposed||Busy||original==null||owner?.AccountId!=original.AccountId||saveInitial==null||journal is not IOrdinaryCheckpointJournal checkpoints)return null;
            var account=owner.AccountId;var stamp=generation();Begin();
            try{
                var rows=await ReadAsync(account);if(!Current(account,stamp))return null;
                Publish(rows,account,stamp);if(OrdinaryContinuationBlocked(rows,account,original.RunId))return null;
                var saved=rows.SingleOrDefault(r=>r.Kind==PendingRunKind.InitializeOrdinary&&r.TargetId==original.RunId&&r.IdempotencyKey==key);
                if(saved?.AcceptedResponseJson==null||!saved.OrdinaryCheckpointRevision.HasValue)return null;
                OrdinaryInitializationReceipt.Validate(saved.AcceptedResponseJson,original);
                if(saved.OrdinaryCheckpointRevision==0){
                    var revision=await saveInitial(saved);if(!Current(account,stamp)||revision<1)return null;
                    rows=await ReadAsync(account);if(!Current(account,stamp))return null;
                    Publish(rows,account,stamp);if(OrdinaryContinuationBlocked(rows,account,original.RunId))return null;
                    if(!await checkpoints.ConfirmFirstCheckpointAsync(saved,revision)||!Current(account,stamp))return null;
                    rows=await ReadAsync(account);if(!Current(account,stamp))return null;
                    var committed=rows.SingleOrDefault(r=>r.SameRequest(saved));
                    if(committed?.OrdinaryCheckpointRevision!=revision||committed.AcceptedResponseJson!=saved.AcceptedResponseJson)return null;
                    saved=committed;
                }
                Publish(rows,account,stamp);if(OrdinaryContinuationBlocked(rows,account,original.RunId))return null;return saved;
            }catch{if(Current(account,stamp))Notice="First combat checkpoint requires recovery. Combat has not been released.";return null;}
            finally{End();}
        }
        bool OrdinaryContinuationBlocked(IEnumerable<PendingRunOperation> rows,string account,string run)
        {
            if(!rows.Any(r=>r.AccountId==account&&r.TargetId==run&&(r.Kind==PendingRunKind.Finish||r.Kind==PendingRunKind.Claim)))return false;
            Notice="Run completion requires its existing result or original-request recovery. Combat cannot resume.";return true;
        }
        public async Task<bool> HandoffAsync(PendingRunOperation operation, Func<PendingRunOperation, Task<bool>> durableSink)
        {
            var owner = currentSession();
            if (disposed || Busy || owner == null || operation == null || operation.Kind==PendingRunKind.InitializeOrdinary || durableSink == null || owner.AccountId != operation.AccountId) return false;
            var account = owner.AccountId; var origin = generation();
            Begin();
            try
            {
                var rows = await ReadAsync(account);
                if (!Current(account, origin)) return false;
                var saved = rows.FirstOrDefault(r => r.Kind == operation.Kind && r.IdempotencyKey == operation.IdempotencyKey);
                if (saved == null || !saved.SameRequest(operation) || saved.AcceptedResponseJson == null) return false;
                saved.Confirmed(saved.AcceptedResponseJson);
                Publish(rows, account, origin);
                var handedOff = await durableSink(saved);
                if (!Current(account, origin)) return false;
                if (!handedOff) { Notice = "Run handoff is not durable. The confirmed response is retained."; return false; }
                var removed = await journal.RemoveConfirmedAsync(account, saved.Kind, saved.IdempotencyKey);
                if (!Current(account, origin)) return false;
                if (!removed) { Notice = "Run handoff cleanup failed. The confirmed response is retained."; return false; }
                rows.Remove(saved); Publish(rows, account, origin); Notice = "";
                return true;
            }
            catch
            {
                if (Current(account, origin)) Notice = "Run handoff failed. The confirmed response is retained.";
                return false;
            }
            finally { End(); }
        }

        private async Task<List<PendingRunOperation>> ReadAsync(string account)
        {
            var result = await journal.ReadAsync(account);
            if (result == null || result.Count > 32 || result.Any(r => r == null)) throw new InvalidOperationException("Invalid run journal.");
            var rows = result.Where(r => r.AccountId == account).ToList();
            if (rows.GroupBy(r => new { r.Kind, r.IdempotencyKey }).Any(g => g.Count() > 1)
                || rows.GroupBy(r => new { r.Kind, r.TargetId }).Any(g => g.Count() > 1)) throw new InvalidOperationException("Duplicate run operation.");
            return rows;
        }
        private void Publish(List<PendingRunOperation> rows, string account, long origin)
        {
            awaitingStorage.RemoveAll(buffered => !rows.Any(row => row.AcceptedResponseJson == null && row.SameRequest(buffered)));
            operations = Array.AsReadOnly(rows.ToArray()); publishedAccount = account; publishedGeneration = origin;
        }
        private bool Current(string account, long origin) => !disposed && account != null
            && generation() == origin && currentSession()?.AccountId == account;
        // Auth owner calls this on change, even when no next run command follows.
        // The original journal stays durable; an in-flight reply still uses its
        // captured account/generation and cannot publish into the next session.
        public void SynchronizeSession()
        {
            if (disposed) return;
            var account = currentSession()?.AccountId; var origin = generation();
            if (publishedAccount != account || publishedGeneration != origin)
            { operations = Array.Empty<PendingRunOperation>(); publishedAccount = null; publishedGeneration = 0; }
            if (storageAccount != account || storageGeneration != origin)
            { awaitingStorage.Clear(); storageAccount = account; storageGeneration = origin; Notice = ""; }
            if (errorAccount != account || errorGeneration != origin)
            { lastErrorCode = null; errorAccount = null; errorGeneration = 0; }
        }
        private void Begin()
        {
            SynchronizeSession();
            var account = currentSession()?.AccountId; var origin = generation();
            if (storageAccount != account || storageGeneration != origin) awaitingStorage.Clear();
            storageAccount = account; storageGeneration = origin;
            idle = new TaskCompletionSource<bool>(); Busy = true;
        }
        private void End()
        {
            var completed = idle;
            idle = null; Busy = false;
            completed.TrySetResult(true);
        }
        public void Dispose() { disposed = true; awaitingStorage.Clear(); operations = Array.Empty<PendingRunOperation>(); Notice = ""; }
    }
}
