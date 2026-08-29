using System;

namespace LastRail.Gacha
{
    public sealed class GachaScreenPullButtonState
    {
        public GachaScreenPullButtonState(GachaPullCount pullCount, bool isEnabled)
        {
            PullCount = GachaPullCount.FromValue(pullCount.Value);
            IsEnabled = isEnabled;
        }

        public GachaPullCount PullCount { get; }
        public bool IsEnabled { get; }
        public string BodyJsonPreview => "{\"pullCount\":" + PullCount.Value + "}";
        public bool RequiresIdempotencyKey => true;
        public bool HasAccountShortcut => false;

        public static GachaScreenPullButtonState Enabled(int pullCount)
        {
            return new GachaScreenPullButtonState(GachaPullCount.FromValue(pullCount), true);
        }

        public static GachaScreenPullButtonState Disabled(int pullCount)
        {
            return new GachaScreenPullButtonState(GachaPullCount.FromValue(pullCount), false);
        }
    }
}
