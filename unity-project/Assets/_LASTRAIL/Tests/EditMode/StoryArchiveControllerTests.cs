using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Narrative;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class StoryArchiveControllerTests
    {
        sealed class Wire : IRunApiHttpTransport
        {
            public int Calls;
            public Func<Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {Assert.That(request.Method,Is.EqualTo("GET"));Assert.That(request.EndpointPath,Is.EqualTo("/v1/story-progress"));Calls++;return Reply();}
        }
        sealed class View : IStoryArchiveView
        {
            public event Action<int> SelectionRequested;
            public event Action OpenRequested,BackRequested,RefreshRequested;
            public StoryArchiveViewState State;public int Renders;
            public void Render(StoryArchiveViewState state){State=state;Renders++;}
            public void Select(int row)=>SelectionRequested?.Invoke(row);
            public void Open()=>OpenRequested?.Invoke();
            public void Back()=>BackRequested?.Invoke();
            public void Refresh()=>RefreshRequested?.Invoke();
        }
        [Test] public async Task TextEntryAndReturnReuseExplicitRecordPermissionWithoutWrites()
        {
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")))};
            var session=SupportInquiryTests.Session();using var reader=new StoryArchiveReadModel(wire,()=>session,()=>1);
            var view=new View();int exits=0;using var controller=new StoryArchiveController(reader,view,()=>exits++);
            await controller.RefreshAsync();Assert.That(view.State.Rows.Count,Is.EqualTo(7));Assert.IsTrue(view.State.CanOpen);
            view.Select(1);Assert.IsFalse(view.State.CanOpen);view.Open();Assert.IsFalse(view.State.IsReading);
            view.Select(0);view.Open();Assert.IsTrue(view.State.IsReading);Assert.That(view.State.DetailText,Is.EqualTo(MissionNarrativeCatalog.CreateActOne().Definitions[0].CodexEntry));
            view.Back();Assert.IsFalse(view.State.IsReading);Assert.That(exits,Is.Zero);view.Back();Assert.That(exits,Is.EqualTo(1));Assert.That(wire.Calls,Is.EqualTo(1));
        }
        [Test] public async Task ClosedViewReceivesNeitherDelayedReadNorOldInputAndBorrowedReaderSurvives()
        {
            var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>pending.Task};var session=SupportInquiryTests.Session();using var reader=new StoryArchiveReadModel(wire,()=>session,()=>1);
            var view=new View();int exits=0;var controller=new StoryArchiveController(reader,view,()=>exits++);var read=controller.RefreshAsync();Assert.That(view.State.Status,Is.EqualTo(StoryArchiveReadStatus.Loading));
            controller.Dispose();int renders=view.Renders;view.Refresh();view.Back();view.Select(1);view.Open();
            pending.SetResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")));await read;
            Assert.That(view.Renders,Is.EqualTo(renders));Assert.That(exits,Is.Zero);Assert.That(wire.Calls,Is.EqualTo(1));Assert.That(reader.Status,Is.EqualTo(StoryArchiveReadStatus.Ready));
            using var replacement=new StoryArchiveController(reader,view,()=>exits++);view.Back();Assert.That(exits,Is.EqualTo(1));
        }
        [Test] public async Task RefreshFailureKeepsExistingTextButAuthenticationFailureClearsIt()
        {
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")))};var session=SupportInquiryTests.Session();
            using var reader=new StoryArchiveReadModel(wire,()=>session,()=>1);var view=new View();using var controller=new StoryArchiveController(reader,view,()=>{});
            await controller.RefreshAsync();view.Open();Assert.IsTrue(view.State.IsReading);
            wire.Reply=()=>Task.FromResult(RunApiHttpResponse.NetworkError("fixture failure"));await controller.RefreshAsync();
            Assert.IsTrue(view.State.IsReading);Assert.IsFalse(view.State.CanOpen);Assert.IsTrue(view.State.Rows[0].CanOpen);Assert.That(view.State.Status,Is.EqualTo(StoryArchiveReadStatus.Unavailable));
            wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(401,"{}"));await controller.RefreshAsync();Assert.IsFalse(view.State.IsReading);Assert.That(view.State.Rows.All(x=>!x.CanOpen),Is.True);
            var retry=new TaskCompletionSource<RunApiHttpResponse>();wire.Reply=()=>retry.Task;
            var retrying=controller.RefreshAsync();Assert.That(view.State.Rows.All(x=>!x.CanOpen),Is.True,"Retry is not successful authentication.");
            retry.SetResult(RunApiHttpResponse.Success(503,"{}"));await retrying;
            Assert.That(view.State.Rows.All(x=>!x.CanOpen),Is.True,"Unavailable retry must not re-expose retained recovery state.");
        }
    }
}
