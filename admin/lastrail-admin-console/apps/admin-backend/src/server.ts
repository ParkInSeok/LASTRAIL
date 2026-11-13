import crypto from "node:crypto";
import fastify, { type FastifyInstance } from "fastify";
import { z, ZodError, type ZodSchema } from "zod";
import {
  AuditLogsResponseSchema,
  AdminRoleSchema,
  BootstrapResponseSchema,
  CreatePlayerNoteRequestSchema,
  CreatePlayerNoteResponseSchema,
  IntegrityEventsResponseSchema,
  PlayerDetailSchema,
  PlayerNotesResponseSchema,
  PlayerSearchResponseSchema,
  RunDetailSchema,
  RunSearchResponseSchema,
  SuspiciousPlayerDetailSchema,
  SuspiciousPlayersResponseSchema,
  managementRoute,
  rolePermissions
} from "@lastrail/admin-contracts";
import type { AdminBackendConfig } from "./config";
import { createUpstreamClient, UpstreamHttpError, type UpstreamClient } from "./upstream";

type BuildServerOptions = {
  config: AdminBackendConfig;
  upstream?: UpstreamClient;
};

const AdminMeResponseSchema = z.object({
  principal: z.object({
    id: z.string().min(1),
    accessSubject: z.string().min(1),
    role: AdminRoleSchema,
    displayName: z.string().nullable()
  })
});

class ProxyRouteError extends Error {
  constructor(
    readonly statusCode: number,
    readonly code: string,
    message: string,
    readonly requestId: string
  ) {
    super(message);
  }
}

function requestIdFrom(headers: Record<string, unknown>): string {
  const header = headers["x-request-id"];
  return typeof header === "string" && header.length > 0
    ? header
    : crypto.randomUUID();
}

function normalizeError(error: unknown, requestId: string) {
  if (error instanceof ZodError) {
    return {
      statusCode: 502,
      body: {
        error: {
          code: "UPSTREAM_SCHEMA_INVALID",
          message: "Admin API response did not match the expected contract.",
          requestId
        }
      }
    };
  }

  return {
    statusCode: 502,
    body: {
      error: {
        code: "UPSTREAM_REQUEST_FAILED",
        message: error instanceof Error ? error.message : "Admin API request failed.",
        requestId
      }
    }
  };
}

async function proxyRead<T>(
  server: FastifyInstance,
  schema: ZodSchema<T>,
  upstream: UpstreamClient,
  upstreamPath: string,
  requestId: string,
  searchParams?: URLSearchParams
): Promise<T> {
  try {
    const raw = await upstream(upstreamPath, {
      method: "GET",
      requestId,
      searchParams
    });
    return schema.parse(raw);
  } catch (error) {
    const normalized = normalizeError(error, requestId);
    throw new ProxyRouteError(
      normalized.statusCode,
      normalized.body.error.code,
      normalized.body.error.message,
      requestId
    );
  }
}

async function proxyWrite<T>(
  schema: ZodSchema<T>,
  upstream: UpstreamClient,
  upstreamPath: string,
  requestId: string,
  body: unknown
): Promise<T> {
  try {
    const raw = await upstream(upstreamPath, {
      method: "POST",
      requestId,
      body
    });
    return schema.parse(raw);
  } catch (error) {
    const normalized = normalizeError(error, requestId);
    throw new ProxyRouteError(
      normalized.statusCode,
      normalized.body.error.code,
      normalized.body.error.message,
      requestId
    );
  }
}

