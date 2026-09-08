import { expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { mapClientRegistry, buildRegistryCommand } from "./clientReleaseRegistry";
import { ClientReleaseRegistryPanel } from "./ClientReleaseRegistryPanel";

const policy = { supportedStatuses: ["STABLE", "DISABLED"], initialStatus: "STABLE", mutableStatuses: ["STABLE", "DISABLED"], testerGate: false };
const fixture = { target: "DEV", registryStatus: "LEGACY", revision: '"r1"', minimumVersionCode: "1", auditHead: null, rows: [], canMutate: true, capabilities: { register: true, changeStatus: true, promote: true, changeMinimum: true }, distributionPolicy: policy };
const context = { target: "DEV" as const, editingRevision: '"r1"', readUnavailable: false, needsReview: false };
const content = { status: "APPROVED", releaseId: "test", contentVersion: "test", catalogUrl: "https://example.invalid/catalog", catalogHash: "sha256:" + "a".repeat(64), minimumVersionCode: "2" };
const input = { operation: "register" as const, version: "0.2", versionCode: "2", contentRelease: content, confirmed: true, reason: "test" };

it("locks legacy DEV writes without a distribution policy while retaining reads", () => {
  const old = mapClientRegistry({ ...fixture, distributionPolicy: undefined });
  expect(old.rows).toEqual([]);
  expect(() => buildRegistryCommand(old, context, input)).toThrow(/배포 정책/);
});
it("preserves explicit DEV policy and first registration confirmation without setting minimum or status", () => {
  const state = mapClientRegistry(fixture);
  expect(state).toHaveProperty("distributionPolicy", policy);
  const command = buildRegistryCommand(state, context, input);
  expect(command.body.legacyTransitionConfirmation).toBe("ENABLE_EXACT_LOOKUP");
  expect(command.body).not.toHaveProperty("minimumVersionCode");
  expect(command.body).not.toHaveProperty("status");
  expect(() => buildRegistryCommand(state, context, { ...input, confirmed: false })).toThrow();
});
it("allows DEV reactivation but never promotion even with a malformed capability", () => {
  const state = mapClientRegistry({ ...fixture, rows: [{ version: "0.2", versionCode: "2", status: "DISABLED", contentRelease: content, artifactIdentity: "sha256:" + "b".repeat(64), validation: { status: "STRUCTURAL_ONLY" } }] });
  expect(buildRegistryCommand(state, context, { operation: "status", versionCode: "2", status: "STABLE", reason: "test" }).body.status).toBe("STABLE");
  expect(() => buildRegistryCommand(state, context, { operation: "promote", versionCode: "2", confirmed: true, reason: "test" })).toThrow();
  expect(() => buildRegistryCommand(state, context, { operation: "status", versionCode: "2", status: "INTERNAL", reason: "test" })).toThrow();
});
it("hides DEV internal/promotion/tester controls and disables missing-policy editing", () => {
  const props = { target: "DEV" as const, loading: false, fetching: false, saving: false, error: null, onTargetChange: vi.fn(), onReload: vi.fn(), onSave: vi.fn() };
  const html = renderToStaticMarkup(<ClientReleaseRegistryPanel {...props} data={mapClientRegistry(fixture)} />);
  expect(html).toContain("정식 버전 등록");
  expect(html).not.toContain("내부 테스트 버전 등록");
  expect(html).not.toContain('value="promote"');
  expect(html).not.toContain("테스터 관리 API");
  const old = renderToStaticMarkup(<ClientReleaseRegistryPanel {...props} data={mapClientRegistry({ ...fixture, distributionPolicy: undefined })} />);
  expect(old).toContain("배포 정책 확인 필요");
  expect(old).toContain('<fieldset disabled=""');
});

it("supports multiple DEV stable codes and preserves CAS, target and separate minimum guards", () => {
  const rows = ["2", "3"].map(versionCode => ({ version: "0." + versionCode, versionCode, status: "STABLE", contentRelease: content, artifactIdentity: "sha256:" + "b".repeat(64), validation: { status: "STRUCTURAL_ONLY" } }));
  const state = mapClientRegistry({ ...fixture, registryStatus: "CONFIGURED", rows });
  const minimum = { operation: "minimum" as const, minimumVersionCode: "3", reason: "test", confirmed: true };
  expect(state.rows).toHaveLength(2);
  expect(buildRegistryCommand(state, context, minimum).body.confirmMinimumChange).toBe(true);
  for (const ctx of [{ ...context, target: "PRD" as const }, { ...context, editingRevision: '"old"' }, { ...context, readUnavailable: true }]) expect(() => buildRegistryCommand(state, ctx, minimum)).toThrow();
  expect(() => buildRegistryCommand(state, context, { ...minimum, confirmed: false })).toThrow();
  expect(() => buildRegistryCommand(state, context, { ...minimum, minimumVersionCode: "4" })).toThrow();
  expect(() => buildRegistryCommand({ ...state, canMutate: false }, context, minimum)).toThrow();
  expect(() => buildRegistryCommand(mapClientRegistry({ ...fixture, distributionPolicy: { ...policy, testerGate: true } }), context, input)).toThrow(/배포 정책/);
});

it("keeps explicit PRD internal and unchanged-artifact promotion semantics", () => {
  const state = mapClientRegistry({ ...fixture, target: "PRD", distributionPolicy: { supportedStatuses: ["INTERNAL", "STABLE", "DISABLED"], initialStatus: "INTERNAL", mutableStatuses: ["INTERNAL", "DISABLED"], testerGate: true }, rows: [{ version: "0.2", versionCode: "2", status: "INTERNAL", contentRelease: content, artifactIdentity: "sha256:" + "b".repeat(64), validation: { status: "STRUCTURAL_ONLY" } }] });
  const ctx = { ...context, target: "PRD" as const };
  const command = buildRegistryCommand(state, ctx, { operation: "promote", versionCode: "2", confirmed: true, reason: "test" });
  expect(command.body.expectedArtifactIdentity).toBe(state.rows[0].artifactIdentity);
  expect(command.body).not.toHaveProperty("minimumVersionCode");
  expect(() => buildRegistryCommand(state, ctx, { operation: "status", versionCode: "2", status: "STABLE", reason: "test" })).toThrow();
  const html = renderToStaticMarkup(<ClientReleaseRegistryPanel target="PRD" data={state} loading={false} fetching={false} saving={false} error={null} onTargetChange={vi.fn()} onReload={vi.fn()} onSave={vi.fn()} />);
  expect(html).toContain('value="promote"');
  expect(html).toContain("내부 테스트 버전 등록");
});
