import {eventDraftSaveSchema,eventDraftSavedSchema,eventDraftListSchema,type EventDraftSave} from '../liveops/eventDraft/eventDraftContract';
import {rewardDraftCommandSchema,rewardDraftDetailSchema,rewardDraftReceiptSchema,type RewardDraftCommand} from '../rewards/rewardDraftContract';
import {outagePreviewInputSchema,outagePreviewSchema,type OutagePreviewInput} from '../liveops/outagePreview/outagePreviewContract';
import {cosmeticReplacementTableSchema} from '../liveops/cosmeticReplacement/cosmeticReplacementContract';
import {unityBuildCapabilitiesSchema,unityBuildListSchema,unityBuildDetailSchema,unityBuildRequestSchema,unityBuildCancelSchema,unityBuildReceiptSchema,unityBuildLogsSchema,unityBuildArtifactsSchema,type UnityBuildRequest,type UnityBuildCancel} from '../buildManagement/unityBuildContract';
import {eventDefinitionDraftSchema,eventDefinitionPreviewSchema,type EventDefinitionDraft} from '../liveops/eventPreview/eventPreviewContract';
import {liveOpsCapabilitiesSchema,liveOpsNoticeListSchema,liveOpsNoticeDetailSchema,liveOpsNoticeHistorySchema,liveOpsNoticeCommandSchema,liveOpsNoticeReviewReceiptSchema,liveOpsNoticeCommandReceiptSchema,type LiveOpsNoticeCommand} from '../liveops/noticeManagement/noticeManagementContract';
import {rewardProposalSchema,rewardTrackingCapabilitiesSchema,rewardReviewPreviewSchema,type RewardProposal} from '../rewards/rewardPreviewContract';
import {opsEventId,opsRequestKey,opsInvestigationReadSchema,opsInvestigationCommandSchema,assertOpsReceipt,opsGroupsSchema,type OpsInvestigationCommand} from "../opsErrorLogs/opsInvestigationContract";
import {COMMENT_LOCK_VERSION,commentLockCommand,assertCommentLockReceipt,type CommentLockBody} from '../community/commentLockContract';
import {MODERATION_VERSION,moderationQuery,moderationPath,reviewCommand,moderateCommand,parseModerationRead,parseModerationReceipt,type ReadKind,type Resource,type ModerationBody,type ModerationQuery} from '../community/moderationContract';
import {COMMUNITY_GRANT_VERSION,communityPrincipalId,communityGrantCommandSchema,assertCommunityGrantState,assertCommunityGrantReceipt,type CommunityGrantCommand} from '../community/communityGrantContract';
import { ticketList, ticketDetail } from "../support/tickets";
import {parseProbabilitySource,probabilityReceiptSchema,type ProbabilityTarget,type ProbabilityPublish} from "../operations/probabilityDisclosure";
import {noticeListSchema,noticeDetailSchema,noticeReceiptSchema,type NoticeCommand} from "../operations/noticeContract";
import {maintenanceListSchema,maintenanceDetailSchema,maintenancePreviewSchema,maintenanceReceiptSchema,assertMaintenanceEnvironment,type MaintenanceEnvironment,type MaintenanceSave} from "../operations/maintenanceContract";
import {mapAdPolicy,mapAdPolicyWrite,type PolicyEnvironment,type AdPolicyCommand} from "../operations/adPolicyContract";
import { mapPlayerActivities } from "../support/playerActivities";
import { mapDeployment, type DeploymentService } from "../release/serverDeployment";
import { buildRegistryCommand, mapClientRegistry, type ClientRegistry, type RegistryEditContext, type RegistryInput, type RegistryTarget } from "../release/clientReleaseRegistry";
import {
  mapDashboardLaunchSummaryResponse
} from "../dashboard/launchSummaryContract";

import {
  mapGachaBannerListResponse,
  mapGachaPityResponse,
  mapGachaPullBatchDetailResponse,
  mapGachaPullBatchListResponse
} from "../gacha/gachaContract";

import {
  mapOfflineRewardDetailResponse,
  mapOfflineRewardListResponse,
  type OfflineRewardReadFiltersPreview
} from "../offlineRewards/offlineRewardContract";

