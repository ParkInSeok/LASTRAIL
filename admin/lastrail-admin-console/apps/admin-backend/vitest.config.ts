import { defineConfig } from "vitest/config";
import { fileURLToPath } from "node:url";

export default defineConfig({
  resolve: {
    alias: {
      "@lastrail/admin-contracts": fileURLToPath(
        new URL("../../packages/admin-contracts/src/index.ts", import.meta.url)
      )
    }
  },
  test: {
    environment: "node"
  }
});
