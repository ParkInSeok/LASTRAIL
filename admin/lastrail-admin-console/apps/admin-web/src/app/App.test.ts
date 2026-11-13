import { describe, expect, it } from "vitest";
import { AdminApiError } from "../shared/api/client";
import { shellStateFromAdminContext } from "./App";

describe("App shell bootstrap state", () => {
  it("keeps the console shell visible when bootstrap fails for Access/admin credentials", () => {
    const state = shellStateFromAdminContext({
      bootstrap: undefined,
      isLoading: false,
      error: new AdminApiError({
        status: 401,
        path: "/api/bootstrap",
        errorCode: "ADMIN_DELEGATED_ACCESS_JWT_REQUIRED"
      })
    });

    expect(state.shouldRenderShell).toBe(true);
    expect(state.bootstrapStatus).toBe("Access/admin credential 확인 필요");
    expect(state.identityDisplayName).toBe("Admin 확인 필요");
    expect(state.environmentLabel).toBe("dev");
    expect(state.permissions.length).toBeGreaterThan(0);
  });
});
