param(
    [switch]$OptIn,
    [string]$PacketPath = "$PSScriptRoot/../QA/DevContentWork/w627167/artifact-packet-a05.json",
    [string]$ReportPath = "$PSScriptRoot/../QA/Reports/dev-publication-check.json"
)
$ErrorActionPreference = 'Stop'
if (-not $OptIn) { throw 'Explicit -OptIn is required for public DEV HTTPS reads.' }
$packetHash = (Get-FileHash -LiteralPath $PacketPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($packetHash -ne 'dc9f0f33ec8897b41aa21273978d18f80c3e8eebfb8ca17e231ceb180f989b9e') {
    throw 'This verifier accepts only the preserved A05 packet.'
}
$packet = Get-Content -LiteralPath $PacketPath -Raw | ConvertFrom-Json
$root = 'https://dev-content.lastrail.app/android/releases/android-main-20260907-df71f4499509f8f6-w627167-a05/'
$report = [ordered]@{
    checkedAtUtc = [DateTime]::UtcNow.ToString('o')
    status = 'BLOCKED'
    scope = 'PUBLIC_HTTPS_BYTES_ONLY_NOT_ACTIVATION_OR_LOGIN'
    packetSha256 = $packetHash
    checks = @()
    reason = $null
}
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$handler.UseCookies = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(45)
try {
    foreach ($file in $packet.publicFiles) {
        $uri = [uri]$file.url
        if ($uri.Scheme -ne 'https' -or -not $uri.IsDefaultPort -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or
            -not $file.url.StartsWith($root, [StringComparison]::Ordinal)) { throw 'Packet URL is outside the fixed DEV release.' }
        $timeout = [System.Threading.CancellationTokenSource]::new(45000)
        $response = $null
        try {
            $response = $client.GetAsync($uri, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead, $timeout.Token).GetAwaiter().GetResult()
            $check = [ordered]@{ file = $file.file; httpStatus = [int]$response.StatusCode; bytesMatch = $false; sha256Match = $false }
            if ([int]$response.StatusCode -ne 200) {
                $report.checks += $check
                $report.reason = 'PUBLIC_FILE_NOT_AVAILABLE_NO_REDIRECT_FOLLOWED'
                break
            }
            $stream = $response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
            $hash = [System.Security.Cryptography.IncrementalHash]::CreateHash([System.Security.Cryptography.HashAlgorithmName]::SHA256)
            try {
                $buffer = [byte[]]::new(65536)
                [long]$count = 0
                while (($read = $stream.ReadAsync($buffer, 0, $buffer.Length, $timeout.Token).GetAwaiter().GetResult()) -gt 0) {
                    $count += $read
                    if ($count -gt [long]$file.bytes) { throw 'Remote file exceeds approved byte length.' }
                    $hash.AppendData($buffer, 0, $read)
                }
                $digest = [Convert]::ToHexString($hash.GetHashAndReset()).ToLowerInvariant()
                $check.bytesMatch = $count -eq [long]$file.bytes
                $check.sha256Match = $digest -eq $file.sha256
                $report.checks += $check
                if (-not $check.bytesMatch -or -not $check.sha256Match) {
                    $report.status = 'FAILED'
                    $report.reason = 'PUBLIC_BYTES_DIFFER_FROM_APPROVED_PACKET'
                    break
                }
            }
            finally { $stream.Dispose(); $hash.Dispose() }
        }
        finally { if ($null -ne $response) { $response.Dispose() }; $timeout.Dispose() }
    }
    if ($report.checks.Count -eq $packet.publicFiles.Count -and -not $report.reason) { $report.status = 'PUBLIC_BYTES_VERIFIED' }
}
catch {
    $report.reason = 'HTTPS_CHECK_UNAVAILABLE'
    # Never include raw response bodies, request headers or exception text in QA evidence.
}
finally {
    $client.Dispose()
    $directory = Split-Path -Parent ([IO.Path]::GetFullPath($ReportPath))
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding utf8
}
Write-Output ($report | ConvertTo-Json -Depth 5 -Compress)
if ($report.status -ne 'PUBLIC_BYTES_VERIFIED') { exit 2 }
