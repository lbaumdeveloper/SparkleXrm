<#
.SYNOPSIS
Builds and packs Spkl.OnPrem, Spkl.OnPrem.Attributes and Spkl.OnPrem.Templates.

.DESCRIPTION
The version comes from <Version> in Directory.Build.props. The template's references to
Spkl.OnPrem and Spkl.OnPrem.Attributes must use the same version, otherwise packing stops.

.PARAMETER OutputDirectory
Folder the .nupkg files are written to.
#>
param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE" }
}

$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw 'No <Version> in Directory.Build.props.' }

$templateReferences = Get-ChildItem (Join-Path $root 'templates\solution') -Recurse -Filter '*.csproj' |
    Select-String -Pattern '<PackageReference Include="(Spkl\.OnPrem(\.Attributes)?)" Version="([^"]+)"'
$mismatches = $templateReferences | Where-Object { $_.Matches[0].Groups[3].Value -ne $version }
if ($mismatches) {
    throw "Template package versions differ from $version in Directory.Build.props:`n$($mismatches -join "`n")"
}

Write-Host "Packing version $version to $OutputDirectory"

Invoke-DotNet build (Join-Path $root 'spkl\CrmSvcUtilFilteringService') -c Release
Invoke-DotNet pack (Join-Path $root 'spkl\spkl\spkl.csproj') -c Release -o $OutputDirectory `
    '-p:NuspecFile=Package\spkl.nuspec' '-p:NuspecBasePath=Package' "-p:NuspecProperties=version=$version"
Invoke-DotNet pack (Join-Path $root 'spkl\Spkl.Attributes') -c Release -o $OutputDirectory
Invoke-DotNet pack (Join-Path $root 'templates\Spkl.OnPrem.Templates.csproj') -c Release -o $OutputDirectory
