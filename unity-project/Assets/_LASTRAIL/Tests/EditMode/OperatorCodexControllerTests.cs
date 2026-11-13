using System;
using System.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class OperatorCodexControllerTests
    {
        sealed class View:IOperatorCodexView
        {
            public event Action<int> SelectionRequested,TabRequested;
            public event Action BackRequested;
            public OperatorCodexViewState State;public int Renders;
            public void Render(OperatorCodexViewState state){State=state;Renders++;}
            public void Select(int value)=>SelectionRequested?.Invoke(value);
            public void Tab(int value)=>TabRequested?.Invoke(value);
            public void Back()=>BackRequested?.Invoke();
        }
        [Test] public void ActualCatalogProvidesIdentityAndNamesWithoutBiographiesOrDiscoveryClaims()
        {
            var entries=OperatorCodexCatalog.Load();Assert.That(entries.Count,Is.EqualTo(15));
            var kyle=entries.Single(x=>x.Id=="op_kyle");Assert.That(kyle.Name,Is.EqualTo("카일"));Assert.That(kyle.Active,Is.EqualTo("전선 방호"));Assert.That(kyle.Ultimate,Is.EqualTo("최후의 방벽"));
            Assert.That(kyle.ActiveDescription,Is.EqualTo("보호막과 도발"));Assert.That(kyle.UltimateDescription,Is.EqualTo("보호막"));
            Assert.That(entries.Where(x=>x.Id!="op_kyle").All(x=>x.ActiveDescription==OperatorCodexEntry.Unknown&&x.UltimateDescription==OperatorCodexEntry.Unknown),Is.True);
        }
        [Test] public void TabsListReturnAndRebindInvokeOnlyCurrentOwner()
        {
            var view=new View();int exits=0;var controller=new OperatorCodexController(OperatorCodexCatalog.Load(),view,()=>exits++);
            Assert.That(view.State.Tab,Is.EqualTo(OperatorCodexTab.Skills));view.Tab(2);Assert.That(view.State.Tab,Is.EqualTo(OperatorCodexTab.Story));Assert.That(view.State.Body,Is.EqualTo(OperatorCodexEntry.Unknown));
            view.Back();Assert.IsTrue(view.State.IsList);view.Select(1);Assert.IsFalse(view.State.IsList);Assert.That(view.State.Selected.Id,Is.EqualTo("op_reina"));
            view.Tab(0);Assert.That(view.State.Body,Does.Not.Contain("보유"));view.Tab(99);Assert.That(view.State.Tab,Is.EqualTo(OperatorCodexTab.Basic));
            view.Back();view.Back();Assert.That(exits,Is.EqualTo(1));controller.Dispose();int before=view.Renders;
            view.Tab(1);view.Select(0);view.Back();Assert.That(exits,Is.EqualTo(1));Assert.That(view.Renders,Is.EqualTo(before));
            using var replacement=new OperatorCodexController(OperatorCodexCatalog.Load(),view,()=>exits++);view.Back();view.Back();Assert.That(exits,Is.EqualTo(2));
        }
    }
}
