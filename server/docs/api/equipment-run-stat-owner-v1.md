# Run equipment HP/ATK/DEF source v1

Version: `equipment.run-hp-atk-def.v1.20261005`. Internal server contract; no new public HTTP endpoint or operating provider is installed.

## Authority and ownership
`RunEquipmentStatOwner` implements the existing `EquipmentFinalHealthSource`. Its private `OriginalRunEquipmentStatContext.prepare(accountId,runId)` must supply the original pre-equipment START bases, wearer-to-health-target mapping, independent original hash and same-batch SQL authority assertions, including safe maintenance. It must never accept client maxima or substitute present owned growth for the original run base. Operating context installation remains incomplete; the QA adapter is explicitly LOCAL.

`capture(accountId,runId,assignments)` reads original run inventory and `EquipmentEffectStateStore`, validates account/run/revision and physical instance attributes, and supplies assertions pinning the entire inventory and current effect source. One or two affected wearers each have exactly three slots. An instance may appear only once. Proposed slots are the existing placement contract, not client stat inputs.

`prepare(context)` requires exact `expectedStatVersion`; returns `statVersion`, SQL assertions and wearer/target/maxHp. Existing `EquipmentHealthPlacementStore` commits those assertions, original placement changes, `CombatHealthStore` rebase and link receipt in one batch. Health remains owned by the original store; no secondary HP state is created.

## Approved calculation and supported range
Actual same-wearer HP/ATK/DEF item bonuses are additive: stars 1/2/3/4/5 use 1000/2500/6000/15000/35000 basis points. Static SET01 count >=1 contributes ATK 400; SET04 count >=1 contributes HP 600, count >=2 contributes DEF 1000, once. Different wearers and virtual tags are never summed. Multiply the original base by (10000 + group)/10000 once. HP floors only at the integer maximum-HP boundary; ATK/DEF retain the existing exact rational representation until the original damage owner.

Pure arithmetic covers stars 1–5. Actual source application currently covers stars 1–2 with the new definition version and no unique effects or enchantment. A valid original higher-star unique source is rejected rather than silently omitted. Other damage, shield, conditional and dynamic set handlers are not implemented here.

## Snapshot and restoration
`capture` returns `{snapshot,assertions}`. Snapshot fields: version/accountId/runId, originalContext/originalContextHash, precommit inventoryRevision, phase `PROPOSED_ASSIGNMENT_NOT_COMMITTED`, actors, fullRuntimeSupported=false and canonical packetHash. Actors retain wearerKind/wearerId/targetId, original base, proposed slots, exact instance/effect revision snapshots and calculated stats. This is a proposed immutable source, not evidence of a committed combat ATK/DEF journal.

`readPinnedRunEquipmentStats(input,trustedOriginal,independentlyPinnedHash)` requires exact original bytes and canonical hash, <=512 KiB, and returns a clone. It does not adopt current equipment or recompute a changed packet into approval. Existing departure snapshots and old contracts are preserved.

## Failure boundaries
Internal 409 `equipment_stat_` codes include version_conflict, original_context_invalid, run_inventory_unavailable, assignments_invalid, wearer_not_bound, duplicate_instance, item_changed, definition_unsupported, effect_source_missing, effect_changed, effect_handler_unavailable, category_unsupported, range_exceeded, capacity and original_snapshot_conflict. Original source corruption errors propagate. Revision/SQL assertion failures roll back placement, HP and receipt. HTTP authentication/cache/retry semantics are not newly defined by this internal contract.

## Evidence and incomplete integration
QA: `D:/Project/LASTRAIL/QA/CommanderS1-20260922/equipment-run-stat-owner-validation-20261005.json` and `equipment-run-stat-owner-sql-sample-20261005.json`. New 15 + related existing 37 = 52 PASS; scoped type/lint exit 0. The sample exercises actual isolated owned SQL, original S27 Lv1 base and original placement/HP stores: Kyle HP 700/1400 -> 812/1624 -> 700/1400, same-key replay and preserved run snapshot. Reopened SQL restores original health. It is neither a public TCP test nor an operating START.

Still incomplete: operating original-base/safe-maintenance adapter; equipped merge plus HP atomic linkage; unique/enchantment/conditional handlers; persisted native ATK/DEF consumption. The existing stored 1->2 merge then equip is tested and must not be described as equipped merge support. Full-runtime/START support remains false. No deployment, real account grant or reward action.
## Later integration result — 2026-10-05
The earlier incomplete equipped-merge boundary above is now implemented by the explicit new internal equipment.health-merge.v1.20261005 coordinator. See equipment-health-merge-v1.md and Server/WAIT#combat-b-run-equipment-stat-owner-29-equipped-merge-result-20261005. New18+related52=70PASS. Operating original context, persisted native ATK/DEF and unique/enchantment handlers remain incomplete; the earlier QA snapshot is preserved historical evidence.
