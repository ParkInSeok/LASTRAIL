# Audio approved production assets

2026-11-14 Audio_2 scoped publication. This is a selected export of the local QA record. Historical trials, listening previews, account details, caches and large generation originals stay local. Runtime integration remains unverified. Source files retain original paths and hashes.

<a id="lobby-departure-runway-loop-approved-20261103"></a>
### 로비·출발 준비 공용 BGM 최종 채택 — 2026-11-03
사용자가 두 바퀴 청취본 제시 후 Runway 「차창 너머」 2분 루프 편집본을 두 화면의 공용 BGM으로 확정했다. 승인 자산은 [재생용 마스터](lobby-departure-loop-review-v01/runway-loop.wav). SHA256 30a9d8940bce518c05c07ebdb22d2d345ec2cfccf5d81e8159a62bb87298b3db. 스테레오 PCM24, 44100 Hz, 전체 5292001 프레임. 루프 시작 0, 끝 5292001 (끝 제외). [BGM 50% 두 바퀴 청취본](lobby-departure-loop-review-v01/runway-two-loops-bgm50.wav)은 검토용이며 마스터의 재생 음량은 게임 BGM 설정에서 한 번 적용한다.
18#lobby-departure-bgm-approved-20261103에 따라 로비↔출발 준비 이동 시 같은 곡의 재생 위치 유지, 실제 교전 진입 시 전투곡으로 전환. 승인 범위는 곡과 편집 루프 채택. Unity 연결·실제 화면 전환 재생은 미검증.

<a id="five-agent-contact-c-level-approved-20261103"></a>
### 기본 5인 평타 전투 믹스 B 확정 — 사용자 청취 선택
BGM 50%와 5인 평타음은 기존 overlap-v01 설정을 유지한다. 명중 C만 A의 원음 대비 선형 게인 0.65에서 −3dB 낮춘 0.46016475984969 (약 46.0%)로 확정. [승인 비교본 B](basic-attack-local-v03/overlap-v01/contact-minus3-v01/battle-C-minus3dB.wav)의 SHA256은 7d3b304264d77cc476e6662549d47df60b196d7f2dae5724dfd3a793c78be4f2. C 원음 hit-local-v03/C.wav의 SHA256은 b434e431f78efa98e06c2d1998d1c3e2e30022cf1c4615dbb5248b5440f3bd43. 개별 원음 소비 시 최종 게인 약 0.460165를 한 번 적용한다. B 청취본에는 보정이 이미 포함되어 있다. 이 결정은 이번 5인 전투 믹스의 C 레벨에 적용하며, Unity 실재생 반영은 미검증.


## Published file verification

Seven production WAV files passed SHA256 match, decode, finite/non-silent and sample clipping checks. Preview links in the approval history refer to local-only review files.

- `basic-attack-local-v03/kyle.wav` SHA256 `71a262434326ca1ec49e26ba86d5cb34b8d87d179df8feb60b8eca7d57482d8c`
- `basic-attack-local-v03/eve.wav` SHA256 `5949633ab4f71d2eaeda704fa718c2fb72f19d36b649a9613907f3f9304e1cd9`
- `basic-attack-local-v03/sera.wav` SHA256 `221f9ed7a89e98bed939624cf93b3b102eff81c8aa1c347ae257ec51865ddbb2`
- `basic-attack-local-v03/nyx.wav` SHA256 `ff556b75518a8f04b3327160f9909060c513fe9f2282cf6a2e6d29e88a8cd4cb`
- `reina-pistol-local-v13/new-pistol.wav` SHA256 `3fdefbd743287d3335759e8479882e9cf2002c56cfc4c5643d48cb360267806a`
- `hit-local-v03/C.wav` SHA256 `b434e431f78efa98e06c2d1998d1c3e2e30022cf1c4615dbb5248b5440f3bd43`
- `lobby-departure-loop-review-v01/runway-loop.wav` SHA256 `30a9d8940bce518c05c07ebdb22d2d345ec2cfccf5d81e8159a62bb87298b3db`
