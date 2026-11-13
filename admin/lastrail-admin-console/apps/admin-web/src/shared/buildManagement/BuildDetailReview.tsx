import {buildPreparation,buildPhaseLabels,type LocalBuildEvidence} from './buildPreparation';
export function BuildDetailReview({evidence}:{evidence:LocalBuildEvidence|null}){return <section aria-label="빌드 상세 준비">
 <h3>승인된 연결 순서 · 실제 지원 미확인</h3><ol>{buildPreparation.profiles.map(p=><li key={`${p.platform}:${p.environment}`}>{p.platform} {p.environment} {p.format} · 환경검사 미준비</li>)}</ol>
 <p>iOS 지원 실행기 미확보 · 초기 실행기당 동시 1건 · 실패 build 번호 자동 재사용 없음</p>
 <p>라이선스·모듈·디스크·소스/패키지·서명 참조 사전검사 필요. Development Build 여부와 서비스 환경은 별개이며 PRD 진단 UI/테스트 인증/fixture 진입은 차단 대상입니다.</p>
 <p>원작업/시도/lease 권위 확인 전 실행·취소·재시도 불가. 통신단절은 실패 확정이 아니며 원작업으로 복구합니다.</p>
 <h3>단계 준비</h3><p>{Object.values(buildPhaseLabels).join(' / ')}</p>
 <p>빌드 성공은 종료코드·BuildReport·필수 파일·manifest/hash 확인이 필요합니다. 등록 복구가 필요하면 동일 빌드를 자동 재실행하지 않습니다.</p>
 {evidence&&<><h3>로컬 검토 관찰값 · 서버 검증 결과 아님</h3><p>revision {evidence.revision} · {buildPhaseLabels[evidence.phase]}</p>
 <p>{evidence.build==='succeeded'&&evidence.registration==='recoveryRequired'?'빌드 성공·파일 등록 복구 필요':`빌드 ${evidence.build} / 등록 ${evidence.registration}`}</p>
 <p>설치 검수 {evidence.installation==='unreviewed'?'미확인':evidence.installation} · 실Player 검수 {evidence.player==='unreviewed'?'미확인':evidence.player}</p>
 <p>로그 offset: {evidence.log?.nextOffset??'미공급'} · 후속 페이지: {evidence.log===null?'미공급':evidence.log.hasMore?'있음':'없음'}</p>
 <p>원문 로그·비밀값은 받지 않습니다. 제한값·정제 계약 공급 전 원문 표시 없음.</p>
 <ul>{evidence.artifacts.map((a,i)=><li key={i}>{a.name} · {a.bytes} bytes · checksum {a.checksum} · {a.platform} · commit {a.commit} · 콘텐츠 {a.contentVersion} · 프로필 {a.profile} · 완결성/다운로드 권한 미검증</li>)}</ul></>}
 <p>manifest에는 Unity/소스/잠금파일/콘텐츠/설정 hash·정식 진입장면·호환 version의 검증 근거가 필요합니다. 파일/로그 보존기간·시간제한·서명키는 임의 생성하지 않습니다.</p>
 </section>;}
