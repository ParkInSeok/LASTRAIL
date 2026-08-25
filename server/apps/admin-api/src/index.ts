import { Hono } from "hono";
import { createRequestId } from "@lastrail/common";

const app = new Hono<{ Bindings: Env }>();

type AdminPrincipalRow = {
  id: string;
  access_subject: string;
  role: "OWNER" | "OPERATOR" | "VIEWER";
  status: "ACTIVE" | "DISABLED";
  display_name: string | null;
};

app.get("/status", (c) => c.json({ ok: true, service: "admin-api", requestId: createRequestId() }));
app.get("/v1/admin/me", async (c) => {
  if (c.env.ADMIN_AUTH_MODE !== "LOCAL_HEADER") {
    return c.json({ error: "ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED" }, 501);
  }

  const accessSubject = c.req.header("Cf-Access-Subject");
  if (!accessSubject) {
    return c.json({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" }, 401);
  }

  const principal = await c.env.DB.prepare(
    `SELECT id, access_subject, role, status, display_name
     FROM admin_principals
     WHERE access_subject = ?
       AND status = 'ACTIVE'`,
  )
    .bind(accessSubject)
    .first<AdminPrincipalRow>();

  if (principal === null) {
    return c.json({ error: "ADMIN_PRINCIPAL_NOT_AUTHORIZED" }, 403);
  }

  return c.json({
    principal: {
      id: principal.id,
      accessSubject: principal.access_subject,
      role: principal.role,
      displayName: principal.display_name,
    },
  });
});

export default app;


