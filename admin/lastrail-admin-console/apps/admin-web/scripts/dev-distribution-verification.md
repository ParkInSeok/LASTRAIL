# DEV distribution alignment - 2026-09-08

Contract source: Server c7163e4 (local only). Deployed c08bfcd and Pages fc419aa6
are older snapshots; this verification does not claim remote rollout or writes.

- DEV register is server-owned STABLE activation of the exact code. Multiple
  STABLE rows may coexist. Re-enable uses STABLE; promotion/tester UI is absent.
- Missing or inconsistent DEV distributionPolicy retains readable evidence but
  locks new registry commands and editor. PRD old-contract compatibility remains.
- First LEGACY registration keeps ENABLE_EXACT_LOOKUP confirmation; register and
  status do not set minimum. CAS, target echo, permission and minimum guards remain.
- PRD keeps INTERNAL registration, unchanged-artifact promotion and independent
  minimum change. No tester management or rollback controls were introduced.
- DEV pipeline upload/hash verification followed by authorized registration is a
  separate owner flow. Upload alone is not registered/active evidence, especially
  in PRD. No pipeline bypass or policy-document direct write was implemented.

Verification actually executed:

- Initial new tests: four failures. Three established missing lock/policy/UI;
  the reactivation fixture initially lacked validation.status and was corrected.
- Focused release suite: 10 files / 34 tests passed before two extra cases.
- Final full suite: 52 files / 275 tests passed, including six distribution tests.
- npm run typecheck and npm run build passed; existing >500kB chunk warning.
- git diff --check passed. Local build JS index-CFYKPEt8.js.

UI assertions use server-rendered markup; no new interactive browser/target-switch
smoke or remote mutation was performed. Existing keyed target editor is unchanged;
command target mismatch and stale revision are tested. Prior registry-ui-qa harness
models the older DEV INTERNAL policy and is not evidence for this new contract.
Remote read/capability and visual smoke remain for separately approved rollout.
Unity-owned scripts/local-http-evidence.ts is excluded from this change.
