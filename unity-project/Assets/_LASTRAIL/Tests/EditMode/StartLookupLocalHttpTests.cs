using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace LastRail.Tests.EditMode
{
    public sealed partial class FormationLocalHttpTests
    {
        static IAndroidKeystoreNativeStoragePort TestSecurePort()=> (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(AccountScreenIntegrationTests).GetNestedType("SecurePort",BindingFlags.NonPublic),true);
        [Test,Explicit("Requires pinnedeef4885; read-only start-result with real memory SQLite and persisted client journal.")]
        public async Task ActualUnknownStartLookupNotFoundToCommittedDurableReplayIsolationAndNoMutation()
        {
            var(wire,session)=await Setup();
            Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            Func<Task<string>> digest=async()=> (string)JObject.Parse((await control("lookup-digest")).BodyJson)["hash"];
            await control("loadout-enable");await control("departure-enable");string preview;
            using(var setup=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1)){
                await setup.RefreshSharedAsync();await setup.RefreshTrainLoadoutAsync();var ids=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};for(int i=0;i<5;i++)setup.Formation.Select(i,ids[i]);await setup.Formation.SaveAsync();await setup.Formation.ActivateAsync();setup.SelectTrainCarriage(0,"car_combat");setup.SelectTrainCarriage(1,"car_gun");await setup.SaveTrainLoadoutAsync();await setup.PreviewDepartureAsync(DepartureContract.Operations[0],"I");Assert.That(setup.Departure,Is.Not.Null,setup.ActionNotice);preview=setup.Departure.Id;
            }
            var port=TestSecurePort();var journal=new AccountActionJournal(port);var action=new PendingAccountAction("account_test","departure-start",preview,0,"old_original_key",new JObject{["operationPreviewId"]=preview}.ToString());Assert.That(journal.Save(action),Is.True);
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                var before=await digest();await state.LookupDepartureStartAsync();Assert.That(state.DepartureLookupStatus,Is.EqualTo("NOT_FOUND"),state.DepartureLookupNotice);Assert.That(state.KnownDepartureRunId,Is.Null);Assert.That(journal.Read("account_test").Key,Is.EqualTo(action.Key));Assert.That(await digest(),Is.EqualTo(before));
            }
            // Historical committed receipt appears after the first read; no public start is enabled.
            var committed=await control("combat-old-run");Assert.That(committed.StatusCode,Is.EqualTo(200));var original=GameApiJson.ReadObject(committed.BodyJson);
            using(var state=new AccountScreenRuntime(wire,new AccountActionJournal(port),()=>session,()=>1)){
                var before=await digest();await state.LookupDepartureStartAsync();Assert.That(state.DepartureLookupStatus,Is.EqualTo("COMMITTED"),state.DepartureLookupNotice);Assert.That(JToken.DeepEquals(state.DepartureStartResult.StartResponse,original),Is.True);Assert.That(state.KnownDepartureRunId,Is.EqualTo("run_old"));Assert.That(await digest(),Is.EqualTo(before));
                Assert.That(state.Pending.Key,Is.EqualTo(action.Key));Assert.That(state.Pending.Payload,Is.EqualTo(action.Payload));Assert.That(state.Pending.StartResultJson,Is.Not.Null);Assert.That(state.CanConfirmDeparture,Is.False);Assert.That(state.CanRetryDeparture,Is.False);
                var root=new GameObject("StartLookupUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenOperations();
                    var deadline=DateTime.UtcNow.AddSeconds(10);while(state.DepartureLookupLoading&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(root.GetComponentsInChildren<Button>().Any(b=>b.name=="LookupDepartureStart"),Is.True);Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains(action.Key)),Is.False);
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"start-result-recovery-pinned-local-http.png"});
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            using(var restored=new AccountScreenRuntime(wire,new AccountActionJournal(port),()=>session,()=>1)){
                restored.SynchronizeSession();Assert.That(restored.KnownDepartureRunId,Is.EqualTo("run_old"));Assert.That(JToken.DeepEquals(restored.DepartureStartResult.StartResponse,original),Is.True);
                await control("combat-completed");await restored.LookupDepartureStartAsync();Assert.That(restored.DepartureStartResult.RunStatus,Is.EqualTo("COMPLETED"));Assert.That(JToken.DeepEquals(restored.DepartureStartResult.StartResponse,original),Is.True);
                await control("lookup-other-key");var otherJournal=new Journal();otherJournal.Save(new PendingAccountAction("account_test","departure-start",preview,0,"other_original_key",action.Payload));
                using(var other=new AccountScreenRuntime(wire,otherJournal,()=>session,()=>1)){await other.LookupDepartureStartAsync();Assert.That(other.DepartureLookupStatus,Is.EqualTo("NOT_FOUND"));Assert.That(other.KnownDepartureRunId,Is.Null);Assert.That(other.Pending,Is.Not.Null);}
                var conflictJournal=new Journal();conflictJournal.Save(new PendingAccountAction("account_test","departure-start","preview_wrong",0,action.Key,"{\"operationPreviewId\":\"preview_wrong\"}"));
                using(var conflict=new AccountScreenRuntime(wire,conflictJournal,()=>session,()=>1)){await conflict.LookupDepartureStartAsync();Assert.That(conflict.DepartureLookupStatus,Is.EqualTo("CONFLICT"));Assert.That(conflict.DepartureStartResult.RunId,Is.Null);Assert.That(conflict.Pending.Target,Is.EqualTo("preview_wrong"));}
                await control("combat-corrupt-snapshot");await restored.LookupDepartureStartAsync();Assert.That(restored.DepartureLookupStatus,Is.EqualTo("SNAPSHOT_UNAVAILABLE"));
                await control("combat-no-snapshot");var before=await digest();await restored.LookupDepartureStartAsync();Assert.That(restored.DepartureLookupStatus,Is.EqualTo("SNAPSHOT_UNAVAILABLE"));Assert.That(await digest(),Is.EqualTo(before));Assert.That(restored.Pending.StartResultJson,Is.Not.Null);Assert.That(JToken.DeepEquals(restored.DepartureStartResult.StartResponse,original),Is.True);
                await control("combat-foreign");var foreignJournal=new Journal();foreignJournal.Save(action);
                using(var foreign=new AccountScreenRuntime(wire,foreignJournal,()=>session,()=>1)){await foreign.LookupDepartureStartAsync();Assert.That(foreign.DepartureLookupStatus,Is.EqualTo("SNAPSHOT_UNAVAILABLE"));Assert.That(foreign.DepartureStartResult.RunId,Is.Null);Assert.That(foreign.DepartureStartResult.StartResponse,Is.Null);}
                await control("lookup-fail");before=await digest();await restored.LookupDepartureStartAsync();Assert.That(restored.DepartureLookupStatus,Is.EqualTo("TEMPORARILY_UNAVAILABLE"));Assert.That(restored.DepartureLookupError,Is.EqualTo("start_lookup_unavailable"));Assert.That(await digest(),Is.EqualTo(before));Assert.That(restored.Pending.Key,Is.EqualTo(action.Key));
                await control("revoke");await restored.LookupDepartureStartAsync();Assert.That(restored.DepartureLookupError,Is.EqualTo("auth_session_invalid"));Assert.That(restored.Pending.StartResultJson,Is.Not.Null);
            }
            Assert.That(wire.Requests.Any(r=>r.EndpointPath=="/v1/runs/start"),Is.False);
            var lookups=wire.Requests.Where(r=>r.EndpointPath=="/v1/runs/start-result").ToArray();Assert.That(lookups.Length,Is.GreaterThanOrEqualTo(8));Assert.That(lookups.All(r=>r.Method=="POST"&&!r.Headers.Any(h=>h.Name.Equals("Idempotency-Key",StringComparison.OrdinalIgnoreCase))&&!r.EndpointPath.Contains("?")),Is.True);
            var firstBody=JObject.Parse(lookups[0].BodyJson);Assert.That(firstBody.Count,Is.EqualTo(2));Assert.That((string)firstBody["startIdempotencyKey"],Is.EqualTo(action.Key));Assert.That((string)firstBody["operationPreviewId"],Is.EqualTo(action.Target));
        }
    }
}
