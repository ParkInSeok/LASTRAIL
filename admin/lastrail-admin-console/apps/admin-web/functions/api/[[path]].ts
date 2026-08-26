import { AdminRoleSchema, BootstrapResponseSchema, rolePermissions } from "@lastrail/admin-contracts";

type Env = {
  ADMIN_API_BASE_URL: string;
  ADMIN_PROFILE?: "local" | "dev" | "prd";
  CF_ACCESS_CLIENT_ID?: string;
  CF_ACCESS_CLIENT_SECRET?: string;
  ADMIN_API_TOKEN?: string;
};

type PagesContext = {
  request: Request;
  env: Env;
  params: {
    path?: string | string[];
  };
};

type AdminMeResponse = {
  principal: {
    id: string;
    role: unknown;
    displayName: string | null;
  };
};

function requestIdFrom(request: Request): string {
  return request.headers.get("x-request-id") ?? crypto.randomUUID();
}

function apiPathFrom(path: string | string[] | undefined): string {
  const parts = Array.isArray(path) ? path : path ? [path] : [];
  return `/${parts.map((part) => encodeURIComponent(part)).join("/")}`;
}

function upstreamPathFor(apiPath: string): string {
  if (apiPath === "/bootstrap") return "/v1/admin/me";
  return `/v1/admin${apiPath}`;
}

function headersFor(request: Request, env: Env, requestId: string): Headers {
  const headers = new Headers({
    accept: "application/json",
    "x-request-id": requestId
  });
  const contentType = request.headers.get("content-type");
  const accessJwt = request.headers.get("cf-access-jwt-assertion");

  if (contentType) headers.set("content-type", contentType);
  if (accessJwt) headers.set("cf-access-jwt-assertion", accessJwt);
  if (env.CF_ACCESS_CLIENT_ID && env.CF_ACCESS_CLIENT_SECRET) {
    headers.set("cf-access-client-id", env.CF_ACCESS_CLIENT_ID);
    headers.set("cf-access-client-secret", env.CF_ACCESS_CLIENT_SECRET);
  }
  if (env.ADMIN_API_TOKEN) headers.set("authorization", `Bearer ${env.ADMIN_API_TOKEN}`);

  return headers;
}

async function proxy(request: Request, env: Env, apiPath: string, requestId: string): Promise<Response> {
  const source = new URL(request.url);
  const upstream = new URL(upstreamPathFor(apiPath), env.ADMIN_API_BASE_URL);
  upstream.search = source.search;

  const response = await fetch(upstream, {
    method: request.method,
    headers: headersFor(request, env, requestId),
    body: request.method === "GET" || request.method === "HEAD" ? undefined : request.body,
    redirect: "manual"
  });

  return new Response(response.body, {
    status: response.status,
    headers: {
      "content-type": response.headers.get("content-type") ?? "application/json",
      "x-request-id": response.headers.get("x-request-id") ?? requestId
    }
  });
}

async function bootstrap(request: Request, env: Env, requestId: string): Promise<Response> {
  const response = await proxy(request, env, "/bootstrap", requestId);
  if (!response.ok) return response;

  const raw = (await response.json()) as AdminMeResponse;
  const role = AdminRoleSchema.parse(raw.principal.role);
  return Response.json(
    BootstrapResponseSchema.parse({
      environment: env.ADMIN_PROFILE ?? "prd",
      admin: {
        id: raw.principal.id,
        displayName: raw.principal.displayName ?? raw.principal.id,
        role,
        permissions: rolePermissions[role]
      }
    }),
    {
      headers: {
        "x-request-id": requestId
      }
    }
  );
}

export const onRequest = async ({ request, env, params }: PagesContext): Promise<Response> => {
  const requestId = requestIdFrom(request);
  const apiPath = apiPathFrom(params.path);

  try {
    if (!env.ADMIN_API_BASE_URL) {
      return Response.json({ error: "ADMIN_API_BASE_URL_REQUIRED" }, { status: 500 });
    }
    if (apiPath === "/bootstrap" && request.method === "GET") {
      return bootstrap(request, env, requestId);
    }
    return proxy(request, env, apiPath, requestId);
  } catch (error) {
    return Response.json(
      {
        error: {
          code: "ADMIN_PAGES_PROXY_ERROR",
          message: error instanceof Error ? error.message : "Admin Pages proxy failed.",
          requestId
        }
      },
      {
        status: 502,
        headers: {
          "x-request-id": requestId
        }
      }
    );
  }
};
