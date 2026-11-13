export type BulkMember = { bulkId: string; index: number };
export function bulkMemberGuard(db: D1Database, accountId: string, member: BulkMember) {
  return db.prepare(`INSERT INTO recurring_guard(valid) SELECT CASE WHEN EXISTS(
    SELECT 1 FROM recurring_bulk_members m JOIN recurring_bulk_requests b ON b.id=m.bulk_id
    WHERE b.account_id=? AND m.bulk_id=? AND m.member_index=? AND m.status='RETRY_PENDING') THEN 1 ELSE 0 END`)
    .bind(accountId, member.bulkId, member.index);
}
export function bulkMemberSuccess(db: D1Database, member: BulkMember, receipt: string, replayed: boolean) {
  return db.prepare("UPDATE recurring_bulk_members SET status=?,receipt_json=?,error_code=NULL WHERE bulk_id=? AND member_index=? AND status='RETRY_PENDING'")
    .bind(replayed ? "REPLAYED" : "SUCCESS", receipt, member.bulkId, member.index);
}
