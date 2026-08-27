# Gacha PRD Rarity Selection Design Draft

This draft defines a future pseudo-random distribution (PRD) policy for gacha
rarity selection. It is a design/contract draft only. It does not implement PRD,
change D1 migrations, change API response shape, deploy Workers, create
Cloudflare resources, or alter the current weighted RNG + guarantee behavior.

Current compatibility anchors:

- Weighted RNG + guarantee enforcement: `0e998a5`.
- Response compatibility smoke: `b8c2340`.
- Client-visible `guaranteeType` values remain:
  - `NONE`
  - `RARE_OR_HIGHER_10`
  - `ELITE_30`
  - `PICKUP_ELITE_60`
- Raw RNG material, seed preimages, probability values, weighted internal
  calculations, tokens, secrets, and account shortcut data are never returned.

## Goals

- Make perceived gacha variance less harsh before hard pity thresholds.
- Keep hard guarantee thresholds as the final Source of Truth.
- Keep Unity and Admin display contracts stable.
- Preserve deterministic server-side replay from committed batch history.
- Keep auditability without exposing raw RNG or probability internals to
  clients.

## Non-goals

- No implementation in this slice.
- No API response shape change.
- No new migration in this slice.
- No remote DEV secret, deploy, or smoke.
- No PRD/STAGING resource creation or deployment.
- No Unity-side probability, pity, guarantee, or RNG calculation.

## Recommendation

Use PRD only at the rarity selection layer for the first implementation.

Recommended first shape:

1. Determine whether a hard guarantee fires.
2. If a guarantee fires, use the current forced-selection path.
3. If no guarantee fires, use PRD-adjusted rarity selection.
4. After rarity is selected, choose the actual result candidate from
   `pool_snapshot_json.results[]` using the existing candidate weights within
   the selected rarity.
5. Keep pickup elite handling as a rarity/result eligibility concern:
   - hard `PICKUP_ELITE_60` stays forced;
   - non-guaranteed pickup preference can later be modeled as a pickup result
     weight inside the elite candidate pool, not as a client-visible response
     field.

This keeps result/item/operator candidate weighting intact and limits PRD to
the player-facing variance problem: "how long since a meaningful rarity
appeared?"

## Selection Flow

Current hard guarantee order must remain:

1. `PICKUP_ELITE_60`
2. `ELITE_30`
3. `RARE_OR_HIGHER_10`
4. `NONE`

Future PRD flow:

```text
for each pull:
  guaranteeType = getNextGachaGuaranteeType(...)

  if guaranteeType != NONE:
    candidatePool = eligible results for guaranteeType
    result = weighted pick from candidatePool
    selectionReason = guaranteeType
  else:
    rarity = selectRarityWithPrd(...)
    candidatePool = positive-weight results matching rarity
    result = weighted pick from candidatePool
    selectionReason = PRD or NONE internal reason

  apply result to pity counters and PRD counters before the next pull
```

Important boundaries:

- Hard guarantee always beats PRD.
- PRD never extends, disables, or weakens 10/30/60 hard thresholds.
- 10-pull batches update counters after each pull, so later pulls in the same
  request use the updated pity/PRD state.
- Duplicate replay returns the stored response shape and never re-runs PRD.

## PRD Application Position

### Rarity-only PRD

Apply PRD to the server's rarity decision before choosing a concrete result.

Pros:

- Directly targets perceived droughts for rare-or-higher, elite, and pickup
  elite outcomes.
- Keeps existing item/operator weights meaningful.
- Easier to test with long-run rarity distribution smoke.
- Avoids changing Unity/Admin response shape.
- Keeps `guaranteeType` semantics clean: it still means hard pity/guarantee,
  not soft PRD.

Cons:

- Requires a new internal rarity-selection stage before current result
  selection.
- Balance config needs explicit base rarity weights or derivation from the
  frozen pool.

Recommendation: use this first.

### Result-level PRD

Apply PRD directly to individual result weights.

Pros:

- One weighted selection step can choose both rarity and result.
- Can boost specific result families.

Cons:

- Harder to reason about support/audit questions.
- More likely to distort item/operator pool weights.
- Makes pickup and duplicate behavior harder to tune.
- More complicated to prove Unity-visible shape is stable under replay.

Recommendation: do not use for the first PRD slice.

### Pickup Multiplier After Rarity

After elite rarity is selected, apply a pickup multiplier among elite
candidates.

Pros:

- Keeps pickup tuning separate from general elite tuning.
- Can support soft pickup preference before `PICKUP_ELITE_60`.

Cons:

- Needs careful wording so players do not confuse soft pickup preference with
  hard pickup guarantee.
