import {createPublishedOfflineForTest as createOfflineAdIntent} from './published-disclosure-fixture';

/** Legacy SSV/settlement tests explicitly model an ad started at creation time.
 * Synthetic fixture only. Actual start HTTP/clock/replay/races are tested separately
 * in ad-runtime-policy.test.ts without this helper. */
export async function seedStartedAdForTest(db:D1Database,consumer:'OFFLINE'|'COMBAT',intent:string) {
 const table=consumer==='OFFLINE'?'offline_ad_intents':'combat_ad_intents';
 await db.prepare(`INSERT INTO ad_start_approvals
 SELECT ?,i.id,i.account_id,i.environment,'TEST_START_'||i.id,p.revision,i.created_at,i.expires_at
 FROM ${table} i JOIN ad_runtime_policies p ON p.environment=i.environment AND p.ads_enabled=1
 WHERE i.id=? AND i.origin='AD_REQUEST' AND i.status='AD_PENDING'
 AND NOT EXISTS(SELECT 1 FROM ad_start_approvals s WHERE s.consumer=? AND s.intent_id=i.id)`)
 .bind(consumer,intent,consumer).run();
}
export async function createStartedOfflineAdIntent(...args:Parameters<typeof createOfflineAdIntent>) {
 const result=await createOfflineAdIntent(...args);
 await seedStartedAdForTest(args[0],'OFFLINE',result.intentId);
 return result;
}
