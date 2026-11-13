using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Presentation.Settings;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class SettingsScreenControllerTests
    {
        sealed class Backend:ILocalSettingsBackend
        {
            public int Writes;float master=.5f;
            public bool Supports(LocalSetting key)=>key==LocalSetting.MasterVolume;
            public float Read(LocalSetting key)=>master;
            public void ApplyAndSave(LocalSetting key,float value){Writes++;master=value;}
            public Task<DisplaySettingsInfo> ReadDisplayAsync()=>Task.FromResult(new DisplaySettingsInfo(new[]{"device configured"},0,"actual frame",false));
        }
        sealed class View:ISettingsScreenView
        {
            public event Action<int> TabRequested;
            public event Action<LocalSetting,float> ValueRequested;
            public event Action BackRequested,RefreshRequested,AccountLinkRequested;
            public SettingsScreenState State;public int Renders;
            public void Render(SettingsScreenState state){State=state;Renders++;}
            public void Tab(int tab)=>TabRequested?.Invoke(tab);
            public void Value(LocalSetting key,float value)=>ValueRequested?.Invoke(key,value);
            public void Back()=>BackRequested?.Invoke();
            public void Refresh()=>RefreshRequested?.Invoke();
            public void Link()=>AccountLinkRequested?.Invoke();
        }
        [Test] public void FourTabsOneCommandAndOwnedUnbindPreserveBorrowedModel()
        {
            var backend=new Backend();using var model=new LocalSettingsModel(backend);var view=new View();int closes=0,links=0;
            var controller=new SettingsScreenController(model,null,view,()=>closes++,()=>links++);
            for(int i=0;i<4;i++){view.Tab(i);Assert.That((int)view.State.Tab,Is.EqualTo(i));}
            view.Link();Assert.That(links,Is.Zero,"Missing account data cannot enable linking.");
            view.Value(LocalSetting.MasterVolume,.25f);Assert.That(backend.Writes,Is.EqualTo(1));Assert.That(view.State.Value(LocalSetting.MasterVolume),Is.EqualTo(.25f));
            controller.Dispose();int rendered=view.Renders;view.Back();view.Refresh();view.Value(LocalSetting.MasterVolume,.75f);view.Tab(0);
            Assert.That(view.Renders,Is.EqualTo(rendered));Assert.That(closes,Is.Zero);Assert.That(backend.Writes,Is.EqualTo(1));
            Assert.IsTrue(model.Change(LocalSetting.MasterVolume,.75f));Assert.That(backend.Writes,Is.EqualTo(2));
            using var rebound=new SettingsScreenController(model,null,view,()=>closes++,()=>links++);view.Back();Assert.That(closes,Is.EqualTo(1));
        }
    }
}
