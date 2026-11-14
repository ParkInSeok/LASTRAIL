using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionTrainScreenControllerTests
    {
        sealed class Journal:IAccountActionJournal
        {
            public PendingAccountAction Row;
            public PendingAccountAction Read(string account)=>Row;
            public bool Save(PendingAccountAction action){Row=action;return true;}
            public bool Remove(PendingAccountAction action){Row=null;return true;}
        }
        sealed class Wire:IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply=request=>Task.FromResult(RunApiHttpResponse.NetworkError("lost"));
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Reply(request);}
        }
        static GameApiAuthSessionSnapshot Session()=>new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));
        static JObject Loadout()
        {
            var limits=new JObject();foreach(var type in new[]{"COMBAT","GUN","CARGO","DEFENSE","MEDICAL"})limits[type]=new JObject{["min"]=type=="COMBAT"?1:0,["max"]=type=="COMBAT"||type=="CARGO"?1:2};
            return new JObject{["rulesVersion"]="train-loadout-v1",["train"]=new JObject{["trainId"]="train_a",["level"]=1,["revision"]=1},["ownedCarriages"]=new JArray(new[]{"car_combat","car_gun"}.Select(id=>new JObject{["carriageId"]=id,["level"]=1,["revision"]=1,["type"]=id=="car_combat"?"COMBAT":"GUN",["installationSupported"]=true,["installable"]=true,["reason"]=null})),["ownedTurrets"]=new JArray(),["unlockedSlotCount"]=2,["progressError"]=null,["typeLimits"]=limits,["revision"]=0,["loadout"]=null,["loadoutError"]=null,["openRun"]=false,["updatedAt"]=null};
        }

        [Test] public async Task TrainUsesOwnedDraftAndRoutesOneSaveThenDisposalBlocksCommands()
        {
            var wire=new Wire{Reply=request=>Task.FromResult(request.Method=="GET"?RunApiHttpResponse.Success(200,Loadout().ToString()):RunApiHttpResponse.NetworkError("lost"))};var session=Session();var journal=new Journal();
            using var owner=new AccountScreenRuntime(wire,journal,()=>session,()=>1);await owner.RefreshTrainLoadoutAsync();Assert.That(owner.TrainLoadoutReady,Is.True,owner.TrainLoadoutNotice);
            TrainScreenState shown=null;int renders=0;var controller=new TrainScreenController(owner,1,0,0,state=>{shown=state;renders++;},null);controller.Render();
            Assert.That(shown.Slots[2].Enabled,Is.False);controller.Install("car_combat");controller.SelectSlot(1);controller.Install("car_gun");
            Assert.That(owner.TrainDraft.Take(2),Is.EqualTo(new[]{"car_combat","car_gun"}));Assert.That(shown.CanSave,Is.True);Assert.That(shown.Rows.All(row=>row.Selected),Is.True);
            await controller.SaveAsync();Assert.That(wire.Requests.Count(request=>request.Method=="POST"),Is.EqualTo(1));Assert.That(journal.Row?.Kind,Is.EqualTo("train-loadout"));Assert.That(shown.SaveLabel,Is.EqualTo("원래 장착 요청 재확인"));
            controller.Dispose();int before=renders,calls=wire.Requests.Count;controller.Clear();controller.Install("car_combat");controller.SelectSlot(0);await controller.SaveAsync();await controller.UpgradeAsync();await controller.RefreshAsync();
            Assert.That(renders,Is.EqualTo(before));Assert.That(wire.Requests.Count,Is.EqualTo(calls));Assert.That(owner.TrainDraft.Take(2),Is.EqualTo(new[]{"car_combat","car_gun"}));
        }

        [Test] public async Task TrainLateReadAndChangedSessionCannotRenderOrMutateOldScreen()
        {
            var completion=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=_=>completion.Task};var session=Session();long generation=1;
            using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>generation);int renders=0;var controller=new TrainScreenController(owner,1,0,0,_=>renders++,null);
            var read=owner.RefreshTrainLoadoutAsync();controller.Dispose();int before=renders;completion.SetResult(RunApiHttpResponse.Success(200,Loadout().ToString()));await read;Assert.That(renders,Is.EqualTo(before));
            using var oldSession=new TrainScreenController(owner,1,0,0,_=>renders++,null);generation++;int calls=wire.Requests.Count;oldSession.Install("car_combat");oldSession.Render();await oldSession.LoadAsync();await oldSession.SaveAsync();Assert.That(wire.Requests.Count,Is.EqualTo(calls));Assert.That(renders,Is.EqualTo(before));Assert.That(owner.TrainDraft.All(id=>id==null),Is.True);
        }

        [Test] public async Task MissionsRetryUsesOriginalCommandAndUnbindLeavesBorrowedRuntimeUsable()
        {
            var session=Session();var wire=new Wire();var journal=new Journal{Row=new PendingAccountAction("account_test","recurring-mail","award",0,"original_key",null)};
            using var owner=new AccountScreenRuntime(wire,journal,()=>session,()=>1);owner.SynchronizeSession();int renders=0;MissionScreenState shown=null;
            var controller=new MissionScreenController(owner,2,99,state=>{shown=state;renders++;},null);controller.Render();Assert.That(shown.Rows,Is.Empty);Assert.That(shown.Page,Is.Zero);Assert.That(shown.RefreshLabel,Is.EqualTo("요청 결과 재확인"));
            await controller.ClaimAsync(0);Assert.That(wire.Requests,Is.Empty);await controller.RefreshAsync();Assert.That(wire.Requests.Count,Is.EqualTo(1));Assert.That(wire.Requests[0].EndpointPath,Is.EqualTo("/v1/quest-mail/award/claim"));Assert.That(wire.Requests[0].Headers.Single(h=>h.Name=="Idempotency-Key").Value,Is.EqualTo("original_key"));
            controller.Dispose();int before=renders,calls=wire.Requests.Count;controller.NextPage();controller.OpenBulk();await controller.RefreshAsync();await controller.LoadAsync();Assert.That(renders,Is.EqualTo(before));Assert.That(wire.Requests.Count,Is.EqualTo(calls));
            await owner.RefreshRecurringAsync();Assert.That(renders,Is.EqualTo(before));Assert.That(wire.Requests.Count,Is.EqualTo(calls+1));
        }

        [Test] public async Task MissionsSessionChangeBlocksSyncRetryAndRendering()
        {
            var wire=new Wire();var session=Session();long generation=1;using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>generation);int renders=0;
            using var controller=new MissionScreenController(owner,0,0,_=>renders++,null);controller.Render();generation++;controller.Render();controller.NextPage();await controller.ClaimAsync(0);await controller.LoadAsync();await controller.RefreshAsync();Assert.That(renders,Is.EqualTo(1));Assert.That(wire.Requests,Is.Empty);
        }
    }
}
