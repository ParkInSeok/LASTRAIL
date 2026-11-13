// Local-only interactive fixture. It never forwards fetch or writes remote storage.
import { useState } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider, useQueryClient } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { VersionManagementPage } from "../src/pages/VersionManagementPage";
import "../src/styles.css";
import "../src/pages/versionManagement.css";

let revision = 1;
let writes = 0;
let readonly = false;
let conflictNext = false;
let failure = false;
let configured = false;
const initialContent = { status: "APPROVED", releaseId: "local-release-one", contentVersion: "local-content-one", catalogUrl: "https://example.invalid/catalog.bin", catalogHash: "sha256:" + "a".repeat(64), minimumVersionCode: "1", maximumVersionCode: "9" };
const rows: { version: string; versionCode: string; status: string; contentRelease: typeof initialContent; artifactIdentity: string; validation: { status: string; artifactBytesVerified: boolean } }[] = [];
let minimumVersionCode = "1";
const etag = () => `"local-${revision}"`;
const snapshot = () => ({ target: "DEV", registryStatus: configured ? "CONFIGURED" : "LEGACY", revision: etag(), minimumVersionCode, auditHead: configured ? "local-audit" : null, rows, canMutate: !readonly, capabilities: { register: !readonly, changeStatus: !readonly, promote: !readonly, changeMinimum: !readonly, rollback: false, manageTesters: false } });
const error = (code: string, status: number) => Response.json({ error: { code } }, { status });
globalThis.fetch = async (input, init) => {
  const url = new URL(String(input), location.origin);
  if (url.origin !== location.origin) throw new Error("Local QA never forwards network requests");
  if (url.pathname === "/api/versions") {
    const stable = rows.filter(row => row.status === "STABLE").sort((a, b) => Number(b.versionCode) - Number(a.versionCode))[0];
    return Response.json({ server: "DEV", version: stable?.version ?? "0.1.0", versionCode: stable?.versionCode ?? "1", minimumVersionCode, revision: etag(), canExport: true });
  }
  if (!url.pathname.startsWith("/api/client-releases")) return error("local_fixture_unavailable", 404);
  if (init?.method !== "POST") {
    if (failure) return error("local_read_failed", 503);
    if (url.searchParams.get("target") !== "DEV") return error("registry_target_mismatch", 400);
    return Response.json(snapshot());
  }
  writes++;
  const body = JSON.parse(String(init.body));
  if (readonly) return error("forbidden", 403);
  if (body.target !== "DEV") return error("registry_target_mismatch", 400);
  if (conflictNext) { conflictNext = false; revision++; }
  if (body.expectedRevision !== etag()) return error("version_current_pointer_conflict", 409);
  const parts = url.pathname.split("/");
  if (parts[3] === "register") {
    if (!configured && body.legacyTransitionConfirmation !== "ENABLE_EXACT_LOOKUP") return error("registry_transition_confirmation_required", 400);
    configured = true;
    rows.push({ version: body.version, versionCode: body.versionCode, status: "INTERNAL", contentRelease: body.contentRelease, artifactIdentity: "sha256:" + "c".repeat(64), validation: { status: "STRUCTURAL_ONLY", artifactBytesVerified: false } });
  }
  else if (parts[3] === "minimum") minimumVersionCode = body.minimumVersionCode;
  else {
    const row = rows.find(row => row.versionCode === parts[3]);
    if (!row) return error("client_version_unregistered", 404);
    row.status = parts[4] === "promote" ? "STABLE" : body.status;
  }
  revision++;
  return Response.json(snapshot());
};
function Fixture() {
  const client = useQueryClient();
  const [, refresh] = useState(0);
  const invalidate = () => void client.invalidateQueries({ queryKey: ["client-release-registry"] });
  return <><header style={{ padding: 16, background: "#fff3c4" }}>LOCAL SYNTHETIC QA · 원격 저장 없음
    <button onClick={() => { revision++; invalidate(); }}>외부 revision 변경</button>
    <button onClick={() => { readonly = !readonly; invalidate(); }}>읽기 권한 전환</button>
    <button onClick={() => { conflictNext = true; }}>다음 저장 409</button>
    <button onClick={() => { failure = !failure; invalidate(); }}>조회 실패 전환</button>
    <button onClick={() => refresh(value => value + 1)}>로컬 쓰기 횟수 확인</button><output>POST attempts: {writes}</output>
  </header><VersionManagementPage /></>;
}
createRoot(document.getElementById("root")!).render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}><MemoryRouter><Fixture /></MemoryRouter></QueryClientProvider>);
