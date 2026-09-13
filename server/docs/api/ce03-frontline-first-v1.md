# Explicit frontline-first prepared profiles

New preparations can opt into enemy ordinary basic attacks targeting eligible front-row operators before rear-row operators. Valid taunt precedence, logical slot/ID/cursor ordering, recovery timing, reserved attacks, explicit skills and exterior targeting retain their existing rules. This includes ordinary `rear_ally` enemy basics; those labels are not special rear-targeting skills. The producer keeps front slots `[3,4]` and rear slots `[0,1,2]`.

The complete normative override text is `frontlinePolicySource` in `apps/game-api/src/ce03-targeting-policy.ts`; its exact UTF-8/LF bytes are pinned in new draft `sourceFiles` as `frontlineFirstPolicyV1`. Existing R1/R2 inputs remain present.

| Field | New value |
| --- | --- |
| `template.execution.targetingPolicyId` | `ce03.frontline-first-v1` |
| `simulationVersion`, `executionPolicy.version`, `template.execution.policyVersion` | `ce03.execution.2026-09-13.001` |
| `executionPolicy.targetTiePolicy` | `ce03.frontline-first-v1` |
| `template.execution.policySha256` | `08cd3350fde47c5d819836bf6c6be981467e01ea5f814ee9cd56a42e17d0f052` |

All remaining execution-policy fields, including `interruptPolicy=P3`, are unchanged. Missing or mixed new identities and altered new policy stages are rejected. Undefined targeting options are normalized to field absence before canonical hashing.

Pass `{ targetingPolicyId: "ce03.frontline-first-v1" }` as the third argument to `prepareLocalCombatDraft`, or include that field in trusted `registerLocalCombatProducer` / `createCe03LocalHost` construction options. Each registered host captures the option at construction. Its normal `/__local/ce03/prepare` route produces the selected policy; request bodies cannot switch policy. Omitting the option preserves the old P2 policy, version, hash and exact absence of the new field.

`ce03-command-contract-v1` and existing profile identifiers remain unchanged: `prepareSha256` already binds the entire draft and command contract. Policy selection therefore changes the prepare hash. Existing accepted envelopes, old fixture files and hosts must not be reinterpreted as the new policy. No mutable targeting setter is needed in Unity; its adapter validates the exact old/new identity during fresh model construction and uses separate checkpoint identity with strict cross-policy rejection.

Run `tsx --tsconfig tsconfig.base.json scripts/validation/verify-ce03-frontline-policy.ts` to create a new disposable prepared fixture under an exclusive `audit-results/ce03-frontline-policy-*` directory. It uses the normal host prepare path, retains command v1, reads back every output file, asserts no DB change during prepare and creates no run. The support selection setup is an explicit write in that new in-memory test DB. The fixture contains no presentation manifest or Unity execution proof and is not ready for admission. This command does not alter the historical `audit-results/ce03-local-producer/draft.json`.

The persistent host CLI and approved presentation projector remain unchanged. Selecting the new policy in a future live host composition still requires the corresponding reviewed Unity producer/adapter and presentation compatibility; this change performs no deployment, admission, START, finish or claim.
