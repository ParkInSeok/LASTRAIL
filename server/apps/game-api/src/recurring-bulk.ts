import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError, activityId } from "./activity-policy";
import { claimRecurringRight, type RecurringClaimAction } from "./recurring-grants";
import { recurringSequence, recurringRetryable } from "./recurring-progress";
import { walletId } from "./crystal-wallet";
import { bulkMemberSuccess, type BulkMember } from "./recurring-bulk-hooks";
const ids = z.array(activityId).min(1).max(50).refine(a => new Set(a).size === a.length);
const levels = z.array(z.number().int().min(1).max(50)).min(1).max(50).refine(a => new Set(a).size === a.length);
export const bulkClaimSchema = z.discriminatedUnion("kind", [
  z.strictObject({ kind: z.literal("QUEST"), awardIds: ids }), z.strictObject({ kind: z.literal("MAIL"), awardIds: ids }),
  z.strictObject({ kind: z.literal("PASS_LEVEL"), policyId: activityId, track: z.enum(["FREE", "PREMIUM"]), levels }),
]);
type Request = z.infer<typeof bulkClaimSchema>;
function actionsFor(request: Request): RecurringClaimAction[] {
  return request.kind === "PASS_LEVEL" ? request.levels.map(level => ({ kind: request.kind, policyId: request.policyId, track: request.track, level }))
    : request.awardIds.map(awardId => ({ kind: request.kind, awardId }));
}
type MemberRow = { member_index: number; action_json: string; status: string; receipt_json: string | null; error_code: string | null };
export async function readBulkClaim(db: D1Database, accountId: string, bulkId: string) {
  const row = await db.prepare(`SELECT b.id,b.request_json,b.created_at,
    (SELECT json_group_array(json_object('member_index',m.member_index,'action_json',m.action_json,'status',m.status,'receipt_json',m.receipt_json,'error_code',m.error_code))
     FROM recurring_bulk_members m WHERE m.bulk_id=b.id) AS members FROM recurring_bulk_requests b WHERE b.id=? AND b.account_id=?`)
    .bind(bulkId, accountId).first<{ id: string; request_json: string; created_at: number; members: string }>();
  if (!row) throw new ActivityError(403, "bulk_claim_not_found");
  const members = (JSON.parse(row.members) as MemberRow[]).sort((a, b) => a.member_index - b.member_index);
  const request = bulkClaimSchema.safeParse(JSON.parse(row.request_json));
  if (!request.success) throw new ActivityError(503, "bulk_result_incomplete");
  const expected = actionsFor(request.data);
  if (members.length !== expected.length || members.some((m, index) => m.member_index !== index
    || canonicalJson(JSON.parse(m.action_json)) !== canonicalJson(expected[index]!)
    || !["SUCCESS", "REPLAYED", "FAILED", "RETRY_PENDING"].includes(m.status)
    || ((m.status === "SUCCESS" || m.status === "REPLAYED") && !m.receipt_json))) {
    throw new ActivityError(503, "bulk_result_incomplete");
  }
  const successCount = members.filter(m => m.status === "SUCCESS" || m.status === "REPLAYED").length;
  const failureCount = members.filter(m => m.status === "FAILED").length, pendingCount = members.length - successCount - failureCount;
  const grantedCount = members.filter(m => m.status === "SUCCESS").length;
  const alreadyGrantedCount = members.filter(m => m.status === "REPLAYED").length;
  return { accepted: true, version: "bulk-claim-v1", bulkId, request: request.data, createdAt: row.created_at,
    resultContract: { version: "data.bulk-result.v1.20261021", coverage: "ORIGINAL_REQUEST", totalsScope: "SUCCESS_MEMBERS_ONLY",
      grantedCount, alreadyGrantedCount, unpaidCount: failureCount, unknownCount: pendingCount,
      // Cumulative for this bulk ID, never an incremental amount to add on replay.
      totalsIdentity: bulkId, completeMeans: "ALL_MEMBERS_TERMINAL_NOT_ALL_GRANTED" },
    status: pendingCount ? "PARTIAL" : "COMPLETE", successCount, failureCount, pendingCount, totalCount: members.length,
    items: members.map(m => ({ index: m.member_index, action: JSON.parse(m.action_json), status: m.status,
      outcome: m.status === "SUCCESS" ? "GRANTED" : m.status === "REPLAYED" ? "ALREADY_GRANTED" : m.status === "FAILED" ? "NOT_GRANTED" : "UNKNOWN",
      includeInAcquiredTotal: m.status === "SUCCESS",
      recovery: m.status === "RETRY_PENDING" ? "ORIGINAL_KEY_AND_BODY" : m.status === "FAILED" ? "NEW_CONFIRMATION_AFTER_REASON_RESOLVED" : "NONE",
      receipt: m.receipt_json ? JSON.parse(m.receipt_json) : null, errorCode: m.error_code })), refreshRequired: successCount > 0 };
}
async function existingRightReceipt(db: D1Database, accountId: string, action: RecurringClaimAction) {
  let right: string;
  if (action.kind === "PASS_LEVEL") {
    const d = await db.prepare("SELECT start_at FROM recurring_definitions WHERE id=? AND system='PASS50'").bind(action.policyId).first<{ start_at: number }>();
    if (!d) return null;
    right = `pass:${d.start_at}:${action.level}:${action.track}`;
  } else right = `quest:${action.awardId}`;
  return db.prepare("SELECT response_json FROM recurring_grants WHERE account_id=? AND right_key=?").bind(accountId, right).first<{ response_json: string }>();
}
async function recordFailure(db: D1Database, accountId: string, member: BulkMember, action: RecurringClaimAction, error: unknown) {
  const transient = !(error instanceof ActivityError) || error.status >= 500 || error.code === "recurring_state_changed"
    || (action.kind === "MAIL" && error.code === "quest_mail_pending");
  const code = error instanceof ActivityError ? error.code : "temporary_processing_failure";
  for (let attempt = 0; attempt < 4; attempt++) {
    try {
      // Fences a racing individual/worker/member grant before fixing a failure.
      const sequence = await recurringSequence(db, accountId);
      const receipt = await existingRightReceipt(db, accountId, action);
      const writes = [db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,?,?,'BULK_MEMBER_RESULT',?)")
        .bind(walletId("bulk_result"), accountId, sequence, Date.now())];
      writes.push(receipt ? bulkMemberSuccess(db, member, receipt.response_json, true)
        : db.prepare("UPDATE recurring_bulk_members SET status=?,error_code=?,attempts=attempts+1 WHERE bulk_id=? AND member_index=? AND status='RETRY_PENDING'")
          .bind(transient ? "RETRY_PENDING" : "FAILED", code, member.bulkId, member.index));
      await db.batch(writes); return;
    } catch (failure) { if (recurringRetryable(failure) && attempt < 3) continue; return; } // Retain durable pending on unavailable DB.
  }
}
export async function claimRecurringBulk(db: D1Database, accountId: string, input: Request, key: string) {
  // Validate also at the domain boundary; membership and ordering are immutable.
  const parsed = bulkClaimSchema.safeParse(input); if (!parsed.success) throw new ActivityError(400, "contract_violation");
  const request = parsed.data, hash = await sha256Hex(canonicalJson(request));
  let bulkId: string | undefined;
  for (let attempt = 0; attempt < 4; attempt++) {
    const existing = await db.prepare("SELECT id,request_hash FROM recurring_bulk_requests WHERE account_id=? AND idempotency_key=?")
      .bind(accountId, key).first<{ id: string; request_hash: string }>();
    if (existing) { if (existing.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict"); bulkId = existing.id; break; }
    const id = walletId("bulk");
    const actions = actionsFor(request);
    try {
      await db.batch([db.prepare("INSERT INTO recurring_bulk_requests(id,account_id,idempotency_key,request_hash,request_json,created_at) VALUES(?,?,?,?,?,?)")
        .bind(id, accountId, key, hash, JSON.stringify(request), Date.now()),
      ...actions.map((action, i) => db.prepare("INSERT INTO recurring_bulk_members(bulk_id,member_index,action_json,status) VALUES(?,?,?,'RETRY_PENDING')").bind(id, i, JSON.stringify(action)))]);
      bulkId = id; break;
    } catch (error) { if (recurringRetryable(error) && attempt < 3) continue; throw new ActivityError(503, "bulk_claim_unavailable"); }
  }
  if (!bulkId) throw new ActivityError(503, "bulk_claim_unavailable");
  const members = (await db.prepare("SELECT member_index,action_json FROM recurring_bulk_members WHERE bulk_id=? AND status='RETRY_PENDING' ORDER BY attempts,member_index LIMIT 10").bind(bulkId).all<MemberRow>()).results;
  const deadline = Date.now() + 2500;
  for (const row of members) {
    if (Date.now() >= deadline) break;
    const member = { bulkId, index: row.member_index }, action = JSON.parse(row.action_json) as RecurringClaimAction;
    try { await claimRecurringRight(db, accountId, action, undefined, member); }
    catch (error) { await recordFailure(db, accountId, member, action, error); }
  }
  return readBulkClaim(db, accountId, bulkId);
}
