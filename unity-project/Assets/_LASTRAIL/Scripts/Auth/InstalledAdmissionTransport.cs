using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using LastRail.Diagnostics;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class InstalledAdmissionTransport : IRunApiHttpTransport
    {
        private readonly IRunApiHttpTransport inner;
        private readonly string version, code, platform, channel;
        private readonly Func<string> contentRelease;
        public InstalledAdmissionTransport(IRunApiHttpTransport inner, AppBuildSnapshot installed, string platform, string channel)
            : this(inner,installed,platform,channel,null) { }
        public InstalledAdmissionTransport(IRunApiHttpTransport inner, AppBuildSnapshot installed, string platform, string channel, Func<string> contentRelease)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            var value = installed?.AppVersionCode;
            version = installed?.AppVersion;
            code = value > 0 && value <= int.MaxValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null;
            this.platform = Label(platform); this.channel = Label(channel);
            this.contentRelease = contentRelease;
        }
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (code == null) return Task.FromResult(RunApiHttpResponse.NetworkError("Installed app admission is unavailable."));
            var headers = new List<RunApiHttpHeader>();
            foreach (var header in request.Headers)
            {
                if (header.Name.StartsWith("X-LastRail-App-", StringComparison.OrdinalIgnoreCase) ||
                    header.Name.Equals("X-LastRail-Content-Release-Id",StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(RunApiHttpResponse.NetworkError("Installed admission headers cannot be overridden."));
                headers.Add(header);
            }
            headers.Add(new RunApiHttpHeader("X-LastRail-App-Version-Code", code));
            if (version != null) headers.Add(new RunApiHttpHeader("X-LastRail-App-Version", version));
            if (platform != null) headers.Add(new RunApiHttpHeader("X-LastRail-App-Platform", platform));
            if (channel != null) headers.Add(new RunApiHttpHeader("X-LastRail-App-Channel", channel));
            var releaseId=contentRelease?.Invoke();
            if (!string.IsNullOrEmpty(releaseId)) headers.Add(new RunApiHttpHeader("X-LastRail-Content-Release-Id",releaseId));
            return inner.SendAsync(new RunApiHttpRequest(request.Method, request.EndpointPath, request.BodyJson, headers));
        }
        private static string Label(string value)
        {
            if (value == null) return null;
            if (value.Length == 0 || value.Length > 64) throw new ArgumentException("Invalid installed build label.");
            foreach (var c in value)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9')
                    && c != '.' && c != '_' && c != ':' && c != '-') throw new ArgumentException("Invalid installed build label.");
            return value;
        }
    }
}
