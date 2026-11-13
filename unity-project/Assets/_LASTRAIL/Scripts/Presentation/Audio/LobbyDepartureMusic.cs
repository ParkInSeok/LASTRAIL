using System;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.Settings;
using UnityEngine;

namespace LastRail.Presentation.Audio
{
    public sealed class LobbyDepartureMusic : IDisposable
    {
        readonly LastRailAudioCuePlayer player;
        readonly LastRailAudioClipBinding lobby, battle;
        CombatSceneDriver watched;

        public LobbyDepartureMusic(Transform lifetimeParent, AudioClip battleClip)
        {
            if (lifetimeParent == null) throw new ArgumentNullException(nameof(lifetimeParent));
            var master = LastRail.Resources.PreparedContent.Load<AudioClip>("CombatPresentation/Audio/LobbyDepartureV01");
            if (master == null) throw new InvalidOperationException("Lobby departure master missing.");
            lobby = new LastRailAudioClipBinding("lobby-departure-v01", master, true, 1);
            battle = new LastRailAudioClipBinding(LaunchBgmLoopPlaybackPolicy.LaunchBgmLoopCueId, battleClip, true, 1);
            var host = new GameObject("LobbyDepartureMusic");
            host.transform.SetParent(lifetimeParent, false);
            if (!UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Any(listener => listener.isActiveAndEnabled))
                host.AddComponent<AudioListener>();
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            player = host.AddComponent<LastRailAudioCuePlayer>();
            player.ConfigureBindings(null, source, new[] { lobby, battle });
            player.ApplyMixSettings(UnitySettingsBackend.SavedMusic, UnitySettingsBackend.SavedEffects);
        }

        public void EnterLobby()
        {
            Unwatch();
            player.PlayBoundMusicLoop(lobby);
        }

        public void WatchCombat(CombatSceneDriver driver)
        {
            Unwatch();
            if (driver == null || driver.IsRetired) return;
            watched = driver;
            watched.OrdinaryExecutionBegan += OnExecution;
            if (watched.HasBegunExecution) OnExecution();
        }

        void OnExecution()
        {
            Unwatch();
            if (battle.Clip == null) { Debug.LogWarning("Ordinary battle music binding missing."); return; }
            player.PlayBoundMusicLoop(battle);
        }

        void Unwatch()
        {
            if (watched != null) watched.OrdinaryExecutionBegan -= OnExecution;
            watched = null;
        }

        public void Dispose()
        {
            Unwatch();
            if (player == null) return;
            player.gameObject.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(player.gameObject);
            else UnityEngine.Object.DestroyImmediate(player.gameObject);
        }
    }
}
