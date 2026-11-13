using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Presentation.Settings;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class SettingsScreenNavigationTests
    {
        sealed class Backend:ILocalSettingsBackend
        {
            public int Writes;
            readonly Dictionary<LocalSetting,float> values=new Dictionary<LocalSetting,float>{{LocalSetting.MasterVolume,.5f},{LocalSetting.PresentationMode,2},{LocalSetting.Quality,0}};
            public bool Supports(LocalSetting key)=>values.ContainsKey(key);
            public float Read(LocalSetting key)=>values[key];
            public void ApplyAndSave(LocalSetting key,float value){Writes++;values[key]=value;}
            public Task<DisplaySettingsInfo> ReadDisplayAsync()=>Task.FromResult(new DisplaySettingsInfo(new[]{"QA actual option A","QA actual option B"},0,"QA frame",false));
        }
        [Test] public void NativeTabsSliderButtonsQualityAndReturnDoNotDuplicateCommands()
        {
            var backend=new Backend();using var settings=new LocalSettingsModel(backend);
            var root=new GameObject("SettingsNavigation",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1600);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindSettings(settings);
                void Click(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name).onClick.Invoke();
                Click("LobbyMenu");Click("OpenSettings");var view=root.GetComponentInChildren<SettingsScreenView>();Assert.IsNotNull(view);
                Assert.That(backend.Writes,Is.Zero,"Opening settings must not save values");
                Assert.IsTrue(view.GetComponentsInChildren<Button>().Single(x=>x.name=="PresentationMode0").GetComponent<Image>().raycastTarget,"Real pointer input needs an enabled graphic target.");
                Click("SettingsTab1");Assert.That(backend.Writes,Is.Zero,"Changing tabs must not save values");var slider=view.GetComponentsInChildren<Slider>().Single(x=>x.name=="MasterVolumeSlider");slider.value=.35f;Assert.That(backend.Writes,Is.EqualTo(1));
                Click("MasterVolumePlus");Assert.That(backend.Writes,Is.EqualTo(2));view.Render(view.CurrentState);view.Render(view.CurrentState);Assert.That(backend.Writes,Is.EqualTo(2));
                view.GetComponentsInChildren<Slider>().Single(x=>x.name=="VoiceVolumeSlider").value=.4f;Assert.That(backend.Writes,Is.EqualTo(2));
                Click("SettingsTab2");Click("QualityChoice");Click("QualityOption1");Assert.That(settings.Value(LocalSetting.Quality),Is.EqualTo(1));Assert.That(backend.Writes,Is.EqualTo(3));
                Click("SettingsTab3");Assert.IsFalse(view.CurrentState.AccountReady);Assert.IsFalse(view.GetComponentsInChildren<Button>().Single(x=>x.name=="SettingsAccountLink").interactable);
                var retainedPlus=view.GetComponentsInChildren<Button>(true).Single(x=>x.name=="MasterVolumePlus");
                view.Unbind();retainedPlus.onClick.Invoke();Assert.That(backend.Writes,Is.EqualTo(3),"Unbound view cannot send a command.");
                deck.CloseDialog();Click("LobbyMenu");Click("OpenSettings");Click("SettingsReturn");Assert.IsNull(root.GetComponentInChildren<SettingsScreenView>());Assert.IsNotNull(root.GetComponentsInChildren<Button>().Single(x=>x.name=="OpenSettings"));
                Click("OpenSettings");using var replacement=new LocalSettingsModel(new Backend());deck.BindSettings(replacement);Assert.IsNull(root.GetComponentInChildren<SettingsScreenView>());
                Assert.IsTrue(settings.Change(LocalSetting.MasterVolume,.6f),"Deck does not dispose the borrowed model.");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
