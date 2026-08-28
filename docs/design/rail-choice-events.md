# Rail Choice Events

## Purpose

Rail Choice Events are small two-option narrative moments attached to Act 1
missions.

The goal is to give the Commander a visible point of view without adding a new
resource, a new growth system, or a large train-community state layer.

Launch MVP should treat these as presentation-first story beats. They may use
existing optional flag, story flag, result text, and diagnostic patterns, but
they should not affect reward, economy, unlock, offline reward, gacha, or combat
math until a later explicit contract exists.

## Scope

Included:

- One candidate choice event per Act 1 operation or raid.
- Two options per event.
- Stable event ids and option ids.
- Local Unity presentation candidates.
- Future server storage/validation contract candidates.
- Future Admin read-only result visibility candidates.

Not included:

- No new currency.
- No morale/trust/reputation meter.
- No train community simulation state.
- No reward multiplier or reward line changes.
- No offline reward multiplier changes.
- No gacha rate, pity, or recruitment effect.
- No story progress requirement beyond ordinary clear rules.
- No immediate server migration, route, or mutation requirement.

## Ownership

Server owns future durable truth only if Rail Choice persistence is explicitly
implemented later.

Unity owns launch MVP presentation:

- event prompt display;
- two-option choice UI;
- local result text branch;
- local diagnostic display.

Admin owns future read-only visibility:

- chosen option id;
- event id;
- mission id;
- run id;
- whether the choice was local-only, submitted, accepted, or missing.

The launch MVP can ship without server persistence if the choice is used only
for immediate presentation and local result flavor.

## Stable Id Policy

Event ids:

```text
rail_choice:<missionId>:<choiceId>
```

Option ids:

```text
rail_choice_option:<missionId>:<choiceId>:<optionId>
```

Optional flag candidates, if persistence is added later:

```text
optional_flag:<missionId>:rail_choice:<optionId>
```

Rules:

- Stable ids stay English and API-safe.
- Player-facing labels can be localized in Unity.
- Unknown ids should remain visible as raw fallback in diagnostics/Admin.

## Launch MVP Rules

- Each event has exactly two options.
- Options should express a value tradeoff, not an economy optimization.
- Both options must be valid Commander choices.
- Neither option should be the obvious correct answer.
- No option grants or removes reward amounts.
- No option changes durable story progress.
- No option unlocks operations, train cars, or features.
- No option changes gacha, pity, offline rewards, run reward multipliers, or
  integrity disposition.
- Result text may acknowledge the option in a short mission-end or codex flavor
  line.

## Act 1 Candidate Events

| Mission | Event Id | Option A | Option B | Launch Effect |
| --- | --- | --- | --- | --- |
| `opn_01_last_departure` | `rail_choice:opn_01_last_departure:boarding_order` | Hold the platform for the last visible survivors | Depart before the engine overheats | Result text flavor only |
| `opn_02_first_signal` | `rail_choice:opn_02_first_signal:signal_truth` | Broadcast a reply immediately | Listen longer before answering | Result text flavor only |
| `opn_03_water_stop` | `rail_choice:opn_03_water_stop:water_pact` | Share repair labor with the station | Prioritize train reserves first | Result text flavor only |
| `opn_04_scrapstar_show` | `rail_choice:opn_04_scrapstar_show:broadcast_override` | Interrupt the arena broadcast with survivor voices | Use the broadcast to misdirect raiders | Result text flavor only |
| `opn_05_night_boarders` | `rail_choice:opn_05_night_boarders:passenger_roles` | Ask passengers to help with evacuation lanes | Keep passengers sheltered while crew fights | Result text flavor only |
| `opn_06_broken_line` | `rail_choice:opn_06_broken_line:final_record` | Preserve the full record even if extraction slows | Extract the route summary and move now | Result text flavor only |
| `raid_raider_king_engine` | `rail_choice:raid_raider_king_engine:crown_engine_end` | Separate prisoner cars before stopping the engine | Disable the engine core before it escapes | Result text flavor only |

These are candidates, not finalized localized copy.

## Mission Fit

### opn_01_last_departure

The choice should make departure feel like responsibility rather than escape.
Both options are defensible: one risks the train for people in sight, the other
protects everyone already aboard.

### opn_02_first_signal

