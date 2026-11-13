import { defineConfig } from "vitest/config";
import { resolve } from "node:path";

export default defineConfig({
  resolve: {
    alias: {
      "@lastrail/common": resolve(import.meta.dirname, "packages/common/src/index.ts")
    }
  },
  test: {
    include: ["tests/**/*.test.ts", "packages/**/*.test.ts", "apps/**/*.test.ts"],
    globals: false
  }
});
