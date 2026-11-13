import configured from "../src/shared/support/fixtures/configured-zero-and-owned-records-21dbd8f1-197f-4623-906e-b885dc205396.json";
import { emptySupportAccountSummary } from "../src/shared/support/supportContract";

// Local QA transport only. Never falls back to network or authenticates an account.
export function createSupportRouteFixture(onEvent: (event: string) => void = () => {}) {
  let release!: () => void;
  const delayed = new Promise<void>(resolve => { release = resolve; });
  return {
    releaseA: () => release(),
    fetch: async (input: RequestInfo | URL, init?: RequestInit) => {
      const path = String(input);
      const match = /^\/api\/(?:players\/(account_[a-z]+)\/activity-summary|support\/accounts\/(account_[a-z]+))$/.exec(path);
      if (!match || (init?.method && init.method !== "GET")) throw new Error("LOCAL_QA_UNEXPECTED_REQUEST");
      const accountId = match[1] ?? match[2];
      onEvent(`requested ${accountId}`);
      if (accountId === "account_a") await delayed;
      onEvent(`responded ${accountId}`);
      if (accountId === "account_denied") return Response.json({ errorCode: "permission_denied" }, { status: 403 });
      if (match[2]) {
        const summary = emptySupportAccountSummary(accountId);
        summary.account.displayName = `SYNTHETIC_${accountId}`;
        return Response.json(summary);
      }
      const data = structuredClone(configured);
      data.accountId = accountId;
      data.shop.data.policyVersion = accountId === "account_a" ? "A_SHOP" : "B_SHOP";
      if (accountId === "account_partial") return Response.json({ ...data, shop: { availability: "UNAVAILABLE", reason: "source_read_failed", data: null } });
      return Response.json(data);
    }
  };
}
