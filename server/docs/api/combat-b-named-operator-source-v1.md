# B named operator source — combat.b-named-operator-source.v1.20261004

S-B-NAMED-OPERATOR-SOURCE-27 supplies an additive read-only owned Kyle/Sable candidate. It does not admit a run or assert native action support. Original C3 definitions, v1-v3 B sources, ongoing departure snapshots and reward bytes remain unchanged.

POST `/v1/combat/b-named-operator-source`, original game Bearer/session, exact `X-B-Named-Operator-Version: combat.b-named-operator-source.v1.20261004`, JSON Content-Type, no query. Body:

```json
{"operatorIds":["op_kyle","op_sable"],"expectedFormationRevision":1,"expectedLoadoutRevision":1}
```

One or two unique named IDs; account/stat/ability/haste claims and unknown fields rejected. Optional expectedOwnedSourceHash is the original C3 `sha256:<64hex>`; expectedDefinitionHash and If-Match packet hash use bare 64hex. Returned ETag is bare packetHash. Cache-Control no-store. Original authenticate runs again after capture. This is game API authentication, not a new public browser session.

Response includes version/accountId, immutable definition/definitionHash, original ownedProfile/ownedProfileHash/ownedSourceHash, formation/loadout revisions, actors, optional original refit, packetHash, runtimeSupported=false/canStart=false. Definition version `combat.b-named-operator-definition.v1.20261004`; actor profiles `combat.b-op-kyle-owned-base.v1.20261004` / `combat.b-op-sable-owned-base.v1.20261004`.

Kyle base HP1400/ATK36/DEF250/basic1800ms; Sable HP1600/ATK32/DEF282/basic2000ms. Original C3 growth arithmetic reused, rational attack preserved, complete-five training required; absent training yields null, never invented level1. Original C3 Sable missing definition remains unchanged; the additive named source owns approved Sable base binding.

Sable HP18% shield/passive10% once/6sec, stance active25%/ultimate20% strongest same group. DEF282 is actor defense. DEF110% comparison, old HP30%, old ultimate shield20%, and abolished commonHP40% cap are excluded. Active/ultimate are definition-only, not executable support. Plan source correction resolves the erroneous DEF-type queue wording.

Public installed route supplies original owned selection only: refit=null with ORIGINAL_B_PLACEMENT_AND_BCB_REFIT missing. Private supplier can borrow original BRefitStore.read to bind persisted LOCAL B selection/refit/rank/car instance. The isolated sample uses original store save/read and LOCAL SQL tables; it is not an operating provider or public client JSON override. Refit eligibility/selection/rank/account/actor placement and repeated source reads checked. Original owned source rechecked after asynchronous reads. This is candidate stability validation, not atomic START.

Actors expose permanentStats, effectiveStats=null, equipment=null, original base/basic/interception definitions/provenance, original owned stat inputs, capability by action and exact missing fields. Basic/intercept definitions available while nativeSupported=false; movement definition unavailable. Accepted timing/haste/geometry in millimeters are null. BCB03 is captured but not pre-applied to stats or assumed accepted haste. S23 accepted interception source owner and original clock retain those responsibilities. Active/ultimate/movement unsupported remains explicit.

Errors: 400 contract_violation/query or combat_b_named_request_invalid/body; 401 original authentication/session errors; 403 combat_b_named_operator_not_selected or original ownership errors; 409 combat_b_named_version_conflict/definition_changed/stale/refit_conflict/refit_changed/refit_operator_conflict/refit_operator_not_placed, original combat_owned_selection_changed/source_changed. Original corrupt/unavailable source errors propagate; no failure converted into success.

`readPinnedBNamedOperatorSource(input, trustedOriginal, independentlyPinnedHash)` strictly bounds 512KiB, validates actor/root shape and digest, compares exact trusted original bytes/hash, and returns a clone. Restore does not reread current ownership/definitions/refits or reauthorize changed data by recalculating its hash. Trusted original/hash must come from the original owned capture boundary, not caller-provided alternatives.

Verification: 12 new tests +5 C3 regression tests=17PASS; one old test intentionally excluded because it overwrites historical QA, its growth case repeated by new tests. Scoped TypeScript/eslint exit0. Signed loopback TCP8 cases; public and original LOCAL-refit Lv1 no-boost samples in existing QA. One independent scoped review: no actionable P1/P2. No operating writes/deploy/START/rewards.

Consumers: U29 can now connect named bases/owned growth/basic/S23 definition and explicit missing capability; full effective equipment/accepted actions are separate owners. Existing S27 AND U29 follow-up remains conditional until actual U29 completion, then last prerequisite owner registers one approved exact-source/no-boost comparison task. No speculative key or early queue registration. S25 all-node/geometry/admission/reward gates remain separate.