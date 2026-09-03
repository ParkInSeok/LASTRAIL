#if UNITY_5_3_OR_NEWER
using System.Text;
using LastRail.Presentation.Audio;
using LastRail.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Battle
{
    public sealed class FullyAnimatedCombatHudRootBehaviour : MonoBehaviour, IFullyAnimatedCombatHudView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private RectTransform exteriorTrainAdvanceLane;
        [SerializeField] private RectTransform interiorBoardingCombatLane;
        [SerializeField] private RectTransform playerSquadSlots;
        [SerializeField] private RectTransform invadingEnemySlots;
        [SerializeField] private RectTransform engagementZoneRoot;
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
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
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

            if (state.EncounterType == RunEncounterType.RaidBoss)
            {
                return "보스전 분대 " + state.OperatorSlotCount + " / 보스 1";
            }

            if (ContainsOperatorIds(state, "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris"))
            {
                return "정예 분대 A " + state.OperatorSlotCount + " / 침입 적 " + state.EnemyUnitCount;
            }

            if (ContainsOperatorIds(state, "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden"))
            {
                return "정예 분대 B " + state.OperatorSlotCount + " / 침입 적 " + state.EnemyUnitCount;
            }

            return state.EncounterType == RunEncounterType.RaidBoss
                ? "보스전 분대 " + state.OperatorSlotCount + " / 보스 1"
                : "초기 분대 " + state.OperatorSlotCount + " / 침입 적 " + state.EnemyUnitCount;
        }

        private static bool ContainsOperatorIds(FullyAnimatedCombatHudViewState state, params string[] ids)
        {
            foreach (var id in ids)
            {
                var found = false;
                foreach (var unit in state.UnitAnimationRefs)
                {
                    if (unit.UnitRole == CombatUnitRole.Operator && unit.UnitId == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static string FormatCueRefs(FullyAnimatedCombatHudViewState state)
        {
            var builder = new StringBuilder();
            builder.Append("피격 / 히트스톱 / VFX / SFX / 진동 준비");
            if (state.WaveTransitionCueId != null)
            {
                builder.Append(" / 공세 전환");
            }

            if (state.BossPhaseTransitionCueId != null)
            {
                builder.Append(" / 보스 페이즈");
            }

            return builder.ToString();
        }
    }
}
#endif
