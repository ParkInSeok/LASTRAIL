import { z } from "zod";
import { ActivityError } from "./activity-policy";

const proofSchema = z.object({
  accountId: z.string().min(1), environment: z.enum(["LOCAL", "DEV", "PRD"]),
  provider: z.enum(["GOOGLE_PLAY", "APP_STORE"]), appId: z.string().min(1).max(200),
  productId: z.string().min(1).max(200), transactionId: z.string().trim().min(1).max(512),
  purchasedAt: z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER),
  verifiedAt: z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER), revoked: z.literal(false),
});
export type StoreProof = z.infer<typeof proofSchema>;
export type StoreIdentity = Pick<StoreProof, "accountId" | "environment" | "provider" | "appId" | "productId">;
export type CrystalPurchaseVerifier = (input: StoreIdentity & { receipt: string; now: number }) => Promise<(StoreProof & { state: "PURCHASED" }) | null>;

export function verifiedStoreProof(value: unknown, expected: StoreIdentity, now: number): StoreProof {
  const parsed = proofSchema.safeParse(value);
  if (!parsed.success || Object.entries(expected).some(([key, value]) => parsed.data[key as keyof StoreIdentity] !== value)
    || parsed.data.verifiedAt > now || parsed.data.verifiedAt < now - 60000 || parsed.data.purchasedAt > now) {
    throw new ActivityError(403, "purchase_not_verified");
  }
  return parsed.data;
}

export function storeOrderInsert(db: D1Database, proof: StoreProof, kind: "PASS" | "CRYSTAL" | "AD_SKIP_PERMANENT", verificationRef: string) {
  const orderId = `store_order_${crypto.randomUUID()}`;
  return { orderId, statement: db.prepare(`INSERT INTO verified_store_orders
    (id,account_id,provider,app_id,product_id,transaction_id,environment,product_kind,verification_ref,purchased_at,verified_at,revoked_at)
    VALUES (?,?,?,?,?,?,?,?,?,?,?,NULL)`).bind(orderId, proof.accountId, proof.provider, proof.appId, proof.productId,
      proof.transactionId, proof.environment, kind, verificationRef, proof.purchasedAt, proof.verifiedAt) };
}