The choice should frame uncertainty. Answering immediately shows hope and risk;
listening longer shows caution and suspicion.

### opn_03_water_stop

The choice should reinforce that survival resources can be secured through
repair and trust instead of looting.

### opn_04_scrapstar_show

The choice should push against Raider spectacle without turning captives into
rewards. One option centers testimony; the other centers tactical deception.

### opn_05_night_boarders

The choice should show passengers becoming a community without implying they
become owned combat units.

### opn_06_broken_line

The choice should connect to inheritance. One option preserves memory; the
other preserves immediate survival.

### raid_raider_king_engine

The choice should stay aligned with the raid meaning: stop a disposable-people
system without adopting its logic.

## Future Server Contract Candidate

No server route is required for launch MVP.

If persistence is later approved, the smallest candidate is to attach one
choice result to accepted run finish:

```json
{
  "railChoiceEvents": [
    {
      "eventId": "rail_choice:opn_03_water_stop:water_pact",
      "optionId": "rail_choice_option:opn_03_water_stop:water_pact:share_repair_labor",
      "clientPresentedAt": "2026-08-29T00:00:00.000Z"
    }
  ]
}
```

Server rules if implemented:

- Authenticate account through existing Game API player auth.
- Bind event ids to the run operation id.
- Reject choices for another mission with `contract_violation`.
- Accept only known event id and option id pairs.
- Store at most one result per event per run.
- Duplicate run finish replay must not reapply choice flags.
- Lower operation replay must not modify higher story progress.
- Choices do not affect reward preview generation.
- Choices do not affect first clear eligibility.
- Choices do not affect offline reward progress.

Potential storage:

- `run_choice_events`
- or `run_final_results.choice_events_json` if the launch shape stays tiny.

Dedicated table is preferred only if Admin filtering/support search needs it.

## Future Admin Read-Only Candidate

Admin may eventually show:

- account id;
- run id;
- mission id;
- event id;
- option id;
- localized label availability;
- accepted/replayed/missing state;
- created at;
- source: `server_persisted`, `client_diagnostic`, or `not_recorded`.

Admin must not expose mutation controls at launch.

Support-safe explanation:

```text
This mission choice changes the story response text only. It does not change
rewards, recruitment odds, unlocks, or offline rewards.
```

## Unity Presentation Candidate

Unity can implement this entirely locally for launch:

```text
RailChoiceEventDefinition
- missionId
- eventId
- promptStringId
- optionAId
- optionAStringId
- optionBId
- optionBStringId
- resultTextByOption
```

Presentation timing candidates:

- pre-mission briefing;
- mid-mission callout pause;
- post-mission result reflection.

Recommended launch timing:

- Use one post-mission or pre-result choice first.
- Avoid interrupting core battle flow until the UX is proven.

## Compatibility Boundaries

Rail Choice Events must not contain:

- reward quantities;
- economy values;
- reward multipliers;
- offline reward multipliers;
- gacha rates;
- pity state;
- raw RNG material;
- tokens or secrets;
- account auth headers;
- full server calculation metadata.

Rail Choice Events may reference:

- stable mission id;
- local narrative string ids;
- optional flag candidate ids;
- result text variants;
- diagnostic choice ids.

## Test Candidates

Unity:

- unknown event id falls back to raw id or safe missing-content label;
- each Act 1 mission fixture has exactly two options;
- result text changes by option;
- no reward/economy/gacha/offline calculation fields exist in the model;
- local diagnostic JSON excludes token, secret, raw RNG, and reward amounts.

Server, only if persistence is implemented:

- event id must match run operation id;
- unknown option id returns `contract_violation`;
- duplicate finish replay returns stored choice result without inserting again;
- choice submission does not change reward preview lines;
- choice submission does not change story progress or unlocks.

Admin, only if read model is implemented:

- list/detail are read-only;
- unknown backend ids are shown as raw fallback;
- mutation-like request bodies are rejected;
- reward/economy/raw auth/raw RNG fields are not returned.

## Open Decisions

- Whether launch MVP should be Unity-local only or server-persisted.
- Whether choices happen before mission, during mission, or after mission.
- Whether result text should live in mission narrative fixtures or a separate
  choice fixture file.
- Whether Admin needs choice visibility at launch or only after support demand.
- Whether optional flags should ever become server-scored objectives.
