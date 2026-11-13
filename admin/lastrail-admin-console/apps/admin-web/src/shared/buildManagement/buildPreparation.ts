/** Approved local planning values. These are NOT server DTOs, API schemas or supported runner profiles. */
export const buildPreparation=Object.freeze({perRunnerConcurrency:1,reuseFailedBuildNumber:false,profiles:Object.freeze([
 Object.freeze({platform:'Windows64',environment:'DEV',format:'bundle',readiness:'unverified'}),
 Object.freeze({platform:'Android',environment:'DEV',format:'APK',readiness:'unverified'}),
 Object.freeze({platform:'Android',environment:'PRD',format:'AAB',readiness:'unverified'})])});
export const buildPhaseLabels={queued:'대기',preflight:'사전검사',sourcePreparation:'소스준비',building:'Unity 빌드',artifactVerification:'산출물검증/등록',completed:'완료',failed:'실패',cancelRequested:'취소요청',cancelled:'취소완료',outcomeUnknown:'결과불명확'} as const;
/** Caller-owned local observation only. A verified producer adapter must validate wire data before any future use. */
export type LocalBuildEvidence=Readonly<{scope:string;generation:number;revision:number;phase:keyof typeof buildPhaseLabels;build:'unknown'|'succeeded'|'failed';registration:'unknown'|'registered'|'recoveryRequired';installation:'unreviewed'|'passed'|'failed';player:'unreviewed'|'passed'|'failed';log:Readonly<{nextOffset:string|null;hasMore:boolean}>|null;artifacts:readonly Readonly<{name:string;bytes:number;checksum:string;platform:string;commit:string;contentVersion:string;profile:string}>[]}>;
/** No network ingestion, state mutation, callback or retry. Equal revisions may be redisplayed without a read. */
export function projectBuildEvidence(evidence:LocalBuildEvidence,scope:string,generation:number,currentRevision:number):LocalBuildEvidence|null{
 return evidence.scope===scope&&evidence.generation===generation&&Number.isSafeInteger(evidence.revision)&&evidence.revision>=currentRevision?evidence:null;
}
