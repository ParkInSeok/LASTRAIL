using System;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class LobbyHostMvcTests
    {
        sealed class Wire:IRunApiHttpTransport
        {
            public int Requests;public TaskCompletionSource<RunApiHttpResponse> Gate=new TaskCompletionSource<RunApiHttpResponse>();
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests++;return Gate.Task;}
        }
        [TestCase(false)] [TestCase(true)]
        public async Task CurrencyReadCannotOpenPopupAfterNavigationOrDispose(bool dispose)
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            int opens=0;using var controller=new LobbyHomeController(account,_=>{},_=>opens++);
            var pending=controller.OpenCurrencyAsync();Assert.Greater(wire.Requests,0);
            if(dispose)controller.Dispose();else controller.CancelPopupRequest();
            wire.Gate.SetResult(RunApiHttpResponse.NetworkError("offline"));await pending;Assert.AreEqual(0,opens);
        }
        [Test] public async Task ResultControllerCannotUseOldRunAfterAccountSwitchOrDispose()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();long generation=1;using var account=new AccountScreenRuntime(wire,null,()=>session,()=>generation);
            using var controller=new RunLifecycleScreenController(account,"run_old",()=>Assert.Fail("Old result cannot open ad"));
            generation++;Assert.IsNull(controller.Display);await controller.LoadAsync(false);await controller.ClaimAsync();await controller.RecoverAsync();await controller.RefreshAsync();Assert.AreEqual(0,wire.Requests);
            controller.Dispose();await controller.ClaimAsync();Assert.AreEqual(0,wire.Requests);
        }
        [Test] public void MissingAccountProjectionHasNoFabricatedBalancesOrPortraits()
        {
            var display=new LobbyHomeState(null);Assert.That(display.Balances,Is.All.EqualTo("—"));Assert.That(display.Operators,Is.All.Null);
            var result=new RunLifecycleScreenState(null,"run");Assert.IsFalse(result.CanClaim);Assert.IsFalse(result.CanRecover);Assert.IsFalse(result.CanRefresh);
        }
    }
}
