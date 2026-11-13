<!-- RESOURCE-REFERENCE-AUDIT-20261029 -->
> **리소스 참조 재검사 완료(2026-10-29):** 남은 169개 문서 표기를 실제 위치·묶음/논리 경로·역사 자료로 대조했다. 정확한 경로는 본문을 정정했고, 없는 옛 증거는 현행 입력에서 제외 표시했다. 상세: D:/Project/LASTRAIL/QA/ResourceCleanup-20261028/169-decisions.json. 역사 자료는 최신 승인본을 대체하지 않는다. 실제 Player에는 옛 요원 미술과 임시 객차가 남아 있어 최신 아트 연결 완료가 아니다.

# 50. 콘텐츠 배포·서명·운영·문의 상세 계약

2026-09-21 번호별 통합. 이 문서는 흩어진 상세 계약을 주제별로 모은 참조본이다. 원문별 제목·출처·기존 검토 상태를 유지하고, 정책 확정·후보·과거 구현 보고를 섞어 승인하지 않는다.

**적용 순서:** 최신 사용자 결정 → 00~29 담당 정책 및 34~37 충돌 정리 → 이 문서의 충돌하지 않는 상세 계약. 원문의 작성일·완료 문구만으로 현재 구현 또는 승인 완료를 뜻하지 않는다. 원문에 있던 C/E 경로는 과거 기록이며 현재 작업 경로는 프로젝트 AGENTS.md의 D 정책을 따른다.

**공통 대체 경계:** 증강 고정 5회·시작 지급은 폐기, 적격 풀 동일 가중치 3후보·카드별 FREE3+EXTRA1·선택 중 완전 정지가 현행이다. 증강 후보 효과 수치는 출시 승인값이 아니다. 순위 보상은 성장 재화 차등 없이 외형·명예로 구분한다. 인물 역할은 20~29 최신 바이블 우선이며 과거 abilityId 매핑은 자동 변경하지 않는다. 작전물자 30/20·노드3선택/물리2방향·협동 시간·T18 HP0 등 미결정은 34와33의 미결 상태를 유지한다.

원문 내 코드 블록·JSON·표·필드·수치는 생략하지 않았다. 옛 상대 경로를 포함하는 코드 예시는 원출처 기준이며 실행 지시가 아니다. 링크와 제목 앵커만 새 위치에 맞게 변환했다.

## 상세 목차

