// Diagnostic UI host only. Domain reads go to the Server-owned live Hono DB.
import { createServer } from "node:http";
import { readFileSync } from "node:fs";
import { resolve, extname, sep } from "node:path";
import { fileURLToPath } from "node:url";

const upstream = new URL(process.argv[2] ?? "http://127.0.0.1:53702");
if (upstream.hostname !== "127.0.0.1" || upstream.protocol !== "http:" || upstream.username || upstream.password) throw new Error("Loopback upstream required");
const base = "http://127.0.0.1:4178";
const dist = resolve(fileURLToPath(new URL("../dist/", import.meta.url)));
const server = createServer(async (request, response) => {
  const send = (status, value) => { response.writeHead(status, { "content-type": "application/json" }); response.end(JSON.stringify(value)); };
  try {
    if (request.headers.host !== "127.0.0.1:4178" || (request.headers.origin && request.headers.origin !== base)) return send(403, { error: "local_origin_required" });
    if (request.method !== "GET") return send(405, { error: "read_only_diagnostic" });
    const url = new URL(request.url, base);
    if (url.pathname === "/api/bootstrap") return send(200, {
      environment: "local", admin: { id: "local-diagnostic", displayName: "LOCAL diagnostic shell", role: "VIEWER", permissions: ["ECONOMY_READ", "SUPPORT_READ"] }
    });
    if (/^\/api\/gacha\/(banners|pull-batches(?:\/[^/]+)?)$/.test(url.pathname)) {
      const target = new URL(url.pathname.replace(/^\/api/, "/v1/admin") + url.search, upstream);
      // Never forward browser auth, cookies, Origin or Host. Fetch sets loopback Host.
      const result = await fetch(target, { redirect: "error" });
      response.writeHead(result.status, { "content-type": "application/json" });
      response.end(Buffer.from(await result.arrayBuffer()));
      console.log(`GET ${url.pathname}${url.search} ${result.status}`);
      return;
    }
    if (url.pathname.startsWith("/api/")) return send(404, { error: "diagnostic_route_not_enabled" });
    const path = resolve(dist, "." + decodeURIComponent(url.pathname));
    if (path !== resolve(dist) && !path.startsWith(dist + sep)) return send(403, { error: "invalid_path" });
    const file = url.pathname.startsWith("/assets/") ? path : resolve(dist, "index.html");
    const bytes = readFileSync(file);
    response.writeHead(200, { "content-type": ({ ".js": "text/javascript", ".css": "text/css", ".html": "text/html" })[extname(file)] ?? "application/octet-stream" });
    response.end(bytes);
  } catch { send(502, { error: "local_bridge_unavailable" }); }
});
server.listen(4178, "127.0.0.1", () => console.log(`${base}/gacha: live diagnostic Admin reads, synthetic UI bootstrap only`));
