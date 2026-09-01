#if UNITY_5_3_OR_NEWER
using System.Text;
using LastRail.Presentation.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Battle
{
    public sealed class FullyAnimatedCombatHudRootBehaviour : MonoBehaviour, IFullyAnimatedCombatHudView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private TMP_Text encounterTitleText;
        [SerializeField] private TMP_Text waveCounterText;
        [SerializeField] private TMP_Text bossStatusText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text refundHintText;
        [SerializeField] private TMP_Text unitRefsText;
        [SerializeField] private TMP_Text cueRefsText;
        [SerializeField] private LastRailAudioCuePlayer audioCuePlayer;

        public FullyAnimatedCombatHudViewState CurrentState { get; private set; }

        public void Render(FullyAnimatedCombatHudViewState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            CurrentState = state;
            ApplySafeArea();
            SetText(encounterTitleText, state.EncounterTitle);
            SetText(waveCounterText, state.WaveCounterLabel);
            SetText(bossStatusText, state.BossStatusPlaceholder);
            SetText(objectiveText, state.BossObjectiveLabel);
            SetText(refundHintText, state.FailureStaminaRefundHint);
            SetText(unitRefsText, FormatUnitRefs(state));
            SetText(cueRefsText, FormatCueRefs(state));
        }

        public void PlayHitCue()
        {
            audioCuePlayer?.TryPlayCue(CurrentState != null ? CurrentState.SfxCueId : "combat_hit");
        }

        public void PlaySkillCue()
        {
            audioCuePlayer?.TryPlayCue("combat_skill");
        }

        public void PlayUltimateCue()
        {
            audioCuePlayer?.TryPlayCue("combat_ultimate");
        }

        public void PlayBossPhaseCue()
        {
            audioCuePlayer?.TryPlayCue("story_raid_warning");
        }

        public void PlayBossDefeatCue()
        {
            audioCuePlayer?.TryPlayCue("boss_defeat");
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var min = safeArea.position;
            var max = safeArea.position + safeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            safeAreaRoot.anchorMin = min;
            safeAreaRoot.anchorMax = max;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static string FormatUnitRefs(FullyAnimatedCombatHudViewState state)
        {
            if (state.UnitAnimationRefs.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < state.UnitAnimationRefs.Count; i++)
            {
                var unitRef = state.UnitAnimationRefs[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(unitRef.UnitRole).Append(" / ");
                builder.Append(unitRef.UnitId).Append(" / ");
                builder.Append(unitRef.AnimationKey).Append(" / ");
                builder.Append(unitRef.AnimationCueId);
            }

            return builder.ToString();
        }

        private static string FormatCueRefs(FullyAnimatedCombatHudViewState state)
        {
            var builder = new StringBuilder();
            builder.Append("damage=").Append(state.DamageCueId);
            builder.Append('\n').Append("hitStop=").Append(state.HitStopCueId);
            builder.Append('\n').Append("vfx=").Append(state.VfxCueId);
            builder.Append('\n').Append("sfx=").Append(state.SfxCueId);
            builder.Append('\n').Append("vibration=").Append(state.VibrationCueId);
            if (state.WaveTransitionCueId != null)
            {
                builder.Append('\n').Append("wave=").Append(state.WaveTransitionCueId);
            }

            if (state.BossPhaseTransitionCueId != null)
            {
                builder.Append('\n').Append("bossPhase=").Append(state.BossPhaseTransitionCueId);
            }

            return builder.ToString();
        }
    }
}
#endif