import { mapNarrativeCoverageResponse } from "../narrative/narrativeCoverageContract";

import {
  mapOpsErrorLogDetailResponse,
  mapOpsErrorLogListResponse,
  mapOpsErrorLogSummaryResponse,
  type OpsErrorLogReadFiltersPreview
} from "../opsErrorLogs/opsErrorLogContract";

import { mapPlayerLaunchEvidence } from "../players/playerLaunchEvidenceContract";
import { mapPlayerDetailResponse } from "../players/playerDetailContract";
import { mapRunDetailResponse } from "../runs/runDetailContract";
import { mapRunVersions } from "../runs/runVersions";
import { mapRunLaunchEvidence } from "../runs/runLaunchEvidenceContract";
import { mapCurrentVersion, mapVersionExportResult, type VersionExportRequest } from "../release/releaseManagementContract";

import {
  mapStoryProgressDetailResponse,
  mapStoryProgressListResponse,
  type StoryProgressFiltersPreview
} from "../storyProgress/storyProgressContract";

import { mapSupportAccountSummaryResponse } from "../support/supportContract";
import {processingSchema,processingReceiptSchema,processingCommandSchema,grantStateSchema,grantCommandSchema,assertGrantReceipt,type ProcessingCommand,type GrantCommand} from "../support/supportProcessing";

import {
  AuditLogsResponseSchema,
  BootstrapResponseSchema,
  CreatePlayerNoteRequestSchema,
  CreatePlayerNoteResponseSchema,
  IntegrityEventsResponseSchema,
  PlayerNotesResponseSchema,
  PlayerSearchResponseSchema,
  RunSearchResponseSchema,
  SuspiciousPlayerDetailSchema,
  SuspiciousPlayersResponseSchema
} from "@lastrail/admin-contracts";

type AdminApiErrorOptions = {
  status: number;
  path: string;
  errorCode?: string;
  upstreamMessage?: string;
  current?: unknown;
};

export class AdminApiError extends Error {
  readonly name = "AdminApiError";
  readonly status: number;
  readonly path: string;
  readonly errorCode?: string;
  readonly upstreamMessage?: string;
  readonly current?: unknown;

  constructor(options: AdminApiErrorOptions) {
    const suffix = options.errorCode ? ` (${options.errorCode})` : "";
    super(`Admin request failed with ${options.status}${suffix}`);
    this.status = options.status;
    this.path = options.path;
    this.errorCode = options.errorCode;
    this.upstreamMessage = options.upstreamMessage;
    this.current = options.current;
  }
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function errorDetailsFrom(payload: unknown): Pick<AdminApiErrorOptions, "errorCode" | "upstreamMessage"> {
  const root = record(payload);
  const nested = record(root?.error);

  if (nested) {
    return {
      errorCode: typeof nested.code === "string" ? nested.code : undefined,
      upstreamMessage: typeof nested.message === "string" ? nested.message : undefined
    };
  }

  return {
    errorCode: typeof root?.error === "string" ? root.error : typeof root?.errorCode === "string" ? root.errorCode : undefined,
    upstreamMessage: typeof root?.message === "string" ? root.message : undefined
  };
}

async function adminApiErrorFrom(response: Response, path: string): Promise<AdminApiError> {
  try {
    const payload = await response.clone().json();
    const details = errorDetailsFrom(payload);
    return new AdminApiError({ status: response.status, path, ...details, current: record(payload)?.current });
  } catch {
    return new AdminApiError({ status: response.status, path });
  }
}

async function readJson(path: string): Promise<unknown> {
  const response = await fetch(path, {
    ...(/^\/api\/(?:ops\/error-logs|liveops|reward-tracking|unity-builds|events)(?:\/|\?|$)/.test(path)?{cache:"no-store" as const}:{}),
    headers: {
      accept: "application/json"
    }
  });

  if (!response.ok) {
    throw await adminApiErrorFrom(response, path);
  }

  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.toLowerCase().includes("application/json")) {
    throw new AdminApiError({
      status: response.status,
      path,
      errorCode: "ADMIN_API_NON_JSON_RESPONSE",
      upstreamMessage: "Admin API returned a non-JSON response."
    });
  }

  return response.json();
}

