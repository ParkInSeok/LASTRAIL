using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Audio
{
    public sealed class LaunchAudioCueCatalog
    {
        private readonly HashSet<string> clipIds;
        private readonly Dictionary<string, string> aliases;

        private LaunchAudioCueCatalog(IEnumerable<string> knownClipIds, IEnumerable<LaunchAudioCueAlias> knownAliases)
        {
            clipIds = new HashSet<string>(StringComparer.Ordinal);
            aliases = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var clipId in knownClipIds)
            {
                clipIds.Add(RequireText(clipId, nameof(knownClipIds)));
            }

            foreach (var alias in knownAliases)
            {
                var from = RequireText(alias.AliasCueId, nameof(knownAliases));
                var to = RequireText(alias.ClipCueId, nameof(knownAliases));
                if (!clipIds.Contains(to))
                {
                    throw new ArgumentException("Alias target is not a known clip id: " + to, nameof(knownAliases));
                }

                aliases[from] = to;
            }
        }

        public int KnownClipCount => clipIds.Count;
        public bool HasServerTransport => false;
        public bool HasGameplayTimingMutation => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static LaunchAudioCueCatalog CreateDefault()
        {
            return new LaunchAudioCueCatalog(
                new[]
                {
                    "ui_tap",
                    "ui_back",
                    "ui_confirm",
                    "ui_error",
                    "ui_toggle_or_tab",
                    "gacha_reveal_start",
                    "gacha_result_rare",
                    "gacha_result_elite",
                    "gacha_result_pickup_elite",
                    "gacha_duplicate_conversion",
                    "reward_claim",
                    "reward_line_tick",
                    "combat_hit",
                    "combat_skill",
                    "combat_ultimate",
                    "combat_shield",
                    "combat_heal",
                    "combat_buff",
                    "combat_debuff",
                    "story_raid_warning",
                    "boss_defeat",
                    "bgm_act1_rail_loop"
                },
                new[]
                {
                    A("bgm_act1_loop", "bgm_act1_rail_loop"),
                    A("sfx_mission_start_confirm", "ui_confirm"),
                    A("sfx_mission_loading", "ui_tap"),
                    A("sfx_mission_result", "ui_confirm"),
                    A("sfx_gacha_common", "gacha_reveal_start"),
                    A("sfx_gacha_rare", "gacha_result_rare"),
                    A("sfx_gacha_elite", "gacha_result_elite"),
                    A("sfx_gacha_pickup_elite", "gacha_result_pickup_elite"),
                    A("sfx_gacha_duplicate_conversion", "gacha_duplicate_conversion"),
                    A("sfx_hit", "combat_hit"),
                    A("sfx_combat_hit", "combat_hit"),
                    A("sfx_combat_skill_cue", "combat_skill"),
                    A("sfx_combat_ultimate_cue", "combat_ultimate"),
                    A("sfx_combat_shield", "combat_shield"),
                    A("sfx_combat_heal", "combat_heal"),
                    A("sfx_combat_buff", "combat_buff"),
                    A("sfx_combat_debuff", "combat_debuff"),
                    A("sfx_wave_transition", "ui_toggle_or_tab"),
                    A("sfx_boss_phase", "story_raid_warning"),
                    A("sfx_story_raid_warning", "story_raid_warning"),
                    A("sfx_boss_defeat", "boss_defeat")
                });
        }

        public string ResolveClipId(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return null;
            }

            var normalized = cueId.Trim();
            if (clipIds.Contains(normalized))
            {
                return normalized;
            }

            return aliases.TryGetValue(normalized, out var mapped) ? mapped : null;
        }

        public bool IsKnownClipId(string cueId)
        {
            return ResolveClipId(cueId) != null;
        }

        private static LaunchAudioCueAlias A(string aliasCueId, string clipCueId)
        {
            return new LaunchAudioCueAlias(aliasCueId, clipCueId);
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private readonly struct LaunchAudioCueAlias
        {
            public LaunchAudioCueAlias(string aliasCueId, string clipCueId)
            {
                AliasCueId = aliasCueId;
                ClipCueId = clipCueId;
            }

            public string AliasCueId { get; }
            public string ClipCueId { get; }
        }
    }
}
