using System;
using System.Collections.Generic;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Presentation
{
    public sealed class UIScreenBindingReadinessAuditReport
    {
        private UIScreenBindingReadinessAuditReport(IEnumerable<UIScreenBindingReadinessAudit> audits)
        {
            ScreenAudits = CopyAudits(audits);
            EditorVerificationChecklist = BuildChecklist(ScreenAudits);
        }

        public IReadOnlyList<UIScreenBindingReadinessAudit> ScreenAudits { get; }
        public IReadOnlyList<string> EditorVerificationChecklist { get; }
        public bool IsReadyForEditorBindingPass => AllAuditsPass();
        public bool RequiresEditorVisualVerification => true;
        public bool HasSceneOrPrefabAsset => false;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static UIScreenBindingReadinessAuditReport ForLaunchScreens(
            MissionShellScreenViewState missionShell,
            GachaScreenViewState gachaScreen,
            FullyAnimatedCombatHudViewState combatHud)
        {
            return new UIScreenBindingReadinessAuditReport(new[]
            {
                UIScreenBindingReadinessAudit.ForMissionShell(missionShell),
                UIScreenBindingReadinessAudit.ForGachaScreen(gachaScreen),
                UIScreenBindingReadinessAudit.ForCombatHud(combatHud)
            });
        }

        private bool AllAuditsPass()
        {
            foreach (var audit in ScreenAudits)
            {
                if (!audit.HasRequiredBindings ||
                    !audit.UsesPortraitSafeArea ||
                    audit.MinimumTouchTargetDp < 48 ||
                    !audit.RequiresOverflowVerification)
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<UIScreenBindingReadinessAudit> CopyAudits(IEnumerable<UIScreenBindingReadinessAudit> audits)
        {
            if (audits == null)
            {
                throw new ArgumentNullException(nameof(audits));
            }

            var copied = new List<UIScreenBindingReadinessAudit>();
            foreach (var audit in audits)
            {
                if (audit == null)
                {
                    throw new ArgumentException("Audit cannot be null.", nameof(audits));
                }

                copied.Add(audit);
            }

            return copied.AsReadOnly();
        }

        private static IReadOnlyList<string> BuildChecklist(IEnumerable<UIScreenBindingReadinessAudit> audits)
        {
            var checklist = new List<string>();
            foreach (var audit in audits)
            {
                foreach (var item in audit.EditorVerificationChecklist)
                {
                    if (!Contains(checklist, item))
                    {
                        checklist.Add(item);
                    }
                }
            }

            return checklist.AsReadOnly();
        }

        private static bool Contains(IEnumerable<string> values, string target)
        {
            foreach (var value in values)
            {
                if (string.Equals(value, target, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
