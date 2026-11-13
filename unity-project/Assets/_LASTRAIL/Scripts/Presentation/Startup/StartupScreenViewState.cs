using System;
using System.Globalization;
using LastRail.Presentation.AuthGate;

namespace LastRail.Presentation.Startup
{
    public enum StartupScreenKind { Auth, Waiting, Downloading, DownloadError, UpdateNotice, DownloadConsent, InternalAccessDenied }

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

        public StartupScreenViewState WithContent(string label, float? progress) => new StartupScreenViewState(
            Kind, Title, Details, PrimaryLabel, SecondaryLabel, progress, label, IconIndex, PhaseKey);

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

        public static StartupScreenViewState NetworkInterrupted(string message) => new StartupScreenViewState(
            StartupScreenKind.DownloadError, "연결이 잠시 끊겼습니다",
            (message ?? "네트워크 상태를 확인한 뒤 다시 시도해 주세요.") + "\n\n확인된 리소스는 다시 사용합니다.\n필요한 부분을 확인한 뒤 이어갑니다.",
            "다시 시도", "로그인 화면으로", null, null, 1, "network_interrupted");

        public static StartupScreenViewState DownloadConsent(long bytes) => new StartupScreenViewState(
            StartupScreenKind.DownloadConsent, "\uB9AC\uC18C\uC2A4 \uB2E4\uC6B4\uB85C\uB4DC",
            (bytes / 1048576d).ToString("0.0", CultureInfo.InvariantCulture) + " MiB\n\uB124\uD2B8\uC6CC\uD06C \uC0AC\uC6A9\uB7C9\uC744 \uD655\uC778\uD574 \uC8FC\uC138\uC694.",
            "\uB2E4\uC6B4\uB85C\uB4DC", "\uCDE8\uC18C", null, null, 0, "download_consent");

        public static StartupScreenViewState UpdateNotice(string versionLabel, string notice) => new StartupScreenViewState(
            StartupScreenKind.UpdateNotice, "새로운 여정을\n준비하세요",
            "계속 플레이하려면 최신 버전이 필요합니다.\n\n" + (notice ?? string.Empty), "Google Play에서 업데이트", "종료", null, null, 3, "update_notice");

        public static StartupScreenViewState InternalAccessDenied() => new StartupScreenViewState(
            StartupScreenKind.InternalAccessDenied, "테스트 계정을 확인해 주세요",
            "이 버전은 내부테스트 참가 계정으로 이용할 수 있습니다.\n\n현재 계정으로는 테스트 콘텐츠에 접근할 수 없습니다.",
            "로그인 화면으로", "종료", null, "권한을 확인하기 전에는 다운로드하지 않습니다.", 1, "internal_access_denied");
        public static StartupScreenViewState SessionExpired() => new StartupScreenViewState(
            StartupScreenKind.DownloadError, "로그인 정보를\n확인해 주세요", "로그인 정보가 만료되었습니다. 다시 로그인해 주세요.",
            "로그인 화면으로", "종료", null, null, 1, "content_session_expired");
    }
}
