$ErrorActionPreference = 'Stop'

# Script canonico dos gates do NewScripts.
# Comentarios em PT-BR; nomes e saida em ingles para facilitar uso em tooling/CI.

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$newScriptsRoot = Join-Path $repoRoot 'Assets\_ImmersiveGames\NewScripts'
$excludedSegments = @('/Dev/', '/Editor/', '/Legacy/', '/QA/')

function Normalize-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $relativePath = $fullPath.Substring($newScriptsRoot.Length).TrimStart('\', '/')
    return $relativePath -replace '\\', '/'
}

function Get-RuntimeCsFiles {
    Get-ChildItem -Path $newScriptsRoot -Recurse -Filter '*.cs' -File |
        Where-Object {
            $normalized = $_.FullName -replace '\\', '/'
            foreach ($segment in $excludedSegments) {
                if ($normalized.Contains($segment)) {
                    return $false
                }
            }
            return $true
        }
}

function Find-Matches {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo[]]$Files,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [switch]$WholeWord
    )

    $regex = if ($WholeWord) { "\\b$Pattern\\b" } else { $Pattern }
    $results = @()

    foreach ($file in $Files) {
        $lines = Get-Content -Path $file.FullName
        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -match $regex) {
                $results += [pscustomobject]@{
                    RelativePath = Normalize-RelativePath -Path $file.FullName
                    LineNumber = $index + 1
                    LineText = $lines[$index]
                }
            }
        }
    }

    return $results
}

function Write-GateResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [bool]$Passed,
        [string[]]$Details = @()
    )

    if ($Passed) {
        Write-Host "PASS $Name"
    }
    else {
        Write-Host "FAIL $Name"
        foreach ($line in $Details) {
            Write-Host "  $line"
        }
    }
}

if (-not (Get-Command rg -ErrorAction SilentlyContinue)) {
    Write-Error 'rg is required but was not found in PATH.'
    exit 1
}

if (-not (Test-Path $newScriptsRoot)) {
    Write-Error "NewScripts root was not found at '$newScriptsRoot'."
    exit 1
}

$runtimeFiles = @(Get-RuntimeCsFiles)
$allPassed = $true

$gateAMatches = @(Find-Matches -Files $runtimeFiles -Pattern 'UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu')
if ($gateAMatches.Count -eq 0) {
    Write-GateResult -Name 'Gate A' -Passed $true
}
else {
    $allPassed = $false
    $details = $gateAMatches | ForEach-Object { "$($_.RelativePath):$($_.LineNumber): $($_.LineText.Trim())" }
    Write-GateResult -Name 'Gate A' -Passed $false -Details $details
}

$gateA2Matches = @(Find-Matches -Files $runtimeFiles -Pattern 'InitializeOnLoadMethod' -WholeWord)
if ($gateA2Matches.Count -eq 0) {
    Write-GateResult -Name 'Gate A2' -Passed $true
}
else {
    $allPassed = $false
    $details = $gateA2Matches | ForEach-Object { "$($_.RelativePath):$($_.LineNumber): $($_.LineText.Trim())" }
    Write-GateResult -Name 'Gate A2' -Passed $false -Details $details
}

$gateBMatches = @(Find-Matches -Files $runtimeFiles -Pattern 'RuntimeInitializeOnLoadMethod')
$expectedGateB = @(
    'Core/Logging/DebugUtility.cs',
    'Infrastructure/Composition/GlobalCompositionRoot.Entry.cs'
) | Sort-Object
$actualGateB = @($gateBMatches | Select-Object -ExpandProperty RelativePath -Unique | Sort-Object)
$gateBPassed = ($actualGateB.Count -eq $expectedGateB.Count) -and (@(Compare-Object -ReferenceObject $expectedGateB -DifferenceObject $actualGateB).Count -eq 0)

if ($gateBPassed) {
    Write-GateResult -Name 'Gate B' -Passed $true
}
else {
    $allPassed = $false
    $details = @('Expected allowlist:') +
        ($expectedGateB | ForEach-Object { "  $_" }) +
        @('Actual matches:') +
        ($actualGateB | ForEach-Object { "  $_" })
    Write-GateResult -Name 'Gate B' -Passed $false -Details $details
}

if ($allPassed) {
    exit 0
}

exit 1
