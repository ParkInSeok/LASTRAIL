using System;
using System.Collections.Generic;
using LastRail.Localization;

namespace LastRail.Gacha
{
    public sealed class GachaScreenViewState
    {
        public const int LaunchMinimumTouchTargetDp = 48;
        public const int PortraitAspectWidth = 9;
        public const int PortraitAspectHeight = 16;

        public GachaScreenViewState(GachaScreenPresentationState screen)
        {
            if (screen == null)
            {
                throw new ArgumentNullException(nameof(screen));
            }

            Kind = screen.Kind;
            ClientOutcome = screen.ClientOutcome;
            Title = "요원 모집";
            StatusLabel = screen.IsIdle ? "모집 대기" : UnityDisplayLabels.ForGachaPullUxStateKind(screen.Kind);
            BannerId = OptionalText(screen.BannerId);
            BannerDisplayName = RequireText(screen.BannerDisplayName, nameof(screen.BannerDisplayName));
            CostLabel = BuildCostLabel(screen.Cost);
            OnePullButtonLabel = BuildPullButtonLabel(screen.OnePullButton);
            TenPullButtonLabel = BuildPullButtonLabel(screen.TenPullButton);
            IsOnePullEnabled = screen.OnePullButton.IsEnabled;
            IsTenPullEnabled = screen.TenPullButton.IsEnabled;
            PitySummaryLines = BuildPitySummary(screen.PostPityState);
            RevealMode = ResolveRevealMode(screen);
            PrimaryCueId = ResolvePrimaryCueId(screen);
            ResultGridEntries = BuildGridEntries(screen);
            ErrorCode = OptionalText(screen.ErrorCode);
            ErrorLabel = UnityDisplayLabels.ForBackendCode(screen.ErrorCode, null);
            ErrorMessage = OptionalText(screen.ErrorMessage);
            CanRetryWithSameIdempotencyKey = screen.CanRetryWithSameIdempotencyKey;
            RequiresAuthRefreshOrGuestLogin = screen.RequiresAuthRefreshOrGuestLogin;
            PrimaryActionLabel = ResolvePrimaryActionLabel(screen);
        }

        public GachaPullUxStateKind Kind { get; }
        public GachaPullClientOutcome ClientOutcome { get; }
        public string Title { get; }
        public string StatusLabel { get; }
        public string BannerId { get; }
        public string BannerDisplayName { get; }
        public string CostLabel { get; }
        public string OnePullButtonLabel { get; }
        public string TenPullButtonLabel { get; }
        public bool IsOnePullEnabled { get; }
        public bool IsTenPullEnabled { get; }
        public IReadOnlyList<string> PitySummaryLines { get; }
        public string RevealMode { get; }
        public string PrimaryCueId { get; }
        public IReadOnlyList<GachaScreenResultGridEntryViewState> ResultGridEntries { get; }
        public string ErrorCode { get; }
        public string ErrorLabel { get; }
        public string ErrorMessage { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public string PrimaryActionLabel { get; }
        public bool UsesPortraitSafeArea => true;
        public int MinimumTouchTargetDp => LaunchMinimumTouchTargetDp;
        public int PreferredAspectWidth => PortraitAspectWidth;
        public int PreferredAspectHeight => PortraitAspectHeight;
        public bool UsesServerResultDisplayFallback => true;
        public bool HasOperatorRosterTruth => false;
        public bool IsPresentationOnly => true;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        private static IReadOnlyList<string> BuildPitySummary(GachaPityDisplayState postPityState)
        {
            var lines = new List<string>();
            if (postPityState == null)
            {
                return lines.AsReadOnly();
            }

            lines.Add("희귀 이상 보장까지 " + postPityState.PullsUntilRareOrHigherGuarantee.ToString(System.Globalization.CultureInfo.InvariantCulture) + "회");
            lines.Add("정예 확정까지 " + postPityState.PullsUntilEliteGuarantee.ToString(System.Globalization.CultureInfo.InvariantCulture) + "회");
            lines.Add("픽업 정예 확정까지 " + postPityState.PullsUntilPickupEliteGuarantee.ToString(System.Globalization.CultureInfo.InvariantCulture) + "회");
            return lines.AsReadOnly();
        }

        private static IReadOnlyList<GachaScreenResultGridEntryViewState> BuildGridEntries(GachaScreenPresentationState screen)
        {
            var entries = new List<GachaScreenResultGridEntryViewState>();
            foreach (var entry in screen.RevealEntries)
            {
                entries.Add(new GachaScreenResultGridEntryViewState(entry));
            }

            return entries.AsReadOnly();
        }

        private static string BuildCostLabel(GachaPullCostDto cost)
        {
            if (cost == null)
            {
                return "비용 정보 없음";
            }

            return UnityDisplayLabels.ForItemCode(cost.ItemId, cost.ItemId) + " x" + cost.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string BuildPullButtonLabel(GachaScreenPullButtonState button)
        {
            return button.PullCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "회 모집";
        }

        private static string ResolveRevealMode(GachaScreenPresentationState screen)
        {
            if (screen.IsIdle)
            {
                return "Idle";
            }

            if (screen.RevealEntries.Count >= 10)
            {
                return "QuickTenPullGrid";
            }

            if (screen.RevealEntries.Count == 1)
            {
                return "SingleCardReveal";
            }

            return "NoReveal";
        }

        private static string ResolvePrimaryCueId(GachaScreenPresentationState screen)
        {
            return screen.RevealEntries.Count == 0 ? null : screen.RevealEntries[0].RevealStartCueId;
        }

        private static string ResolvePrimaryActionLabel(GachaScreenPresentationState screen)
        {
            if (screen.CanRetryWithSameIdempotencyKey)
            {
                return "같은 요청으로 재시도";
            }

            if (screen.RequiresAuthRefreshOrGuestLogin)
            {
                return "로그인 확인";
            }

            if (screen.Kind == GachaPullUxStateKind.RecoverableRejected)
            {
                return "모집 조건 확인";
            }

            return screen.IsIdle ? "모집 준비" : "결과 확인";
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
