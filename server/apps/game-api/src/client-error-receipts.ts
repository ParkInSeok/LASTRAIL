import {ActivityError} from './activity-policy';
import {clientErrorV2} from './client-error-contract';

/** Receipt facts outlive deletable diagnostic rows; they confer no gameplay authority. */
export class ClientErrorReceipts {
 constructor(private readonly db:D1Database){}
 async read(accountId:string,key:string,ingestId:string){
  const r=await this.db.prepare('SELECT ingest_id,version,accepted_event_count,received_at,environment FROM client_error_receipts WHERE account_id=? AND idempotency_key=?')
   .bind(accountId,key).first<{ingest_id:string;version:string;accepted_event_count:number;received_at:number;environment:string}>();
  if(!r||r.ingest_id!==ingestId||r.version!==clientErrorV2||!Number.isSafeInteger(r.accepted_event_count)||r.accepted_event_count<1||r.accepted_event_count>25
   ||!Number.isSafeInteger(r.received_at)||!['DEV','PRD'].includes(r.environment))throw new ActivityError(503,'client_error_ingest_receipt_unavailable');
  return r;
 }
 guard(accountId:string,windowStart:number,limit:number){return this.db.prepare(`INSERT INTO client_error_ingest_write_guard(valid) SELECT CASE WHEN
   EXISTS(SELECT 1 FROM accounts WHERE id=? AND status='ACTIVE') AND
   (SELECT count(*) FROM idempotency_records WHERE account_id=? AND operation='client_error_ingest' AND created_at>=?)<?
   THEN 1 ELSE 0 END`).bind(accountId,accountId,windowStart,limit);}
 save(accountId:string,key:string,ingestId:string,count:number,now:number,environment:string){return this.db.prepare(`INSERT INTO client_error_receipts
   (account_id,idempotency_key,ingest_id,version,accepted_event_count,received_at,environment) VALUES(?,?,?,?,?,?,?)`)
   .bind(accountId,key,ingestId,clientErrorV2,count,now,environment);}
}
