# B operation risk LOCAL contract v1 — 2026-10-03

Task S-B-OPERATION-RISK-17@risk-table-approved-20261003; approval 44#combat-b-operation-risk-approved-20261003; contract `combat.b-operation-risk.v1.20261003`. Immutable exact rational definition, strictly bound source/reader, independent signed LOCAL supply. Existing run/source packets and numeric defense implementation remain unchanged.

| Risk | HP or EHP once | Raw action damage once |
|---|---|---|
| I | 1/1 | 1/1 |
| II | 5/4 | 23/20 |
| III | 31/20 | 13/10 |
| IV | 19/10 | 3/2 |
| V | 23/10 | 7/4 |

Progress EARLY/MIDDLE/LATE adds only damage 1/1,11/10,6/5 once. It does not affect HP, attack period, preparation/recovery/warnings, actor ATK, growth, rewards, unlocks or facilities. World/unsupported original profiles are not enabled by this table.

## Ownership and inputs

`BOperationRiskFixture.create(binding, original)` owns a synchronous clone before the first await. Strict binding contains version, operationId, runId, contextId, selected risk, progress, rawSourceHash, revision=0 and provenance=BOUNDED_LOCAL_EXPLICIT_NOT_MISSION_START. The exact approved definition/hash is included in the source. Original contains exactly one hp field (BASE_HP or EHP_BUDGET) and one damage field (RAW_ACTION_DAMAGE). Each has sourceId and exact rational value. HP riskAppliedVersion=null, damage riskAppliedVersion=null and progressAppliedVersion=null attest an unscaled source. Non-null/already-scaled, wrong/extra fields, unknown risk/band/version, conflicting source hash and numeric range overflow fail closed. Raw hash is canonical SHA256 of the complete original source. Restoring a new binding into the same owner is forbidden; exact private replay returns the old packet.

The trusted source producer must establish that source ID/profile really owns the unscaled value. Clearing flags in an untrusted request cannot establish that fact. These helpers and private fixtures do not authenticate a mission's original enemy profile, identify a Scout damage coefficient or grant spawn permissions. Never derive raw action damage by pretending an actor ATK is already a fully resolved action. Only the original authorized action profile can supply that value.

`scaleHp()` returns exact original×risk, source/kind, riskAppliedVersion and progressAppliedVersion=null, spawnConversion=NOT_PERFORMED. A BASE_HP and an EHP budget cannot both be supplied. If consuming an EHP budget, derive spawn HP from the once-scaled budget with the original defense profile; do not subsequently apply risk to derived HP. Existing entity integer conversion must be the final allocation boundary, not an extra intermediate rounding here. This contract does not convert EHP or publish native HP.

`scaleDamage()` returns exact original×risk×progress with both applied markers. `resolveDamage({pipeline,riskAppliedVersion:null,progressAppliedVersion:null})` requires the unchanged numeric-defense schema and original base exactly equal to the pinned raw action. It substitutes only the exact scaled base into the unchanged numeric defense resolver. Original effect groups, weakening, penetration, reductions and final floor still belong that resolver. No intermediate floor is introduced. V/LATE raw60→126 before defense; DEF1000→63. Raw1/3→7/10 before defense remains fractional until the original final floor. Results and snapshots are owned copies; reads/replays never accumulate factors.

Actual validity of DEF/effect eligibility and original profile belongs its producer/consumer trust boundary, not this table. The solo native HP/shield/receipt owner remains Unity.

## Pinned read/restore

`read()` returns contract/definition, binding, original, scaled exact HP/damage, input JSON schemas, support flags and missingMissionFields, startReady=false, packetHash. `readPinnedOperationRiskPacket(input,trustedBinding,expectedPacketHash)` checks exact strict binding and complete recomputed canonical packet. Trusted binding must be captured from the authenticated source/original departure, and expectedPacketHash from actual authenticated supply or a previously trusted capture. Do not pass an untrusted packet's own hash as the anchor. Other operation/run/context/risk/band/source/version/revision, altered original/derived fields or rehashed mutations cannot silently restore. Older schemas require their original reader; do not migrate a live run by applying this table retroactively.

## Explicit LOCAL supply

From canonical Server, use existing tsx with `--tsconfig scripts/validation/combat-b-operation-risk-tsconfig.json` and `startBOperationRiskSocket` from scripts/local/combat-b-operation-risk-socket.ts. TEMP/TMP/TMPDIR and all QA outputs remain D:/Project/LASTRAIL/LocalTemp or D QA. Default lifetime120s, bounded1..900000ms, ephemeral 127.0.0.1 TCP. No DB, game session, production service or mission admission is created.

Creator-only methods: `intake('a'|'b')` returns isolated signed session headers, `capture(role,binding,original)` validates/fixes one owner source, `checkpoint()` returns trusted creator-only original/binding copies (no credentials), `close()` stops/drains HTTP and private work and discards sessions/source/secret. Restore via `{restore:checkpoint}` creates fresh sessions; old token fails. Checkpoints are trusted creator inputs, not public uploads. Close all suppliers after tests; never save or message intake secrets. Creator capture is the only write mechanism, and is not exposed over HTTP.

`GET /_local/combat-b/operation-risk` requires exact `X-Combat-B-Risk-Version` and the supplier's private Bearer token/current session/account/expiry; same Origin if given, HTTP loopback only, no request body/query. Returns no-store JSON. If not privately captured it returns only definition with sourceBound=false/missionStart=false; after capture it returns the exact pinned source. Another account cannot read that source. POST/risk query cannot mutate selection. These LOCAL sessions use the original signing/verifier library and memory session allowlist; they are not game Bearer or browser DEV login sessions.

Errors: 400 combat_b_risk_request_invalid/source_invalid/pipeline_invalid/packet_invalid; 401 combat_b_risk_auth_invalid; 403 combat_b_risk_origin_forbidden; 404 combat_b_risk_local_only; 409 combat_b_risk_version_conflict/source_conflict/pipeline_source_conflict/packet_conflict or private binding conflict; 503 combat_b_risk_unavailable. Domain validation uses ActivityError; lifecycle range overflow preserves the original exact-value error. No public mutation/cache/pagination/retry policy. Exact private capture replay is safe; changed binding is rejected.

## Supported boundary and remaining mission fields

Supported: approved enum/table, bound trusted LOCAL source, exact one-time arithmetic, original defense pipeline composition, strict pinned read/restore, signed loopback read/isolation. Missing actual mission data: authenticated mission departure run ID, operation/enemy profile version/hash, server-selected unlock-eligible risk, pinned unscaled HP-or-EHP and raw action profile plus applied flags, actual progress band/evidence. Do not substitute arbitrary LOCAL values for these. Contract flags missionStart=false/enemyAuto=false/hpSpawnConversion=false explicitly preserve that boundary. Pending mission input does not make the approved risk table pending approval again.

QA/CommanderS1-20260922/combat-b-operation-risk-traces-20261003.mts and .json provide actual signed transport samples and explicitly labeled arithmetic projections. Sample700HP/60raw are LOCAL contract examples, not Scout stats, spawn HP, mission support or actual native HP changes. Followup U-B-OPERATION-RISK-17 consumes these multipliers and existing pipeline/snapshots; it does not authorize unresolved Scout route/24×250%, enemy AI/START/rewards/world/facilities/operating deployment/View.