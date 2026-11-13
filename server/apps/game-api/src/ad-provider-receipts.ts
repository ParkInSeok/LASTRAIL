import {ActivityError} from './activity-policy';
export type AdReceiptConsumer='OFFLINE'|'COMBAT'|'AUGMENT';
export async function existingProviderReceipt(db:D1Database,transaction:string,consumer:AdReceiptConsumer,intent:string,account:string){
 const p=await db.prepare("SELECT consumer,intent_id,account_id FROM ad_provider_receipts WHERE provider='ADMOB' AND transaction_id=?")
  .bind(transaction).first<{consumer:string;intent_id:string;account_id:string}>();
 if(!p)return false;
 if(p.consumer!==consumer||p.intent_id!==intent||p.account_id!==account)throw new ActivityError(409,'ad_reward_state_conflict');
 return true;
}
/** Include in the consumer's atomic verification batch. No independent grant. */
export function providerReceiptStatement(db:D1Database, transaction:string, consumer:AdReceiptConsumer, intent:string, account:string) {
  return db.prepare(`INSERT INTO ad_provider_receipts(provider,transaction_id,consumer,intent_id,account_id)
    VALUES('ADMOB',?,?,?,CASE WHEN
      NOT EXISTS(SELECT 1 FROM ad_reward_transactions WHERE provider='ADMOB' AND provider_transaction_id=? AND status IN ('VERIFIED','GRANTED'))
      AND NOT EXISTS(SELECT 1 FROM offline_ad_intents WHERE provider_transaction_id=? AND NOT (?='OFFLINE' AND id=?))
      AND NOT EXISTS(SELECT 1 FROM combat_ad_intents WHERE provider_transaction_id=? AND NOT (?='COMBAT' AND id=?))
      THEN ? ELSE NULL END)`)
    .bind(transaction,consumer,intent,transaction,transaction,consumer,intent,transaction,consumer,intent,account);
}
