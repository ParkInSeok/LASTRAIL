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
  SuspiciousPlayersResponseSchema,
  rolePermissions
} from "@lastrail/admin-contracts";
import type { AdminBackendConfig } from "./config";
import { createUpstreamClient, type UpstreamClient } from "./upstream";

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

  server.addHook("onRequest", async (request, reply) => {
    const requestId = requestIdFrom(request.headers);
    request.headers["x-request-id"] = requestId;
    reply.header("x-request-id", requestId);
  });

  server.setErrorHandler((error: Error, request, reply) => {
    const requestId = requestIdFrom(request.headers);
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
