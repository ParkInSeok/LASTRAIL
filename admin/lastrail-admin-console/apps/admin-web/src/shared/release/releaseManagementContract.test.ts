import { describe, expect, it } from "vitest";
import {
  buildSimpleVersionManagementSnapshot,
  simpleVersionExportAction,
  simpleVersionInputFields,
  simpleVersionServerCards
} from "./releaseManagementContract";

describe("releaseManagementContract", () => {
  it("keeps version management focused on current server, current version, and one target version input", () => {
    const snapshot = buildSimpleVersionManagementSnapshot();

    expect(simpleVersionServerCards.map((card) => card.label)).toEqual(["현재 서버", "현재 버전"]);
    expect(simpleVersionInputFields.map((field) => field.label)).toEqual(["변경할 버전"]);
    expect(snapshot.serverCards).toEqual(simpleVersionServerCards);
    expect(snapshot.inputFields).toEqual(simpleVersionInputFields);
  });

  it("prepares one R2 export action without promotion, rollback, or repair controls", () => {
    expect(simpleVersionExportAction).toEqual({
      code: "export_version_to_r2",
      label: "내보내기",
      enabled: false,
      status: "R2 내보내기 API 대기",
      note: "입력한 버전을 서버 내보내기 API가 검증한 뒤 R2 current version JSON에 갱신합니다."
    });

    const serialized = JSON.stringify(buildSimpleVersionManagementSnapshot());
    expect(serialized).not.toContain("rollback");
    expect(serialized).not.toContain("승급");
    expect(serialized).not.toContain("QA 승인");
    expect(serialized).not.toContain("manual grant");
    expect(serialized).not.toContain("token");
    expect(serialized).not.toContain("secret");
  });
});
