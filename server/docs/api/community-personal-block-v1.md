# 개인 차단 저장·WEB API v1 — 2026-10-08
계약 community.personal-block.v1.20261008 / X-Community-Block-Version. Plan11 community-public-contract-v1 승인. 기존 WEB cookie/exact Origin/DEV exact tester 재사용; POST CSRF필수, GET CSRF불필요, 모든 응답 no-store/Vary Origin. 게임Bearer/관리자 자격증명을 웹 인증으로 사용하지 않는다. 약관 동의 gate를 차단에 추가하지 않는다.

POST /v1/web/community/blocks
요청 strict JSON {commandId,publicAuthorId,blocked:boolean,expectedRevision:nonnegative safe integer}. ID ASCII A-Za-z0-9_- 1..128, actor 서버세션 소유. expectedRevision은 해당 계정 전체 차단관계 revision, 처음0. 실제 관계가 바뀌면+1, 현재값과 같은 no-op은 증가0. 응답 {contractVersion,commandId,publicAuthorId,blocked,revision}. 같은키동일payload는 원receipt, 다른payload409. 원receipt는 현재차단상태가 아니며 해제후원receipt 재생으로 관계를 복원하지 않는다. 새block은 실제opaque publicauthor mapping 필요, 자기차단403, 미확인author404. 과거차단author mapping퇴역 뒤 기존관계 해제는 가능하다. 관계/actorrevision/권한·target검증/receipt 동일DBbatch, 실패전체rollback.

GET /v1/web/community/blocks?limit=20&cursor=...
기본20/최대50, 다른필드/중복query/body 거절. 응답 {contractVersion,revision,items:[{publicAuthorId}],nextCursor:null|string}. 내부account/이메일/본문/회원정보 없음. publicAuthorId BINARY ASC, cursor는 owner digest/계약/current 관계revision/afterID 결속; 계정변경400, 목록사이차단변경409→새페이지부터 명시재조회. cursor 불투명취급. 서버 callback 최종재인증+revision재검사.

오류: 400 contract_violation/community_command_invalid/community_cursor_invalid, 401 GET web_session_required, 403 쓰기web_csrf_forbidden(세션없음/회수포함)/web_origin_forbidden/tester_forbidden/community_self_block_forbidden, 404 community_author_unavailable, 409 community_version_conflict/community_request_conflict/community_block_revision_conflict, 503 community_unavailable/community_storage_invalid/community_storage_unavailable. 임의자동재명령키/현재값변경재전송 없음. OPTIONS GET/POST exact Origin 허용, 다른method400.

소유: CommunityBlockStore 단일관계/receipt소유, CommunityContentStore 내용불변, community-write-authorization.ts 기존WEB commit권한fence 공유. 0113 migration additive 테이블만, 실제운영적용/identity seed0. 타인의반대방향열람/게임매칭/게임제재/경제 변경0. public reader에 isBlocked연결은 후속이며 이번은 저장/WEB계약 단위만 완료.

검증: store9/API6 신규 +기존151 중첩 관련166PASS0FAIL0pending, game-api type/scopedlint exit0. QA/CommanderS1-20260922/community-personal-block-final-20261008.json SHA5fddb15b291a474f8a86d91d17c922a08bd0e915cb25fce7e9c2ff9b9fb57c91. static review 중요한지적0/실행은Server. Hono/sql.js 합성로그인 fixture, 실제DEVURL/browser/remoteD1/부하/배포/게시 검증0. receipt퇴역/탈퇴·실파기/관계보존 분류는 운영활성화잔여, 무기한보존완료 주장0. 화면은 최종시안후별도등록, H04 typed부분인수만.