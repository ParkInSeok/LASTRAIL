import { deploymentBucket } from "./local-deployment-fixtures";

// Explicit localhost mode only; contains no credentials or remote client.
export function localVersionEditBucket(initial: unknown) {
  const evidence = deploymentBucket(initial);
  let policy = structuredClone(initial);
  let revision = 1;
  return {
    async get(key: string) {
      if (key !== "versions/current.json") {
        const object = await evidence.get(key);
        return object ? { ...object, httpEtag: `"${object.etag}"` } : null;
      }
      const snapshot = structuredClone(policy);
      return { httpEtag: `"local-revision-${revision}"`, json: async () => snapshot };
    },
    async put(key: string, value: string, options: {onlyIf: Headers}) {
      if (key !== "versions/current.json") throw new Error("Local version key only");
      if (options.onlyIf.get("If-Match") !== `"local-revision-${revision}"`) return null;
      policy = JSON.parse(value);
      revision++;
      return {httpEtag: `"local-revision-${revision}"`};
    }
  };
}
