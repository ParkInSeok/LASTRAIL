import type { AdminBackendConfig } from "./config";
export class UpstreamHttpError extends Error {
  constructor(readonly status:number,readonly errorCode:string,readonly current?:unknown){super(`Upstream request failed with ${status}`);}
}

export type UpstreamClient = (
  path: string,
  init: {
    method: "GET" | "POST";
    requestId: string;
    searchParams?: URLSearchParams;
    body?: unknown;
    idempotencyKey?: string;
    communityGrantVersion?: string;
    communityModerationVersion?: string;
    communityCommentLockVersion?: string;
  }
) => Promise<unknown>;

export function createUpstreamClient(config: AdminBackendConfig): UpstreamClient {
  return async (path, init) => {
    const url = new URL(path, config.adminApiBaseUrl);
    if (init.searchParams) {
      init.searchParams.forEach((value, key) => {
        url.searchParams.set(key, value);
      });
    }

    const opsRoute = /^\/v1\/admin\/ops\/error-logs(?:\/|$)/.test(path);
    const managementRoute=/^\/v1\/admin\/(?:liveops|reward-tracking|unity-builds|events)(?:\/|$)/.test(path);
    const headers = new Headers({
      accept: "application/json",
      "x-request-id": init.requestId
    });
    if (init.body !== undefined) {
      headers.set("content-type", "application/json");
    }
    if((path==="/v1/admin/runtime-policy" || path==="/v1/admin/probability-disclosures" || /^\/v1\/admin\/(?:notices|maintenance-plans)\/[^/]+\/commands$/.test(path)) && init.idempotencyKey)headers.set("Idempotency-Key",init.idempotencyKey);

    if (config.cloudflareAccessClientId && config.cloudflareAccessClientSecret) {
      headers.set("CF-Access-Client-Id", config.cloudflareAccessClientId);
      headers.set("CF-Access-Client-Secret", config.cloudflareAccessClientSecret);
    }
    const commentLockRoute=/^\/v1\/admin\/community\/contents\/[A-Za-z0-9_-]{1,128}\/comment-lock$/.test(path);
    if(commentLockRoute&&init.communityCommentLockVersion)headers.set('X-Community-Comment-Lock-Version',init.communityCommentLockVersion);
    const moderationRoute=/^\/v1\/admin\/community\/(?:reports|contents)(?:\/[A-Za-z0-9_-]{1,128}(?:\/(?:review|moderate))?)?$/.test(path);
    if(moderationRoute&&init.communityModerationVersion)headers.set('X-Community-Moderation-Version',init.communityModerationVersion);
    const communityCommand=/^\/v1\/admin\/community\/moderator-grants\/[A-Za-z0-9_-]{1,128}$/.test(path);
    if(communityCommand&&init.communityGrantVersion)headers.set('X-Community-Moderation-Grant-Version',init.communityGrantVersion);
    const supportCommand=commentLockRoute||moderationRoute||communityCommand||/^\/v1\/admin\/support\/(?:tickets\/[^/]+\/processing|processor-grants\/[^/]+)$/.test(path)||["/v1/admin/maintenance/current-state","/v1/admin/maintenance/immediate-review","/v1/admin/maintenance/immediate-apply"].includes(path);
    if(supportCommand&&init.idempotencyKey)headers.set("Idempotency-Key",init.idempotencyKey);

    if (config.adminApiToken) {
      headers.set("authorization", `Bearer ${config.adminApiToken}`);
    }

    if (config.profile === "local" && config.localAccessSubject) {
      headers.set("Cf-Access-Subject", config.localAccessSubject);
    }

    if(/^\/v1\/admin\/liveops\/notices\/[A-Za-z0-9][A-Za-z0-9_-]{0,79}\/commands$/.test(path)&&init.idempotencyKey)headers.set('Idempotency-Key',init.idempotencyKey);
    if(/^\/v1\/admin\/unity-builds(?:\/[A-Za-z0-9][A-Za-z0-9._-]{0,127}\/cancel)?$/.test(path)&&init.idempotencyKey)headers.set('Idempotency-Key',init.idempotencyKey);
    if(/^\/v1\/admin\/(?:events\/definitions|reward-tracking\/drafts)\/[^/]+\/commands$/.test(path)&&init.idempotencyKey)headers.set('Idempotency-Key',init.idempotencyKey);
    if(opsRoute && init.method === "POST" && init.idempotencyKey)headers.set("Idempotency-Key",init.idempotencyKey);
    const response = await fetch(url, {
      method: init.method,
      headers,
      ...(init.body !== undefined ? { body: JSON.stringify(init.body) } : {})
    });

    if (!response.ok) {
      if(opsRoute||managementRoute){
        let code="UPSTREAM_REQUEST_FAILED",current:unknown;
        try{const payload=await response.json() as {error?:unknown;current?:unknown};if(typeof payload.error==="string")code=payload.error;else if(payload.error&&typeof payload.error==="object"&&"code" in payload.error&&typeof payload.error.code==="string")code=payload.error.code;current=payload.current;}catch{/* Unreadable response remains an explicit failure. */}
        throw new UpstreamHttpError(response.status,code,current);
      }
      if(supportCommand){
        let code="UPSTREAM_REQUEST_FAILED";
        try{const payload=await response.json() as {error?:unknown};if(typeof payload.error==="string")code=payload.error;}catch{/* Malformed failure remains explicit. */}
        throw new UpstreamHttpError(response.status,code);
      }
      if(path==="/v1/admin/runtime-policy" || path==="/v1/admin/notices" || path.startsWith("/v1/admin/notices/") || path==="/v1/admin/maintenance-plans" || path.startsWith("/v1/admin/maintenance-plans/") || path==="/v1/admin/probability-disclosures/source" || path==="/v1/admin/probability-disclosures"){
        let code="UPSTREAM_REQUEST_FAILED";
        try{const payload=await response.json() as {error?:unknown};if(typeof payload.error==="string")code=payload.error;}catch{/* No trustworthy upstream error code. */}
        throw new UpstreamHttpError(response.status,code);
      }
      throw new Error(`Upstream request failed with ${response.status}`);
    }

    return response.json();
  };
}
