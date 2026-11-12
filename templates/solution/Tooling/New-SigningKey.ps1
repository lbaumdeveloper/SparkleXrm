<#
.SYNOPSIS
Creates the strong-name key Tooling\key.snk that Directory.Build.props signs the plugin assemblies with.

.DESCRIPTION
Run once per solution and commit key.snk, so every developer and build produces the same assembly identity.
Does nothing if the key already exists. Same format as "sn.exe -k".
#>
param(
    [string]$Path = (Join-Path $PSScriptRoot 'key.snk')
)

$ErrorActionPreference = 'Stop'

if (Test-Path $Path) {
    Write-Host "$Path already exists."
    return
}

$parameters = [System.Security.Cryptography.CspParameters]::new()
$parameters.KeyNumber = [int][System.Security.Cryptography.KeyNumber]::Signature
$parameters.Flags = [System.Security.Cryptography.CspProviderFlags]::CreateEphemeralKey

$rsa = [System.Security.Cryptography.RSACryptoServiceProvider]::new(1024, $parameters)
try {
    [System.IO.File]::WriteAllBytes($Path, $rsa.ExportCspBlob($true))
}
finally {
    $rsa.Dispose()
}

Write-Host "Created $Path"