export function buildServer(options: BuildServerOptions): FastifyInstance {
  const server = fastify({
    logger: {
      level: process.env.LOG_LEVEL ?? "info"
    },
    trustProxy: false
  });
  const upstream = options.upstream ?? createUpstreamClient(options.config);
  const opsFilterKeys = new Set(["category","q","serverVersion","appVersion","appVersionCode","phase","contractVersion","environment","service","severity","status","source","errorCode","requestId","accountId","runId","rewardPreviewId","rewardClaimId","offlineRewardClaimId","gachaBatchId","gachaBannerId","storyProgressId","integrityEventId","supportTicketId","occurredFrom","occurredTo","limit","cursor"]);
  for(const route of [
    {method:"GET",url:"/api/ops/error-logs"},{method:"GET",url:"/api/ops/error-logs/summary"},{method:"GET",url:"/api/ops/error-logs/groups"},
    {method:"GET",url:"/api/ops/error-logs/:id"},{method:"GET",url:"/api/ops/error-logs/:id/investigation"},{method:"POST",url:"/api/ops/error-logs/:id/investigation"}
  ] as const){
    server.route({...route,handler:async(request,reply)=>{
      reply.header("cache-control","no-store");const source=new URL(request.raw.url!,"http://local"),key=request.headers["idempotency-key"];
      const aggregate=/^\/api\/ops\/error-logs(?:\/(?:summary|groups))?$/.test(source.pathname);
      const detail=/^\/api\/ops\/error-logs\/[A-Za-z0-9_-]{1,128}(?:\/investigation)?$/.test(source.pathname);
      if(!aggregate&&!detail || !aggregate&&source.search || aggregate&&Array.from(source.searchParams.keys()).some(k=>!opsFilterKeys.has(k)||source.searchParams.getAll(k).length!==1))return reply.code(400).send({error:"contract_violation"});
      if(request.method==="POST"&&(typeof key!=="string"||!(/^[\x21-\x7e]{1,128}$/u).test(key)))return reply.code(400).send({error:"idempotency_key_required"});
      try{return await upstream(source.pathname.replace(/^\/api\//,"/v1/admin/"),{method:request.method as "GET"|"POST",requestId:requestIdFrom(request.headers),...(aggregate?{searchParams:source.searchParams}:{}),...(request.method==="POST"?{body:request.body,idempotencyKey:key as string}:{})});}
      catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode,...(error.current!==undefined?{current:error.current}:{})});return reply.code(502).send({error:"ADMIN_OPS_PROXY_ERROR"});}
    }});
  }
  server.post("/api/probability-disclosures",async(request,reply)=>{
    reply.header("cache-control","no-store");const key=request.headers["idempotency-key"];
    if(new URL(request.raw.url!,"http://local").search||typeof key!=="string"||!/^[\x21-\x7e]{1,128}$/u.test(key))return reply.code(400).send({error:"contract_violation"});
    try{return await upstream("/v1/admin/probability-disclosures",{method:"POST",requestId:requestIdFrom(request.headers),body:request.body,idempotencyKey:key});}
    catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
  });
  for(const route of ["current-state","immediate-review","immediate-apply"]){
    server.route({method:route==="current-state"?"GET":"POST",url:"/api/maintenance/"+route,handler:async(request,reply)=>{
      reply.header("cache-control","no-store");const source=new URL(request.raw.url!,"http://local"),key=request.headers["idempotency-key"];
      if(source.search||(request.method==="GET"&&request.body!==undefined))return reply.code(400).send({error:"contract_violation"});
      if(route==="immediate-apply"&&(typeof key!=="string"||!/^[\x21-\x7e]{1,128}$/.test(key)))return reply.code(400).send({error:"idempotency_key_required"});
      try{return await upstream(source.pathname.replace(/^\/api\//,"/v1/admin/"),{method:request.method as "GET"|"POST",requestId:requestIdFrom(request.headers),...(request.method==="POST"?{body:request.body}:{}),...(route==="immediate-apply"?{idempotencyKey:key as string}:{})});}catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
    }});
  }
  server.post('/api/community/contents/:id/comment-lock',async(request,reply)=>{
   reply.header('cache-control','no-store');const source=new URL(request.raw.url!,'http://local'),key=request.headers['idempotency-key'],version=request.headers['x-community-comment-lock-version'];
   if(source.search||!/^\/api\/community\/contents\/[A-Za-z0-9_-]{1,128}\/comment-lock$/.test(source.pathname))return reply.code(400).send({error:'contract_violation'});
   if(typeof key!=='string'||!/^[\x21-\x7e]{1,128}$/.test(key))return reply.code(400).send({error:'idempotency_key_required'});
   try{return await upstream(source.pathname.replace(/^\/api\//,'/v1/admin/'),{method:'POST',requestId:requestIdFrom(request.headers),body:request.body,idempotencyKey:key,...(typeof version==='string'?{communityCommentLockVersion:version}:{})});}catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
  });
  for(const route of [{url:'/api/community/reports',method:'GET'}, {url:'/api/community/reports/:id',method:'GET'},{url:'/api/community/contents/:id',method:'GET'},{url:'/api/community/reports/:id/review',method:'POST'},{url:'/api/community/contents/:id/moderate',method:'POST'}] as const){
   server.route({method:route.method,url:route.url,handler:async(request,reply)=>{
    reply.header('cache-control','no-store');const source=new URL(request.raw.url!,'http://local'),list=route.url==='/api/community/reports',key=request.headers['idempotency-key'];
    if(request.method==='GET'&&request.body!==undefined||!list&&source.search||list&&Array.from(source.searchParams.keys()).some(k=>!['status','limit','cursor'].includes(k)||source.searchParams.getAll(k).length!==1)||!list&&!/^\/api\/community\/(?:reports|contents)\/[A-Za-z0-9_-]{1,128}(?:\/(?:review|moderate))?$/.test(source.pathname))return reply.code(400).send({error:'contract_violation'});
    if(request.method==='POST'&&(typeof key!=='string'||!/^[\x21-\x7e]{1,128}$/.test(key)))return reply.code(400).send({error:'idempotency_key_required'});
    const version=request.headers['x-community-moderation-version'];try{return await upstream(source.pathname.replace(/^\/api\//,'/v1/admin/'),{method:request.method as 'GET'|'POST',requestId:requestIdFrom(request.headers),...(typeof version==='string'?{communityModerationVersion:version}:{}),...(list?{searchParams:source.searchParams}:{}),...(request.method==='POST'?{body:request.body,idempotencyKey:key as string}:{})});}catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
   }});
  }
  for(const url of ["/api/support/tickets/:ticketId/processing","/api/support/processor-grants/:principalId","/api/community/moderator-grants/:principalId"]){
    server.route({method:["GET","POST"],url,handler:async(request,reply)=>{
      reply.header("cache-control","no-store");const source=new URL(request.raw.url!,"http://local"),key=request.headers["idempotency-key"];
      if(source.search||(request.method==="GET"&&request.body!==undefined))return reply.code(400).send({error:"contract_violation"});
      if(request.method==="POST"&&(typeof key!=="string"||!/^[\x21-\x7e]{1,128}$/.test(key)))return reply.code(400).send({error:"idempotency_key_required"});
      const community=source.pathname.startsWith('/api/community/moderator-grants/');
      if(community&&!/^\/api\/community\/moderator-grants\/[A-Za-z0-9_-]{1,128}$/.test(source.pathname))return reply.code(400).send({error:'contract_violation'});
      const version=request.headers['x-community-moderation-grant-version'];
      try{return await upstream(source.pathname.replace(/^\/api\//,"/v1/admin/"),{method:request.method as "GET"|"POST",requestId:requestIdFrom(request.headers),...(community&&typeof version==='string'?{communityGrantVersion:version}:{}),...(request.method==="POST"?{body:request.body,idempotencyKey:key as string}:{})});}
      catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
    }});
  }
  server.get("/api/probability-disclosures/source",async(request,reply)=>{
    reply.header("cache-control","no-store");const url=new URL(request.raw.url!,"http://local");
    if(request.body!==undefined)return reply.code(400).send({error:"contract_violation"});
    for(const key of url.searchParams.keys())if(!["consumer","sourceKind","policyVersion"].includes(key)||url.searchParams.getAll(key).length!==1)return reply.code(400).send({error:"contract_violation"});
    try{return await upstream("/v1/admin/probability-disclosures/source",{method:"GET",requestId:requestIdFrom(request.headers),searchParams:url.searchParams});}
    catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
  });
  server.post("/api/maintenance-plans/:maintenanceId/commands",async(request,reply)=>{
    reply.header("cache-control","no-store");const url=new URL(request.raw.url!,"http://local"),key=request.headers["idempotency-key"];
    if(url.search)return reply.code(400).send({error:"contract_violation"});
    if(typeof key!=="string"||!/^[\x21-\x7e]{1,128}$/u.test(key))return reply.code(400).send({error:"idempotency_key_required"});
    try{return await upstream(url.pathname.replace(/^\/api\//,"/v1/admin/"),{method:"POST",requestId:requestIdFrom(request.headers),body:request.body,idempotencyKey:key});}
    catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
  });
  for(const route of ["/api/maintenance-plans","/api/maintenance-plans/:maintenanceId","/api/maintenance-plans/:maintenanceId/preview"]){
    server.get(route,async(request,reply)=>{
      reply.header("cache-control","no-store");
      const url=new URL(request.raw.url!,"http://local");
      const allowed=route.endsWith("/preview")?["at"]:route==="/api/maintenance-plans"?["after","limit"]:[];
      if(request.body!==undefined)return reply.code(400).send({error:"contract_violation"});
      for(const key of url.searchParams.keys())if(!allowed.includes(key)||url.searchParams.getAll(key).length!==1)return reply.code(400).send({error:"contract_violation"});
      try{return await upstream(url.pathname.replace(/^\/api\//,"/v1/admin/"),{method:"GET",requestId:requestIdFrom(request.headers),searchParams:url.searchParams});}
      catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
    });
  }
  for(const route of ["/api/notices","/api/notices/:noticeId","/api/notices/:noticeId/commands"]){
    server.route({method:route.endsWith("/commands")?"POST":"GET",url:route,handler:async(request,reply)=>{
      reply.header("cache-control","no-store");
      const url=new URL(request.raw.url!,"http://local");
      const key=request.headers["idempotency-key"];
      if(request.method==="POST"&&(typeof key!=="string"||!/^[\x21-\x7e]{1,128}$/u.test(key)))return reply.code(400).send({error:"idempotency_key_required"});
      if(request.method==="GET"&&request.body!==undefined)return reply.code(400).send({error:"contract_violation"});
      if(route!=="/api/notices"&&url.search)return reply.code(400).send({error:"contract_violation"});
      for(const queryKey of url.searchParams.keys())if(!["after","limit"].includes(queryKey)||url.searchParams.getAll(queryKey).length!==1)return reply.code(400).send({error:"contract_violation"});
      try{return await upstream(url.pathname.replace(/^\/api\//,"/v1/admin/"),{method:request.method as "GET"|"POST",requestId:requestIdFrom(request.headers),searchParams:url.searchParams,...(request.method==="POST"?{body:request.body,idempotencyKey:key as string}:{})});}
      catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
    }});
  }
  server.route({method:["GET","POST"],url:"/api/runtime-policy",handler:async(request,reply)=>{
    reply.header("cache-control","no-store");
    if(Object.keys(request.query as object).length || (request.method==="GET" && request.body!==undefined))return reply.code(400).send({error:"contract_violation"});
    const key=request.headers["idempotency-key"];
    if(request.method==="POST" && (typeof key!=="string" || !/^[\x21-\x7e]{1,128}$/u.test(key)))return reply.code(400).send({error:"idempotency_key_required"});
    try{return await upstream("/v1/admin/runtime-policy",{method:request.method as "GET"|"POST",requestId:requestIdFrom(request.headers),...(request.method==="POST"?{body:request.body,idempotencyKey:key as string}:{})});}
    catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode});throw error;}
  }});

  server.addHook("onRequest", async (request, reply) => {
    const requestId = requestIdFrom(request.headers);
    request.headers["x-request-id"] = requestId;
    reply.header("x-request-id", requestId);
    if(/^\/api\/(?:liveops|reward-tracking|unity-builds|events)(?:\/|\?|$)/.test(request.raw.url??""))reply.header("cache-control","no-store");
  });

  server.setErrorHandler((error: Error, request, reply) => {
    const requestId = requestIdFrom(request.headers);
    const parser=error as Error&{statusCode?:number};
    if(/^\/api\/(?:liveops|reward-tracking|unity-builds|events)(?:\/|\?|$)/.test(request.raw.url??"")&&parser.statusCode&&parser.statusCode>=400&&parser.statusCode<500){return reply.code(parser.statusCode).header("cache-control","no-store").send({error:{code:"contract_violation",message:"요청 본문 형식을 확인해 주세요.",requestId}});}
    const routeError =
      error instanceof ProxyRouteError
        ? error
        : new ProxyRouteError(500, "INTERNAL_ERROR", error.message, requestId);

    reply.status(routeError.statusCode).send({
      error: {
        code: routeError.code,
        message: routeError.message,
        requestId: routeError.requestId
      }
    });
  });

  for(const route of ['/api/liveops/*','/api/reward-tracking/*','/api/unity-builds','/api/unity-builds/*','/api/events/*'])server.all(route,async(request,reply)=>{
    reply.header('cache-control','no-store');const url=new URL(request.raw.url!,'http://local');const policy=managementRoute(url.pathname.replace(/^\/api/,''),request.method,url.searchParams,request.headers['if-match']!==undefined);
    if(!policy||policy.error||request.method==='GET'&&request.body!==undefined)return reply.code(400).send({error:'contract_violation'});
    const key=request.headers['idempotency-key'];if(policy.requiresKey&&(typeof key!=='string'||!/^[\x21-\x7e]{1,128}$/.test(key)))return reply.code(400).send({error:'idempotency_key_required'});
    try{return await upstream(url.pathname.replace(/^\/api\//,'/v1/admin/'),{method:request.method as 'GET'|'POST',requestId:requestIdFrom(request.headers),searchParams:url.searchParams,...(request.method==='POST'?{body:request.body}:{}),...(policy.requiresKey?{idempotencyKey:key as string}:{})});}
    catch(error){if(error instanceof UpstreamHttpError)return reply.code(error.status).send({error:error.errorCode,...(error.current!==undefined?{current:error.current}:{})});throw error;}
  });
  server.get("/api/bootstrap", async (request) => {
    const adminMe = await proxyRead(
      server,
      AdminMeResponseSchema,
      upstream,
      "/v1/admin/me",
      requestIdFrom(request.headers)
    );

    return BootstrapResponseSchema.parse({
      environment: options.config.profile,
      admin: {
        id: adminMe.principal.id,
        displayName: adminMe.principal.displayName ?? adminMe.principal.id,
        role: adminMe.principal.role,
        permissions: rolePermissions[adminMe.principal.role]
      }
    });
  });

  server.get("/api/players", async (request) => {
    const query = new URLSearchParams();
    const rawQuery = request.query as Record<string, string | undefined>;
    if (rawQuery.q) query.set("q", rawQuery.q);
    if (rawQuery.cursor) query.set("cursor", rawQuery.cursor);

    return proxyRead(
      server,
      PlayerSearchResponseSchema,
      upstream,
      "/v1/admin/players",
      requestIdFrom(request.headers),
      query
    );
  });

  server.get("/api/players/:accountId", async (request) => {
    const params = request.params as { accountId: string };
    return proxyRead(
      server,
      PlayerDetailSchema,
      upstream,
      `/v1/admin/players/${encodeURIComponent(params.accountId)}`,
      requestIdFrom(request.headers)
    );
  });

  server.post("/api/players/:accountId/notes", async (request, reply) => {
    const params = request.params as { accountId: string };
    const body = CreatePlayerNoteRequestSchema.parse(request.body);
    const response = await proxyWrite(
      CreatePlayerNoteResponseSchema,
      upstream,
      `/v1/admin/players/${encodeURIComponent(params.accountId)}/notes`,
      requestIdFrom(request.headers),
      body
    );

    return reply.status(201).send(response);
  });

  server.get("/api/players/:accountId/notes", async (request) => {
    const params = request.params as { accountId: string };
    return proxyRead(
      server,
      PlayerNotesResponseSchema,
      upstream,
      `/v1/admin/players/${encodeURIComponent(params.accountId)}/notes`,
      requestIdFrom(request.headers)
    );
  });

  server.get("/api/audit", async (request) => {
    const query = new URLSearchParams();
    const rawQuery = request.query as Record<string, string | undefined>;
    if (rawQuery.action) query.set("action", rawQuery.action);
    if (rawQuery.resourceId) query.set("resourceId", rawQuery.resourceId);
    if (rawQuery.result) query.set("result", rawQuery.result);

    return proxyRead(
      server,
      AuditLogsResponseSchema,
      upstream,
      "/v1/admin/audit",
      requestIdFrom(request.headers),
      query
    );
  });

  server.get("/api/integrity/events", async (request) => {
    const query = new URLSearchParams();
    const rawQuery = request.query as Record<string, string | undefined>;
    if (rawQuery.accountId) query.set("accountId", rawQuery.accountId);
    if (rawQuery.classification) query.set("classification", rawQuery.classification);
    if (rawQuery.reviewState) query.set("reviewState", rawQuery.reviewState);

    return proxyRead(
      server,
      IntegrityEventsResponseSchema,
      upstream,
      "/v1/admin/integrity/events",
      requestIdFrom(request.headers),
      query
    );
  });

  server.get("/api/integrity/suspicious-players", async (request) => {
    const query = new URLSearchParams();
    const rawQuery = request.query as Record<string, string | undefined>;
    if (rawQuery.reviewStatus) query.set("reviewStatus", rawQuery.reviewStatus);

    return proxyRead(
      server,
      SuspiciousPlayersResponseSchema,
      upstream,
      "/v1/admin/integrity/suspicious-players",
      requestIdFrom(request.headers),
      query
    );
  });

  server.get("/api/integrity/suspicious-players/:accountId", async (request) => {
    const { accountId } = request.params as { accountId: string };

    return proxyRead(
      server,
      SuspiciousPlayerDetailSchema,
      upstream,
      `/v1/admin/integrity/suspicious-players/${encodeURIComponent(accountId)}`,
      requestIdFrom(request.headers)
    );
  });

  server.get("/api/runs", async (request) => {
    const query = new URLSearchParams();
    const rawQuery = request.query as Record<string, string | undefined>;
    if (rawQuery.accountId) query.set("accountId", rawQuery.accountId);
    if (rawQuery.cursor) query.set("cursor", rawQuery.cursor);

    return proxyRead(
      server,
      RunSearchResponseSchema,
      upstream,
      "/v1/admin/runs",
      requestIdFrom(request.headers),
      query
    );
  });

  server.get("/api/runs/:runId", async (request) => {
    const params = request.params as { runId: string };
    return proxyRead(
      server,
      RunDetailSchema,
      upstream,
      `/v1/admin/runs/${encodeURIComponent(params.runId)}`,
      requestIdFrom(request.headers)
    );
  });

  return server;
}



