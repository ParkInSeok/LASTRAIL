import { createServer } from "node:http";

// Local test harness only. No DB export, public bind, request auth forwarding,
// mutation endpoint, CORS relaxation or changes to the production Hono app.
export async function startGachaAuditReadBridge(readAdmin: (path: string) => Promise<Response>, lifetimeMs = 30 * 60 * 1000) {
  let finish!: () => void;
  const closed = new Promise<void>((resolve) => { finish = resolve; });
  const server = createServer(async (request, response) => {
    const address = server.address();
    const host = typeof address === "object" && address ? `127.0.0.1:${address.port}` : "";
    const reject = (status: number) => { response.writeHead(status, { "Cache-Control": "no-store" }); response.end(); };
    if (request.method !== "GET") { reject(405); return; }
    if (request.headers.host !== host || (request.headers.origin && request.headers.origin !== `http://${host}`)) { reject(403); return; }
    if (!request.url?.startsWith("/") || request.url.startsWith("//")) { reject(404); return; }
    const url = new URL(request.url, `http://${host}`);
    if (!/^\/v1\/admin\/gacha\/(?:banners|pull-batches(?:\/[A-Za-z0-9_-]+)?)$/u.test(url.pathname)) { reject(404); return; }
    try {
      const result = await readAdmin(url.pathname + url.search);
      response.writeHead(result.status, { "Content-Type": result.headers.get("Content-Type") ?? "application/json", "Cache-Control": "no-store", "X-Content-Type-Options": "nosniff" });
      response.end(await result.text());
    } catch { reject(500); }
  });
  await new Promise<void>((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => { server.off("error", reject); resolve(); });
  });
  const address = server.address();
  if (address === null || typeof address === "string") throw new Error("Loopback bind failed");
  const close = () => { server.close(() => finish()); server.closeAllConnections(); };
  const timer = setTimeout(close, lifetimeMs);
  server.once("close", () => { clearTimeout(timer); finish(); });
  return { url: `http://127.0.0.1:${address.port}`, closed, close };
}
