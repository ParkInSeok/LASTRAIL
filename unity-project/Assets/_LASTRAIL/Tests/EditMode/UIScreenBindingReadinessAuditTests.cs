using System;
using NUnit.Framework;
using LastRail.Gacha;
using LastRail.Presentation;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Tests.EditMode
{
    public sealed class UIScreenBindingReadinessAuditTests
    {
        [Test]
        public void AuditMissionShellGachaAndCombatScreens_UsesSharedSafeAreaTouchAndOverflowReadiness()
        {
            var mission = UIScreenBindingReadinessAudit.ForMissionShell(MissionShellState());
            var gacha = UIScreenBindingReadinessAudit.ForGachaScreen(GachaState());
            var combat = UIScreenBindingReadinessAudit.ForCombatHud(CombatState());

            Assert.That(mission.ScreenKey, Is.EqualTo("mission_shell"));
            Assert.That(gacha.ScreenKey, Is.EqualTo("gacha_screen"));
            Assert.That(combat.ScreenKey, Is.EqualTo("fully_animated_combat_hud"));

            Assert.That(mission.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(gacha.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(combat.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(mission.PreferredAspectLabel, Is.EqualTo("9:16"));
            Assert.That(gacha.PreferredAspectLabel, Is.EqualTo("9:16"));
            Assert.That(combat.PreferredAspectLabel, Is.EqualTo("9:16"));
            Assert.That(mission.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(gacha.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(combat.MinimumTouchTargetDp, Is.EqualTo(48));

            Assert.That(mission.RequiresOverflowVerification, Is.EqualTo(true));
            Assert.That(gacha.RequiresOverflowVerification, Is.EqualTo(true));
            Assert.That(combat.RequiresOverflowVerification, Is.EqualTo(true));
            Assert.That(mission.OverflowRiskKey, Is.EqualTo("mission_briefing_text"));
            Assert.That(gacha.OverflowRiskKey, Is.EqualTo("gacha_ten_pull_result_grid"));
            Assert.That(combat.OverflowRiskKey, Is.EqualTo("combat_boss_wave_labels"));
            Assert.That(combat.KeepsHudAboveAnimationLayer, Is.EqualTo(true));

            Assert.That(mission.HasRequiredBindings, Is.EqualTo(true));
            Assert.That(gacha.HasRequiredBindings, Is.EqualTo(true));
            Assert.That(combat.HasRequiredBindings, Is.EqualTo(true));
            Assert.That(mission.RequiredBindingKeys.Count, Is.EqualTo(8));
            Assert.That(gacha.RequiredBindingKeys.Count, Is.EqualTo(14));
            Assert.That(combat.RequiredBindingKeys.Count, Is.EqualTo(8));
        }

        [Test]
        public void ToDebugJson_WritesStableBindingAuditFieldsWithoutTruthTransportOrRawPayload()
        {
            var json = UIScreenBindingReadinessAuditJsonAdapter.ToJson(UIScreenBindingReadinessAudit.ForCombatHud(CombatState()));

            Assert.That(json.Contains("\"screenKey\":\"fully_animated_combat_hud\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"preferredAspectLabel\":\"9:16\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"minimumTouchTargetDp\":48"), Is.EqualTo(true));
            Assert.That(json.Contains("\"overflowRiskKey\":\"combat_boss_wave_labels\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRequiredBindings\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"keepsHudAboveAnimationLayer\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresEditorVisualVerification\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"editorVerificationChecklist\""), Is.EqualTo(true));
            Assert.That(json.Contains("safe area anchors cover portrait notch/home indicator"), Is.EqualTo(true));
            Assert.That(json.Contains("10-pull result grid wraps or scrolls without covering actions"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("economyGrant"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("GoogleSignIn"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
        }

        [Test]
        public void CombinedReport_KeepsScreenSpecificOverflowChecklistWithoutCreatingSceneOrPrefabTruth()
        {
            var report = UIScreenBindingReadinessAuditReport.ForLaunchScreens(
                MissionShellState(),
                GachaState(),
                CombatState());

            Assert.That(report.ScreenAudits.Count, Is.EqualTo(3));
            Assert.That(report.IsReadyForEditorBindingPass, Is.EqualTo(true));
            Assert.That(report.RequiresEditorVisualVerification, Is.EqualTo(true));
            Assert.That(report.HasSceneOrPrefabAsset, Is.EqualTo(false));
            Assert.That(report.HasServerTransport, Is.EqualTo(false));
            Assert.That(report.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(report.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(report.HasClientSideRng, Is.EqualTo(false));
            Assert.That(report.EditorVerificationChecklist[0], Is.EqualTo("safe area anchors cover portrait notch/home indicator"));
            Assert.That(report.EditorVerificationChecklist[1], Is.EqualTo("mission briefing text wraps without covering primary action"));
            Assert.That(report.EditorVerificationChecklist[2], Is.EqualTo("10-pull result grid wraps or scrolls without covering actions"));
            Assert.That(report.EditorVerificationChecklist[3], Is.EqualTo("combat boss/wave labels remain readable over animated combat"));
        }

        private static MissionShellScreenViewState MissionShellState()
        {
            return new MissionShellScreenViewState(
                MissionShellSurface.Briefing,
                "마지막 출발",
                "Act 1",
                new[] { "역이 무너지고 있습니다.", "조금 더 버티면 더 태울 수 있습니다." },
                new[] { "narrative.opn_01_last_departure.briefing.line_001" },
                "마지막 출발 시작",
                "narrative.opn_01_last_departure.mission_start.line_001",
                true,
                null,
                MissionShellSurface.LoadingHint,
                Array.Empty<MissionShellMissionRowState>(),
                null,
                null,
                false,
                false,
                false);
        }

        private static GachaScreenViewState GachaState()
        {
            var screen = GachaScreenPresentationState.Idle(
                "banner_act1_launch",
                "Act 1 구조 신호",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPityDisplayState("banner_act1_launch", 29, 29, 59, 9));

            return new GachaScreenViewState(screen);
        }

        private static FullyAnimatedCombatHudViewState CombatState()
        {
            var combat = FullyAnimatedCombatPresentationState.CreateRaidBoss(
                "raid_raider_king_engine",
                2,
                "boss_crown_engine",
                "aug_pkg_raid_001",
                "tactic_pkg_raid_001",
                new[]
                {
                    new CombatUnitAnimationState("op_vex", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                    new CombatUnitAnimationState("op_rin", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                    new CombatUnitAnimationState("op_nyx", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                    new CombatUnitAnimationState("op_cipher", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                    new CombatUnitAnimationState("op_iris", CombatUnitRole.Operator, "idle_loop", "operator_idle"),
                    new CombatUnitAnimationState("boss_crown_engine", CombatUnitRole.RaidBoss, "boss_phase_attack", "boss_attack_cue")
                },
                CombatPresentationCueSet.ForRaidBossPhase(
                    "damage_number_pop",
                    "hit_flash",
                    "hit_stop_heavy",
                    "screen_shake_heavy",
                    "vfx_boss_phase",
                    "sfx_boss_phase",
                    "vibration_heavy",
                    "boss_phase_transition"));

            return FullyAnimatedCombatHudViewState.FromCombatState(
                combat,
                MobileCombatHudPresentationPolicy.FromCombatState(
                    combat,
                    MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48)));
        }
    }
}
