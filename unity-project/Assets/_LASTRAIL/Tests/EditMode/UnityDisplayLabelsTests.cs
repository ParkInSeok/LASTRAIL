using LastRail.Client;
using LastRail.Gacha;
using LastRail.Localization;
using LastRail.Run;
using LastRail.Sortie;
using LastRail.Tactics;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class UnityDisplayLabelsTests
    {
        [Test]
        public void EnumLabels_ReturnKoreanUserFacingText()
        {
            Assert.That(UnityDisplayLabels.ForClientEnvironment(ClientEnvironment.Dev), Is.EqualTo("개발"));
            Assert.That(UnityDisplayLabels.ForClientEnvironment(ClientEnvironment.Prd), Is.EqualTo("운영"));
            Assert.That(UnityDisplayLabels.ForRunResultOutcome(RunResultOutcome.Victory), Is.EqualTo("승리"));
            Assert.That(UnityDisplayLabels.ForRunResultOutcome(RunResultOutcome.Failure), Is.EqualTo("실패"));
            Assert.That(UnityDisplayLabels.ForRunResultOutcome(RunResultOutcome.Abandoned), Is.EqualTo("포기"));
            Assert.That(UnityDisplayLabels.ForTacticKind(TacticKind.Command), Is.EqualTo("지휘전술"));
            Assert.That(UnityDisplayLabels.ForTacticKind(TacticKind.Ultimate), Is.EqualTo("얼티밋 전술"));
            Assert.That(UnityDisplayLabels.ForSortieStage(SortieStage.Wave), Is.EqualTo("공세"));
            Assert.That(UnityDisplayLabels.ForSortieStage(SortieStage.FinalOperation), Is.EqualTo("최종 작전"));
            Assert.That(UnityDisplayLabels.ForSortieStage(SortieStage.Complete), Is.EqualTo("완료"));
        }

        [Test]
        public void RunFlowLabels_ReturnKoreanUserFacingText()
        {
            Assert.That(UnityDisplayLabels.ForSubmitAction(RunApiFlowSubmitAction.ShowRewardReview), Is.EqualTo("보상 확인"));
            Assert.That(UnityDisplayLabels.ForSubmitAction(RunApiFlowSubmitAction.SyncRecognizedResult), Is.EqualTo("제출 결과 동기화"));
            Assert.That(UnityDisplayLabels.ForSubmitAction(RunApiFlowSubmitAction.ShowRecoverableRejected), Is.EqualTo("복구 가능 오류"));
            Assert.That(UnityDisplayLabels.ForSubmitAction(RunApiFlowSubmitAction.ShowFatalRejected), Is.EqualTo("치명 오류"));
            Assert.That(UnityDisplayLabels.ForSubmitAction(RunApiFlowSubmitAction.RetryWithSameIdempotencyKey), Is.EqualTo("재시도"));
            Assert.That(UnityDisplayLabels.ForRunEndUxStateKind(RunEndUxStateKind.RewardReviewReady), Is.EqualTo("보상 확인 준비"));
            Assert.That(UnityDisplayLabels.ForRunEndUxStateKind(RunEndUxStateKind.RecognizedDuplicate), Is.EqualTo("중복 제출 확인"));
            Assert.That(UnityDisplayLabels.ForRunEndUxStateKind(RunEndUxStateKind.RecoverableRejected), Is.EqualTo("복구 가능 제출 거부"));
            Assert.That(UnityDisplayLabels.ForRunEndUxStateKind(RunEndUxStateKind.FatalRejected), Is.EqualTo("제출 거부"));
            Assert.That(UnityDisplayLabels.ForRunEndUxStateKind(RunEndUxStateKind.RetryableNetworkError), Is.EqualTo("네트워크 재시도 필요"));
        }

        [Test]
        public void RiskRewardLabels_ReturnKoreanDisplayTextWithoutCalculatingRewards()
        {
            Assert.That(UnityDisplayLabels.ForRiskLevel(1), Is.EqualTo("위험도 I"));
            Assert.That(UnityDisplayLabels.ForRiskLevel(2), Is.EqualTo("위험도 II"));
            Assert.That(UnityDisplayLabels.ForRiskLevel(3), Is.EqualTo("위험도 III"));
            Assert.That(UnityDisplayLabels.ForRiskLevel(4), Is.EqualTo("위험도 IV"));
            Assert.That(UnityDisplayLabels.ForRiskLevel(5), Is.EqualTo("위험도 V"));
            Assert.That(UnityDisplayLabels.ForRiskLevel(0), Is.EqualTo("위험도 0"));
            Assert.That(UnityDisplayLabels.ForFirstClearBonus(true), Is.EqualTo("첫 클리어 보너스"));
            Assert.That(UnityDisplayLabels.ForFirstClearBonus(false), Is.EqualTo("첫 클리어 보너스 없음"));
        }

        [Test]
        public void OfflineRewardStoryProgressLabels_ReturnActOneStoryProgressTextWithoutCalculatingRewards()
        {
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("none"), Is.EqualTo("스토리 진행 없음"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("opn_01_last_departure"), Is.EqualTo("마지막 출발"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("opn_02_first_signal"), Is.EqualTo("첫 신호"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("opn_03_water_stop"), Is.EqualTo("급수 정차"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("opn_04_scrapstar_show"), Is.EqualTo("스크랩스타 쇼"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("opn_05_night_boarders"), Is.EqualTo("야간 승선전"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("opn_06_broken_line"), Is.EqualTo("끊어진 선로"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("raid_raider_king_engine"), Is.EqualTo("약탈왕 기관차"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("future_story_node"), Is.EqualTo("future_story_node"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress("   future_story_node   "), Is.EqualTo("future_story_node"));
            Assert.That(UnityDisplayLabels.ForActOneStoryProgress(null, "알 수 없음"), Is.EqualTo("알 수 없음"));
        }

        [Test]
        public void OfflineRewardCapLabel_FormatsServerProvidedCapHoursWithoutCalculatingCap()
        {
            Assert.That(UnityDisplayLabels.ForOfflineRewardCapHours(4), Is.EqualTo("최대 4시간"));
            Assert.That(UnityDisplayLabels.ForOfflineRewardCapHours(12), Is.EqualTo("최대 12시간"));
            Assert.That(UnityDisplayLabels.ForOfflineRewardCapHours(0), Is.EqualTo("최대 0시간"));
        }

        [Test]
        public void GachaLabels_ReturnKoreanUserFacingText()
        {
            Assert.That(UnityDisplayLabels.ForGachaPullUxStateKind(GachaPullUxStateKind.ResultReviewReady), Is.EqualTo("모집 결과 확인"));
            Assert.That(UnityDisplayLabels.ForGachaPullUxStateKind(GachaPullUxStateKind.RecognizedDuplicate), Is.EqualTo("이전 모집 결과 동기화"));
            Assert.That(UnityDisplayLabels.ForGachaPullUxStateKind(GachaPullUxStateKind.RecoverableRejected), Is.EqualTo("복구 가능 모집 오류"));
            Assert.That(UnityDisplayLabels.ForGachaPullUxStateKind(GachaPullUxStateKind.AuthRequired), Is.EqualTo("인증 필요"));
            Assert.That(UnityDisplayLabels.ForGachaPullUxStateKind(GachaPullUxStateKind.FatalRejected), Is.EqualTo("모집 거부"));
            Assert.That(UnityDisplayLabels.ForGachaPullUxStateKind(GachaPullUxStateKind.RetryableNetworkError), Is.EqualTo("네트워크 재시도 필요"));
            Assert.That(UnityDisplayLabels.ForGachaPullRejectionRecovery(GachaPullRejectionRecovery.None), Is.EqualTo("조치 없음"));
            Assert.That(UnityDisplayLabels.ForGachaPullRejectionRecovery(GachaPullRejectionRecovery.Recoverable), Is.EqualTo("복구 가능"));
            Assert.That(UnityDisplayLabels.ForGachaPullRejectionRecovery(GachaPullRejectionRecovery.AuthRequired), Is.EqualTo("인증 갱신 필요"));
            Assert.That(UnityDisplayLabels.ForGachaPullRejectionRecovery(GachaPullRejectionRecovery.Unrecoverable), Is.EqualTo("복구 불가"));
            Assert.That(UnityDisplayLabels.ForGachaPullResultKind(GachaPullResultKind.Operator), Is.EqualTo("요원"));
            Assert.That(UnityDisplayLabels.ForGachaPullResultKind(GachaPullResultKind.OperatorToken), Is.EqualTo("요원 증표"));
            Assert.That(UnityDisplayLabels.ForGachaPullResultKind(GachaPullResultKind.Item), Is.EqualTo("아이템"));
            Assert.That(UnityDisplayLabels.ForGachaPullResultKind(GachaPullResultKind.Currency), Is.EqualTo("재화"));
            Assert.That(UnityDisplayLabels.ForGachaRarity(GachaRarity.Common), Is.EqualTo("일반"));
            Assert.That(UnityDisplayLabels.ForGachaRarity(GachaRarity.Rare), Is.EqualTo("희귀"));
            Assert.That(UnityDisplayLabels.ForGachaRarity(GachaRarity.Elite), Is.EqualTo("정예"));
        }

        [Test]
        public void GachaBackendCodes_ReturnKoreanLabelsAndExposeUnknownCodes()
        {
            Assert.That(UnityDisplayLabels.ForGachaGuaranteeType("NONE"), Is.EqualTo("보장 없음"));
            Assert.That(UnityDisplayLabels.ForGachaGuaranteeType("RARE_OR_HIGHER_10"), Is.EqualTo("10회 희귀 이상 보장"));
            Assert.That(UnityDisplayLabels.ForGachaGuaranteeType("ELITE_30"), Is.EqualTo("30뽑 정예 확정"));
            Assert.That(UnityDisplayLabels.ForGachaGuaranteeType("PICKUP_ELITE_60"), Is.EqualTo("60뽑 픽업 정예 확정"));
            Assert.That(UnityDisplayLabels.ForGachaGuaranteeType("FUTURE_GUARANTEE"), Is.EqualTo("FUTURE_GUARANTEE"));
            Assert.That(UnityDisplayLabels.ForGachaGuaranteeType("   FUTURE_GUARANTEE   "), Is.EqualTo("FUTURE_GUARANTEE"));
            Assert.That(UnityDisplayLabels.ForGachaGuaranteeType(null, "알 수 없음"), Is.EqualTo("알 수 없음"));
            Assert.That(UnityDisplayLabels.ForBackendCode("insufficient_command_ticket"), Is.EqualTo("지휘권 부족"));
            Assert.That(UnityDisplayLabels.ForBackendCode("auth_required"), Is.EqualTo("인증 필요"));
            Assert.That(UnityDisplayLabels.ForBackendCode("banner_inactive"), Is.EqualTo("비활성 모집 배너"));
            Assert.That(UnityDisplayLabels.ForBackendCode("invalid_pull_count"), Is.EqualTo("잘못된 모집 횟수"));
            Assert.That(UnityDisplayLabels.ForBackendCode("gacha_pool_unavailable"), Is.EqualTo("모집 풀 사용 불가"));
            Assert.That(UnityDisplayLabels.ForBackendCode("gacha_state_conflict"), Is.EqualTo("모집 상태 충돌"));
            Assert.That(UnityDisplayLabels.ForBackendCode("gacha_temporarily_unavailable"), Is.EqualTo("모집 일시 사용 불가"));
        }

        [Test]
        public void ItemCodeLabels_ReturnKoreanLabelsAndExposeUnknownCodes()
        {
            Assert.That(UnityDisplayLabels.ForItemCode("cur_command_ticket"), Is.EqualTo("지휘권"));
            Assert.That(UnityDisplayLabels.ForItemCode("cur_command_ticket_fragment"), Is.EqualTo("지휘권 조각"));
            Assert.That(UnityDisplayLabels.ForItemCode("cur_special_core"), Is.EqualTo("특수 코어"));
            Assert.That(UnityDisplayLabels.ForItemCode("cur_operator_token"), Is.EqualTo("요원 증표"));
            Assert.That(UnityDisplayLabels.ForItemCode("future_item"), Is.EqualTo("future_item"));
            Assert.That(UnityDisplayLabels.ForItemCode("   future_item   "), Is.EqualTo("future_item"));
            Assert.That(UnityDisplayLabels.ForItemCode(null, "알 수 없음"), Is.EqualTo("알 수 없음"));
        }

        [Test]
        public void BackendCodeLabels_MapKnownCodesAndExposeUnknownCodes()
        {
            Assert.That(UnityDisplayLabels.ForBackendCode("ACCEPTED"), Is.EqualTo("수락"));
            Assert.That(UnityDisplayLabels.ForBackendCode("DUPLICATE_REPLAY"), Is.EqualTo("중복 제출"));
            Assert.That(UnityDisplayLabels.ForBackendCode("REJECTED"), Is.EqualTo("거부"));
            Assert.That(UnityDisplayLabels.ForBackendCode("contract_violation"), Is.EqualTo("계약 위반"));
            Assert.That(UnityDisplayLabels.ForBackendCode("duplicate_replay"), Is.EqualTo("중복 제출"));
            Assert.That(UnityDisplayLabels.ForBackendCode("future_backend_code"), Is.EqualTo("future_backend_code"));
            Assert.That(UnityDisplayLabels.ForBackendCode("   future_backend_code   "), Is.EqualTo("future_backend_code"));
        }

        [Test]
        public void BackendCodeLabels_UseFallbackOnlyWhenCodeIsEmpty()
        {
            Assert.That(UnityDisplayLabels.ForBackendCode(null, "알 수 없음"), Is.EqualTo("알 수 없음"));
            Assert.That(UnityDisplayLabels.ForBackendCode(" ", "알 수 없음"), Is.EqualTo("알 수 없음"));
            Assert.That(UnityDisplayLabels.ForBackendCode("unknown_code", "알 수 없음"), Is.EqualTo("unknown_code"));
        }
    }
}
