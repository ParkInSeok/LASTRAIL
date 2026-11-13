import {it,expect} from 'vitest';
import {canonicalJson} from '@lastrail/common';
import {setup} from '../helpers/departure-fixture';
import {BPhysicalPublicationStore} from '../../apps/game-api/src/combat-b-physical-publication';
async function fixture(){
 const f=await setup();
 const key=await crypto.subtle.importKey('raw',new TextEncoder().encode('LOCAL_SYNTHETIC_PUBLICATION_KEY_ONLY'),{name:'HMAC',hash:'SHA-256'},false,['sign','verify']);
 const context={accountId:'a',runId:'local-run',operationPreviewId:'local-preview'};
 const owned={accountId:'a',runId:'local-run',assignmentHash:'a'.repeat(64)};
 const body={version:'combat.b-physical-publication.v1.20261020',environment:'DEV',publicationId:'local-publication',keyId:'local-key',
  authoring:{context,assignmentHash:owned.assignmentHash},measurements:[]};
 const sign=async(value:unknown)=>{
  const bodyJson=canonicalJson(value as never);
  const signature=Array.from(new Uint8Array(await crypto.subtle.sign('HMAC',key,new TextEncoder().encode(bodyJson))),b=>b.toString(16).padStart(2,'0')).join('');
  return {bodyJson,signature};
 };
 const store=new BPhysicalPublicationStore(f.env.DB,'DEV',{keyId:'local-key',key},'local-publication');
 return {...f,context,owned,body,sign,store,key};
}
it('authenticates immutable original bytes and recovers the same receipt without republishing',async()=>{
 const f=await fixture();try{
  const envelope=await f.sign(f.body),result=await f.store.publish(envelope);
  expect(result).toMatchObject({publicationId:'local-publication',executionApproved:false});
  expect(await f.store.publish(envelope)).toEqual(result);
  const captured=await f.store.capture(f.context as never,f.owned as never);
  expect(captured.authoring).toEqual(f.body.authoring);
  await f.env.DB.batch([...captured.assertions.geometry,f.env.DB.prepare('DELETE FROM departure_write_guard')]);
  const altered=await f.sign({...f.body,authoring:{...f.body.authoring,extra:true}});
  await expect(f.store.publish(altered)).rejects.toThrow('publication_conflict');
 }finally{f.sql.close();}
});
it('bounds UTF-8 source bytes before storing oversized signed data',async()=>{
 const f=await fixture();try{
  const large=await f.sign({...f.body,authoring:{...f.body.authoring,note:'한'.repeat(200000)}});
  await expect(f.store.publish(large)).rejects.toThrow('too_large');
  expect(f.scalar('SELECT count(*) FROM b_physical_publications')).toBe(0);
 }finally{f.sql.close();}
});
it('recovers committed bytes after a lost acknowledgement and converges concurrent publishes',async()=>{
 const f=await fixture();try{
  let dropped=false;
  const db={prepare:(sql:string)=>{
   const statement=f.env.DB.prepare(sql);
   if(!sql.startsWith('INSERT INTO b_physical_publications'))return statement;
   return {bind:(...values:unknown[])=>{
    const bound=statement.bind(...values);
    return {run:async()=>{await bound.run();if(!dropped){dropped=true;throw new Error('LOCAL_ACK_LOST');}}};
   }};
  }} as unknown as D1Database;
  const store=new BPhysicalPublicationStore(db,'DEV',{keyId:'local-key',key:f.key},'local-publication');
  const e=await f.sign(f.body);
  const results=await Promise.all([store.publish(e),f.store.publish(e)]);
  expect(results[0]).toEqual(results[1]);expect(dropped).toBe(true);
  expect(f.scalar('SELECT count(*) FROM b_physical_publications')).toBe(1);
 }finally{f.sql.close();}
});
it('rejects tampering, wrong signing key/environment and mismatched owned scope',async()=>{
 const f=await fixture();try{
  const e=await f.sign(f.body);
  await expect(f.store.publish({...e,signature:'0'.repeat(64)})).rejects.toThrow('signature_invalid');
  await expect(f.store.publish(await f.sign({...f.body,keyId:'other'}))).rejects.toThrow('trust_conflict');
  await expect(f.store.publish(await f.sign({...f.body,environment:'PRD'}))).rejects.toThrow('trust_conflict');
  await f.store.publish(e);
  await expect(f.store.capture({...f.context,runId:'other'} as never,f.owned as never)).rejects.toThrow('scope_conflict');
  await expect(f.store.capture(f.context as never,{...f.owned,assignmentHash:'b'.repeat(64)} as never)).rejects.toThrow('scope_conflict');
 }finally{f.sql.close();}
});
it('fences withdrawal between capture and departure and refuses replay after revocation',async()=>{
 const f=await fixture();try{
  await f.store.publish(await f.sign(f.body));
  const captured=await f.store.capture(f.context as never,f.owned as never);
  await f.store.revoke('withdraw-local-draft');
  await expect(f.env.DB.batch(captured.assertions.execution)).rejects.toThrow();
  await expect(f.store.capture(f.context as never,f.owned as never)).rejects.toThrow('revoked');
  await expect(f.store.publish(await f.sign(f.body))).rejects.toThrow('revoked');
 }finally{f.sql.close();}
});
