using System;
using System.Globalization;
using LastRail.Presentation.AuthGate;

namespace LastRail.Presentation.Startup
{
    public enum StartupScreenKind { Auth, Waiting, Downloading, DownloadError, UpdateNotice }

    public sealed class StartupScreenViewState
    {
        private StartupScreenViewState(StartupScreenKind kind, string title, string details,
            string primary, string secondary, float? progress, string progressLabel, int icon, string phaseKey)
        {
            Kind = kind; Title = title; Details = details;
            PrimaryLabel = primary; SecondaryLabel = secondary;
            Progress = progress; ProgressLabel = progressLabel; IconIndex = icon; PhaseKey = phaseKey;
        }

        public StartupScreenKind Kind { get; }
        public string Title { get; }
        public string Details { get; }
        public string PrimaryLabel { get; }
        public string SecondaryLabel { get; }
        public float? Progress { get; }
        public string ProgressLabel { get; }
        public int IconIndex { get; }
        public string PhaseKey { get; }
        public bool IsBusy => Kind == StartupScreenKind.Waiting || Kind == StartupScreenKind.Downloading;

        public static StartupScreenViewState FromAuth(AuthGateUxState auth)
        {
            if (auth == null) throw new ArgumentNullException(nameof(auth));
            return new StartupScreenViewState(StartupScreenKind.Auth, auth.Title, string.Empty,
                auth.PrimaryActionLabel, auth.SecondaryActionLabel, null, null,
                auth.Kind >= AuthGateUxStateKind.AuthRequired ? 1 : 4, "auth:" + auth.Kind);
        }

        public static StartupScreenViewState Waiting() => new StartupScreenViewState(
            StartupScreenKind.Waiting, "\uC900\uBE44 \uC911", string.Empty, null, null, null, null, 0, "waiting");

        public static StartupScreenViewState Downloading(long receivedBytes, long? totalBytes)
        {
            if (receivedBytes < 0 || (totalBytes.HasValue && (totalBytes.Value <= 0 || receivedBytes > totalBytes.Value)))
                throw new ArgumentOutOfRangeException(nameof(receivedBytes), "Download progress must come from valid reported byte counts.");
            var fraction = totalBytes.HasValue ? (float?)((double)receivedBytes / totalBytes.Value) : null;
            var amount = (receivedBytes / 1048576d).ToString("0.0", CultureInfo.InvariantCulture) + " MiB";
            if (totalBytes.HasValue)
                amount = ((int)(100d * receivedBytes / totalBytes.Value)).ToString(CultureInfo.InvariantCulture) + "%  |  "
                    + amount + " / " + (totalBytes.Value / 1048576d).ToString("0.0", CultureInfo.InvariantCulture) + " MiB";
            return new StartupScreenViewState(StartupScreenKind.Downloading,
                "\uB9AC\uC18C\uC2A4 \uB2E4\uC6B4\uB85C\uB4DC", string.Empty, null, null, fraction, amount, 0, "downloading");
        }

        public static StartupScreenViewState DownloadError(string message) => new StartupScreenViewState(
            StartupScreenKind.DownloadError, "\uB2E4\uC6B4\uB85C\uB4DC \uC624\uB958", message ?? string.Empty,
            "\uB2E4\uC2DC \uC2DC\uB3C4", null, null, null, 1, "download_error");

        public static StartupScreenViewState UpdateNotice(string versionLabel, string notice) => new StartupScreenViewState(
            StartupScreenKind.UpdateNotice, "\uC5C5\uB370\uC774\uD2B8 \uC548\uB0B4",
            (versionLabel ?? string.Empty) + "\n" + (notice ?? string.Empty), "\uD655\uC778", null, null, null, 3, "update_notice");
    }
}
