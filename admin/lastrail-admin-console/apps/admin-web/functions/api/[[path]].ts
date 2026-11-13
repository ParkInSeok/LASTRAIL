import { AdminRoleSchema, BootstrapResponseSchema, managementRoute, rolePermissions } from "@lastrail/admin-contracts";

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
  if (accessJwt) headers.set("x-lastrail-user-access-jwt", accessJwt);
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
  const headers = headersFor(request, env, requestId);
  const management=managementRoute(apiPath,request.method,source.searchParams,request.headers.has('If-Match'));
  if(management){
    if(management.error||request.method==='GET'&&request.body!==null)return Response.json({error:'contract_violation'},{status:400,headers:{'cache-control':'no-store'}});
    if(management.requiresKey){const key=request.headers.get('Idempotency-Key');if(!key||!/^[\x21-\x7e]{1,128}$/.test(key))return Response.json({error:'idempotency_key_required'},{status:400,headers:{'cache-control':'no-store'}});headers.set('Idempotency-Key',key);}
  }
  const opsRoute=apiPath==="/ops/error-logs"||apiPath.startsWith("/ops/error-logs/");
  if(opsRoute){
    const aggregate=/^\/ops\/error-logs(?:\/(?:summary|groups))?$/.test(apiPath),detail=/^\/ops\/error-logs\/[A-Za-z0-9_-]{1,128}(?:\/investigation)?$/.test(apiPath),investigation=/\/investigation$/.test(apiPath);
    const filterKeys=new Set(["category","q","serverVersion","appVersion","appVersionCode","phase","contractVersion","environment","service","severity","status","source","errorCode","requestId","accountId","runId","rewardPreviewId","rewardClaimId","offlineRewardClaimId","gachaBatchId","gachaBannerId","storyProgressId","integrityEventId","supportTicketId","occurredFrom","occurredTo","limit","cursor"]);
    if(!aggregate&&!detail || !aggregate&&source.search || request.method!=="GET"&&!(investigation&&request.method==="POST") || aggregate&&Array.from(source.searchParams.keys()).some(k=>!filterKeys.has(k)||source.searchParams.getAll(k).length!==1))return Response.json({error:"contract_violation"},{status:400,headers:{"cache-control":"no-store"}});
    if(request.method==="POST"){
      const key=request.headers.get("Idempotency-Key");if(!key||!/^[\x21-\x7e]{1,128}$/u.test(key))return Response.json({error:"idempotency_key_required"},{status:400,headers:{"cache-control":"no-store"}});
      headers.set("Idempotency-Key",key);
    }
  }
  const maintenanceRoute=["/maintenance/current-state","/maintenance/immediate-review","/maintenance/immediate-apply"].includes(apiPath);
  if(maintenanceRoute){
    const method=apiPath==="/maintenance/current-state"?"GET":"POST";
    if(source.search||request.method!==method)return Response.json({error:"contract_violation"},{status:400,headers:{"cache-control":"no-store"}});
    if(apiPath==="/maintenance/immediate-apply"){
      const key=request.headers.get("Idempotency-Key");if(!key||!/^[\x21-\x7e]{1,128}$/.test(key))return Response.json({error:"idempotency_key_required"},{status:400,headers:{"cache-control":"no-store"}});
      headers.set("Idempotency-Key",key);
    }
  }
  if ((apiPath === "/runtime-policy" || apiPath === "/maintenance-runtime/initialize" || apiPath === "/probability-disclosures" || /^\/(?:notices|maintenance-plans)\/[^/]+\/commands$/.test(apiPath)) && request.method === "POST") {
    const key = request.headers.get("Idempotency-Key");
    if (key) headers.set("Idempotency-Key", key);
  }

  const commentLockRoute=/^\/community\/contents\/[^/]+\/comment-lock$/.test(apiPath);
  if(commentLockRoute){
    if(source.search||request.method!=='POST'||!/^\/community\/contents\/[A-Za-z0-9_-]{1,128}\/comment-lock$/.test(apiPath))return Response.json({error:'contract_violation'},{status:400,headers:{'cache-control':'no-store'}});
    const version=request.headers.get('X-Community-Comment-Lock-Version');if(version)headers.set('X-Community-Comment-Lock-Version',version);
    const key=request.headers.get('Idempotency-Key');if(!key||!/^[\x21-\x7e]{1,128}$/.test(key))return Response.json({error:'idempotency_key_required'},{status:400,headers:{'cache-control':'no-store'}});headers.set('Idempotency-Key',key);
  }
  const moderationRoute=/^\/community\/(?:reports|contents)(?:\/[^/]+(?:\/(?:review|moderate))?)?$/.test(apiPath);
  if(moderationRoute){
    const write=/\/(?:review|moderate)$/.test(apiPath),list=apiPath==='/community/reports';
    const valid=list||/^\/community\/(?:reports\/[A-Za-z0-9_-]{1,128}(?:\/review)?|contents\/[A-Za-z0-9_-]{1,128}(?:\/moderate)?)$/.test(apiPath);
    if(!valid||request.method!==(write?'POST':'GET')||(!list&&source.search)||list&&Array.from(source.searchParams.keys()).some(k=>!['status','limit','cursor'].includes(k)||source.searchParams.getAll(k).length!==1))return Response.json({error:'contract_violation'},{status:400,headers:{'cache-control':'no-store'}});
    const version=request.headers.get('X-Community-Moderation-Version');if(version)headers.set('X-Community-Moderation-Version',version);
    if(write){const key=request.headers.get('Idempotency-Key');if(!key||!/^[\x21-\x7e]{1,128}$/.test(key))return Response.json({error:'idempotency_key_required'},{status:400,headers:{'cache-control':'no-store'}});headers.set('Idempotency-Key',key);}
  }
  const communityCommand=/^\/community\/moderator-grants\/[^/]+$/.test(apiPath);
  if(communityCommand){
    if(source.search||!['GET','POST'].includes(request.method)||!/^\/community\/moderator-grants\/[A-Za-z0-9_-]{1,128}$/.test(apiPath))return Response.json({error:'contract_violation'},{status:400,headers:{'cache-control':'no-store'}});
    const version=request.headers.get('X-Community-Moderation-Grant-Version');if(version)headers.set('X-Community-Moderation-Grant-Version',version);
    if(request.method==='POST'){const key=request.headers.get('Idempotency-Key');if(!key||!/^[\x21-\x7e]{1,128}$/.test(key))return Response.json({error:'idempotency_key_required'},{status:400,headers:{'cache-control':'no-store'}});headers.set('Idempotency-Key',key);}
  }
  const supportCommand=/^\/support\/(?:tickets\/[^/]+\/processing|processor-grants\/[^/]+)$/.test(apiPath);
  if(supportCommand){
    if(source.search||!["GET","POST"].includes(request.method))return Response.json({error:"contract_violation"},{status:400,headers:{"cache-control":"no-store"}});
    if(request.method==="POST"){
      const key=request.headers.get("Idempotency-Key");
      if(!key||!/^[\x21-\x7e]{1,128}$/.test(key))return Response.json({error:"idempotency_key_required"},{status:400,headers:{"cache-control":"no-store"}});
      headers.set("Idempotency-Key",key);
    }
  }
  const response = await fetch(upstream, {
    method: request.method,
    headers,
    body: request.method === "GET" || request.method === "HEAD" ? undefined : request.body,
    redirect: "manual"
  });

  return new Response(response.body, {
    status: response.status,
    headers: {
      "content-type": response.headers.get("content-type") ?? "application/json",
      "x-request-id": response.headers.get("x-request-id") ?? requestId,
      ...(management||opsRoute||commentLockRoute||moderationRoute||communityCommand||supportCommand||maintenanceRoute||apiPath==="/maintenance-runtime"||apiPath==="/maintenance-runtime/initialize"?{"cache-control":"no-store"}:{}),
      ...(apiPath === "/runtime-policy" || apiPath === "/notices" || apiPath.startsWith("/notices/") || apiPath === "/maintenance-plans" || apiPath.startsWith("/maintenance-plans/") || apiPath === "/probability-disclosures/source" || apiPath === "/probability-disclosures" ? {"cache-control":"no-store"} : {})
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
      return Response.json({ error: "ADMIN_API_BASE_URL_REQUIRED" }, { status: 500,headers:/^\/(?:liveops|reward-tracking|unity-builds|events)(?:\/|$)/.test(apiPath)?{"cache-control":"no-store"}:{} });
    }
    if (apiPath === "/bootstrap" && request.method === "GET") {
      return bootstrap(request, env, requestId);
    }
    return await proxy(request, env, apiPath, requestId);
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
          ...(apiPath.startsWith("/unity-builds")||apiPath.startsWith("/events/")||apiPath.startsWith("/liveops/")||apiPath.startsWith("/reward-tracking/")||apiPath.startsWith("/ops/error-logs")||apiPath.startsWith("/community/reports")||apiPath.startsWith("/community/contents")||apiPath.startsWith("/community/moderator-grants/")||apiPath.startsWith("/maintenance/")||apiPath==="/maintenance-runtime"||apiPath==="/maintenance-runtime/initialize"?{"cache-control":"no-store"}:{}),
          "x-request-id": requestId
        }
      }
    );
  }
};



