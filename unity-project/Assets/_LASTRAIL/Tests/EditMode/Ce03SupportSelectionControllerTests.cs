using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class Ce03SupportSelectionControllerTests
{
    sealed class View : ICe03SupportSelectionView
    {
        public event Action<string> ToggleRequested;
        public event Action<int> RemoveRequested, MoveLeftRequested;
        public event Action SaveRequested, BackRequested;
        public Ce03SupportSelectionState State;
        public int Renders;
        public void Render(Ce03SupportSelectionState state) { State=state; Renders++; }
        public void Toggle(string id)=>ToggleRequested?.Invoke(id);
        public void Remove(int index)=>RemoveRequested?.Invoke(index);
        public void Move(int index)=>MoveLeftRequested?.Invoke(index);
        public void Save()=>SaveRequested?.Invoke();
        public void Back()=>BackRequested?.Invoke();
    }
    sealed class Wire : IRunApiHttpTransport
    {
        public static readonly string[] Ids={"support_fire","barrier","emergency_repair","ce03.role.suppressive_barrage","ce03.role.medical_support","ce03.role.smoke","ce03.role.weakness_scan","ce03.role.firepower_boost","ce03.role.electronic_disruption","ce03.role.emergency_protection"};
        public int Reads,Saves; public bool FailSave,MalformedRead; public string FirstBody,LastBody;
        public TaskCompletionSource<bool> ReadGate,SaveGate;
        long revision; JArray slots=new JArray();
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method=="GET") { Reads++; if(ReadGate!=null)await ReadGate.Task; if(MalformedRead)return Reply(new JObject());return Reply(new JObject{["selection"]=Selection(),["catalog"]=new JObject{["scope"]="LOCAL_SELECTION_ONLY",["entries"]=new JArray(Ids.Select((id,i)=>new JObject{["skillId"]=id,["executionAvailable"]=i<3}))}}); }
            Saves++; LastBody=request.BodyJson; if(FirstBody==null)FirstBody=LastBody;
            if(SaveGate!=null)await SaveGate.Task;
            if(FailSave)throw new InvalidOperationException("Lost response");
            slots=(JArray)JObject.Parse(LastBody)["slots"].DeepClone(); revision++; return Reply(Selection());
        }
        JObject Selection()=>new JObject{["rulesVersion"]="ce03-support-selection-v1",["catalogVersion"]="ce03-local-support-roles-v1",["revision"]=revision,["slots"]=slots.DeepClone()};
        static RunApiHttpResponse Reply(JObject body)=>RunApiHttpResponse.Success(200,body.ToString());
    }
    static Ce03LocalClient Client(Wire wire)=>new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"mvc",wire,new GameApiAccessToken("lst1.test"),_=>{});
    [Test] public async Task SelectionLimitOrderAndPendingRetryUseOriginalCommand()
    {
        var wire=new Wire(); var view=new View(); using(var controller=new Ce03SupportSelectionController(Client(wire),view,()=>{})) {
            await controller.ActivateAsync(); foreach(var id in Wire.Ids.Take(4))view.Toggle(id);
            Assert.AreEqual(3,view.State.Selected.Count); view.Move(2);view.Remove(0);Assert.IsFalse(view.State.CanSave);view.Toggle(Wire.Ids[0]);
            wire.FailSave=true;await controller.SaveAsync();Assert.IsTrue(view.State.CanSave);Assert.IsFalse(view.State.CanEdit);
            view.Toggle(Wire.Ids[4]);wire.FailSave=false;await controller.SaveAsync();Assert.AreEqual(wire.FirstBody,wire.LastBody);
            CollectionAssert.AreEqual(new[]{Wire.Ids[2],Wire.Ids[1],Wire.Ids[0]},view.State.Selected);Assert.AreEqual(1,wire.Reads);
        }
    }
    [Test] public async Task DisabledReadAndDisposedCallbacksCannotRenderOrDispatch()
    {
        var wire=new Wire{ReadGate=new TaskCompletionSource<bool>()};var view=new View();int backs=0;
        var controller=new Ce03SupportSelectionController(Client(wire),view,()=>backs++);var read=controller.ActivateAsync();controller.Deactivate();int renders=view.Renders;
        wire.ReadGate.SetResult(true);await read;Assert.AreEqual(renders,view.Renders);
        await controller.ActivateAsync();view.Back();Assert.AreEqual(1,backs);controller.Dispose();controller.Dispose();renders=view.Renders;
        view.Toggle(Wire.Ids[0]);view.Remove(0);view.Move(1);view.Save();view.Back();await controller.ActivateAsync();
        Assert.AreEqual(renders,view.Renders);Assert.AreEqual(1,backs);Assert.AreEqual(0,wire.Saves);
    }
    [Test] public async Task ReentryWaitsForSaveAndBindsEachIntentOnce()
    {
        var wire=new Wire{SaveGate=new TaskCompletionSource<bool>()};var view=new View();int backs=0;
        using(var controller=new Ce03SupportSelectionController(Client(wire),view,()=>backs++)) {
            await controller.ActivateAsync();foreach(var id in Wire.Ids.Take(3))view.Toggle(id);var save=controller.SaveAsync();view.Back();Assert.AreEqual(0,backs);
            controller.Deactivate();var reentry=controller.ActivateAsync();await controller.ActivateAsync();Assert.AreEqual(1,wire.Reads);
            wire.SaveGate.SetResult(true);await save;await reentry;Assert.AreEqual(2,wire.Reads);
            view.Toggle(Wire.Ids[0]);Assert.AreEqual(2,view.State.Selected.Count);view.Toggle(Wire.Ids[0]);view.Save();Assert.AreEqual(2,wire.Saves);
            Assert.AreEqual(1,(long)JObject.Parse(wire.LastBody)["expectedRevision"]);
        }
    }
    [Test] public async Task DisposeDuringSaveSuppressesViewButPreservesClientReceipt()
    {
        var wire=new Wire{SaveGate=new TaskCompletionSource<bool>()};var client=Client(wire);var view=new View();
        var controller=new Ce03SupportSelectionController(client,view,()=>{});await controller.ActivateAsync();
        foreach(var id in Wire.Ids.Take(3))view.Toggle(id);var save=controller.SaveAsync();Assert.IsTrue(client.HasPending);
        controller.Dispose();int renders=view.Renders;wire.SaveGate.SetResult(true);await save;
        Assert.AreEqual(renders,view.Renders);Assert.IsFalse(client.HasPending);Assert.AreEqual(1,client.KnownSupportRevision);
    }
    [Test] public async Task InvalidReadReportsFailureWithoutPartiallyReplacingSelection()
    {
        var wire=new Wire();var view=new View();using(var controller=new Ce03SupportSelectionController(Client(wire),view,()=>{})) {
            await controller.ActivateAsync();view.Toggle(Wire.Ids[0]);var previous=view.State;controller.Deactivate();wire.MalformedRead=true;
            await controller.ActivateAsync();CollectionAssert.AreEqual(previous.Selected,view.State.Selected);
            Assert.AreEqual(10,view.State.Entries.Count);StringAssert.Contains("불러오지 못했습니다",view.State.Notice);Assert.IsFalse(view.State.CanSave);
        }
    }
}
