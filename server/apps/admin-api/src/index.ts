import { Hono } from "hono";
import { createRequestId } from "@lastrail/common";

const app = new Hono<{ Bindings: Env }>();

app.get("/status", (c) => c.json({ ok: true, service: "admin-api", requestId: createRequestId() }));

export default app;