- Requires extra audit fields if operators need to distinguish soft pickup
  bias from normal elite selection.

Recommendation: keep as a later balance option. The first PRD design should
not require it.

## Counter Model

Hard pity counters already exist in `player_gacha_pity`:

- `elite_pity_count`
- `pickup_elite_pity_count`
- `rare_or_higher_pity_count`
- `total_pull_count`

PRD needs soft counters with similar reset semantics:

- `rare_or_higher_prd_count`: pulls since rare-or-higher result.
- `elite_prd_count`: pulls since elite result.
- `pickup_elite_prd_count`: pickup banner pulls since pickup elite result.

Recommended storage for first implementation:

- Add `prd_state_json` to `player_gacha_pity` in a future migration, or use a
  separate `player_gacha_prd_state` table if the state grows beyond counters.
- Keep hard pity columns as-is. Do not overload hard pity counters with PRD
  tuning state.
- Store pre/post PRD state in `gacha_pull_batches.result_summary_json` or a
  future `pre_prd_json` / `post_prd_json` only if support/audit needs it.

Why not reuse hard pity counters only?

- Hard pity counters are player-facing guarantee state.
- PRD counters may have curve/table-specific behavior and should be adjustable
  without changing hard guarantee semantics.
- Separating them reduces the chance that a future tuning change accidentally
  breaks 10/30/60 guarantees.

Reset semantics:

- Rare-or-higher result resets rare PRD counter.
- Elite result resets elite PRD counter and rare PRD counter.
- Pickup elite result resets pickup elite PRD counter, elite PRD counter, and
  rare PRD counter.
- Non-matching result increments the relevant PRD counters by one.
- For STANDARD banners, pickup elite PRD counter should stay absent or zero.

## Probability Adjustment Options

### Linear Multiplier

Increase the target rarity's effective weight linearly with the relevant PRD
counter.

Example concept:

```text
effectiveRareWeight = baseRareWeight * (1 + rarePrdCount * rareStep)
```

Pros:

- Simple to explain and test.
- Good first version for predictable tuning.
- Easy to cap before hard pity thresholds.

Cons:

- Can feel too mechanical near thresholds.
- Needs caps so soft odds do not become too aggressive before hard guarantees.

Recommendation: preferred first implementation style.

### Curve/Table Multiplier

Read a multiplier table from `pity_config_json` or balance config.

Example concept:

```json
{
  "prd": {
    "rareMultiplierByCount": [1.0, 1.0, 1.05, 1.1],
    "eliteMultiplierByCount": [1.0, 1.0, 1.02],
    "pickupEliteMultiplierByCount": [1.0]
  }
}
```

Pros:

- Most controllable for live balancing.
- Can shape early/mid/late pull feel without formula changes.
- Good for audit because the applied table can be frozen per banner.

Cons:

- More balance data to validate.
- Easy to create accidental cliffs if tables are edited poorly.

Recommendation: phase 2 or use table-backed values after the linear model is
validated.

### Additive Weight Bonus

Add a flat bonus to target rarity weight based on counter.

Pros:

- Easy to implement against integer weights.
- Avoids floating point if all weights are scaled integers.

Cons:

- Distorts low-probability and high-probability banners differently.
- Harder to reason about across banners with different base weights.

Recommendation: acceptable only if the whole gacha model uses integer rarity
weights and balance wants explicit additive tuning.

## Recommended Config Shape

For a future implementation, store PRD config under frozen
`gacha_banners.pity_config_json` so historic pulls remain auditable.

Candidate:

```json
{
  "rare": 10,
  "elite": 30,
  "pickup": 60,
  "prd": {
    "enabled": true,
    "mode": "LINEAR_MULTIPLIER",
    "rareOrHigher": {
      "startsAfter": 2,
      "stepBps": 500,
      "maxMultiplierBps": 30000
    },
    "elite": {
      "startsAfter": 10,
      "stepBps": 250,
      "maxMultiplierBps": 25000
    },
    "pickupElite": {
      "startsAfter": 20,
      "stepBps": 200,
      "maxMultiplierBps": 20000
    }
  }
}
```

Notes:

- Basis points avoid floating point storage and make tests deterministic.
- `startsAfter` prevents early-pull odds from changing immediately.
- `maxMultiplierBps` caps soft odds below hard guarantee thresholds.
- Exact numbers above are illustrative design-shape examples only, not
  approved balance values.

## Storage / Audit Candidates

Keep existing guarantees:

- `rng_seed_hash` remains a hash/commitment only.
- Raw RNG material is not stored or returned.
- Raw seed/preimage is not stored or returned.
- Probability and internal weighted calculation details are not returned.

