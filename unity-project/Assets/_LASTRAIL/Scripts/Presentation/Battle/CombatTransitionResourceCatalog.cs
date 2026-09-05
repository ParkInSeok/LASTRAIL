using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Battle
{
    public static class CombatTransitionResourceCatalog
    {
        public static IReadOnlyList<string> SpritePaths { get; } = Array.AsReadOnly(new[]
        {
            "Assets/_LASTRAIL/UI/Common/Skins/ui_status_chip_9slice.png",
            "Assets/_LASTRAIL/UI/MissionShellResources/Badges/mission_wave_badge.png",
            "Assets/_LASTRAIL/UI/SystemFeedbackResources/Warning/warning_strip_banner_accent.png",
            "Assets/_LASTRAIL/UI/CombatResources/VFX/vfx_story_raid_danger.png",
            "Assets/_LASTRAIL/UI/KeyVisuals/kv_story_raid_boss_entrance.png",
            "Assets/_LASTRAIL/UI/CombatResources/Transitions/combat_victory.png",
            "Assets/_LASTRAIL/UI/CombatResources/Transitions/combat_defeat.png"
        });
        public static IReadOnlyList<string> AudioPaths { get; } = Array.AsReadOnly(new[]
        {
            "Assets/_LASTRAIL/Audio/Sfx/UI/ui_toggle_or_tab.wav",
            "Assets/_LASTRAIL/Audio/Sfx/StoryRaid/story_raid_warning.wav"
        });
    }
}
