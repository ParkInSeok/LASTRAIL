# S-16 POST 작성 v1 — 2026-10-08

POST /v1/web/community/posts · X-Community-Content-Version: community.post-create.v1.20261008
JSON {commandId,boardId,title,markdown,spoiler?,wikiRefs?,sourceBalanceVersion?}. boardId 기술 안정ID 매핑: free=자유, qna=질문, guide_build=공략·빌드, feedback=피드백. 표시명으로권한판단0·추가게시판0. spoiler 생략false, wikiRefs생략[], sourceBalanceVersion생략null. wikiRef={snapshotId:'pcs_…',itemId:actualPublicId}. 제목100/본문10000 Unicode·existing renderer·transport128000byte, rawHTMLescaped/이미지·embed·fetch없음. semantic parsedpayloadhash는 생략기본과 명시기본을동일원명령으로취급, COMMENT원hash변경0.

WEBcookie/exactOrigin/CSRF/DEVexacttester, Game Bearer/Admin0, all no-store. OPTIONS exactOrigin POST. serveractor/ID/opaqueauthor/normalVISIBLE/revision1/createdAt 결정. 추가자동분류0, 명시필수reviewsource미지원503. 정상텍스트는 공개wiki자료없는환경에서도 작성가능; 실제운영활성화/게시를 뜻하지않음.

응답200 {contractVersion,rendererVersion,commandId,contentId,parentId:null,boardId,detailsHash,kind:'POST',revision:1,visibility,createdAt}. body/rawMarkdown/internalID없음. 동일actor/key/semanticpayload는 원receipt, 변형409, 원receipt를최신게시상태로사용0. 기존ownGET community.own-content-read.v1.20261008에서 CREATE_POST 원renderer/receipt/audit/statehash를검증하여본문읽기 가능. title/body수정은기존edit별도, spoiler/wikiRefs/source metadata수정은현재공급하지않음.

community-content-creator.ts가 POST/COMMENT 단일생성거래 소유, 기존 community-comment-creator.ts는 호환reexport만(소비import이행후제거). active state/domainrenderer/권한은기존소유재사용. 0111 community_post_details·본문없는 create audit 추가, COMMENT0108~0110·기존hash/receipt보존. rights/current source JSON+digest/rate 조건을같은batchfence로 확인하고 content+metadata+원receipt+minimal audit원자. POST최소60초/rolling(t-60분,t]10 성공만·failed/replay0, 정확interval/하한허용·429최대해제ceil retryAfter/Retry-After. COMMENT한도와독립. audit180일retainUntil기록은실파기/receipt퇴역/법적로그전체완료아님.

위키는 기존 readPublicCatalogSnapshot/readPublicReleaseIndex 검증기를재사용. 같은environment·DEVexactappCode의 실제verified+published fixedsnapshot/publicitem만참조. stage-only불허, unknown/private/다른버전ref는400 community_wiki_reference_unavailable(이름/존재노출0); 저장/검증source실패503 community_wiki_source_unavailable. sourceBalanceVersion도실published snapshot에서확인. 같은snapshot 한요청1회검증cache, sourceBalance조회는published후보1개선택. 고정과거publicID/history보존·CURRENThead만으로과거참조덮기0. 검증후guard를위한두번째raw조회도원검증snapshot의canonicalJSON/sha와일치필수, 이후batchsource경합은rollback. 운영원본없음은ref만차단·plaintext전체대기0.

오류:400계약/입력/renderer/wiki generic,403기존WEBmutation cookie/Origin/CSRF/tester권한,409version/request/rate외revision경합,429community_rate_limited{retryAfter},503review/wiki/storage source미준비·불일치. 공개목록/공개상세·ownPOST목록/개인차단/신고/Admin운영검수는별도잔여. 기존H04비화면typedPOST/receipt/error수명소비만, 화면연결시안후별도.

QA: D:/Project/LASTRAIL/QA/CommanderS1-20260922/community-post-create-verified-20261008.json. 실제Hono app.request/sql.js synthetic로그인/publicrelease fixture(격리내stage/publish만), 실제DEVURL/remoteD1/browser/TCP/WorkersCPU·실게시·운영migration·배포근거아님. reviewerP2 검증후rawsource재조회경합 실제RED200→수정503/소비0; defaultomission회귀. 중간final파일137PASS1FAIL은oldfixture schema누락 역사근거로보존, 완료근거로사용0.
후속(2026-10-08): ownPOST목록은 community-own-post-list-v1.md 별도공급. 이전 미구현문구는그시점역사이며public읽기/신고/차단/검수잔여는유지.

<a id="community-creation-consent-envelope-20261008"></a>
2026-10-08 총괄 동의명세 적용: 신규 작성의 추가필수 X-Community-Creation-Eligibility-Version community.creation-eligibility.v1.20261008, actualpublished약관쌍/현재계정동의 원자gate. 정확계약 community-consent-v1.md. 기존body/hash/receipt維持·原receipt현재WEBauth재생/자동재동의0, actualsource없음503/미동의403/current변경409. 기존v1본문/rate 단위완료와 운영가입/약관선행완료를혼동하지 않음. read/edit/delete/block/report gate확대0. 실약관원문/버전/발행/보존·탈퇴未確認·운영활성화不可.
