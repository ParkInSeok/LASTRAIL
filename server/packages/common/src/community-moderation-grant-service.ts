import {PrincipalGrantService,grantCommandSchema} from './principal-grant-service';
import type {SupportDatabase} from './support-ticket-service';
export {PrincipalGrantError as CommunityGrantError} from './principal-grant-service';
export const communityModerationGrantVersion='community.moderation-grants.v1.20261008' as const;
export const communityModerationPermission='COMMUNITY_MODERATE' as const;
export const communityModerationGrantCommandSchema=grantCommandSchema(communityModerationPermission);
/** Independent permission state; support grants never imply community authority. */
export class CommunityModerationGrantService{
 private readonly grants:PrincipalGrantService;
 constructor(private readonly db:SupportDatabase,environment:'DEV'|'PRD'){this.grants=new PrincipalGrantService(db,environment,'COMMUNITY');}
 canManage(actor:string){return this.grants.canManage(actor);}
 read(actor:string,target:string){return this.grants.read(actor,target);}
 change(actor:string,target:string,key:string,value:unknown){return this.grants.change(actor,target,key,value);}
 async canModerate(actor:string){return !!await this.db.prepare("SELECT p.id FROM admin_principals p JOIN community_moderation_grants g ON g.principal_id=p.id WHERE p.id=? AND p.status='ACTIVE' AND p.role IN ('OWNER','OPERATOR') AND g.permission='COMMUNITY_MODERATE'").bind(actor).first();}
}