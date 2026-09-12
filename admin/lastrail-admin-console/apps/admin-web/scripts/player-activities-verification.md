# Player activities local verification - 2026-09-12

Source contract: Server ee7d194ce33d57a630d188ae25f393b50c4eea30,
GET /v1/admin/players/:accountId/activity-summary.

Support account consumes /api/players/:accountId/activity-summary through the
existing generic Pages proxy. No auth substitution or new mutation surface.
Independent React Query key contains exact accountId. Adapter requires exact
account/version/readOnly/permission/consistency and section availability shape;
only listed scalar fields and bounded item arrays reach display.

Four Server synthetic SQLite response fixtures copied byte-for-byte:

- configured-zero-and-owned-records: 95D26AC961EF06E598F4BDDC3437625A033BED2ACA90B8217B7DF6FA7AC59381
- monthly-incomplete-binding: 26C1266F52E99B3DD59574EB2CEB1D6BB72153DFFECAEBC84E369C6287A9458E
- unavailable-source: B292823A8D849E7D1E34163B59E265DA700144E14D1EFB8B0ABD46C9333A5439
- unconfigured: 9BA5C8A08298C2BF8731844FA4F252F87FDE40DF07BE5C42BB1BA6016D2367D4

These are not live player records and are not imported into the production bundle.

RED: missing playerActivities module. GREEN: four fixture parsing, zero/null,
independent unavailable status, exact identity/version, safe projection, GET path.
Final full npm test: 56 files / 283 tests pass (13:47 KST).
Final npm run build: tsc --noEmit and Vite pass; existing size warning >500kB.
Artifacts index-CBMEwe49.js / index-M02gnw3l.css; no remote deployment.
Scoped git diff --check pass. Whole-repo check includes unrelated Unity whitespace
errors; those files were not changed by this work.

CUA local browser QA used scripts/support-read-qa.html with real synthetic fixture
through the actual display adapter/component, plus loading/empty/long-reference
states. Screenshots inspected in tool output, not persisted image files.
1280x900: document scrollWidth1265, innerWidth1280, action inputs/buttons0.
390x844: scrollWidth375, innerWidth390, overflowing dt/dd0, actions0.
Temporary viewport reset, tab closed, local Vite process stopped after QA.
This is component browser verification, NOT remote auth or full routed-page E2E.

Remaining: authorized matching Server/Admin rollout and live authenticated smoke;
no new deployment/seed/registry/real-account permissions were used. Purchase
verification and Store prices remain explicitly unobserved; policy missing is
not zero; latest100 is not a total; quest mail claimable is not grant approval.
