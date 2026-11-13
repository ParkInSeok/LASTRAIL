using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AugmentSelectionControllerTests
    {
        // Serialized storage fixture persists across controller instances; not a disk/keystore claim.
        sealed class Journal:IAugmentSelectionJournal
        {
            public readonly Dictionary<string,string> Data=new Dictionary<string,string>();
            public bool FailSave,FailComplete;
            public string Load(string key)=>Data.TryGetValue(key,out var value)?value:null;
            public void SaveNew(string key,string value){if(FailSave||Data.ContainsKey(key))throw new InvalidOperationException("CAS");Data.Add(key,value);}
            public void Complete(string key,string expected){if(FailComplete||Load(key)!=expected)throw new InvalidOperationException("CAS");Data.Remove(key);}
        }
        sealed class Port:IAugmentSelectionPort
        {
            public int ReadCount;
            public string Current=AugmentSelectionContractTests.Initial;
            public readonly List<AugmentSelectionCommand> Commands=new List<AugmentSelectionCommand>();
            public Func<AugmentSelectionCommand,Task<string>> Execute;
            public Func<Task<string>> Read;
            public Task<string> ReadAsync(AugmentSelectionScope scope,CancellationToken token){ReadCount++;return Read==null?Task.FromResult(Current):Read();}
            public Task<string> ExecuteAsync(AugmentSelectionScope scope,AugmentSelectionCommand command,CancellationToken token)
            {Commands.Add(command);return Execute(command);}
        }
        static string Result(AugmentSelectionCommand command,string outcome="REROLLED",string cause="PURCHASE_ENTITLEMENT")
        {
            var next=JObject.Parse(command.Before.Json);var slot=(JObject)next["slots"][command.SlotIndex];
            if(outcome!="NO_CANDIDATE")next["grantRevision"]=command.Before.GrantRevision+1;
            if(outcome=="SELECTED"){next["state"]="CONFIRMED";next["selectedCandidateId"]=slot["candidateId"];}
            if(outcome=="REROLLED"){slot["candidateId"]="replacement-"+(command.Before.GrantRevision+1);string key=command.Mode=="FREE"?"freeRemaining":"extraRemaining";slot[key]=slot[key].Value<int>()-1;}
            JObject approval=null;
            if(outcome=="REROLLED"&&command.Mode=="EXTRA")approval=new JObject{{"approvalId","TEST_ONLY"},{"cause",cause},{"target",new JObject{{"accountId","account"},{"environment","TEST"},{"runId","run"},{"eventId","event"},{"stageId","stage"},{"catalogVersion","TEST_ONLY"},{"slotIndex",command.SlotIndex},{"candidateId",command.Before.Slots[command.SlotIndex].CandidateId},{"grantRevision",command.Before.GrantRevision},{"extraOrdinal",1}}}};
            return new JObject{{"outcome",outcome},{"snapshot",next},{"approval",approval}}.ToString(Formatting.None);
        }
        static string SnapshotOf(string result)=>JObject.Parse(result)["snapshot"].ToString(Formatting.None);
        static AugmentSelectionOwner Owner()=>new AugmentSelectionOwner("account","TEST");
        static Port AutoPort()
        {
            var port=new Port();port.Execute=c=>{string result=Result(c,c.Operation=="SELECT"?"SELECTED":"REROLLED");port.Current=SnapshotOf(result);return Task.FromResult(result);};return port;
        }
        static Task OpenSupplied(AugmentSelectionController controller,AugmentSelectionScope scope,AugmentSelectionSnapshot snapshot)
        {
            var method=typeof(AugmentSelectionController).GetMethod("OpenFromSnapshotAsync");
            Assert.That(method,Is.Not.Null,"Missing existing-controller intake for supplied node event snapshot");
            return (Task)method.Invoke(controller,new object[]{scope,snapshot});
        }
        [Test] public async Task O22OriginalIsCommittedBeforeRequestCompletionWithoutFollowingGet()
        {
            var packet=global::CombatO22OriginalTests.Packet();var confirmed=(JObject)packet["definition"]["selectionSource"]["selection"];
            var initial=(JObject)confirmed.DeepClone();initial["state"]="SELECTING";initial["grantRevision"]=0;initial["selectedCandidateId"]=null;
            var owner=Owner();var scope=new AugmentSelectionScope(owner,"run","grant");var journal=new Journal();var port=new Port{Current=initial.ToString()};
            port.Execute=c=>Task.FromResult(new JObject{["outcome"]="SELECTED",["snapshot"]=confirmed,["approval"]=null,["applicationOriginal"]=packet}.ToString());
            int commits=0;bool fail=true;
            string saved=null;
            var originalStore=new LastRail.Run.CombatO22ApplicationStore(Path.GetFullPath("../LocalTemp/o22-receiver-"+Guid.NewGuid().ToString("N")),()=>saved,s=>{if(fail)throw new IOException("save failed");saved=s;});
            var receiver=new O22SelectionOriginalReceiver(originalStore,scope,new string('a',64),()=>true);
            Action<AugmentSelectionScope,AugmentSelectionSnapshot,string> commit=(s,selection,json)=>{
                Assert.IsNotNull(journal.Load(s.StorageKey));Assert.IsTrue(selection.Confirmed);
                receiver.Commit(s,selection,json);commits++;
            };
            var ctor=typeof(AugmentSelectionController).GetConstructors().SingleOrDefault(c=>c.GetParameters().Length==4);Assert.IsNotNull(ctor);
            using var controller=(AugmentSelectionController)ctor.Invoke(new object[]{port,journal,new Func<AugmentSelectionOwner>(()=>owner),commit});
            await controller.OpenFromSnapshotAsync(scope,AugmentSelectionSnapshot.Parse(port.Current));await controller.SelectAsync(0);
            Assert.IsTrue(controller.HasPending);Assert.AreEqual(0,commits);Assert.IsFalse(controller.Snapshot.Confirmed);
            Assert.AreEqual("선택한 효과를 복구하고 있습니다.",controller.Notice);
            string originalKey=port.Commands[0].Key;fail=false;await controller.RetryAsync();
            Assert.AreEqual(1,commits);Assert.IsFalse(controller.HasPending);Assert.IsTrue(controller.Snapshot.Confirmed);Assert.AreEqual(0,port.ReadCount);Assert.AreEqual(originalKey,port.Commands[1].Key);
        }
        [Test] public async Task SuppliedNodeEventOpensAndCommandsWithoutInitialRead()
        {
            var owner=Owner();var port=AutoPort();port.Read=()=>throw new IOException("No read needed");
            using var controller=new AugmentSelectionController(port,new Journal(),()=>owner);
            await OpenSupplied(controller,new AugmentSelectionScope(owner,"run","event"),AugmentSelectionSnapshot.Parse(port.Current));
            await controller.RerollAsync(0,"FREE");await controller.SelectAsync(1);
            Assert.That(controller.Snapshot.Confirmed,Is.True);Assert.That(controller.HasPending,Is.False);
            Assert.That(port.ReadCount,Is.Zero);Assert.That(port.Commands.Count,Is.EqualTo(2));
        }
        [Test] public async Task SuppliedNodeEventRejectsWrongOwnerScopeAndOlderReceiptWithoutReset()
        {
            var owner=Owner();var port=AutoPort();using var controller=new AugmentSelectionController(port,new Journal(),()=>owner);
            var initial=AugmentSelectionSnapshot.Parse(port.Current);var scope=new AugmentSelectionScope(owner,"run","event");
            await OpenSupplied(controller,scope,initial);await controller.RerollAsync(0,"FREE");var current=controller.Snapshot;
            Assert.ThrowsAsync<FormatException>(async()=>await OpenSupplied(controller,scope,initial));
            Assert.ThrowsAsync<ArgumentException>(async()=>await OpenSupplied(controller,new AugmentSelectionScope(Owner(),"run","event"),current));
            Assert.ThrowsAsync<ArgumentException>(async()=>await OpenSupplied(controller,new AugmentSelectionScope(owner,"another","event"),current));
            Assert.That(controller.Snapshot,Is.SameAs(current));Assert.That(port.ReadCount,Is.Zero);
        }
        [Test] public async Task SuppliedNodeEventPreservesPendingRecoveryReadAndRequestIdentity()
        {
            var owner=Owner();var port=AutoPort();var journal=new Journal();
            using(var first=new AugmentSelectionController(port,journal,()=>owner)){await first.OpenAsync("run","event");port.Execute=c=>throw new IOException("lost");await first.SelectAsync(0);}
            string original=port.Commands[0].Serialized;var beforeReads=port.ReadCount;
            using var next=new AugmentSelectionController(port,journal,()=>owner);
            await OpenSupplied(next,new AugmentSelectionScope(owner,"run","event"),AugmentSelectionSnapshot.Parse(port.Current));
            Assert.That(next.HasPending,Is.True);Assert.That(port.ReadCount,Is.EqualTo(beforeReads+1));
            port.Execute=c=>Task.FromResult(Result(c,"SELECTED"));await next.RetryAsync();
            Assert.That(port.Commands[1].Serialized,Is.EqualTo(original));Assert.That(journal.Data,Is.Empty);
        }
        [Test] public async Task SuppliedNodeEventKeepsNewerConfirmedFloorDuringRegressedRecoveryRead()
        {
            var owner=Owner();var port=AutoPort();var journal=new Journal{FailComplete=true};
            using(var first=new AugmentSelectionController(port,journal,()=>owner)){await first.OpenAsync("run","event");await first.SelectAsync(0);Assert.That(first.HasPending,Is.True);}
            var confirmed=AugmentSelectionSnapshot.Parse(port.Current);
            port.Current=AugmentSelectionContractTests.Initial;
            using var next=new AugmentSelectionController(port,journal,()=>owner);
            await OpenSupplied(next,new AugmentSelectionScope(owner,"run","event"),confirmed);
            Assert.That(next.Snapshot.Confirmed,Is.True);Assert.That(next.Snapshot.GrantRevision,Is.EqualTo(confirmed.GrantRevision));
            Assert.That(next.HasPending,Is.True);Assert.That(journal.Data.Count,Is.EqualTo(1));
        }
        [Test] public async Task CompleteCommandSnapshotNeedsNoFollowupReadForRerollOrSelection()
        {
            var port=AutoPort();var owner=Owner();using var controller=new AugmentSelectionController(port,new Journal(),()=>owner);
            await controller.OpenAsync("run","event");port.Read=()=>throw new IOException("unnecessary followup read");
            await controller.RerollAsync(0,"FREE");Assert.That(controller.HasPending,Is.False);Assert.That(controller.Snapshot.Slots[0].FreeRemaining,Is.EqualTo(2));
            await controller.SelectAsync(1);Assert.That(controller.HasPending,Is.False);Assert.That(controller.Snapshot.Confirmed,Is.True);
            Assert.That(port.ReadCount,Is.EqualTo(1));Assert.That(port.Commands.Count,Is.EqualTo(2));
            // This is an internal port count, not HTTP; CONFIRMED remains a selection decision only.
        }
        [Test] public async Task ActualInternalServerFixturesRoundTripThroughOneController()
        {
            var samples=JObject.Parse(File.ReadAllText(Path.GetFullPath("../QA/AdSkipUnityPhase2-20260917/ServerFixtures/internal-domain-fixtures.json")))["samples"];
            var owner=new AugmentSelectionOwner("TEST_ACCOUNT","TEST_ONLY");var port=new Port{Current=samples["initial"].ToString()};var journal=new Journal();
            var names=new Queue<string>(new[]{"free1","free2","free3","extra","selected"});
            port.Execute=command=>
            {
                var sample=samples[names.Dequeue()];Assert.That(JToken.DeepEquals(JObject.Parse(command.BodyJson)["command"],sample["input"]),Is.True);
                string result=sample["result"].ToString();port.Current=SnapshotOf(result);return Task.FromResult(result);
            };
            using var controller=new AugmentSelectionController(port,journal,()=>owner);await controller.OpenAsync("TEST_RUN","TEST_EVENT");
            for(int i=0;i<3;i++)await controller.RerollAsync(0,"FREE");await controller.RerollAsync(0,"EXTRA");await controller.SelectAsync(1);
            Assert.That(names,Is.Empty);Assert.That(controller.Snapshot.Confirmed,Is.True);Assert.That(controller.Snapshot.GrantRevision,Is.EqualTo(5));Assert.That(controller.HasPending,Is.False);
        }
        [Test] public async Task ChangedListenerResetMustCancelBeforeSendingACommand()
        {
            var port=AutoPort();var owner=Owner();var journal=new Journal();using var controller=new AugmentSelectionController(port,journal,()=>owner);
            await controller.OpenAsync("run","event");controller.Changed+=()=>{if(controller.Busy&&controller.HasPending)controller.Reset();};await controller.RerollAsync(0,"FREE");
            Assert.That(port.Commands,Is.Empty,"Unbinding synchronously during notification must stop the send");Assert.That(journal.Data.Count,Is.EqualTo(1));
        }
        [Test] public async Task FreeThreeAndSharedExtraOneUseTheSamePipeline()
        {
            var port=AutoPort();var journal=new Journal();var owner=Owner();using var controller=new AugmentSelectionController(port,journal,()=>owner);
            await controller.OpenAsync("run","event");await controller.RerollAsync(0,"EXTRA");Assert.That(port.Commands.Count,Is.Zero);
            for(int i=0;i<3;i++)await controller.RerollAsync(0,"FREE");
            await controller.RerollAsync(0,"FREE");Assert.That(port.Commands.Count,Is.EqualTo(3));
            await controller.RerollAsync(0,"EXTRA");await controller.RerollAsync(0,"EXTRA");
            Assert.That(port.Commands.Count,Is.EqualTo(4));Assert.That(controller.Snapshot.Slots[0].ExtraRemaining,Is.Zero);
            Assert.That(controller.Snapshot.Slots[1].FreeRemaining,Is.EqualTo(3));Assert.That(controller.Snapshot.Slots[2].CandidateId,Is.EqualTo("c"));
            await controller.OpenAsync("run","event");Assert.That(controller.Snapshot.Slots[0].FreeRemaining,Is.Zero,"Reopening must not reset counts");
            await controller.SelectAsync(1);Assert.That(controller.Snapshot.SelectedCandidateId,Is.EqualTo("b"));await controller.RerollAsync(1,"FREE");Assert.That(port.Commands.Count,Is.EqualTo(5));
        }
        [Test] public async Task LostResponseSurvivesRecreationWithIdenticalKeyAndBody()
        {
            var port=AutoPort();var journal=new Journal();var owner=Owner();string receipt=null;
            port.Execute=c=>{receipt=Result(c);port.Current=SnapshotOf(receipt);throw new TimeoutException();};
            using(var first=new AugmentSelectionController(port,journal,()=>owner)){await first.OpenAsync("run","event");await first.RerollAsync(0,"FREE");Assert.That(first.HasPending,Is.True);}
            var original=port.Commands[0];port.Execute=c=>Task.FromResult(receipt);
            using var restored=new AugmentSelectionController(port,journal,()=>owner);await restored.OpenAsync("run","event");await restored.RerollAsync(1,"FREE");Assert.That(port.Commands.Count,Is.EqualTo(1));await restored.RetryAsync();
            Assert.That(port.Commands[1].Key,Is.EqualTo(original.Key));Assert.That(port.Commands[1].BodyJson,Is.EqualTo(original.BodyJson));
            Assert.That(restored.Snapshot.Slots[0].FreeRemaining,Is.EqualTo(2));Assert.That(journal.Data,Is.Empty);
        }
        [Test] public async Task OwnerChangeAndDisposeDiscardLateResultsButPreserveRequest()
        {
            var port=AutoPort();var journal=new Journal();var owner=Owner();var wait=new TaskCompletionSource<string>();port.Execute=c=>wait.Task;
            using var controller=new AugmentSelectionController(port,journal,()=>owner);await controller.OpenAsync("run","event");var task=controller.RerollAsync(0,"FREE");await controller.RerollAsync(1,"FREE");Assert.That(port.Commands.Count,Is.EqualTo(1));
            owner=new AugmentSelectionOwner("other","TEST");Assert.That(controller.Snapshot,Is.Null);controller.Dispose();wait.SetResult(Result(port.Commands[0]));await task;
            Assert.That(controller.Snapshot,Is.Null);Assert.That(journal.Data.Count,Is.EqualTo(1));
        }
        [Test] public async Task SelectingShowsApplyingAndBlocksOtherCardsAndRerolls(){
            var port=AutoPort();var journal=new Journal();var owner=Owner();var wait=new TaskCompletionSource<string>();port.Execute=c=>wait.Task;
            using var controller=new AugmentSelectionController(port,journal,()=>owner);await controller.OpenAsync("run","event");
            var task=controller.SelectAsync(0);Assert.AreEqual("선택을 반영하고 있습니다.",controller.Notice);
            await controller.SelectAsync(1);await controller.RerollAsync(0,"FREE");Assert.AreEqual(1,port.Commands.Count);
            wait.SetResult(Result(port.Commands[0],"SELECTED"));await task;Assert.IsFalse(controller.HasPending);Assert.AreEqual("",controller.Notice);
        }
        [Test] public async Task RestoredUnsupportedSelectionCannotSilentlyCompleteWithoutAnEffect(){
            var port=AutoPort();var journal=new Journal();var owner=Owner();var scope=new AugmentSelectionScope(owner,"run","event");
            var initial=AugmentSelectionSnapshot.Parse(port.Current);var pending=AugmentSelectionCommand.Create(scope,"SELECT",0,null,initial);
            journal.SaveNew(scope.StorageKey,pending.Serialized);
            using var controller=new AugmentSelectionController(port,journal,()=>owner,null,id=>id=="O22");
            await controller.OpenFromSnapshotAsync(scope,initial);await controller.RetryAsync();
            Assert.AreEqual(0,port.Commands.Count);Assert.IsTrue(controller.HasPending);Assert.AreEqual(pending.Serialized,journal.Load(scope.StorageKey));
            Assert.AreEqual("현재 전투에서 지원하지 않는 선택입니다.",controller.Notice);
        }
        [Test] public async Task ReopenSuppressesAnOlderRead()
        {
            var port=AutoPort();var owner=Owner();var wait=new TaskCompletionSource<string>();port.Read=()=>wait.Task;
            using var controller=new AugmentSelectionController(port,new Journal(),()=>owner);var old=controller.OpenAsync("run","event");port.Read=null;
            var second=JObject.Parse(port.Current);second["eventId"]="second";port.Current=second.ToString();await controller.OpenAsync("run","second");wait.SetResult(AugmentSelectionContractTests.Initial);await old;
            Assert.That(controller.Snapshot.EventId,Is.EqualTo("second"));Assert.That(controller.Busy,Is.False);
        }
        [Test] public async Task NoCandidateDoesNotConsumeAndSaveFailureDoesNotSend()
        {
            var port=AutoPort();var owner=Owner();var journal=new Journal{FailSave=true};port.Execute=c=>Task.FromResult(Result(c,"NO_CANDIDATE"));
            using var controller=new AugmentSelectionController(port,journal,()=>owner);await controller.OpenAsync("run","event");await controller.RerollAsync(0,"FREE");Assert.That(port.Commands,Is.Empty);
            journal.FailSave=false;await controller.RerollAsync(0,"FREE");Assert.That(controller.Snapshot.GrantRevision,Is.Zero);Assert.That(controller.Snapshot.Slots[0].FreeRemaining,Is.EqualTo(3));Assert.That(journal.Data,Is.Empty);
        }
        [Test] public async Task MalformedReceiptAndJournalCasFailureRemainRetryable()
        {
            var port=AutoPort();var owner=Owner();var journal=new Journal();port.Execute=c=>Task.FromResult("{}");
            using var controller=new AugmentSelectionController(port,journal,()=>owner);await controller.OpenAsync("run","event");await controller.RerollAsync(0,"FREE");Assert.That(controller.HasPending,Is.True);Assert.That(controller.Snapshot.GrantRevision,Is.Zero);
            port.Execute=c=>{string result=Result(c);port.Current=SnapshotOf(result);return Task.FromResult(result);};journal.FailComplete=true;await controller.RetryAsync();Assert.That(controller.HasPending,Is.True);
            journal.FailComplete=false;await controller.RetryAsync();Assert.That(controller.HasPending,Is.False);Assert.That(port.Commands[2].Key,Is.EqualTo(port.Commands[0].Key));
        }
        [Test] public async Task HistoricalReplayCannotRegressCurrentConfirmedSnapshot()
        {
            var port=AutoPort();var owner=Owner();var journal=new Journal();string result=null;
            port.Execute=c=>{result=Result(c);throw new TimeoutException();};
            using var controller=new AugmentSelectionController(port,journal,()=>owner);await controller.OpenAsync("run","event");await controller.RerollAsync(0,"FREE");
            var newer=JObject.Parse(SnapshotOf(result));newer["grantRevision"]=2;newer["state"]="CONFIRMED";newer["selectedCandidateId"]="b";port.Current=newer.ToString();await controller.RefreshAsync();port.Execute=c=>Task.FromResult(result);await controller.RetryAsync();
            Assert.That(controller.Snapshot.GrantRevision,Is.EqualTo(2));Assert.That(controller.Snapshot.Confirmed,Is.True);Assert.That(controller.HasPending,Is.False);
        }
        [Test] public async Task DefinitiveConflictReadsCurrentBeforeClearingOriginalRequest()
        {
            var port=AutoPort();var owner=Owner();var journal=new Journal();port.Execute=c=>throw new AugmentSelectionRejectedException("revision_conflict");
            using var controller=new AugmentSelectionController(port,journal,()=>owner);await controller.OpenAsync("run","event");await controller.SelectAsync(0);Assert.That(controller.HasPending,Is.False);Assert.That(controller.Notice,Does.Contain("거절"));
        }
        [Test] public async Task InvalidReadAndWrongOwnerJournalAreNotAccepted()
        {
            var port=AutoPort();var owner=Owner();var journal=new Journal();using var controller=new AugmentSelectionController(port,journal,()=>owner);
            await controller.OpenAsync("wrong-run","event");Assert.That(controller.Snapshot,Is.Null);await controller.SelectAsync(0);Assert.That(port.Commands,Is.Empty);
            var scope=new AugmentSelectionScope(owner,"run","event");var cmd=AugmentSelectionCommand.Create(scope,"SELECT",0,null,AugmentSelectionSnapshot.Parse(AugmentSelectionContractTests.Initial));
            var other=new AugmentSelectionScope(new AugmentSelectionOwner("other","TEST"),"run","event");Assert.Throws<FormatException>(()=>AugmentSelectionCommand.Restore(other,cmd.Serialized));
        }
        [Test] public async Task SameRevisionCannotReplaceCandidateFacts()
        {
            var port=AutoPort();var owner=Owner();using var controller=new AugmentSelectionController(port,new Journal(),()=>owner);
            await controller.OpenAsync("run","event");var changed=JObject.Parse(port.Current);changed["slots"][0]["candidateId"]="changed-without-revision";port.Current=changed.ToString();await controller.RefreshAsync();
            Assert.That(controller.Snapshot.Slots[0].CandidateId,Is.EqualTo("a"));Assert.That(controller.Notice,Does.Contain("확인하지 못"));
        }
        [Test] public void ResetAfterDisposeIsSafeAndCannotReconnect()
        {
            var owner=Owner();var controller=new AugmentSelectionController(AutoPort(),new Journal(),()=>owner);controller.Dispose();Assert.DoesNotThrow(controller.Reset);Assert.That(controller.Snapshot,Is.Null);
        }
    }
}
