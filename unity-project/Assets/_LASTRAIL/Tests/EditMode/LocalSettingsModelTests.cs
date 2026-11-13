using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Presentation.Settings;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class LocalSettingsModelTests
    {
        sealed class Backend:ILocalSettingsBackend
        {
            public int Writes;public bool Fail;
            public readonly Dictionary<LocalSetting,float> Values=new Dictionary<LocalSetting,float>{{LocalSetting.PresentationMode,2},{LocalSetting.MasterVolume,.6f}};
            public TaskCompletionSource<DisplaySettingsInfo> Display=new TaskCompletionSource<DisplaySettingsInfo>();
            public bool Supports(LocalSetting key)=>key==LocalSetting.PresentationMode||key==LocalSetting.MasterVolume;
            public float Read(LocalSetting key)=>Values.TryGetValue(key,out var value)?value:0;
            public void ApplyAndSave(LocalSetting key,float value){Writes++;if(Fail)throw new InvalidOperationException("fixture save failure");Values[key]=value;}
            public Task<DisplaySettingsInfo> ReadDisplayAsync()=>Display.Task;
        }
        [Test] public void UnsupportedAndInvalidInputsNeverSaveOrInventSuccess()
        {
            var backend=new Backend();using var model=new LocalSettingsModel(backend);
            Assert.IsFalse(model.Change(LocalSetting.VoiceVolume,.5f));Assert.IsFalse(model.Change(LocalSetting.PresentationMode,3));Assert.IsFalse(model.Change(LocalSetting.MasterVolume,float.NaN));Assert.IsFalse(model.Change(LocalSetting.MasterVolume,2));
            Assert.That(backend.Writes,Is.Zero);Assert.That(model.SaveStatus,Is.Not.EqualTo(SettingsSaveStatus.Saved));
            Assert.IsTrue(model.Change(LocalSetting.MasterVolume,.4f));Assert.That(backend.Writes,Is.EqualTo(1));Assert.That(model.Value(LocalSetting.MasterVolume),Is.EqualTo(.4f));Assert.That(model.SaveStatus,Is.EqualTo(SettingsSaveStatus.Saved));
            Assert.IsTrue(model.Change(LocalSetting.MasterVolume,.4f));Assert.That(backend.Writes,Is.EqualTo(1),"Unchanged values are not saved twice.");
        }
        [Test] public void SaveFailureRetainsObservedValueAndReportsFailure()
        {
            var backend=new Backend{Fail=true};using var model=new LocalSettingsModel(backend);
            Assert.IsFalse(model.Change(LocalSetting.PresentationMode,1));Assert.That(model.Value(LocalSetting.PresentationMode),Is.EqualTo(2));Assert.That(model.SaveStatus,Is.EqualTo(SettingsSaveStatus.Failed));
        }
        [Test] public async Task DisplayReadShowsLoadingAndNeverGuessesOptions()
        {
            var backend=new Backend();using var model=new LocalSettingsModel(backend);var read=model.RefreshDisplayAsync();
            Assert.That(model.DisplayStatus,Is.EqualTo(SettingsReadStatus.Loading));Assert.IsNull(model.Display);
            backend.Display.SetResult(new DisplaySettingsInfo(new[]{"actual configured quality"},0,"현재 프레임 설정",false));await read;
            Assert.That(model.DisplayStatus,Is.EqualTo(SettingsReadStatus.Ready));Assert.That(model.Display.QualityNames,Is.EqualTo(new[]{"actual configured quality"}));
        }
        [Test] public async Task DisposedDisplayReadCannotNotifyOrRestoreState()
        {
            var backend=new Backend();var model=new LocalSettingsModel(backend);int changes=0;model.Changed+=()=>changes++;var read=model.RefreshDisplayAsync();model.Dispose();int before=changes;
            backend.Display.SetResult(new DisplaySettingsInfo(new[]{"fixture"},0,"fixture",false));await read;Assert.That(changes,Is.EqualTo(before));Assert.IsNull(model.Display);
        }
    }
}
