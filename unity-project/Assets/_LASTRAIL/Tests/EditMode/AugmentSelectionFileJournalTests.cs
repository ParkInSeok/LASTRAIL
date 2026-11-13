using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class AugmentSelectionFileJournalTests
{
    sealed class UncertainPort:IAugmentSelectionPort
    {
        public string Sent;public int Calls;
        public Task<string> ReadAsync(AugmentSelectionScope scope,CancellationToken token)=>throw new NotSupportedException();
        public Task<string> ExecuteAsync(AugmentSelectionScope scope,AugmentSelectionCommand command,CancellationToken token){Sent=command.Serialized;Calls++;throw new IOException("network lost");}
    }
    [Test]public async Task ControllerRecoversSavedIntentAfterLostSaveAcknowledgementWithoutReopening()
    {
        var scope=Scope();string path=Path.GetFullPath("../LocalTemp/selection-controller-"+Guid.NewGuid().ToString("N")),saved=null;
        var journal=Make(scope,()=>saved,s=>{saved=s;throw new IOException("save ack lost");},path);var port=new UncertainPort();
        using(var controller=new AugmentSelectionController(port,journal,()=>scope.Owner)){
            var before=AugmentSelectionCommand.Restore(scope,Command(scope)).Before;
            await controller.OpenFromSnapshotAsync(scope,before);await controller.SelectAsync(0);
            Assert.IsTrue(controller.HasPending);Assert.AreEqual(0,port.Calls);
            string original=journal.Load(scope.StorageKey);await controller.RetryAsync();
            Assert.AreEqual(1,port.Calls);Assert.AreEqual(original,port.Sent);Assert.IsTrue(controller.HasPending);
        }
    }
    static AugmentSelectionScope Scope()=>new AugmentSelectionScope(new AugmentSelectionOwner("account","TEST"),"run","grant");
    static string Command(AugmentSelectionScope scope)
    {
        var p=(JObject)CombatO22OriginalTests.Packet()["definition"]["selectionSource"]["selection"];
        p["state"]="SELECTING";p["grantRevision"]=0;p["selectedCandidateId"]=null;
        return AugmentSelectionCommand.Create(scope,"SELECT",0,null,AugmentSelectionSnapshot.Parse(p.ToString())).Serialized;
    }
    static IAugmentSelectionJournal Make(AugmentSelectionScope scope,Func<string> read,Action<string> write,string path)
    {
        var type=typeof(AugmentSelectionController).Assembly.GetType("LastRail.Presentation.AugmentSelection.AugmentSelectionFileJournal");Assert.IsNotNull(type);
        return (IAugmentSelectionJournal)Activator.CreateInstance(type,new object[]{scope,path,read,write});
    }
    [Test]public void DurableOriginalKeySurvivesReloadAndCasCannotDeleteNewRequest()
    {
        var scope=Scope();string path=Path.GetFullPath("../LocalTemp/selection-journal-"+Guid.NewGuid().ToString("N")),saved=null;
        var a=Make(scope,()=>saved,s=>saved=s,path);string first=Command(scope);a.SaveNew(scope.StorageKey,first);
        var b=Make(scope,()=>saved,s=>saved=s,path);Assert.AreEqual(first,b.Load(scope.StorageKey));
        Assert.Throws<InvalidOperationException>(()=>b.SaveNew(scope.StorageKey,Command(scope)));
        b.Complete(scope.StorageKey,first);Assert.IsNull(a.Load(scope.StorageKey));
        string second=Command(scope);a.SaveNew(scope.StorageKey,second);Assert.Throws<InvalidOperationException>(()=>b.Complete(scope.StorageKey,first));Assert.AreEqual(second,a.Load(scope.StorageKey));
        Assert.Throws<ArgumentException>(()=>a.Load("other"));
    }
    [Test]public void LostSaveAcknowledgementRecoversSameOriginalWithoutReplacingIt()
    {
        var scope=Scope();string path=Path.GetFullPath("../LocalTemp/selection-journal-"+Guid.NewGuid().ToString("N")),saved=null;bool lose=true;int writes=0;
        var journal=Make(scope,()=>saved,s=>{saved=s;writes++;if(lose)throw new IOException("lost ack");},path);string command=Command(scope);
        Assert.Throws<IOException>(()=>journal.SaveNew(scope.StorageKey,command));lose=false;
        journal.SaveNew(scope.StorageKey,command);Assert.AreEqual(1,writes);Assert.AreEqual(command,journal.Load(scope.StorageKey));
    }
}
