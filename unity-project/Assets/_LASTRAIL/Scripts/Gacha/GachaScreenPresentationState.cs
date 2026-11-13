using System;
using System.Collections.Generic;

namespace LastRail.Gacha
{
    public sealed class GachaScreenPresentationState
    {
        private const string DefaultBannerDisplayName = "모집 배너";

        private GachaScreenPresentationState(
            GachaPullUxStateKind kind,
            GachaPullClientOutcome clientOutcome,
            string bannerId,
            string bannerDisplayName,
            GachaScreenPullButtonState onePullButton,
            GachaScreenPullButtonState tenPullButton,
            GachaPullCostDto cost,
            GachaPityDisplayState postPityState,
            GachaPullBatchPresentation batch,
            IEnumerable<GachaScreenRevealEntryPresentation> revealEntries,
            GachaPullRejectionRecovery rejectionRecovery,
            string errorCode,
            string errorMessage,
            bool canRetryWithSameIdempotencyKey,
            bool requiresAuthRefreshOrGuestLogin,
            bool isIdle)
        {
            Kind = kind;
            ClientOutcome = clientOutcome;
            BannerId = OptionalText(bannerId);
            BannerDisplayName = ResolveBannerDisplayName(bannerDisplayName, BannerId);
            OnePullButton = onePullButton ?? throw new ArgumentNullException(nameof(onePullButton));
            TenPullButton = tenPullButton ?? throw new ArgumentNullException(nameof(tenPullButton));
            Cost = cost;
            PostPityState = postPityState;
            Batch = batch;
            RevealEntries = CopyRevealEntries(revealEntries);
            RejectionRecovery = rejectionRecovery;
            ErrorCode = OptionalText(errorCode);
            ErrorMessage = OptionalText(errorMessage);
            CanRetryWithSameIdempotencyKey = canRetryWithSameIdempotencyKey;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            IsIdle = isIdle;
        }

        public GachaPullUxStateKind Kind { get; }
        public GachaPullClientOutcome ClientOutcome { get; }
        public string BannerId { get; }
        public string BannerDisplayName { get; }
        public GachaScreenPullButtonState OnePullButton { get; }
        public GachaScreenPullButtonState TenPullButton { get; }
        public GachaPullCostDto Cost { get; }
        public GachaPityDisplayState PostPityState { get; }
        public GachaPullBatchPresentation Batch { get; }
        public IReadOnlyList<GachaScreenRevealEntryPresentation> RevealEntries { get; }
        public GachaPullRejectionRecovery RejectionRecovery { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool IsIdle { get; }
        public bool HasBatch => Batch != null;
        public bool IsPresentationOnly => true;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static GachaScreenPresentationState Idle(
            string bannerId,
            string bannerDisplayName,
            GachaPullCostDto cost,
            GachaPityDisplayState postPityState)
        {
            RequireText(bannerId, nameof(bannerId));
            if (cost == null)
            {
                throw new ArgumentNullException(nameof(cost));
            }

            if (postPityState == null)
            {
                throw new ArgumentNullException(nameof(postPityState));
            }

            return new GachaScreenPresentationState(
                GachaPullUxStateKind.ResultReviewReady,
                GachaPullClientOutcome.Accepted,
                bannerId,
                bannerDisplayName,
                GachaScreenPullButtonState.Enabled(1),
                GachaScreenPullButtonState.Enabled(10),
                cost,
                postPityState,
                null,
                Array.Empty<GachaScreenRevealEntryPresentation>(),
                GachaPullRejectionRecovery.None,
                null,
                null,
                false,
                false,
                true);
        }

        public static GachaScreenPresentationState FromUxState(
            GachaPullUxState uxState,
            string bannerDisplayName)
        {
            if (uxState == null)
            {
                throw new ArgumentNullException(nameof(uxState));
            }

            var revealEntries = new List<GachaScreenRevealEntryPresentation>();
            if (uxState.Batch != null)
            {
                foreach (var result in uxState.Batch.Results)
                {
                    revealEntries.Add(new GachaScreenRevealEntryPresentation(
                        result,
                        GachaRevealPresentationCue.FromPresentation(result)));
                }
            }

            return new GachaScreenPresentationState(
                uxState.Kind,
                uxState.ClientOutcome,
                ResolveBannerId(uxState),
                bannerDisplayName,
                GachaScreenPullButtonState.Enabled(1),
                GachaScreenPullButtonState.Enabled(10),
                uxState.Cost,
                uxState.PostPityState,
                uxState.Batch,
                revealEntries,
                uxState.RejectionRecovery,
                uxState.ErrorCode,
                uxState.ErrorMessage,
                uxState.CanRetryWithSameIdempotencyKey,
                uxState.RequiresAuthRefreshOrGuestLogin,
                false);
        }

        private static string ResolveBannerId(GachaPullUxState uxState)
        {
            if (uxState.Batch != null)
            {
                return uxState.Batch.BannerId;
            }

            if (uxState.PostPityState != null)
            {
                return uxState.PostPityState.BannerId;
            }

            return null;
        }

        private static string ResolveBannerDisplayName(string bannerDisplayName, string bannerId)
        {
            if (!string.IsNullOrWhiteSpace(bannerDisplayName))
            {
                return bannerDisplayName.Trim();
            }

            return string.IsNullOrWhiteSpace(bannerId) ? DefaultBannerDisplayName : bannerId;
        }

        private static IReadOnlyList<GachaScreenRevealEntryPresentation> CopyRevealEntries(
            IEnumerable<GachaScreenRevealEntryPresentation> revealEntries)
        {
            if (revealEntries == null)
            {
                throw new ArgumentNullException(nameof(revealEntries));
            }

            var copy = new List<GachaScreenRevealEntryPresentation>();
            foreach (var entry in revealEntries)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Reveal entry cannot be null.", nameof(revealEntries));
                }

                copy.Add(entry);
            }

            return copy.AsReadOnly();
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