Candidate stored audit fields:

- `prd_state_json` in `player_gacha_pity` for current soft-counter state.
- `selection_reason_json` in `gacha_pull_results` for sanitized internal audit.
- `prd_debug_json` in `gacha_pull_batches` only if admin probability audits
  need batch-level metadata.

Recommended first implementation:

- Do not add client-visible fields.
- Prefer storing a sanitized `selection_reason_json` per result only after the
  DB migration is approved.
- Keep it compact and non-sensitive:

```json
{
  "kind": "PRD_RARITY",
  "rarity": "ELITE",
  "counterBefore": 18,
  "mode": "LINEAR_MULTIPLIER",
  "configVersion": 1
}
```

Do not store:

- rolled random value;
- raw seed;
- per-candidate probability;
- full candidate weight list;
- token/secret/account shortcut data.

Admin display recommendation:

- Continue exposing `guaranteeType` for hard guarantees.
- If a future read-only admin detail needs PRD context, expose
  `selectionReason.kind` such as `PRD_RARITY` or `WEIGHTED_RARITY`.
- Do not show raw probability in Admin by default.

## Unity / Admin Impact

Unity:

- No calculation.
- No request body change.
- No response shape change in this design.
- Continue treating `guaranteeType` as the hard guarantee display field.
- Continue treating `DUPLICATE_REPLAY` as previous result sync.
- Continue excluding raw RNG, probability, weighted internals, tokens, and
  secrets from diagnostic JSON.

Admin:

- Current gacha read-only skeleton and labels remain compatible.
- Future admin details may show PRD reason as sanitized audit metadata.
- Admin should not imply PRD is a hard guarantee.
- Unknown backend codes should continue to fall back to raw code display.

Future optional display field:

```text
selectionReason
```

Do not add this field to live Game API responses in the first PRD design. If it
is added later, it must go through Unity/Admin adapter updates and compatibility
smoke first.

## Test / Smoke Candidates

When implementation begins, add tests before changing selection code.

Long-run distribution sanity:

- Use deterministic seeds over many pulls.
- Verify rare/elite/pickup observed rates remain within broad expected bands.
- Verify PRD does not make impossible rarity candidates appear.
- Verify raw RNG/probability fields are absent from responses.

Rare 10-pull interaction:

- Build state close to, but not at, `RARE_OR_HIGHER_10`.
- Verify PRD can affect ordinary rarity selection before threshold.
- Verify threshold pull still returns `RARE_OR_HIGHER_10` forced guarantee.
- Verify rare-or-higher result resets hard pity and PRD counters.

Elite 30 interaction:

- Build state close to `ELITE_30`.
- Verify PRD may soften elite drought before threshold.
- Verify threshold pull always returns `ELITE_30`.
- Verify elite result resets elite and rare counters.

Pickup elite 60 interaction:

- Pickup banner only.
- Verify PRD cannot override `PICKUP_ELITE_60`.
- Verify threshold pull always picks pickup elite eligible result.
- Verify pickup elite result resets pickup/elite/rare counters.

Duplicate replay compatibility:

- Commit a PRD-affected pull.
- Replay with same `Idempotency-Key`.
- Verify response shape is exact previous result sync.
- Verify command tickets are not spent again.
- Verify operators/items/currencies are not granted again.
- Verify PRD counters and hard pity counters are not changed again.

Rejected shape:

- Keep current stable error codes:
  - `auth_required`
  - `insufficient_command_ticket`
  - `banner_inactive`
  - `invalid_pull_count`
  - `contract_violation`
  - `gacha_pool_unavailable`
  - `gacha_state_conflict`
  - `gacha_temporarily_unavailable`

## Migration Impact

No migration in this design slice.

Future migration candidates:

- `player_gacha_pity.prd_state_json`
- `gacha_pull_results.selection_reason_json`
- Optional `gacha_pull_batches.prd_debug_json` only if batch-level support
  metadata is required.

Do not change existing `guarantee_type` enum for PRD. `guaranteeType` is for
hard guarantee/pity triggers only.

## Open Decisions

- Exact PRD curve values and whether they live in `pity_config_json` or the
  broader balance config payload.
- Whether first implementation uses formula-only `LINEAR_MULTIPLIER` or a
  table-backed curve.
- Whether `prd_state_json` should be a JSON column or a separate table.
- Whether sanitized `selection_reason_json` is needed at launch or can wait for
  Admin probability audit tooling.
- Whether pickup soft preference is needed before hard `PICKUP_ELITE_60`.
- Whether a future client-visible `selectionReason` field is useful enough to
  justify Unity/Admin response contract updates.
