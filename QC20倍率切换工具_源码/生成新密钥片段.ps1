param(
    [Parameter(Mandatory=$true)]
    [string]$Base32Secret
)

$ErrorActionPreference = "Stop"
$alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
$normalized = $Base32Secret.Trim().Replace(" ", "").ToUpperInvariant()
if ($normalized.Length -lt 16) {
    throw "建议至少使用 16 位 Base32；正式使用推荐 32 位。"
}

$bits = New-Object Text.StringBuilder
foreach ($character in $normalized.ToCharArray()) {
    $value = $alphabet.IndexOf($character)
    if ($value -lt 0) {
        throw "密钥包含非 Base32 字符：$character"
    }
    [void]$bits.Append([Convert]::ToString($value, 2).PadLeft(5, "0"))
}

$byteCount = [Math]::Floor($bits.Length / 8)
[byte[]]$decoded = New-Object byte[] $byteCount
for ($index = 0; $index -lt $byteCount; $index++) {
    $decoded[$index] = [Convert]::ToByte(
        $bits.ToString($index * 8, 8), 2)
}

[byte[]]$mask = New-Object byte[] $decoded.Length
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $rng.GetBytes($mask)
}
finally {
    $rng.Dispose()
}

[byte[]]$fragment = New-Object byte[] $decoded.Length
for ($index = 0; $index -lt $decoded.Length; $index++) {
    $fragment[$index] = $decoded[$index] -bxor $mask[$index]
}

function Format-Bytes([byte[]]$bytes) {
    return ($bytes | ForEach-Object {
        "0x" + $_.ToString("X2")
    }) -join ", "
}

Write-Output "将下面两行同时更新到 SecretVault.cs 和 build.ps1："
Write-Output ("FragmentA = " + (Format-Bytes $fragment))
Write-Output ("FragmentB = " + (Format-Bytes $mask))

[Array]::Clear($decoded, 0, $decoded.Length)
[Array]::Clear($fragment, 0, $fragment.Length)
[Array]::Clear($mask, 0, $mask.Length)
