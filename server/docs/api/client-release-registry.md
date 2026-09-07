# Client release registry — implemented local contract (2026-09-07)

Source: `apps/game-api/src/index.ts`, `apps/admin-api/src/client-release-registry.ts`,
`apps/admin-api/src/index.ts`, `packages/common/src/version-policy.ts`.
Local uncommitted implementation only. No deployment, PRD mutation, real tester grants,
secret additions, Unity/device success or artifact-byte validation is claimed.

## Stored document and migration

Existing `versions/current.json` keeps `version`, `versionCode`, `minimumVersionCode`
and optional legacy `contentRelease`. Optional `clientReleaseRegistry` is:

```json
{"schemaVersion":1,"entries":[{"version":"0.2.0","versionCode":"2","status":"INTERNAL","contentRelease":{"status":"APPROVED","releaseId":"example-release","contentVersion":"example-content","catalogUrl":"https://example.test/example-release/catalog.bin","catalogHash":"sha256:<64 lowercase hex>","minimumVersionCode":"2","maximumVersionCode":"2"}}],"auditHead":"<history UUID>"}
```

All version codes are decimal strings. Entries use exact versionCode equality;
larger-than-stable does not imply INTERNAL. Minimum is evaluated first. Status is
INTERNAL, STABLE or DISABLED. Artifact and version identity cannot be edited after
registration; use another version code. Status mutation is the only row edit.
One releaseId cannot identify different contentRelease metadata within the registry.
Registry absent preserves legacy admission. First registration enables exact lookup:
unregistered legacy codes then fail closed; no legacy status is inferred/imported.
For this first registration only, the body must explicitly include
`legacyTransitionConfirmation:"ENABLE_EXACT_LOOKUP"`; absence returns
`registry_transition_confirmation_required` (400) without a storage write.
This confirmation is separate from promotion and minimum-version confirmation.
Legacy export rejects registry documents with `version_registry_managed` (409).

## Game / Unity handshake

Send `X-LastRail-App-Version-Code`, existing App-Version/Platform/Channel headers.
`GET /v1/client-config` always returns HTTP200 with safe decision fields. Existing
response fields remain; added `registryRevision` (whole quoted R2 ETag or null),
`clientAdmission.distribution` (INTERNAL/STABLE/DISABLED or null), and
`bootstrap:{canAuthenticate:boolean,contentAuthorized:boolean,authenticationRequired:boolean}`.
For INTERNAL without valid login: accepted=false, status=AUTH_REQUIRED,
errorCode=authentication_required, canAuthenticate=true, contentAuthorized=false.
Content release fields including URL/hash/releaseId are null. Authentication can
proceed without ConfigPassed/content download. Never interpret canAuthenticate as gameplay approval.

Allowed pre-content authentication: POST `/v1/auth/guest`, `/v1/auth/password/register`,
`/v1/auth/password/login`, `/v1/auth/google`, `/v1/auth/refresh`.
Authenticated hydration allowed before tester approval: GET `/v1/profile`,
GET `/v1/profile/display-name/check`, POST `/v1/profile/display-name`, GET `/v1/story-progress`.
These retain each handler's authentication and validation. Existing recovery/reset
exceptions remain. No generic /v1 or economy bypass is introduced.

After authentication, GET `/v1/client-config/authorized` with Bearer session.
It authenticates first, then checks version and, for INTERNAL, exact account+environment+
versionCode ACTIVE/unrevoked/unexpired server tester entitlement. Success:
accepted=true, status=ACCEPTED, contentRelease.status=APPROVED_COMPATIBLE plus
releaseId/contentVersion/catalogUrl/catalogHash/compatibility. STABLE permits
pre-auth content; gameplay still requires normal session authentication.
Authenticated GET `/v1/client-config` also evaluates tester entitlement.

Pin accepted releaseId in `X-LastRail-Content-Release-Id` on registry gameplay/economy
requests. Missing/mismatched pin returns409. Every protected request reevaluates current
minimum/status/tester authorization. Promotion preserves artifact/pin. Logout/session
change, revoke, disabled version or update requirement requires client revalidation.
Keep stable Wi-Fi predownload/non-Wi-Fi consent behavior; do not invent Editor headers.
Public asset hosting is not made confidential by withholding metadata.

