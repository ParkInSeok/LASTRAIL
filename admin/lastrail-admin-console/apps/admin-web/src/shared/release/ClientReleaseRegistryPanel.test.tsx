import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { ClientReleaseRegistryPanel } from "./ClientReleaseRegistryPanel";
import { mapClientRegistry } from "./clientReleaseRegistry";

const state = mapClientRegistry({ target: "DEV", registryStatus: "CONFIGURED", revision: '"r1"', minimumVersionCode: "2", auditHead: "history-reference", rows: [{ version: "0.2.0", versionCode: "2", status: "INTERNAL", contentRelease: { status: "APPROVED", releaseId: "immutable-release", contentVersion: "content-one", catalogUrl: "https://example.invalid/catalog.bin", catalogHash: "sha256:" + "a".repeat(64), minimumVersionCode: "2" }, artifactIdentity: "sha256:" + "b".repeat(64), validation: { status: "STRUCTURAL_ONLY", artifactBytesVerified: false } }], canMutate: false, capabilities: {} });
const base = { target: "DEV" as const, data: state, loading: false, fetching: false, saving: false, error: null, onTargetChange: vi.fn(), onReload: vi.fn(), onSave: vi.fn() };
describe("registry public read state surface", () => {
  it("shows immutable content and structural-only evidence, no tester or rollback action", () => {
    const html = renderToStaticMarkup(<ClientReleaseRegistryPanel {...base} />);
    expect(html).toContain("immutable-release");
    expect(html).toContain("내부 테스트");
    expect(html).toContain("바이트 미검증");
    expect(html).not.toContain("테스터 관리 API 미지원");
    expect(html).toContain("배포 정책 확인 필요");
    expect(html).toContain("읽기 전용");
    expect(html).not.toMatch(/<button[^>]*>[^<]*(테스터|롤백)/);
    expect(html).toContain('type="submit" disabled=""');
  });
  it("distinguishes loading, empty, unavailable and mismatching targets", () => {
    expect(renderToStaticMarkup(<ClientReleaseRegistryPanel {...base} data={undefined} loading />)).toContain("조회 중");
    expect(renderToStaticMarkup(<ClientReleaseRegistryPanel {...base} data={{ ...state, rows: [] }} />)).toContain("등록된 버전 없음");
    expect(renderToStaticMarkup(<ClientReleaseRegistryPanel {...base} data={undefined} error={new Error("local failure")} />)).toContain("버전 등록 조회 실패");
    expect(renderToStaticMarkup(<ClientReleaseRegistryPanel {...base} target="PRD" />)).toContain("선택 대상과 응답 서버 불일치");
  });
});
