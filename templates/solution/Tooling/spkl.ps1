<#
.SYNOPSIS
Runs a spkl task against the spkl.json in this folder, using the spkl package installed once for the solution.

.PARAMETER Task
spkl task, e.g. plugins, workflow, webresources, earlybound, download-webresources, unpack, pack, import.

.PARAMETER Profile
Optional spkl.json profile, e.g. plugins, actions, workflows.

.PARAMETER Connection
Optional connection string. Defaults to $env:SPKL_CONNECTION; when empty spkl shows the interactive login.

.PARAMETER Path
Folder spkl works in. Defaults to this folder, which holds spkl.json. The instrument task needs the plugin project folder instead.

.PARAMETER DryRun
plugins, workflow and webresources only: show what would be created, updated and deleted without writing anything.
#>
param(
    [Parameter(Mandatory = $true)][string]$Task,
    [string]$Profile,
    [string]$Connection = $env:SPKL_CONNECTION,
    [string]$Path = $PSScriptRoot,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Find-Spkl {
    $solutionPackages = Join-Path $PSScriptRoot '..\packages'
    $globalPackages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\packages' }

    $candidates = @(
        Get-ChildItem -Path $solutionPackages -Filter 'spkl.*' -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName 'tools\spkl.exe' }
        Get-ChildItem -Path (Join-Path $globalPackages 'spkl') -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName 'tools\spkl.exe' }
    ) | Where-Object { Test-Path $_ }

    $latest = $candidates |
        Sort-Object { [version](($_ -split '[\\/]')[-3] -replace '^spkl\.', '' -replace '-.*$', '') } -Descending |
        Select-Object -First 1

    if (-not $latest) {
        throw "spkl.exe not found. Restore the spkl NuGet package in the Tooling project."
    }
    return $latest
}

$spkl = Find-Spkl
Write-Host "Using $spkl"

$arguments = @($Task, (Resolve-Path $Path).Path)
if ($Connection) { $arguments += $Connection }
if ($Profile) { $arguments += "/p:$Profile" }
if ($DryRun) { $arguments += '/dry-run' }

& $spkl @arguments
exit $LASTEXITCODE
