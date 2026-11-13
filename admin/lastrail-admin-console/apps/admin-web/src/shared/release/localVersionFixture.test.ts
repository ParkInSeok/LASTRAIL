import { expect, it } from "vitest";
import { localVersionEditBucket } from "../../../scripts/local-version-edit-fixtures";
it("local version fixture rejects stale CAS without touching remote storage", async () => {
  const bucket = localVersionEditBucket({versionCode:"1"});
  const first = await bucket.get("versions/current.json");
  const onlyIf = new Headers({"If-Match":first!.httpEtag!});
  expect(await bucket.put("versions/current.json",'{"versionCode":"2"}',{onlyIf})).not.toBeNull();
  expect(await bucket.put("versions/current.json",'{"versionCode":"3"}',{onlyIf})).toBeNull();
  expect(await (await bucket.get("versions/current.json"))!.json()).toEqual({versionCode:"2"});
});
