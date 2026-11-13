$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Set-UnityDEnvironment.ps1')
$jdk = 'D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin'
$verifier = Join-Path $PSScriptRoot 'VerifySignedBundle.java'
$temp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$root = [IO.Path]::GetFullPath((Join-Path $temp ('lastrail-signature-fixture-' + [Guid]::NewGuid().ToString('N'))))
if (-not $root.StartsWith($temp, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid fixture directory.' }
$secret = [Guid]::NewGuid().ToString('N')

function Run([string]$Tool, [string[]]$Arguments, [bool]$ExpectSuccess = $true) {
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = Join-Path $jdk ($Tool + '.exe')
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.Environment['LASTRAIL_FIXTURE_PASSWORD'] = $secret
    foreach ($argument in $Arguments) { [void]$info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($info)
    try {
        $output = $process.StandardOutput.ReadToEndAsync()
        $errors = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        if (($process.ExitCode -eq 0) -ne $ExpectSuccess) { throw ('Unexpected fixture tool result: ' + $Tool) }
        return $output.GetAwaiter().GetResult()
    } finally { $info.Environment.Remove('LASTRAIL_FIXTURE_PASSWORD') | Out-Null; $process.Dispose() }
}

try {
    [void][IO.Directory]::CreateDirectory($root)
    $payload = Join-Path $root 'payload.txt'
    [IO.File]::WriteAllText($payload, 'Synthetic signing-verifier fixture, not an application.')
    $key = Join-Path $root 'fixture.p12'
    $unsigned = Join-Path $root 'unsigned.jar'
    $signed = Join-Path $root 'signed.jar'
    Run 'keytool' @('-genkeypair', '-keystore', $key, '-storetype', 'PKCS12', '-alias', 'fixture', '-keyalg', 'RSA', '-keysize', '2048', '-validity', '1', '-dname', 'CN=Synthetic Test Only', '-storepass:env', 'LASTRAIL_FIXTURE_PASSWORD', '-noprompt') | Out-Null
    $pem = Run 'keytool' @('-exportcert', '-rfc', '-keystore', $key, '-alias', 'fixture', '-storepass:env', 'LASTRAIL_FIXTURE_PASSWORD')
    $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new([Convert]::FromBase64String(($pem -replace '-----BEGIN CERTIFICATE-----|-----END CERTIFICATE-----|\s', '')))
    $fingerprint = $certificate.GetCertHashString([Security.Cryptography.HashAlgorithmName]::SHA256)
    $certificate.Dispose()
    Run 'jar' @('--create', '--file', $unsigned, '-C', $root, 'payload.txt') | Out-Null
    Run 'java' @($verifier, $unsigned, $fingerprint) $false | Out-Null
    Write-Output 'PASS reject unsigned archive'
    Copy-Item -LiteralPath $unsigned -Destination $signed
    Run 'jarsigner' @('-keystore', $key, '-storepass:env', 'LASTRAIL_FIXTURE_PASSWORD', $signed, 'fixture') | Out-Null
    Run 'java' @($verifier, $signed, $fingerprint) | Out-Null
    Write-Output 'PASS accept verified fixture signature'
    Run 'java' @($verifier, $signed, ('0' * 64)) $false | Out-Null
    Write-Output 'PASS reject unexpected signer'
    [IO.File]::WriteAllText($payload, 'Tampered fixture payload.')
    Run 'jar' @('--update', '--file', $signed, '-C', $root, 'payload.txt') | Out-Null
    Run 'java' @($verifier, $signed, $fingerprint) $false | Out-Null
    Write-Output 'PASS reject tampered signed payload'
} finally {
    $secret = $null
    $resolved = [IO.Path]::GetFullPath($root)
    if ($resolved.StartsWith($temp, [StringComparison]::OrdinalIgnoreCase) -and (Split-Path $resolved -Leaf) -like 'lastrail-signature-fixture-*') {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
