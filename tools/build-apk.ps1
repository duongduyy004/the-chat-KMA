<#
.SYNOPSIS
    Build the KMA Android player for ARM64 and/or x86_64 in one headless Unity session.
.DESCRIPTION
    Windows counterpart of tools/build-apk.sh. Windows PowerShell 5.1 compatible.
    The Unity Editor must be closed: batchmode cannot open a locked project.
.EXAMPLE
    .\tools\build-apk.ps1
.EXAMPLE
    .\tools\build-apk.ps1 -Abi x86_64 -Name kma-emulator
#>
[CmdletBinding()]
param(
    # arm64 | x86_64 | all | comma-separated
    [string] $Abi = 'arm64',
    # APK directory, relative to the project root
    [string] $OutputDir = 'Builds/Android',
    # APK base name; files land at <base>-<abi>.apk
    [string] $Name = 'kma',
    # Unity executable; defaults to $env:KMA_UNITY_EDITOR, else a Unity Hub lookup
    [string] $Unity = $env:KMA_UNITY_EDITOR,
    # Unity log file; defaults to <OutputDir>\build-apk.log
    [string] $Log
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
$versionLine = Select-String -Path $versionFile -Pattern '^m_EditorVersion: *(.+)$'
if (-not $versionLine) {
    throw "build-apk: cannot read the editor version from $versionFile"
}
$unityVersion = $versionLine.Matches[0].Groups[1].Value.Trim()

if ([string]::IsNullOrWhiteSpace($Unity)) {
    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add("C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe")
    $candidates.Add("C:\Program Files (x86)\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe")

    # Unity Hub can install editors outside Program Files.
    $secondary = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
    if (Test-Path $secondary) {
        $root = (Get-Content -Raw -Path $secondary).Trim().Trim('"')
        if (-not [string]::IsNullOrWhiteSpace($root)) {
            $candidates.Add((Join-Path $root "$unityVersion\Editor\Unity.exe"))
        }
    }
    $candidates.Add("C:\Program Files\Unity\Editor\Unity.exe")

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { $Unity = $candidate; break }
    }
}

if ([string]::IsNullOrWhiteSpace($Unity) -or -not (Test-Path -LiteralPath $Unity -PathType Leaf)) {
    [Console]::Error.WriteLine("build-apk: Unity $unityVersion not found.")
    [Console]::Error.WriteLine("  Pass -Unity 'C:\path\to\Editor\Unity.exe' or set `$env:KMA_UNITY_EDITOR.")
    exit 1
}

if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp\UnityLockfile')) {
    [Console]::Error.WriteLine('build-apk: Temp\UnityLockfile exists - close the Unity Editor before building.')
    exit 1
}

$outputFull = Join-Path $projectRoot $OutputDir
if (-not (Test-Path -LiteralPath $outputFull)) {
    New-Item -ItemType Directory -Path $outputFull -Force | Out-Null
}
if ([string]::IsNullOrWhiteSpace($Log)) { $Log = Join-Path $outputFull 'build-apk.log' }
$logDir = Split-Path -Parent $Log
if ($logDir -and -not (Test-Path -LiteralPath $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

Write-Host "Unity   : $Unity ($unityVersion)"
Write-Host "ABI     : $Abi"
Write-Host "Output  : $OutputDir/$Name-<abi>.apk"
Write-Host "Log     : $Log"
Write-Host ''

# Stream Unity's live log to the terminal while retaining the complete log on disk.
$unityArgs = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $projectRoot,
    '-executeMethod', 'KMA.EditorTools.AndroidBuildMatrix.Build',
    '-androidAbi', $Abi,
    '-buildOutputDir', $OutputDir,
    '-buildName', $Name,
    '-logFile', '-'
)
$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& $Unity @unityArgs 2>&1 |
    ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) {
            $_.Exception.Message
        } else {
            $_
        }
    } |
    Tee-Object -FilePath $Log
$unityExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorActionPreference

if ($unityExitCode -ne 0) {
    Write-Host "build-apk: Unity exited with $unityExitCode. Last 40 log lines:"
    if (Test-Path -LiteralPath $Log) { Get-Content -Path $Log -Tail 40 | Write-Host }
    exit $unityExitCode
}

Write-Host ''
Get-ChildItem -Path $outputFull -Filter "$Name-*.apk" -File | ForEach-Object {
    $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.ToLower()
    Write-Host ("{0}  {1} bytes  sha256={2}" -f $_.FullName, $_.Length, $hash)
}
