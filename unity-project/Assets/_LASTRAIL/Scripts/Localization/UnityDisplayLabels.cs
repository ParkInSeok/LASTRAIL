using System;
using System.Collections.Generic;
using LastRail.Client;
using LastRail.Gacha;
using LastRail.Run;
using LastRail.Sortie;
using LastRail.Tactics;

namespace LastRail.Localization
{
    public static class UnityDisplayLabels
    {
        private static readonly IReadOnlyDictionary<string, string> BackendCodeLabels = new Dictionary<string, string>
        {
            { "ACCEPTED", "수락" },
            { "DUPLICATE_REPLAY", "중복 제출" },
            { "REJECTED", "거부" },
            { "contract_violation", "계약 위반" },
            { "duplicate_replay", "중복 제출" },
            { "run_state_conflict", "런 상태 충돌" },
            { "validator_temporarily_unavailable", "검증기 일시 사용 불가" },
            { "stale_client_state", "오래된 클라이언트 상태" },
            { "insufficient_command_ticket", "지휘권 부족" },
            { "auth_required", "인증 필요" },
            { "banner_inactive", "비활성 모집 배너" },
            { "invalid_pull_count", "잘못된 모집 횟수" },
            { "gacha_pool_unavailable", "모집 풀 사용 불가" },
            { "gacha_state_conflict", "모집 상태 충돌" },
            { "gacha_temporarily_unavailable", "모집 일시 사용 불가" }
        };

        private static readonly IReadOnlyDictionary<string, string> GachaGuaranteeTypeLabels = new Dictionary<string, string>
        {
            { "NONE", "보장 없음" },
            { "RARE_OR_HIGHER_10", "10회 희귀 이상 보장" },
            { "ELITE_30", "30뽑 정예 확정" },
            { "PICKUP_ELITE_60", "60뽑 픽업 정예 확정" }
        };

        private static readonly IReadOnlyDictionary<string, string> ActOneStoryProgressLabels = new Dictionary<string, string>
        {
            { "none", "스토리 진행 없음" },
            { "opn_01_last_departure", "마지막 출발" },
            { "opn_02_first_signal", "첫 신호" },
            { "opn_03_water_stop", "급수 정차" },
            { "opn_04_scrapstar_show", "스크랩스타 쇼" },
            { "opn_05_night_boarders", "야간 승선전" },
            { "opn_06_broken_line", "끊어진 선로" },
            { "raid_raider_king_engine", "약탈왕 기관차" }
        };

        private static readonly IReadOnlyDictionary<string, string> ItemCodeLabels = new Dictionary<string, string>
        {
            { "cur_command_ticket", "지휘권" },
            { "cur_command_ticket_fragment", "지휘권 조각" },
            { "cur_special_core", "특수 코어" },
            { "cur_operator_token", "요원 증표" },
            { "ui_fuel_stamina", "연료" },
            { "ui_supply_crate", "보급 상자" },
            { "ui_reward_chest", "보상 상자" }
        };

        public static string ForClientEnvironment(ClientEnvironment environment)
        {
            switch (environment)
            {
                case ClientEnvironment.Unconfigured:
                    return "미설정";
                case ClientEnvironment.Dev:
                    return "개발";
                case ClientEnvironment.Prd:
                    return "운영";
                default:
                    return environment.ToString();
            }
        }

        public static string ForRunResultOutcome(RunResultOutcome outcome)
        {
            switch (outcome)
            {
                case RunResultOutcome.Victory:
                    return "승리";
                case RunResultOutcome.Failure:
                    return "실패";
                case RunResultOutcome.Abandoned:
                    return "포기";
                default:
                    return outcome.ToString();
            }
        }

        public static string ForTacticKind(TacticKind kind)
        {
            switch (kind)
            {
                case TacticKind.Command:
                    return "지휘전술";
                case TacticKind.Ultimate:
                    return "얼티밋 전술";
                default:
                    return kind.ToString();
            }
        }

        public static string ForSortieStage(SortieStage stage)
        {
            switch (stage)
            {
                case SortieStage.Wave:
                    return "공세";
                case SortieStage.FinalOperation:
                    return "최종 작전";
                case SortieStage.Complete:
                    return "완료";
                default:
                    return stage.ToString();
            }
        }

        public static string ForSubmitAction(RunApiFlowSubmitAction action)
        {
            switch (action)
            {
                case RunApiFlowSubmitAction.ShowRewardReview:
                    return "보상 확인";
                case RunApiFlowSubmitAction.SyncRecognizedResult:
                    return "제출 결과 동기화";
                case RunApiFlowSubmitAction.ShowRecoverableRejected:
                    return "복구 가능 오류";
                case RunApiFlowSubmitAction.ShowFatalRejected:
                    return "치명 오류";
                case RunApiFlowSubmitAction.RetryWithSameIdempotencyKey:
                    return "재시도";
                default:
                    return action.ToString();
            }
        }

