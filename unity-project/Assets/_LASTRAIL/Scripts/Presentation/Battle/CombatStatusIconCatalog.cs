using System.Collections.Generic;

namespace LastRail.Presentation.Battle
{
    public static class CombatStatusIconCatalog
    {
        private const string Root = "Assets/_LASTRAIL/UI/CombatResources/";
        public static IReadOnlyList<string> IconPaths { get; } = System.Array.AsReadOnly(new[]
        {
            Root + "VFX/vfx_shield_status.png",
            Root + "VFX/vfx_heal_status.png",
            Root + "StatusIcons/status_buff.png",
            Root + "StatusIcons/status_debuff.png",
            Root + "StatusIcons/status_cooldown.png",
            Root + "StatusIcons/status_available.png",
            Root + "StatusIcons/status_unavailable.png",
            Root + "StatusIcons/status_selected_target.png",
            "Assets/_LASTRAIL/UI/Common/Skins/ui_slider_track_9slice.png",
            "Assets/_LASTRAIL/UI/Common/Skins/ui_slider_fill_9slice.png"
        });

        public static IReadOnlyList<string> ResolveCueIds(string vfxCueId, string sfxCueId)
        {
            var ids = new List<string>();
            // These are presentation events, not persistent effects or per-unit status.
            if (vfxCueId == "vfx_shield_status") ids.Add("shield");
            if (vfxCueId == "vfx_heal_status") ids.Add("heal");
            string audioId = null;
            switch (sfxCueId)
            {
                case "combat_shield": case "sfx_combat_shield": audioId = "shield"; break;
                case "combat_heal": case "sfx_combat_heal": audioId = "heal"; break;
                case "combat_buff": case "sfx_combat_buff": audioId = "buff"; break;
                case "combat_debuff": case "sfx_combat_debuff": audioId = "debuff"; break;
            }
            if (audioId != null && !ids.Contains(audioId)) ids.Add(audioId);
            return ids.AsReadOnly();
        }

        public static int CueIconIndex(string id)
        {
            switch (id)
            {
                case "shield": return 0;
                case "heal": return 1;
                case "buff": return 2;
                case "debuff": return 3;
                default: return -1;
            }
        }
    }
}
