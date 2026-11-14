using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LobbyMvcBoundaryTests
    {
        sealed class Wire:IRunApiHttpTransport
        {
            public int Writes;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {if(request.Method!="GET")Writes++;return Task.FromResult(RunApiHttpResponse.Success(200,FormationContractTests.S03Json()));}
        }
        [Test] public async Task FormationInputsStopAtDisposeAndNewControllerUsesSameDraft()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.EnsureAccountAsync();int renders=0;FormationScreenState shown=null;
            var controller=new FormationScreenController(owner,s=>{renders++;shown=s;});controller.Render();controller.SelectOperator("op-a");controller.SelectPosition(2);
            Assert.AreEqual("op-a",shown.Selection[2]);controller.Rename("kept");controller.Dispose();int previous=renders;
            controller.NewPreset();controller.Rename("stale");controller.SelectPosition(1);await controller.ActivateAsync();controller.Render();
            Assert.AreEqual(previous,renders);Assert.AreEqual("kept",owner.Formation.Name);Assert.AreEqual("op-a",owner.Formation.Selection[2]);Assert.AreEqual(0,wire.Writes);
            using var next=new FormationScreenController(owner,s=>shown=s);next.Render();Assert.AreEqual("kept",shown.Name);Assert.AreEqual("op-a",shown.Selection[2]);
        }
        [Test] public async Task InvalidPresetDoesNotOverwriteDraftAndProjectionMarksItInvalid()
        {
            var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(new Wire(),null,()=>session,()=>1);await owner.EnsureAccountAsync();
            owner.Formation.Select(1,"op-a");owner.Formation.Rename("unsaved");string id=owner.Formation.PresetId;
            var invalid=AccountStateSnapshot.Parse(AccountStateReadTests.Json(),"account_test").Presets[0];
            Assert.IsFalse(owner.Formation.Load(invalid));Assert.AreEqual(id,owner.Formation.PresetId);Assert.AreEqual("unsaved",owner.Formation.Name);Assert.AreEqual("op-a",owner.Formation.Selection[1]);Assert.IsTrue(owner.Formation.Dirty);
            var view=new FormationPresetDisplay(invalid,null);Assert.IsFalse(view.Valid);Assert.That(view.Operators,Is.All.Null);
        }
        [Test] public async Task OperatorControllerStopsOldSessionAndDisposedIntents()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();long generation=1;using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>generation);await owner.EnsureAccountAsync();
            int renders=0,dialogs=0;OperatorScreenState shown=null;
            var controller=new OperatorScreenController(owner,s=>{renders++;shown=s;},s=>dialogs++,(a,b,c,d)=>dialogs++);
            controller.Render();Assert.AreEqual("op-a",shown.Id);controller.OpenCore();Assert.AreEqual(1,dialogs);int prior=renders;
            generation++;controller.Render();controller.OpenCore();controller.ReplaceCore(0);controller.SyncCore();await controller.GrowAsync(1);
            Assert.AreEqual(prior,renders);Assert.AreEqual(1,dialogs);Assert.AreEqual(0,wire.Writes);
            controller.Dispose();controller.Select(0);Assert.AreEqual(prior,renders);
        }
        [Test] public async Task OperatorViewReentryReusesControlsAndRetiredInputsCannotAct()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);await owner.EnsureAccountAsync();
            var root=new GameObject("MvcOperators",typeof(RectTransform));
            try{
                var pages=LobbyDestinationPages.Create(root.transform,null,()=>{});pages.BindAccount(owner);pages.Open(LobbyPage.Operators);
                var first=root.GetComponentsInChildren<Button>().Single(b=>b.name=="GrowOperator0");pages.RefreshAccountView(owner);Assert.AreSame(first,root.GetComponentsInChildren<Button>().Single(b=>b.name=="GrowOperator0"));
                var stale=first.onClick;pages.Close();stale.Invoke();pages.Open(LobbyPage.Operators);stale.Invoke();Assert.AreEqual(0,wire.Writes);Assert.IsTrue(pages.IsOpen);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
