using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class AugmentSelectHttpTests
{
    sealed class Journal:IAugmentSelectionJournal
    {
        public string Saved;
        public string Load(string key)=>Saved;
        public void SaveNew(string key,string value){Assert.IsNull(Saved);Saved=value;}
        public void Complete(string key,string expected){Assert.AreEqual(expected,Saved);Saved=null;}
    }
    [TestCase("augment_selection_revision_conflict",false)]
    [TestCase("augment_selection_request_conflict",true)]
    public async Task DefinitiveRejectionAllowsSuppliedSnapshotButAmbiguousConflictRetainsOriginal(string code,bool pending)
    {
        var owner=new AugmentSelectionOwner("account","TEST");var scope=new AugmentSelectionScope(owner,"run","grant");var journal=new Journal();
        var wire=new Wire{Reply=()=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(409,new JObject{["accepted"]=false,["errorCode"]=code}.ToString())))};
        using(var controller=new AugmentSelectionController(Port(wire,owner,()=>true),journal,()=>owner))
        {
            await controller.OpenFromSnapshotAsync(scope,Before());await controller.SelectAsync(1);
            Assert.AreEqual(pending,controller.HasPending);Assert.AreEqual(pending,journal.Saved!=null);Assert.AreEqual(1,wire.Calls);
            if(pending)return;
            Assert.IsNull(controller.Snapshot);
            var next=JObject.Parse(Before().Json);next["grantRevision"]=1;
            await controller.OpenFromSnapshotAsync(scope,AugmentSelectionSnapshot.Parse(next.ToString()));
            Assert.AreEqual(1,controller.Snapshot.GrantRevision);Assert.AreEqual(1,wire.Calls);
        }
    }
    sealed class Wire:IGameApiAuthenticatedHttpTransport
    {
        public RunApiHttpRequest Request;public int Calls;public Func<Task<GameApiAuthenticatedHttpResult>> Reply;
        public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){Request=request;Calls++;return Reply();}
    }
    static GameApiAuthSessionSnapshot Session()=>new GameApiAuthSessionSnapshot("account","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
    static AugmentSelectionSnapshot Before(int count=3){var p=(JObject)CombatO22OriginalTests.Packet()["definition"]["selectionSource"]["selection"];p["state"]="SELECTING";p["grantRevision"]=0;p["selectedCandidateId"]=null;while(((JArray)p["slots"]).Count>count)((JArray)p["slots"]).RemoveAt(count);return AugmentSelectionSnapshot.Parse(p.ToString());}
    static IAugmentSelectionPort Port(Wire wire,AugmentSelectionOwner owner,Func<bool> current)
    {
        var t=typeof(AugmentSelectionController).Assembly.GetType("LastRail.Presentation.AugmentSelection.AugmentSelectHttpPort");Assert.IsNotNull(t);
        var captured=Session();return (IAugmentSelectionPort)Activator.CreateInstance(t,wire,owner,new Func<GameApiAuthSessionSnapshot>(()=>captured),current);
    }
    [Test]public async Task SendsOnlySelectIntentAndReturnsCommittedInnerResultWithoutGet()
    {
        var owner=new AugmentSelectionOwner("account","TEST");var scope=new AugmentSelectionScope(owner,"run","grant");var command=AugmentSelectionCommand.Create(scope,"SELECT",0,null,Before());
        var inner=new JObject{["outcome"]="SELECTED",["snapshot"]=JObject.Parse(command.Before.Json),["approval"]=null};
        var wire=new Wire{Reply=()=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,new JObject{["version"]="combat.augment-select.v1.20261019",["committed"]=true,["result"]=inner}.ToString())))};
        var port=Port(wire,owner,()=>true);Assert.IsTrue(JToken.DeepEquals(inner,JObject.Parse(await port.ExecuteAsync(scope,command,CancellationToken.None))));
        Assert.AreEqual("POST",wire.Request.Method);Assert.AreEqual("/v1/runs/run/augment-grants/grant/select",wire.Request.EndpointPath);Assert.AreEqual(command.Key,wire.Request.Headers.Single(h=>h.Name=="Idempotency-Key").Value);
        var body=JObject.Parse(wire.Request.BodyJson);CollectionAssert.AreEquivalent(new[]{"version","command"},body.Properties().Select(p=>p.Name));Assert.AreEqual(1,wire.Calls);
        Assert.ThrowsAsync<NotSupportedException>(async()=>await port.ReadAsync(scope,CancellationToken.None));Assert.AreEqual(1,wire.Calls);
    }
    [Test]public async Task OwnerChangeWhileAwaitingResponseCannotPublishOldResponse()
    {
        bool current=true;var owner=new AugmentSelectionOwner("account","TEST");var scope=new AugmentSelectionScope(owner,"run","grant");var completion=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();var wire=new Wire{Reply=()=>completion.Task};var port=Port(wire,owner,()=>current);
        var task=port.ExecuteAsync(scope,AugmentSelectionCommand.Create(scope,"SELECT",0,null,Before()),CancellationToken.None);current=false;completion.SetResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,"{}")));
        try{await task;Assert.Fail("Old owner response accepted");}catch(OperationCanceledException){}
    }
    [Test]public void SparseV7GrantPreservesRealSlotsAndRejectsAbsentSlotCommand()
    {
        var snapshot=Before(1);Assert.AreEqual(1,snapshot.Slots.Count);var scope=new AugmentSelectionScope(new AugmentSelectionOwner("account","TEST"),"run","grant");
        Assert.Throws<FormatException>(()=>AugmentSelectionCommand.Create(scope,"SELECT",1,null,snapshot));
    }
    [Test]public async Task ExplicitFreeRerollUsesOriginalCommandAndNoCandidateDoesNotConsume(){
        var owner=new AugmentSelectionOwner("account","TEST");var scope=new AugmentSelectionScope(owner,"run","grant");var before=Before(1);var captured=Session();
        var result=new JObject{["outcome"]="NO_CANDIDATE",["snapshot"]=JObject.Parse(before.Json),["approval"]=null};
        var readout=new JObject{["version"]="augment.target-readout.v1.20261021",["runId"]=before.RunId,["eventId"]=before.EventId,["catalogVersion"]=before.CatalogVersion,["grantRevision"]=before.GrantRevision,["cards"]=new JArray()};
        var wire=new Wire{Reply=()=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,new JObject{["version"]="combat.augment-reroll.v1.20261021",["committed"]=true,["result"]=result,["targetReadout"]=readout}.ToString())))};
        var constructor=typeof(AugmentSelectHttpPort).GetConstructor(new[]{typeof(IGameApiAuthenticatedHttpTransport),typeof(AugmentSelectionOwner),typeof(Func<GameApiAuthSessionSnapshot>),typeof(Func<bool>),typeof(bool)});Assert.IsNotNull(constructor,"Explicit FREE capability must use the existing authenticated selection port");
        var port=(IAugmentSelectionPort)constructor.Invoke(new object[]{wire,owner,new Func<GameApiAuthSessionSnapshot>(()=>captured),new Func<bool>(()=>true),true});var journal=new Journal();
        using(var controller=new AugmentSelectionController(port,journal,()=>owner)){
            await controller.OpenFromSnapshotAsync(scope,before);await controller.RerollAsync(0,"FREE");
            Assert.IsFalse(controller.HasPending);Assert.AreEqual(3,controller.Snapshot.Slots[0].FreeRemaining);Assert.AreEqual(0,controller.Snapshot.GrantRevision);
            Assert.AreEqual("/v1/runs/run/augment-grants/grant/reroll",wire.Request.EndpointPath);Assert.AreEqual("FREE",(string)JObject.Parse(wire.Request.BodyJson)["command"]["mode"]);Assert.AreEqual(1,wire.Calls);
        }
    }
}
