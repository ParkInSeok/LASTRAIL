using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Support;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class EntrySupportScreenControllerTests
    {
        sealed class InquiryClient:ISupportInquiryClient
        {
            public int Calls;public readonly TaskCompletionSource<InquiryReceipt> Reply=new TaskCompletionSource<InquiryReceipt>();
            public Task<InquiryReceipt> SubmitAsync(InquirySubmission request,GameApiAuthSessionSnapshot session){Calls++;return Reply.Task;}
        }
        sealed class Wire:IRunApiHttpTransport
        {
            public int Calls;public readonly TaskCompletionSource<RunApiHttpResponse> Reply=new TaskCompletionSource<RunApiHttpResponse>();
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Calls++;return Reply.Task;}
        }
        [Test] public async Task SupportDetachPreservesReceiptAndReentryHasOneSubscription()
        {
            var client=new InquiryClient();var model=new SupportInquiryController(SupportInquiryTests.Session(),client,new[]{"fixture"},100,()=>new AppBuildSnapshot("1",1));int renders=0;
            var screen=new SupportInquiryScreenController(model,()=>renders++);screen.Activate();screen.Activate();screen.Edit(0,"inquiry draft");
            var sending=screen.SubmitAsync();await screen.SubmitAsync();Assert.AreEqual(1,client.Calls);screen.Deactivate();int hidden=renders;
            client.Reply.SetResult(new InquiryReceipt(InquiryDelivery.Received,"ticket",DateTimeOffset.UtcNow));await sending;Assert.AreEqual(hidden,renders);Assert.AreEqual(InquiryState.Received,model.State);
            screen.Activate();screen.Activate();int before=renders;await screen.SubmitAsync();Assert.AreEqual(InquiryState.Editing,model.State);Assert.AreEqual(before+1,renders);
            screen.Dispose();before=renders;screen.Edit(0,"ignored");await screen.SubmitAsync();Assert.AreEqual(before,renders);Assert.AreEqual(1,client.Calls);
        }
        [Test] public async Task CredentialConsentAndSecretClearPrecedeDispatchAndCancelInvalidatesLateReply()
        {
            var wire=new Wire();var flow=new PasswordCredentialFlow(new PasswordEmailClient(wire),"Register",()=>null,()=>0,_=>Task.FromResult(true),()=>DateTimeOffset.UtcNow);
            int renders=0,clears=0,closes=0;var screen=new PasswordCredentialScreenController(flow,()=>renders++,()=>{Assert.AreEqual(0,wire.Calls);clears++;},()=>closes++);
            screen.Activate();await screen.SendCodeAsync("fixture","test@example.invalid","password8","password8",false,true);Assert.AreEqual(0,wire.Calls);
            var send=screen.SendCodeAsync("fixture","test@example.invalid","password8","password8",true,true);Assert.AreEqual(1,wire.Calls);Assert.AreEqual(1,clears);Assert.IsTrue(screen.HasRegistrationDraft);
            screen.Deactivate();Assert.IsFalse(screen.HasRegistrationDraft);int hidden=renders;wire.Reply.SetResult(RunApiHttpResponse.NetworkError("fixture"));await send;Assert.AreEqual(hidden,renders);
            screen.Activate();screen.Dispose();await screen.ConfirmCodeAsync("12345678");screen.Cancel();Assert.AreEqual(0,closes);Assert.AreEqual(1,wire.Calls);
        }
        [Test] public void CredentialCancelClosesOnceAndClearsDraft()
        {
            var flow=new PasswordCredentialFlow(new PasswordEmailClient(new Wire()),"Register",()=>null,()=>0,_=>Task.FromResult(true),()=>DateTimeOffset.UtcNow);
            int closes=0;PasswordCredentialScreenController screen=null;
            screen=new PasswordCredentialScreenController(flow,()=>{},()=>{},()=>{closes++;screen.Cancel();screen.Deactivate();});screen.Activate();screen.Cancel();screen.Cancel();
            Assert.AreEqual(1,closes);Assert.AreEqual(PasswordCredentialStage.Cancelled,flow.Stage);Assert.IsFalse(screen.HasRegistrationDraft);screen.Dispose();
        }
    }
}
