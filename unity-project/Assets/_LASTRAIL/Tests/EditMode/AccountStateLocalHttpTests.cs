using System;
using System.IO;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    // Explicit local-only integration: actual owner Game API + in-memory SQLite harness.
    public sealed class AccountStateLocalHttpTests
    {
        [Test,Explicit("Requires account-state-local-bridge.mts running on loopback.")]
        public async Task ActualHttpSnapshotMutationReplayAndRevokedSession()
        {
            var fixture=JObject.Parse(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"../QA/ScreenIntegration/local-s01-fixture.json")));
            var url=(string)fixture["url"];Assert.That(new Uri(url).Host,Is.EqualTo("127.0.0.1"));
            var wire=new InstalledAdmissionTransport(new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(url,10)),new AppBuildSnapshot("1.0.0",100),"android",null);
            var token=new GameApiAccessToken((string)fixture["token"]);
            var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",token,DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("local-fixture"),DateTime.UtcNow.AddMinutes(10));
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){
                await state.RefreshAccountAsync();Assert.That(state.AccountStatus,Is.EqualTo(AccountStateReadStatus.Ready),state.AccountErrorCode);
                Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(123));Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(1000));
                Assert.That(state.Account.TrainingCoreSlots[0].Number("trainedLevel"),Is.EqualTo(3));Assert.That(state.Account.ActivationSupported,Is.False);
                var key=Guid.NewGuid().ToString("N");
                var grow=new RunApiHttpRequest("POST","/v1/operators/training-core/2/level-up",new JObject{["economyVersion"]=fixture["economyVersion"],["targetLevel"]=4}.ToString(),new[]{new RunApiHttpHeader("Idempotency-Key",key)});
                var first=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(grow,token));Assert.That(first.StatusCode,Is.EqualTo(200),first.BodyJson);
                await state.RefreshAccountAsync();Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(940));Assert.That(state.Account.TrainingCoreSlots[0].Number("trainedLevel"),Is.EqualTo(4));
                var sequence=state.Account.MutationSequence;
                var replay=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(grow,token));Assert.That(replay.StatusCode,Is.EqualTo(200),replay.BodyJson);
                await state.RefreshAccountAsync();Assert.That(state.Account.MutationSequence,Is.EqualTo(sequence));Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(940));
                var snapshot=state.Account;
                var revoke=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/revoke",null,null),token));Assert.That(revoke.StatusCode,Is.EqualTo(200));
                await state.RefreshAccountAsync();Assert.That(state.AccountStatus,Is.EqualTo(AccountStateReadStatus.AuthRequired));Assert.That(state.Account,Is.SameAs(snapshot));Assert.That(state.AccountIsStale,Is.True);
            }
        }
    }
}
