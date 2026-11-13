import {sha256Hex} from '@lastrail/common';
import {ProbabilityDisclosureError as Failure,publicDisclosureSchema,disclosureProjectionSchema,disclosureId} from '../../../packages/common/src/probability-disclosure-policy';
type StoredDisclosure={disclosure_id:string;payload_json:string;payload_digest:string};
async function validateDisclosure(row:StoredDisclosure,environment:string){
  try{
    if(row.payload_json.length>16384||await sha256Hex(row.payload_json)!==row.payload_digest)throw new Error('checksum');
    const payload=publicDisclosureSchema.parse(JSON.parse(row.payload_json));
    const projection={environment:payload.environment,consumer:payload.consumer,policyVersion:payload.policyVersion,scope:payload.scope,sourceDigest:payload.sourceDigest,publicSchemaVersion:payload.publicSchemaVersion,distribution:payload.distribution,...(payload.publicSchemaVersion===2?{sourceKind:payload.sourceKind}:{})};
    if(payload.environment!==environment||payload.disclosureId!==row.disclosure_id||await disclosureId(disclosureProjectionSchema.parse(projection))!==row.disclosure_id)throw new Error('identity');
    return payload;
  }catch{throw new Failure(503,'probability_disclosure_storage_invalid');}
}
export async function readProbabilityDisclosure(db:D1Database,environment:string,id:string){
  const row=await db.prepare('SELECT disclosure_id,payload_json,payload_digest FROM probability_disclosures WHERE environment=? AND disclosure_id=?')
    .bind(environment,id).first<StoredDisclosure>();
  return row?validateDisclosure(row,environment):null;
}
/** A bounded page of approved history, not a current release selection. */
export async function listProbabilityDisclosures(db:D1Database,environment:'DEV'|'PRD',after:string,limit:number){
  const rows=await db.prepare('SELECT disclosure_id,payload_json,payload_digest FROM probability_disclosures WHERE environment=? AND disclosure_id>? ORDER BY disclosure_id LIMIT ?')
    .bind(environment,after,limit).all<StoredDisclosure>();
  // Validate the lookahead too: a corrupt row must not create a successful next-page promise.
  return Promise.all(rows.results.map(row=>validateDisclosure(row,environment)));
}
