import {BuildDetailReview} from './BuildDetailReview';
import {projectBuildEvidence,type LocalBuildEvidence} from './buildPreparation';
import {buildInputLabels,type BuildInput,type BuildInputDraft} from './BuildInputDraft';
type Props={state:ReturnType<BuildInputDraft['snapshot']>;onChange:(field:keyof BuildInput,value:string)=>void;localEvidence?:LocalBuildEvidence;minimumEvidenceRevision?:number};
/** Pure controlled View: parent binds its draft owner's change callback. No effects/subscriptions or remote I/O. */
export function BuildManagementReview({state,onChange,localEvidence,minimumEvidenceRevision=0}:Props){return <section aria-label="Unity 빌드 입력 검토">
 <h2>Unity 빌드 입력 검토</h2><p>로컬 검토 초안 · 실행기/API/권한 계약 미연결 · 지원 플랫폼/승인 프로필 미확인</p>
 <p>입력값은 승인·지원·번호 예약을 확정하지 않습니다. 비밀값·서명값을 입력하지 마세요.</p>
 {Object.entries(buildInputLabels).map(([key,label])=>{const field=key as keyof BuildInput;return <label key={field} style={{display:'block'}}>{label}{field==='environment'?<select value={state.input.environment} onChange={e=>onChange(field,e.target.value)}><option value="">선택 필요</option><option value="DEV">DEV</option><option value="PRD">PRD</option></select>:field==='note'?<textarea value={state.input[field]} onChange={e=>onChange(field,e.target.value)}/>:<input value={state.input[field]} onChange={e=>onChange(field,e.target.value)}/>}</label>;})}
 <button type="button" disabled>실행 불가 · 실행기/API/권한 계약 대기</button>
 <h3>작업 상태 미연결</h3><p>요청ID·시작/종료시간·단계·경고/오류는 원본 계약 공급 후 표시합니다. 취소요청·취소완료·결과불명확은 서로 다른 상태입니다.</p>
 <h3>단계별 로그 미공급</h3><p>비밀 정제와 크기 제한을 확인한 로그만 연결합니다. 원본 로그를 입력하거나 누적하지 않습니다.</p>
 <h3>산출물 미공급</h3><p>플랫폼·commit·version·프로필·파일크기·checksum·다운로드 권한을 확인할 계약이 필요합니다. 외부 URL은 자동 실행하지 않습니다.</p>
 <ul><li>설치 검수 미확인</li><li>실Player 검수 미확인</li><li>배포·스토어 공개 미확인</li></ul>
 <p>파일 생성/검증 성공과 설치·실Player 검수·배포 공개는 각각 별도로 확인합니다.</p>
 <BuildDetailReview evidence={localEvidence?projectBuildEvidence(localEvidence,state.scope,state.generation,minimumEvidenceRevision):null}/>
 </section>;}


