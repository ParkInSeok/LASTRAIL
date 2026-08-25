import type { AdminBackendConfig } from "./config";

export type UpstreamClient = (
  path: string,
  init: {
    method: "GET";
    requestId: string;
    searchParams?: URLSearchParams;
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

    const headers = new Headers({
      accept: "application/json",
      "x-request-id": init.requestId
    });

    if (config.cloudflareAccessClientId && config.cloudflareAccessClientSecret) {
      headers.set("CF-Access-Client-Id", config.cloudflareAccessClientId);
      headers.set("CF-Access-Client-Secret", config.cloudflareAccessClientSecret);
    }

    if (config.adminApiToken) {
      headers.set("authorization", `Bearer ${config.adminApiToken}`);
    }

    const response = await fetch(url, {
      method: init.method,
      headers
    });

    if (!response.ok) {
      throw new Error(`Upstream request failed with ${response.status}`);
    }

    return response.json();
  };
}

