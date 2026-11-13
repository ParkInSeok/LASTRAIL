import type initSqlJs from 'sql.js';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {CommunityConsentService} from '../../apps/game-api/src/community-consent';
/** Isolated synthetic source only. Never imported by app code, migrations, publication, or operating seeds. */
export async function installSyntheticCommunityConsent(sql:initSqlJs.Database,db:D1Database,revision=1,acceptA=true,activate=true){
 const documents={serviceTerms:{id:'synthetic_service_terms',version:'fixture_v'+revision,contentDigest:await sha256Hex('SYNTHETIC TEST SERVICE TERMS '+revision)},communityRules:{id:'synthetic_community_rules',version:'fixture_v'+revision,contentDigest:await sha256Hex('SYNTHETIC TEST COMMUNITY RULES '+revision)}};
 for(const environment of ['DEV','PRD'] as const){for(const [key,kind,text] of [['serviceTerms','SERVICE_TERMS','SYNTHETIC TEST SERVICE TERMS '],['communityRules','COMMUNITY_RULES','SYNTHETIC TEST COMMUNITY RULES ']] as const){const d=documents[key];sql.run('INSERT OR IGNORE INTO community_policy_documents VALUES(?,?,?,?,?,?,?,?)',[environment,'ko-KR',d.id,d.version,kind,d.contentDigest,text+revision,'PUBLISHED']);}
  const manifest={environment,locale:'ko-KR',policyRevision:revision,documents};sql.run('INSERT OR IGNORE INTO community_policy_pairs VALUES(?,?,?,?,?,?)',[environment,'ko-KR',revision,canonicalJson(documents),await sha256Hex(canonicalJson(manifest)),'PUBLISHED']);if(activate)sql.run("INSERT INTO community_policy_current VALUES(?, 'ko-KR',?) ON CONFLICT(environment,locale) DO UPDATE SET policy_revision=excluded.policy_revision",[environment,revision]);
  if(activate&&acceptA){const service=new CommunityConsentService(db,environment,'ko-KR');await service.accept('a',{commandId:'synthetic_accept_'+environment+'_'+revision,...(await service.current('a')).policy});}
 }
}