# 상인 배경·열차 수리 독립 미술 검토 v61
2026-10-02 / D-EQ-01@equipment-supply-v18 / **신규 두 자산 사용자 미술 승인 대기**

## 이전 승인 등록
사용자 "승인 다음 ㄱㄱ"으로 equipment-composition-v60-review 조합4화면 및 healing-potion.png를 최종 미술 승인 등록했다. manifest/qa/REVIEW/index 승인 readback 및 기존 자산 해시 확인. 실제 게임·실물터치·영수증/거래 검증은 승인범위가 아니다.

## 이번 제작
- 상인 배경 merchant-background.png: 승인 equipment-merchant-storage-v12-illustration의 철도역/보급상자·황동 문양/따뜻한 램프/우측 정차기관차를 참조, UI없는 불투명 배경 재제작.
- 수리 아이콘 train-repair.png: 같은 승인 화면 수리행의 기관차를 참조, 금속/황동/헤드램프만 독립 투명서비스삽화. 이전 파랑렌치 아이콘 참조는 역사v60에보존.
- 두 원화는 builtin image_gen 재제작 후보이며 픽셀동일 추출을 주장하지 않는다. 생성 원본 D:/CodexData/Home/generated_images/01a0c3e9-35b6-7010-ae1c-6463f4047997/exec-387b6100-85f9-4397-9ad1-e9ee5cba8966.png 및 exec-3e3c9853-7d7b-4fef-ac4f-cb01134c7cdb.png 보존.
- v60 승인 포션 동일 SHA 재사용, 프레임/장비/태그/별/문구/가격/버튼/입력 별도. 수리20/20%, 포션24/25% 현행 승인정책과 기존 상인정리복귀 계약 유지.

## 검증
qa.json **headless Chrome 360/430/720폭의 상인 화면 45/45 PASS**. 이미지 로드/스크립트 오류0, 가로넘침없음, 신규배경/수리 경로연결 및 독립숨김/표시, 기존가득참장비구매차단/서비스입력·보관함정리/복귀/재고·보급·scrollTop유지/구매가능상태 재평가, 배지중앙/독립숨김 검사.
수리 alpha 4px 간격 표본: 완전투명44628/부분투명53781, 크기1448×1086. 배경1672×941, 정책수치/제목/버튼 굽지 않음. alpha 표본·SHA는 assets/alpha-qa.json 및 manifest 참고.
생성 원화2종과 merchant-360.png/merchant-430.png를 육안 검토: 배경중요소재/기관차서비스그림/포션/가격과본문 가독성 확인. 캡처는 단건정리 후29/30 상태이며 initial merchant.html은30/30. 이전 다른3화면 브라우저138검수근거는 승인v60 그대로이며 이번 새로 전수검수했다고 주장하지 않는다.

## 남은 범위
새배경/수리 독립원화와 상인 조합의 사용자 승인. 실물터치·드래그/합성/최고랭크·잠금소모/기기성능·Unity import/9slice·실START초상/instanceID·재고·현재HP·할인counter·권한/revision/원영수증/네트워크연결은 별도. service 클릭은 검토 안내만, 구매/회복실행0.
이번 신규원화2종을 승인하지않은 다른화면/정책/런타임에 자동 적용하지 않는다. 전투·포탑성장 신규제작 보류유지. 전체D-EQ큐완료/소비후속조건충족으로표시하지 않는다.

