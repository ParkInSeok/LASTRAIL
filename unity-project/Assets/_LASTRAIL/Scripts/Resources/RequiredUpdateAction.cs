using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LastRail.Resources
{
    public interface ITrustedUpdateDestination
    {
        string PlayUrl { get; }
    }

    public sealed class RequiredUpdateAction
    {
        private const string RequiredNotice = "\uD544\uC218 \uC5C5\uB370\uC774\uD2B8\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4.";
        private const string MissingNotice = "\uC5C5\uB370\uC774\uD2B8 \uC8FC\uC18C\uAC00 \uC544\uC9C1 \uC124\uC815\uB418\uC9C0 \uC54A\uC558\uC2B5\uB2C8\uB2E4.";
        private const string InvalidNotice = "\uC548\uC804\uD55C \uC5C5\uB370\uC774\uD2B8 \uC8FC\uC18C\uB97C \uD655\uC778\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.";
        private const string FailedNotice = "\uC5C5\uB370\uC774\uD2B8 \uD398\uC774\uC9C0\uB97C \uC5F4\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694.";
        private const string OpenedNotice = "\uC5C5\uB370\uC774\uD2B8 \uD398\uC774\uC9C0 \uC5F4\uAE30\uB97C \uC694\uCCAD\uD588\uC2B5\uB2C8\uB2E4. \uC5C5\uB370\uC774\uD2B8\uB97C \uC644\uB8CC\uD574 \uC8FC\uC138\uC694.";
        private const string PlayPrefix = "https://play.google.com/store/apps/details?id=";
        private static readonly Regex PackageId = new Regex(@"\A[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+\z", RegexOptions.CultureInvariant);
        private readonly string url;
        private readonly string unavailableNotice;
        private readonly Func<string, bool> tryOpen;

        public RequiredUpdateAction(ITrustedUpdateDestination destination,
            IReadOnlyCollection<string> approvedExactUrls, Func<string, bool> tryOpen)
        {
            this.tryOpen = tryOpen;
            try { url = destination?.PlayUrl; }
            catch { unavailableNotice = InvalidNotice; }

            // Freeze approval at construction; later mutations cannot authorize a different target.
            var approved = false;
            if (IsCanonicalPlayUrl(url) && approvedExactUrls != null)
                foreach (var candidate in approvedExactUrls)
                    if (string.Equals(url, candidate, StringComparison.Ordinal)) { approved = true; break; }

            CanOpen = approved && tryOpen != null;
            if (unavailableNotice == null)
                unavailableNotice = string.IsNullOrEmpty(url) ? MissingNotice : !approved ? InvalidNotice : FailedNotice;
            Notice = CanOpen ? RequiredNotice : unavailableNotice;
        }

        public bool CanOpen { get; }
        public string Notice { get; private set; }

        public bool OpenFromUserClick()
        {
            if (!CanOpen) { Notice = unavailableNotice; return false; }
            try
            {
                if (tryOpen(url)) { Notice = OpenedNotice; return true; }
            }
            catch { }
            Notice = FailedNotice;
            return false;
        }

        internal static bool IsCanonicalPlayUrl(string value)
        {
            // Raw canonical form excludes URI normalization, encoded IDs, extra query and port tricks.
            if (string.IsNullOrEmpty(value) || value.Length > 2048
                || !value.StartsWith(PlayPrefix, StringComparison.Ordinal)
                || !PackageId.IsMatch(value.Substring(PlayPrefix.Length))) return false;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps && uri.Host == "play.google.com"
                && uri.IsDefaultPort && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0
                && uri.AbsolutePath == "/store/apps/details";
        }
    }
}