- [Client release registry — DEV / PRD contract (2026-09-08)](#source-b61196006fde8005)
- [Signed builder → content publication → DEV registry integration](#source-d251ceee4d8ef83c)
- [DEV foundations deployment — 2026.09.07.4](#source-3a289b92502a9144)
- [Final package evidence — 2026-09-07](#source-5a294569bbf71d21)
- [원거리 외부 전투 구도 채택 및 실행 지시](#source-918fcaae83ee66a6)
- [Operation Selection And Reward Preview V1](#source-3d3fbdfb7df50669)
- [Ordinary Wave And Recovery Pacing](#source-801dac3e02a80640)
- [Predeparture Selection Validation And Freeze](#source-babcee7f63115e46)
- [Android Signing And Build Identity](#source-3f7d85fd467f15d7)
- [CE03 local profile selection proposal r1](#source-a78e5f3e47439a53)
- [Version-Code Content Release Policy](#source-0c064bbc63da0cf7)
- [Customer Support Escalation Workflow](#source-eca6ede5f64bdf03)
- [Customer Support Response Templates](#source-de295d7c8db198f2)
- [Customer Support Workflow](#source-1351fc3086aa0d11)
- [Computed scenarios — design inputs only](#source-39523bef0fe14920)
- [Ops Error Logs Source of Truth](#source-23ad5b6c04b82589)
- [LAST RAIL parallel pursuit source delivery](#source-12ecfb769e13c466)
- [Android Signing Builder Implementation Plan](#source-4e31919889a4c5f1)
- [DEV Play Internal Publisher Implementation Plan](#source-de88e58240d87fd7)
- [Version Registry / Unity Handshake Review](#source-1cb86b4d2982451e)
- [Cloudflare Environments](#source-2a676ccd3c25f69b)
- [PRD Launch Preflight](#source-0a74736945cfcb2f)
- [Release DB and API compatibility policy](#source-7e2cb4ac27ccff9b)
- [Backup And Recovery](#source-b1bfe7845e3ec25c)
- [비상 장비 — 보유·사용·지원 버튼 연결 상세안](#source-bc08b6cddc2e5a52)
- [외부 공간·영역·유효 거리 공통 상세안](#source-73ed48ca0540200c)
- [낙석·선로 잔해 피해와 감속 상세 제안](#source-05c99f4bad69cf6e)
- [LASTRAIL 문서 통합·정리 실행 계획](#source-8944219b62a9c76d)

<a id="source-b61196006fde8005"></a>
## Client release registry — DEV / PRD contract (2026-09-08)

원출처: `WorkspaceRepo/docs/api/client-release-registry.md`  
원문 SHA-256: `4d7c486935009c3cd514c7d3bf1ccc1e4846157a656f7fc50c48da16c6bd7ddf`  
검토·적용 경계: Detailed client registry/publication contract survives canonical policy overrides; prior audit identified missing wire/ETag/exact-version/rollback details in summaries.

<!-- source-body:source-b61196006fde8005:start -->
<!-- Local correction source: c7163e4b00e0b946d4be775c40992c7c5695116b; deployed DEV remains c08bfcd. -->

<a id="source-b61196006fde8005-client-release-registry--dev--prd-contract-2026-09-08"></a>
### Client release registry — DEV / PRD contract (2026-09-08)

Source: `apps/game-api/src/index.ts`, `apps/admin-api/src/client-release-registry.ts`,
`apps/admin-api/src/index.ts`, `packages/common/src/version-policy.ts`.
This DEV-policy correction is LOCAL ONLY on `codex/dev-stable-registry-20260907`,
based on c08bfcdab273923a9a81faa2a7da039d758edd81.
Deployment evidence: [DEV2026.09.07.4 report](50_RELEASE_OPERATIONS_CONTRACTS.md#source-3a289b92502a9144). The deployed DEV2026.09.07.4
still uses c08bfcd (old DEV INTERNAL behavior); do not enable the new UI contract
against it. No new deployment, remote write, device success or artifact-byte proof.

<a id="source-b61196006fde8005-environment-contract-and-activation"></a>
#### Environment contract and activation

| Behavior | DEV | PRD |
| --- | --- | --- |
| Register initial status | STABLE (immediately eligible, subject to all admission checks) | INTERNAL |
| Supported stored statuses | STABLE, DISABLED | INTERNAL, STABLE, DISABLED |
| Status request values | STABLE, DISABLED | INTERNAL, DISABLED |
| Re-enable disabled row | status=STABLE, same immutable artifact | status=INTERNAL; promote separately |
| Promote | unsupported; capability=false; POST returns400 registry_promotion_environment_disabled | INTERNAL→STABLE with artifact confirmation |
| Tester entitlement | never queried or required | required only for INTERNAL |
| Minimum | separate explicit operation; never raised by register/status | separate explicit operation; never raised by register/promote |

DEV has one distribution channel, not a restriction to one registered version code.
Multiple explicit STABLE codes can coexist; no larger/unregistered code is inferred.
Registration is activation of that exact DEV row after successful whole-document CAS.
There is no separate activation endpoint or promote step in DEV. Re-enabling uses
`/:versionCode/status` with `status:"STABLE"`. Registration/reactivation cannot replace
version or artifact fields. A valid compatible content release is still required.

LEGACY GET returns rows=[] with the environment-specific distributionPolicy below.
First register requires `legacyTransitionConfirmation:"ENABLE_EXACT_LOOKUP"` in both
environments. It atomically turns on exact lookup; all unregistered legacy codes stop
being accepted. It neither imports the old row nor changes minimum. No auto-activation
on reading the page. Capability=true expresses role support, not deployment approval
or artifact readiness. If distributionPolicy is absent, Admin must treat this as the
older contract and not synthesize new DEV write semantics.

A stored DEV INTERNAL row is invalid configuration, not an implicit STABLE conversion.
Game returns version_policy_unconfigured (503 on protected routes; safe GET config200
accepted=false, no content or authentication bootstrap). Admin GET returns502
version_storage_read_failed; mutations return400 registry_document_environment_invalid.
Rollback cannot restore an INTERNAL row into DEV. Such pre-existing documents need
separately reviewed repair, not an entitlement grant or automatic permission expansion.
Last deployment readback had an empty/absent DEV registry; no data conversion is part
of this patch. D1 migration0024 remains unchanged; existing DEV entitlement records
are ignored by DEV admission. No new migration is required.

<a id="source-b61196006fde8005-stored-document-and-migration"></a>
#### Stored document and migration

Existing `versions/current.json` keeps `version`, `versionCode`, `minimumVersionCode`
and optional legacy `contentRelease`. Optional `clientReleaseRegistry` is:

```json
{"schemaVersion":1,"entries":[{"version":"0.2.0","versionCode":"2","status":"INTERNAL","contentRelease":{"status":"APPROVED","releaseId":"example-release","contentVersion":"example-content","catalogUrl":"https://example.test/example-release/catalog.bin","catalogHash":"sha256:<64 lowercase hex>","minimumVersionCode":"2","maximumVersionCode":"2"}}],"auditHead":"<history UUID>"}
```

All version codes are decimal strings. Entries use exact versionCode equality;
larger-than-stable does not imply INTERNAL. Minimum is evaluated first. Status follows the environment table above; the stored-document example uses PRD INTERNAL. Artifact and version identity cannot be edited after
registration; use another version code. Status mutation is the only row edit.
One releaseId cannot identify different contentRelease metadata within the registry.
Registry absent preserves legacy admission. First registration enables exact lookup:
unregistered legacy codes then fail closed; no legacy status is inferred/imported.
For this first registration only, the body must explicitly include
`legacyTransitionConfirmation:"ENABLE_EXACT_LOOKUP"`; absence returns
`registry_transition_confirmation_required` (400) without a storage write.
This confirmation is separate from promotion and minimum-version confirmation.
Legacy export rejects registry documents with `version_registry_managed` (409).

<a id="source-b61196006fde8005-game--unity-handshake"></a>
#### Game / Unity handshake

Send `X-LastRail-App-Version-Code`, existing App-Version/Platform/Channel headers.
`GET /v1/client-config` always returns HTTP200 with safe decision fields. Existing
response fields remain; added `registryRevision` (whole quoted R2 ETag or null),
`clientAdmission.distribution` (INTERNAL/STABLE/DISABLED or null), and
`bootstrap:{canAuthenticate:boolean,contentAuthorized:boolean,authenticationRequired:boolean}`.
For PRD INTERNAL without valid login: accepted=false, status=AUTH_REQUIRED,
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
It authenticates first, then checks version and, for PRD INTERNAL, exact account+environment+
versionCode ACTIVE/unrevoked/unexpired server tester entitlement. Success:
accepted=true, status=ACCEPTED, contentRelease.status=APPROVED_COMPATIBLE plus
releaseId/contentVersion/catalogUrl/catalogHash/compatibility. STABLE permits
pre-auth content; gameplay still requires normal session authentication.
Authenticated GET `/v1/client-config` evaluates tester entitlement only for PRD INTERNAL.
DEV STABLE permits content metadata before login; normal authenticated gameplay and
the authorized endpoint still require a session. DEV has no AUTH_REQUIRED→tester gate.

Pin accepted releaseId in `X-LastRail-Content-Release-Id` on registry gameplay/economy
requests. Missing/mismatched pin returns409. Every protected request reevaluates current
minimum/status and (PRD INTERNAL only) tester authorization. Promotion preserves artifact/pin. Logout/session
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

<a id="source-b61196006fde8005-admin-api"></a>
#### Admin API

Existing Admin authentication applies. Reads accept all active Admin roles; writes
require OWNER. target must equal serving ENVIRONMENT and be DEV or PRD. Local PRD
tests use fake storage; this API implementation does not authorize remote PRD writes.

GET `/v1/admin/client-releases?target=DEV|PRD` returns:

```text
{target, registryStatus:LEGACY|CONFIGURED, revision:string|null,
 minimumVersionCode:string|null, auditHead:string|null,
 rows:[{version,versionCode,status,contentRelease,artifactIdentity,
        validation:{status:STRUCTURAL_ONLY,artifactBytesVerified:false}}],
 distributionPolicy:{supportedStatuses:string[],initialStatus:string,mutableStatuses:string[],testerGate:boolean},
 canMutate:boolean, capabilities:{register,changeStatus,promote,changeMinimum,rollback,manageTesters:false}}
```

DEV distributionPolicy is exactly
`{supportedStatuses:["STABLE","DISABLED"],initialStatus:"STABLE",mutableStatuses:["STABLE","DISABLED"],testerGate:false}`.
PRD is exactly
`{supportedStatuses:["INTERNAL","STABLE","DISABLED"],initialStatus:"INTERNAL",mutableStatuses:["INTERNAL","DISABLED"],testerGate:true}`.
For OWNER, DEV capabilities are register/changeStatus/changeMinimum/rollback=true,
promote/manageTesters=false. PRD adds promote=true; manageTesters remains false.
Non-OWNER readers get canMutate=false and all mutation capabilities=false.

artifactIdentity is `sha256:` plus SHA256(canonicalJson(contentRelease)), not an
artifact download proof. revision is **whole document**, never a per-row revision.
Every POST body includes `{target,expectedRevision,reason}`; reason is nonempty <=500
characters; expectedRevision is quoted ETag from GET. Extra body fields are rejected.
Success returns the same fresh GET DTO (readback may observe a later writer).

| POST path | Additional fields / behavior |
| --- | --- |
| `/v1/admin/client-releases/register` | version,versionCode,contentRelease; DEV initial STABLE / PRD initial INTERNAL; existing code rejected; first legacy transition additionally requires legacyTransitionConfirmation=`ENABLE_EXACT_LOOKUP` |
| `/v1/admin/client-releases/:versionCode/status` | DEV status=STABLE or DISABLED; PRD status=INTERNAL or DISABLED; artifact unchanged |
| `/v1/admin/client-releases/:versionCode/promote` | expectedArtifactIdentity,confirmation=`PROMOTE_UNCHANGED_ARTIFACT`; PRD INTERNAL→STABLE only; DEV unsupported400 |
| `/v1/admin/client-releases/minimum` | minimumVersionCode,confirmMinimumChange=true; separate action, usable STABLE at/above minimum required |
| `/v1/admin/client-releases/rollback` | historyId,confirmation=`RESTORE_REGISTRY`; restore committed history's before-registry, preserve current minimum |

Rollback retains later registrations as DISABLED tombstones so a version code can
never be reused for another artifact. They may be re-enabled with the same artifact.
Promotion never raises minimum. Root latest version advances on PRD promotion or DEV registration/reactivation to
an equal/higher code. Disabling a row does not lower root latest or minimum. Rollback cannot restore legacy permissive mode or invalidate minimum.
Candidate history objects are immutable at `versions/client-release-history/<UUID>.json`
and record actor,reason,time,operation,before/after,expectedRevision,parent. History is
written before pointer CAS; only the auditHead parent chain proves a candidate committed.
CAS-losing orphan candidates cannot be rolled back. No public history listing API yet;
retain returned auditHead for review/rollback. Minimum has its own explicit confirmation.

Errors use `{error:{code}}`: missing revision428; stale revision,
version_code_already_registered, registry_artifact_identity_conflict,
registry_promotion_state_invalid409; role403; invalid body/target/transition400;
storage unavailable503/read or write failure502. Refresh GET after409; never auto-force.

<a id="source-b61196006fde8005-tester-provisioning-boundary"></a>
#### Tester provisioning boundary

Migration0024 creates empty `account_tester_entitlements`: account_id,environment,
version_code,status,expires_at,revoked_at,granted_by,revision,created_at,updated_at.
No existing tester source was found; no production grants or public management route
are added. `manageTesters:false` is deliberate. Only synthetic local test fixtures
populate entitlements. Production provisioning/revocation ownership remains separate.

<!-- source-body:source-b61196006fde8005:end -->


<a id="source-d251ceee4d8ef83c"></a>
## Signed builder → content publication → DEV registry integration

원출처: `WorkspaceRepo/docs/api/signed-builder-publication-integration.md`  
원문 SHA-256: `b0809a92b774850c404bdcec02f8338637521c41dca23e6c5d56a0ad045d198b`  
검토·적용 경계: Detailed client registry/publication contract survives canonical policy overrides; prior audit identified missing wire/ETag/exact-version/rollback details in summaries.

<!-- source-body:source-d251ceee4d8ef83c:start -->
<a id="source-d251ceee4d8ef83c-signed-builder--content-publication--dev-registry-integration"></a>
### Signed builder → content publication → DEV registry integration

2026-09-08. SERVER_1 response to Plan and Unity; local integration guidance only.
No version-specific execution, new remote authorization, credential provisioning,
PRD configuration, deployment or publication was performed.

<a id="source-d251ceee4d8ef83c-existing-source-and-readiness"></a>
#### Existing source and readiness

Registry contract: [client-release-registry.md](50_RELEASE_OPERATIONS_CONTRACTS.md#source-b61196006fde8005).
Local implementation c7163e4b00e0b946d4be775c40992c7c5695116b, branch
codex/dev-stable-registry-20260907, source root:
C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server.
DEV2026.09.07.4 still runs c08bfcd, which registers DEV INTERNAL. A separately
approved deployment and readback of the corrected contract must precede automatic
DEV registration. Do not silently run against the old contract.

Existing operations helper (local/uncommitted, not a generic publisher API):
C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/gacha-audit-20260907/content-release-operations.mts.
Bindings: sibling content-bindings.jsonc. CLI shape is
`<tsx runner> <helper.mts> <audit|upload|verify|activate> <absolute candidate.json>`;
its working directory must be C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL because
imports/config/report locations are relative. This describes its interface, not
an instruction to execute remote modes now.

It accepts DEV Android only, status LOCAL_BUILT_NOT_DEPLOYED_NOT_DEVICE_VERIFIED,
appVersion 0.1.0, numeric versionCode/min/max=1, frozen source/provenance hash,
Main source presence, catalog SHA256, matching Unity Hash128 sidecar and exact
bundle manifest. It writes a local report keyed by releaseId and candidate hash.
These bounds cannot be silently removed or passed off as an arbitrary-version or
PRD-capable uploader. Generalized local builder integration is still Unity work.
Reuse its audit/upload/verify safeguards through the existing owner-controlled
publication path; do not duplicate a raw upload or version-pointer writer.
**Do not call its activate mode:** that is a historical direct legacy versions
JSON CAS writer, not the authenticated registry registration contract below.

<a id="source-d251ceee4d8ef83c-environment-allowlist"></a>
#### Environment allowlist

| Field | DEV existing scope | PRD current integration value |
| --- | --- | --- |
| Content bucket | lastrail-content-dev | UNCONFIGURED / null, stop |
| Public release root | https://dev-content.lastrail.app/android/releases/ | UNCONFIGURED / null, stop |
| Object key | android/releases/<validated releaseId>/<manifest filename> | no derived DEV fallback |
| Registry authority | https://dev-admin.lastrail.app | pipeline never calls a registry mutation |
| Version bucket (server-owned) | lastrail-versions-dev | current source lacks configured PRD VERSION_BUCKET |

PRD Admin source also has Access/D1 placeholders. Do not derive a PRD bucket/host
from naming convention or provision it from the builder. Once separately configured
and authorized, PRD pipeline ends after immutable content upload + HTTPS verification
and a sanitized receipt. No register/status/activate/promote/minimum/current mutation,
legacy export, Play Console upload/publish or track change. Release opening stays
manual in Admin / Play Console.

<a id="source-d251ceee4d8ef83c-publication-prerequisites-and-immutable-resume"></a>
#### Publication prerequisites and immutable resume

Freeze one manifest tying the verified signed app environment/package/version/code
to its exact Addressables releaseId/contentVersion/catalog URL/SHA256/compatibility,
source provenance and allowed public file bytes/hashes. Candidate/provenance/source,
private signing material and logs are not public payloads. Require content and app
identity agreement; never infer a latest version or substitute A05 for a new build.

The helper first requires an unused release namespace and persists uploadStartedAt.
Resume requires the same local receipt, candidate hash and exact manifest. It rejects
unknown objects; an existing expected object is accepted only if its bytes hash matches.
Missing objects use If-None-Match:* and R2 readback hashing. A creation race/null/412
stops; reconcile read-only before deciding whether identical bytes allow resume.
No overwrite/delete on conflict, no new release/code allocated to conceal failure.
A lost receipt is not permission to adopt an existing namespace automatically.

Only after all objects exist, verify every public HTTPS URL: status200, no redirect,
exact byte length and SHA256 including catalog/sidecar/bundles. Unity Hash128 sidecar
is not the catalog SHA256. Persist sanitized verification receipt tied to manifest.
Partial immutable objects may remain after failure; existing active registry is untouched.
The registry API performs structural validation only (artifactBytesVerified=false),
so upload/HTTPS receipts are a mandatory builder precondition, not a server guarantee.

<a id="source-d251ceee4d8ef83c-authenticated-dev-registration"></a>
#### Authenticated DEV registration

Use the existing Admin Access path. Server verifies Access JWT signature, issuer,
audience and expiry, then resolves an ACTIVE admin_principals user with OWNER role.
A service identity additionally requires valid delegated user JWT via
X-Lastrail-User-Access-Jwt; service-token transport alone is insufficient. Existing
Admin/Pages delegation may be reused where its operator session and transport are
approved. No headless builder credential/session provider is established by this
contract. Without valid existing authorization, stop AUTH_REQUIRED; do not mint
credentials, expose them in args/files/logs, spoof Cf-Access-Subject/LOCAL_HEADER,
use a Game session as Admin authority, or create a new privileged endpoint.

1. After verified upload, GET /v1/admin/client-releases?target=DEV with Admin auth.
   Require target=DEV, quoted whole-document revision, OWNER canMutate/register=true,
   distributionPolicy exactly STABLE/DISABLED, initialStatus=STABLE, testerGate=false,
   and capabilities.promote=false. Missing policy means old deployment: stop.
2. Validate installed code >= existing minimum and approved content compatibility.
   Do not raise/lower minimum to make the build pass. Registry exact lookup supports
   multiple explicit DEV codes; one DEV channel does not disable prior rows.
3. POST /v1/admin/client-releases/register with the following shape. Substitutions
   are the verified immutable manifest and fresh GET values; version codes are strings.

```json
{
  "target": "DEV",
  "expectedRevision": "\"<whole GET ETag>\"",
  "reason": "Verified signed build <manifest identity>",
  "version": "<app version>",
  "versionCode": "<decimal installed code>",
  "contentRelease": {
    "status": "APPROVED",
    "releaseId": "<immutable release id>",
    "contentVersion": "<content version>",
    "catalogUrl": "<allowlisted exact HTTPS catalog>",
    "catalogHash": "sha256:<64 lowercase hex>",
    "minimumVersionCode": "<approved compatible minimum>",
    "maximumVersionCode": "<approved compatible maximum>"
  }
}
```

Reason is nonempty <=500 characters. Send no status, minimumVersionCode at the body
root, arbitrary fields or credentials in the JSON. Root minimum remains unchanged.
For registryStatus=LEGACY, additionally require explicit reviewed first-cutover
acknowledgement and send legacyTransitionConfirmation=ENABLE_EXACT_LOOKUP.
Missing acknowledgement blocks the first cutover; a general automatic-build option
must not fabricate it. First registration atomically enables exact lookup and blocks
all unregistered legacy codes; it imports no old row. This guard remains mandatory.

4. Successful registration makes the exact DEV row STABLE immediately. No separate
   activation/promote request. Persist the returned revision/auditHead and artifact
   identity, then GET fresh: confirm exact version/code/contentRelease match and STABLE.
   artifactIdentity is SHA256 of canonicalJson(contentRelease), not raw JSON order
   or a replacement for HTTPS validation. Ensure current minimum still admits the code.
   Report registration complete only after reconciliation; a later writer may have
   changed the returned DTO/current state. Game admission readback may validate the
   exact code/release without creating an account; session/gameplay checks remain separate.

<a id="source-d251ceee4d8ef83c-conflicts-ambiguous-responses-and-idempotent-resume"></a>
#### Conflicts, ambiguous responses and idempotent resume

The registry has no Idempotency-Key protocol; duplicate code returns409, not automatic
success. Local resume must reconcile exact immutable identity and effective status:

- On 409, timeout or 502, first GET current registry. Exact same version/code/artifact
  STABLE may be recorded as already registered/currently active; preserve receipt and
  do not POST again. This does not prove which actor performed it.
- A same code with different immutable identity is a hard conflict. DISABLED is a
  deliberate state: do not auto-reactivate on retry. INTERNAL in DEV is invalid/old
  policy. Missing row after conflict remains pending; do not force fresh-ETag writes
  in a loop. Revalidate original manifest, all guards and intent before bounded resume.
- A POST can commit CAS and subsequently fail readback. Never claim every HTTP failure
  means no state change or issue automatic rollback. Immutable history is written
  before CAS, so an orphan candidate can remain after409. Only auditHead parent chain
  proves commitment. No deletion of history/payload or overwrite of current state.
- Explicit later reactivation, if separately intended, uses /:versionCode/status with
  status=STABLE plus target/revision/reason. It is not part of ordinary upload retry.
- 400 validation/cutover,401/403 auth,428 missing revision,503 unavailable policy and
  read/write failures stop the phase. Minimum changes always remain separate.

<a id="source-d251ceee4d8ef83c-verified-sources-fixtures-and-remaining-gates"></a>
#### Verified sources, fixtures and remaining gates

Read-only source inspection for this guidance: helper/config above; c7163e4
apps/admin-api/src/index.ts (Access/delegation/OWNER and actual routes),
apps/admin-api/src/client-release-registry.ts (validation/CAS/history/readback),
packages/common/src/version-policy.ts (whole-document conditional save),
apps/admin-api/wrangler.jsonc (DEV and PRD readiness). No remote request executed.

Existing synthetic fixtures under the c7163e4 server root:
- tests/unit/client-release-admin.test.ts: fake R2 CAS, DEV/PRD status, LEGACY guard,
  immutable identity, stale/duplicate409, orphan history, rollback environment safety.
- tests/integration/game-api-release-registry-smoke.test.ts: DEV no tester, PRD tester,
  exact code/minimum/auth/pin/economy guards using local SQL.js fixtures.
- gacha-audit-20260907/check-content-audit-rejections.mjs under the outer workspace:
  missing sidecar and catalog-URL mismatch negative fixtures. Not a generalized
  publisher test suite or evidence of successful remote upload.
Prior verified totals: 398 full tests, final15 registry targeted tests,4TS/lint PASS.
No tests were rerun for this documentation-only inspection; no live publisher proved.

Remaining gates: c7163e4 deployment/readback; approved per-build manifest/version and
content profiles; existing valid Admin delegation for builder registration; explicit
first LEGACY cutover acknowledgement; PRD content bucket/host configuration before
PRD uploads. Historical A05 upload was rejected before execution by automatic approval
review for insufficient exact payload/destination approval evidence; this contract is
not permission to retry it through another tool. Resolve any review gate normally for
the concrete action. Signed keys/build baseline/device readiness remain Unity-owned.

<!-- source-body:source-d251ceee4d8ef83c:end -->


<a id="source-3a289b92502a9144"></a>
## DEV foundations deployment — 2026.09.07.4

원출처: `WorkspaceRepo/docs/cloudflare/server-foundations-dev-deployment-2026-09-07.md`  
원문 SHA-256: `3ff360a4cc25fddd7b901366ad74d7929af0f88c733521c9405b38f6c6268a40`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-3a289b92502a9144:start -->
<a id="source-3a289b92502a9144-dev-foundations-deployment--202609074"></a>
### DEV foundations deployment — 2026.09.07.4

Executed 2026-09-07 after explicit user approval of DEV0022–0024 and the fixed
Game/Admin candidate. Source `c08bfcdab273923a9a81faa2a7da039d758edd81` only.
No newer dirty source, policy seed, registry registration/promotion/minimum change,
real tester grant, PRD, secret, Access or domain configuration change was included.

| Service | Worker version | Deployment | Traffic |
| --- | --- | --- | --- |
| lastrail-game-api-dev | 46c1eded-bd34-41e5-b496-70f264aad42d | d3fbb08b-2d17-41d0-a1fb-6c2f4bf0726e | 100% |
| lastrail-admin-api-dev | f13108f7-66eb-4dff-bb62-a349680eed8c | 1b683096-d807-4b6c-8aad-8f410a0b3ea2 | 100% |

Both runtime versions carry human tag2026.09.07.4 and the exact source SHA in their
deployment messages. Game deployed12:41:41UTC; Admin12:42:25UTC. API control-plane
readback confirmed both100% and unchanged bindings/runtime settings, including
existing secret names. No secret value was read/exported or rewritten.

<a id="source-3a289b92502a9144-source-and-recovery-preparation"></a>
#### Source and recovery preparation

Local release branch `codex/release-2026.09.07.4` and service tags
`game-api-2026.09.07.4`, `admin-api-2026.09.07.4` point to c08bfcd.
Backup branch `codex/backup-dev-before-2026.09.07.4` retains prior deployed
65b0263524bfd21ed42e597f1205cc81723fd5b9. No Git push/merge was performed.
All229 fixed archive source files were checked again immediately before migration.
The fixed-source package previously passed independent offline install/download0,
67files395tests,4TypeScriptprojects,lint,2DEVdryruns and fresh local24migrations.

Current DEV config/vars/nonsecret binding memberships, DB/R2/queue targets,
compatibility date/flags matched the prior live versions before rollout. Deploy used
`--keep-vars --strict`. Existing missing DEV realtime DO binding warning is unchanged;
this release does not claim realtime co-op readiness or modify that binding.

D1 Time Travel recovery bookmark was obtained before changes and stored in the local
rollout evidence. The old R2 versions/current.json bytes and SHA were copied locally.
Recovery defaults to forward correction preserving all schema and ledgers, not a
destructive down migration or automatic Time Travel restore. A Worker rollback must
first recheck live policy/data compatibility. Pre-registry binaries become prohibited
rollback targets once registry is activated; this operation did not activate it.

<a id="source-3a289b92502a9144-database-result"></a>
#### Database result

Target `lastrail-core-dev`, ID14c31f63-3b9d-46b9-bd4a-8e093aaba436.
Remote before-state contained exactly migrations0001–0021. Applied in order:

1. 0022_attendance_pass.sql
2. 0023_crystal_wallet.sql
3. 0024_tester_entitlements.sql

Final remote readback confirms all24; foreign_key_check empty,quick_check=ok.
Activity policies0,wallet policies0,tester grants0. Accounts12,reward transactions7
unchanged. Existing rows and deterministic hashes were identical before migration,
after migration and after both deployments:

| Data | Rows | SHA256 of ordered rows |
| --- | --- | --- |
| gacha_pull_results | 22 | 2d4cc6b417235e11f755227a65a76789a36459e765ce25c4f869b4b33f89a65c |
| account_items | 5 | d874a54f120e45ffa174bbb5db8467445b62f7f0799ec0315e79dfc539b50ee7 |
| item_ledger | 29 | f711347c0528ff944e8b97ee5d04b39ea36914819852c6c90310a7ace2a21249 |

The initial baseline query used an incorrect table name and failed read-only; it
was corrected before any write. No partial migration or rollback was needed.

<a id="source-3a289b92502a9144-actual-read-smoke"></a>
#### Actual read smoke

- GET official DEV `/status`:200,ok=true,game-api.
- GET `/v1/client-config` with code1:200,accepted=false,
  content_release_unconfigured,registryRevision=null,catalog withheld as expected.
- GET `/v1/client-config/authorized` without credentials:401,auth_required.
- Existing authenticated Chrome session opened a fresh Admin `/versions` page.
  After loading it showed DEV/Owner,targetdev-admin.lastrail.app,legacy0.1.0/code1/min1,
  whole revision `"11987b151204b99a4c7597c57f8c817f"`,registered versions0,
  tester management unsupported. No field, checkbox or write action was used.
- R2 current policy remains exactly the original3fields; SHA
  76baaeef622aca777a19273ab4af03b068dd6b229c8970feeb5b22c9316bf3fc unchanged.

Admin's server-history panel still shows no deployment record because this rollout
did not write the separate R2 server-deployment metadata store. Actual Cloudflare
IDs above are verified runtime truth. Do not mislabel the empty UI history as failure
to deploy, or claim a populated remote deployment-history test.

Admin follow-up request: retain/read-verify the new registry target/revision/LEGACY
empty-state, first-cutover confirmation and readonly behavior against this deployed
API. Do not register content, promote, change minimum or grant testers in a read smoke.
SERVER_1's browser check above is completed evidence; deeper owner UI checks remain
separate from this infrastructure rollout.

<a id="source-3a289b92502a9144-evidence-and-remaining-gates"></a>
#### Evidence and remaining gates

Evidence root:
`C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/release-packages/server-foundations-20260907/remote-rollout/`
`rollout.json`,preflight.json,game/admin-before/config-before/after.json,
d1-time-travel-before.json,d1-before/after-migrations/after.json,migrations-apply.log,
game/admin-deploy.log,version-policy-before/after.json,public-read-smoke.json,
admin-browser-read.json. Raw ledger records and credentials are not in these reports.

The prior "registry not deployed" prerequisite is now resolved for DEV infrastructure.
A05 publication/activation normal-review gate, initial registry cutover, exact tester
entitlement, actual Unity session/download/lobby and separate11draw/replay evidence
remain distinct. No new guest, seed, reward or draw was executed. Narrow DEV session
consent remains valid and is not requested again. PRD and launch numeric/store policy
decisions remain outside this completed rollout.

<!-- source-body:source-3a289b92502a9144:end -->


<a id="source-5a294569bbf71d21"></a>
## Final package evidence — 2026-09-07

원출처: `WorkspaceRepo/docs/cloudflare/server-foundations-release-candidate-2026-09-07.md`  
원문 SHA-256: `41933cf2f36854190bfedb4e23449bfd99dce73aa39b949f2ceee3af9d7d04a1`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-5a294569bbf71d21:start -->
<a id="source-5a294569bbf71d21-superseding-deployment-result--2026-09-07"></a>
#### Superseding deployment result — 2026-09-07

The additional DEV infrastructure approval was granted and executed: human2026.09.07.4, fixed c08bfcd, Game/Admin100%,remoteD1migrations24/FKclean/dataunchanged. See [actual deployment report](50_RELEASE_OPERATIONS_CONTRACTS.md#source-3a289b92502a9144). Earlier NOT_EXECUTED/approval-pending wording below is historical for this infrastructure scope. Registry/content activation,tester grants,policy seeds and PRD remain excluded.

<a id="source-5a294569bbf71d21-final-package-evidence--2026-09-07"></a>
### Final package evidence — 2026-09-07

Frozen commit c08bfcdab273923a9a81faa2a7da039d758edd81 (local only).
Package: [candidate-c08bfcd.zip](../../../Worktrees/11ec/LASTRAIL/release-packages/server-foundations-20260907/candidate-c08bfcd.zip)
Manifest: [manifest.json](../../../Worktrees/11ec/LASTRAIL/release-packages/server-foundations-20260907/manifest.json)
Delivery SHA256208be3c04a0ac8f46f0c18eb3338f87af18638bed32f198262f032769dad3245.
Independent offline frozen install downloaded0;229archive source files match;67files395tests,
4TSprojects,lint,2DEVdryruns and fresh24localmigrations/integrity allPASS.
No remote-ready/deployed claim. Source candidate ledger follows unchanged below.

<a id="source-5a294569bbf71d21-server-foundations-release-candidate-and-completion-ledger"></a>
### Server foundations release candidate and completion ledger

2026-09-07; SERVER_1. Candidate source is based on deployed65b0263524bfd21ed42e597f1205cc81723fd5b9.
The accompanying external package manifest records the frozen commit, archive and
bundle hashes, exact verification outputs and current readiness. This is a DEV
candidate preparation record, not deployment evidence or approval to mutate remote state.

<a id="source-5a294569bbf71d21-completed-local-slice"></a>
#### Completed local slice

| Slice | Implemented / verified boundary |
| --- | --- |
| Attendance/pass0022 | Explicit empty immutable policies; 04:00 KST day boundary; visit/milestone/recovery receipts; owned completed-run XP; premium verifier boundary; atomic ledger/replay/commit-time expiry guards |
| Crystal0023 | FREE/PAID lots, FREE-first spending, ticket exchange/recovery composition, verified order provenance, premium FREE linkage, idempotent verified refund/revocation and spent-deficit record |
| Registry0024 | Exact INTERNAL/STABLE/DISABLED; auth-first tester approval; limited hydration; gameplay release pin; Admin CAS/audit/status/promotion/minimum/rollback; empty tester table |
| Release hardening | Rollback reserves removed versions as DISABLED; conflicting releaseId metadata rejected; first legacy-to-registry registration requires explicit ENABLE_EXACT_LOOKUP confirmation |
| Consumer contracts | server/docs/api/client-release-registry.md, attendance-pass.md, crystal-wallet.md and dev-login-download-lobby-test.md |

Independent reviews covered the activity/premium clock boundary, historical refund
policy independence, registry rollback identity and final additive migration/rollback
gates. Reproduced findings were fixed and tested. See final package verification for
the frozen-source full-suite count; earlier394 is superseded by the added cutover test.

<a id="source-5a294569bbf71d21-admin-dto-questions-resolved"></a>
#### Admin DTO questions resolved

The authoritative API contract is `../api/client-release-registry.md` (canonical copy
in planning docs/api). No response/endpoint remains intentionally unspecified for
the authorized local registry UI:

- GET `/v1/admin/client-releases?target=DEV|PRD`; target must equal the serving environment.
- Rows contain version/versionCode/status/contentRelease/artifactIdentity and explicit
  STRUCTURAL_ONLY validation. It is not artifact-byte, device or store validation.
- `revision` is the quoted whole-document ETag; every mutation takes expectedRevision
  and reason. Read unavailable/target mismatch/missing revision/readonly capabilities
  must disable writes.409 means refresh/review, never force or automatic resave.
- Registration starts INTERNAL. Editing means status change only; artifact/versionCode
  cannot be replaced. First LEGACY registration additionally requires
  legacyTransitionConfirmation=`ENABLE_EXACT_LOOKUP` because it is an admission cutover.
- Promotion requires expectedArtifactIdentity and PROMOTE_UNCHANGED_ARTIFACT; server
  uses the stored artifact. Minimum change is another endpoint with confirmMinimumChange=true.
- Rollback requires committed historyId and RESTORE_REGISTRY; current minimum remains,
  newer version identities remain reserved as DISABLED. auditHead is returned; no
  history-listing API is claimed. Persist/use reviewed returned IDs as needed.
- Read roles follow existing active Admin authentication; only OWNER mutates.
  `manageTesters:false`; no entitlement management endpoint or local eligibility inference.
- PRD is a supported contract target only when correctly configured; current PRD
  infrastructure is incomplete. Local DTO/mock coverage is not a PRD readiness claim.

The Admin owner retains its frontend implementation and Pages rollout. Server changes
do not prove its UI completed, deployed or exercised actual writes. No rejected
cross-task disclosure is retried through another channel.

<a id="source-5a294569bbf71d21-one-consolidated-remote-approval-scope-not-executed"></a>
#### One consolidated remote approval scope (NOT EXECUTED)

No new credential destination approval is requested: the dedicated DEV session to
`https://dev-api.lastrail.app/v1/client-config/authorized` is already authorized.
The existing one guest and11-ticket/1+10-draw/replay budget is unchanged.

The minimum additional **infrastructure rollout** decision is whether to permit the
following exact DEV-only candidate scope after review of its manifest:

1. On `lastrail-core-dev` (`14c31f63-3b9d-46b9-bd4a-8e093aaba436`), verify current migration
   state and recoverability, then apply only0022_attendance_pass.sql,
   0023_crystal_wallet.sql,0024_tester_entitlements.sql in order. These add tables/indexes/
   triggers and the nullable pass_entitlements.order_id; no policy, entitlement or
   economy seed is included. Preserve all existing gacha receipts and balances.
2. Deploy the frozen candidate Game API `lastrail-game-api-dev` and Admin API
   `lastrail-admin-api-dev` with their existing bindings/Access/admission settings.
   Reserve a new unique human server version using verified target history, then
   create versioned release branch/service tags. Do not reuse2026.09.07.3. Archive
   and record prior Worker identities, new runtime IDs and approved read-only smoke.
3. Leave the current version pointer in its legacy state during infrastructure smoke.
   No registry registration, minimum change, content activation or policy seed is
   implied by deploying these Workers. New economy endpoints fail closed while empty.

**Separate optional activation scope**, required for a real INTERNAL case, must name:
the exact approved app version code and immutable content artifact; deliberate first
registry cutover timing and compatible Unity/Admin consumers; one dedicated account
identifier once created; DEV-only exact version entitlement expiry and revocation
owner. A minimal grant is one ACTIVE row in0024 with those values and an audit actor,
then explicit revocation after the agreed window. No wildcard tester, open-ended
expiry, arbitrary account, public grant endpoint or PRD permission is included.
The current absence of an account identifier is not permission to create another.

A05's separately approved public-file upload and guarded legacy content activation
remain blocked by normal tool review/actual public404 evidence. Do not conflate that
scope with the new registry cutover or retry through a rejected alternative tool.

<a id="source-5a294569bbf71d21-impact-order-and-recovery"></a>
#### Impact, order and recovery

Migrations0022–0024 are additive but operationally forward-only once new ledgers,
orders/entitlements exist. Keep them during Worker recovery; do not drop/reverse them.
Preserve new receipts and provenance even if a feature is disabled. No destructive
cleanup, balance conversion or automatic confiscation is packaged.

Before registry activation, legacy client admission is retained; new policy-driven
features stay unconfigured. Rollback to the old Worker still requires checking actual
current data/policy semantics; no remote rollback verification is claimed.
After the first registry write, **pre-registry Game/Admin Workers are forbidden rollback
targets**: they do not enforce tester/status/pins or preserve registry writes. Recover
with a compatible fixed Worker and guarded registry history operations. Do not remove
the registry to evade the gate, and never lower minimum automatically during rollback.

First registration creates only an INTERNAL entry and immediately blocks unregistered
legacy clients. The explicit confirmation prevents accidental ordinary-row activation;
it does not provide zero-downtime migration. Coordinate supported versions, consumers
and tester permissions before the cutover. Promotion alone never changes minimum.

Wrangler DEV dry-run reports an existing missing env.dev Durable Object inheritance
warning for realtime co-op; this candidate leaves that configuration unchanged. Do
not claim realtime room binding readiness from the successful bundle. PRD remains
blocked by D1/Access placeholders and missing VERSION_BUCKET bindings; no PRD bundle
or deployment readiness is claimed. Async Worker source/deployment is unchanged.

<a id="source-5a294569bbf71d21-final-unfinished-work-ledger"></a>
#### Final unfinished-work ledger

| Category | Item / owner / next action |
| --- | --- |
| Slice complete | Server activity/wallet/registry source, local SQL/API tests, independent fixes/review, DEV bundle preparation and frozen package |
| Policy pending | Attendance dates/tables/milestones/recovery price/cap; pass season/XP/level/free-premium tables; currency ID/name/exchange rate/SKUs; publish only approved immutable policies |
| Policy pending | Offline current compiled100/50/30/20 differs from old24/2/1/80 draft; current usage proven, final launch approval unresolved. Fuel100/cost10/recharge300 are DEV seed values, not inferred launch approval |
| Implementation plus external setup | Real store purchase/refund adapters, authenticated webhook handling and product/app ownership; synthetic verifiers are not production implementation. No real billing until separate setup/policy |
| External permission/readiness | New DEV0022–0024/Worker rollout; optional exact tester entitlement/cutover; A05 publication/activation normal-review gate |
| Consumer implementation | Admin local registry UI and Unity actual authorized adapter/session pin/test runner; owners report actual fixed source and tests. Narrow DEV session consent already supplied |
| Real verification pending | Actual A05 hashes/activation → sole guest/profile/name → download/lobby; separate approved11draw/replay/DB evidence. No local fixture is promoted into this column |
| Deferred backlog | Tester management UI/API, public audit-history listing, unsupported source-less Admin metrics, Rail Choice/World Raid/exact concurrent-user work; no silent expansion into this candidate |
| PRD/app distribution | PRD infra/Access/D1/R2, Google/store registration, signed Android build/device test remain outside this DEV candidate |

Preserve existing login/first-clear claim identities if a future policy changes ticket
rewards to crystals. No retroactive claim reset, free regrant, inventory conversion,
gacha probability/pity change or unapproved launch numeric default is included.

<!-- source-body:source-5a294569bbf71d21:end -->


<a id="source-918fcaae83ee66a6"></a>
## 원거리 외부 전투 구도 채택 및 실행 지시

원출처: `WorkspaceRepo/docs/design/exterior-distance-approved-20260912/BRIEF.md`  
원문 SHA-256: `773c8b1ea0ca566c26216d0d600749af36b7d584c541f37770d0ddbfef39a672`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: 전투/작전 교차분야 상세 계약. 구형15wave/start package와 새 노드정책은 프로토콜 이행 대조 필요

<!-- source-body:source-918fcaae83ee66a6:start -->
<a id="source-918fcaae83ee66a6-원거리-외부-전투-구도-채택-및-실행-지시"></a>
### 원거리 외부 전투 구도 채택 및 실행 지시
2026-09-12 / LAST RAIL_Plan_4 / task 01a0956d-aa68-72d3-9cba-97be7a3e2614

<a id="source-918fcaae83ee66a6-사용자-확정과-기준"></a>
#### 사용자 확정과 기준
사용자 직접 “원거리시안으로 하는걸로 결정하고 관련 작업 진행시켜”. 이전 시안만 제작/구현하지 말라는 제한은 이번 구도 적용 업무에 대해 실행 승인으로 전환됐다. 근거리안이 아니라 원거리 전체 전투 화면을 채택한다. 기준 이미지는 approved-distant-fullscreen.png. 이는 생성형 구도/배치 시안이며 이미지 안의 캐릭터 변형·차량 수·포탑 수·문구·지형상의 적 수·스탯이 새 게임 계약으로 승인된 것은 아니다.

<a id="source-918fcaae83ee66a6-적용-범위"></a>
#### 적용 범위
- 상단 외부 전투 영역에 먼 사선 측면 시점 적용. 열차는 왼쪽 아래, 오른쪽 진행. 우측/상부에 적의 등장·접근 경로와 사격 공간 확보. 기존 세로 전체 화면의 header/상단viewport/열차HP/실내전투/카드·버튼 영역 경계 유지.
- 승인 시안의 먼 거리감을 우선. 최초 train group 폭 약40~45%를 로컬 조정 출발점으로 사용하며 실제 소유/장착칸 수에 맞춘다. 비율은 Plan 구현 가이드이지 사용자 직접 수치 지시 아님. 자동으로 근거리안으로 되돌리지 않는다. 주요 포탑/적 가독성은 실기준720폭 전체화면으로 확인하고 필요한 국소 크기·대비만 조정.
- 첫 구현은 승인 이미지의 협곡 구도 한 맵. 후속 언덕/폐도시는 동일 camera·train anchor를 공유하는 지형/진입로 변형 구조로 준비, 모든 맵 완성으로 선언하지 않는다.
- 배경/먼지/지형·적·차체·바퀴·포탑·총구·손상효과는 분리된 실제 runtime 요소. 전체 시안 PNG를 전투 배경으로 통째로 붙여 기능 완료처럼 처리하지 않는다. 승인 기존 열차 외형·실제 장착/소유·기관포구를 유지; 사선용 원천이 부족하면 정확한 자산 요구를 제시하고 독립 가능한 부분 계속.
- 실제 typed exterior target만 공격가능 대상으로 표현. 배경 등장 연출과 활성 표적을 구분, 준비/타깃불가/사격시점은 기존 계약 유지. 보이는 언덕 동선을 이유로 새 적·이동전투·거리판정·데미지·스폰 timing을 임의 추가하지 않는다. 미지원 깊이/진입 데이터는 표현 매핑과 실제 상태의 차이를 문서화.
- 실내전투·대원배치·기존 UI·N%·단일태양·네온 유지. 새 요원모션/미확인 socket/expiry 추측 금지. 최근 c8ac 열차HP 손상·수리·pause 동작 유지하되 원거리 가독성/차체transform추종 재검증.

<a id="source-918fcaae83ee66a6-기존-담당별-실행"></a>
#### 기존 담당별 실행
Unity_1: E runtime 소유. 현소스 snapshot 보존 후 상단 viewport/world-to-screen 및 실제train/turret/exterior target anchor 구현. Direction에 정확 layer/crop/pivot/크기 계약 전달. 준비된 요소로 실제 동작 slice를 먼저 진행하고 최종 협곡 자산 연결. 공격방향/actualtarget/trace/HP손상효과/클리핑/입력/경고/복원·pause·speed 회귀. 변경별 fresh source/resource packet.
Direction_1: 승인원거리 image 직접 열어 제작 기준으로 채택. 협곡 첫맵의 배경·진입지형·가림/전경·먼지 layer와 실제 적 진입 표시 계획 제작, 기존 열차외형 보존 및 사선원천 부족 확인. Unity와 layer/pivot 계약 맞춘 후 자기 작업경로에 실제 공급물+출처/manifest. E 직접 덮어쓰기 없음. 최소720폭 실제 전체화면에서 적/포탑/손상 식별 및 실내HUD 가림 검수. 신규미술 전체승인은 실제산출물 범위만.
SERVER_1: 새 gameplay 계약 추가 없이 현재 accepted producer/증거 보존. Unity 신규 packet과 기존 c8ac 대비 source delta/모델 및 상태 동일성을 독립 검토. 실제 exterior target 활성/스폰 계약 질문만 좁게 대응. 새 START/finish/claim/배포 불필요, 기존 settled state 보존.
Plan_4: 담당 간 의존성과 실제 산출물 인수/가독성·상태 증거 경계 검수. Admin 신규 배정 없음.

<a id="source-918fcaae83ee66a6-완료-증거"></a>
#### 완료 증거
같은 초기조건/seed/명령 전후 gameplay state exact, 기존 UI입력과 effect음향 계약 보존. 실제 전체세로화면 PNG+약20~30초 외부접근/포탑추적·발사/차체주행 및 실내전투 동시표시 영상. 새 구도720폭 가독성, 화면경계/겹침, 낮은HP손상/수리 시정렬, pause/speed/restore의중복효과 억제 확인. 정지fixture/연속재현/native버튼/음향 유무 구분하고 시행하지 않은 항목은 미검증. 자동decode는실청음아님. 새 admission 증거로 소급하지 않음.
각 담당 canonical 갱신·readback 후 실제 변경/commit·local상태/테스트/산출물/잔여/다음행동을 Plan_4로 결과보고. ACK와 구현완료 분리,이미 같은 업무 진행 중이면 중복 실행 금지.

<a id="source-918fcaae83ee66a6-보존과-권한"></a>
#### 보존과 권한
상시 프로젝트 조율·구현/QA 승인 유지. c8ac/75ec46 열차손상 v05 및 과거 v03/23e3·7c127·f496 완료/실패 증거 보존. 새 원격배포/실계정 경제변경/유료구매 승인 확대 없음. 본 문서와 reference는 로컬 미커밋, 제품구현/테스트 완료 증거 아님.
Reference SHA256: 06C56C81EBB69F75F87F80BDD7860396D7F467B04B6E09067DFE0A6B5621B581

<!-- source-body:source-918fcaae83ee66a6:end -->


<a id="source-3d3fbdfb7df50669"></a>
## Operation Selection And Reward Preview V1

원출처: `WorkspaceRepo/docs/design/operation-selection-detail-v1.md`  
원문 SHA-256: `0ad3db807781229de0651f22fdcc876ceca243b3fb37c24a0f1d78a7b738d671`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Four contentaxes category/encounter/mode/difficulty; sixstorymissions≠fourtypes; previewnotgrant/newcatalognotpermission; sourcecategoryunknownscopednotwholehistoryabsence.

<!-- source-body:source-3d3fbdfb7df50669:start -->
<a id="source-3d3fbdfb7df50669-operation-selection-and-reward-preview-v1"></a>
### Operation Selection And Reward Preview V1

2026-09-09 / Plan / proposed UI detail only.
Sources: online-coop-mode-policy.md, chapter1-encounter-curve-v1.md,
predeparture-validation-freeze-v1.md. No new catalog or reward policy.

<a id="source-3d3fbdfb7df50669-content-routing"></a>
#### Content Routing

Logical content groups: story, existing basic operation categories, World Raid when
available in the selected environment/release. This is not authorization to enable
World Raid now. Story/story raid is solo; basic operations support approved1/2 modes;
World Raid1/2 has no difficulty. Never infer mode from15wave encounter type alone:
story mission and repeatable operation may share combat format but different entry.
Canonical content membership/mode support must come from authored server metadata.
The four basic category IDs are still not recovered here; do not invent names or
use the six chapter1 story missions as four categories. No Rail Choice inclusion.

<a id="source-3d3fbdfb7df50669-portrait-ui"></a>
#### Portrait UI

Selection list/map gives operation name, relevant thumbnail, access/clear state and
one primary objective. Tap to show detail in the same surface or a back-navigable
detail view. Avoid forcing large map plus full details side by side on portrait.
Keep selected mode/difficulty/cost visible above the preparation action; detailed
reward lines can scroll. Short preview of known threats is content-authored, not
a forecast of random rewards or unpublished narrative spoilers.

Detail contains objective (15waves or boss defeat where applicable), mode selector,
eligible difficulty selector, entry conditions, quoted cost/balance, repeat reward
preview and first-clear reward status. World shows current event/end time and links
to its score-tier rules instead of a fabricated fixed win reward or boss kill goal.

<a id="source-3d3fbdfb7df50669-rewards-and-requirements"></a>
#### Rewards And Requirements

- First-clear and repeat rewards are separate. Already received first-clear reward
  displays received, not still available because mode or difficulty changed. Use
  server-defined claim scope; don't invent per-mode/per-tier independent first clears.
- Actual reward amounts/items come from server preview. If a reward is variable,
  display its authored range/type only when supplied; never show it as guaranteed
  or synthesize drop odds. No hardcoded design-test amounts in production UI.
- Preview is not a grant. Changing mode/difficulty refreshes quote/rewards under
  the same revision. Don't mix old cheap cost with new richer rewards.
- Unlock conditions separate from optional recommended growth. Reference levels
  in chapter1 test curve are not confirmed live requirements or launch recommendations.
  No power gate or predicted win percentage. Actual unavailable conditions name
  the requirement rather than generic 'power too low'.
- Below top2 invite exception displays separately from personal/random eligibility;
  solo/random action remains unavailable when individually locked. No automatic
  conversion to an invite room or future difficulty unlock after carry.

<a id="source-3d3fbdfb7df50669-actions-and-states"></a>
#### Actions And States

Solo Prepare -> existing editor; coop mode -> explicit Random Match / Invite / Join
actions, then normal acceptance/preparation. No entry charge on browsing or selecting
mode. Changing criteria during existing queue requires cancelling that queue first.
World event rollover refreshes selection for explicit confirmation, not invisible
replacement with another boss. Story progression controls narrative visibility only
under existing world policy, not an extra entry condition.

Availability states: loading, available, progression-locked, event-closed, temporarily
unavailable, read-error and no content. Show applicable reason; no test thumbnail/data
substitution masquerading as playable content. An expired quote reloads before entry.
Returning from setup preserves legal selection and scroll position, revalidating
content/version/ownership/cost. Invalid selection is explained rather than replaced
by auto-starting a default mission. No automatic shop/upgrade redirect.

<a id="source-3d3fbdfb7df50669-content-classification-contract-detail---2026-09-09"></a>
#### Content Classification Contract Detail - 2026-09-09

Four independent semantic axes are required, not one overloaded operation type:

| Axis | Meaning | Must not infer |
| --- | --- | --- |
| Content category and operation identity | Story mission, authored basic category/operation, active world event | Six story missions are not four basic categories |
| Encounter format |15wave or finite storyboss, separately authored world score encounter | Encounter format alone does not permit coop |
| Supported party mode | Explicit solo/coop capability for that content | Coop capability doesn't authorize invite carry at every tier |
| Difficulty selection | Authored tiers, or not applicable for world | Do not manufacture a default world difficultyI |

Operation definitions also link objective semantics, entry policy, reward/first-clear
scope, content version and required resource set. These are logical requirements,
not approved new field names or a migration. Read missing definitions as incomplete
content data; don't populate a clickable mode with a guessed default.

Selection UI reads supported modes; preparation validates the same operation's
policy; matchmaking keys use the approved exact content/mode/difficulty or event
identity; battle uses encounter format; results use objective/reward policy. Each
step preserves the selected version and cannot reinterpret a record from display name.
Localizable labels may change without changing identity or resetting first clear.

World absence of difficulty must survive requests/results without pretending that
null is tier1. Actual schema representation belongs to owner contract. Story's
solo-only capability persists even when the same15wave combat implementation is used.
Invite carry remains admission-policy exception, not a separate reward content ID
or automatic story progression. No new world event or basic category invented here.

<a id="source-3d3fbdfb7df50669-source-recovery-status"></a>
##### Source Recovery Status

Plan searched local docs Markdown/JSON for four-type/operationType terms, then
E:/Projects/LASTRAIL/docs and server/docs for matching type/category terms. Found
existing explicit unknown-ID notes but not four canonical definitions in that scope.
No full repository/history absence claim. Sent narrow original-source recovery
request to active Direction_1 task01a07f14-b742-72b0-8119-e91ef8af1424; send returned
success, result pending. Asked exact paths/status and no invented names. No code,
art generation or new capture authorized. Classification remains a design contract
proposal pending actual catalog recovery, not a completed integration.

<a id="source-3d3fbdfb7df50669-original-verification-boundary"></a>
#### Original Verification Boundary

<a id="source-3d3fbdfb7df50669-source-recovery-follow-up---2026-09-09"></a>
##### Source Recovery Follow-up - 2026-09-09

Plan read Direction_1 canonical docs/handoffs/LAST_RAIL_DIRECTION_1.md investigation
entry. Owner result is UNCONFIRMED, not still an uninspected outstanding report:
count4 and solo/coop policy are documented, exact category names/IDs were not recovered
in its scoped docs/source/history investigation. Initial policy commit already carried
the unknown-ID note according to owner evidence; Plan did not rerun that git audit.
This is not exhaustive chat/cloud/history absence. Direct report delivery was blocked
per owner record; Plan read the local canonical file, did not retry/relay messaging.

Do not map the six Chapter1 missions, two encounter enums, six threat profiles or
four semantic axes onto four category slots. First-operation solo fixture is still
usable for isolated design validation, but cannot certify all four categories/coop.
Exact original list or explicitly approved newly authored catalog is required before
generic category routing and per-category high-tier wave tables can be finalized.

Future cases: story15wave not coop, four-category metadata missing, first-clear
already claimed across permitted modes, stale reward/cost pair, invite-only eligible,
event rollover, unknown preview amount, long localization and back navigation.
No runtime tests performed. Policy reads and document/handoff readback only; local
uncommitted. No UI art, Unity order, API/deploy/video. Actual selection/preview
catalog contracts remain prerequisites, not functionality claimed by this document.

<!-- source-body:source-3d3fbdfb7df50669:end -->


<a id="source-801dac3e02a80640"></a>
## Ordinary Wave And Recovery Pacing

원출처: `WorkspaceRepo/docs/design/ordinary-wave-recovery-pacing-proposal-20260909.md`  
원문 SHA-256: `0be5a8e416aa7c3a5f0d3d25be40e9ef92329a418a902042acdcec5b26bdc43a`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: 전투/작전 교차분야 상세 계약. 구형15wave/start package와 새 노드정책은 프로토콜 이행 대조 필요

<!-- source-body:source-801dac3e02a80640:start -->
<a id="source-801dac3e02a80640-ordinary-wave-and-recovery-pacing"></a>
### Ordinary Wave And Recovery Pacing

Status: USER ACCEPTED INITIAL TEST BASELINE on2026-09-09; no runtime tests,
implementation or validated final balance.
Scope ordinary15waves; not boss-only story raid or timed World Raid.

<a id="source-801dac3e02a80640-duration-budget"></a>
#### Duration Budget

Reference appropriate-entry-strength team: target approximately3-4min active
ordinary encounter. This is an encounter design target, not fixed wall-time gate.
Example per-wave reference clear times by five blocks:1-3=8s,4-6=10s,7-9=12s,
10-12=14s,13-15=16s. Total active180s plus14 proposed1s transitions=194s.
Stronger teams finish sooner; do not hold an empty wave until target seconds.
Initial stage/count/HP/spawn composition are not finalized from this arithmetic.
Slow-team failure timer, if needed, is separate explicit decision; no hidden
enrage or timeout introduced here. Device loading/menus excluded from target.

<a id="source-801dac3e02a80640-threat-schedule"></a>
#### Threat Schedule

1-3 establish basic assault;4-6 one additional ranged threat;7-9 introduce one
visible sabotage;10-12 combine previously shown families;13-15 increase overlap
without mandatory rare skill. First chapter may simplify introductions; do not
restart tutorial every run. No automatic boss added at5/10/15.
Clear only when required enemies/objectives and scheduled wavespawns complete.
Recovery/cooldown timers continue through transition, no freeHP/charge refresh.
Initial benchmark: at most one major mandatory response window overlapping for
solo ordinary; two-player complementary actions may be one shared mechanic,
not double simultaneous impossible tasks. Exact spawn quotas remain open.

<a id="source-801dac3e02a80640-carriage-recovery-candidate"></a>
#### Carriage Recovery Candidate

Per carriage, per whole attempt: first10s, second15s, third+20s cap. Restore40percent
carriageHP, proposed2s protection from ordinary carriage hits. Does not protect
total train or reverse existing transferred damage. Existing committed mechanic
exceptions require clear effect rules, not blanket invulnerability.
Automatic timer independent of all carriage devices; no repair-resource cost.
Manual acceleration still requires exact skill/cap and cannot reset count. No
count reset on wave/reconnect. Total-train0 ordinary failure cancels restoration.
Three downs lose45s device activity (23.2percent of194s reference), not a whole
team DPS calculation. Five parallel damaged cars do not add waits serially.

<a id="source-801dac3e02a80640-acceptance-before-adoption"></a>
#### Acceptance Before Adoption

Compare no-down/healing-heavy/intentional-down builds, lowest eligible squad,
strong carry and poor-network cases. Check loss of cargo/fire/barrier does not
create mandatory-counterplay deadlock; max healing/repair cannot stall infinitely.
Wave scheduler must not extend dead periods purely to reach duration target.
Validate actual14transition count and late projectiles/spawn callbacks at end.
Source guessed OrdinaryWaveRules.cs path absent; existing real wave/run files
must be inspected before future implementation, no claim current pacing matches.
Next map enemy counts/stats and skill damage/healing into this provisional budget.

<!-- source-body:source-801dac3e02a80640:end -->


<a id="source-babcee7f63115e46"></a>
## Predeparture Selection Validation And Freeze

원출처: `WorkspaceRepo/docs/design/predeparture-validation-freeze-v1.md`  
원문 SHA-256: `d3e582c2c533b321e857c42fe660125eefb3dc41dd7ed4ebb3d57efdaba7173e`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Preserve substantive clauses under: Existing Contract And Actual Gap; User Flow; Proposed Preview Responsibilities; Revision And Edits; Start Atomicity; Coop Extension, Not New Live Route; Failure UX Mapping; Finish And Compatibility; Test Cases And Status

<!-- source-body:source-babcee7f63115e46:start -->
<a id="source-babcee7f63115e46-predeparture-selection-validation-and-freeze"></a>
### Predeparture Selection Validation And Freeze

2026-09-09 Plan. PROPOSED CONTRACT DESIGN; no endpoint/schema implementation.
Sources: local server run-start-augment-package.md, selected game-api/index.ts
metadata references, start-package-selection-detail-v1 and combat-design-readiness-
index-v1. Source inspection is not confirmation of latest deployed behavior.

<a id="source-babcee7f63115e46-existing-contract-and-actual-gap"></a>
#### Existing Contract And Actual Gap

Documented start accepts operationPreviewId only. Package refs and allowed slot
metadata are returned/stored; finish selections can be empty for compatibility.
Matching packageRef at finish is NOT sufficient proof all chosen IDs/quantities/
owners were frozen before combat. Do not claim that validation exists based on
metadata or test strings. Full current owner audit is required before coding.

Recommended approach: build a validated immutable preview for the chosen loadout,
then keep the current start-by-preview shape. Alternative of accepting all choices
in finish is rejected for authoritative combat. Alternative of silently extending
start body breaks known clients and is not adopted here. Preview preparation's
exact route/schema remains Server-owned and unimplemented by this document.

<a id="source-babcee7f63115e46-user-flow"></a>
#### User Flow

Operation selection -> squad/train setup -> package selection -> validated summary
-> departure -> accepted frozen run -> battle. No mid-wave choice screen.
One final departure action can prepare+start through existing adapter orchestration;
do not require extra user confirmations for each internal request. Show preparing
vs starting, block double tap, keep errors attached to offending slot.

Local edits are drafts and free. Preview preparation spends no stamina/ticket.
Only accepted run start consumes existing entry cost. No autosave grants or new
permanent currency sinks. Selected package is run-only, not inventory ownership.

<a id="source-babcee7f63115e46-proposed-preview-responsibilities"></a>
#### Proposed Preview Responsibilities

| Input from player | Server-derived or validated value |
| --- | --- |
| Operation/difficulty/mode reference | Availability, mode rules and entry requirements |
| Five operator IDs and slot order | Ownership, uniqueness, legal slot layout, current growth |
| Selected ultimate operator | Must be one of accepted five; compatible kit |
| Train/carriage selections | Ownership/unlocks, exact slot count, Combat1 and type limits |
| Equipment IDs if enabled | Correct owner/slot/rank and actual available catalog |
| Augment/tactic IDs | Referenced allowed pool, category counts, effect applicability |
| Observed revision | Conflict detection; never client authority |

Client does NOT supply maxHP,damage,healing,prices,entry eligibility or world score.
No local invented IDs from proposal documents accepted as production definitions.
Server computes resolved stats/package effects and records the exact balance/content
revision used. Same train IDs with different upgrade levels produce different
resolved snapshot. Missing definition fails preparation, not fallback guessed stats.

<a id="source-babcee7f63115e46-revision-and-edits"></a>
#### Revision And Edits

An edited draft produces a NEW immutable preview revision/reference; it does not
mutate an already-started preview. Only current accepted revision is startable in
this proposed flow. A pending request for old revision cannot overwrite newer UI.
Change squad/ultimate -> revalidate tactics; change package/version -> clear invalid
choices and explain. Do not silently choose a replacement or reset paid inventory.

At start compare relevant authoritative inventory/growth/availability revisions.
If changed, re-prepare and show updated summary before starting; do not consume
entry cost using a mixture of old HP/new ownership. Avoid rejecting for unrelated
account updates such as support-message count. Exact dependency revision mechanism
must be specified by Server, not a client hash of all account data.
Preview TTL is server-provided, not a hardcoded new duration. Expiry requires fresh
preparation without charge. Refreshed preview does not extend an active attempt.

<a id="source-babcee7f63115e46-start-atomicity"></a>
#### Start Atomicity

Validate auth/admission/content; verify latest valid preview and ownership; check
cost; atomically consume preview+charge+create run/snapshot+idempotency record.
One logical successful start owns one run. If atomicity cannot be supported by the
existing persistence path, implementation needs a documented recoverable state
machine; do not promise atomicity through UI alone or a best-effort log write.

Same start key+same preview returns original result/snapshot. Same key+different
preview rejects. Lost response retries original identity, not another charge.
If result committed, do not revalidate against changed policy on replay and create
a second run. If definitively not started, release any reservations exactly once.
Uncertain write is pending reconciliation, not automatic refund plus valid run.

Freeze selected IDs, resolved growth/device stats, package/category choices,
profile versions, slot layout and mode. Cosmetic animation refs separate from
authoritative damage. Only sanitized relevant data returned to client/Admin;
no auth secrets/hidden economy payloads in diagnostics.

<a id="source-babcee7f63115e46-coop-extension-not-new-live-route"></a>
#### Coop Extension, Not New Live Route

Both roles confirm the same room configuration revision. Train player owns train
and train/support augments; squad player owns five/ultimate/squad augment/tactic,
per current proposal. Any relevant edit clears BOTH ready states. Neither host
can override partner selection nor let a stale ready start after revision changed.

Start when both ready and current admission valid. Charge each actual participant
according to existing mode cost policy; no second charge to host. Shared start
identity/room revision deduplicates concurrent start requests. Require all-or-none
accepted start or explicit compensation/recovery protocol, not one client showing
battle while the other's rejected state is treated as voluntary abandonment.
No disconnected-player auto-choice or free role swap introduced.

World event timing/tickets remain its own policy; this proposal does not promote
World Raid into launch. Solo story never waits for a nonexistent second ready.

<a id="source-babcee7f63115e46-failure-ux-mapping"></a>
#### Failure UX Mapping

| Failure category | UX | Charge |
| --- | --- | --- |
| Missing/duplicate operator or illegal car | Highlight slot, remain in setup | None |
| Inapplicable tactic after roster edit | Clear tactic selection, explain | None |
| Expired/stale preview | Refresh preview, review relevant changes | None unless original start already committed |
| Insufficient entry resource | Show server requirement/current availability | None |
| Update/content requirement | Existing update/download route | No premature new start |
| Start outcome unknown | Connecting/reconcile same request | Never assume charge/refund outcome |
| Already started replay | Resume original accepted flow | No new charge |

These are conceptual categories, not new claimed existing errorCode strings.
No purchase coercion, device-local unlock or automatic downgrade to different risk.

<a id="source-babcee7f63115e46-finish-and-compatibility"></a>
#### Finish And Compatibility

Finish validates against original run snapshot, never current edited roster or
new package. Echo cannot add/replace an augment. Legacy empty selections allowed
only under that legacy contract; do not interpret as permission for arbitrary
unrecorded effects in the new version. Existing runs finish with original rules.
Do not reject an old paid run merely because a new package version was published.

<a id="source-babcee7f63115e46-test-cases-and-status"></a>
#### Test Cases And Status

Duplicate IDs; nonowned operator; wrong owner gear; too many cars; supplied slot
count matches list but not actual unlock; changed ultimate invalidates tactic;
unknown proposal key; stale revision response; expiry; upgrade between preview/
start; simultaneous starts; replay after version change; partial persistence;
coop readiness race; auth loss; finish modified echo. Actual tests NOT RUN.
This unit produced design/source review only, no code, request, grant or deployment.
Next source-owner contract audit or explicitly authorized isolated simulation,
not another cosmetic/gameplay feature needed to close this preparation gap.

<!-- source-body:source-babcee7f63115e46:end -->


<a id="source-3f7d85fd467f15d7"></a>
## Android Signing And Build Identity

원출처: `WorkspaceRepo/docs/ops/android-signing-build-policy.md`  
원문 SHA-256: `351f89090d16d5a32931a2a7300a93aa5a77bd1470a508f5e9d802dbeab8b4ed`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-3f7d85fd467f15d7:start -->
<a id="source-3f7d85fd467f15d7-android-signing-and-build-identity"></a>
### Android Signing And Build Identity

<a id="source-3f7d85fd467f15d7-2026-09-08-user-approval"></a>
#### 2026-09-08 User Approval

User authorizes separate DEV/PRD keystore generation and a Unity Editor build
script selecting the matching package and signing configuration. The user
specified the password privately in the task; do not reproduce it in source,
handoffs, logs, command-line arguments, public artifacts or this document.

| Environment | Application ID | Signing role |
| --- | --- | --- |
| DEV | app.lastrail.game.dev | Dedicated DEV key |
| PRD internal and production | app.lastrail.game | Dedicated Play upload key |

PRD testing and production are tracks of the same app, not different packages.
The upload key is distinct from Google Play's app signing key. OAuth registration
must use the certificate of the actually installed distribution.

<a id="source-3f7d85fd467f15d7-storage-and-validation"></a>
#### Storage And Validation

- Use independent key pairs and aliases; never overwrite discovered existing keys.
- Keep keystores outside Git, with appropriate local access restrictions.
  E:/Projects/LASTRAIL-Signing/ is a proposed location, not verified creation.
- Obtain credentials through non-serialized secure input or process environment;
  never embed the password in the Editor script or project settings file.
- Record only public certificate fingerprints, aliases, paths and expiry.
- Verify distinct public certificates, password/alias usability and secure storage.
  A single local copy is not a completed backup. No external backup transfer implied.

<a id="source-3f7d85fd467f15d7-editor-build-contract"></a>
#### Editor Build Contract

- Reuse existing build pipeline; explicit DEV/PRD selection determines package,
  signing and environment settings. APK/AAB format must not infer the environment.
- Reject missing credentials and inconsistent defines/endpoints/content profiles.
- Set application ID explicitly rather than relying on company/product fallback.
- DEV sets LASTRAIL_ENV_DEV and PRD sets LASTRAIL_ENV_PRD. Remove the opposite
  environment symbol, preserve unrelated feature symbols, and reject conflicting
  environment input. Account for compile/domain reload timing so the actual
  player uses the selected environment, not stale Editor assemblies.
- Preserve existing Addressables local-Editor/remote-player behavior.
- Restore prior Editor settings on success, failure and cancellation; clear signing
  password fields when finished. Preserve all unrelated user changes.
- Validate actual artifact package/signature when an artifact is built; preflight
  tests alone do not prove final APK/AAB identity.

<a id="source-3f7d85fd467f15d7-scope-and-current-state"></a>
#### Scope And Current State

<a id="source-3f7d85fd467f15d7-content-publication-integration"></a>
##### Content Publication Integration

Latest user decision: integrate Addressables build/upload with this pipeline.
DEV performs verified upload then automatic exact-version activation; PRD stops
after verified R2 upload and leaves Admin activation/Play distribution manual.
See [release policy](50_RELEASE_OPERATIONS_CONTRACTS.md#source-0c064bbc63da0cf7) for CAS, failure and
retry boundaries. Earlier no-upload notes describe earlier scope and are
superseded for this integration design only; no unspecified remote run is implied.

<a id="source-3f7d85fd467f15d7-approved-output-layout"></a>
##### Approved Output Layout

User confirmed the output layout on 2026-09-08:

`E:/Projects/LASTRAIL/unity-project/Builds/Android/<DEV|PRD>/<version>_<versionCode>/`

Example filenames: LASTRAIL_DEV_0.1.1_2.apk and LASTRAIL_PRD_0.1.1_15.aab.
Format remains an explicit build choice, not a different version-code sequence.
Validate all path components and containment; do not overwrite prior artifacts.
Stage and verify candidates before marking them successful. Include a non-secret
build report; exclude outputs from Git. Keep failed outputs distinct and never
apply the QA-video keep-only-latest deletion rule to build archives.
Addressables build outputs and immutable remote release paths remain separate.

<a id="source-3f7d85fd467f15d7-automation-guidance-not-infrastructure-approval"></a>
##### Automation Guidance, Not Infrastructure Approval

User explicitly deferred Jenkins/CI infrastructure work on 2026-09-08.
Continue only the already-approved local builder work for now.

A Jenkins controller is optional: the same deterministic Editor/batch entrypoint
can run locally or on a dedicated CI agent. First stabilize the approved builder,
then evaluate Jenkins or another CI orchestrator. No Jenkins install, service,
public endpoint, cloud subscription or automatic upload is approved by this question.
CI should use an isolated workspace, fixed Unity/Android toolchain, appropriate
Unity licensing, protected credentials and a single authoritative per-package
version-code state. Local per-machine counters cannot safely allocate concurrent
CI codes independently. Serialize builds sharing an Editor workspace even across
DEV/PRD. Tests and artifact verification precede any separately approved release.
MCP editor control is not the required automation backend.

<a id="source-3f7d85fd467f15d7-parameterized-builds---2026-09-08"></a>
##### Parameterized Builds - 2026-09-08

User requires server and displayed app version parameters, with optional version
code. Proposed implementation dispatched to Unity:

- Required server: DEV or PRD; required version: explicit app bundleVersion.
- Explicit versionCode: use exactly the requested valid code or report a conflict;
  never silently substitute another value.
- Omitted versionCode: increment the environment's last successful build baseline
  by one. Keep DEV and PRD separate; PRD internal/production and APK/AAB share
  their package's sequence. Changing version text does not reset the sequence.
- Establish initial baseline from evidence; current project code1 is not proof
  of a Play upload. Missing state/new machines need baseline reconciliation.
  Local state must not be presented as an automatically verified Play maximum.
- Reject duplicate/regressive explicit codes against known successful/uploaded
  history and in-flight reservation, invalid integers and platform-limit overflow.
- Preflight failure consumes no code. Lock and atomically persist build state;
  commit success only after artifact identity/signature verification. Failed or
  cancelled attempts must not mark success or publish partial output. Never reuse
  a code known to have been externally published/uploaded.
- Restore transient Unity settings but preserve the successful counter in a
  separate non-secret build-state record. Document recovery and rollback behavior.
- Provide Editor UI and batch executeMethod argument parsing/examples plus tests.

These rules are implementation requirements, not a verified working script or
authorization for Play uploads/remote deployment. No concrete CLI name is final.

User-provided Play Console app-list screenshot on 2026-09-08 confirms two draft
app records: LAST RAIL (app.lastrail.game) and LAST RAIL_DEV
(app.lastrail.game.dev), both zero installed users. This supersedes the earlier
creation-form-only evidence, not the unverified signing/build status. It does
not prove AAB upload, Play app signing setup, OAuth setup or release approval.

Unity task received the request. Generation, script implementation and tests are
not yet verified. This approval does not authorize store upload, OAuth registration,
remote publication, replacing existing signing identity or PRD release.
Full player builds are not claimed or required for this implementation dispatch.

<!-- source-body:source-3f7d85fd467f15d7:end -->


<a id="source-a78e5f3e47439a53"></a>
## CE03 local profile selection proposal r1

원출처: `WorkspaceRepo/docs/ops/ce03-local-profile-selection-proposal-20260911.md`  
원문 SHA-256: `f6a0a6a33df113d288ecf1878ecf26fccc8a7d38ce2c07f773e0affae5b891bd`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-a78e5f3e47439a53:start -->
<a id="source-a78e5f3e47439a53-ce03-local-profile-selection-proposal-r1"></a>
### CE03 local profile selection proposal r1

<a id="source-a78e5f3e47439a53-select-three-commander-skills-and-leader-presentation-detail-r1--2026-09-11"></a>
#### Select Three Commander Skills and Leader Presentation Detail r1 — 2026-09-11

Unity relayed explicit user acceptance of approximately10 commander support skills, choose3 without duplicates before departure, ordered HUD slots/no battle swaps, individual cooldowns/shared cast lock and separate squad-leader charge skill. Initial all10 selectable is combination-test scope, not permanent unlock/economy. Plan read latest Unity canonical confirming the request.

Created docs/design/combat-command-loadout-and-leader-presentation-20260911.md, SHA256260456EDA7225CF7C258D8B346C289F55B3ED513BC5E9F7CC252B1084BEB68D8, exact readback verified. Includes ten-role/target-decision table, preparation/save/revision/atomic admission/frozen selection/recovery impacts, separate gauge/ready logic, five-stage full-charge presentation, input→accepted→cast→effect→end leader presentation with proposed timings/small cut-in/camera/VFX/SFX/haptics, attack/defense/support mapping and owner-specific implementation/test requirements.

Approved directions are distinct from proposed detailed visuals and unresolved combat numbers. No numerical charge gains/consumption/carryover/down policies, support effect coefficients/cooldowns/stacking/unlocks invented as approved. Presentation timings are explicitly proposals. Previous fixed3 support and cooldown-only leader implementation/tests remain historical evidence, not proof of the new version. Existing accepted snapshots must not be rewritten; new profile/algorithm/contracts/evidence required when implemented. No code/runtime/VFX/economy change or new test run this unit; document local/uncommitted.

Propagation to Server/Direction/Unity requested by the user through Unity, but not sent: prior automatic review rejected destination authorization. No blocked outbound retry or indirect relay. Concrete reviewable document is complete; destination-specific permission remains needed for blocked inter-task sharing. Actual host/admission/lifecycle/video remains OPEN.

<a id="source-a78e5f3e47439a53-user-direction-change-industrial-a-and-commander-charge-gauge--2026-09-11"></a>
#### User Direction Change: Industrial A and Commander Charge Gauge — 2026-09-11

Unity relays user selection of industrial visual candidate A and acceptance of normal skills retaining cooldowns while commander ultimate charges through squad combat activity and becomes manually usable at100%. User requested final mockup; this unit does not authorize or claim a completed code change. Plan directly read Unity canonical and visually inspected D:/Project/LASTRAIL/Worktrees/5f93/LASTRAIL/QA/Design/CombatCandidates-20260911/A-final-charge-gauge-v02.png, raw SHA25652B65F5EDCF8D1187785F1205ABD0FE560F695EE340A81063E352C50D4EED0E6. Image shows100% charge/ready badge/cyan commander border. It is a mockup, not gameplay evidence. Depicted names/portraits/stats do not independently amend the adopted owned roster or runtime content IDs.

For future implementation this supersedes commander cooldown-only/first-ready15s/reuse45s direction in the adopted local profile. Existing runtime and previous passing tests still implement cooldown semantics and must not be presented as validating the new gauge. Preserve original frozen history; introduce a new explicit profile/algorithm version when the gauge policy is fully specified, not an in-place semantic change to accepted runs.

Unspecified decisions remain charge-earning events and amounts, initial charge/cap/overflow, consumption and failed/cancelled/duplicate input behavior, wave carryover and down/recovery policy. Do not invent coefficients from the image. Required implementation impact: Unity command gating/readmodel/HUD, deterministic charge events and checkpoint/CAS recovery; Server closed policy/profile/attestation evidence and snapshot/replay version consistency; Direction industrial-A visual composition and clear charge/ready feedback. New-policy tests must cover no double gain/spend on duplicate or restore and exactly defined consumption timing. Normal skill cooldowns and explicit selected-commander authority remain.

Earlier broad visual-approval uncertainty is now superseded for the selected A direction, not arbitrary details beyond the relayed user choice. Outbound propagation to Server/Direction/Unity has NOT been sent: prior automatic-review rejections and destination-specific approval request remain unresolved. No intermediary/indirect transmission attempted. This update records the incoming decision in canonical planning only; runtime still cooldown-based and actual admission/lifecycle/video remain OPEN.

<a id="source-a78e5f3e47439a53-local-qa-profile-adopted-by-plan--2026-09-11"></a>
#### Local QA Profile Adopted by Plan — 2026-09-11

Supersedes earlier blanket local adoption-pending statements. Unity relayed the user's actual-combat implementation/recording authorization, repeated continue instructions and standing approval preference. Plan treats reversible local QA selection as implementation discretion within that task, not individual user approval of every coefficient.

Adopt local selection version ce03.local.qa.20260911.001: ce03-local-profile-selection-proposal-20260911.md r1 first operation, solo/risk I, all15 waves, selected initial five, ordered Combat/Gun, disposable account with actual L1/P0/kit1/train1 facts, equipment/research/extra pressure OFF, proposed per-wave actor HP table, train test values, and documented three augmentation plus one tactic package scopes. Adopt execution-policy r1 with r2 corrective precedence, including its proposed timing/order/queue/target/channel/cap/direct-impact/no-wall-catchup/terminal policies for this local version. Preserve accepted recovery and exact owned skillTraining rank mapping. Ultimate selection must be explicit in UI or named automated fixture; no hidden default.

Unity is instructed to proceed with the actual config-driven consumer and local scene. Selection is fixed; an executable artifact is NOT yet frozen. Complete r4.1 projections, actual effect coverage, code and executed vectors must precede canonical runtime hash pinning and readiness claims. Proposal text hashes are provenance only. Missing or conflicting fields must be resolved explicitly by Plan without reinstating a blanket adoption wait.

This authorizes no public readiness, remote deployment, real-account fabrication, remote seed/DB operation, or release balance. Local authenticated end-to-end integration still requires working producer/consumer evidence; no completed battle/video is claimed. Original pending history is preserved below, superseded only for this local selection. Unity notified; Server and Direction informed for coordination.

2026-09-11 Plan_2. REVIEWABLE PROPOSAL ONLY. No runtime adoption, release balance approval, asset registration or public gate change. Existing user goal is real local combat round trip plus QA video; implementation scope remains with existing owners.

<a id="source-a78e5f3e47439a53-approach-and-candidate-selection"></a>
#### Approach and candidate selection

Recommend the complete first-operation subset: opn_01_last_departure, solo, risk I, all15 mission-specific waves. Compared with later missions this uses only scout/gunner/railbike and two cars; the raid needs phases/immunity and is not simpler just because it has one boss. Package-off may be useful as a separately labelled diagnostic but does not exercise the proposed3+1 package flow. We retain the package path in this candidate rather than describing a reduced test as the complete reference.

References: ce03-content-20260911/ce03-minimum-operation-report.md and its evidence manifest, version ce03.direction.2026-09-11.001; Server ce03-local-adoption-inputs-20260911.md r1 at11ec; fixed server7ad482c. Direction manifest raw SHA256034115a76319ed4abd8c21593a6533440a4d0dbe9cd62da182ac6c711e94b5a2. Evidence manifest is not an executable catalog, and raw file hash is not canonical runtime hash.

Candidate owned formation: Kyle/Reina/Eve/Sera/Mira; Combat/Gun in server-validated ordered slots. Reference operatorL1/P0/kit1 and trainL1 use only a disposable local fixture whose account actually has those facts. Never overwrite real account facts or allow client stats to confer ownership. Selected ultimate remains explicit user selection among the validated five; no default Kyle. Equipment/research OFF and extra pressure OFF are explicit local comparison variants, not deletion of account entitlements or all-feature support.

Train candidate uses the existing test override HP5000/armor1000BPS/ATK60/attack1s, engine65 for movement only, weight60/speed1.05. All selected five kits/passives, Combat/Gun devices, train manual actions, three enemies and shared rules must be resolved; train values alone do not complete this profile.

<a id="source-a78e5f3e47439a53-proposed-actor-hp-allocation--existing-formula-mission1-adoption-pending"></a>
#### Proposed actor HP allocation — existing formula, mission1 adoption pending

For each wave and lane, use mission-specific armor-weighted EHP budget B once. Do not add generic ordinaryWaves or baseHP on top. For actor i, take authored baseHP h_i and armor a_i in BPS; w_i=h_i*10000/(10000-a_i). Allocate E_i=B*w_i/sum(w); rawHP_i=floor(E_i*(10000-a_i)/10000). Use exact rational integer arithmetic and final floor, with no intermediate rounding. Log actual sum(rawHP_i*10000/(10000-a_i)) and residual B-actual. Do not redistribute the remainder or claim exact equality after flooring. Reject a_i>=10000, missing inputs or nonpositive resolvedHP.

The selected three enemies all have armor500BPS, so this candidate reduces to exact baseHP proportions (scout400, gunner300; exterior one railbike480). Proposed values per actor:

| Waves | Interior roster | Scout rawHP each | Gunner rawHP each | Exterior railbike rawHP |
| --- | --- | ---: | ---: | ---: |
| 1–3 | SS | 494 | N/A | 592 |
| 4–6 | SSG | 449 | 336 | 741 |
| 7–9 | SSG | 538 | 404 | 889 |
| 10–12 | SSSG | 461 | 345 | 1037 |
| 13–15 | SSGG | 564 | 423 | 1185 |

Plan PowerShell arithmetic reproduced these values. This is arithmetic verification of a proposal, not gameplay balancing or consumer execution. Original lane target totals23400/14040 are EHP budgets before flooring, not HP to save directly. Damage rounding and armor apply separately at their defined execution boundary; EHP accounting does not remove actor armor in battle.

<a id="source-a78e5f3e47439a53-proposed-package-selection"></a>
#### Proposed package selection

Use existing proposal keys only in the isolated profile: proposal_aug_squad_fire, proposal_aug_train_fire, proposal_aug_barrier_support plus proposal_tactic_precision. Preserve documented effect scope: operator damage-only8%; locomotive/Gun8% including support and excluding Combat; shield-issued10%; single-target operator ACTIVE direct damage12%. Do not submit proposal keys as live catalog IDs. Consumer review must resolve additive/multiplicative categories, caps, timing and affected event categories against the original kit/rules documents before these become executable values. No new coefficients are introduced here.

<a id="source-a78e5f3e47439a53-runtime-contract-and-remaining-review"></a>
#### Runtime contract and remaining review

Direction next provides typed profile/wave/package mapping and real resource resolver export after selection; existing227 visual files need Unity import/load validation, not new image production. factionRef is unknown: do not invent a shipping faction ID from narrative or filenames. Server/Unity must agree whether the isolated schema uses explicit actor/enemy IDs without faction bonuses, with an explicit local unsupported-faction disposition, or requires an authored local faction binding. Existing strict required fields cannot be bypassed with an arbitrary string.

Unity owns execution system order/fixed50ms candidate, exact effect vocabulary/target tie-break/caps, complete actor/effect/input/PRNG checkpoint and actual scene. Server owns authenticated owned selection, original start idempotency and atomic localCombat/seed pin in existing batch. A rollback can discard tentative seed; accepted replay never reseeds or calls creation again. Finish→preview→explicit user claim uses actual server contracts and receipts.

Before adoption: obtain Unity's concrete execution scope and exact schema with every selected effect covered; reconcile unresolved timing/stacking/target/termination semantics; freeze typed manifest/profile/snapshot canonical projections and immutable versions. Then record the local-only selection distinctly from release approval and proceed through disposable core tests then a loopback authenticated allowlisted QA host. No blank fields may be treated as approved executable data.

Acceptance must cover actual15-wave combat, manual ability/ultimate behavior, terminal outcome through server finish, user claim and lobby refresh; same-run loading/restart/checkpoint recovery, lost start/finish/claim responses, concurrent admission one debit/seed, and no silent fallback/auto victory. Result-only video is labelled separately until actual played round-trip evidence exists.
<a id="source-a78e5f3e47439a53-source-correction-and-execution-policy-review--2026-09-11"></a>
#### Source correction and execution-policy review — 2026-09-11

Direct source check: combat-stats-v1.md258–260 already defines q=waveEHP/sum(baseHP/(1-armor)) and spawnHP=floor(baseHP*q). The allocation above is algebraically equivalent; it is not an entirely new formula. Adopting the complete mission1 profile/budget/package still remains pending. The health recovery document48–51 already records user-accepted TEST6/9/subsequent12s,50%HP/2s protection; fourth+12s needs no new coefficient decision.

Unity candidates now available in5f93/LASTRAIL/docs/handoffs: ce03-execution-policy-candidate-r1-20260911.md, ce03-checkpoint-candidate-r1-20260911.md and ce03-execution-vectors-candidate-r1-20260911.json. They define reviewable algorithms/state, not implemented gameplay. Twelve arithmetic examples were checked by Unity's Fraction assertions;18ordered-state examples are handwritten/unexecuted. None is runtime conformance.

New execution choices remain proposed: rear slots0/1/2 and front3/4; no wall-clock catchup in isolated local mode; one queued command with expiry/rejection; operator target fallback restarts full windup; enemy basic windup interruptible; train manual/basic share one action channel; status refresh and shield-cap allocation; immediate logical impacts with decorative projectile visuals; train0 irreversible failure wins same-tick final enemy kill. Later authored sequential-effects/full-cooldown/50ms/entry600ms/caps are candidate selections, not silently inherited runtime approvals. These are additional execution decisions, not included merely by accepting HP/package selection.

Server r3 must explicitly represent magnitude bonus cap and timing/channel policies. Full state checkpoint and storage ownership remain separate from admission. Proposal document hashes only pin reviewed text; implementation and executed vectors need separate evidence before readiness or durable ACK.
<a id="source-a78e5f3e47439a53-source-preserving-rank-mapping-fixed--2026-09-11"></a>
#### Source-preserving rank mapping fixed — 2026-09-11

Plan integration decision: isolated skillRank=exact owned skillTraining1..5, no offset/default/clamp. Directly verified DDL0003 default1/check1..5, actual acquisition INSERT1, four growth cost steps/index(level-1) and design R1..5. This preserves the stored meaning; it does not adopt the candidate rank magnitude scale or alter costs/HP/package/execution policies. Invalid/missing/mismatched source or changed revision rejects before debit; accepted replay preserves the original resolved rank/coefficient. No new user numeric approval is claimed. Consolidated producer input is Server r4 (rawSHA D91D5AEEA533FD9BE865060017DB1831D8CB4DED011F365D8B5ECCE9DCB83B44); runtime implementation remains separate.

<!-- source-body:source-a78e5f3e47439a53:end -->


<a id="source-0c064bbc63da0cf7"></a>
## Version-Code Content Release Policy

원출처: `WorkspaceRepo/docs/ops/content-release-channel-policy.md`  
원문 SHA-256: `6698e83ea1c54c511acd9a5fe8c575758a72da24a00d1fce9674656e9a76c00a`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-0c064bbc63da0cf7:start -->
<a id="source-0c064bbc63da0cf7-version-code-content-release-policy"></a>
### Version-Code Content Release Policy

<a id="source-0c064bbc63da0cf7-accepted-direction---2026-09-07"></a>
#### Accepted Direction - 2026-09-07

Latest correction: DEV is development testing only; its registered active build
is the environment's normal/stable build. INTERNAL/STABLE test-channel separation
and tester eligibility apply ONLY to PRD. DEV must not require tester grants or
offer an internal-to-stable promotion workflow. Its authentication, exact build
registration, minimum supported version and content verification remain intact.
This supersedes any earlier wording applying internal distribution to both targets.

- DEV and PRD remain environments; internal testing is a release status, not a
  third environment define. Resolve exact registered app versionCode and its
  compatibility information, never infer testing from greater version numbers.
- Registered stable builds receive compatible content. Internal builds require
  authentication and server-verified tester eligibility before content admission.
  Unknown builds fail closed; unsupported old builds receive update guidance.
- Pre-login bootstrap exposes safe status/update/auth-required information.
  Internal builds must defer downloads until tester admission; they must not
  prefetch stable content as a substitute. Auth endpoints must remain reachable
  for this handshake without opening gameplay/economic APIs before admission.
- Public R2 content URLs are not confidential just because the application gates
  entry. Private content delivery would require a separate access design.
- Promote the identical validated artifact from internal to stable. Minimum
  supported version changes are explicit separate release actions.
- Keep conditional pointer changes, audit evidence and compatible rollback;
  pin release identity during a download/startup to avoid mixed bundles.
- Normal Editor testing uses local Addressables. Packaged Android uses approved
  remote catalog. Local content mode is not an auth/economy bypass or remote QA.

<a id="source-0c064bbc63da0cf7-pipeline-and-pending-details"></a>
#### Pipeline And Pending Details

<a id="source-0c064bbc63da0cf7-2026-09-08-environment-specific-publication-decision"></a>
##### 2026-09-08 Environment-Specific Publication Decision

User explicitly chooses DEV upload AND immediate activation after verification;
PRD uploads only, with activation managed in Admin and app distribution in Play
Console. This supersedes manual-only DEV activation guidance, not validation.

- Build and verify content and matching signed player artifact first. Upload
  immutable catalog/hash/bundles to the selected environment; verify HTTPS bytes
  and dependencies before any activation. Never publish signing keys or APK/AAB
  to public content storage as an implied side effect.
- DEV automatically registers/enables the exact version/content through the
  existing authenticated, permission-checked CAS contract, including explicit
  first-LEGACY confirmation. Conflicts or failed verification keep the old state.
  Do not automatically bump minimum supported version or disable older rows.
- PRD pipeline cannot activate/promote/change minimum/current or submit to Play.
  Provide verified artifact/release evidence for human Admin/Play actions.
- Track local build, upload and activation as distinct outcomes. Retry the same
  verified release idempotently, not a new build/code on every upload retry.
- Require configured approved bucket/host/credentials; missing configuration fails
  closed. Use the existing publisher/contracts and normal tool approval, never
  an alternative route around prior security-review denial.

Implementation delegated; this decision alone does not claim any actual upload
or activation, nor select an app version to execute immediately. PRD infrastructure
provisioning, secrets, store release and server deployment remain separately scoped.

Content build, immutable upload and byte verification must be separated from
activation/promotion. Existing A05 remains unchanged. Version_code_cNNN naming
was proposed, not a mandate to rename frozen artifacts. Final schema, numbering
reservation, legacy JSON transition and tester permission source require owner
contract design. Never hardcode fictitious tester credentials or release values.

<a id="source-0c064bbc63da0cf7-work-authorization"></a>
#### Work Authorization

User approved related local Server/Admin/Unity implementation and tests. New PRD
changes, live tester grants, store/signing/APK/device/video work are not implied.
Previously approved DEV publication remains subject to normal tool review; do
not bypass the existing rejected upload or cross-task disclosure restrictions.

This planning specification is local/uncommitted; see owner handoffs for actual
implementation, deployment and verification results.

<!-- source-body:source-0c064bbc63da0cf7:end -->


<a id="source-eca6ede5f64bdf03"></a>
## Customer Support Escalation Workflow

원출처: `WorkspaceRepo/docs/ops/customer-support-escalation-workflow.md`  
원문 SHA-256: `8c0580a279faa1a18249137aaa8948a199540c809a60225eb1a0c671d20ebc67`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-eca6ede5f64bdf03:start -->
<a id="source-eca6ede5f64bdf03-customer-support-escalation-workflow"></a>
### Customer Support Escalation Workflow

<a id="source-eca6ede5f64bdf03-purpose"></a>
#### Purpose

This document defines when LAST RAIL customer support should resolve a report with a safe response, escalate it for review, or block action until better evidence exists.

Launch support remains read-only. Escalation creates an evidence package and review state, not a repair or mutation.

<a id="source-eca6ede5f64bdf03-severity"></a>
#### Severity

| Severity | Korean Label | Meaning | Launch Action |
| --- | --- | --- | --- |
| `info` | 안내 | Server records show expected behavior. | Respond with an approved template. |
| `needs_review` | 검토 필요 | Records are incomplete or may disagree. | Escalate to operator review. |
| `high_risk` | 고위험 | Economy, reward, integrity, or account ownership impact is possible. | Escalate to owner or senior operator review. |
| `blocked` | 처리 보류 | Support cannot safely decide from available records. | Do not promise repair; request more context or wait for tooling. |

<a id="source-eca6ede5f64bdf03-escalation-categories"></a>
#### Escalation Categories

| Category | Korean Label | Default Severity |
| --- | --- | --- |
| `reward_claim_missing_ledger` | 보상 수령 장부 누락 | `high_risk` |
| `reward_preview_without_claim` | 보상 미리보기 후 수령 누락 | `needs_review` |
| `offline_state_conflict` | 오프라인 보상 상태 충돌 | `high_risk` |
| `offline_claim_duplicate_mismatch` | 오프라인 중복 수령 불일치 | `high_risk` |
| `story_unlock_missing` | 스토리 해금 누락 | `needs_review` |
| `gacha_guarantee_conflict` | 모집 보장 상태 충돌 | `high_risk` |
| `integrity_restricted_reward` | 무결성 보상 제한 | `high_risk` |
| `account_identity_conflict` | 계정 식별 충돌 | `blocked` |
| `narrative_content_missing` | 내러티브 콘텐츠 누락 | `needs_review` |
| `expected_behavior` | 정상 동작 안내 | `info` |

<a id="source-eca6ede5f64bdf03-evidence-package"></a>
#### Evidence Package

Every escalation should collect a compact evidence package.

Required fields:

```text
caseType:
severity:
category:
accountId:
primaryReferenceId:
observedStatus:
idempotencyStatus:
serverEvidence:
playerFacingSummary:
requestedAction:
```

Optional reference fields:

- `runId`
- `rewardPreviewId`
- `rewardClaimId`
- `offlineRewardClaimId`
- `gachaBatchId`
- `economyTransactionId`
- `rewardTransactionId`
- `accountMutationId`
- `currencyLedgerEntryIds`
- `storyProgressId`
- `lastProgressRunId`
- `integrityEventId`
- `opsErrorLogId`
- `supportTicketId`

Do not include:

- access tokens
- refresh tokens
- raw auth headers
- raw request bodies
- raw RNG material
- RNG seed or seed preimage
- per-candidate probability
- full weight lists
- raw multiplier bps
- internal reward calculation details
- localized dialogue full text

<a id="source-eca6ede5f64bdf03-launch-actions"></a>
#### Launch Actions

Allowed launch actions:

- `respond_with_template`
- `request_more_player_context`
- `escalate_to_operator_review`
- `escalate_to_owner_review`
- `mark_as_known_issue_candidate`
- `attach_read_only_evidence`

Not allowed at launch:

- `manual_reward_grant`
- `currency_correction`
- `offline_reward_timestamp_reset`
- `story_progress_mutation`
- `unlock_mutation`
- `gacha_reroll`
- `reward_repair`
- `economy_repair`
- `narrative_editing`
- `localization_publishing`

<a id="source-eca6ede5f64bdf03-case-rules"></a>
#### Case Rules

<a id="source-eca6ede5f64bdf03-expected-behavior"></a>
##### Expected Behavior

Use severity `info` when server records show expected behavior.

Examples:

- Offline reward is below minimum claim time.
- Offline reward is capped by storage limit.
- Duplicate replay returned the stored result.
- First clear reward was already consumed for the same operation and risk.
- Lower operation clear did not regress higher story progress.

Action:

- Use an approved response template.
- Do not escalate unless the player provides new evidence.

<a id="source-eca6ede5f64bdf03-reward-claim-missing-ledger"></a>
##### Reward Claim Missing Ledger

Use category `reward_claim_missing_ledger` when a successful reward claim exists but currency or economy ledger evidence is missing.

Required evidence:

- reward claim id
- reward transaction id
- economy transaction id
- expected ledger presence status
- idempotency status
- affected currency codes without raw internal formulas

Action:

- Escalate to owner or senior operator review.
- Do not issue manual compensation from launch support tools.

<a id="source-eca6ede5f64bdf03-reward-preview-without-claim"></a>
##### Reward Preview Without Claim

Use category `reward_preview_without_claim` when a reward preview exists but no claim record exists.

Required evidence:

- reward preview id
- run id
- preview status
- claim status if any
- idempotency status
- integrity disposition

Action:

- Escalate to operator review if the player cannot retry claim safely.
- If a supported retry path exists, guide the player to retry.

<a id="source-eca6ede5f64bdf03-offline-state-conflict"></a>
##### Offline State Conflict

Use category `offline_state_conflict` when offline reward state and claim records disagree.

Examples:

- State revision changed without a matching claim.
- `last_offline_reward_claim_at` moved forward after an empty claim.
- Stored story progress id is unknown.
- Claim uses a baseline inconsistent with state.

Required evidence:

- offline reward state source
- last claim baseline
- state revision
- offline reward claim id if any
- reward/economy transaction ids
- idempotency status

Action:

- Escalate to owner review.
- Do not reset offline state at launch.

<a id="source-eca6ede5f64bdf03-offline-duplicate-mismatch"></a>
##### Offline Duplicate Mismatch

Use category `offline_claim_duplicate_mismatch` when duplicate replay evidence does not match the stored claim.

Required evidence:

- idempotency operation
- duplicate replay status
- offline reward claim id
- ledger write count
- state revision before/after if visible

Action:

- Escalate as high risk.
- Do not advise the player to repeatedly retry if writes appear inconsistent.

<a id="source-eca6ede5f64bdf03-story-unlock-missing"></a>
##### Story Unlock Missing

Use category `story_unlock_missing` when a full victory should advance story progress but no unlock/progress evidence exists.

Required evidence:

- run id
- operation id
- outcome
- cleared waves
- current story progress id
- unlocked operations
- progression ledger presence

Action:

- Escalate to operator review.
- Do not mutate story progress from support at launch.

<a id="source-eca6ede5f64bdf03-gacha-guarantee-conflict"></a>
##### Gacha Guarantee Conflict

Use category `gacha_guarantee_conflict` when guarantee type and pity state appear inconsistent.

Required evidence:

- gacha batch id
- banner id
- pull count
- guarantee type
- pity counter status
- duplicate replay status
- ledger result presence

Action:

- Escalate to owner or senior operator review.
- Do not expose raw probability, RNG, seed, or full weight lists.
- Do not reroll or replace recruitment result at launch.

<a id="source-eca6ede5f64bdf03-integrity-restricted-reward"></a>
##### Integrity Restricted Reward

Use category `integrity_restricted_reward` when a reward or progress action is blocked or limited by integrity disposition.

Required evidence:

- integrity disposition
- affected run/reward/offline/gacha reference ids
- ops error code if available
- whether restriction is soft anomaly or hard restriction

Action:

- Escalate to owner or integrity review.
- Use neutral player-facing language.

<a id="source-eca6ede5f64bdf03-account-identity-conflict"></a>
##### Account Identity Conflict

Use category `account_identity_conflict` when support cannot safely determine account ownership.

Examples:

- Player claims progress belongs to another account.
- Guest continuation produced multiple candidate accounts.
- Platform proof and server account status disagree.

Required evidence:

- account ids involved
- public player display names if available
- account status
- created/last active timestamps
- non-secret auth identity references

Action:

- Mark as `blocked`.
- Request more account ownership context.
- Do not merge accounts or move rewards at launch.

<a id="source-eca6ede5f64bdf03-narrative-content-missing"></a>
##### Narrative Content Missing

Use category `narrative_content_missing` when story progress exists but briefing, callout, codex, or localization content is missing.

Required evidence:

- mission id
- story progress id
- narrative version
- content status
- localization status
- fixture status

Action:

- Escalate to content/localization review.
- Do not treat narrative content status as reward or unlock truth.

<a id="source-eca6ede5f64bdf03-admin-read-only-fields"></a>
#### Admin Read-Only Fields

Future Admin support surfaces should expose:

- case type
- severity
- escalation category
- recommended launch action
- support permission
- primary reference id
- related reference ids
- idempotency status
- ledger presence status
- integrity disposition
- safe player-facing summary availability
- mutation controls disabled status

Suggested permission:

- `SUPPORT_READ`

Future permissions:

- `SUPPORT_ESCALATION_READ`
- `SUPPORT_ESCALATION_WRITE`
- `SUPPORT_REPAIR_APPROVE`

Only `SUPPORT_READ` is in launch read-only scope.

<a id="source-eca6ede5f64bdf03-player-facing-escalation-language"></a>
#### Player-Facing Escalation Language

Use:

- "서버 기록 기준으로 추가 확인이 필요한 상태입니다."
- "기록 불일치 가능성이 있어 운영 검토로 전달하겠습니다."
- "현재 단계에서는 임의 보정 없이 저장된 기록을 기준으로 확인합니다."
- "보상이나 진행도 변경은 검토 절차가 끝난 뒤 별도 기준에 따라 처리됩니다."

Avoid:

- "보상해 드리겠습니다" before an approved repair workflow exists.
- "확률상 문제가 없습니다" with raw probability detail.
- "다시 뽑아 드리겠습니다" for gacha cases.
- "기록을 수정했습니다" unless an audited mutation actually happened.

<a id="source-eca6ede5f64bdf03-rollout-order"></a>
#### Rollout Order

1. Keep support escalation as documentation and Admin placeholder only.
2. Add Server read-only support account summary endpoint.
3. Add Admin read-only support account page.
4. Add support escalation status placeholders.
5. Add ops error log correlation.
6. Define audited repair policy.
7. Implement repair workflows only after approval, audit, and permission design.

<a id="source-eca6ede5f64bdf03-open-decisions"></a>
#### Open Decisions

- Whether escalation records need their own table or can start as support ticket metadata.
- Whether support ticket ids are required for every escalation.
- Whether `SUPPORT_ESCALATION_WRITE` should be available to OPERATOR or OWNER only.
- How long evidence package references should be retained.
- Whether player-facing response templates should be copied manually or sent through Admin.
- Which integrity cases require a separate security review workflow.

<!-- source-body:source-eca6ede5f64bdf03:end -->


<a id="source-de295d7c8db198f2"></a>
## Customer Support Response Templates

원출처: `WorkspaceRepo/docs/ops/customer-support-response-templates.md`  
원문 SHA-256: `3c423bd3304d98898cd970ec91c108f1a4f0ab69e4c00921708ddb60e6c19c51`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-de295d7c8db198f2:start -->
<a id="source-de295d7c8db198f2-customer-support-response-templates"></a>
### Customer Support Response Templates

<a id="source-de295d7c8db198f2-purpose"></a>
#### Purpose

This document defines launch-scope customer support response templates for LAST RAIL.

The templates are written for support staff. They separate:

- Internal support checks: what the operator verifies in Admin.
- Player-facing response: safe wording that can be sent to the player.
- Escalation criteria: when the case should move beyond read-only support.

Support staff should not expose raw security, economy, probability, RNG, or token details.

<a id="source-de295d7c8db198f2-global-rules"></a>
#### Global Rules

Use these rules for every support response:

- Base answers on server records, not screenshots or client-local state.
- Do not promise manual compensation unless an approved repair workflow exists.
- Do not expose access tokens, refresh tokens, raw auth headers, raw request bodies, RNG seeds, seed preimages, per-candidate probabilities, full weight tables, raw multiplier bps, or internal exploit-sensitive calculations.
- Keep stable backend ids in internal notes. Share them with players only when needed as a support reference.
- Describe recruited operators as rescued specialists, defectors, or signal responders. Do not describe them as purchased goods.
- If the evidence is incomplete, say the case is being escalated for record review instead of guessing.

<a id="source-de295d7c8db198f2-case-type-labels"></a>
#### Case Type Labels

| Code | Korean Label | Support Use |
| --- | --- | --- |
| `account_auth` | 계정/인증 | Session, guest account, account status reports |
| `run_completion` | 출격 완료 | Finish, outcome, cleared waves, unlock reports |
| `run_reward` | 출격 보상 | Reward preview, claim, first clear reports |
| `offline_reward` | 오프라인 보상 | Offline preview/claim/cap reports |
| `story_progress` | 스토리 진행 | Operation unlock and story progress reports |
| `gacha_recruitment` | 모집/합류 | Gacha pull, guarantee, pity reports |
| `integrity_restriction` | 무결성 제한 | Restricted reward or suspicious activity reports |

<a id="source-de295d7c8db198f2-accountauth"></a>
#### Account/Auth

<a id="source-de295d7c8db198f2-internal-checks"></a>
##### Internal Checks

- Confirm account exists and is active.
- Check account created time and latest authenticated activity.
- Check whether the report is about guest continuation, session expiry, or account reset.
- Check whether related story/reward records belong to the same authenticated account context.

<a id="source-de295d7c8db198f2-player-facing-template"></a>
##### Player-Facing Template

서버 기록 기준으로 계정 상태를 확인했습니다. 현재 확인된 접속 기록과 계정 상태를 기준으로 이어서 조사하겠습니다. 계정 보상이나 진행도는 기기 화면이 아니라 서버에 저장된 계정 기록을 기준으로 확인됩니다.

<a id="source-de295d7c8db198f2-escalate-when"></a>
##### Escalate When

- Account is missing but player has purchase or platform proof.
- Multiple account records appear to represent one player.
- Auth session state conflicts with account status.
- Support cannot verify account ownership from available records.

<a id="source-de295d7c8db198f2-run-completion"></a>
#### Run Completion

<a id="source-de295d7c8db198f2-internal-checks-1"></a>
##### Internal Checks

- Confirm `runId`, operation id, risk tier, outcome, cleared waves, and finished time.
- Check idempotency record for duplicate finish submit.
- Check whether `VICTORY` with 15 cleared waves occurred.
- Check story progress mutation and unlock records.
- Check reward preview id created at finish time.

<a id="source-de295d7c8db198f2-player-facing-template-1"></a>
##### Player-Facing Template

출격 결과는 서버 완료 기록을 기준으로 확인됩니다. 일반 스토리 진행은 승리와 전체 공세 클리어가 함께 기록된 경우에 갱신됩니다. 중복 제출된 완료 요청은 같은 결과를 다시 확인하는 방식이며, 진행도나 보상을 다시 적용하지 않습니다.

<a id="source-de295d7c8db198f2-escalate-when-1"></a>
##### Escalate When

- Full victory exists but no story progress or unlock record exists.
- Finish record exists but reward preview was not created.
- Run status, final result, and idempotency records disagree.
- Integrity disposition blocks completion but no support-visible reason exists.

<a id="source-de295d7c8db198f2-run-reward"></a>
#### Run Reward

<a id="source-de295d7c8db198f2-internal-checks-2"></a>
##### Internal Checks

- Confirm reward preview id and reward claim id.
- Check reward transaction, economy transaction, and currency ledger entries.
- Check first clear state for `accountId + operationId + risk`.
- Check whether the reward claim was a duplicate replay.
- Check integrity disposition.

<a id="source-de295d7c8db198f2-player-facing-template-2"></a>
##### Player-Facing Template

출격 보상은 서버가 완료 기록, 위험도, 클리어 상태, 첫 클리어 여부를 기준으로 계산한 보상 계획을 따릅니다. 첫 클리어 보상은 완료 처리 시점에 확인되며, 같은 요청이 다시 들어온 경우에는 기존 처리 결과를 다시 보여줍니다.

<a id="source-de295d7c8db198f2-first-clear-template"></a>
##### First Clear Template

첫 클리어 보상은 작전과 위험도 단위로 서버에서 확인됩니다. 이미 해당 작전/위험도의 첫 완료 기록이 있으면 같은 첫 클리어 보상이 다시 생성되지 않습니다.

<a id="source-de295d7c8db198f2-escalate-when-2"></a>
##### Escalate When

- Reward claim exists but ledger entries are missing.
- Reward preview exists but claim cannot be recovered or retried.
- First clear state conflicts with stored run completion.
- Integrity restriction requires manual review.

<a id="source-de295d7c8db198f2-offline-reward"></a>
#### Offline Reward

<a id="source-de295d7c8db198f2-internal-checks-3"></a>
##### Internal Checks

- Confirm offline reward state row source.
- Check `last_offline_reward_claim_at`, `last_story_progress_id`, revision, and updated time.
- Check preview status and claim status.
- Check claimable seconds/hours, storage cap hours, minimum claim seconds, and max claimable hours.
- Check latest offline reward transaction and economy ledger.
- Check idempotency record for duplicate claim.

<a id="source-de295d7c8db198f2-not-claimable-template"></a>
##### Not Claimable Template

현재 서버 기록 기준으로 수령 가능한 오프라인 보상이 없습니다. 오프라인 보상은 최소 누적 시간과 보관 한도 기준을 함께 적용하며, 기준 시간보다 짧은 경우에는 수령 처리되지 않습니다.

<a id="source-de295d7c8db198f2-smaller-than-expected-template"></a>
##### Smaller Than Expected Template

오프라인 보상은 서버에 저장된 스토리 진행도와 마지막 수령 시각, 보관 한도를 기준으로 계산됩니다. 장시간 미접속하더라도 보관 한도를 초과한 시간은 기본 수령량에 포함되지 않을 수 있습니다.

<a id="source-de295d7c8db198f2-duplicate-claim-template"></a>
##### Duplicate Claim Template

같은 수령 요청이 중복으로 들어온 경우에는 서버가 기존 처리 결과를 다시 확인합니다. 이 경우 보상이 한 번 더 지급되거나 수령 기준 시간이 다시 변경되지 않습니다.

<a id="source-de295d7c8db198f2-escalate-when-3"></a>
##### Escalate When

- Successful claim exists but currency ledger is missing.
- State revision changes without a matching claim transaction.
- Empty claim advanced `last_offline_reward_claim_at`.
- Stored story progress is unknown or policy is missing.
- Clock tamper or state conflict is suspected.

<a id="source-de295d7c8db198f2-story-progressunlock"></a>
#### Story Progress/Unlock

<a id="source-de295d7c8db198f2-internal-checks-4"></a>
##### Internal Checks

- Confirm highest story progress id and highest cleared operation id.
- Check cleared operation ids and unlocked operations.
- Check last progress run id.
- Check progression ledger entries.
- Check narrative version and missing content status.

<a id="source-de295d7c8db198f2-player-facing-template-3"></a>
##### Player-Facing Template

스토리 진행도와 작전 해금은 서버 완료 기록을 기준으로 확인됩니다. 이미 더 높은 진행도가 저장된 경우, 낮은 작전의 재클리어로 진행도가 되돌아가지는 않습니다.

<a id="source-de295d7c8db198f2-missing-narrative-template"></a>
##### Missing Narrative Template

진행도 기록과 내러티브 표시 데이터는 서로 분리되어 있습니다. 진행도는 서버 기록으로 확인하고, 표시 문구나 브리핑 누락은 콘텐츠 상태로 별도 확인하겠습니다.

<a id="source-de295d7c8db198f2-escalate-when-4"></a>
##### Escalate When

- Full victory should advance progress but no update exists.
- Unlock record is missing for the next expected operation.
- Narrative content is missing for an unlocked operation.
- Server progress and progression ledger disagree.

<a id="source-de295d7c8db198f2-gacharecruitment"></a>
#### Gacha/Recruitment

<a id="source-de295d7c8db198f2-internal-checks-5"></a>
##### Internal Checks

- Confirm gacha batch id, banner id, pull count, and banner status.
- Check guarantee type and pity counters.
- Check idempotency record for duplicate pull.
- Check account item/operator ledger references.
- Check sanitized selection reason if available.

<a id="source-de295d7c8db198f2-player-facing-template-4"></a>
##### Player-Facing Template

모집 결과는 서버에 저장된 모집 기록을 기준으로 확인됩니다. 중복 요청은 같은 모집 결과를 다시 보여주는 방식이며, 결과를 새로 뽑거나 다시 계산하지 않습니다.

<a id="source-de295d7c8db198f2-guarantee-template"></a>
##### Guarantee Template

보장 여부는 서버의 보장 상태와 모집 기록을 기준으로 확인됩니다. 내부 확률표나 난수 정보는 공개하지 않지만, 보장 조건이 적용되었는지는 기록으로 확인할 수 있습니다.

<a id="source-de295d7c8db198f2-escalate-when-5"></a>
##### Escalate When

- Guarantee type conflicts with pity counter state.
- Pull batch exists but item/operator ledger is missing.
- Duplicate replay returned a different result.
- Banner state changed during a claim window.

<a id="source-de295d7c8db198f2-integrity-restriction"></a>
#### Integrity Restriction

<a id="source-de295d7c8db198f2-internal-checks-6"></a>
##### Internal Checks

- Confirm integrity disposition and related ops error code.
- Check affected run/reward/offline/gacha reference ids.
- Check whether the restriction is soft anomaly or hard restriction.
- Check suspicious replay or tamper signals only through sanitized status.

<a id="source-de295d7c8db198f2-player-facing-template-5"></a>
##### Player-Facing Template

일부 기록은 서버 무결성 확인 절차에 따라 추가 검토가 필요할 수 있습니다. 보상이나 진행도 처리에 제한이 적용된 경우, 서버 기록을 기준으로 확인 후 안내드리겠습니다.

<a id="source-de295d7c8db198f2-escalate-when-6"></a>
##### Escalate When

- Hard restriction blocks reward or progress.
- Integrity status is missing but reward is restricted.
- Suspicious replay conflicts with normal duplicate replay records.
- Operator decision is needed before a player response.

<a id="source-de295d7c8db198f2-internal-note-format"></a>
#### Internal Note Format

Support notes should use this compact structure:

```text
caseType:
accountId:
primaryReferenceId:
observedStatus:
serverEvidence:
playerFacingSummary:
escalationNeeded:
```

Do not paste raw auth headers, raw request bodies, tokens, RNG material, or full probability tables into support notes.

<a id="source-de295d7c8db198f2-safe-explanation-snippets"></a>
#### Safe Explanation Snippets

Use these short snippets when a full template is too long:

- 서버 기록 기준으로 확인했습니다.
- 중복 요청은 기존 처리 결과를 다시 보여주는 방식입니다.
- 수령 가능 시간과 보관 한도가 함께 적용됩니다.
- 첫 클리어 보상은 완료 처리 시점 기준으로 확인됩니다.
- 진행도와 표시 문구는 분리되어 확인됩니다.
- 내부 확률표와 난수 정보는 공개하지 않습니다.
- 추가 검토가 필요한 기록으로 확인되어 운영 검토로 전달하겠습니다.

<a id="source-de295d7c8db198f2-open-decisions"></a>
#### Open Decisions

- Whether response templates should be stored in Admin as static content or generated from a backend support contract.
- Whether support case notes should require a support ticket id.
- Which escalation types require OWNER approval.
- Whether support can send templated responses directly from Admin or only copy reviewed text.
- Whether each template needs separate Korean, English, and Japanese localization keys before launch.

<!-- source-body:source-de295d7c8db198f2:end -->


<a id="source-1351fc3086aa0d11"></a>
## Customer Support Workflow

원출처: `WorkspaceRepo/docs/ops/customer-support-workflow.md`  
원문 SHA-256: `436fed1490810507be47fe6648c4833dc536c846eae842b64e018a3efefa53ba`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-1351fc3086aa0d11:start -->
<a id="source-1351fc3086aa0d11-customer-support-workflow"></a>
### Customer Support Workflow

<a id="source-1351fc3086aa0d11-purpose"></a>
#### Purpose

This document defines the launch-scope customer support workflow for LAST RAIL.

The goal is to let support staff answer player reports from server-owned records without opening repair or mutation tools too early.

<a id="source-1351fc3086aa0d11-ownership"></a>
#### Ownership

- Server owns account truth, run truth, reward truth, gacha truth, story progress truth, unlock truth, and idempotency records.
- Admin owns read-only operational visibility and support triage surfaces.
- Unity owns player-facing presentation and local debug context, but does not own durable truth.
- Customer support uses Admin read-only data first. Manual correction is a later controlled workflow.

<a id="source-1351fc3086aa0d11-launch-scope"></a>
#### Launch Scope

Launch customer support should support read-only investigation for:

- Account/auth issues
- Run completion reports
- Run reward preview or claim reports
- Offline reward preview or claim reports
- First clear reward reports
- Story progress or unlock reports
- Gacha result or guarantee reports
- Integrity-restricted reward reports

Launch customer support should not include:

- Manual currency grants
- Reward repair buttons
- Offline reward timestamp reset
- Story progress mutation
- Unlock mutation
- Gacha result reroll
- Localization publishing
- Narrative editing
- Economy correction

<a id="source-1351fc3086aa0d11-support-case-types"></a>
#### Support Case Types

<a id="source-1351fc3086aa0d11-accountauth"></a>
##### Account/Auth

Common reports:

- Guest account cannot continue
- Session expired
- Account appears reset
- Player cannot access reward or story state

Read-only evidence:

- `accountId`
- account status
- created time
- auth session status
- latest successful authenticated request
- device/session diagnostic context if available

Do not expose:

- access tokens
- refresh tokens
- raw auth headers
- secret material

<a id="source-1351fc3086aa0d11-run-completion"></a>
##### Run Completion

Common reports:

- Run finished but result did not update
- Victory did not unlock next operation
- Failure reward looked wrong
- Duplicate submit returned unexpected response

Read-only evidence:

- `runId`
- `previewId`
- operation id
- risk tier
- outcome
- cleared waves
- finished time
- idempotency operation/key presence
- story progress mutation presence
- reward preview id

Support interpretation:

- Only `VICTORY` with 15 cleared waves advances ordinary story progress.
- Duplicate finish replay should not reapply story progress, first clear, or rewards.
- Lower operation clear should not regress higher stored progress.

<a id="source-1351fc3086aa0d11-run-reward"></a>
##### Run Reward

Common reports:

- Reward missing after run
- First clear bonus missing
- Same risk appears to reward differently
- Claim failed after preview

Read-only evidence:

- `rewardPreviewId`
- reward claim id
- reward transaction id
- economy transaction id
- currency ledger entries
- first clear state for `accountId + operationId + risk`
- integrity disposition
- idempotency replay state

Support interpretation:

- First clear is risk-scoped.
- First clear is consumed at accepted run finish preview generation time.
- If a preview exists but claim failed, recovery should focus on stored preview/claim state, not recalculating first clear eligibility.
- Unity-supplied reward amount or multiplier fields are not trusted.

<a id="source-1351fc3086aa0d11-offline-reward"></a>
##### Offline Reward

Common reports:

- Offline reward not claimable
- Offline reward was smaller than expected
- Offline claim duplicated
- Player returned after long absence and expected uncapped reward

Read-only evidence:

- `offlineRewardClaimId`
- `claimStatus`
- `last_offline_reward_claim_at`
- `last_story_progress_id`
- `revision`
- preview status
- claimable seconds/hours
- storage cap hours
- minimum claim seconds
- max claimable hours
- reward transaction id
- economy transaction id
- currency ledger entries
- idempotency operation/key presence

Support interpretation:

- Offline rewards use server-stored story progress.
- Risk tier does not directly multiply offline rewards.
- 10 minutes or less than the launch minimum is not claimable.
- Empty claims should not advance `last_offline_reward_claim_at`.
- Duplicate replay should not grant rewards again or increment state revision.
- Existing accounts without an offline state row may use account creation time fallback until claim creates state.

<a id="source-1351fc3086aa0d11-story-progressunlock"></a>
##### Story Progress/Unlock

Common reports:

- Next operation did not unlock
- Story progress appears stuck
- Offline reward multiplier did not increase
- Narrative or briefing appears missing

Read-only evidence:

- highest story progress id
- highest cleared operation id
- cleared operation ids
- unlocked operations
- last progress run id
- narrative version
- narrative/localization content status
- progression ledger entries

Support interpretation:

- Server durable story progress is the source of truth.
- Client-supplied story progress, unlocked operations, and narrative flags are not trusted.
- Offline reward multiplier uses server-stored highest story progress.
- Narrative version is diagnostic metadata, not reward or unlock input.

<a id="source-1351fc3086aa0d11-gacharecruitment"></a>
##### Gacha/Recruitment

Common reports:

- Guarantee did not trigger
- Duplicate pull looked different
- Player questions recruitment result
- Banner unavailable or inactive

Read-only evidence:

- gacha batch id
- banner id/status
- pull count
- guarantee type
- pity counter state
- idempotency replay state
- reward/account item ledger entries
- sanitized selection reason if available

Support interpretation:

- Hard guarantee is represented by `guaranteeType`.
- Duplicate replay should return stored result, not rerun selection.
- Raw RNG material, seed preimage, per-candidate probabilities, and full weight lists are not support-facing data.
- Recruits should be described as rescued specialists, defectors, or signal responders, not purchased goods.

<a id="source-1351fc3086aa0d11-support-decision-matrix"></a>
#### Support Decision Matrix

| Symptom | Primary Evidence | Likely Outcome |
| --- | --- | --- |
| Reward preview exists, claim missing | reward preview + idempotency + claim records | Reopen/retry claim path candidate |
| Claim exists, ledger missing | reward transaction + economy transaction + ledger | Escalate as economy mutation mismatch |
| Duplicate claim report | idempotency record + claim id | Explain duplicate replay if no extra ledger write |
| Offline claim too small | story progress + cap + last claim time | Explain cap/minimum/server baseline |
| First clear missing | first clear record + run finish timing | Check consumed-at-finish policy |
| Story unlock missing | run final result + story progress + unlocks | Escalate if full victory with no progress/update |
| Gacha guarantee dispute | batch + guarantee type + pity state | Explain stored result/guarantee state |
| Integrity restriction | integrity disposition + ops error code | Escalate with read-only evidence |

<a id="source-1351fc3086aa0d11-admin-surface-candidates"></a>
#### Admin Surface Candidates

<a id="source-1351fc3086aa0d11-account-support-summary"></a>
##### Account Support Summary

Candidate route:

- `/support/accounts/:accountId`

Read-only sections:

- account status
- recent runs
- run reward previews/claims
- offline reward state/claims
- story progress/unlocks
- gacha batches
- idempotency records
- integrity restrictions
- recent ops error logs

<a id="source-1351fc3086aa0d11-run-support-context"></a>
##### Run Support Context

Candidate fields:

- run id
- operation id
- risk tier
- outcome
- cleared waves
- reward preview id
- reward claim id
- first clear consumed status
- story progress mutation id
- idempotency replay status
- integrity disposition

<a id="source-1351fc3086aa0d11-offline-reward-support-context"></a>
##### Offline Reward Support Context

Candidate fields:

- offline reward claim id
- claim status
- last claim baseline
- state revision
- story progress id
- claimable duration
- storage cap
- reward transaction id
- economy transaction id
- duplicate replay status

<a id="source-1351fc3086aa0d11-gacha-support-context"></a>
##### Gacha Support Context

Candidate fields:

- gacha batch id
- banner id
- pull count
- guarantee type
- pity counters
- sanitized selection reason
- item/operator ledger references
- duplicate replay status

<a id="source-1351fc3086aa0d11-permissions"></a>
#### Permissions

Launch recommended permissions:

- `SUPPORT_READ`: read-only support case context
- `PLAYERS_READ`: existing player lookup
- `RUNS_READ`: existing run visibility
- `STORY_PROGRESS_READ`: story progress visibility
- future `OFFLINE_REWARD_READ`: offline reward state/claim visibility
- future `GACHA_READ`: gacha batch/pity visibility

Launch should not grant support users:

- reward mutation
- economy mutation
- story progress mutation
- unlock mutation
- offline reward timestamp reset
- gacha reroll or repair

<a id="source-1351fc3086aa0d11-player-facing-response-guidelines"></a>
#### Player-Facing Response Guidelines

Use:

- "서버 기록 기준으로 확인했습니다."
- "수령 가능 시간과 보관 한도 기준으로 계산되었습니다."
- "중복 요청은 같은 처리 결과를 다시 보여주는 방식입니다."
- "첫 클리어 보상은 완료 처리 시점 기준으로 확인됩니다."

Avoid:

- raw internal ids unless needed for support ticket reference
- raw probability or weight details
- RNG seed or seed preimage
- token/session details
- exact exploit-sensitive formulas beyond public policy language
- language implying characters or rescued people are purchased goods

<a id="source-1351fc3086aa0d11-ops-error-code-candidates"></a>
#### Ops Error Code Candidates

Support-facing labels should preserve stable backend codes and add Korean labels in Admin.

- `reward_plan_mismatch`
- `reward_multiplier_missing`
- `reward_plan_unavailable`
- `reward_integrity_restricted`
- `offline_reward_not_claimable`
- `offline_reward_state_conflict`
- `offline_reward_claim_conflict`
- `offline_reward_plan_mismatch`
- `economy_mutation_conflict`
- `story_progress_state_conflict`
- `narrative_content_missing`
- `narrative_version_mismatch`
- `gacha_temporarily_unavailable`
- `banner_inactive`
- `invalid_pull_count`
- `duplicate_replay`

<a id="source-1351fc3086aa0d11-rollout-order"></a>
#### Rollout Order

1. Define CS read-only workflow and terminology.
2. Add Server support read model contract for account support summary.
3. Add Admin support skeleton with read-only placeholders.
4. Connect Admin to read-only Server support endpoints.
5. Add ops error log correlation for support case types.
6. Define audited repair workflow separately.
7. Only then consider controlled mutation tools.

<a id="source-1351fc3086aa0d11-open-decisions"></a>
#### Open Decisions

- Whether support should have a dedicated `/support` Admin section or be integrated into player/run detail pages first.
- Whether `SUPPORT_READ` should be a new permission or composed from existing read permissions.
- How long support timelines and recent claim/run/gacha history should be retained.
- Whether support ticket ids should be stored in Admin audit logs.
- Which repair flows require OWNER approval.
- Whether player-facing explanations should be generated from templates or maintained manually.

<!-- source-body:source-1351fc3086aa0d11:end -->


<a id="source-39523bef0fe14920"></a>
## Computed scenarios — design inputs only

원출처: `WorkspaceRepo/docs/ops/economy-model-20260908/tables.md`  
원문 SHA-256: `b3c9017f5634f1af916ddd88c5c49f843aa8d30162bdda8dca8fd6da42597b0f`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-39523bef0fe14920:start -->
<a id="source-39523bef0fe14920-computed-scenarios--design-inputs-only"></a>
### Computed scenarios — design inputs only

Zero starting crystal except named existing-paid case; optional boss excluded except named case. Two accepted runs/active day,10 waves each except named login/low-wave cases. Single-month cases initialize weekly state empty (cold-start window); only two-month runs preserve cross-month history. Discount-first; ordinary count is only executed at last active day for sweep strategy.

| Month | Case | Days played | XP / level | Crystals granted | Discount / full-price | Opening tickets | Elite ticket | Closing crystal |
| --- | --- | ---: | --- | ---: | --- | ---: | ---: | ---: |
| 2027-02 | month_start_daily_free | 28 | 4600 / 30 | 2310 | 28 / 9 | 0 | 1 | 10 |
| 2027-02 | month_start_daily_premium | 28 | 4600 / 30 | 4810 | 28 / 34 | 0 | 1 | 10 |
| 2027-02 | new_daily_free | 28 | 4600 / 30 | 2310 | 28 / 9 | 12 | 1 | 10 |
| 2027-02 | new_daily_premium | 28 | 4600 / 30 | 4810 | 28 / 34 | 12 | 1 | 10 |
| 2027-02 | return_day8_free | 21 | 3450 / 30 | 1970 | 21 / 9 | 0 | 0 | 20 |
| 2027-02 | return_day8_premium | 21 | 3450 / 30 | 4470 | 21 / 34 | 0 | 0 | 20 |
| 2027-02 | three_days_MWF_free | 12 | 3000 / 30 | 1540 | 12 / 9 | 0 | 0 | 40 |
| 2027-02 | three_days_MWF_premium | 12 | 3000 / 30 | 4040 | 12 / 34 | 0 | 0 | 40 |
| 2027-02 | five_days_weekdays_free | 20 | 3800 / 30 | 1900 | 20 / 9 | 0 | 0 | 0 |
| 2027-02 | five_days_weekdays_premium | 20 | 3800 / 30 | 4400 | 20 / 34 | 0 | 0 | 0 |
| 2027-02 | late_day15_free | 14 | 2300 / 23 | 1330 | 14 / 6 | 0 | 0 | 30 |
| 2027-02 | late_day15_premium | 14 | 2300 / 23 | 3330 | 14 / 26 | 0 | 0 | 30 |
| 2027-02 | late_day22_free | 7 | 1150 / 11 | 640 | 7 / 2 | 0 | 0 | 90 |
| 2027-02 | late_day22_premium | 7 | 1150 / 11 | 1640 | 7 / 12 | 0 | 0 | 90 |
| 2027-02 | late_purchase_day20 | 28 | 4600 / 30 | 4810 | 28 / 0 | 0 | 1 | 3410 |
| 2027-02 | daily_optional_boss | 28 | 4800 / 30 | 2310 | 28 / 0 | 0 | 1 | 910 |
| 2027-02 | three_days_low_waves | 12 | 2400 / 24 | 1240 | 12 / 0 | 0 | 0 | 640 |
| 2027-02 | login_only | 28 | 1160 / 11 | 1050 | 21 / 0 | 0 | 1 | 0 |
| 2026-09 | month_start_daily_free | 30 | 5250 / 30 | 2500 | 30 / 10 | 0 | 1 | 0 |
| 2026-09 | month_start_daily_premium | 30 | 5250 / 30 | 5000 | 30 / 35 | 0 | 1 | 0 |
| 2026-09 | new_daily_free | 30 | 5250 / 30 | 2500 | 30 / 10 | 12 | 1 | 0 |
| 2026-09 | new_daily_premium | 30 | 5250 / 30 | 5000 | 30 / 35 | 12 | 1 | 0 |
| 2026-09 | return_day8_free | 23 | 4100 / 30 | 2210 | 23 / 10 | 0 | 0 | 60 |
| 2026-09 | return_day8_premium | 23 | 4100 / 30 | 4710 | 23 / 35 | 0 | 0 | 60 |
| 2026-09 | three_days_MWF_free | 13 | 2650 / 26 | 1410 | 13 / 7 | 0 | 0 | 60 |
| 2026-09 | three_days_MWF_premium | 13 | 2650 / 26 | 3910 | 13 / 32 | 0 | 0 | 60 |
| 2026-09 | five_days_weekdays_free | 22 | 4450 / 30 | 2340 | 22 / 12 | 0 | 0 | 40 |
| 2026-09 | five_days_weekdays_premium | 22 | 4450 / 30 | 4840 | 22 / 37 | 0 | 0 | 40 |
| 2026-09 | late_day15_free | 16 | 2950 / 29 | 1670 | 16 / 8 | 0 | 0 | 70 |
| 2026-09 | late_day15_premium | 16 | 2950 / 29 | 4170 | 16 / 33 | 0 | 0 | 70 |
| 2026-09 | late_day22_free | 9 | 1800 / 18 | 980 | 9 / 5 | 0 | 0 | 30 |
| 2026-09 | late_day22_premium | 9 | 1800 / 18 | 2480 | 9 / 20 | 0 | 0 | 30 |
| 2026-09 | late_purchase_day20 | 30 | 5250 / 30 | 5000 | 30 / 0 | 0 | 1 | 3500 |
| 2026-09 | daily_optional_boss | 30 | 5500 / 30 | 2500 | 30 / 0 | 0 | 1 | 1000 |
| 2026-09 | three_days_low_waves | 13 | 2200 / 22 | 1160 | 13 / 0 | 0 | 0 | 510 |
| 2026-09 | login_only | 30 | 1350 / 13 | 1200 | 22 / 0 | 0 | 1 | 100 |
| 2026-10 | month_start_daily_free | 31 | 5350 / 30 | 2520 | 31 / 9 | 0 | 1 | 70 |
| 2026-10 | month_start_daily_premium | 31 | 5350 / 30 | 5020 | 31 / 34 | 0 | 1 | 70 |
| 2026-10 | new_daily_free | 31 | 5350 / 30 | 2520 | 31 / 9 | 12 | 1 | 70 |
| 2026-10 | new_daily_premium | 31 | 5350 / 30 | 5020 | 31 / 34 | 12 | 1 | 70 |
| 2026-10 | return_day8_free | 24 | 4200 / 30 | 2230 | 24 / 10 | 0 | 0 | 30 |
| 2026-10 | return_day8_premium | 24 | 4200 / 30 | 4730 | 24 / 35 | 0 | 0 | 30 |
| 2026-10 | three_days_MWF_free | 13 | 3100 / 30 | 1560 | 13 / 9 | 0 | 0 | 10 |
| 2026-10 | three_days_MWF_premium | 13 | 3100 / 30 | 4060 | 13 / 34 | 0 | 0 | 10 |
| 2026-10 | five_days_weekdays_free | 22 | 4000 / 30 | 2190 | 22 / 10 | 0 | 0 | 90 |
| 2026-10 | five_days_weekdays_premium | 22 | 4000 / 30 | 4690 | 22 / 35 | 0 | 0 | 90 |
| 2026-10 | late_day15_free | 17 | 3050 / 30 | 1690 | 17 / 8 | 0 | 0 | 40 |
| 2026-10 | late_day15_premium | 17 | 3050 / 30 | 4190 | 17 / 33 | 0 | 0 | 40 |
| 2026-10 | late_day22_free | 10 | 1900 / 19 | 1100 | 10 / 6 | 0 | 0 | 0 |
| 2026-10 | late_day22_premium | 10 | 1900 / 19 | 3100 | 10 / 26 | 0 | 0 | 0 |
| 2026-10 | late_purchase_day20 | 31 | 5350 / 30 | 5020 | 31 / 0 | 0 | 1 | 3470 |
| 2026-10 | daily_optional_boss | 31 | 5600 / 30 | 2520 | 31 / 0 | 0 | 1 | 970 |
| 2026-10 | three_days_low_waves | 13 | 2500 / 25 | 1360 | 13 / 0 | 0 | 0 | 710 |
| 2026-10 | login_only | 31 | 1370 / 13 | 1200 | 24 / 0 | 0 | 1 | 0 |
| 2026-09 | two_months_None | 61 | 10150 / 30/30 | 4870 | 61 / 0 | 0 | 2 | 1820 |
| 2026-09 | two_months_1 | 61 | 10150 / 30/30 | 9870 | 61 / 0 | 0 | 2 | 6820 |
| 2026-09 | late_new_unconverted_tickets | 16 | 2950 / 29 | 1670 | 16 / 0 | 12 | 0 | 870 |
| 2026-09 | login_only_existing_paid200 | 30 | 1350 / 13 | 1200 | 26 / 0 | 0 | 1 | 100 |

<a id="source-39523bef0fe14920-free--premium-per-level-table"></a>
#### Free / premium per-level table

| Level | Cumulative XP | Free | Premium (5x) |
| ---: | ---: | --- | --- |
| 1 | 100 | {'crystal': 100} | {'crystal': 500} |
| 2 | 200 | {'credits': 500} | {'credits': 2500} |
| 3 | 300 | {'parts': 100} | {'parts': 500} |
| 4 | 400 | {'exp': 500} | {'exp': 2500} |
| 5 | 500 | {'data': 50} | {'data': 250} |
| 6 | 600 | {'credits': 500} | {'credits': 2500} |
| 7 | 700 | {'crystal': 100} | {'crystal': 500} |
| 8 | 800 | {'credits': 500} | {'credits': 2500} |
| 9 | 900 | {'parts': 100} | {'parts': 500} |
| 10 | 1000 | {'exp': 500} | {'exp': 2500} |
| 11 | 1100 | {'data': 50} | {'data': 250} |
| 12 | 1200 | {'credits': 500} | {'credits': 2500} |
| 13 | 1300 | {'crystal': 100} | {'crystal': 500} |
| 14 | 1400 | {'credits': 500} | {'credits': 2500} |
| 15 | 1500 | {'parts': 100} | {'parts': 500} |
| 16 | 1600 | {'exp': 500} | {'exp': 2500} |
| 17 | 1700 | {'data': 50} | {'data': 250} |
| 18 | 1800 | {'credits': 500} | {'credits': 2500} |
| 19 | 1900 | {'crystal': 100} | {'crystal': 500} |
| 20 | 2000 | {'credits': 500} | {'credits': 2500} |
| 21 | 2100 | {'parts': 100} | {'parts': 500} |
| 22 | 2200 | {'exp': 500} | {'exp': 2500} |
| 23 | 2300 | {'data': 50} | {'data': 250} |
| 24 | 2400 | {'credits': 500} | {'credits': 2500} |
| 25 | 2500 | {'crystal': 100} | {'crystal': 500} |
| 26 | 2600 | {'credits': 500} | {'credits': 2500} |
| 27 | 2700 | {'parts': 100} | {'parts': 500} |
| 28 | 2800 | {'exp': 500} | {'exp': 2500} |
| 29 | 2900 | {'data': 50} | {'data': 250} |
| 30 | 3000 | {'credits': 500} | {'credits': 2500} |

<a id="source-39523bef0fe14920-continuing-accounts-previous-month-history-retained"></a>
#### Continuing accounts (previous month history retained)

| Month | Pattern | Premium | Active | XP / level | Granted crystals | Opening | Discount / normal | Closing |
| --- | --- | --- | ---: | --- | ---: | ---: | --- | ---: |
| 2027-02 | None | False | 28 | 4600 / 30 | 2310 | 70 | 28 / 9 | 80 |
| 2027-02 | None | True | 28 | 4600 / 30 | 4810 | 70 | 28 / 34 | 80 |
| 2027-02 | [0, 2, 4] | False | 12 | 3000 / 30 | 1540 | 10 | 12 / 9 | 50 |
| 2027-02 | [0, 2, 4] | True | 12 | 3000 / 30 | 4040 | 10 | 12 / 34 | 50 |
| 2027-02 | [0, 1, 2, 3, 4] | False | 20 | 3800 / 30 | 1900 | 70 | 20 / 9 | 70 |
| 2027-02 | [0, 1, 2, 3, 4] | True | 20 | 3800 / 30 | 4400 | 70 | 20 / 34 | 70 |
| 2026-09 | None | False | 30 | 5250 / 30 | 2500 | 20 | 30 / 10 | 20 |
| 2026-09 | None | True | 30 | 5250 / 30 | 5000 | 20 | 30 / 35 | 20 |
| 2026-09 | [0, 2, 4] | False | 13 | 3100 / 30 | 1560 | 10 | 13 / 9 | 20 |
| 2026-09 | [0, 2, 4] | True | 13 | 3100 / 30 | 4060 | 10 | 13 / 34 | 20 |
| 2026-09 | [0, 1, 2, 3, 4] | False | 22 | 4450 / 30 | 2340 | 70 | 22 / 13 | 10 |
| 2026-09 | [0, 1, 2, 3, 4] | True | 22 | 4450 / 30 | 4840 | 70 | 22 / 38 | 10 |
| 2026-10 | None | False | 31 | 4900 / 30 | 2370 | 0 | 30 / 8 | 70 |
| 2026-10 | None | True | 31 | 4900 / 30 | 4870 | 0 | 31 / 33 | 20 |
| 2026-10 | [0, 2, 4] | False | 13 | 3550 / 30 | 1710 | 60 | 13 / 11 | 20 |
| 2026-10 | [0, 2, 4] | True | 13 | 3550 / 30 | 4210 | 60 | 13 / 36 | 20 |
| 2026-10 | [0, 1, 2, 3, 4] | False | 22 | 4000 / 30 | 2190 | 40 | 22 / 11 | 30 |
| 2026-10 | [0, 1, 2, 3, 4] | True | 22 | 4000 / 30 | 4690 | 40 | 22 / 36 | 30 |

<!-- source-body:source-39523bef0fe14920:end -->


<a id="source-23ad5b6c04b82589"></a>
## Ops Error Logs Source of Truth

원출처: `WorkspaceRepo/docs/ops/ops-error-logs-source-of-truth.md`  
원문 SHA-256: `18ad3e0c24027620fbc211dd9dc4a93eaabc951ffe335bc570397c5d4c52cdc0`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-23ad5b6c04b82589:start -->
<a id="source-23ad5b6c04b82589-ops-error-logs-source-of-truth"></a>
### Ops Error Logs Source of Truth

<a id="source-23ad5b6c04b82589-purpose"></a>
#### Purpose

This document defines the source-of-truth model for LAST RAIL operational error logs.

Ops error logs are used by Admin dashboards, customer support summaries, and incident review. They are read-only evidence for launch support, not a repair mechanism.

<a id="source-23ad5b6c04b82589-ownership"></a>
#### Ownership

- Server owns creation, storage, and correlation of ops error logs.
- Admin owns read-only visibility, filtering, labels, and support context display.
- Unity may provide client diagnostic hints in future, but client diagnostics are never the source of truth for server-side error facts.
- Customer support uses ops error logs as supporting evidence only.

<a id="source-23ad5b6c04b82589-launch-scope"></a>
#### Launch Scope

Launch ops error logs should capture server-observed failures or restricted actions for:

- run finish
- run reward preview/claim
- offline reward preview/claim
- story progress update/read
- gacha pull/claim
- auth/account status
- integrity restriction
- support read model gaps

Launch ops error logs should not include:

- raw request bodies
- access tokens
- refresh tokens
- raw auth headers
- RNG material
- seed preimages
- per-candidate probability
- full weight tables
- raw multiplier bps
- localized dialogue full text
- unrestricted stack traces in Admin responses

<a id="source-23ad5b6c04b82589-recommended-table"></a>
#### Recommended Table

Future migration candidate: `ops_error_logs`.

Columns:

- `id TEXT PRIMARY KEY`
- `created_at INTEGER NOT NULL`
- `severity TEXT NOT NULL`
- `error_code TEXT NOT NULL`
- `message TEXT`
- `status TEXT NOT NULL`
- `source TEXT NOT NULL`
- `account_id TEXT REFERENCES accounts(id) ON DELETE SET NULL`
- `run_id TEXT REFERENCES runs(id) ON DELETE SET NULL`
- `reward_preview_id TEXT REFERENCES run_reward_previews(id) ON DELETE SET NULL`
- `reward_transaction_id TEXT REFERENCES reward_transactions(id) ON DELETE SET NULL`
- `economy_transaction_id TEXT REFERENCES economy_transactions(id) ON DELETE SET NULL`
- `offline_reward_claim_id TEXT`
- `gacha_batch_id TEXT`
- `story_progress_id TEXT`
- `operation_id TEXT`
- `risk INTEGER`
- `idempotency_operation TEXT`
- `idempotency_key_hash TEXT`
- `integrity_event_id TEXT`
- `support_ticket_id TEXT`
- `correlation_json TEXT NOT NULL`
- `sanitized_context_json TEXT NOT NULL`
- `resolved_at INTEGER`
- `resolved_by TEXT`
- `resolution_note TEXT`

Indexes:

- `idx_ops_error_logs_created_at`
- `idx_ops_error_logs_severity_created`
- `idx_ops_error_logs_error_code_created`
- `idx_ops_error_logs_account_created`
- `idx_ops_error_logs_run_created`
- `idx_ops_error_logs_reward_transaction_created`
- `idx_ops_error_logs_offline_claim_created`
- `idx_ops_error_logs_gacha_batch_created`
- `idx_ops_error_logs_status_created`

<a id="source-23ad5b6c04b82589-severity"></a>
#### Severity

| Severity | Korean Label | Meaning |
| --- | --- | --- |
| `info` | 안내 | Expected or explainable operational state. |
| `warning` | 주의 | Non-fatal mismatch, retry, missing optional data, or support-relevant anomaly. |
| `error` | 오류 | Failed operation or missing required server-side state. |
| `critical` | 치명 | Economy, integrity, account identity, or broad incident risk. |

<a id="source-23ad5b6c04b82589-status"></a>
#### Status

| Status | Korean Label | Meaning |
| --- | --- | --- |
| `open` | 열림 | Needs visibility or investigation. |
| `acknowledged` | 확인됨 | Operator has seen it; not resolved yet. |
| `resolved` | 해결됨 | Root cause or expected behavior confirmed. |
| `ignored` | 무시됨 | Known harmless signal. |

Launch Admin should treat status as read-only unless an explicit escalation workflow is later added.

<a id="source-23ad5b6c04b82589-source"></a>
#### Source

| Source | Korean Label |
| --- | --- |
| `game_api` | 게임 API |
| `admin_api` | 어드민 API |
| `worker_cron` | 예약 작업 |
| `migration_verify` | 마이그레이션 검증 |
| `support_read_model` | 고객지원 조회 모델 |
| `integrity_pipeline` | 무결성 파이프라인 |

<a id="source-23ad5b6c04b82589-error-code-taxonomy"></a>
#### Error Code Taxonomy

<a id="source-23ad5b6c04b82589-authaccount"></a>
##### Auth/Account

- `auth_required`
- `access_token_invalid`
- `auth_session_invalid`
- `account_not_active`
- `account_not_found`
- `account_identity_conflict`

<a id="source-23ad5b6c04b82589-contractinput"></a>
##### Contract/Input

- `contract_violation`
- `idempotency_key_required`
- `duplicate_replay`

<a id="source-23ad5b6c04b82589-runreward"></a>
##### Run/Reward

- `reward_plan_mismatch`
- `reward_multiplier_missing`
- `reward_plan_unavailable`
- `reward_integrity_restricted`
- `reward_claim_missing_ledger`
- `reward_preview_without_claim`

<a id="source-23ad5b6c04b82589-offline-reward"></a>
##### Offline Reward

- `offline_story_progress_missing`
- `offline_reward_policy_missing`
- `offline_reward_policy_invalid`
- `offline_reward_not_claimable`
- `offline_reward_state_missing`
- `offline_reward_state_conflict`
- `offline_reward_clock_tamper`
- `offline_reward_claim_conflict`
- `offline_reward_plan_mismatch`

<a id="source-23ad5b6c04b82589-storynarrative"></a>
##### Story/Narrative

- `story_progress_policy_missing`
- `story_progress_state_conflict`
- `story_unlock_missing`
- `narrative_content_missing`
- `narrative_version_mismatch`

<a id="source-23ad5b6c04b82589-economy"></a>
##### Economy

- `economy_mutation_conflict`
- `currency_ledger_missing`
- `reward_transaction_missing`

<a id="source-23ad5b6c04b82589-gacha"></a>
##### Gacha

- `gacha_temporarily_unavailable`
- `banner_inactive`
- `invalid_pull_count`
- `gacha_guarantee_conflict`
- `gacha_duplicate_replay_mismatch`

<a id="source-23ad5b6c04b82589-integrity"></a>
##### Integrity

- `integrity_restricted_reward`
- `integrity_soft_anomaly`
- `integrity_hard_restriction`
- `suspicious_replay`

<a id="source-23ad5b6c04b82589-correlation-policy"></a>
#### Correlation Policy

Ops logs should store stable reference ids where available:

- `accountId`
- `runId`
- `operationId`
- `risk`
- `rewardPreviewId`
- `rewardTransactionId`
- `economyTransactionId`
- `offlineRewardClaimId`
- `gachaBatchId`
- `storyProgressId`
- `integrityEventId`
- `supportTicketId`
- `idempotencyOperation`
- `idempotencyKeyHash`

Raw idempotency keys should not be exposed in Admin. Store a server-side hash or stable reference only.

<a id="source-23ad5b6c04b82589-sanitized-context"></a>
#### Sanitized Context

Allowed sanitized context:

- public status values
- stable enum codes
- counts
- timestamps
- boolean presence flags
- reference ids
- display-safe labels
- support-safe summaries

Forbidden context:

- raw request bodies
- token strings
- session secrets
- raw auth headers
- raw RNG material
- seed preimages
- per-candidate probabilities
- full weight tables
- raw multiplier bps
- internal reward formulas beyond public policy labels
- localized dialogue full text

<a id="source-23ad5b6c04b82589-creation-rules"></a>
#### Creation Rules

Create an ops error log when:

- A server-side required state is missing.
- A ledger/economy/reward transaction mismatch is detected.
- A duplicate replay does not match stored response references.
- Integrity policy restricts reward, story progress, or claim behavior.
- Story progress or unlock expected from full victory is missing.
- Offline reward state revision or timestamp conflicts with claim records.
- Gacha guarantee state conflicts with pull result or pity counters.
- Admin support summary cannot compose a required read-only section.

Do not create noisy logs for:

- normal `offline_reward_not_claimable` caused only by minimum time
- expected duplicate replay with matching stored response
- normal no-row fallback for legacy offline reward state
- missing optional narrative localization before content lock

Those can be represented as support context or low-level metrics instead.

<a id="source-23ad5b6c04b82589-admin-read-model"></a>
#### Admin Read Model

Future Admin endpoint candidates:

- `GET /v1/admin/ops/error-logs`
- `GET /v1/admin/ops/error-logs/:id`

List filters:

- `severity`
- `status`
- `errorCode`
- `source`
- `accountId`
- `runId`
- `operationId`
- `offlineRewardClaimId`
- `gachaBatchId`
- `storyProgressId`
- `supportTicketId`
- `createdFrom`
- `createdTo`
- `limit`
- `cursor`

Response should include:

- `id`
- `createdAt`
- `severity`
- `status`
- `source`
- `errorCode`
- `message`
- `correlation`
- `sanitizedContext`
- `resolvedAt`
- `resolvedBy`
- `resolutionNote`

Launch should keep resolution mutation disabled unless escalation workflow is implemented.

<a id="source-23ad5b6c04b82589-support-summary-integration"></a>
#### Support Summary Integration

`GET /v1/admin/support/accounts/:accountId` currently may return `opsErrors: []` as a placeholder.

Future support summary should include recent open ops error logs for the account:

- latest 5 open or acknowledged errors
- critical errors first
- no raw request or secret data
- correlation ids only
- safe labels for Admin

Support should use these logs to decide escalation category, not to issue repair directly.

<a id="source-23ad5b6c04b82589-retention"></a>
#### Retention

Recommended launch retention:

- `critical`: 180 days minimum
- `error`: 90 days minimum
- `warning`: 30 days minimum
- `info`: 14 days minimum or metrics-only where possible

Retention can be adjusted once production volume is known.

<a id="source-23ad5b6c04b82589-alerting-candidates"></a>
#### Alerting Candidates

Create alert candidates for:

- any `critical` error
- repeated `reward_claim_missing_ledger`
- repeated `offline_reward_state_conflict`
- repeated `gacha_guarantee_conflict`
- surge in `contract_violation`
- surge in `integrity_hard_restriction`
- support summary composition failures

Alerting is not required for the first read-only API slice.

<a id="source-23ad5b6c04b82589-rollout-order"></a>
#### Rollout Order

1. Finalize ops error log schema and taxonomy.
2. Add Server migration and write helper.
3. Add read-only Admin API list/detail endpoints.
4. Connect Admin `/ops/error-logs` to actual read-only data.
5. Add support summary integration for recent account errors.
6. Add alerting thresholds.
7. Add resolution workflow only after escalation permissions are ready.

<a id="source-23ad5b6c04b82589-open-decisions"></a>
#### Open Decisions

- Whether ops error logs should be D1-only or also streamed to a remote log system.
- Whether `message` should be operator-authored, server-authored, or code-derived only.
- Whether status changes need a separate audit table.
- Whether support ticket ids are optional or required for resolution.
- Whether critical integrity logs need a separate restricted permission.
- Exact retention by environment.

<!-- source-body:source-23ad5b6c04b82589:end -->


<a id="source-12ecfb769e13c466"></a>
## LAST RAIL parallel pursuit source delivery

원출처: `WorkspaceRepo/docs/ops/visual-upgrade-20260913/STAGE-1-DESIGN.md`  
원문 SHA-256: `6f6213dd7fa1d8c4a7b4f27cef2f303305800379a178eaaf6ac7a2f59181fa6d`  
검토·적용 경계: Planning source contains policy/proposal/asset direction beyond category summary; retain current approved-versus-proposed distinction and unique details.

<!-- source-body:source-12ecfb769e13c466:start -->
<a id="source-12ecfb769e13c466-last-rail-parallel-pursuit-source-delivery"></a>
### LAST RAIL parallel pursuit source delivery

User-authorized architectural visual change, stage 1 of four. Direction owns supply and review; Unity owns E runtime. All files local/uncommitted. The four-stage order remains exterior, interior animation, cards, final HUD/details; this delivery does not complete later stages.

<a id="source-12ecfb769e13c466-camera-and-projection"></a>
#### Camera and projection

Keep 720x1280, exterior viewport (0,66,720,273), current train anchor/painted width approximately310, existing interior and HUD boundaries. Use parallel pursuit for the normal presentation. Keep the old hairpin resources/layout as a named special encounter presentation; never show both incompatible road systems together.

Unity agreed lanes: local y=154+.045*x and y=186+.045*x plus the same continuous world-space wave of amplitude<=2 for both road sampling and actor ground contact. Rail remains y=223.86+.0606667*x. Preserve model availability, gameplay distance, attack/target states and hit results. Entrance x790 to assigned encounter x near600, rank spacing at least90 before actual bbox check. Visible actor contact is evaluated on the same road function at its projected x and current motionSeconds. No independent y offset for rank.

The road texture phase moves left at64px/s, middle ridges at24px/s, existing near layers130/162.5px/s. Roadside objects are attached to the road's64px/s world coordinates, not a separate45px/s velocity which would slide their bases against the road. All use the existing simulation-derived motionSeconds, pause/speed/restore preserved. These are presentation tuning values, not new gameplay speed statistics.

Reuse original canyon-far-v01.png under docs/ops/exterior-canyon-20260912: directly inspected again, no close hairpin baked into it. Its existing bounded slow movement remains far atmosphere; the passing middle structures create sustained motion. Upper-right warm amber illumination matches the brightest far sky and new asset lit faces. Do not mirror any source because it reverses lighting. Train/enemy relighting must preserve silhouette and real mount sockets.

<a id="source-12ecfb769e13c466-exact-assets-and-importer-contract"></a>
#### Exact assets and importer contract

All new images are2172x724, built-in image_gen, exact prompts in PROMPTS.json. Byte hashes, mode, alpha and bbox measurement in asset-inspection.json. Original bytes unchanged.

- road-surface-v01.png: RGB full-surface earth/gravel with two horizontal tracks. PursuitRoad uses single-track crop UV[0,.64,1,.20] (top-left source y=.16..36), avoiding squeezing both tracks into one28px lane. Raw full-height left/right edge mean absolute RGB difference26.433; not seamless certified. Consumer uses its collar/overlap mesh then tests full wrap. No entire canyon picture tiling.
- mid-ridge-v01.png: RGBA genuine alpha. PursuitRidges. Opaque top-left bbox[15,177,2136,402]. Two separately placed copies at differing size; individual offscreen recycle, not a seamless PNG. Maintain aspect.
- landmarks-v01.png: RGBA genuine alpha. PursuitMarkers. Nonuniform atlas rects: boulder[22,76,895,600], marker[978,133,438,533], tooth[1574,20,554,675]. No equal-third slicing. Foot is bottom-center of each measured rectangle; visually verify base after import.
- pursuit-layout-v01.json: TextAsset PursuitLayout, exact Unity-requested schema. UVs are normalized bottom-left, rect metadata above is pixels top-left. Generated/validated by build_layout.py. Decoration y for ridge is local top; roadside y is offset from RoadY to sprite top as requested.

Low-alpha red RGB seen in raw tool display has maximum alpha2/255 for both RGBA images, zero such pixels above128. Actual browser standard-alpha composite on brown background directly inspected at720width: no visible red fringe or checkerboard. Therefore do NOT apply the initially floated .5 alpha cutoff by default: it would unnecessarily harden edges. Preserve alpha, Clamp, normal blending, suitable transparent-texture import bleed handling and bilinear filtering; inspect on actual sky at runtime before deciding a material adjustment. No raster cleanup performed.

<a id="source-12ecfb769e13c466-assembly-and-review-criteria"></a>
#### Assembly and review criteria

Draw far, mid ridges, supporting road bands, roadside props ordered by ground depth, actors with exact feet/HP/muzzle, rail/near terrain, wheel dust. A road prop placed behind an actor must not be forced in front by atlas order. Never let a passing ridge occlude train/HP/HUD. The flat bottom of a ridge must be buried under supporting ground, not float over visible canyon. If two28px road strips leave exposed canyon between/below, add a shared textured support shelf in native geometry using this ground material; do not bridge two roads over a visible void.

Passing landmarks must fully exit before recycling: period960, existing three-copy overscan. Directly inspect interval spanning wraps in both directions of viewport boundaries, with at least0.5seconds before/after. Check dark seams/alpha double-opacity/visible repeated unique features and exposed ground gaps. Source edge metric alone does not certify the composed loop.

Time-axis QA: 1x,1.5x,2x,pause/resume,restore during partial actor entry; keep actual wheel footprints on rails and ground dust trailing left. Midground motion needs a recognisable feature displacement across frames; narrow foreground alone is insufficient. Actual turret recoil/trace origins, vehicle silhouette at upper/lower depths, 4-actor diagnostic identification,35/15% fixture actualrepair+400 and stage reduction all need regression evidence after scene assembly. Record diagnostic vs real gameplay and direct viewing vs telemetry. Stage1 passes only with actual Unity source-linked material review and motion evidence; browser source preview is not runtime QA.

<a id="source-12ecfb769e13c466-subsequent-stages"></a>
#### Subsequent stages

Stage2 starts after stage1 visual gate. Inspect exact current actor art/animation metadata first; preserve Kyle/Reina/Eve/Sera/Mira and their actual abilities, reduce size around stable foot anchors, source real separate parts or distinct pose frames with weapon/hand/foot socket metadata. No one-mesh stretching as detailed animation completion. If generated parts fail anatomical/identity/alpha QA, preserve failure and use a concretely reviewed alternative rather than repeating the same failed request.

Stage3 uses four separate deliverable classes: frame, portrait, ability-specific icon, state presentation. Source actual roster/ability mapping, do not copy incorrect names from reference art. Stage4 finalizes logo/wave/HP/bottom controls/world details without changing approved contracts. Reference A-final-charge-gauge-v02.png is material/finish direction, not permission to restore its near camera or invented roster.

<!-- source-body:source-12ecfb769e13c466:end -->


<a id="source-4e31919889a4c5f1"></a>
## Android Signing Builder Implementation Plan

원출처: `WorkspaceRepo/docs/superpowers/plans/2026-09-08-android-signing-builder.md`  
원문 SHA-256: `7df4b667c00d0062aa0d280376cda5b442d5020fe2f87450ffdd2f97b9e6abd7`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-4e31919889a4c5f1:start -->
<a id="source-4e31919889a4c5f1-android-signing-builder-implementation-plan"></a>
### Android Signing Builder Implementation Plan

> Execute serially as Unity owner; preserve the shared dirty workspace.

**Goal:** Implement the approved local DEV/PRD signing and parameterized build contract without running a full player build or publishing anything.

**Spec:** ../../ops/android-signing-build-policy.md

**Architecture:** Pure argument/define/version registry policy with focused executable tests; Unity Editor orchestration resumes after selected-symbol compilation and restores settings on every terminal path. Credentials remain transient; restart recovery cancels interrupted builds rather than silently rebuilding.

<a id="source-4e31919889a4c5f1-work-units"></a>
#### Work Units

- [x] Add focused RED tests for explicit parameters, define replacement, confirmed baselines, locks and success-only counters. Use pure C# policy under Assets/_LASTRAIL/Editor/Signing and a standalone local harness so MCP reconnect is not a prerequisite.
- [x] Implement policy and GREEN tests, including code maximum2100000000, independent DEV/PRD state and explicit code preservation. Final44 checks passed.
- [ ] Add secure outside-repo key provisioning with existing-file refusal, restrictive ACL, env/stdin credentials and public certificate evidence. No secret literals or raw credentials in reports. Actual generation requires the authorized password through a secure input channel.
- [x] Implement Editor UI/executeMethod, profile/key/output preflight, reload-resumable state, BuildPipeline integration, actual artifact identity validation and restoration/recovery. No automatic build after a crash. Real artifact validation remains unrun.
- [ ] Run focused policy tests and Unity compile/reload tests with a single owned editor; no full APK/AAB run. Verify changed files and handoff; directly report actual results and remaining first-build inputs.

<a id="source-4e31919889a4c5f1-constraints"></a>
#### Constraints

- DEV app.lastrail.game.dev / LASTRAIL_ENV_DEV; PRD app.lastrail.game / LASTRAIL_ENV_PRD.
- PRD tracks share a counter and upload identity; APK/AAB share each package counter.
- Unknown history is not zero. Explicit baseline reconciliation required; no Play API or remote changes.
- Preserve unrelated symbols and settings. Clear password fields. Quarantine incomplete artifacts.
- Current MCP batch editor must be released before another LASTRAIL editor is started.

<a id="source-4e31919889a4c5f1-evidence-sources"></a>
#### Evidence Sources

- https://docs.unity.cn/6000.0/Documentation/Manual/custom-scripting-symbols.html
- https://developer.android.com/studio/publish/versioning

<a id="source-4e31919889a4c5f1-follow-on-publication-integration"></a>
#### Follow-On Publication Integration

Spec received after the signing slice: ../../api/signed-builder-publication-integration.md.

- [ ] Freeze one verified app/content manifest and durable phase receipt. Retry
  resumes that exact artifact/versionCode/release; it must not call BuildPlayer
  or allocate a new code. Separate local validation, uploaded and activated states.
- [ ] Integrate owner-controlled audit/upload/HTTPS verification safeguards. The
  existing helper accepts only DEV0.1.0/code1 and is not a generic uploader. Do
  not silently broaden its approval or invoke its historical activate mode.
- [ ] Add authenticated registry adapter only after the established credential
  provider is identified. No service-token-only OWNER assumption or invented
  privileged endpoint. DEV requires corrected STABLE policy deployment readback,
  exact content identity, minimum admission and explicit LEGACY cutover guard.
- [ ] GET fresh ETag, bounded register and GET reconciliation;409/timeout/502
  means readback, not forced fresh-ETag retry. DISABLED never auto-reactivates.
- [ ] PRD stops after verified content upload; its bucket/host are UNCONFIGURED,
  so fail closed until separately configured. Never upload app binaries to R2,
  mutate registry/minimum/current or publish to Play from this pipeline.
- [ ] Test immutable resume, lost/mismatched receipt, unknown object, hash/size
  mismatch, upload failure,409 and ambiguous commit, legacy policy/auth absence,
  exact stable reconciliation and PRD no-mutation with local fixtures.

No remote invocation is authorized merely by this plan. Current blocking facts:
corrected registry contract not deployed/read back, no established headless Admin
delegation provider, PRD authority absent, real keys/baselines/device absent. These
do not prevent writing local fixtures, but no publisher integration is claimed yet.

<!-- source-body:source-4e31919889a4c5f1:end -->


<a id="source-de88e58240d87fd7"></a>
## DEV Play Internal Publisher Implementation Plan

원출처: `WorkspaceRepo/docs/superpowers/plans/2026-09-08-dev-play-internal.md`  
원문 SHA-256: `15b061196eb34c2708ce82124bc713ba7e9f1054f6df5d5a7b1b95d94a6c2cb5`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-de88e58240d87fd7:start -->
<a id="source-de88e58240d87fd7-dev-play-internal-publisher-implementation-plan"></a>
### DEV Play Internal Publisher Implementation Plan

**Spec:** .././../api/signed-builder-publication-integration.md plus Plan-relayed
user approval for DEV Play internal-testing release integration, 2026-09-08.

**Goal:** Extend the existing signed builder with fixture-tested, resumable DEV
publication stages, never a second build pipeline or implicit remote execution.

<a id="source-de88e58240d87fd7-ownership-and-sequence"></a>
#### Ownership And Sequence

- [ ] Google API transport adapter and offline request fixtures under
  unity-project/Tools/Publisher/play-api.mjs and play-api.test.mjs, isolated worker.
  Built-in Node22 fetch, no credential creation/dependencies. Strict fixed DEV
  package and internal track, no production mutation, no credential logging.
- [ ] Main owner: publisher receipt/state machine and fixtures, existing content
  owner adapter gates, authenticated registry adapter guards, immutable identity
  and Play edit/upload/track/commit/readback reconciliation.
- [ ] Main owner: existing Unity builder/window integration and signed manifest,
  no rebuild/counter increment on publish retry. Preserve PRD build-only behavior.
- [ ] Review, offline tests, Unity compile, readiness documentation and canonical.
  Direct Plan report currently blocked by auto-review; await specific approval.

<a id="source-de88e58240d87fd7-api-contract-for-adapter"></a>
#### API Contract For Adapter

Export `PlayApi` class constructed with `{tokenProvider, fetchImpl}`; default
fetchImpl is global fetch, tokenProvider returns an existing OAuth access token.
Methods (fixed app.lastrail.game.dev internally):
`readCommittedReleases()` GET internal/releases,
`createEdit()` POST edits `{}`,
`getEdit(editId)`, `listTracks(editId)`, `listBundles(editId)`,
`uploadBundle(editId, bytes)` media upload octet-stream,
`updateInternal(editId, release)` PUT internal track only,
`validateEdit(editId)`, `commitEdit(editId)` with ERROR_IF_IN_REVIEW.
Return parsed JSON; throw sanitized error with status/code, never response body
or token. Reject unexpected IDs, redirects, invalid tokens and noninternal track
input. No retry policy inside transport; coordinator reconciles ambiguous writes.

<a id="source-de88e58240d87fd7-main-invariants"></a>
#### Main Invariants

- Freeze exact AAB bytes/hash, signer fingerprint, version/code and content candidate
  hash/release manifest. Public report is not proof of byte validation.
- R2 owner contract/version scope unchanged; missing compatible adapter stops.
  No legacy activate, raw D1 write, arbitrary R2 upload or fake successful receipt.
- DEV registry success needs fresh authenticated policy/ETag, compatibility and
  exact STABLE readback; no minimum mutation or automatic DISABLED reactivation.
- Play edit ID/manifest hash recorded before upload. Read tracks and bundles;
  existing code requires exact SHA256. Unknown upload/commit outcomes reconcile,
  never replace artifact, bump code or create duplicate release blindly.
- Track must be actually returned as internal; API read-only readiness must not
  create an edit. No PRD/production release methods. No app binary R2 publication.
- Pending review must not be cancelled; commit ERROR_IF_IN_REVIEW. First-app,
  signing, authorization or permission failures remain explicit blocked phases.
- All network-changing methods remain unrun during local fixture implementation.

<a id="source-de88e58240d87fd7-official-sources"></a>
#### Official Sources

- https://developers.google.com/android-publisher/api-ref/rest/v3/edits.tracks
- https://developers.google.com/android-publisher/api-ref/rest/v3/edits.bundles
- https://developers.google.com/android-publisher/api-ref/rest/v3/edits/commit
- https://developers.google.com/android-publisher/api-ref/rest/v3/applications.tracks.releases/list

<!-- source-body:source-de88e58240d87fd7:end -->


<a id="source-1cb86b4d2982451e"></a>
## Version Registry / Unity Handshake Review

원출처: `WorkspaceRepo/docs/unity-version-registry-handshake-review-2026-09-07.md`  
원문 SHA-256: `d3e002a8700827bc942205fcd9834de9ee3219ed1acf29b29218e75447609529`  
검토·적용 경계: Current workspace guidance, routing or distinct technical handoff; not covered by this retirement batch

<!-- source-body:source-1cb86b4d2982451e:start -->
<a id="source-1cb86b4d2982451e-version-registry--unity-handshake-review"></a>
### Version Registry / Unity Handshake Review

Status: contract dependency review, not implemented or remotely verified.
Owner: LAST RAIL_Unity, 2026-09-07. A05 stays immutable.

<a id="source-1cb86b4d2982451e-approved-behavior"></a>
#### Approved Behavior

- Ordinary Editor uses local Addressables; remote verification is an explicit mode.
- APK/AAB uses the exact installed versionCode's server-approved catalog.
- No registry entry: block. Below minimum: update. No directory scan/latest inference.
- INTERNAL: permit authentication, then server tester authorization, then content.
  No stable fallback or pre-auth internal/stable bundle download.
- STABLE: preserve approved Wi-Fi predownload and non-Wi-Fi consent behavior.
- Pin the approved release throughout download. Revalidate internal access on
  session expiry, logout and entry; client labels do not grant tester authority.
- Local resource selection must not bypass real auth/economy or fake their success.
- Build/verify/immutable upload/verify are distinct from activation/PRD promotion.
  No folder convention, release number or concurrent allocation algorithm invented.

<a id="source-1cb86b4d2982451e-existing-dependency-cycle"></a>
#### Existing Dependency Cycle

Current original source prefix: E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/.

- Resources/ClientAdmissionContract.cs requires ACCEPTED admission and an
  APPROVED_COMPATIBLE contentRelease in the same accepted response.
- Resources/DownloadCoordinator.cs raises ConfigPassed only after that gate
  evaluates to Initializing. It then starts catalog/size/download preparation.
- Resources/ResourceBootstrapController.cs ConfigPassed triggers session restore;
  login buttons also require coordinator.ConfigAccepted.
- Auth/PlayerEntryRuntime.cs requires profile, nickname and story preparation
  before AcceptAuthenticatedSessionAsync releases the authenticated content gate.
- Existing content admission request carries installed version code, not the new
  authenticated tester authorization exchange. Merely withholding a catalog or
  moving the existing auth wait earlier cannot implement the new handshake.

<a id="source-1cb86b4d2982451e-minimum-server-contract-needed"></a>
#### Minimum Server Contract Needed

1. Exact pre-auth response schema/status for registered INTERNAL, STABLE,
   unregistered and minimum-version rejection, including what enables login.
2. Exact authenticated tester/content lookup endpoint, token/header rules and
   accepted/denied/expired response schema. Define whether catalog metadata is
   withheld before authorization, not only bundle download.
3. Authentication, refresh, profile, nickname and story endpoint admission rules
   during pre-content INTERNAL login, so account hydration cannot deadlock.
4. Authoritative versionCode/channel/release identity, compatibility, revision,
   expiry/revalidation and pin-change behavior, including logout/revocation.
5. Ordinary Editor auth identity/storage policy independent of local resources.
   Existing Editor supplies no installed version code and no Android Keystore.
   No fake installed headers or in-memory success storage will be added by default.
6. Publication reservation/collision contract and activation boundary. A05 IDs
   and paths remain unchanged; version_code_cNNN is not a committed contract.

These are semantic requirements, NOT invented response property names. Server's
canonical/schema artifact must be consumed before implementing a wire parser.
Direct-report HOLD prevents a coordination message; this document and the Unity
canonical handoff record the dependency. No DTO agreement has been claimed.

<a id="source-1cb86b4d2982451e-planned-verification"></a>
#### Planned Verification

- Local Editor content selection does not fetch R2 or relax auth/economy.
- Explicit remote mode does not silently fall back to local content.
- Unknown version blocks; below minimum updates; registry lookup is exact.
- INTERNAL login remains usable without content; no pre-auth catalog/bundle fetch.
- Authorized tester receives only pinned internal release; denied/expired tester
  never downloads stable fallback or enters Main.
- STABLE Wi-Fi predownload and cellular consent continue unchanged.
- Mid-download pointer change does not replace the pinned plan.
- Logout/revocation cancels or invalidates pending entry, including stale callbacks.
- Concurrent publication cannot reserve/overwrite an existing immutable namespace.

No new tests or builds were run in this review. Runtime source is unchanged.
Server snapshot reports implementation queued after its crystal work; inspected
attendance/pass and runtime API docs contain no finalized INTERNAL/STABLE schema.

<!-- source-body:source-1cb86b4d2982451e:end -->


<a id="source-2a676ccd3c25f69b"></a>
## Cloudflare Environments

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/cloudflare/environments.md`  
원문 SHA-256: `6ac0d37997c5eee2ff5493604b0a262c8caaa4835e6ead5d26d603e196bff833`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-2a676ccd3c25f69b:start -->
<a id="source-2a676ccd3c25f69b-cloudflare-environments"></a>
### Cloudflare Environments

LOCAL, DEV, and PRD must not share remote D1, Queues, R2 buckets, Secrets, or Worker names.

| Environment | Game Worker | Admin Worker | Async Worker | D1 | Queue |
| --- | --- | --- | --- | --- | --- |
| DEV | `lastrail-game-api-dev` | `lastrail-admin-api-dev` | `lastrail-async-worker-dev` | `lastrail-core-dev` | `lastrail-admin-jobs-dev` |
| PRD | `lastrail-game-api-prd` | `lastrail-admin-api-prd` | `lastrail-async-worker-prd` | `lastrail-core-prd` | `lastrail-admin-jobs-prd` |

DEV may be connected when `wrangler whoami` succeeds. PRD resource creation or deployment requires explicit approval and the checklist in `docs/cloudflare/prd-launch-preflight.md`.

R2 buckets and Analytics Engine datasets must also be environment-separated. DEV bindings are pending until those products are enabled in the Cloudflare account. STAGING is currently outside scope.


<!-- source-body:source-2a676ccd3c25f69b:end -->


<a id="source-0a74736945cfcb2f"></a>
## PRD Launch Preflight

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/cloudflare/prd-launch-preflight.md`  
원문 SHA-256: `86fa1e3bd9e928081ed2c4efbcba02d1bf6e0e548652757ba80bb33b4d0cf107`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-0a74736945cfcb2f:start -->
<a id="source-0a74736945cfcb2f-prd-launch-preflight"></a>
### PRD Launch Preflight

This checklist defines the server-side preflight for an Act 1 PRD launch. It is
not a deployment record and does not authorize creating secrets, production
resources, DNS routes, tags, or releases without the concrete inputs below being
present and explicitly confirmed.

<a id="source-0a74736945cfcb2f-scope"></a>
#### Scope

- DEV Google OAuth smoke is not a launch blocker. DEV may keep returning
  `google_auth_not_configured` for real Google routes.
- Real Google OAuth audience/client id configuration is PRD/app-registration
  scope only.
- Do not invent, hardcode, print, commit, or log OAuth values or Worker secrets.
- Rail Choice Events and World Raid remain outside Act 1 launch scope.

<a id="source-0a74736945cfcb2f-release-shape"></a>
#### Release Shape

Before PRD deployment, prepare and verify:

- Release branch: `release/YYYY-MM-DD-act1`.
- Release tag: `act1-launch-YYYY-MM-DD`.
- LAST RAIL server version: a unique calendar version such as
  `2026.09.06.1`. Do not deploy a Worker without one, and do not reuse one for
  the same environment/service pair.
- Service deploy tags: immutable tags that include both service and server
  version, for example `game-api-2026.09.06.1`,
  `admin-api-2026.09.06.1`, and `async-worker-2026.09.06.1` when those
  services are included.
- Final local regression: `pnpm test`, `pnpm typecheck`, `pnpm lint`,
  `pnpm d1:local:verify`.
- Config diff review for `apps/game-api/wrangler.jsonc`,
  `apps/admin-api/wrangler.jsonc`, and `apps/async-worker/wrangler.jsonc`.
- PRD migration plan for `packages/db/migrations/0001` through current HEAD,
  including whether every migration is additive/rollback-compatible or requires
  an enforced `minimumVersionCode` gate.
- Rollback note: keep the previous Worker version id for each deployed Worker;
  rollback uses Wrangler Worker version rollback after confirming no migration
  rollback is needed. D1 schema rollback must be a separate approved plan.
- Source preservation note: release branches and tags preserve the verified
  source. They are not DB backups and do not authorize D1 rollback.
- Server deployment evidence note: the actual Cloudflare Worker deployment id is
  runtime truth. The LAST RAIL server version links that runtime to source,
  tests, smoke results, and Admin evidence.
- Compatibility note: a stored minimum version document or Unity update popup is
  not by itself a safe guard for compatibility-breaking DB/API changes. The Game
  API admission path must consume the policy before incompatible routes rely on
  it.

<a id="source-0a74736945cfcb2f-required-prd-inputs"></a>
#### Required PRD Inputs

| Area | Required input | Current local status | Blocker |
| --- | --- | --- | --- |
| D1 | `lastrail-core-prd` database id | `__PRD_D1_DATABASE_ID__` placeholder in all PRD Worker configs | Create/confirm PRD D1, then replace placeholder |
| Queue | `lastrail-admin-jobs-prd` queue | Config references name, remote list currently shows only DEV queue | Create/confirm PRD queue before async/admin PRD deploy |
| Game API secret | `AUTH_SIGNING_KEY_CURRENT` | Cannot exist until `lastrail-game-api-prd` Worker exists | Generate/provide PRD signing key and inject as Worker secret after Worker/resource approval |
| Game API config | `GOOGLE_AUTH_AUDIENCES` | Not in PRD vars yet | Provide Google OAuth client id/audience allow-list from app registration |
| Admin Access | `ADMIN_ACCESS_ISSUER`, `ADMIN_ACCESS_AUD` | PRD placeholders | Create/confirm PRD Cloudflare Access app for `admin.lastrail.app` |
| Admin delegated Access | `ADMIN_DELEGATED_ACCESS_ISSUER`, `ADMIN_DELEGATED_ACCESS_AUD` | PRD placeholders | Confirm Pages/Admin delegated Access path or disable delegated path until ready |
| Admin principal | `admin_principals.access_subject` for PRD owner/operator | No PRD D1 yet | Seed only after PRD Access login subject is known |
| Domains | `api.lastrail.app`, `admin.lastrail.app` | Configured as PRD routes, not verified here | Confirm DNS/custom domain approval and Access protection for admin host |

Notes:

- Google server verification uses the Google `sub` as provider subject after ID
  token verification. Email is not the identity key.
- `GOOGLE_AUTH_AUDIENCES` is configuration, not a secret, but it must come from
  the real PRD app registration and must not be guessed.
- No Google client secret is currently required by the Game API ID-token verify
  flow; if the app-registration flow introduces one later, add it as a Worker
  secret and do not expose it in D1, logs, or docs.

<a id="source-0a74736945cfcb2f-prd-preflight-commands"></a>
#### PRD Preflight Commands

Run only after the required inputs above are confirmed.

```bash
pnpm test
pnpm typecheck
pnpm lint
pnpm d1:local:verify
pnpm wrangler:types
pnpm --filter @lastrail/game-api wrangler deploy --env prd --dry-run
pnpm --filter @lastrail/admin-api wrangler deploy --env prd --dry-run
pnpm --filter @lastrail/async-worker wrangler deploy --env prd --dry-run
```

Before actual deploy, validate the server deployment plan with the common
release-policy schema. The plan must include the new server version, clean
source commit, versioned release branch, all service deploy tags, DB
compatibility class, and local regression/dry-run success. This validation is a
local preflight; it does not create branches, tags, remote resources, secrets,
or R2 objects by itself.

The local preflight entrypoint is:

```bash
pnpm preflight:server-deploy -- --plan <plan.json> --service game-api --env prd --history <used-history.json>
```

The supported launch-safe deploy dry-run entrypoint wraps the same mandatory
preflight before invoking Wrangler:

```bash
pnpm deploy:server:dry-run -- --plan <plan.json> --service game-api --env prd --history <used-history.json>
```

Run it once per deployed service before Wrangler dry-run and before actual
deploy. The optional history file is a sanitized list of prior
`environment/service/serverVersion` tuples and is used to reject version reuse.
The dry-run wrapper refuses actual deploy mode until a separate approved release
operation wires that path explicitly. Invalid plans, target mismatches, and
server-version reuse fail before Wrangler is called. The command prints only
scrubbed release identity and never prints secrets, tokens, raw request bodies,
Google tokens, RNG material, raw probability tables, or R2 credentials.

The shared release-flow helper models the approved production order with local
fake executors: preflight, deploy, remote smoke, then immutable success
history/current pointer writes. It writes success history only after deploy
execution and remote smoke both pass. Deploy failure or smoke failure writes a
separate sanitized failure-attempt record and leaves success history/current
untouched; current-pointer conflicts are degraded evidence states, not automatic
redeploys or D1 rollback triggers. Real deploy execution, remote smoke, and R2
history writes still require explicit environment approval.

Remote PRD checks must be value-scrubbed:

```bash
pnpm exec wrangler d1 migrations list lastrail-core-prd --remote --config apps/game-api/wrangler.jsonc
pnpm exec wrangler d1 execute lastrail-core-prd --remote --command "PRAGMA quick_check;" --config apps/game-api/wrangler.jsonc
pnpm exec wrangler d1 execute lastrail-core-prd --remote --command "PRAGMA foreign_key_check;" --config apps/game-api/wrangler.jsonc
pnpm --filter @lastrail/game-api wrangler secret list --env prd
pnpm --filter @lastrail/admin-api wrangler secret list --env prd
pnpm --filter @lastrail/async-worker wrangler secret list --env prd
```

Do not print secret values. Secret list output should be used only to confirm
presence by name.

<a id="source-0a74736945cfcb2f-deploy-order-candidate"></a>
#### Deploy Order Candidate

1. Create/confirm PRD D1 and PRD Queue.
2. Replace PRD config placeholders with the approved PRD resource ids and Access
   issuer/audience values.
3. Regenerate Worker types and run the final local regression.
4. Apply PRD D1 migrations after explicit PRD migration approval. If a migration
   is compatibility-breaking, first confirm new app availability, server-side
   minimum-version admission, old-session/retry tolerance, and rollback target
   compatibility with the current D1 state.
5. Deploy PRD Workers with dry-run first, then actual deploy after approval.
6. Inject/rotate PRD Worker secrets only through Wrangler secret storage.
7. Run scrubbed PRD smoke for public Game API, protected Admin API, and async
   worker queue health where credentials permit.
8. Write immutable sanitized server deployment history and update the current
   pointer only after deploy and smoke pass. If deployment succeeds but the R2
   evidence write fails, mark the release evidence as degraded and reconcile the
   pointer; do not auto-deploy again and do not run D1 rollback.
9. Tag the verified release commit as `act1-launch-YYYY-MM-DD`.

<a id="source-0a74736945cfcb2f-prd-smoke-candidate"></a>
#### PRD Smoke Candidate

- `GET /status` and `GET /v1/client-config` on Game API.
- Guest auth and refresh.
- Google sign-in/link with real PRD Google ID token only after app registration
  values are configured.
- Run start/finish/replay, story raid failure stamina refund, reward
  preview/claim, login bonus claim, offline preview/claim, story progress, and
  gacha 1/10 pull with PRD-safe test accounts.
- Admin read-only support, ops error logs, story progress, offline reward, runs,
  and players only through PRD Cloudflare Access and authorized admin principal.

Keep smoke logs free of raw tokens, Authorization headers, raw request bodies,
Google ID tokens/full claims, RNG material, probability/full weight tables,
raw multiplier internals, and secrets.

<!-- source-body:source-0a74736945cfcb2f:end -->


<a id="source-7e2cb4ac27ccff9b"></a>
## Release DB and API compatibility policy

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/cloudflare/release-db-api-compatibility-policy.md`  
원문 SHA-256: `624a8ab10643daf5c8734e9fd22e88c78172fad1d771d2ed99cbbd5bfe4f54ce`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-7e2cb4ac27ccff9b:start -->
<a id="source-7e2cb4ac27ccff9b-release-db-and-api-compatibility-policy"></a>
### Release DB and API compatibility policy

This policy is the launch rule for app, API, database, and content release
compatibility. It separates four version axes:

- App build: the installed Unity app `versionCode` and display version.
- Server code: Cloudflare Worker deployment versions.
- Database state: D1 migrations and durable data.
- Content release: immutable Addressables/catalog manifests and active pointers.

Server code releases also carry a LAST RAIL server version such as
`2026.09.06.1`. This is separate from Unity app versions and Cloudflare's
runtime deployment id.

<a id="source-7e2cb4ac27ccff9b-confirmed-policy"></a>
#### Confirmed Policy

Compatibility-breaking DB-changing updates are protected by the minimum
supported app version policy. Additive DB changes that old clients and old
Workers can ignore do not require a forced app update by themselves.

Confirmed launch rule:

- DB-changing updates that break compatibility must raise or enforce the
  server-owned `minimumVersionCode` gate before the incompatible behavior is
  reachable.
- API-only updates must be designed and verified as rollback-compatible by
  default. If an API change is not rollback-compatible for old installed apps,
  it uses the same minimum-version gate as a breaking DB change.

D1 migrations are treated as forward-moving operational changes, not ordinary
rollback targets. If a DB migration creates an issue, the default recovery path
is a new forward migration, correction record, or audited repair record. Do not
offer an Admin "rollback DB" action in the launch surface.

Cloudflare Worker code rollback remains available only when the database shape
and data semantics are still compatible with the previous Worker version.

Do not deploy a Worker without an explicit, unique server version. Reusing a
server version for a later deployment is forbidden because support, Admin
evidence, and ops error logs must be able to identify the exact runtime.

Before a deploy, preserve the verified source with a release branch whose name
includes the server version, for example
`release/YYYY-MM-DD-2026.09.06.1`. Create immutable service-specific deploy tags
for each Worker, for example `game-api-2026.09.06.1` and
`admin-api-2026.09.06.1`. Branches and tags preserve source state; they are not
database backups.

<a id="source-7e2cb4ac27ccff9b-migration-classes"></a>
#### Migration Classes

Compatible migrations may ship before a minimum app version raise:

- add a new table;
- add a nullable column;
- add an index;
- add a read-only table used only by newer code;
- add rows or metadata that older code ignores safely.

Compatibility-breaking migrations require an enforced minimum app version gate
before they become active:

- remove or rename a table or column;
- make a nullable field required for an existing route;
- change the meaning, enum domain, or validation semantics of existing data;
- require a client request field that old apps cannot send;
- stop returning a field that old apps require.

Destructive cleanup should be a later release after old app versions are blocked
by an actual server-side admission gate and release verification confirms the
cleanup is safe. A stored `minimumVersionCode` document alone is not enough.

<a id="source-7e2cb4ac27ccff9b-deploy-order"></a>
#### Deploy Order

Use this order for DB-sensitive changes:

1. Add backward-compatible schema/data first.
2. Choose a new unique server version and verify that it has not been used for
   the target environment/service pair.
3. Run local regression, config diff, migration compatibility review, and
   Wrangler dry-run.
4. Create the versioned release branch and service deploy tags only after the
   source is clean and verified.
5. Deploy server code that can read old and new shapes.
6. Release the new Unity app through the approved app distribution flow.
7. Activate or raise `minimumVersionCode` only after the new app is available
   and the Game API admission check consumes the policy.
8. Drain or tolerate existing sessions, cached offline flows, and retry traffic
   from old clients during the compatibility window.
9. Apply compatibility-breaking behavior only after the minimum app gate is
   active and verification confirms old clients cannot reach the incompatible
   contract path.
10. Do destructive cleanup in a separate explicitly approved migration.

If any step fails, rollback Worker code only when the current D1 state remains
compatible with the target rollback version. Otherwise, roll forward. Before a
Worker rollback, verify that the rollback target can safely read current schema,
current enum values, current idempotency records, current ledger records, and
current content/version pointers.

The actual Cloudflare Worker deployment id remains runtime truth after deploy.
The LAST RAIL server version is the human/support release identity that links
source, tests, deploy tags, smoke results, and Admin evidence. Store only
sanitized deployment metadata; never store secrets, raw auth headers, raw
request bodies, Google tokens, RNG material, or raw probability/weight tables.

<a id="source-7e2cb4ac27ccff9b-api-change-policy"></a>
#### API Change Policy

API changes should remain rollbackable by default:

- Add fields instead of replacing fields.
- Make new response fields nullable or optional first.
- Keep existing field names, types, and meanings stable.
- Add new endpoints rather than changing old endpoint behavior when possible.
- Keep compatibility windows for old clients until the minimum app gate is
  active.

Breaking API changes require the same enforced minimum app version protection as
compatibility-breaking DB changes. Startup update prompts are useful UX, but
they do not replace server-side admission for direct API calls, stale sessions,
offline retry, or tampered client payloads.

Deploy evidence may carry an optional API compatibility block:

- `api.compatibility = NO_API_CHANGE`: no API contract change in this deploy.
- `api.compatibility = ROLLBACK_COMPATIBLE`: the release plan verified old and
  new Workers can read current D1 state and tolerate old-client traffic during
  rollback.
- `api.compatibility = MIN_VERSION_GATE_REQUIRED`: the API change is breaking
  and must include `api.minimumVersionCode`.

`ROLLBACK_COMPATIBLE` evidence must set `api.rollbackVerified = true`.
`MIN_VERSION_GATE_REQUIRED` evidence must include the minimum app version code.

<a id="source-7e2cb4ac27ccff9b-admin-version-management-boundary"></a>
#### Admin Version Management Boundary

The Admin version-management surface may manage:

- current app display version and `versionCode`;
- `minimumVersionCode` policy;
- approved content release manifests and active content pointers;
- compatibility/readiness status for app, server, DB, and content axes.

It must not:

- run D1 rollback;
- delete old content as a side effect of a minimum version raise;
- treat a version document save as Worker deploy success;
- treat a version document save as content pointer activation unless a separate
  active-pointer operation exists;
- lower `minimumVersionCode` automatically during rollback.

Content rollback means moving an active content pointer to a previously approved
compatible manifest. Server Worker rollback uses Cloudflare Worker version
rollback. Database recovery uses forward migrations or audited corrections.

Admin should present server policy as read-only operational evidence:

- Server version is independent from Unity app version.
- DB rollback is not supported from Admin.
- Breaking DB updates require minimum app version gating.
- API-only updates are rollbackable only after compatibility verification.
- Breaking API updates require the same minimum app version gating as breaking
  DB updates.

Admin may show server deployment history and current deployment pointers as
read-only evidence. A successful deploy whose deployment-history write fails
must be marked degraded until the actual Worker id and server history pointer
are reconciled. It must not trigger an automatic second deploy or a D1 rollback.

<a id="source-7e2cb4ac27ccff9b-server-deployment-metadata"></a>
#### Server Deployment Metadata

Server deployment metadata should be written as immutable history records plus a
small current pointer, separate from the app/content version document used by
`GET /v1/client-config`.

Candidate R2 keys:

- `server-deployments/<env>/<service>/<serverVersion>.json`
- `server-deployments/<env>/<service>/current.json`

The immutable record should contain only sanitized fields:

- `releaseId`
- `environment`
- `service`
- `serverVersion`
- verified `commitSha`
- versioned `releaseBranch`
- immutable service deploy tag
- Cloudflare Worker deployment id and previous known-good deployment id
- DB compatibility class and `minimumVersionCode` when required
- API compatibility class, rollback verification status, and API
  `minimumVersionCode` when required
- linked content `releaseId`/`contentVersion`, not the full manifest body
- local regression, dry-run, remote smoke, and current-pointer status

The current pointer may move only after deploy and smoke verification.
Successful deploys are written to immutable deployment history. Failed deploy or
smoke attempts are written to separate sanitized failure-attempt records and do
not become current. If deploy succeeds and R2 current-pointer write fails, the
runtime remains live and the record must show a degraded evidence state until
manually reconciled through the approved release process.

The local implementation exposes a deployment-history helper that writes the
immutable history object with an R2 conditional `If-None-Match: *` precondition
and moves the current pointer with an `If-Match` / `If-None-Match` conditional
write. A duplicate write with the same sanitized evidence is treated as an
idempotent replay. A different object at the immutable history key or a stale
current-pointer etag is a degraded evidence conflict, not a trigger for another
deploy or a D1 rollback.

The helper is covered with a local fake R2-compatible store. Real R2 writes are
still a deploy-operation step and require explicit environment approval.

The local release-flow model also verifies ordering with fake deploy and smoke
executors: immutable success history and the current pointer are written only
after preflight accepts, deploy succeeds, and remote smoke passes. Deploy or
smoke failure writes a sanitized failure-attempt record under:

- `server-deployments/<env>/<service>/<serverVersion>/failures/<attemptId>.json`

Failure attempts do not write the success history key and do not move the
current pointer. A pointer write conflict after successful deploy/smoke is
degraded evidence that must be reconciled through the approved release process;
it must not trigger automatic redeploy or D1 rollback.

<a id="source-7e2cb4ac27ccff9b-game-api-admission"></a>
#### Game API Admission

Game API `GET /v1/client-config` evaluates an app build code supplied by the
client in `X-LastRail-App-Version-Code` against the server-owned
`minimumVersionCode`. The app display version string is for humans and must not
be the authoritative comparison key.

The admission check is server-owned and returns explicit fail-closed states when
version policy or approved compatible content release metadata is missing. It
must not trust a display version string, R2 folder name, or Unity-only update
prompt as the only guard.

Compatibility-breaking migrations still require a release plan that verifies
the current version document, available app build, content release pointer, and
rollback/roll-forward path before activation.

<!-- source-body:source-7e2cb4ac27ccff9b:end -->


<a id="source-b1bfe7845e3ec25c"></a>
## Backup And Recovery

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/operations/backup-recovery.md`  
원문 SHA-256: `2188b11340754e3b79d81a06febcf23d8f1e18d41f7a2089b69b334344ba2ab9`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-b1bfe7845e3ec25c:start -->
<a id="source-b1bfe7845e3ec25c-backup-and-recovery"></a>
### Backup And Recovery

Production migration requires a restore point or backup before applying SQL. Production direct SQL edits are prohibited.

Economy recovery should prefer ledger correction, reversal, or compensation before full database restore.

<!-- source-body:source-b1bfe7845e3ec25c:end -->


<a id="source-bc08b6cddc2e5a52"></a>
## 비상 장비 — 보유·사용·지원 버튼 연결 상세안

원출처: `docs/design/EMERGENCY-EQUIPMENT-DETAIL-20260916.md`  
원문 SHA-256: `1d2d8c83b2d39c4c0d537a932df08a1c3c331b466807045753a4601bfe119816`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Emergency consumable shared slot, price25,350ms hold, E01obstacle/E02lure/E03repair−5s proposals; distinct from category13 gear and not approval.

<!-- source-body:source-bc08b6cddc2e5a52:start -->
<a id="source-bc08b6cddc2e5a52-비상-장비--보유사용지원-버튼-연결-상세안"></a>
### 비상 장비 — 보유·사용·지원 버튼 연결 상세안

2026-09-16. 사용자 다음 상세기획 요청. 작전 물자/정비역의 일회용 장비를 구체화한 설계 후보다. 아래 효과·입력·수치는 아직 런타임/사용성/밸런스 검증 전이다. 기존 증강과 전투 스킬을 임의 변경하지 않는다.

<a id="source-bc08b6cddc2e5a52-1-역할과-보유"></a>
#### 1. 역할과 보유

비상 장비는 특정 위험에 대응하는 한 번의 선택이다. 객차 개조처럼 지속되지 않고, 지원 스킬을 복제하는 추가 충전도 아니다. 상점에서는 장비 효과·적격 대상·연결 버튼·1회 사용을 명시한다.

- 첫안은 운행 공용 보관칸1개, 수량1개. 세 종류를 동시에 쌓지 않는다. 구매 시작 가격25 작전 물자이며 검증값이다.
- 보유 중 다른 장비를 사면 기존 장비 폐기+새 구매를 한 번 확인한다. 기존 장비 환불/판매/무료 교환은 없다. 동일 장비 재구매는 막는다.
- 다음 정비역/웨이브까지 미사용 장비를 유지한다. 재접속도 보존하고, 작전 종료 때 소멸한다. 종료 시 사용을 강요하는 알림은 없다.
- 장비가 없어도 기본 전투 조작/장애물 해결/객차 복구는 가능해야 한다. 특정 장비를 필수 열쇠로 요구하는 맵은 만들지 않는다.

<a id="source-bc08b6cddc2e5a52-2-기존-세-버튼으로-조작"></a>
#### 2. 기존 세 버튼으로 조작

|입력|동작|
|---|---|
|일반 짧은 터치|기존 지원 스킬. 장비를 소비하지 않음|
|장비가 연결된 버튼 길게 누르기|장비 사용 패널 열기. 이 동작 자체로 장비/스킬 사용 없음|
|패널에서 대상 확인 후 ‘장비 사용’|전용 장비 명령 한 번 요청|
|취소·패널 밖 터치·닫기|사용 없음. 기본 스킬도 발사하지 않음|
|AUTO|기존 스킬 정책만 수행. 장비 사용 금지|

길게 누르기 시작 기준은350ms 검증안이다. 임계시간 전에 놓으면 정상 터치, 길게 누르기가 성립한 뒤 손을 놓으면 기본 클릭을 억제한다. 드래그 이탈·포인터 취소 시 둘 다 실행하지 않는다. 키보드/패드에는 같은 패널 열기 액션을 매핑하며 업무 코드를 별도로 복제하지 않는다.

장비가 있는 버튼에 작은 장비 표식과1개 표시, 첫 획득 시 짧은 ‘길게 눌러 장비 사용’ 안내를 제공한다. 새 상시 네 번째 버튼은 추가하지 않는다. 패널 안 사용/취소는 일시적인 UI다. 길게 누르기 발견성·반응 지연이 실제 모바일에서 나쁘면 동작을 확정하지 않고 입력 설계를 재검토한다.

기본 스킬이 쿨다운이어도 장비 패널은 열 수 있다. 스킬 실행 가능 여부와 컨테이너의 장비 입력 가능 여부를 분리한다. 반면 전투 종료·전역 입력 잠금·명령 권한 없음에서는 둘 다 거부한다. 일시정지 중에는 미리보기만 가능하고 실제 장비 사용은 재개 후에만 허용한다.

장비 패널만 열었다고 시간이 느려지거나 멈추지는 않는다. 전투 중에는 대상 상태가 변할 수 있으므로 실행 순간 재검증한다. 정비역에서는 정보 확인만 가능하며 장비 발동은 금지한다.

<a id="source-bc08b6cddc2e5a52-3-첫-후보-3종"></a>
#### 3. 첫 후보 3종

<a id="source-bc08b6cddc2e5a52-e01-장애물-파쇄탄--지원-사격-버튼"></a>
##### E01 장애물 파쇄탄 — 지원 사격 버튼

문구: **현재 선로의 파괴 가능한 장애물에 파쇄탄을 사용합니다.**

- 현재 선택 선로에 있고 발견됐으며 유효 사거리 안인 지상 장애물1개. 여러 개면 패널에서 한 개 선택. 적·언덕 포대·공중 낙석·다른 선로 장애물은 대상이 아니다.
- 별도 유한 파쇄 피해를 주며 항상 파괴를 보장하지 않는다. 예상 잔여 내구도 또는 예측 불가 사유를 표시한다. 파괴 불가 지형에는 사용 버튼을 활성화하지 않는다.
- 운용: 장애물을 제거할 시간이 부족할 때 한 번 써서 통과 여유를 만든다. 기본 사격으로 해결 가능하면 아껴 다음 구간에 쓴다. 충돌 전 실제 발사/도착 시간이 필요하며 충돌 후 소급 취소하지 않는다.
- 지원 사격 쿨다운/공격 예산을 초기화하지 않는다. T07방벽 방출/T12탄막/T18예비전원/O18외부탄창 준비를 만들거나 소비하지 않는다.
- T06 잔해 준비의 기존 원본 공격 자격에 자동 포함하지 않는다. 장애물 파괴 사건은 남지만 장비가 공짜 재활용 연쇄를 만들지 않는다. 별도 보상 목표가 해당 장애물 파괴를 요구하면 목표의 원인 허용표에 장비를 명시하고 고유 사건당 한 번만 지급한다.

<a id="source-bc08b6cddc2e5a52-e02-유인-신호탄--방어막-버튼"></a>
##### E02 유인 신호탄 — 방어막 버튼

문구: **유도 가능한 적의 다음 외부 공격 1회를 신호탄으로 유인합니다.**

- 현재 외부 위협 중 유도 가능 태그와 향후 표적 확정 전 행동이 확인된 적1개를 패널에서 선택한다. 선택 적의 다음 적격 공격 한 회가 신호탄을 조준하게 하는 한시 표적 상태다.
- 이미 발사된 탄이나 확정된 지점 포격은 변경하지 않는다. 광역 공격의 파편/범위 피해를 무조건 삭제하지 않는다. 신호탄 위치가 열차를 안전하게 피하는 유효 외부 공간인지 확인해야 한다.
- 대상은 태그/예고로 명시한다. 모든 포대에 통하거나 모든 적이 공격을 멈추는 연막 무적 효과가 아니다. 해당 적이 죽거나 기회 없이 기한이 끝나면 사용한 장비는 돌아오지 않는다.
- 운용: 고속 통과 중 위험한 다음 한 발을 다른 곳으로 보내거나, 저속 장애물 처리 시간을 번다. 이미 날아오는 탄은 기존 방어막/요격으로 대응한다.
- 방어막을 생성하지 않으므로 T02/T07/T11/T14/T17/T19의 원본 방어막 사건이 아니다. T11요격과 함께 있으면 실제 남은 포탄만 처리하고 유인 공격을 다시 요격 보상으로 만들지 않는다.
- 외부 표적 확정 시점·안전한 유인 위치·유도 가능 행동 태그가 새로 필요하다. 현재 구현 지원을 확인하지 못했으므로 3종 중 공간 계약 의존도가 가장 높은 후보이며 제작 완료 전 판매 금지.

<a id="source-bc08b6cddc2e5a52-e03-응급-복구-팩--긴급-수리-버튼"></a>
##### E03 응급 복구 팩 — 긴급 수리 버튼

문구: **파손된 객차 한 칸의 남은 복구 시간을 앞당깁니다.**

- 편성에 남아 있고 기본 복구 절차가 진행 중인 객차1개를 선택한다. 정상 객차의 HP 회복이나 영구 제거 객차 재생은 없다.
- 남은 복구 시간에서 정해진 시간을 차감한다. 검증 시작값5초. 남은 시간이 더 짧으면0까지만 적용하고 나머지는 버린다. 기본 복구가 끝나면 기존40%HP/보호/장치 대기 등 해당 모드의 승인된 복구 규칙을 따르며, 이 문서가 그 수치를 새로 확정하지 않는다.
- 운용: 핵심 포격칸이 돌아올 때까지 버틸지, 장비를 써 지금 복귀를 앞당길지 결정한다. 파손 횟수는 초기화하지 않으며 복귀 즉시 무료 사격하지 않는다.
- T03/T13촉진과 같은 남은 복구시간 소유자에 한 번 적용한다. 시간을 음수로 만들거나 완료 사건을 두 번 발생시키지 않는다.
- T18한시가동 중인 복구 객차도 적격이면 원래 복구를 앞당길 수 있다. 복구 완료 시 기존 T18 종료/정상 복귀를 따른다. 새 예비전원은 생성하지 않는다.
- 긴급 수리 원본이 아니므로 T06키트/T13새 정비창/T18준비/O01미라 연계/기존 수리 게이지를 발동시키지 않는다. M04교체식 장갑도 보충하지 않는다.

<a id="source-bc08b6cddc2e5a52-4-명령-성공실패와-시간"></a>
#### 4. 명령 성공·실패와 시간

보유→대상 선택→요청 중→사용 확정/실패 복귀의 상태를 구분한다. UI에서 먼저 개수를0으로 만들고 실패 후 복구하는 방식으로 이중 사용을 허용하지 않는다.

- 접수 순간 작전·역할·전투 상태·장비 소유·적격 대상을 검증한다. 거절/취소/없어진 대상이면 소비0.
- 효과 행동이 권위 상태에 확정될 때 장비 소비와 함께 원자적으로 기록한다. E01의 발사 확정 이후 대상이 사라지거나 E02유인 기회가 사라지는 것은 이미 실행한 결과이며 환불하지 않는다. E03즉시 효과도 동일 원자성 원칙을 따른다.
- 장비 행동에는 고유 명령 ID를 사용한다. 응답 유실 시 같은 ID 상태를 조회/재시도하며 새 장비 사용으로 처리하지 않는다. 화면을 닫아도 이미 접수된 명령을 무조건 취소됐다고 표시하지 않는다.
- 스킬과 장비는 같은 버튼 모양을 쓰지만 서로 다른 도메인 명령이다. 기존 스킬 쿨다운을 건드리지 않으며 장비도 원본 스킬 시전 사건을 발행하지 않는다.
- 각 장비는 정해진 준비/발사 시간을 가진다. 기존 스킬과 같은 틱이면 안정적 명령 순서와 각 대상 최신 상태로 처리한다. 별도 장비 사용이 스킬 후딜을 취소하는 우회가 되지 않도록 외부 행동 잠금 조건을 명시해야 한다.
- 장비 수명·발사·복구 차감은 전투 시계 기준. 일시정지/증강 hold에서 현실 시간이 흘렀다고 효과가 진행하지 않는다.

<a id="source-bc08b6cddc2e5a52-5-재고경제협동"></a>
#### 5. 재고·경제·협동

각 역에서 실제 구현·편성 적격성이 확인된 장비 중 서로 다른2종을 각1개 제시하는 기존안이다. E02가 미구현이면 빈자리를 가짜 판매로 채우지 않는다. 출발 시 고정한 재고는 보유 증강/재접속으로 추천 재추첨하지 않는다.

2인 첫 후보는 공용 장비1개, 열차 담당이 사용하고 요원 담당은 대상 요청을 전달한다. 공용 물자 구매권한과 함께 아직 최종 확정 전이다. 역할 위임/연결 끊김 정책 없이 다른 클라이언트가 임의 소비하지 못한다. 개인별로 같은 장비를 복제하지 않는다.

<a id="source-bc08b6cddc2e5a52-6-표현책임검증"></a>
#### 6. 표현·책임·검증

공용 지원 스킬 아이콘은55기준 그대로 유지한다. 작은 독립 장비 표식/수량과 사용 패널만 조합한다. 스킬 아이콘 자체를 상품 그림으로 바꿔 같은 스킬 식별을 깨지 않는다. 장비 아이콘·프레임·대상·설명·가격·수량·대기/실패 표시는 독립 자산/텍스트다.

View는 press/hold/cancel 의도와 표시만 담당한다. Controller가 장비 사용 유스케이스에 연결하고 run 장비 소유자가 소비·이력을 관리한다. 재바인딩/화면 종료/포인터 취소/해제 후 응답을 검증한다. 같은 버튼의 일반 터치와 길게 누르기가 두 명령을 발행하면 실패다.

미실행 검증: 일반탭1스킬,hold후release기본0,장비확정1,취소0,AUTO소비0,쿨다운중장비사용,잠금중모두거부,범위밖대상/사망/이미발사표적,마지막복구틱경합,중복명령/재접속,위치변경후재검증,소비실패원복,지원·증강게이지불변,2인동시입력,모바일조작성/마지막순간사용지연.

다음은 목적지·정비역 배치: 분기 사전 공개,기본정비2회와선택회수기회,실제작전길이에따른물자수입및위험배분을 함께 설계한다. 비전투우선/실경제변경금지를 유지한다.

<!-- source-body:source-bc08b6cddc2e5a52:end -->


<a id="source-73ed48ca0540200c"></a>
## 외부 공간·영역·유효 거리 공통 상세안

원출처: `docs/design/EXTERIOR-SPACE-RANGE-CONTRACT-20260916.md`  
원문 SHA-256: `aef1a68a89b3152556f6b25602d57f71d54c09636fe6f95e73948ca6a0b01e9b`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Distance-space coordinate/source contract, target/range projection and snapshot fields, warning/interaction/readmodel boundaries.

<!-- source-body:source-73ed48ca0540200c:start -->
<a id="source-73ed48ca0540200c-외부-공간영역유효-거리-공통-상세안"></a>
### 외부 공간·영역·유효 거리 공통 상세안

2026-09-16 사용자 상세기획 진행. 계류 방벽/거점 전개/외부 궁극기/잔류/회수·호위가 공유할 설계 계약이다. 실제 월드 공간·물리·공격 모델 구현 완료 아님. 기존 출발시고정맵/분기전체객차추종/비선택경로공격불가 원칙을 유지한다.

<a id="source-73ed48ca0540200c-1-서로-다른-세-좌표를-분리"></a>
#### 1. 서로 다른 세 좌표를 분리

|좌표|용도|금지|
|---|---|---|
|맵 논리 월드 좌표|외부 표적/영역/사거리/포대 위치|카메라 패럴랙스 픽셀로 판정|
|선로 경로ID+호 길이|선두·객차 이동,분기 이력,꼬리 통과|다른 분기의 진행거리 숫자만 비교|
|화면 좌표|카메라 투영·터치·효과 표시|해상도 변경으로 판정 범위 변경|

맵 단위는 공통 물리 길이 단위로 고정하고 월드 배치 변환은 이동/회전만 사용한다. 첫 지형은 기존 수평 연결 규격을 따른다. 높이/시야 차폐는 명시된 데이터로 처리하며 임의의 그림자/언덕 그림에서 판정하지 않는다. 실미터 대비 아트 스케일과 범위 수치는 회색상자 검증 후 확정한다.

객차 위치는 기관차 화면 위치에서X를빼는방식이아니라,실제로지나온선로이력에서객차길이+간격만큼뒤를추적한다. 기관차가합류해도마지막객차가이전분기에남아있을수있다. 조난객차추가후에도동일계약이며최대길이는맵생성시검증한다.

<a id="source-73ed48ca0540200c-2-공통-영역표적-기록"></a>
#### 2. 공통 영역·표적 기록

영역에는 run/region ID,부모효과·소유장치ID,월드형상,활성선로/모듈범위,생성/만료전투틱,예산,적격대상태그를둔다. 특정언어의자료구조를강제하는것은아니다. 원형·캡슐·경로구간 중효과에맞는명시형상을사용하며첫범위에임의복잡메시판정은필요없다.

표적은고유ID,월드기준점,부피/차폐판정,생존/퇴장상태,영역(내부/외부),경로귀속을갖는다. 화면밖과논리퇴장은다르다. 카메라밖에서도유효탄/영역은존속할수있지만이미지숨김만으로피해를없애지않는다.

거리 판정은 목적에 따라 나눈다.
- 주행/도착 예상:확정된선로호길이와실제속도적분. 목표속도로나눈값을확정도착시간으로쓰지않음.
- 공격/통신 거리:월드공간기준점사이거리+효과별유효경로/차폐조건. 직선거리가짧아도비선택경로는표적이아님.
- 충돌/위험 점유:객차몸체가차지하는범위와위험형상의교차. 선두점이벗어났다는이유로전객차안전처리금지.

얇은영역을고속한틱에넘을때이전→현재이동구간을검사해진입/이탈을누락하지않는다. 동일틱경계사건은권위시뮬레이션의안정된시각/순서를따른다. 화면프레임/클라이언트도착순서로결과를바꾸지않는다.

<a id="source-73ed48ca0540200c-3-t19-계류-방벽"></a>
#### 3. T19 계류 방벽

지원방어막실행시기관차보호기준점을중심으로월드고정영역을만든다. 열차를따라움직이지않는다. 첫안은반경이명시된영역이며화면전체보호효과가아니다.

보호여부는 **실제피격대상의보호기준점이명중시점에영역안인가**로정한다. 각객차는자기중심기준점,열차직접공격은기관차기준점을사용한다. 한객차가남아있다고전체열차가보호되는것도,선두가나갔다고후미보호가사라지는것도아니다. 위험접촉검사는몸체교차지만방벽보호는이기준점규칙이라는차이를시각적으로설명해야한다.

영역의열차보호예산은부모방어막하나를공유한다. 여러객차에같은잔량을복제하지않는다. 기존분배/상한계약에따라T17요원분은분리하고열차분만영역에귀속한다. T14펄스는같은영역과부모예산안에서만작동한다.

T07은지원사격중심점이그영역안이고남은적격예산이있을때소비한다. 범위밖이면원래지원사격만실행하고방벽잔량보존. 소비된방벽은그뒤피격에다시사용하지않는다.

열차가완전히벗어나도영역은원래만료까지존속하되전진만허용된경로에서다시따라오지않는다. 신규방어막시전의동일소유자영역은기존영역을교체하는첫안이며미소모예산환급없음. 원본지원방어막의중복상한과충돌하는지는구현전검증한다.

<a id="source-73ed48ca0540200c-4-t20-거점-전개"></a>
#### 4. T20 거점 전개

유효지원사격실행후선택외부구역내검증된전개점에Gun장치를배치한다. cliff/장애물속/비선택경로에임의포탑을그리지않는다. 유효전개점이없으면원본지원사격만실행하고전개실패사유표시,기존Gun을억지로중단하지않는다.

포대는월드위치에고정하고동일Gun의공격기회·탄창·개조상태를사용한다. 열차위Gun과분리포대가동시에공격하지않는다. 장치소유객차의파손/퇴장/복구상태를무시하지않는다.

통신거리는전개점↔소유객차장치기준점의월드거리로측정한다. 분기이후에도유효모듈연결/통신허용조건을만족해야한다. 첫전개구간에서허용한표적영역만공격하며지나간다른맵적을전체추적하지않는다.

거리상한이탈/수명종료/긴급수리원본명령/소유객차파손시신규포대공격예약을중지하고회수상태로전환한다. 이미발사된탄은원래판정으로종결한다. 첫안의회수는유한회수지연후차량장치역할복귀이며그동안공격없음. 물리적으로포탑이순간이동하며무료발사하는연출은금지한다. 회수시간은설정후검증,범위재진입으로회수취소·재전개반복하지않는다.

복귀시현재장치상태를재평가한다. 정상Gun이면기존남은주기후복귀,파손이면공격하지않는다. T13정비중이면기존정비정책,탄창은남은상태유지. T10기뢰/T11요격/T16예약/수정T18임시가동과동시전개우선권은아직미완료이며이번거리규칙만으로합성PASS하지않는다.

<a id="source-73ed48ca0540200c-5-o18-외부-지원-궁극기와-o14-잔류"></a>
#### 5. O18 외부 지원 궁극기와 O14 잔류

마지막직접선택영역/외부대상ID가유효할때만해당요원의외부변환을접수한다. 카메라이동·hover·자동표적이선택영역을바꾸지않는다. 대상이무효면궁극기/준비를소비하지않고명시거부하며내부적에게몰래전환하지않는다.

시전이실제확정되면공격영역·대상/지점·부모ID를고정한다. 이후대상사망/이동으로생기는빗나감·재지정은궁극기별원래규칙을따르며,시전후무효를이유로전부환급하지않는다. 내부요원슬롯번호를외부좌표로사용하지않는다.

O14 외부잔류는시전확정지점을중심으로고정한다. 적이벗어나면맞지않고다른적이들어오면같은예산/틱규칙으로판정한다. 집중대상변경으로영역이따라가지않는다. 시전자별활성1개,새잔류교체/수명종료/해당교전묶음의종료시삭제하는외부후보규칙이며내부O14의기존wave종료계약은별도로유지한다. 외부묶음과서버wave매핑은구현전확정필수다.

O03전파는같은외부적격영역안에서만. O20저장기본공격은여전히내부대상이므로외부궁극기위치를복사하지않는다. 세라외부표식매핑은별도미확정이며허용하지않는다.

<a id="source-73ed48ca0540200c-6-회수호위와-선로-이탈"></a>
#### 6. 회수·호위와 선로 이탈

회수설비는월드고정영역과접근가능선로를가진다. 진행의기준은기관차가아니라명시된회수장치기준점이다. 첫안은기관차후부견인장치한곳으로고정하되향후객차마다회수속도가증가하는효과를암묵적으로추가하지않는다.

작업은해당기준점이영역안이고유효작업상태일때만전투시간으로진행한다. 나가면진행정지,확정통과/기한끝이면실패. 되돌아가회수할수없고진행량을다른설비에이월하지않는다. 한틱에영역을가로지르면실제로영역안에있었던시간만계산한다. 도착한프레임에필요작업시간전부를주지않는다.

F03호위차량은플레이어객차가아닌독립경로/HP/위협대상의사건개체다. 합류지점의유효범위도달과생존으로성공확정. 플레이어열차가통과했어도구조차량이남아있으면성공아니다. 적표적AI/진행차단조건은별도사건구현과검증해야한다.

<a id="source-73ed48ca0540200c-7-포대투사체화면밖-처리"></a>
#### 7. 포대·투사체·화면밖 처리

고정포대는열차가사거리/선로노출을벗어나면새발사를멈춘다. 포대모델의화면밖퇴장과발사된탄의수명은분리한다. 발사된포탄은원래유효충돌/명중/만료규칙으로끝나며카메라밖이라는이유로삭제하지않는다.

전방장애물은포대처럼그냥사거리밖소멸시킬수없다. 파괴·충돌·비선택경로제외중명시된결과로종결한다. 비선택경로의장식포대/장애물은공격/회수/물자농사대상아니다.

맵표현오브젝트는화면밖에서해제가능하지만활성영역/탄/포대/호위/작업이참조하는논리데이터는종결전보존한다. 성능을이유로모듈을삭제해효과를조기종료하지않는다. 영구보존도금지하며소유자가참조종결후해제한다.

<a id="source-73ed48ca0540200c-8-숫자와-표시의-검증-방식"></a>
#### 8. 숫자와 표시의 검증 방식

첫밸런스수치는고정픽셀반경으로정하지않는다. 실제선로속도·객차길이·영역크기로보호체류/포대가동/회수기회를측정한다. 예시로순항속도v에서T19유효진행폭D의체류는직선·정속일때D/v에가깝지만,곡선/가감속/객차기준점은실적분한다. 이식은운영확정수치가아니다.

UI는영역테두리·선택대상·회수진행·통신거리이탈예고를별도레이어로표시한다. 실월드판정과같은변환을사용하고1280/1600또는카메라줌에서실제판정이바뀌지않는다. 범위밖거부와효과미구현을같은비활성문구로숨기지않는다.

<a id="source-73ed48ca0540200c-9-미실행-검증-목록"></a>
#### 9. 미실행 검증 목록

직선/곡선/분기두경로/합류,최소·최대·구조후편성,저속·고속·가감속·배속,얇은영역고속통과,객차일부만방벽내부,동일부모예산다중피격,T07소비경합,포대통신이탈과발사동시,파손회수,잔류재입장/교체,회수작업실체류,비선택경로차단,화면밖탄/모듈해제,재접속복원·동일맵버전·2클라이언트결과일치.

도메인소유자는선로/운행/영역/전투스케줄/사건으로책임을구분하고중복좌표판정함수를View에넣지않는다. 기록에는run/대상/영역/부모/판정시각/거부사유를남긴다. 다음단위는 **실제맵회색상자검증 시나리오와 제작할독립리소스 목록**이다. 신규코드/맵리소스/실START·경제쓰기없음.

후속: [확장 맵 검증·리소스 계획](45_MAP_STATION_DETAIL_CONTRACTS.md#source-4634883c310b311b). 기존6모듈재사용후보/G01~G12미실행/R01~R12필요목록과검수단계. 기존프로토타입PASS를신규기능완료로확대하지않음.

<!-- source-body:source-73ed48ca0540200c:end -->


<a id="source-05c99f4bad69cf6e"></a>
## 낙석·선로 잔해 피해와 감속 상세 제안

원출처: `docs/design/PHYSICAL-HAZARD-DAMAGE-20260915.md`  
원문 SHA-256: `57135eae66fcd093bdcb67a99f550cbace432811d380af2c6eb5b372bdd92a70`  
검토·적용 경계: Retain active pending actual consolidation or explicit current-vs-candidate review: Physical hazard formulas candidate; one direct-or-carriage pathway, transfer30%actualHP ≠ brokenfallback0.30; contact/destroy tie andslow values unapproved.

<!-- source-body:source-05c99f4bad69cf6e:start -->
<a id="source-05c99f4bad69cf6e-낙석선로-잔해-피해와-감속-상세-제안"></a>
### 낙석·선로 잔해 피해와 감속 상세 제안

2026-09-15 / Plan_5. 기존 방향을 구체화한 제안이며 신규 계수·감속 수치는 승인/구현 완료가 아니다. [속도·장애물](45_MAP_STATION_DETAIL_CONTRACTS.md#source-eeb6f5c756ecc820), [객차 피해](44_COMBAT_DETAIL_CONTRACTS.md#source-d25448b3ff1afe52), [낙석 우회](45_MAP_STATION_DETAIL_CONTRACTS.md#source-018bb2d2f8ee2b6c)를 따른다.

<a id="source-05c99f4bad69cf6e-위험을-구분한다"></a>
#### 위험을 구분한다

| 위험 | 주요 대응 | 판정 |
|---|---|---|
| 고정 포대 | 집중 사격으로 파괴 또는 속도로 사거리 통과 | 기존 발사/탄착 계약. 접촉 피해를 추가하지 않음 |
| 파괴 가능한 선로 잔해 | 미리 사격·감속·분기 우회 | 선두 최초 접촉, 전체 열차 직접 피해와 감속1회 |
| 낙하 중인 낙석 | 경고 구간을 속도·경로로 피함 | 활성 시간과 실제 차량 몸체의 최초 접촉1회 |

낙석에 고속 충돌 제곱식을 무조건 쓰지 않는다. 낙하 위치에 오래 머무는 위험과 선로 잔해에 고속으로 들이받는 위험은 다르다. 낙석의 사격 파괴 가능 여부는 위험 정의별로 명시하며 모든 낙석이 파괴 가능한 것으로 확대하지 않는다.

<a id="source-05c99f4bad69cf6e-선로-잔해--기존-제곱식-구체화"></a>
#### 선로 잔해 — 기존 제곱식 구체화

- `R = 남은 장애물HP / 최대HP`, `S = 접촉 직전 실제속도 / 순항속도`.
- 방어 전 충돌 피해: `floor(위험별 기본피해 × R × clamp(S, 0, 허용최고속도배율)^2)`.
- 고속1.3배에서 같은 잔해의 방어 전 피해는 순항의1.69배다. 반쯤 부순 잔해는 같은 속도에서 피해가 절반이다. 정수화는 전체 식 마지막에 한 번 한다.
- 기본피해는 콘텐츠 데이터이며 플레이어 최대HP에 자동 비례시키지 않는다. 방어는 기존 직접 열차 피해 계약에 따라 한 번 처리한다.
- 접촉 이전 파괴됐다면 피해/감속0. 이미 소비된 잔해를 후속 객차가 통과해도 재피해·재감속하지 않는다. 같은 사건을 바위 잔해 효과로 다시 만들지 않는다.
- 고속에서 기본피해를 더 올리는 별도 숨은 배율이나 요원 피해를 덧붙이지 않는다.

<a id="source-05c99f4bad69cf6e-낙하-중-낙석--접촉-차량에-따라-단일-경로"></a>
#### 낙하 중 낙석 — 접촉 차량에 따라 단일 경로

- 낙석별 방어 전 낙하피해를 명시한다. 이동 속도는 맞을지/벗어날지를 바꾸고 낙하피해 자체를 제곱 증폭하지 않는 안을 추천한다.
- 기관차 최초 접촉: 전체 열차 방어 후 직접 피해.
- 정상 객차 최초 접촉: 객차 방어 후 실제HP 손실, 그30%만 열차에 전이. 이미 승인된 전이 규칙을 사용한다.
- 이미 파손된 객차 최초 접촉: 별도 구조물 직접 피해. 시작 계수로 `floor(낙석 방어 전 기본피해 × 0.30)`을 제안하고 열차 직접 방어를 한 번 적용한다. 이는 실제HP손실30% 전이와 다른 경로다. 객차 장갑과 열차 장갑이 다르므로 결과가 정확히 같다는 보장은 없다.
- 같은 사건의 정상 객차 타격으로 방금 파손됐을 때 위 파손 fallback을 추가하지 않는다. 새로운 낙석 사건에만 적용한다.
- 새 계수0.30은 밸런스 제안이다. 기존 승인된 전이율30%라는 이유로 자동 승인하지 않는다. 위험별 계약에 명시하지 않은 채0이나 일반피해로 대체하지 않는다.
- 낙하 시작 때 여러 차량이 범위 안이면 전방부터 실제 몸체가 겹치는 첫 차량 하나를 고른다. 연결부 빈 공간은 차체로 취급하지 않는다. 맵/차량 권위 좌표를 쓰며 렌더 이미지 크기로 판정하지 않는다.

<a id="source-05c99f4bad69cf6e-감속-제안"></a>
#### 감속 제안

선로 잔해의 첫 플레이테스트 시작안:

- 접촉 직전 실제속도에서 최대40% 감소: `감속후속도 = min(접촉전속도, max(순항속도×0.20, 접촉전속도×(1−0.40×R)))`.
- 제한 유지시간은 전투 시간 `1.5초 × R`. 잔해HP가 절반이면 최대20%감속/0.75초다.
- 하한 때문에 더 느린 열차가 충돌로 가속되지 않도록 바깥 min을 둔다. 완전 파괴R0은 전체 충돌 처리를 생략한다.
- 유지 중 속도는 감속후속도를 상한으로 두되 사용자의 추가 감속은 허용한다. 목표 스로틀 위치는 유지하며 강제 제한 상태를 별도 점등한다.
- 종료 후 현재 속도에서 사용자가 지정한 목표로 기존 가속률에 따라 복귀한다. 즉시 목표속도로 순간이동하지 않는다.
- 연속된 별개 충돌은 새 피해를 각각 처리하되 속도 제한은 더 낮은 상한, 종료는 더 늦은 해제시각을 사용한다. 지속시간을 단순 합산하지 않는다. 새로운 충돌은 이미 감속된 실제속도로 피해를 계산한다.
- 강제 감속 중 저속 화력/순항 재충전 보너스를 중단한다. 해제 뒤 실제속도와 정상 안정화 조건이 다시 맞아야 켜진다.
- 방어막이 피해를 모두 흡수해도 물리 감속은 유지한다. 객차 복구 보호도 충돌 감속을 면제하지 않는다.
- 낙하 중 낙석 감속은 같은 콘텐츠별 감속 계약을 쓰되 잔여HP비율이 없는 낙석에 가짜R을 붙이지 않는다. 낙석별 감속값은 별도 미정이며 위 잔해 수치를 몰래 복사하지 않는다.

<a id="source-05c99f4bad69cf6e-시간사건-경계"></a>
#### 시간·사건 경계

- 같은 시뮬레이션 스텝에 파괴와 접촉이 있으면 권위 스텝 내 발생시각으로 먼저 발생한 사건을 처리하는 방향을 추천한다. 정확히 같은 시각은 유효 파괴 우선으로 하여 마지막 순간 성공한 사격이 충돌 피해를 남기지 않게 한다.
- 현재 코드가 스텝 내 순서를 표현하지 못하면 명시적 계약 추가 전 해당 경계 완료를 주장하지 않는다. 포대/일반공격 순서를 이 문서로 임의 변경하지 않는다.
- 충돌 ID별 피해·감속·소비기록을 같은 상태 변경으로 확정한다. 저장 실패/재전송에서 피해만 두 번 받거나 감속만 다시 시작하지 않는다.
- 패배한 열차에는 이후 감속 종료가 운행·장치·복구를 재개시키지 않는다. 시각 잔해 정리는 도메인 피해와 분리한다.
- 일반 분기와 위험은 증강 선택 대기 지점에 겹쳐 배치하지 않는다. 이 기획은 새로운 증강 지급 횟수나 강제 분기를 만들지 않는다.

<a id="source-05c99f4bad69cf6e-화면배치검수"></a>
#### 화면·배치·검수

- 위험 경고는 3차 시안의 외부 장면에서 거리·타입·방향으로 표시한다. 하단 레버 안에 낙석 그림/긴 경고 문구를 다시 넣지 않는다.
- 사격 가능한 잔해만 HP와 집중사격 표식을 제공한다. 미파괴 충돌 시 피해 받은 차량 하나와 짧은 감속 상태를 강조하며 여러객차 모두 피해 받은 것처럼 번쩍이지 않는다.
- 회피 불가능한 연속 충돌로 영구 저속이 되지 않도록 기존 맵 배치기의 가속/반응 여유 검사를 확장해야 한다. 맵 최소간격과 반응시간은 별도 검증값이다.
- 검수: 속도1/1.3 피해비, 잔여HP0/절반/전체, 방어막완전흡수+감속, 정상/파손 객차낙석, 같은사건후속차량, 연결부공백, 동시파괴/접촉, 감속중또충돌/재접속/패배, D설정단위와정수화.

승인 대기: 잔해40%최대감속·1.5초·0.20하한, 파손객차낙석 직접계수0.30, 동시파괴우선 계약. 남은 수치: 위험별기본피해·낙석감속·맵안전간격. 운영 적용/실제 충돌 검증은 하지 않았다.

<!-- source-body:source-05c99f4bad69cf6e:end -->


<a id="source-8944219b62a9c76d"></a>
## LASTRAIL 문서 통합·정리 실행 계획

원출처: `docs/superpowers/plans/2026-09-21-document-consolidation.md`  
원문 SHA-256: `dda7b7f05eb577ffb714f4fcb56e267eba82245f77fe51ba5d2001d4354e4daa`  
검토·적용 경계: Current document integration work plan, still updated by coordinator.

<!-- source-body:source-8944219b62a9c76d:start -->
<a id="source-8944219b62a9c76d-lastrail-문서-통합정리-실행-계획"></a>
### LASTRAIL 문서 통합·정리 실행 계획

> 실행: executing-plans 절차로 현재 작업에서 수행한다. 문서 점검이므로 게임 테스트 추가·빌드·배포·코드 변경은 하지 않는다.

**Goal:** 프로젝트 MD의 누락·중복·현행 원문 연결을 확인하고 기존 카테고리 체계와 단일 진입점으로 정리한다.
**Architecture:** 현행 전문은 plans_categories, 기존 상세 계약은 원위치 연결, 과거 검증/불변 백업은 보존한다. 전수 파일 목록과 의미 검토 범위를 구분한다.
**Spec:** 사용자 2026-09-21 전체 MD 점검/정리 요청, CODEX_INTEGRATED_HANDOFF_20260921.md, 루트 AGENTS.md.

<a id="source-8944219b62a9c76d-제약"></a>
#### 제약

- 미커밋 자료 보존. 내용이 없는 전문·정책·ID를 만들어 누락을 감추지 않는다.
- 사용자 최신 정정: 카드마다 무료3+추가1. 기존 공유 EXTRA 해석 철회.
- 백업/원본/QA는 이력, 외부 의존성 문서는 제외 근거 명시. 기존 상세 API 계약을 요약으로 대체하지 않는다.
- 경로 이동은 정확한 파일 단위, 충돌·참조 점검 후 실행하고 원래→현재 매핑을 남긴다.
- 삭제보다 보존 분류를 우선. Unity 자산/meta/소스/배포는 작업 범위 밖.
- 전수 목록·링크 검사와 전문 정책 대조는 별도 결과로 보고한다.

<a id="source-8944219b62a9c76d-작업"></a>
#### 작업

- [x] 전체 접근 가능 MD 목록/해시/역할/중복 및 제외·오류를 기록.
- [x] 누락 번호와 최신 전문을 저장소·자료에서 탐색, 기존 상세 계약을 담당 카테고리에 연결.
- [x] 읽은 문서의 정책 충돌/누락을 근거에 따라 정리하고 미검토는 남김.
- [x] 단일 진입점·원문 매핑·최신 규칙을 등록하고 검증된 중복 사본 정리.
- [x] 전후 해시·링크·번호·JSON 검사, 인계 갱신, 잔여 자료 요구 보고.

<a id="source-8944219b62a9c76d-판단-기록"></a>
#### 판단 기록

- 파일 날짜/길이만으로 우선순위를 정하지 않는다. 누락 전문은 기존 구현 설명으로 대체하지 않는다.
- 현 작업은 문서 정리다. 보존 백업까지 전문을 재검토했다고 주장하지 않으며, 해시 동일 사본은 대표 검토와 연결한다.

<a id="source-8944219b62a9c76d-종료-범위"></a>
#### 종료 범위

<a id="source-8944219b62a9c76d-후속-사용자-요청-전체-내용-충돌-대조"></a>
##### 후속: 사용자 요청 전체 내용 충돌 대조

- [x] 고유 상세 원문362개를 운영147/전투116/경제67/아트·규칙32로 전문 검토하고 개별 coverage를 남긴다. 추가 구현 계획/지침24개도 전문 검토했다.
- [x] 명시 대체·유효 전문·미승인 제안·진짜 미결정을 구분하여 기존 최종 폴더에 반영한다.
- [x] 기존 규칙에 빠진 반복보상/성장/화면 승인 전문과 호환 경계를 같은 폴더의 보충 문서로 연결한다.
- [x] 출처 보존/중복/삭제 불가를 개별 근거로 판정하고 링크·JSON·해시·정책 불변식을 재검사한다.

후속 결과는 docs/superpowers/plans_categories/DOCUMENT_RECONCILIATION.md와 QA/DocumentIntegration-20260921/semantic-audit/reconciliation-summary.json에 있다. 386개 선정 원문 전문 정책 대조와 모든 저장소 MD/API 필드의 무손실 이전은 다르다. 원본·API 계약·QA는 무조건 삭제하는 대상이 아니다.

현재 확보 문서의 목록·원문 연결·수정 diff·안전한 사본 이동·구조 검사를 완료했다. 이는 전체 MD 전문의 의미/API 대조 완료를 뜻하지 않는다. 남은 상세 계약 대조 범위는 QA/DocumentIntegration-20260921/REPORT.md를 따른다. 추가6개 전문은 작업 중 사용자 제공으로 모두 확보·병합했다.

<!-- source-body:source-8944219b62a9c76d:end -->

2026-09-22 물리위험 후속 후보: [44 v03](44_COMBAT_DETAIL_CONTRACTS.md#damage-shield-hazard-detail-v03)에 고정 피해/낙석감속/파손경로0.30/최종내림·예고/배치검증 구체화. 신규 승인 대기, 구run과 위 원문 보존. 현행 전체 위험 구현 완료 아님.

<a id="sns-channel-recovery-20260929"></a>
## 홍보 채널 선택 누락 복구 — 2026-09-29
원 통합기획 사용자 L27064 “페북 인스타 x”에 따라 **Facebook·Instagram·X**를 선택한 운영 채널로 기록한다. L27074는 관련 계정 확보 비용/구조 조사를 요청한 후속이며, 실제 계정 생성·소유·연동 완료의 근거는 아니다.
assistant L27067의 Instagram/X 메인·Facebook 보조, 주2~3회 게시, 계정명 통일 등은 추천이지 이 복구 작업으로 확정할 정책이 아니다. 실제 URL/계정명/담당·게시 주기·예산·자동 발행·로그인 연동은 미확인. 이번에는 문서만 복구하며 계정 생성·외부 게시·홈페이지 링크 변경을 실행하지 않는다.
근거: [점검표](../../../LASTRAIL_CHAT_CATEGORY_GAP_CHECK_20260929.md), [대조표](33_CURRENT_WORK_STATUS.md#chat-gap-reconciliation-20260929).

<a id="korea-social-launch-plan-v1-20260930"></a>
## 한국 출시 SNS 초기 운영 상세 v1 — 2026-09-30 / 신규 운영안 승인 대기

### 목적·확정 범위
기존 sns-channel-recovery-20260929에서 Facebook·Instagram·X 선택은 승인됐지만 게시 내용/주기/담당/계정은 미확인이었다. 한국 우선 출시를 기준으로, 게임의 실제 선택과 성장 차이를 보여 주고 기존 공식 홈페이지의 공개 정보로 연결하는 운영안을 제안한다. 이번 문서는 계정 생성·외부 게시·자동화·광고 집행 권한이 아니다. 승인 미술을 사용하되 게임정책/공개 승인과 미술 승인 자체를 혼동하지 않는다.

### 초기 추천과 대안
추천은 **주2개 원본 주제, 최대3개 채널별 게시물로 재편집**하는4주 시험 운영이다. 총8개 원본 주제로 같은 게임 정보를 일관되게 전달한다. 매일 별개 콘텐츠를 만드는 안은 제작/검수 부담이 크고, 주1회안은 부담은 작지만 어떤 내용이 이해되는지 비교할 기회가 적다. 주2회가 초기 작업량과 학습을 나누기 적합하다는 기획 판단이며 외부 플랫폼의 도달률을 보장하는 근거는 아니다.

| 채널 | 역할 후보 | 같은 원본의 편집 방식 |
| --- | --- | --- |
| Instagram | 승인 미술과 플레이 차이를 시각적으로 소개 | 실제 화면/짧은 플레이 영상·비교 이미지, 텍스트는 핵심 선택과 결과 중심 |
| X | 짧은 개발 소식·공개 업데이트·질문 응답 | 한 주제의 요점과 공개 상세 링크, 긴 조건은 공식 원문으로 연결 |
| Facebook | 같은 소식의 정리·공식 안내 보조 | 핵심 이미지와 문맥 설명, 같은 공지 버전/링크 사용 |

Instagram/X 중심·Facebook 보조는 이번 새 추천이다. 채널별 기술 기능/파일 규격·예약 발행 지원을 이 문서로 확정하지 않는다. 실제 업로드 작업 시 당시 공식 도구의 지원 규격을 확인한다. 동일 원본을 재편집하되 플랫폼별 수치/보상/일정을 다르게 쓰지 않는다.

### 최초4주·8주제 편성 후보
기산은 공개 가능 자료와 담당이 준비된 실제 운영 시작일이며 미확정 출시일을 역산하지 않는다.

| 주 | 첫 번째 주제 | 두 번째 주제 | 필요한 공개 근거 |
| --- | --- | --- | --- |
|1|열차와 요원으로 어떤 전투를 하는지|공개된 요원1명의 역할과 실제 스킬 장면|실지원 빌드/승인 공개 캐릭터·화면, 시안이면 시안임을 표시 |
|2|전체 공개 작전 지도에서 경로를 고르는 이유|장비를 분산할 때와 합성할 때 달라지는 선택|현행 지도 정책·실아이콘, 장비 공식/실장면·동일 비교 조건 |
|3|지휘실 한 트리에서 분야를 섞어 투자하는 예|열차 수리와 장비 구매 중 선택하는 장면|현재 노드/비용 승인본, 상인 상품/가격·지원 상태 확인 |
|4|공개 허용된 보스 예고에 대응하는 장면|4주간 자주 나온 질문과 실제 개선사항|스포일러 공개 범위·실패/회복 포함한 실제 장면, 검증된 변경 이력 |

구현이 준비되지 않은 주제는 해당 주를 위해 가짜 플레이 영상을 만들지 않는다. 공개 승인된 설정/미술 소개로 대체하되 개발 시안·미구현 여부를 표시하거나 게시를 보류한다. 베일/오르트·희귀 요원·스토리 결말은 문서에 존재한다는 이유만으로 공개하지 않는다. 별도 신규 보스/캐릭터 공개 순서를 여기서 확정하지 않는다.

### 콘텐츠 단위·검수·담당
각 원본 주제는 콘텐츠 ID, 담당 소유자, 공개 범위, 근거 MD 절/콘텐츠 버전, 실제 빌드 또는 시안 구분, 사용 자산/권리, 원문/채널별 문안, 이동할 공개 URL, 검수 결과, 게시 후 실제 URL·시각을 연결한다. 이름이 아직 없는 담당자를 임의로 지정하지 않는다.

추천 흐름: 원안 작성→게임 정보/공개 범위 검수→미술/본문 확인→게시 권한자의 최종 확인→각 채널 게시 결과 기록. 실패한 채널은 미게시로 남기고 성공한 채널을 반복 발행하지 않는다. 작업 담당은 기존 역할에서 정하며 SNS를 이유로 홈페이지에 새 CMS/관리 화면을 신설하지 않는다.

장비 확률 모형 결과는 실제 드랍 보장처럼 쓰지 않는다. 비교 화력은 레벨/장비/증강/대상 조건을 공개하고 임의 최대치만 정상 평균으로 표현하지 않는다. 미검증 기능/출시일/보상·유료상품을 확정 표현으로 게시하지 않는다. 사용자 닉네임/채팅/계정 정보가 나온 플레이 장면은 공개 가능 여부를 확인하고 필요 부분을 가린다.

### 문의·수정·홈페이지 연결
댓글에는 공개 가능한 기능 설명만 답한다. 계정/결제/신고 증거는 기존 비공개 문의 경로로 안내하며 공개 댓글에서 개인정보를 요청하지 않는다. 근거 없는 장애 복구 시각이나 보상 지급 약속을 하지 않는다.

잘못된 수치/일정은 원문을 정정하고 영향 있는 채널별 게시 상태를 기록한다. 여러 채널에 같은 오류가 있으면 각각 확인하며 한 게시물 수정으로 전체 정정 완료라고 처리하지 않는다. 기존 공지/점검/확률 공시의 권위는 승인된 홈페이지/서버 공개 원문에 두고 SNS 게시물이 실행 설정을 대신하지 않는다.

실제 공식 계정 소유·URL 확인 전 홈페이지 아이콘에 추측 주소/빈 링크를 넣지 않는다. 기존 홈페이지 구성은 유지한다. 공개 가능 문서/문의 경로가 없으면 없는 페이지로 유도하지 않고 관련 게시를 보류한다. 계정 확보·로그인 연동·계정 구매·유료 광고는 이번 운영안의 자동 실행 항목이 아니다.

### 시험 운영 평가와 이후 결정
4주 뒤 채널별 게시 성공 여부, 조회/노출·반응·공식 상세로의 이동(해당 채널에서 실제 제공되는 측정값만), 반복 질문, 원본1개당 제작/검수 시간을 비교한다. 플랫폼별 지표 정의가 다른 값을 합쳐 하나의 성과처럼 쓰지 않는다. 팔로워 수/매출 목표를 근거 없이 확정하지 않는다. 추적 도구/쿠키를 새로 설치하는 권한도 추가하지 않는다.

결과로 주1/2/3회 중 다음 주기를 정하고 초기부터 게시량을 늘리거나 유료 광고를 자동 시작하지 않는다. 실제 게임 이해를 돕는 내용과 단순 그림 반응을 구분해 본다. 부족한 정보는 FAQ/원기획 보완으로 연결하며 댓글 요청만으로 게임 보상/밸런스를 즉시 바꾸지 않는다.

신규 승인 대상은 주2원본×4주 시험, 채널 역할,8주제 후보,검수/문의/정정 및 평가 흐름이다. 실제 일정·계정 소유자/URL·게시 담당·공개 캐릭터/스토리 범위는 실행 전 확인한다. 현재 기획안 작성 완료/운영안 승인 대기이며 외부 게시·계정 생성·미디어 제작·광고비 지출은0이다.

<a id="korea-social-launch-approved-v1-20260930"></a>
## 한국 SNS 초기 운영 v1 승인 — 2026-09-30
사용자 “승인 다음 기획 ㄱㄱ”로 직전 초기 운영안을 승인했다. 주2원본×4주 시험, Instagram/X 중심·Facebook 보조,8주제 후보와 근거 검수/문의/정정/평가 흐름을 채택한다. 이전 v1의 운영안 승인대기는 해소한다. 실제 계정/URL/담당·날짜·캐릭터/스토리별 공개 범위는 별도 확인하며 이번 승인으로 외부 게시·계정 생성·유료 광고·미디어 제작을 실행하지 않는다.

<a id="public-story-disclosure-proposal-v2-20260930"></a>
## 한국 초기 홍보의 캐릭터·스토리 공개 범위 v2 — 2026-09-30 / 신규 제안

목적은 승인된4주 운영안을 공개 가능한 실제 장면으로 구성하는 것이다. [32 장별 공개 승인](32_NARRATIVE_REFERENCE.md#story17-dialogue-approved-20260930)은 자기계정 완료에 따른 기록실/월드 회고·중립 판본 정책이다. SNS는 진행도를 알 수 없는 공개 공간이므로 그 판본을 그대로 무조건 게시하지 않는다. 미술 승인·구현 완료·게임 내 해금과 외부 공개 승인은 각각 별개다.

### 초기 공개 추천표
| 대상 | 추천 공개 | 초기 홍보에서 제외 |
| --- | --- | --- |
| 기본5인 Kyle/Reina/Eve/Sera/Mira | 승인 외형·이름·현재 역할, 실제 지원되는 기본/스킬 장면 | 미확정 과거·배신/혈연 등 관계 추정, 미검증 수치·획득 보장 |
| Eve/Sera/Mira 역할 | Eve 현장 엔지니어/센트리, Sera 의료, Mira 화력지원의 현행 설정 | 과거 Eve 의료/Sera 정찰/Mira 정비 문구 재사용 |
| 지도·장비·지휘실·상인 | 승인된 선택 구조와 실제 지원 상태 | 예시 맵을 고정 드랍 보장처럼 표현, 고점 모형 확률을 실계정 확률로 표현 |
| 약탈왕·베일·오르트 | 원승인 외형/이름 및 실제 공격 예고·장치 대응의 중립 장면 | 장완료 회고·제압 후 결말·조직의 최종 운명, 스토리와 월드 규칙 혼합 |
| 별 | 초기8주제에서는 별도 주인공 소개를 예약하지 않음. 추후 소개한다면 공개 원문 범위의 공연자·사람을 지키는 모습까지만 | 무연의 제자 정체·사부/사형제·새 전투 캐릭터 출시/획득 약속 |
| 대단절·열차 코어·후속Act | 확정되고 공개가 승인된 표면적 배경만 사용 | 코어 기원/인공신의 진실·세 초월자의 관계 해설·미확정 이름·후속 결말 |

위 표의 이름/외형/기본5인 소개 허용은 이번 공개 정책 제안이다. 과거 외부에 이미 게시됐다고 단정하지 않는다. 보스 소개가 월드보스 입장 자격·개최 일정·새 별 보스 추가를 의미하지 않으며 게임의 원해금 정책은 그대로다.

### 실제 게시물에 적용할 계약
원본 콘텐츠별로 `공개 소개 가능`, `이야기 스포일러 별도 검토`, `미확정/공개 보류`를 구분하는 안이다. 이는 내부 편집 상태이며 이용자 화면에 등급표·새 팝업을 만들지 않는다. 초기8주제는 첫 상태만 사용한다. 숨긴 정보를 제목/썸네일/이미지 설명/자막/음성/게시물 링크 미리보기에서 다시 노출하지 않는다.

보스 영상은 계정 진행이 높은 개발자의 촬영본이라도 회고 대사를 그대로 쓰지 않는다. 중립 판본의 대사/자막/음성/이력을 확인하되 공격 예고·기믹 순서·시간·보상을 조작해서 다른 전투로 보이게 만들지 않는다. 안전한 원본이 없으면 자막만 가리고 완성으로 처리하지 않고 다른 적격 장면으로 대체하거나 보류한다.

최종 대사/결말/기록실 본문을 홍보에 사용하려면 대상 장·구체 대사/컷·공개 목적을 지정해 별도 검토한다. 출시 후 일정 기간이 지났다는 이유만으로 자동 공개하지 않는다. 커뮤니티 이용자의 자발적 스포일러 규칙까지 이 내부 홍보안으로 바꾸지 않는다.

### 기존8주제 연결
1주 요원 소개는 기본5인 중 자료가 준비된1명을 선택한다. 특정 캐릭터를 무료 지급한다고 추가하지 않는다.4주 보스는 중립 장면이 준비된1명으로 시작하며 세 수장을 한 번에 공개하는 의무가 없다. 미지원 보스 장면 대신 이미 공개 가능한 전투 대응/FAQ를 사용해 주2회 일정을 위해 결말을 노출하지 않는다. 주제 대체는 원본 ID와 사유를 기록하고 새 기능 출시 확정처럼 포장하지 않는다.

개별 게시 준비물은 원문 절·대본/콘텐츠 버전·촬영 build·실제 사용 이미지/영상·공개 판정·게시 문안·링크다. Plan은 설정/공개 범위, 원제작 담당은 자산/실장면, 게시 권한자는 실제 송출을 확인한다. 이름이 없는 담당을 임의로 지정하지 않는다. 기존 홈페이지·도감·월드 입장 구현에는 새 게이트를 만들지 않는다.

수용 사례: 역할3명 구설정 혼입0, 높은 진행 촬영본 회고노출0,자막/음성/썸네일 숨김일치,별 제자정체 누출0,개발시안/실플레이 구분,허용장면 없는 주제 보류/대체,공개 정책이 게임 계정 해금에 영향0. 현재 문서 대조만 완료·실제 콘텐츠 검수는 NOT_RUN이다. 신규 승인 대상은 위 초기 공개표와 중립 장면 우선/결말 별도 검토 정책이며 외부 게시 권한은 포함하지 않는다.

<a id="public-story-disclosure-approved-v2-20260930"></a>
## 초기 홍보 공개 범위 v2 승인 — 2026-09-30
사용자 “승인 다음 기획 ㄱㄱ”로 직전 공개표와 중립 장면 우선·결말 별도 검토안을 승인했다. 기본5인 이름/외형/현재역할·실스킬 장면, 보스 이름/외형/중립 대응 장면을 초기 공개 가능 범위로 삼는다. 별 제자정체·코어 진실·장결말 등 제외와 제목/썸네일/음성/링크 미리보기 대조를 유지한다. 계정 해금·도감·월드 입장 정책은 변경하지 않는다. 실제 게시물/자산의 제작·검수·게시 완료는 아니다.

<a id="social-week-one-editorial-v3-20260930"></a>
## 첫 주2주제 원고·제작 인수 v3 — 2026-09-30 / 편집 초안

승인된 초기 운영/공개 범위에 따라 바로 검토 가능한 첫 주 원고를 작성했다. 아래 문구는 새 홍보 편집 초안이며 게임 대사 원문/출시 공시/실제 게시물이 아니다. 날짜·다운로드 링크·출시 약속·무료 지급·팔로우 보상은 추가하지 않았다. 홈페이지 공식 URL은 확인되지 않아 문안에 넣지 않는다.

### LR-SOC-W01-A / 게임 소개 / 선택으로 만드는 작전
목표: 처음 보는 사람이 경로·장비·편성의 선택을 이해하게 한다. 원근거는14 전체 지도 공개,13 장비 장착/합성,09 단일 지휘 트리 및04 작전 흐름이다. 전체 기능의 실행 완료를 뜻하지 않도록 현재 개발 소개 문구를 사용한다.

Instagram 원고:
> 어느 길로 갈지, 어떤 장비를 남길지.
>
> 개발 중인 LAST RAIL은 열차와 요원을 운용하며 작전의 경로와 성장을 선택하는 게임입니다.
> 지도에서 사건을 살펴 길을 고르고, 얻은 장비를 나눠 장착하거나 합성하며 다음 전투를 준비합니다.
>
> 여러분은 장비를 넓게 나눠 줄까요, 한 요원에게 먼저 집중할까요?

X 원고:
> 어느 길로 갈지, 어떤 장비를 남길지.
> 개발 중인 LAST RAIL에서는 지도에서 경로를 고르고, 요원과 포탑의 장비를 배분하며 작전을 준비합니다.
> 골고루 강화하기와 한곳에 집중하기, 여러분의 선택은?

Facebook 원고:
> LAST RAIL 개발 소식을 시작합니다.
>
> 작전에서는 지도의 사건을 살펴 경로를 선택하고, 획득한 장비를 요원과 포탑에 배분하거나 합성해 다음 전투를 준비합니다.
> 같은 장비라도 어디에 투자하느냐에 따라 운용이 달라지는 선택을 만들고 있습니다.
>
> 앞으로 열차, 요원, 그리고 작전에서 만나게 될 선택을 차례로 소개하겠습니다.

제작 명세: 표지 ‘어느 길로 갈지, 어떤 장비를 남길지’→승인된 작전 지도 전체공개 상태→보유 장비 배분/합성 상태의3장 구성을 우선한다. 신규 아트 생성 없이 현재 승인 자료의 사용 가능 버전을 확인한다. 시안은 ‘개발 시안’, 실제 구현 촬영은 해당 검수 결과에 맞춰 ‘개발 중 화면’으로 구별한다. 플레이가 아닌 시안에 실제플레이라는 문구를 붙이지 않는다. 새 캐릭터·보스 정체가 지도 카드/툴팁에 나타나는지도 확인한다. 상세 수치가 낡았으면 수치를 임의로 보정한 화면을 만들어 통과시키지 말고 적격 원자료로 교체한다.

### LR-SOC-W01-B / Kyle 소개 / 전열에서 버티는 요원
목표: Kyle을 방어·블록·도발 역할로 소개한다. 근거는21의 현행 Tank/Block/Taunt 경계와17의 공통 역할이다.32 §5-A는 ‘현재 역할 대조 전 사용 보류’인 과거 대사 후보이므로 첫 인사 문장을 인용하지 않는다. 아래 원고는 홍보 설명이지 캐릭터 신규 정사 대사가 아니다.

Instagram 원고:
> 앞에서 버텨 줄 한 사람.
>
> LAST RAIL의 요원 Kyle을 소개합니다.
> Kyle은 전열에서 방어와 도발을 맡는 요원입니다. 적의 주의를 끌고 앞을 지키는 역할을 중심으로 설계하고 있습니다.
>
> 첫 요원 소개는 Kyle부터. 앞으로 동료들의 역할도 하나씩 소개하겠습니다.

X 원고:
> 요원 소개 — Kyle
> 전열에서 방어와 도발을 맡는 요원입니다.
> 앞에서 버티는 Kyle을 시작으로 LAST RAIL 동료들의 역할을 하나씩 소개합니다.
> 개발 중인 캐릭터 소개입니다.

Facebook 원고:
> 첫 번째 요원 소개는 Kyle입니다.
>
> Kyle의 핵심 역할은 전열 방어와 도발입니다. 적의 주의를 끌고 앞에서 버티는 역할을 중심으로 설계하고 있습니다.
> 이번에는 캐릭터의 모습과 역할을 먼저 소개합니다. 구체적인 전투 장면은 실제 동작 검수를 마친 자료로 이어서 보여드리겠습니다.

제작 명세: 승인된 Kyle 초상/전신 원본과 역할 문구를 분리한 소개1장을 우선한다. 기존 화면을 무조건 새캐릭터 홍보 원화로 재제작하지 않는다. 전투 컷은 실제 적격 검수 자료가 있을 때 추가하고 필수로 요구하지 않는다.21의 미확정 무기·방패·주손·궁극기 동작 후보를 이번 홍보안으로 확정하지 않는다. 임의 방패/신무기 추가, HP계수/확률/최강·필수 캐릭터 표현, 무료 지급 약속은 없다.

### 제작/게시 인수 체크
- 두 콘텐츠의 계획 순서는A→B다. 날짜·시각은 계정/담당/적격 자료가 준비된 뒤 확정한다. 준비되지 않은 게시물을 주2회 목표 때문에 강제 송출하지 않는다.
- 원고는3채널6개, 필요 자료는지도/장비 상태와 Kyle 승인 외형이다. 실제 asset ID/버전·공개 링크·송출 계정은 아직 확인하지 않았다. 이 문서의 항목명을 실제 자산 파일명으로 오인하지 않는다.
- 제작 담당에게 전달할 때 원문/표지 문구/자산 근거·상태/금지 정보/개발 표기를 묶는다. 이미지에 넣는 문구는 편집 가능한 독립 요소로 유지하며 생성 이미지에 글자·실수치를 굽지 않는다.
- 문안 검수는 공개 범위·역할·과장/미구현 표현·확률/획득 약속을 대조한다. 플랫폼의 실제 지원 형식/글자수·링크 미리보기는 업로드 준비 시 당시 공식 도구로 확인한다. 여기서 지원 규격이나 업로드 성공을 주장하지 않는다.
- 현재 상태: 편집 초안 작성/원문 대조 완료. 미디어 제작·시각 검수·외부 게시 NOT_RUN. 별도 외부 메시지·게시·계정 생성·광고 지출은 수행하지 않는다.

다음 원고 단위는2주차 경로 선택/장비 분산·합성이다. 단순 숫자 비교보다 같은 조건에서 플레이어가 무엇을 선택하는지 보여 주고, 실제 촬영 자료가 없으면 게임 설계 설명 범위를 지킨다.

<a id="sns-deferred-until-internal-test-20261001"></a>
## SNS 기획 보류 — 게임 내부 테스트 이후 재검토 / 2026-10-01 사용자 결정

사용자 지시: “sns 쪽 기획은 게임 내부테스트 이후에봐야할거같은데”. SNS 관련 후속 기획은 게임 내부 테스트 이후로 보류한다. 앞의 ‘다음 원고 단위는 2주차’는 당시 순서 기록이며 현재 착수 지시가 아니다.

- 기존 채널·운영·공개 범위 승인 이력과 첫 주 편집 초안은 보존한다. 보류는 기존 문서 폐기나 미승인 원고의 승인으로 해석하지 않는다.
- 추가 원고, 게시 일정, 계정 운영 준비, 홍보 리소스 제작·게시 작업을 현재 실행 큐에 발행하지 않는다.
- 내부 테스트 후 실제 구현 상태·테스트 결과·공개 가능한 검수 자료를 기준으로 기존 운영안과 원고의 유효성을 재검토한다. 테스트 완료 자체가 제작·게시의 자동 실행 조건은 아니다.
- 현재 우선순위는 게임 내부 테스트 준비와 기존 게임 구현·통합 점검이다. 이번 변경은 기획 순서 조정이며 실제 테스트 완료나 구현 착수를 뜻하지 않는다.
