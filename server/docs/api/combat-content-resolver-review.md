# CE02 server field contract and reward-reference review

Current emitted diagnostic version: `run-combat-requirements-v2` (CE01).
This is a requirements DTO, not an approved combat manifest or proof of Loaded.
Unity may build a typed resolver around these inputs while remaining blocked.

| Existing field | Type / source | Consumer meaning |
| --- | --- | --- |
| requiredContent.operationId | string|null, server operation preview | Exact operation key; never substitute a nearby node |
| requiredContent.contentVersion | string|null, preview metadata | Exact existing `act1.launch.content.v1` requirement; not proof of assets |
| startAugmentPackageRef / startTacticPackageRef under requiredContent | string|null | Existing package refs; typed augment/tactic resolution is separate |
| requiredContent.encounterType | string|null | Existing ORDINARY_15_WAVE / RAID_BOSS; unknown value unsupported |
| requiredContent.raidId / bossId | string|null | Null is normal for ordinary encounters; raid requires actual approved binding |
| requiredContent.bossPhaseRefs | string[] | Existing ordered phase refs, not loaded phase implementations |
| requiredContent.bindingVersion / catalogHash | null | No approved combat binding/manifest identity supplied |
| enemyFactionId / wavePlanId | null | Actual operation/content bindings still missing |
| rewardTableId | null | There is no separately identified table in the server model |
| riskTier / riskLevel | Roman string|null / integer1..5|null | Explicit versioned tier projection; root old risk types are unchanged |
| operatorSlotCount | number5|null | Count, not legacy array of slot labels |
| ultimateOperatorId / ultimateSelectionRevision | string|null / number|null | Validated CE01 admission pin; never builder fallback |

The current field names, nesting and version are fixed by this commit's API code.
Do not infer a bindingVersion/hash from contentVersion, or install a fixture
manifest into the shipping catalog. ApprovedCatalogSafety's locator/hash admission
and AddressableContentBackend loading can be reused, but screen/scene catalog
approval does not imply approval of faction, wave, reward or stats resources.
Absent binding identity should produce a binding-missing diagnosis before actual
resource lookup. An unsupported schema/type, invalid identity/hash, missing bound
resource and failed load should remain distinct; none means Loaded/READY. Exact
Unity enum/API and a future manifest schema are not emitted by this server slice.

## Structured reward reference: feasible adapter direction, not yet a wire field

Actual server evidence: `departure.ts` validates selected active balance
`runRewards.operations[operationId]`, operation progression and risk multipliers,
then pins balance version and full config hash. `index.ts` run-finish query joins
balance_config_versions using runs.balance_config_version; it parses runRewards
and calls calculateRunRewardPreview with preview operationId, risk, outcome,
cleared waves and first-clear status. `packages/balance/src/index.ts` supports
runRewards schemaVersion1 and resolves config.operations[operationId]. No
rewardTableId participates. The shared Unity RunStartResponseJsonAdapter still
requires legacy rewardTableId, so the current legacy adapter is not compatible.

A separate versioned Unity adapter can accept a structured source, for example:

```ts
// Proposed next contract only; NOT emitted by CE01 and NOT a manifest approval.
type RewardReference = {
  kind: "BALANCE_RUN_REWARDS";
  schemaVersion: 1;
  balanceVersion: string;
  operationId: string;
};
```

Those components already have real server meanings. Use the pinned version,
not whatever balance is active later; keep risk/outcome/first-clear inputs
separate. This avoids fabricating an opaque table ID. Client needs a verified
resource/delivery path for that exact version if it displays reward data. Server
finish/claim remains reward authority: this reference is neither a grant nor an
amount promise. Before removing reward_table_binding_missing, agree and implement
the producer field, dedicated consumer and versioned resource policy together.
No fake rewardTableId or approved reward-content asset is introduced here.

## Separate remaining decisions

Actual faction/wave bindings and manifest version/hash need approved resource
evidence. Combat seed needs an independently approved purpose, RNG algorithm and
version, generation rule, run lifetime and exact replay policy. Engine/effective
stats need approved formula/profile version, level mapping and rounding. Canonical
test profiles and numeric proposals are not those approvals. CE01 supplies only
the ultimate choice; it does not adopt timing/effects, seed or numerical baselines.
