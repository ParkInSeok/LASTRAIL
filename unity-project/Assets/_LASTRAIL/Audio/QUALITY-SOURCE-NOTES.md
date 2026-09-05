# Launch Audio Technical Pass

These remain procedural placeholders, not final sound design or a finished score.
No actual listening was performed: available tools provide file/PCM analysis and
Unity execution, but no model-accessible audio audition or music production.
No outside samples, purchased assets, new account or remote generation service used.

## Source And Reproduction

All 22 original WAV files remain available at Git revision `06a08bf` under their
unchanged `Assets/_LASTRAIL/Audio` paths. The original synthesis source is
`Assets/_LASTRAIL/Editor/LaunchAudioResourceBuilder.cs` at that revision.
The current generator retains the same deterministic noise, oscillators, IDs,
44.1kHz mono 16-bit format, sample counts, onset and duration. Use its **Rebuild Audio
PCM Only** command to regenerate clips/import settings without rebuilding scenes,
showcase prefabs, or the existing integration manifest. No external paid asset dependency.

17 WAVs changed; 5 are byte-identical (gacha_reveal_start,
gacha_result_pickup_elite, gacha_duplicate_conversion, combat_skill, combat_shield).
Only peaks above -6dBFS are attenuated; quieter clips are not normalized upward.
DC correction applies to ui_back and combat_hit only, with 5ms smooth edge weights
preserving zero endpoints. Existing SFX envelopes already end at zero; extra fades
were unnecessary. The BGM loses its 0.5Hz component and now imports as lossless PCM,
DecompressOnLoad, avoiding a lossy loop boundary. Its 529200-sample buffer is about
2.02 MiB when represented as float PCM, an explicit memory tradeoff for this short loop.

## Playback Contract

LastRailAudioCuePlayer keeps AudioSource volume/mute unchanged. SFX binding gain is
applied once through PlayOneShot; source volume remains the owner-controlled bus gain.
The one supported BGM uses its authored source gain (existing prefabs: 0.35); its
legacy binding volume remains serialized but does not override that source setting.
Repeated requests do not reassign an already bound BGM clip. There is no forced start.
Same-frame identical resolved SFX IDs, including aliases, coalesce per player; distinct
IDs and next-frame events remain eligible. This is neither event-identity deduplication
nor a cross-frame cooldown, and does not guarantee a clipping-free multi-source mix.

Existing real consumers: MissionShell and archive navigation callbacks, Gacha click
and reveal callbacks, reward/offline callbacks, combat HUD cue methods, and the audio
showcase. Combat transition presentation also uses its own dedicated AudioSource.
Catalog and prefab clip references remain stable. Binding readiness is not proof
that every possible gameplay event has a live producer. No settings UI or persistent
master mixer was found/added; existing source settings remain authoritative.

## Remaining Production Work

- UI: ui_tap, ui_back, ui_confirm, ui_error, ui_toggle_or_tab need auditioned tactile
  sound design and repeated-navigation fatigue checks.
- Recruitment: gacha_reveal_start, gacha_result_rare, gacha_result_elite,
  gacha_result_pickup_elite, gacha_duplicate_conversion need distinct authored reveal
  layers and a reviewed multi-result mix.
- Rewards: reward_claim, reward_line_tick need authored confirmation/tick textures.
- Combat: combat_hit, combat_skill, combat_ultimate, combat_shield, combat_heal,
  combat_buff, combat_debuff need differentiated transient/effect recordings or synthesis
  and an in-context mix across simultaneous effects.
- Story raid: story_raid_warning, boss_defeat need authored warning/outcome sound design.
- Music: bgm_act1_rail_loop needs composition, arrangement, production and mastered
  loop delivery. Current 55/110/220Hz stationary signal is not a score. Its 200ms repeat
  correlation is about 1.0; removing an infrasonic component is not a fatigue fix.

All 22 hashes are unique and spectra/durations differ, but several categories share
the same chime/click/riser recipe. Numerical distinction does not prove perceptual
separation. Human audition on headphones and target device, category recognizability,
long-session fatigue, full-mix true peak and loudness remain unverified.
See the Style quality manifest and QA before/after JSON for measurements, not a sonic PASS.