async function writeJson(path: string, body: unknown, idempotencyKey?: string): Promise<unknown> {
  const response = await fetch(path, {
    method: "POST",
    ...(/^\/api\/(?:liveops|reward-tracking|unity-builds|events)(?:\/|\?|$)/.test(path)?{cache:"no-store" as const}:{}),
    headers: {
      accept: "application/json",
      "content-type": "application/json",
      ...(idempotencyKey ? {"Idempotency-Key":idempotencyKey} : {})
    },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    throw await adminApiErrorFrom(response, path);
  }

  return response.json();
}

function storyProgressQuery(filters: Partial<StoryProgressFiltersPreview>): string {
  return queryString(filters);
}

function offlineRewardQuery(filters: Partial<OfflineRewardReadFiltersPreview>): string {
  return queryString(filters);
}

function opsErrorLogQuery(filters: Partial<OpsErrorLogReadFiltersPreview>): string {
  return queryString(filters);
}

function gachaQuery(filters: Record<string, string | undefined>): string {
  return queryString(filters);
}

function queryString(filters: Record<string, string | undefined>): string {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value) params.set(key, value);
  });
  const query = params.toString();
  return query ? `?${query}` : "";
}

export const adminApi = {
  async unityBuildCapabilities(){return unityBuildCapabilitiesSchema.parse(await readJson('/api/unity-builds/capabilities'));},
  async unityBuilds(after=''){return unityBuildListSchema.parse(await readJson('/api/unity-builds?limit=20'+(after?'&after='+encodeURIComponent(after):'')));},
  async unityBuild(id:string){const r=unityBuildDetailSchema.parse(await readJson('/api/unity-builds/'+encodeURIComponent(id)));if(r.job.jobId!==id)throw new Error('빌드 작업 응답 불일치');return r;},
  async requestUnityBuild(input:UnityBuildRequest,key:string){return unityBuildReceiptSchema.parse(await writeJson('/api/unity-builds',unityBuildRequestSchema.parse(input),key));},
  async cancelUnityBuild(id:string,input:UnityBuildCancel,key:string){const r=unityBuildReceiptSchema.parse(await writeJson('/api/unity-builds/'+encodeURIComponent(id)+'/cancel',unityBuildCancelSchema.parse(input),key));if(r.job.jobId!==id)throw new Error('빌드 취소 응답 불일치');return r;},
  async unityBuildLogs(id:string,after=0){const r=unityBuildLogsSchema.parse(await readJson('/api/unity-builds/'+encodeURIComponent(id)+'/logs?after='+encodeURIComponent(after)));if(r.jobId!==id)throw new Error('빌드 로그 응답 불일치');return r;},
  async unityBuildArtifacts(id:string){const r=unityBuildArtifactsSchema.parse(await readJson('/api/unity-builds/'+encodeURIComponent(id)+'/artifacts'));if(r.jobId!==id)throw new Error('빌드 산출물 응답 불일치');return r;},
  async eventDrafts(after='',limit=20){const query=new URLSearchParams({limit:String(limit)});if(after)query.set('after',after);return eventDraftListSchema.parse(await readJson('/api/events/definitions?'+query));},
  async eventDraft(roundId:string){const result=eventDraftSavedSchema.parse(await readJson('/api/events/definitions/'+encodeURIComponent(roundId)));if(result.roundId!==roundId)throw new Error('이벤트 초안 응답 불일치');return result;},
  async saveEventDraft(roundId:string,input:EventDraftSave,key:string){const body=eventDraftSaveSchema.parse(input);if(body.definition.roundId!==roundId)throw new Error('이벤트 회차 불일치');return eventDraftSavedSchema.parse(await writeJson('/api/events/definitions/'+encodeURIComponent(roundId)+'/commands',body,key));},
  async rewardDraft(batchId:string){const result=rewardDraftDetailSchema.parse(await readJson('/api/reward-tracking/drafts/'+encodeURIComponent(batchId)));if(result.batchId!==batchId)throw new Error('보상 초안 응답 불일치');return result;},
  async saveRewardDraft(batchId:string,input:RewardDraftCommand,key:string){const body=rewardDraftCommandSchema.parse(input);if(body.proposal.batchId!==batchId)throw new Error('보상 배치 불일치');return rewardDraftReceiptSchema.parse(await writeJson('/api/reward-tracking/drafts/'+encodeURIComponent(batchId)+'/commands',body,key));},
  async eventOutagePreview(input:OutagePreviewInput){return outagePreviewSchema.parse(await writeJson('/api/events/outage-preview',outagePreviewInputSchema.parse(input)));},
  async eventCosmeticReplacementTable(){return cosmeticReplacementTableSchema.parse(await readJson('/api/events/cosmetic-replacement-table'));},
  async eventDefinitionPreview(input:EventDefinitionDraft){return eventDefinitionPreviewSchema.parse(await writeJson('/api/events/definition-preview',eventDefinitionDraftSchema.parse(input)));},
  async liveOpsCapabilities(){return liveOpsCapabilitiesSchema.parse(await readJson('/api/liveops/capabilities'));},
  async liveOpsNotices(after=''){return liveOpsNoticeListSchema.parse(await readJson('/api/liveops/notices?limit=20'+(after?'&after='+encodeURIComponent(after):'')));},
  async liveOpsNotice(id:string){const r=liveOpsNoticeDetailSchema.parse(await readJson('/api/liveops/notices/'+encodeURIComponent(id)));if(r.notice.noticeId!==id)throw new Error('운영 공지 응답 불일치');return r;},
  async liveOpsNoticeHistory(id:string,before?:number){const r=liveOpsNoticeHistorySchema.parse(await readJson('/api/liveops/notices/'+encodeURIComponent(id)+'/history?limit=20'+(before!==undefined?'&before='+encodeURIComponent(before):'')));if(r.noticeId!==id)throw new Error('운영 공지 이력 응답 불일치');return r;},
  async liveOpsNoticeCommand(id:string,input:LiveOpsNoticeCommand,key:string){const body=liveOpsNoticeCommandSchema.parse(input),raw=await writeJson('/api/liveops/notices/'+encodeURIComponent(id)+'/commands',body,key);const result=body.kind==='REVIEW'?liveOpsNoticeReviewReceiptSchema.parse(raw):liveOpsNoticeCommandReceiptSchema.parse(raw);if(result.notice.noticeId!==id)throw new Error('운영 공지 명령 응답 불일치');return result;},
  async rewardTrackingCapabilities(){return rewardTrackingCapabilitiesSchema.parse(await readJson('/api/reward-tracking/capabilities'));},
  async rewardReviewPreview(input:RewardProposal){return rewardReviewPreviewSchema.parse(await writeJson('/api/reward-tracking/review-preview',rewardProposalSchema.parse(input)));},
 async communityCommentLock(environment:'DEV'|'PRD',id:string,body:CommentLockBody,key:string){
  communityPrincipalId.parse(id);const command=commentLockCommand.parse(body);if(!/^[\x21-\x7e]{1,128}$/.test(key))throw new Error('Invalid idempotency key');
  const path=`/api/community/contents/${id}/comment-lock`;const response=await fetch(path,{method:'POST',cache:'no-store',headers:{accept:'application/json','content-type':'application/json','X-Community-Comment-Lock-Version':COMMENT_LOCK_VERSION,'Idempotency-Key':key},body:JSON.stringify(command)});if(!response.ok)throw await adminApiErrorFrom(response,path);return assertCommentLockReceipt(await response.json(),environment,id,command);
 },
 async communityModerationRead(environment:'DEV'|'PRD',kind:ReadKind,id?:string,query?:ModerationQuery){
  const path=moderationPath(kind,id);const q=kind==='LIST'?moderationQuery.parse(query??{}):{};
  const params=new URLSearchParams(Object.entries(q).map(([k,v])=>[k,String(v)]));const url=path+(params.size?'?'+params:'');
  const response=await fetch(url,{cache:'no-store',headers:{accept:'application/json','X-Community-Moderation-Version':MODERATION_VERSION}});if(!response.ok)throw await adminApiErrorFrom(response,path);
  return parseModerationRead(kind,await response.json(),environment,id);
 },
 async communityModerationWrite(environment:'DEV'|'PRD',kind:Resource,id:string,body:ModerationBody,key:string){
  const path=moderationPath(kind,id,true),command=kind==='REPORT'?reviewCommand.parse(body):moderateCommand.parse(body);if(!/^[\x21-\x7e]{1,128}$/.test(key))throw new Error('Invalid idempotency key');
  const response=await fetch(path,{method:'POST',cache:'no-store',headers:{accept:'application/json','content-type':'application/json','X-Community-Moderation-Version':MODERATION_VERSION,'Idempotency-Key':key},body:JSON.stringify(command)});if(!response.ok)throw await adminApiErrorFrom(response,path);
  return parseModerationReceipt(await response.json(),environment,kind,id,command);
 },
  async communityModeratorGrant(environment:'DEV'|'PRD',id:string) {
    communityPrincipalId.parse(id);const path=`/api/community/moderator-grants/${encodeURIComponent(id)}`;
    const response=await fetch(path,{cache:'no-store',headers:{accept:'application/json','X-Community-Moderation-Grant-Version':COMMUNITY_GRANT_VERSION}});
    if(!response.ok)throw await adminApiErrorFrom(response,path);
    return assertCommunityGrantState(await response.json(),environment,id);
  },
  async changeCommunityModeratorGrant(environment:'DEV'|'PRD',id:string,body:CommunityGrantCommand,key:string) {
    communityPrincipalId.parse(id);const command=communityGrantCommandSchema.parse(body);
    if(!/^[\x21-\x7e]{1,128}$/.test(key))throw new Error('Invalid idempotency key');
    const path=`/api/community/moderator-grants/${encodeURIComponent(id)}`;
    const response=await fetch(path,{method:'POST',cache:'no-store',headers:{accept:'application/json','content-type':'application/json','X-Community-Moderation-Grant-Version':COMMUNITY_GRANT_VERSION,'Idempotency-Key':key},body:JSON.stringify(command)});
    if(!response.ok)throw await adminApiErrorFrom(response,path);
    return assertCommunityGrantReceipt(await response.json(),environment,id,command);
  },
  async publishProbability(body:ProbabilityPublish,key:string,expectedId:string) {
    const result=probabilityReceiptSchema.parse(await writeJson("/api/probability-disclosures",body,key));
    if(result.environment!==body.environment||result.disclosureId!==expectedId)throw new Error("공시 게시 응답 불일치");return result;
  },
  async probabilitySource(environment:"DEV"|"PRD",target:ProbabilityTarget) {
    const params=new URLSearchParams(target);
    return parseProbabilitySource(await readJson(`/api/probability-disclosures/source?${params}`),environment,target);
  },
  async saveMaintenanceDraft(id:string,body:MaintenanceSave,key:string) {
    const result=assertMaintenanceEnvironment(maintenanceReceiptSchema.parse(await writeJson(`/api/maintenance-plans/${encodeURIComponent(id)}/commands`,body,key)),body.environment);
    if(result.maintenanceId!==id||result.draftRevision!==(body.expectedDraftRevision??0)+1)throw new Error("점검 저장 응답 불일치");return result;
  },
  async maintenancePlans(environment:MaintenanceEnvironment,after="") {
    return assertMaintenanceEnvironment(maintenanceListSchema.parse(await readJson(`/api/maintenance-plans?limit=20${after?`&after=${encodeURIComponent(after)}`:""}`)),environment);
  },
  async maintenancePlan(environment:MaintenanceEnvironment,id:string) {
    const result=assertMaintenanceEnvironment(maintenanceDetailSchema.parse(await readJson(`/api/maintenance-plans/${encodeURIComponent(id)}`)),environment);
    if(result.plan.maintenanceId!==id)throw new Error("점검 계획 응답 불일치");return result;
  },
  async maintenancePreview(environment:MaintenanceEnvironment,id:string,revision:number,at:number) {
    const result=assertMaintenanceEnvironment(maintenancePreviewSchema.parse(await readJson(`/api/maintenance-plans/${encodeURIComponent(id)}/preview?at=${at}`)),environment);
    if(result.preview.maintenanceId!==id||result.preview.draftRevision!==revision||result.preview.at!==at)throw new Error("점검 계획이 변경되었거나 예상 시각이 다릅니다. 최신 계획을 다시 조회해 주세요.");return result;
  },
  async notices(environment:"DEV"|"PRD",after="") {
    const result=noticeListSchema.parse(await readJson(`/api/notices?limit=20${after?`&after=${encodeURIComponent(after)}`:""}`));
    if(result.environment!==environment)throw new Error("공지 환경 응답 불일치");
    return result;
  },
  async notice(environment:"DEV"|"PRD",id:string) {
    const result=noticeDetailSchema.parse(await readJson(`/api/notices/${encodeURIComponent(id)}`));
    if(result.environment!==environment||result.notice.noticeId!==id||(result.publication&&result.publication.noticeId!==id))throw new Error("공지 응답 불일치");
    return result;
  },
  async noticeCommand(id:string,command:NoticeCommand,key:string) {
    const result=noticeReceiptSchema.parse(await writeJson(`/api/notices/${encodeURIComponent(id)}/commands`,command,key));
    if(result.environment!==command.environment||result.noticeId!==id||result.command!==command.command||result.revision!==(command.expectedRevision??0)+1)throw new Error("공지 처리 응답 불일치");
    return result;
  },
  async adPolicy(environment:PolicyEnvironment) {
    return mapAdPolicy(await readJson("/api/runtime-policy"),environment);
  },
  async updateAdPolicy(command:AdPolicyCommand,idempotencyKey:string) {
    return mapAdPolicyWrite(await writeJson("/api/runtime-policy",command,idempotencyKey),command);
  },
  async clientReleaseRegistry(target: RegistryTarget) {
    const result = mapClientRegistry(await readJson(`/api/client-releases?target=${target}`));
    if (result.target !== target) throw new AdminApiError({ status: 409, path: "/api/client-releases", errorCode: "registry_target_mismatch" });
    return result;
  },
  async updateClientReleaseRegistry(state: ClientRegistry, context: RegistryEditContext, input: RegistryInput) {
    const command = buildRegistryCommand(state, context, input);
    const result = mapClientRegistry(await writeJson(command.path, command.body));
    if (result.target !== context.target) throw new AdminApiError({ status: 409, path: command.path, errorCode: "registry_target_mismatch" });
    return result;
  },
  async serverDeploymentCurrent(service: DeploymentService) {
    return mapDeployment(await readJson(`/api/server-deployments/current?service=${encodeURIComponent(service)}`), service, "current");
  },
  async serverDeploymentHistory(service: DeploymentService, version: string) {
    return mapDeployment(await readJson(`/api/server-deployments/history/${encodeURIComponent(version)}?service=${encodeURIComponent(service)}`), service, "history", version);
  },
  async bootstrap() {
    return BootstrapResponseSchema.parse(await readJson("/api/bootstrap"));
  },
  async supportProcessing(ticketId:string){
    const result=processingSchema.parse(await readJson(`/api/support/tickets/${encodeURIComponent(ticketId)}/processing`));
    if(result.ticket.ticketId!==ticketId)throw new Error('문의 처리 응답 대상 불일치');return result;
  },
  async supportProcess(ticketId:string,body:ProcessingCommand,key:string){
    const parsed=processingCommandSchema.parse(body);
    const result=processingReceiptSchema.parse(await writeJson(`/api/support/tickets/${encodeURIComponent(ticketId)}/processing`,parsed,key));
    if(result.revision!==body.expectedRevision+1)throw new Error('문의 처리 원결과 변경 번호 불일치');return result;
  },
  async supportGrant(environment:'DEV'|'PRD',id:string){
    const result=grantStateSchema.parse(await readJson(`/api/support/processor-grants/${encodeURIComponent(id)}`));
    if(result.environment!==environment||result.state.principalId!==id)throw new Error('권한 조회 환경 또는 대상 불일치');return result;
  },
  async changeSupportGrant(environment:'DEV'|'PRD',id:string,body:GrantCommand,key:string){
    const command=grantCommandSchema.parse(body);
    return assertGrantReceipt(await writeJson(`/api/support/processor-grants/${encodeURIComponent(id)}`,command,key),environment,id,command);
  },
  async exportVersionToR2(request: VersionExportRequest) {
    return mapVersionExportResult(await writeJson("/api/versions/export", request), request);
  },
  async currentVersion() {
    return mapCurrentVersion(await readJson("/api/versions"));
  },
  async dashboardLaunchSummary() {
    return mapDashboardLaunchSummaryResponse(await readJson("/api/dashboard/launch-summary"));
  },
  async searchPlayers(query: string) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    return PlayerSearchResponseSchema.parse(
      await readJson(`/api/players?${params.toString()}`)
    );
  },
  async playerDetail(accountId: string) {
    const raw = await readJson(`/api/players/${encodeURIComponent(accountId)}`);
    return mapPlayerDetailResponse(raw, accountId, mapPlayerLaunchEvidence(raw));
  },
  async createPlayerNote(accountId: string, note: string) {
    const body = CreatePlayerNoteRequestSchema.parse({ note });
    return CreatePlayerNoteResponseSchema.parse(
      await writeJson(`/api/players/${encodeURIComponent(accountId)}/notes`, body)
    );
  },
  async playerNotes(accountId: string) {
    return PlayerNotesResponseSchema.parse(
      await readJson(`/api/players/${encodeURIComponent(accountId)}/notes`)
    );
  },
  async auditLogs(filters: { action?: string; resourceId?: string; result?: "SUCCESS" | "FAILURE" }) {
    const params = new URLSearchParams();
    if (filters.action) params.set("action", filters.action);
    if (filters.resourceId) params.set("resourceId", filters.resourceId);
    if (filters.result) params.set("result", filters.result);
    const query = params.toString();
    return AuditLogsResponseSchema.parse(
      await readJson(`/api/audit${query ? `?${query}` : ""}`)
    );
  },
  async integrityEvents(filters: {
    accountId?: string;
    classification?: "SOFT_ANOMALY" | "HARD_VIOLATION";
    reviewState?: "OPEN" | "IN_REVIEW" | "RESOLVED";
  }) {
    const params = new URLSearchParams();
    if (filters.accountId) params.set("accountId", filters.accountId);
    if (filters.classification) params.set("classification", filters.classification);
    if (filters.reviewState) params.set("reviewState", filters.reviewState);
    const query = params.toString();
    return IntegrityEventsResponseSchema.parse(
      await readJson(`/api/integrity/events${query ? `?${query}` : ""}`)
    );
  },
  async suspiciousPlayers(filters: {
    reviewStatus?: "NEW" | "REVIEWING" | "CLEARED" | "SCORE_EXCLUDED" | "REWARD_HELD" | "ESCALATED" | "ACTIONED" | "FALSE_POSITIVE";
  }) {
    const params = new URLSearchParams();
    if (filters.reviewStatus) params.set("reviewStatus", filters.reviewStatus);
    const query = params.toString();
    return SuspiciousPlayersResponseSchema.parse(
      await readJson(`/api/integrity/suspicious-players${query ? `?${query}` : ""}`)
    );
  },
  async suspiciousPlayerDetail(accountId: string) {
    return SuspiciousPlayerDetailSchema.parse(
      await readJson(`/api/integrity/suspicious-players/${encodeURIComponent(accountId)}`)
    );
  },
  async searchRuns(accountId?: string) {
    const params = new URLSearchParams();
    if (accountId) params.set("accountId", accountId);
    const raw = await readJson(`/api/runs?${params.toString()}`);
    const parsed = RunSearchResponseSchema.parse(raw);
    const rows = (raw as { items: unknown[] }).items;
    return { ...parsed, items: parsed.items.map((item, index) => ({ ...item, versionEvidence: mapRunVersions(rows[index]) })) };
  },
  async runDetail(runId: string) {
    const raw = await readJson(`/api/runs/${encodeURIComponent(runId)}`);
    return mapRunDetailResponse(raw, runId, mapRunLaunchEvidence(raw));
  },
  async storyProgressList(filters: Partial<StoryProgressFiltersPreview>) {
    return mapStoryProgressListResponse(await readJson(`/api/story-progress${storyProgressQuery(filters)}`));
  },
  async storyProgressDetail(accountId: string) {
    return mapStoryProgressDetailResponse(
      await readJson(`/api/story-progress/${encodeURIComponent(accountId)}`),
      accountId
    );
  },
  async narrativeCoverage(actId: string) {
    return mapNarrativeCoverageResponse(await readJson(`/api/narrative/coverage${queryString({ actId })}`));
  },
  async offlineRewardList(filters: Partial<OfflineRewardReadFiltersPreview>) {
    return mapOfflineRewardListResponse(await readJson(`/api/offline-rewards${offlineRewardQuery(filters)}`));
  },
  async offlineRewardDetail(accountId: string) {
    return mapOfflineRewardDetailResponse(
      await readJson(`/api/offline-rewards/${encodeURIComponent(accountId)}`),
      accountId
    );
  },
  async supportAccountSummary(accountId: string) {
    const path = `/api/support/accounts/${encodeURIComponent(accountId)}`;
    const raw = await readJson(path);
    const identity = raw && typeof raw === "object" ? (raw as { account?: { accountId?: unknown } }).account?.accountId : undefined;
    if (identity !== accountId) throw new AdminApiError({ status: 200, path, errorCode: "ADMIN_ACCOUNT_RESPONSE_MISMATCH" });
    return mapSupportAccountSummaryResponse(raw, accountId);
  },
  async playerActivities(accountId: string) {
    const path = `/api/players/${encodeURIComponent(accountId)}/activity-summary`;
    return mapPlayerActivities(await readJson(path), accountId);
  },
  async supportTickets(filters: Record<string, string>) {
    return ticketList(await readJson(`/api/support/tickets${queryString(filters)}`));
  },
  async supportTicket(ticketId: string) {
    return ticketDetail(await readJson(`/api/support/tickets/${encodeURIComponent(ticketId)}`));
  },
  async gachaBanners(filters: { status?: string } = {}) {
    return mapGachaBannerListResponse(await readJson(`/api/gacha/banners${gachaQuery(filters)}`));
  },
  async gachaAccountPity(accountId: string, bannerId?: string) {
    return mapGachaPityResponse(
      await readJson(`/api/gacha/accounts/${encodeURIComponent(accountId)}/pity${gachaQuery({ bannerId })}`)
    );
  },
  async gachaPullBatches(filters: { accountId?: string; bannerId?: string; limit?: string; cursor?: string }) {
    return mapGachaPullBatchListResponse(await readJson(`/api/gacha/pull-batches${gachaQuery(filters)}`));
  },
  async gachaPullBatchDetail(batchId: string) {
    return mapGachaPullBatchDetailResponse(await readJson(`/api/gacha/pull-batches/${encodeURIComponent(batchId)}`));
  },
  async opsErrorLogs(filters: Partial<OpsErrorLogReadFiltersPreview>) {
    return mapOpsErrorLogListResponse(await readJson(`/api/ops/error-logs${opsErrorLogQuery(filters)}`));
  },
  async opsErrorLogSummary(filters: Partial<OpsErrorLogReadFiltersPreview> = {}) {
    return mapOpsErrorLogSummaryResponse(await readJson(`/api/ops/error-logs/summary${opsErrorLogQuery(filters)}`));
  },
  async opsErrorLogGroups(filters: Partial<OpsErrorLogReadFiltersPreview> = {}) {
    return opsGroupsSchema.parse(await readJson(`/api/ops/error-logs/groups${opsErrorLogQuery(filters)}`));
  },
  async opsErrorLogInvestigation(errorLogId: string) {
    opsEventId.parse(errorLogId);
    const result = opsInvestigationReadSchema.parse(await readJson(`/api/ops/error-logs/${errorLogId}/investigation`));
    if(result.investigation.errorLogId !== errorLogId)throw new Error("OPS_INVESTIGATION_ID_MISMATCH");
    return result;
  },
  async opsErrorLogInvestigationCommand(errorLogId:string,input:OpsInvestigationCommand,key:string) {
    opsEventId.parse(errorLogId);opsRequestKey.parse(key);
    const command = opsInvestigationCommandSchema.parse(input);
    return assertOpsReceipt(await writeJson(`/api/ops/error-logs/${errorLogId}/investigation`,command,key),errorLogId,command);
  },
  async opsErrorLogDetail(errorLogId: string) {
    return mapOpsErrorLogDetailResponse(
      await readJson(`/api/ops/error-logs/${encodeURIComponent(errorLogId)}`),
      errorLogId
    );
  }
};