| Decision | status / errorCode | authorized/protected HTTP |
| --- | --- | --- |
| Header missing | APP_VERSION_REQUIRED / app_version_required | 400 |
| Below minimum | UPDATE_REQUIRED / client_update_required | 426 |
| Exact code absent | UNREGISTERED / client_version_unregistered | 403 |
| Disabled | DISABLED / client_version_disabled | 403 |
| Internal login needed | AUTH_REQUIRED / authentication_required | 401 |
| Tester missing/expired/revoked | TESTER_FORBIDDEN / tester_forbidden | 403 |
| Pin missing/mismatch | content_release_pin_required / content_release_pin_mismatch | 409 |
| Invalid/missing policy | version_policy_unconfigured | 503 |

Authorized endpoint's initial authentication failures retain existing auth error DTO/status.
The two client-config responses are no-store. Legacy requests do not require a pin.

## Admin API

Existing Admin authentication applies. Reads accept all active Admin roles; writes
require OWNER. target must equal serving ENVIRONMENT and be DEV or PRD. Local PRD
tests use fake storage; this API implementation does not authorize remote PRD writes.

GET `/v1/admin/client-releases?target=DEV|PRD` returns:

```text
{target, registryStatus:LEGACY|CONFIGURED, revision:string|null,
 minimumVersionCode:string|null, auditHead:string|null,
 rows:[{version,versionCode,status,contentRelease,artifactIdentity,
        validation:{status:STRUCTURAL_ONLY,artifactBytesVerified:false}}],
 canMutate:boolean, capabilities:{register,changeStatus,promote,changeMinimum,rollback,manageTesters:false}}
```

artifactIdentity is `sha256:` plus SHA256(canonicalJson(contentRelease)), not an
artifact download proof. revision is **whole document**, never a per-row revision.
Every POST body includes `{target,expectedRevision,reason}`; reason is nonempty <=500
characters; expectedRevision is quoted ETag from GET. Extra body fields are rejected.
Success returns the same fresh GET DTO (readback may observe a later writer).

| POST path | Additional fields / behavior |
| --- | --- |
| `/v1/admin/client-releases/register` | version,versionCode,contentRelease; initial INTERNAL; existing code rejected; first legacy transition additionally requires legacyTransitionConfirmation=`ENABLE_EXACT_LOOKUP` |
| `/v1/admin/client-releases/:versionCode/status` | status=INTERNAL or DISABLED; artifact unchanged |
| `/v1/admin/client-releases/:versionCode/promote` | expectedArtifactIdentity,confirmation=`PROMOTE_UNCHANGED_ARTIFACT`; INTERNAL→STABLE only |
| `/v1/admin/client-releases/minimum` | minimumVersionCode,confirmMinimumChange=true; separate action, usable STABLE at/above minimum required |
| `/v1/admin/client-releases/rollback` | historyId,confirmation=`RESTORE_REGISTRY`; restore committed history's before-registry, preserve current minimum |

Rollback retains later registrations as DISABLED tombstones so a version code can
never be reused for another artifact. They may be re-enabled with the same artifact.
Promotion never raises minimum. Root latest version advances on promotion to an equal/
higher code. Rollback cannot restore legacy permissive mode or invalidate minimum.
Candidate history objects are immutable at `versions/client-release-history/<UUID>.json`
and record actor,reason,time,operation,before/after,expectedRevision,parent. History is
written before pointer CAS; only the auditHead parent chain proves a candidate committed.
CAS-losing orphan candidates cannot be rolled back. No public history listing API yet;
retain returned auditHead for review/rollback. Minimum has its own explicit confirmation.

Errors use `{error:{code}}`: missing revision428; stale revision,
version_code_already_registered, registry_artifact_identity_conflict,
registry_promotion_state_invalid409; role403; invalid body/target/transition400;
storage unavailable503/read or write failure502. Refresh GET after409; never auto-force.

## Tester provisioning boundary

Migration0024 creates empty `account_tester_entitlements`: account_id,environment,
version_code,status,expires_at,revoked_at,granted_by,revision,created_at,updated_at.
No existing tester source was found; no production grants or public management route
are added. `manageTesters:false` is deliberate. Only synthetic local test fixtures
populate entitlements. Production provisioning/revocation ownership remains separate.
