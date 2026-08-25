import { z } from "zod";

const ConfigSchema = z.object({
  ADMIN_API_BASE_URL: z.string().url(),
  LASTRAIL_ADMIN_PROFILE: z
    .enum(["local", "staging", "production"])
    .default("local"),
  ADMIN_BACKEND_HOST: z.string().default("127.0.0.1"),
  ADMIN_BACKEND_PORT: z.coerce.number().int().positive().default(4174),
  CF_ACCESS_CLIENT_ID: z.string().optional(),
  CF_ACCESS_CLIENT_SECRET: z.string().optional(),
  ADMIN_API_TOKEN: z.string().optional(),
  ADMIN_LOCAL_ACCESS_SUBJECT: z.string().optional()
});

export type AdminBackendConfig = {
  adminApiBaseUrl: string;
  profile: "local" | "staging" | "production";
  host: string;
  port: number;
  cloudflareAccessClientId?: string;
  cloudflareAccessClientSecret?: string;
  adminApiToken?: string;
  localAccessSubject?: string;
};

export function loadConfig(env: NodeJS.ProcessEnv = process.env): AdminBackendConfig {
  const parsed = ConfigSchema.parse(env);

  return {
    adminApiBaseUrl: parsed.ADMIN_API_BASE_URL,
    profile: parsed.LASTRAIL_ADMIN_PROFILE,
    host: parsed.ADMIN_BACKEND_HOST,
    port: parsed.ADMIN_BACKEND_PORT,
    cloudflareAccessClientId: parsed.CF_ACCESS_CLIENT_ID,
    cloudflareAccessClientSecret: parsed.CF_ACCESS_CLIENT_SECRET,
    adminApiToken: parsed.ADMIN_API_TOKEN,
    localAccessSubject: parsed.ADMIN_LOCAL_ACCESS_SUBJECT
  };
}
