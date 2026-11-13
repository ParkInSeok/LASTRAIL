import { afterEach, describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { MemoryRouter } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { SupportTicketsPage, SupportTicketPage } from "./SupportTicketsPage";
import { adminApi } from "../shared/api/client";
vi.mock("@tanstack/react-query", () => ({ useQuery: vi.fn() }));
vi.mock('../shared/support/SupportProcessingPanel',()=>({SupportProcessingPanel:()=>null}));
const row = {ticketId:"synthetic-ticket",accountId:"synthetic-account",category:"BUG_OTHER",bodyPreview:"<b>sample</b>",body:"<script>sample</script>",appVersion:"1.0.0",appVersionCode:"9223372036854775807",serverReceivedAt:"2026-09-06T00:00:00Z"};
function render(page: React.ReactNode) { return renderToStaticMarkup(<MemoryRouter>{page}</MemoryRouter>); }
afterEach(() => { vi.unstubAllGlobals(); vi.clearAllMocks(); });
describe("customer inquiry read-only views", () => {
  it("escapes preview/body and retains the account support link", () => {
    vi.mocked(useQuery).mockReturnValue({data:{items:[row]},isLoading:false} as never);
    const list = render(<SupportTicketsPage/>);
    expect(list).toContain("&lt;b&gt;sample&lt;/b&gt;");
    expect(list).toContain("/support/accounts/synthetic-account");
    vi.mocked(useQuery).mockReturnValue({data:row,isLoading:false} as never);
    const detail = render(<SupportTicketPage/>);
    expect(detail).toContain("&lt;script&gt;sample&lt;/script&gt;");
    expect(detail).not.toContain("<script>");
    expect(detail).not.toContain("<button");
    expect(detail).toContain("9223372036854775807");
  });
  it("distinguishes loading, empty, and failed requests", () => {
    vi.mocked(useQuery).mockReturnValue({isLoading:true} as never);
    expect(render(<SupportTicketsPage/>)).toContain("조회 중");
    vi.mocked(useQuery).mockReturnValue({data:{items:[]},isLoading:false} as never);
    expect(render(<SupportTicketsPage/>)).toContain("문의 없음");
    vi.mocked(useQuery).mockReturnValue({error:new Error("shape"),isLoading:false} as never);
    expect(render(<SupportTicketsPage/>)).not.toContain("문의 없음");
  });
  it("calls only encoded read endpoints with filters", async () => {
    const fetch = vi.fn().mockResolvedValueOnce(Response.json({items:[row],nextCursor:null})).mockResolvedValueOnce(Response.json({ticket:row}));
    vi.stubGlobal("fetch",fetch);
    await adminApi.supportTickets({accountId:"account/a",category:"BUG_OTHER",limit:"25",cursor:"25"});
    await adminApi.supportTicket("ticket/a");
    expect(fetch.mock.calls[0][0]).toBe("/api/support/tickets?accountId=account%2Fa&category=BUG_OTHER&limit=25&cursor=25");
    expect(fetch.mock.calls[1][0]).toBe("/api/support/tickets/ticket%2Fa");
    expect(fetch.mock.calls.every(([,options]) => !options?.body && (!options?.method || options.method === "GET"))).toBe(true);
  });
});
