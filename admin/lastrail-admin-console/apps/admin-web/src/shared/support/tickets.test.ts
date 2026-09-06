import { describe, expect, it } from "vitest";
import { ticketList, ticketDetail } from "./tickets";
describe("support tickets", () => {
  it("preserves plain text and decimal version codes but drops unknown fields", () => {
    const ticket = ticketDetail({ ticket: { ticketId: "t1", body: "<script>test</script>", appVersionCode: "9223372036854775807", secret: "forbidden" } });
    expect(ticket.body).toBe("<script>test</script>");
    expect(ticket.appVersionCode).toBe("9223372036854775807");
    expect(ticket).not.toHaveProperty("secret");
  });
  it("distinguishes malformed responses from empty success", () => {
    expect(ticketList({items: [], nextCursor: null}).items).toEqual([]);
    expect(() => ticketList({})).toThrow();
    expect(() => ticketDetail({ticket:null})).toThrow();
  });
});
