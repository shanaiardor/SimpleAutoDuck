param(
    [string]$Configuration = "Release",
    [string]$InnoPath = "",
    [switch]$NoPush,
    [switch]$NoTag,
    [switch]$AllowDirty,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Resolve-ExistingFile {
    param([string[]]$Candidates)

    foreach ($candidate in $Candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }

        if (Test-Path -LiteralPath $candidate -PathType Container) {
            $exe = Join-Path $candidate "ISCC.exe"
            if (Test-Path -LiteralPath $exe -PathType Leaf) {
                return (Resolve-Path -LiteralPath $exe).Path
            }
        }
    }

    return $null
}

function Resolve-CommandPath {
    param([string]$CommandName)

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if ($command -and $command.Source) {
        return $command.Source
    }

    return $null
}

function Read-Text {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path)
}

function Write-Text {
    param(
        [string]$Path,
        [string]$Text
    )

    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Text, $utf8)
}

function Get-NextPatchVersion {
    param([string]$Version)

    $parts = $Version.Split(".")
    if ($parts.Length -ne 4) {
        throw "Version '$Version' must have four numeric parts, for example 0.0.2.0."
    }

    $numbers = @()
    foreach ($part in $parts) {
        $value = 0
        if (-not [int]::TryParse($part, [ref]$value)) {
            throw "Version '$Version' contains a non-numeric part."
        }
        $numbers += $value
    }

    $numbers[2] += 1
    $numbers[3] = 0
    return ($numbers -join ".")
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

$assemblyInfoPath = Join-Path $repoRoot "SimpleAutoDuck\Properties\AssemblyInfo.cs"
$installerScriptPath = Join-Path $repoRoot "installer.iss"
$readmePath = Join-Path $repoRoot "README.md"

Write-Step "Checking git state"
git rev-parse --is-inside-work-tree | Out-Null
$initialStatus = git status --porcelain
if ($initialStatus -and -not $AllowDirty -and -not $DryRun) {
    throw "Working tree has uncommitted changes. Commit them first, or rerun with -AllowDirty if you know what you are doing."
}

$branch = git branch --show-current
if ([string]::IsNullOrWhiteSpace($branch)) {
    throw "Cannot push from a detached HEAD. Check out a branch first."
}

Write-Step "Resolving tools"
$msbuild = Resolve-ExistingFile @(
    $env:MSBUILD_EXE,
    (Resolve-CommandPath "MSBuild.exe"),
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)
if (-not $msbuild) {
    throw "MSBuild.exe was not found. Install Visual Studio 2022 with .NET desktop workload, or set MSBUILD_EXE."
}

$iscc = Resolve-ExistingFile @(
    $InnoPath,
    (Resolve-CommandPath "ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6"),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6"
)
if (-not $iscc) {
    throw "ISCC.exe was not found. Install Inno Setup 6 or pass -InnoPath 'C:\Path\To\Inno Setup 6'."
}

Write-Host "MSBuild: $msbuild"
Write-Host "Inno Setup: $iscc"

Write-Step "Bumping version"
$assemblyText = Read-Text $assemblyInfoPath
$installerText = Read-Text $installerScriptPath

$assemblyMatch = [regex]::Match($assemblyText, 'AssemblyVersion\("(?<version>\d+\.\d+\.\d+\.\d+)"\)')
$fileMatch = [regex]::Match($assemblyText, 'AssemblyFileVersion\("(?<version>\d+\.\d+\.\d+\.\d+)"\)')
$innoMatch = [regex]::Match($installerText, '#define\s+MyAppVersion\s+"(?<version>\d+\.\d+\.\d+\.\d+)"')

if (-not $assemblyMatch.Success -or -not $fileMatch.Success -or -not $innoMatch.Success) {
    throw "Could not find all version fields in AssemblyInfo.cs and installer.iss."
}

$currentVersion = $assemblyMatch.Groups["version"].Value
if ($fileMatch.Groups["version"].Value -ne $currentVersion -or $innoMatch.Groups["version"].Value -ne $currentVersion) {
    throw "Version fields are not in sync. AssemblyVersion=$currentVersion, AssemblyFileVersion=$($fileMatch.Groups["version"].Value), MyAppVersion=$($innoMatch.Groups["version"].Value)."
}

$newVersion = Get-NextPatchVersion $currentVersion
Write-Host "Version: $currentVersion -> $newVersion"

if ($DryRun) {
    Write-Host ""
    Write-Host "Dry run complete. No files were changed." -ForegroundColor Green
    return
}

$assemblyText = [regex]::Replace($assemblyText, 'AssemblyVersion\("\d+\.\d+\.\d+\.\d+"\)', "AssemblyVersion(`"$newVersion`")")
$assemblyText = [regex]::Replace($assemblyText, 'AssemblyFileVersion\("\d+\.\d+\.\d+\.\d+"\)', "AssemblyFileVersion(`"$newVersion`")")
$installerText = [regex]::Replace($installerText, '#define\s+MyAppVersion\s+"\d+\.\d+\.\d+\.\d+"', "#define MyAppVersion `"$newVersion`"")

Write-Text $assemblyInfoPath $assemblyText
Write-Text $installerScriptPath $installerText

if (Test-Path -LiteralPath $readmePath -PathType Leaf) {
    $readmeText = Read-Text $readmePath
    $readmeText = [regex]::Replace($readmeText, 'SimpleAutoDuckSetup-\d+\.\d+\.\d+\.\d+\.exe', "SimpleAutoDuckSetup-$newVersion.exe")
    Write-Text $readmePath $readmeText
}

Write-Step "Building $Configuration"
& $msbuild "SimpleAutoDuck.sln" "/m" "/t:Rebuild" "/p:Configuration=$Configuration"

Write-Step "Packaging with Inno Setup"
& $iscc $installerScriptPath

$installerOutput = Join-Path $repoRoot "installer\Output\SimpleAutoDuckSetup-$newVersion.exe"
if (-not (Test-Path -LiteralPath $installerOutput -PathType Leaf)) {
    throw "Expected installer was not created: $installerOutput"
}

Write-Step "Creating release commit"
git add -- $assemblyInfoPath $installerScriptPath $readmePath $installerOutput

$staged = git diff --cached --name-only
if (-not $staged) {
    throw "No release files were staged."
}

git commit -m "release: v$newVersion"

if (-not $NoTag) {
    Write-Step "Creating tag v$newVersion"
    git tag "v$newVersion"
}

if (-not $NoPush) {
    Write-Step "Pushing to GitHub"
    git push origin $branch
    if (-not $NoTag) {
        git push origin "v$newVersion"
    }
}

Write-Host ""
Write-Host "Release package complete: $installerOutput" -ForegroundColor Green
