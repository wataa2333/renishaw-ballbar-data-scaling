param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = [IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
$resourceDirectory = Join-Path $projectRoot "Resources"
$distributionDirectory = Join-Path $projectRoot "dist"
$libraryDirectory = Join-Path $projectRoot "Lib"
$assetDirectory = Join-Path $projectRoot "Assets"

New-Item -ItemType Directory -Path $resourceDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $distributionDirectory -Force | Out-Null

function Get-SecretBytes {
    [byte[]]$fragmentA = 0x9E,0x63,0x1F,0x0C,0x27,0xBE,0x19,0x5A,0x18,0x6E
    [byte[]]$fragmentB = 0xA3,0x5C,0x91,0x27,0xE8,0x4D,0x36,0xB2,0x7F,0x19
    [byte[]]$result = New-Object byte[] $fragmentA.Length
    for ($index = 0; $index -lt $result.Length; $index++) {
        $result[$index] = $fragmentA[$index] -bxor $fragmentB[$index]
    }
    return $result
}

function Get-ResourceKeys {
    [byte[]]$secret = Get-SecretBytes
    [byte[]]$context = [Text.Encoding]::UTF8.GetBytes("Wataa.QC20.Resource.v1")
    [byte[]]$combined = New-Object byte[] ($secret.Length + $context.Length)
    [Buffer]::BlockCopy($secret, 0, $combined, 0, $secret.Length)
    [Buffer]::BlockCopy($context, 0, $combined, $secret.Length, $context.Length)
    $sha = [Security.Cryptography.SHA512]::Create()
    try {
        [byte[]]$digest = $sha.ComputeHash($combined)
    }
    finally {
        $sha.Dispose()
        [Array]::Clear($secret, 0, $secret.Length)
        [Array]::Clear($combined, 0, $combined.Length)
    }
    [byte[]]$aesKey = New-Object byte[] 32
    [byte[]]$macKey = New-Object byte[] 32
    [Buffer]::BlockCopy($digest, 0, $aesKey, 0, 32)
    [Buffer]::BlockCopy($digest, 32, $macKey, 0, 32)
    [Array]::Clear($digest, 0, $digest.Length)
    return @($aesKey, $macKey)
}

function Protect-Resource {
    param(
        [Parameter(Mandatory=$true)][string]$InputPath,
        [Parameter(Mandatory=$true)][string]$OutputPath
    )

    [byte[]]$plain = [IO.File]::ReadAllBytes($InputPath)
    $keys = Get-ResourceKeys
    [byte[]]$aesKey = $keys[0]
    [byte[]]$macKey = $keys[1]
    [byte[]]$iv = New-Object byte[] 16
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($iv)
    }
    finally {
        $rng.Dispose()
    }

    $aes = [Security.Cryptography.Aes]::Create()
    try {
        $aes.KeySize = 256
        $aes.Mode = [Security.Cryptography.CipherMode]::CBC
        $aes.Padding = [Security.Cryptography.PaddingMode]::PKCS7
        $aes.Key = $aesKey
        $aes.IV = $iv
        $encryptor = $aes.CreateEncryptor()
        try {
            [byte[]]$cipher = $encryptor.TransformFinalBlock($plain, 0, $plain.Length)
        }
        finally {
            $encryptor.Dispose()
        }
    }
    finally {
        $aes.Dispose()
        [Array]::Clear($plain, 0, $plain.Length)
    }

    [byte[]]$magic = [Text.Encoding]::ASCII.GetBytes("QCR1")
    [byte[]]$authenticated = New-Object byte[] ($magic.Length + $iv.Length + $cipher.Length)
    [Buffer]::BlockCopy($magic, 0, $authenticated, 0, $magic.Length)
    [Buffer]::BlockCopy($iv, 0, $authenticated, 4, $iv.Length)
    [Buffer]::BlockCopy($cipher, 0, $authenticated, 20, $cipher.Length)
    $hmac = New-Object Security.Cryptography.HMACSHA256 (,$macKey)
    try {
        [byte[]]$mac = $hmac.ComputeHash($authenticated)
    }
    finally {
        $hmac.Dispose()
    }
    [byte[]]$package = New-Object byte[] ($authenticated.Length + $mac.Length)
    [Buffer]::BlockCopy($authenticated, 0, $package, 0, $authenticated.Length)
    [Buffer]::BlockCopy($mac, 0, $package, $authenticated.Length, $mac.Length)
    [IO.File]::WriteAllBytes($OutputPath, $package)

    [Array]::Clear($aesKey, 0, $aesKey.Length)
    [Array]::Clear($macKey, 0, $macKey.Length)
    [Array]::Clear($cipher, 0, $cipher.Length)
    [Array]::Clear($authenticated, 0, $authenticated.Length)
    [Array]::Clear($mac, 0, $mac.Length)
    [Array]::Clear($package, 0, $package.Length)
}

$dnlibPath = Join-Path $libraryDirectory "dnlib.dll"
$avatarPath = Join-Path $assetDirectory "avatar.jpg"
if (-not (Test-Path -LiteralPath $dnlibPath -PathType Leaf)) {
    throw "Missing build dependency: $dnlibPath"
}
if (-not (Test-Path -LiteralPath $avatarPath -PathType Leaf)) {
    throw "Missing avatar: $avatarPath"
}

$encryptedDnlib = Join-Path $resourceDirectory "dnlib.enc"
$encryptedAvatar = Join-Path $resourceDirectory "avatar.enc"
Protect-Resource -InputPath $dnlibPath -OutputPath $encryptedDnlib
Protect-Resource -InputPath $avatarPath -OutputPath $encryptedAvatar

$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "C# compiler not found: $compiler"
}

# UTF-8 Base64 avoids Windows PowerShell 5 reading a BOM-less build script
# with a legacy code page and corrupting the Chinese output file name.
$outputName = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String(
        "6Zu35bC857uNUUMyMOeQg+adhuS7quS9jeenu+WBj+W3ruaVsOaNruWAjeeOh+WIh+aNouW3peWFty5leGU="))
$outputExe = Join-Path $distributionDirectory $outputName
$sources = Get-ChildItem -LiteralPath $projectRoot -Filter "*.cs" -File |
    Sort-Object Name |
    ForEach-Object { $_.FullName }

$arguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/debug-",
    ("/out:" + $outputExe),
    ("/win32manifest:" + (Join-Path $projectRoot "app.manifest")),
    ("/reference:" + $dnlibPath),
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Security.dll",
    "/reference:System.Windows.Forms.dll",
    ("/resource:" + $encryptedDnlib + ",Wataa.QC20Tool.Resources.dnlib.enc"),
    ("/resource:" + $encryptedAvatar + ",Wataa.QC20Tool.Resources.avatar.enc")
)
$arguments += $sources

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

$hash = (Get-FileHash -LiteralPath $outputExe -Algorithm SHA256).Hash
Write-Output "Built: $outputExe"
Write-Output "SHA256: $hash"
