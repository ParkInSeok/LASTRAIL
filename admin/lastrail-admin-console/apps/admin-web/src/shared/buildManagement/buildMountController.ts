import type {BootstrapResponse} from '@lastrail/admin-contracts';import {unityBuildRequestSchema} from './unityBuildContract';import type {UnityBuildManagement} from './UnityBuildManagement';import type {BuildInputDraft} from './BuildInputDraft';
export function buildMountScope(bootstrap:BootstrapResponse|undefined,authenticationBlocked:boolean){if(authenticationBlocked||!bootstrap?.admin.permissions.includes('SERVER_DEPLOYMENT_READ'))return null;const {admin,environment}=bootstrap;return {actor:admin.id,environment:environment==='prd'?'PRD' as const:'DEV' as const,key:JSON.stringify([admin.id,admin.role,environment,[...admin.permissions].sort()])};}
export function parseBuildLogOffset(text:string){if(!/^(0|[1-9]\d*)$/.test(text)||!Number.isSafeInteger(Number(text)))throw new Error('로그 offset은 0 이상의 안전한 정수여야 합니다.');return Number(text);}
/** Connects UI intents to existing owners; no build, permission or request recovery rules are duplicated. */
export function createBuildMountController(model:UnityBuildManagement,draft:BuildInputDraft){return {
 open:async()=>{await model.capabilities();if(model.snapshot().phase==='active')await model.list();},
 refresh:async()=>{await model.capabilities(true);if(model.snapshot().phase==='active')await model.list('',true);},
 select:(id:string,refresh=false)=>model.open(id,refresh),
 next:()=>model.snapshot().list?.nextAfter?model.list(model.snapshot().list!.nextAfter!):Promise.resolve(),
 logs:(offset:string)=>{const id=model.snapshot().selected;if(!id)throw new Error('작업을 먼저 선택하세요.');return model.logs(id,parseBuildLogOffset(offset));},
 artifacts:()=>{const id=model.snapshot().selected;if(!id)throw new Error('작업을 먼저 선택하세요.');return model.artifacts(id);},
 request:(key:string)=>{const i=draft.snapshot().input;if(!/^[1-9]\d*$/.test(i.buildNumber))return Promise.reject(new Error('설치 build 번호는 양의 정수여야 합니다.'));const input=unityBuildRequestSchema.parse({environment:model.environment,profileId:i.profile,platform:i.platform,commit:i.commit,contentVersion:i.contentVersion,appVersion:i.appVersion,buildNumber:Number(i.buildNumber),note:i.note});return model.request(input,key);},
 cancel:(reason:string,key:string)=>{const job=model.snapshot().detail?.job;if(!job)return Promise.reject(new Error('작업 원본을 먼저 선택하세요.'));return model.cancel(job.jobId,{expectedUpdatedAt:job.updatedAt,reason},key);},
 recover:()=>model.recover()
 };}
