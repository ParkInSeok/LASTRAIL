using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Tests.EditMode {
 public sealed class UltimateSelectionTests {
  [Test] public void ExplicitSelectionHasSeparateReadAndSave() {
   Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshUltimateAsync"),Is.Not.Null);
   Assert.That(typeof(AccountScreenRuntime).GetMethod("SaveUltimateAsync"),Is.Not.Null);
  }
  static JObject Read(string id=null,long revision=0)=>new JObject{["accepted"]=true,["rulesVersion"]="ultimate-selection-v1",["operatorId"]=id==null?JValue.CreateNull():new JValue(id),["revision"]=revision,["formationRevision"]=1,["selectionStatus"]=id==null?"UNSELECTED":"SELECTED",["changeLocked"]=false};
  [Test] public void StrictNullChoiceAndSafeRevisionContract() {
   var r=Read();Assert.That(new UltimateSelectionSnapshot(r.ToString()).OperatorId,Is.Null);
   r["selectionStatus"]="SELECTED";Assert.Throws<FormatException>(()=>new UltimateSelectionSnapshot(r.ToString()));
   r=Read("op_kyle",1);r["revision"]=9007199254740991L;Assert.Throws<FormatException>(()=>new UltimateSelectionSnapshot(r.ToString()));
   var body=new JObject{["rulesVersion"]="ultimate-selection-v1",["operatorId"]=JValue.CreateNull(),["expectedRevision"]=0,["expectedFormationRevision"]=1};
   Assert.DoesNotThrow(()=>new PendingAccountAction("account_test","ultimate-selection","active",0,"key:/+",body.ToString()));
   body["fallback"]="op_kyle";Assert.Throws<FormatException>(()=>UltimateSelectionSnapshot.ValidateRequest("active",body.ToString()));
  }
  sealed class Wire:IRunApiHttpTransport {
   public readonly List<TaskCompletionSource<RunApiHttpResponse>> Replies=new List<TaskCompletionSource<RunApiHttpResponse>>();
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Assert.That(r.EndpointPath,Is.EqualTo("/v1/formation/ultimate"));Assert.That(r.Method,Is.EqualTo("GET"));Assert.That(r.BodyJson,Is.Null);var p=new TaskCompletionSource<RunApiHttpResponse>();Replies.Add(p);return p.Task;}
  }
  static GameApiAuthSessionSnapshot Session()=>new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));
  [TestCase(false)][TestCase(true)] public async Task PreviousGenerationReadCannotReplaceChoiceOrFailure(bool failure) {
   var wire=new Wire();var session=Session();long generation=1;using var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation);
   var old=state.RefreshUltimateAsync();generation++;state.SynchronizeSession();var latest=state.RefreshUltimateAsync();wire.Replies[1].SetResult(RunApiHttpResponse.Success(200,Read("op_eve",2).ToString()));await latest;
   wire.Replies[0].SetResult(failure?RunApiHttpResponse.NetworkError("old"):RunApiHttpResponse.Success(200,Read("op_kyle",1).ToString()));await old;
   Assert.That(state.Ultimate.OperatorId,Is.EqualTo("op_eve"));Assert.That(state.UltimateDraft,Is.EqualTo("op_eve"));Assert.That(state.UltimateReady,Is.True);
  }
  [Test] public async Task OwnerChangeDiscardsReadWithoutSelectingDefault() {
   var wire=new Wire();var session=Session();long generation=1;using var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation);
   var old=state.RefreshUltimateAsync();generation++;state.SynchronizeSession();wire.Replies[0].SetResult(RunApiHttpResponse.Success(200,Read("op_kyle",1).ToString()));await old;
   Assert.That(state.Ultimate,Is.Null);Assert.That(state.UltimateDraft,Is.Null);Assert.That(state.CanEditUltimate,Is.False);
  }
 }
}
