using System.Threading.Tasks;
using LastRail.Presentation.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class UnitySettingsBackendTests
    {
        [Test] public async Task MasterVolumeAndPresentationPersistThroughTheRealUnityBoundary()
        {
            bool hadMaster = PlayerPrefs.HasKey(UnitySettingsBackend.MasterKey), hadMode = PlayerPrefs.HasKey(UnitySettingsBackend.ModeKey);
            float master = PlayerPrefs.GetFloat(UnitySettingsBackend.MasterKey), listener = AudioListener.volume;
            int mode = PlayerPrefs.GetInt(UnitySettingsBackend.ModeKey);
            try
            {
                var backend = new UnitySettingsBackend(null);
                backend.ApplyAndSave(LocalSetting.MasterVolume, .37f);
                Assert.That(AudioListener.volume, Is.EqualTo(.37f).Within(.001));
                Assert.That(new UnitySettingsBackend(null).Read(LocalSetting.MasterVolume), Is.EqualTo(.37f).Within(.001));
                backend.ApplyAndSave(LocalSetting.PresentationMode, 1);
                Assert.That(new UnitySettingsBackend(null).Read(LocalSetting.PresentationMode), Is.EqualTo(1));
                var display = await backend.ReadDisplayAsync();
                Assert.That(display.QualityNames, Is.EqualTo(QualitySettings.names));
                Assert.That(display.CurrentQuality, Is.EqualTo(QualitySettings.GetQualityLevel()));
                Assert.IsTrue(backend.Supports(LocalSetting.MusicVolume));
                Assert.IsTrue(backend.Supports(LocalSetting.EffectsVolume));
                Assert.IsTrue(backend.Supports(LocalSetting.ReducedPresentation));
                Assert.IsFalse(backend.Supports(LocalSetting.Shake));
            }
            finally
            {
                if (hadMaster) PlayerPrefs.SetFloat(UnitySettingsBackend.MasterKey, master); else PlayerPrefs.DeleteKey(UnitySettingsBackend.MasterKey);
                if (hadMode) PlayerPrefs.SetInt(UnitySettingsBackend.ModeKey, mode); else PlayerPrefs.DeleteKey(UnitySettingsBackend.ModeKey);
                PlayerPrefs.Save(); AudioListener.volume = listener;
            }
        }
    }
}