        public static string ForRunEndUxStateKind(RunEndUxStateKind kind)
        {
            switch (kind)
            {
                case RunEndUxStateKind.RewardReviewReady:
                    return "보상 확인 준비";
                case RunEndUxStateKind.RecognizedDuplicate:
                    return "중복 제출 확인";
                case RunEndUxStateKind.RecoverableRejected:
                    return "복구 가능 제출 거부";
                case RunEndUxStateKind.FatalRejected:
                    return "제출 거부";
                case RunEndUxStateKind.RetryableNetworkError:
                    return "네트워크 재시도 필요";
                default:
                    return kind.ToString();
            }
        }

        public static string ForRiskLevel(int riskLevel)
        {
            switch (riskLevel)
            {
                case 1:
                    return "위험도 I";
                case 2:
                    return "위험도 II";
                case 3:
                    return "위험도 III";
                case 4:
                    return "위험도 IV";
                case 5:
                    return "위험도 V";
                default:
                    return "위험도 " + riskLevel.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static string ForFirstClearBonus(bool hasFirstClearBonus)
        {
            return hasFirstClearBonus ? "첫 클리어 보너스" : "첫 클리어 보너스 없음";
        }

        public static string ForActOneStoryProgress(string storyProgressId, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(storyProgressId))
            {
                return fallback ?? string.Empty;
            }

            var normalized = storyProgressId.Trim();
            return ActOneStoryProgressLabels.TryGetValue(normalized, out var label) ? label : normalized;
        }

        public static string ForOfflineRewardCapHours(int capHours)
        {
            return "최대 " + capHours.ToString(System.Globalization.CultureInfo.InvariantCulture) + "시간";
        }

        public static string ForGachaPullUxStateKind(GachaPullUxStateKind kind)
        {
            switch (kind)
            {
                case GachaPullUxStateKind.ResultReviewReady:
                    return "모집 결과 확인";
                case GachaPullUxStateKind.RecognizedDuplicate:
                    return "이전 모집 결과 동기화";
                case GachaPullUxStateKind.RecoverableRejected:
                    return "복구 가능 모집 오류";
                case GachaPullUxStateKind.AuthRequired:
                    return "인증 필요";
                case GachaPullUxStateKind.FatalRejected:
                    return "모집 거부";
                case GachaPullUxStateKind.RetryableNetworkError:
                    return "네트워크 재시도 필요";
                default:
                    return kind.ToString();
            }
        }

        public static string ForGachaPullRejectionRecovery(GachaPullRejectionRecovery recovery)
        {
            switch (recovery)
            {
                case GachaPullRejectionRecovery.None:
                    return "조치 없음";
                case GachaPullRejectionRecovery.Recoverable:
                    return "복구 가능";
                case GachaPullRejectionRecovery.AuthRequired:
                    return "인증 갱신 필요";
                case GachaPullRejectionRecovery.Unrecoverable:
                    return "복구 불가";
                default:
                    return recovery.ToString();
            }
        }

        public static string ForGachaPullResultKind(GachaPullResultKind kind)
        {
            switch (kind)
            {
                case GachaPullResultKind.Operator:
                    return "요원";
                case GachaPullResultKind.OperatorToken:
                    return "요원 증표";
                case GachaPullResultKind.Item:
                    return "아이템";
                case GachaPullResultKind.Currency:
                    return "재화";
                default:
                    return kind.ToString();
            }
        }

        public static string ForGachaRarity(GachaRarity rarity)
        {
            switch (rarity)
            {
                case GachaRarity.Common:
                    return "일반";
                case GachaRarity.Rare:
                    return "희귀";
                case GachaRarity.Elite:
                    return "정예";
                default:
                    return rarity.ToString();
            }
        }

        public static string ForGachaGuaranteeType(string guaranteeType, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(guaranteeType))
            {
                return fallback ?? string.Empty;
            }

            var normalized = guaranteeType.Trim();
            return GachaGuaranteeTypeLabels.TryGetValue(normalized, out var label) ? label : normalized;
        }

        public static string ForItemCode(string itemCode, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                return fallback ?? string.Empty;
            }

            var normalized = itemCode.Trim();
            return ItemCodeLabels.TryGetValue(normalized, out var label) ? label : normalized;
        }

        public static string ForBackendCode(string code, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return fallback ?? string.Empty;
            }

            var normalized = code.Trim();
            return BackendCodeLabels.TryGetValue(normalized, out var label) ? label : normalized;
        }
    }
}
