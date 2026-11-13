import {PrincipalGrantService,grantCommandSchema} from './principal-grant-service';
import type {SupportDatabase} from './support-ticket-service';
export {PrincipalGrantError as SupportGrantError} from './principal-grant-service';
export const supportGrantContractVersion='support-grant-management-v1' as const;
export const supportGrantCommandSchema=grantCommandSchema('SUPPORT_TICKET_PROCESS');
/** Legacy contract facade; current state/CAS/idempotency rules have one shared owner. */
export class SupportGrantService{
 private readonly grants:PrincipalGrantService;
 constructor(db:SupportDatabase,environment:'DEV'|'PRD'){this.grants=new PrincipalGrantService(db,environment,'SUPPORT');}
 canManage(actor:string){return this.grants.canManage(actor);}
 read(actor:string,target:string){return this.grants.read(actor,target);}
 change(actor:string,target:string,key:string,value:unknown){return this.grants.change(actor,target,key,value);}
}