using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class TrainLoadoutTests
    {
        static JObject Snapshot(long revision=0)
        {
            var limits=new JObject();foreach(var type in new[]{"COMBAT","GUN","CARGO","DEFENSE","MEDICAL"})limits[type]=new JObject{["min"]=type=="COMBAT"?1:0,["max"]=type=="COMBAT"||type=="CARGO"?1:2};
            return new JObject{["rulesVersion"]="train-loadout-v1",["train"]=new JObject{["trainId"]="train_a",["level"]=1,["revision"]=1},["ownedCarriages"]=new JArray(new[]{"car_combat","car_gun"}.Select(id=>new JObject{["carriageId"]=id,["level"]=1,["revision"]=1,["type"]=id=="car_combat"?"COMBAT":"GUN",["installationSupported"]=true,["installable"]=true,["reason"]=null})),["ownedTurrets"]=new JArray(),["unlockedSlotCount"]=2,["progressError"]=null,["typeLimits"]=limits,["revision"]=revision,["loadout"]=revision==0?null:TrainLoadoutWrite.Selection("train_a",new[]{"car_combat","car_gun"},2),["loadoutError"]=null,["openRun"]=false,["updatedAt"]=revision==0?null:"2026-09-10T00:00:00.000Z"};
        }
        [Test] public void GetHasNoAcceptedEnvelopeAndPreservesInvalidSavedSelection()
        {
            var raw=Snapshot(3);raw["unlockedSlotCount"]=null;raw["progressError"]="story_progress_inconsistent";raw["loadoutError"]="story_progress_inconsistent";
            var snapshot=new TrainLoadoutSnapshot(raw.ToString());Assert.That(snapshot.SlotCount,Is.Null);Assert.That(snapshot.Loadout,Is.Not.Null);Assert.That(snapshot.Revision,Is.EqualTo(3));
            var copied=snapshot.Loadout;copied["carriages"][0]["carriageId"]="other";Assert.That((string)snapshot.Loadout["carriages"][0]["carriageId"],Is.EqualTo("car_combat"));
        }
        [Test] public void LoadoutPostIsStrictAndJournalRetainsExactPayload()
        {
            var r=TrainLoadoutWrite.Selection("train_a",new[]{"car_combat","car_gun"},2);r["expectedRevision"]=0;var payload=r.ToString();var action=new PendingAccountAction("account_test","train-loadout","train_a",0,"original_key",payload);Assert.That(action.Payload,Is.EqualTo(payload));
            r["clientStats"]=new JObject();Assert.Throws<FormatException>(()=>TrainLoadoutWrite.Validate("train_a",r.ToString()));r.Remove("clientStats");r["carriages"][1]["carriageId"]="car_combat";Assert.Throws<FormatException>(()=>TrainLoadoutWrite.Validate("train_a",r.ToString()));
        }
        sealed class Wire:IRunApiHttpTransport {public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)=>Reply(r);}
        [TestCase(false)][TestCase(true)] public async Task PreviousGenerationLoadoutReadRejectsOldSuccessOrFailure(bool fails)
        {
            var old=new TaskCompletionSource<RunApiHttpResponse>();int calls=0;var wire=new Wire{Reply=r=>++calls==1?old.Task:Task.FromResult(RunApiHttpResponse.Success(200,Snapshot(5).ToString()))};var session=SupportInquiryTests.Session();
            long generation=1;using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation)){var first=state.RefreshTrainLoadoutAsync();generation++;state.SynchronizeSession();await state.RefreshTrainLoadoutAsync();if(fails)old.SetException(new Exception("old"));else old.SetResult(RunApiHttpResponse.Success(200,Snapshot(1).ToString()));await first;Assert.That(state.TrainLoadout.Revision,Is.EqualTo(5));Assert.That(state.TrainLoadoutReady,Is.True);}
        }
        [Test] public async Task OpenRunBlocksEditingEvenWhenIndividualCarriageIsInstallable()
        {
            var r=Snapshot();r["openRun"]=true;var wire=new Wire{Reply=q=>Task.FromResult(RunApiHttpResponse.Success(200,r.ToString()))};var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){await state.RefreshTrainLoadoutAsync();Assert.That((bool)state.TrainLoadout.Carriages[0]["installable"],Is.True);Assert.That(state.CanEditTrain,Is.False);Assert.That(state.SelectTrainCarriage(0,"car_combat"),Is.False);}
        }
    }
}
