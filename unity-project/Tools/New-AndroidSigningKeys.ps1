param(
    [string]$KeyTool = 'D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe'
)

$ErrorActionPreference = 'Stop'
$root = 'D:\Project\LASTRAIL-Signing'
$identities = @(
    @{ Environment = 'DEV'; File = 'lastrail-dev.keystore'; Alias = 'lastrail-dev' },
    @{ Environment = 'PRD'; File = 'lastrail-prd-upload.keystore'; Alias = 'lastrail-prd-upload' }
)
if (-not (Test-Path -LiteralPath $KeyTool -PathType Leaf)) { throw 'Unity keytool is missing.' }
foreach ($parent in @('D:\', 'D:\Project', $root)) {
    if ((Test-Path -LiteralPath $parent) -and ((Get-Item -LiteralPath $parent).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Signing paths must not be reparse points.'
    }
}
foreach ($identity in $identities) {
    if (Test-Path -LiteralPath (Join-Path $root $identity.File)) { throw 'Existing signing key found; refusing replacement.' }
}
if ((Test-Path -LiteralPath $root) -and @(Get-ChildItem -LiteralPath $root -Force).Count -ne 0) {
    throw 'Signing directory is not empty. Review existing identities before provisioning.'
}

$password = Read-Host 'Enter the previously approved signing password (hidden)' -AsSecureString
$confirmation = Read-Host 'Confirm signing password (hidden)' -AsSecureString
$plain = [Net.NetworkCredential]::new('', $password).Password
$repeat = [Net.NetworkCredential]::new('', $confirmation).Password
if ($plain.Length -lt 6 -or $plain -cne $repeat) { throw 'Password confirmation failed or password is too short.' }
$repeat = $null
$lock = $null
$lockPath = Join-Path $root '.provision.lock'

function Invoke-KeyTool([string[]]$Arguments, [string]$Secret) {
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $KeyTool
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in $Arguments) { [void]$info.ArgumentList.Add($argument) }
    $info.Environment['LASTRAIL_KEYTOOL_PASSWORD'] = $Secret
    $process = [Diagnostics.Process]::Start($info)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw 'keytool failed; no raw credential or tool output is logged.' }
        return $stdout.GetAwaiter().GetResult()
    } finally {
        $info.Environment.Remove('LASTRAIL_KEYTOOL_PASSWORD') | Out-Null
        $process.Dispose()
    }
}

try {
    [void][IO.Directory]::CreateDirectory($root)
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $inheritance = [Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit'
    foreach ($sid in @([Security.Principal.WindowsIdentity]::GetCurrent().User,
        [Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($sid, 'FullControl', $inheritance, 'None', 'Allow'))
    }
    Set-Acl -LiteralPath $root -AclObject $acl
    $lock = [IO.FileStream]::new($lockPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    foreach ($identity in $identities) {
        if (Test-Path -LiteralPath (Join-Path $root $identity.File)) { throw 'Signing identity appeared during provisioning; refusing replacement.' }
    }
    $public = @()
    foreach ($identity in $identities) {
        $path = Join-Path $root $identity.File
        $candidate = Join-Path $root ('.pending-' + [Guid]::NewGuid().ToString('N') + '.p12')
        Invoke-KeyTool -Secret $plain -Arguments @('-genkeypair', '-storetype', 'PKCS12', '-keystore', $candidate,
            '-alias', $identity.Alias, '-keyalg', 'RSA', '-keysize', '3072', '-validity', '10950',
            '-dname', ('CN=LASTRAIL ' + $identity.Environment + ',O=LASTRAIL'),
            '-storepass:env', 'LASTRAIL_KEYTOOL_PASSWORD', '-keypass:env', 'LASTRAIL_KEYTOOL_PASSWORD', '-noprompt') | Out-Null
        $pem = Invoke-KeyTool -Secret $plain -Arguments @('-exportcert', '-rfc', '-keystore', $candidate,
            '-alias', $identity.Alias, '-storepass:env', 'LASTRAIL_KEYTOOL_PASSWORD')
        $base64 = ($pem -replace '-----BEGIN CERTIFICATE-----|-----END CERTIFICATE-----|\s', '')
        $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new([Convert]::FromBase64String($base64))
        try {
            [IO.File]::Move($candidate, $path)
            $public += [ordered]@{ environment = $identity.Environment; path = $path; alias = $identity.Alias
                sha1 = $certificate.Thumbprint
                sha256 = $certificate.GetCertHashString([Security.Cryptography.HashAlgorithmName]::SHA256)
                expiresUtc = $certificate.NotAfter.ToUniversalTime().ToString('o'); algorithm = 'RSA-3072'
                role = $(if ($identity.Environment -eq 'PRD') { 'Play upload key, not app signing key' } else { 'DEV signing key' }) }
        } finally { $certificate.Dispose() }
    }
    if ($public[0].sha256 -eq $public[1].sha256) { throw 'Certificates must differ.' }
    $report = Join-Path $root 'public-certificates.json'
    if (Test-Path -LiteralPath $report) { throw 'Public report already exists.' }
    $public | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $report -Encoding utf8
    Write-Output ('Verified independent keys. Public certificate report: ' + $report)
    Write-Output 'Only one local copy exists. Secure backup is NOT complete.'
} finally {
    if ($null -ne $lock) { $lock.Dispose(); Remove-Item -LiteralPath $lockPath -Force }
    $plain = $null
    $repeat = $null
    $password.Dispose()
    $confirmation.Dispose()
}
