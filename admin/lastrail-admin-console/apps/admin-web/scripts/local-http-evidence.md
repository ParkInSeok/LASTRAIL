# Local HTTP / Browser Evidence

Run from `server` after building Admin Web:

```powershell
./node_modules/.bin/tsx.cmd --tsconfig ../admin/lastrail-admin-console/apps/admin-web/scripts/local-http-tsconfig.json ../admin/lastrail-admin-console/apps/admin-web/scripts/local-http-evidence.ts
```

The harness binds only 127.0.0.1:4174 and refuses an occupied port. Stop with Ctrl+C.
It applies repository migrations to a fresh in-memory sql.js database, calls real
Game/Admin handlers through Node HTTP, and serves the Admin `dist` build.
It creates only a synthetic principal/account, inquiry and diagnostic. It has no
remote database, queues, R2 or persistent credential bindings. No tokens are logged.
Admin methods other than GET are disabled in this harness.

The local `/api` bridge maps paths and bootstrap role permissions; it is NOT the
Cloudflare Pages proxy or Access authentication. This does not verify Unity UI,
Workers runtime, remote D1, DEV deployment, or production authentication.

## 2026-09-06 Execution

- Network: guest POST, inquiry POST, client-errors POST returned 200.
- Network: inquiry list/detail read back the generated ticket and exact body.
- Network: frontend appVersion=1.0.0-local/code=42 returned one row; code=43 returned zero.
- Chrome: CS list and detail displayed generated ticket, category, body, timestamps,
  app version and code. `<b>plain text</b>` appeared literally, not as HTML.
- Chrome: frontend tab displayed the stored error; applying version/code=43 showed
  empty data, switching to 42 restored the record.
- Failure found: server tab also returned this frontend diagnostic. HTTP request
  correctly contained category=server. Server ops-log-search lacked its exclusion
  clause. Reported to Server.

## Retest After Server 88458e0

- PASS: actual HTTP pagination, server 26 rows (25+1), frontend 27 rows (25+2),
  no cross-category results and no duplicate ids across pages.
- Fixtures: one frontend row from actual player POST, plus 26 frontend and 26
  legacy server rows directly seeded into this disposable database for pagination.
- PASS: Chrome server next page contains only local_server_evidence; frontend
  next page contains only frontend_unity_exception. Tab switch resets page to 1.
- PASS: frontend version code 43 returns empty and resets page to 1.
- No Pages, Access, DEV deployment or remote D1 claim is made by this retest.

## Daily Metrics After Server 30b2933

- Real local launch-summary HTTP 200, Chrome root showed today's synthetic signup=1,
  session-created accounts=1, ad reward verified/granted=0.
- 7/30-day selection and expandable daily-value table verified in Chrome; screenshot
  confirmed nonblank bar chart. Session-created accounts explicitly not activity DAU.
- Revenue, pass conversion and activity ratios remain unavailable, not calculated.
- Run start/finish and gacha cause-reference mapping covered by local adapter/render
  tests only in this pass, not a real run/gacha creation browser E2E.
- All browser tabs and local HTTP processes stopped after verification.

Browser steps after `ready: true`: open `/support/tickets`, follow the generated
ticket link, open `/ops/error-logs`, select frontend, enter version/code, search.
HTTP assertions run automatically; browser verification is manual CUA, not mocked SSR.
