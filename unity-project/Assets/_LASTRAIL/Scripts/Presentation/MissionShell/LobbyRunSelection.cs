using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public enum LobbyRunSelectionKind { None, AwaitingConfirmation, Recovery, Terminal, Conflict }
    // Display/recovery only: a recorded run does not authorize combat entry.
    public sealed class LobbyRunSelection
    {
        public LobbyRunSelectionKind Kind { get; }
        public string RunId { get; }
        public IReadOnlyList<string> KnownRuns { get; }
        public string Notice => Kind==LobbyRunSelectionKind.Conflict?"서로 다른 작전 기록이 있습니다. 기록을 확인해 주세요.":
            Kind==LobbyRunSelectionKind.AwaitingConfirmation?"출격 확인 중입니다. 원래 요청의 결과를 확인해 주세요.":
            Kind==LobbyRunSelectionKind.Terminal?"종료된 작전 기록이 있습니다. 전투·보상 기록을 확인해 주세요.":
            Kind==LobbyRunSelectionKind.Recovery?"보관된 작전 기록이 있습니다. 복구 상태를 확인해 주세요.":"";
        LobbyRunSelection(LobbyRunSelectionKind kind,string id,IEnumerable<string> runs)
        {Kind=kind;RunId=id;KnownRuns=Array.AsReadOnly(runs.OrderBy(x=>x,StringComparer.Ordinal).ToArray());}
        public static LobbyRunSelection Resolve(string account,IEnumerable<PendingRunOperation> operations,
            RunLifecycleSnapshot lifecycle,DepartureStartResult lookup,string startedRun,PendingAccountAction pending,string explicitRun)
        {
            var runs=new HashSet<string>(StringComparer.Ordinal);var terminal=new HashSet<string>(StringComparer.Ordinal);
            var confirmedPreviews=new HashSet<string>(StringComparer.Ordinal);var uncertain=new HashSet<string>(StringComparer.Ordinal);
            if(string.IsNullOrEmpty(account))return new LobbyRunSelection(LobbyRunSelectionKind.None,null,runs);
            void Add(string id){if(!string.IsNullOrEmpty(id))runs.Add(id);}
            string Request(string preview,string key)=>preview+"\0"+key;
            foreach(var operation in operations??Array.Empty<PendingRunOperation>()){
                if(operation.AccountId!=account)continue;
                if(operation.Kind==PendingRunKind.Start){
                    if(operation.AcceptedResponseJson==null){uncertain.Add(Request(operation.TargetId,operation.IdempotencyKey));continue;}
                    Add(GameApiJson.String(GameApiJson.ReadObject(operation.AcceptedResponseJson),"runId"));confirmedPreviews.Add(Request(operation.TargetId,operation.IdempotencyKey));
                }else{Add(operation.TargetId);if((operation.Kind==PendingRunKind.Finish||operation.Kind==PendingRunKind.Claim)&&operation.AcceptedResponseJson!=null)terminal.Add(operation.TargetId);}
            }
            if(pending?.Account==account&&pending.Kind=="departure-start"){
                if(pending.StartResultJson==null)uncertain.Add(Request(pending.Target,pending.Key));
                else{var receipt=new DepartureStartResult(200,pending.StartResultJson,pending.Target);Add(receipt.RunId);confirmedPreviews.Add(Request(pending.Target,pending.Key));}
            }
            if(lookup?.Status=="COMMITTED"){
                Add(lookup.RunId);
                if(pending?.Account==account&&pending.Kind=="departure-start"&&lookup.OperationPreviewId==pending.Target)confirmedPreviews.Add(Request(pending.Target,pending.Key));
                if(IsTerminal(lookup.RunStatus))terminal.Add(lookup.RunId);
            }
            Add(startedRun);Add(explicitRun);
            if(lifecycle!=null){Add(lifecycle.RunId);if(IsTerminal(lifecycle.RunStatus))terminal.Add(lifecycle.RunId);}
            if(runs.Count>1)return new LobbyRunSelection(LobbyRunSelectionKind.Conflict,null,runs);
            if(uncertain.Except(confirmedPreviews).Any())return new LobbyRunSelection(LobbyRunSelectionKind.AwaitingConfirmation,null,runs);
            var run=runs.SingleOrDefault();
            return new LobbyRunSelection(run==null?LobbyRunSelectionKind.None:terminal.Contains(run)?LobbyRunSelectionKind.Terminal:LobbyRunSelectionKind.Recovery,run,runs);
        }
        static bool IsTerminal(string state)=>state=="COMPLETED"||state=="FAILED"||state=="ABANDONED";
    }
    public sealed partial class AccountScreenRuntime
    {
        LobbyRunSelection runSelection;
        object selectionOperations,selectionLifecycle,selectionLookup,selectionPending,selectionStarted;
        string selectionAccount,selectionExplicit;
        public LobbyRunSelection RunSelection
        {
            get{
                var account=owner?.AccountId;var rows=runRecovery?.Operations;var explicitRun=OriginalRunId?.Invoke();
                if(runSelection!=null&&selectionAccount==account&&ReferenceEquals(selectionOperations,rows)&&ReferenceEquals(selectionLifecycle,Lifecycle)&&ReferenceEquals(selectionLookup,DepartureStartResult)&&ReferenceEquals(selectionPending,Pending)&&ReferenceEquals(selectionStarted,departureStarted)&&selectionExplicit==explicitRun)return runSelection;
                selectionAccount=account;selectionOperations=rows;selectionLifecycle=Lifecycle;selectionLookup=DepartureStartResult;selectionPending=Pending;selectionStarted=departureStarted;selectionExplicit=explicitRun;
                return runSelection=LobbyRunSelection.Resolve(account,rows,Lifecycle,DepartureStartResult,(string)departureStarted?["runId"],Pending,explicitRun);
            }
        }
        void ResetRunSelection(){runSelection=null;selectionOperations=selectionLifecycle=selectionLookup=selectionPending=selectionStarted=null;selectionAccount=selectionExplicit=null;}
    }
}
