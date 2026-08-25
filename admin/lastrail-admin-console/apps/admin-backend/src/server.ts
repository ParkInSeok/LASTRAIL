import crypto from "node:crypto";
import fastify, { type FastifyInstance } from "fastify";
import { ZodError, type ZodSchema } from "zod";
import {
  BootstrapResponseSchema,
  PlayerDetailSchema,
  PlayerSearchResponseSchema,
  RunDetailSchema,
  RunSearchResponseSchema
} from "@lastrail/admin-contracts";
import type { AdminBackendConfig } from "./config";
import { createUpstreamClient, type UpstreamClient } from "./upstream";

type BuildServerOptions = {
  config: AdminBackendConfig;
  upstream?: UpstreamClient;
};

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

async function proxyRead(
  server: FastifyInstance,
  schema: ZodSchema,
  upstream: UpstreamClient,
  upstreamPath: string,
  requestId: string,
  searchParams?: URLSearchParams
) {
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
    return proxyRead(
      server,
      BootstrapResponseSchema,
      upstream,
      "/bootstrap",
      requestIdFrom(request.headers)
    );
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
      "/players",
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
      `/players/${encodeURIComponent(params.accountId)}`,
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
      "/runs",
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
      `/runs/${encodeURIComponent(params.runId)}`,
      requestIdFrom(request.headers)
    );
  });

  return server;
}
