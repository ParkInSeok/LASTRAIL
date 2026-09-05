import { describe, expect, it } from "vitest";
import {
  versionManagementExportAction,
  versionManagementInputFields,
  versionManagementServerCards
} from "./VersionManagementPage";

describe("VersionManagementPage configuration", () => {
  it("shows only the current server and current version summary cards", () => {
    expect(versionManagementServerCards.map((card) => [card.code, card.label, card.value])).toEqual([
      ["currentServer", "현재 서버", "DEV Admin API"],
      ["currentVersion", "현재 버전", "조회 API 대기"],
      ["currentVersionCode", "현재 버전코드", "조회 API 대기"]
    ]);
  });

  it("keeps version editing to target version and version code inputs with one R2 export action", () => {
    expect(versionManagementInputFields).toEqual([
      {
        key: "targetVersion",
        label: "배포할 버전",
        value: "",
        placeholder: "예: act1.2026-09-05.001"
      },
      {
        key: "targetVersionCode",
        label: "배포할 버전코드",
        value: "",
        placeholder: "예: 100"
      }
    ]);
    expect(versionManagementExportAction.label).toBe("내보내기");
    expect(versionManagementExportAction.status).toBe("입력 후 내보내기 가능");
  });
});
