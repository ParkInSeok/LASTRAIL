# LAST RAIL | 영구 광고 스킵·구매권한·사용승인

범위: 상품 구매·복원·회수, 권한 조회, 기존 보상 기회 사용승인. 보상 계산/표시/지급은 [06](06_REWARDS_ROULETTE.md), 증강 리롤은 [05](05_AUGMENTS.md)에만 정의한다.

## 1. 상품과 책임 경계

Store verification adapter → purchase/order/entitlement → opportunity approval의 승인 사유 → 기존 draw/settlement 순으로 책임을 나눈다. Hono route는 검증/호출, Unity AccountScreenRuntime는 작은 service/controller 조합을 맡는다. 구매권한 하나를 얻는 것이 자체 보상 지급은 아니다.

단독 **14,900은 기획값**이며 실제 상품 가격으로 표시할 fallback이 아니다. 공급자·product/SKU·checkout duration·실가격·adapter 설정이 없으면 구매/Store 복원을 unconfigured로 잠근다. 가짜 ACTIVE·결제 성공·실가격을 생성하지 않는다. 서버 권한 새로고침은 Store 구매 복원과 다른 동작이다. [R28](00_INDEX.md#r28) [R29](00_INDEX.md#r29) [R30](00_INDEX.md#r30)

## 2. 구매·복원·회수

catalog schema, checkout의 account/environment 결속, verify/restore adapter, request digest/replay, 유일 store transaction을 검증한다. 결제 검증 전 소유권을 주지 않으며 같은 거래 복원은 중복 권한을 만들지 않는다. revoke tombstone과 entitlement revision은 뒤늦은 원거래 응답이 회수된 권한을 부활시키지 못하게 한다.

원격 반환 URL을 그대로 인증 호출에 사용하지 않는다. 타계정·타환경 거래와 위조를 거부하고 Store adapter 부재를 fixture로 숨기지 않는다. 테스트용 구매 공급자는 격리 환경에 한정한다. [R29](00_INDEX.md#r29) [R30](00_INDEX.md#r30)

## 3. 계정 권한 상태

| 상태/상황 | 처리 |
|---|---|
| NONE / ACTIVE / REVOKED | 서버가 확인한 권한 snapshot의 상태 |
| UNKNOWN | 조회 실패/미확인 상태. NONE이나 가짜 REVOKED DTO로 만들지 않음 |
| 과거 ACTIVE cache | 현재 사용승인 권한이 아님 |
| 늦은 응답 | account/generation/request serial 검증으로 오래된 read가 최신 상태를 덮지 못함 |
| 사용승인에서 회수·미보유 확인 | 오래된 보유 cache를 UNKNOWN으로 무효화 |
| 설정 유효 AVAILABLE | 기능 설정 상태이며 실제 소유권과 독립 |

AdSkipAccountService가 read/lifetime을, AdSkipUseController가 사용승인과 durable recovery를 소유한다. 권한·사용·스토어 구매를 한 bool 또는 거대 owner로 합치지 않는다. [R30](00_INDEX.md#r30)

## 4. API 버전과 승인 사유

`GET /v1/ad-skip`의 `ad-skip-v1`, `checkout-intents`, `purchases/verify`, `purchases/restore`, `uses/authorize` 계약을 사용한다. 승인·관련 read/spin에는 **`X-Reward-Contract: reward-opportunity-v2`**를 명시한다. 기존 empty-body spin과 AD v1 호환은 보존하되 상품을 지원하지 않는 v1은 명시 거부한다.

v2는 검증된 identity/approval, nullable adContext, server-pinned baseRewardLines를 정규화한다. 상품 경로에 fake adUnit/customData/SSV를 만들지 않는다. 승인 origin/cause/order/revision을 불변 기록하고 AD-origin만 광고 계수에 반영한다. 승인 CAS는 현재 권한과 기회를 함께 확인하며 승인 replay는 이후 revoke에도 원승인 결과를 보존한다.

0039/0040 migration과 승인 CHECK 재구성은 원키·역사 주문·0038 고정 결과/weights/영수증을 보존하고 구 writer의 허용되지 않는 쓰기를 거부한다. 실패 시 schema/data rollback을 격리 검증한다. 늦은 SSV는 원기한과 유효 기회 규칙을 지키며 원승인 cause를 덮어쓰지 않는다. 실제 운영 migration 적용은 별도다. [R28](00_INDEX.md#r28) [R29](00_INDEX.md#r29)

## 5. 보급·전투 호스트와 복구

ACTIVE 사용자는 한 번 입력해 **승인 → 기존 spin**으로 이동하며 광고 `ShowAsync`를 호출하지 않는다. 미보유자의 신규 광고는 [10](10_LIVEOPS_NOTICES.md#ads)의 광고 ON·현재 가용성을 만족할 때에만 명시 입력으로 제공한다. OFF/UNKNOWN에서 기존 광고 경로로 자동 전환하지 않는다. UNKNOWN에서 신규 광고를 자동 실행하지 않고 기본 보상 수령/무료 규칙을 잠그지 않는다. 기존 pending 광고 조회는 UNKNOWN이어도 가능하지만 SDK를 다시 재생하지 않는다.

사용승인 identity/payload/key를 네트워크 전에 `reward-approval` journal에 저장한다. 기존 광고와의 전이는 `IAccountRewardApprovalJournal.ReplaceApproval` CAS로 처리한다. body는 원 opportunity를 보내고 원광고 context/key는 복원 metadata에 보존한다. 확정 거부 시 이전 기록 복원이 가능해야 하며 응답 유실은 원본문·원키로 재시도한다. 구매영수증은 이 보상 슬롯에 저장하지 않는다.

controller의 전체 UI Busy는 기존 spin의 좁은 내부 실행 경계로 인계하는 동안 유지해 중복 입력을 막는다. 계정/화면 해제·재바인딩·닫기 중 응답을 검증한다. 승인 기회에 고정된 baseline을 기존 표시 흐름에 먼저 전달하고 missing/ambiguous 값은 중립으로 처리한다. 룰렛 연출/최종 결과 규칙은 [06](06_REWARDS_ROULETTE.md)에 둔다. [R30](00_INDEX.md#r30)

## 6. 남은 상용 연결

실제 Store SDK/등록/SKU/가격·구매·복원·환불/회수 검증과 기기 결제는 로컬 fixture와 별개다. 리롤 연결 현황은 [05](05_AUGMENTS.md)만 기준으로 삼는다. 보급·전투 사용 성공만으로 세 혜택 전체나 상품 출시 준비가 끝났다고 보고하지 않으며 실제 판매 활성화는 세 혜택 검증 이후 별도 단계다.

기존 상세 원문은 [영구권 상세](48_AD_REWARD_DETAIL_CONTRACTS.md#source-d06447f1a5c3fef4), [서버 계약](48_AD_REWARD_DETAIL_CONTRACTS.md#source-5cbc96b63e17ce2a), [Unity 계약](48_AD_REWARD_DETAIL_CONTRACTS.md#source-2b83cf73b602eca9), [화면 흐름](48_AD_REWARD_DETAIL_CONTRACTS.md#source-fcd64dcd8469bdc3)에 보존한다. 최신05의 카드별 예산·10의 광고 OFF 정책이 충돌하는 과거 설명보다 우선한다. source/기능/UI/Store 검증 상태는 [문서 안내](00_INDEX.md#qa)에 기록한다. [R28](00_INDEX.md#r28) [R29](00_INDEX.md#r29) [R30](00_INDEX.md#r30)

## 7. 광고 없는 데모·광고 마스터 스위치와의 관계 — U7

[10](10_LIVEOPS_NOTICES.md#off-benefits)의 adsEnabled는 광고 노출 허용이며 소유권 bool이 아니다. false를 전원 ACTIVE 또는 REVOKED로 변환하지 않는다. **J08 승인:** 첫 데모에서는 영구 광고 스킵 신규 판매를 숨기고 신규 checkout을 미활성화하지만, 이미 존재하는 적법한 소유권·복원·원승인·고정결과를 삭제하지 않는다.

**J08 확정:** 서버가 ACTIVE로 확인한 구매자는 adsEnabled=false여도 광고 없이 기존 보급/클리어 룰렛과 적격 추가 리롤을 상품 사용승인으로 이용한다. [05 §3](05_AUGMENTS.md)의 카드(슬롯)별 추가1회를 사용하며 같은 카드에서 광고·상품으로 추가2회를 지급하지 않는다. 2026-09-21 사용자 최신 정정에 따른다. 상품 승인으로 발생한 혜택의 가용성은 기존 상품 계약과 기능 구현 상태로 판단한다. 광고 스위치만 껐다는 이유로 유효 상품을 회수하지 않고, 이 값 하나로 다른 모든 IAP도 중단하지 않는다. 이미 시작한 승인/지급 복구는 10의 전환 계약과 본문의 원요청 규칙을 함께 지킨다. 미래 광고 도입은 SDK·동의·스토어·세 혜택 검증이 준비된 별도 단계다.


## 8. U9 점검과 구매권한

점검은 영구권 회수/삭제 사유가 아니다. 신규 구매/상품사용은 실제 기능 가용성에 따라 제한할 수 있지만 기존 거래·권한·원결과는 보존한다. [12](12_SERVICE_MAINTENANCE.md#recovery)의 복구 허용 경로를 따르며, 점검 때문에 미확정 거래를 새 주문/새 키로 재생성하지 않는다. 광고 OFF와 점검은 서로 독립적인 정책이다.
## 기존 구현·검증 원문

- [R28 2026-09-17-ad-skip-local-integration.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-514303eac46ac52f6ba3)
- [R29 2026-09-17-ad-skip-server.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-1abea96db825288c5dbb)
- [R30 2026-09-17-ad-skip-unity-phase1.md](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-b5053d073811199f9bf0)
