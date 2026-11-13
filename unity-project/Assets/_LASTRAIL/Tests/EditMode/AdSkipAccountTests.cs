using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AdSkipAccountTests
    {
        sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Calls=new();
            public Func<Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Calls.Add(request);return Reply();}
        }
        static GameApiAuthSessionSnapshot Session()=>new("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
        static object Create(Wire wire,Func<GameApiAuthSessionSnapshot> session,Func<long> generation)
        {
            var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.MissionShell.AdSkipAccountService");
            Assert.That(type,Is.Not.Null,"Account-scoped ad-skip read service is missing");
            return Activator.CreateInstance(type,wire,session,generation);
        }
        static object Get(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target,string name)=>target.GetType().GetMethod(name).Invoke(target,null);
        [Test] public async Task ConfirmedAbsenceAndMissingStoreStayDistinctFromUnknown()
        {
            const string body="{\"accepted\":true,\"contractVersion\":\"ad-skip-v1\",\"owned\":false,\"entitlement\":null,\"catalog\":null,\"purchaseAvailable\":false,\"featureAvailability\":{\"OFFLINE\":\"UNAVAILABLE\",\"COMBAT\":\"UNAVAILABLE\",\"REROLL\":\"UNSUPPORTED\"},\"serverTime\":1000}";
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,body))};var session=Session();var service=Create(wire,()=>session,()=>1);
            try
            {
                await (Task)Call(service,"RefreshAsync");var snapshot=Get(service,"Snapshot");Assert.That(snapshot,Is.Not.Null);
                Assert.That(Get(snapshot,"Owned"),Is.False);Assert.That(Get(snapshot,"PurchaseAvailable"),Is.False);Assert.That(Get(snapshot,"CatalogConfigured"),Is.False);
                Assert.That(Get(snapshot,"RerollAvailability"),Is.EqualTo("UNSUPPORTED"));Assert.That(Get(service,"Notice"),Is.EqualTo(""));
                wire.Reply=()=>Task.FromResult(RunApiHttpResponse.NetworkError("offline"));await (Task)Call(service,"RefreshAsync");Assert.That(Get(service,"Snapshot"),Is.Null);
            }
            finally{((IDisposable)service).Dispose();}
        }

        [Test] public async Task UnavailableReadCannotInventOwnershipOrCallAdvertising()
        {
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.NetworkError("offline"))};var session=Session();
            var service=Create(wire,()=>session,()=>1);
            try
            {
                await (Task)Call(service,"RefreshAsync");
                Assert.That(Get(service,"Snapshot"),Is.Null);Assert.That(Get(service,"Loading"),Is.False);
                Assert.That((string)Get(service,"Notice"),Does.Contain("확인"));
                Assert.That(wire.Calls.Count,Is.EqualTo(1));Assert.That(wire.Calls[0].Method,Is.EqualTo("GET"));Assert.That(wire.Calls[0].EndpointPath,Is.EqualTo("/v1/ad-skip"));
            }
            finally{((IDisposable)service).Dispose();}
        }
        [Test] public async Task ResetDropsLateResponseAndItsNotice()
        {
            var gate=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>gate.Task};var session=Session();long generation=1;
            var service=Create(wire,()=>session,()=>generation);
            try
            {
                var pending=(Task)Call(service,"RefreshAsync");generation++;Call(service,"Reset");
                gate.SetResult(RunApiHttpResponse.NetworkError("late account A"));await pending;
                Assert.That(Get(service,"Snapshot"),Is.Null);Assert.That(Get(service,"Loading"),Is.False);Assert.That(Get(service,"Notice"),Is.EqualTo(""));
            }
            finally{((IDisposable)service).Dispose();}
        }
        [Test] public async Task InvalidSuccessfulBodyIsNotAnOwnedEntitlement()
        {
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,"{}"))};var session=Session();
            var service=Create(wire,()=>session,()=>1);
            try{await (Task)Call(service,"RefreshAsync");Assert.That(Get(service,"Snapshot"),Is.Null);Assert.That(Get(service,"Loading"),Is.False);Assert.That((string)Get(service,"Notice"),Does.Contain("확인"));}
            finally{((IDisposable)service).Dispose();}
        }
        [Test] public async Task OlderSuccessfulReadCannotOverwriteLatestRevocation(){
            var first=new TaskCompletionSource<RunApiHttpResponse>();int count=0;var wire=new Wire{Reply=()=>++count==1?first.Task:Task.FromResult(RunApiHttpResponse.Success(200,AdSkipUseTests.Sample("17-revoked")))};var owner=Session();using var service=new AdSkipAccountService(wire,()=>owner,()=>1);
            var pending=service.RefreshAsync();await service.RefreshAsync();first.SetResult(RunApiHttpResponse.Success(200,AdSkipUseTests.Sample("07-owned-available")));await pending;
            Assert.That(service.Snapshot.Status,Is.EqualTo("REVOKED"));Assert.That(service.Snapshot.Owned,Is.False);
        }
        [TestCase(false)][TestCase(true)] public async Task AccountChangeOrDisposeDropsLatePositiveEntitlement(bool dispose){
            var gate=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>gate.Task};var owner=Session();long generation=1;using var service=new AdSkipAccountService(wire,()=>owner,()=>generation);var pending=service.RefreshAsync();
            if(dispose)service.Dispose();else{owner=AdSkipUseTests.Session("b");generation++;service.Reset();}
            gate.SetResult(RunApiHttpResponse.Success(200,AdSkipUseTests.Sample("07-owned-available")));await pending;Assert.That(service.Snapshot,Is.Null);Assert.That(service.Loading,Is.False);
        }
    }
}

