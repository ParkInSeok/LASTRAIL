import { describe, expect, it } from "vitest";
import { mapCurrentVersion } from "./releaseManagementContract";
import { canSaveVersionRevision } from "./versionPrecondition";

describe("version revision precondition", () => {
  const base = {server:"DEV",canExport:true,version:null,versionCode:null};
  it("preserves quoted revisions, absence and legacy separately", () => {
    expect(mapCurrentVersion({...base,revision:'"r1"'}).revision).toBe('"r1"');
    expect(mapCurrentVersion({...base,revision:null}).revision).toBeNull();
    expect(mapCurrentVersion(base).revision).toBeUndefined();
  });
  it("blocks stale edits, unavailable reads and post-write review", () => {
    expect(canSaveVersionRevision('"r1"','"r1"',false,false)).toBe(true);
    expect(canSaveVersionRevision(null,null,false,false)).toBe(true);
    expect(canSaveVersionRevision(undefined,undefined,false,false)).toBe(false);
    expect(canSaveVersionRevision('"r1"','"r2"',false,false)).toBe(false);
    expect(canSaveVersionRevision('"r1"','"r1"',true,false)).toBe(false);
    expect(canSaveVersionRevision('"r1"','"r1"',false,true)).toBe(false);
  });
});
