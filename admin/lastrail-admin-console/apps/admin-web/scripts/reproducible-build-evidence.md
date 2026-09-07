# Independent Admin build verification - 2026-09-07

Node 22.22.3, npm 10.9.8. Existing installation was not upgraded.

## Findings and correction

- Root has no scripts. README root commands were stale; Pages Functions, not
  only the historical local Fastify backend, proxy remote browser reads.
- No committed locks. App hidden npm lock had installed graph; contracts used
  parent admin/node_modules. Generated package locks with npm package-lock-only.
- Isolated build RED: React JSX and Node types were undeclared, previously
  inherited from parent installation. Added exactly installed @types/react
  19.2.18, @types/react-dom19.2.5, @types/node22.20.1, without upgrading them.
- Compared106 installed app packages to first generated lock: no version drift.
  Contracts direct zod3.25.76/vitest3.2.7/typescript5.9.3 match resolved parent
  installation. This is not a claim every parent transitive package was matched.

## Independent verification

Copied only source/config/manifests/locks into
`C:/Users/dlstj/AppData/Local/Temp/lastrail-admin-repro-29565beede864177b78f7a7168264f70`.
No dependency junctions or parent runtime node_modules. No credentials copied.

- App first offline npm ci failed ENOTCACHED yallist. Registry-backed npm ci
  then succeeded with ignore-scripts; after type declaration fix, offline npm ci
  succeeded111packages. Contracts offline npm ci succeeded53packages.
- App full42files241tests PASS. Build tsc/Vite PASS with existing >500KB warning.
- Contracts1file8tests PASS and typecheck PASS.
- Built files: index-DLeeegRX.js / index-CX_sa0t9.css. Not deployed.

Locks permit version-pinned installation; a cold cache still needs the registry.
Do not equate this check with authenticated Pages smoke, actual R2 writes or
deployment approval. Hono fixture/browser verification is documented separately.
