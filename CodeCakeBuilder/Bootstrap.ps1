<#
.SYNOPSIS
Builds CodeCakeBuilder, and downloads tools like NuGet.exe.

.DESCRIPTION
This script builds CodeCakeBuilder with the help of nuget.exe (in Tools/, downloaded if missing).
Requires Visual Studio 2017.

.NOTES
You may move this Bootstrap.ps1 to the solution directory, or let it in CodeCakeBuilder folder:
The $solutionDir and $builderDir variables are automatically set.
VSSetup module is required to find MSBuild. If missing, it will be installed.

.EXAMPLE
.Bootstrap.ps1 -Verbose
#>
[CmdletBinding()]
Param()
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$nugetDownloadUrl = 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe'
$vssetupUrl = 'https://github.com/Microsoft/vssetup.powershell/releases/download/1.0.58/VSSetup.zip'

# Go back a level if ./CodeCakeBuilder isn't found
$solutionDir = $PSScriptRoot
$builderDir = Join-Path $solutionDir "CodeCakeBuilder"
if (!(Test-Path $builderDir -PathType Container)) {
    $builderDir = $PSScriptRoot
    $solutionDir = Join-Path $builderDir ".."
}
Write-Verbose "Using solution directory: $solutionDir"
Write-Verbose "Using builder directory: $builderDir"

# Ensure CodeCakeBuilder project exists
$builderProj = Join-Path $builderDir "CodeCakeBuilder.csproj"
if (!(Test-Path $builderProj)) {
    Throw "Could not find $builderProj"
}
Write-Verbose "Using builder project: $builderProj"

# Ensure packages.config file exists.
$builderPackageConfig = Join-Path $builderDir "packages.config"
if (!(Test-Path $builderPackageConfig)) {
    Throw "Could not find $builderPackageConfig"
}

$msbuildExe = "msbuild"
# Accept MSBuild from PATH
if (!(Get-Command $msbuildExe -ErrorAction SilentlyContinue)) {
    Write-Verbose "MSBuild does not exist in path."
    # Install VSSetup module
    if (!(Get-Command "Get-VSSetupInstance" -ErrorAction SilentlyContinue)) {
        Write-Verbose "Installing VSSetup module..."
        # Install VSSetup on user
        if($PSVersionTable.PSVersion.Major -ge 5) {
            Write-Verbose "Installing VSSetup using Install-Module"
            # PS5+: Use Install-Module
            Install-PackageProvider -Name NuGet -Scope CurrentUser -Force
            Install-Module VSSetup -Scope CurrentUser -Force
        } else {
            Write-Verbose "Installing VSSetup using local extraction"
            # PS3-4: Extract locally
            $vssetupTempFile = [System.IO.Path]::GetTempFileName()
            Invoke-WebRequest -Uri $vssetupUrl -OutFile $vssetupTempFile
            Add-Type -assembly "system.io.compression.filesystem"
            $vssetupDir = "$([Environment]::GetFolderPath('MyDocuments'))\WindowsPowerShell\Modules\VSSetup"
            Write-Verbose "VSSetup module installed in: $vssetupDir"
            [io.compression.zipfile]::ExtractToDirectory($vssetupTempFile, $vssetupDir)
            Remove-Item $vssetupTempFile -Force
            Import-Module 'VSSetup'
        }
    }
    if (!(Get-Command "Get-VSSetupInstance" -ErrorAction SilentlyContinue)) {
        Throw "VSSetup module could not be loaded after installation"
    }
    # Find MSBuild (Package Microsoft.Component.MSBuild)
    $vsi = Get-VSSetupInstance -All | Select-VSSetupInstance -Require 'Microsoft.Component.MSBuild' -Latest
    if (!($vsi)) {
        Throw "Could not find a Visual Studio instance with Microsoft.Component.MSBuild"
    }
    Write-Verbose "Found Visual Studio installation in $($vsi.InstallationPath)"
    $vsiPackage = $vsi.Packages | Where {$_.Id -eq 'Microsoft.Component.MSBuild'}
    $msbuildExe = [io.path]::combine($vsi.InstallationPath,'MSBuild',"$($vsiPackage.Version.Major).$($vsiPackage.Version.Minor)",'Bin','MSBuild.exe')
}

if (!(Test-Path $msbuildExe)) {
    Throw "Could not find $msbuildExe"
}
Write-Verbose "Using MSBuild: $msbuildExe"

# Tools directory is for nuget.exe but it may be used to 
# contain other utilities.
$toolsDir = Join-Path $builderDir "Tools"
if (!(Test-Path $toolsDir)) {
    New-Item -ItemType Directory $toolsDir | Out-Null
}

# Try download NuGet.exe if do not exist.
$nugetExe = Join-Path $toolsDir "nuget.exe"
if (!(Test-Path $nugetExe)) {
    Write-Verbose "Downloading nuget.exe from $nugetDownloadUrl"
    Invoke-WebRequest -Uri $nugetDownloadUrl -OutFile $nugetExe
    # Make sure NuGet worked.
    if (!(Test-Path $nugetExe)) {
        Throw "Could not find NuGet.exe"
    }
}

$nugetConfigFile = Join-Path $solutionDir "NuGet.config"
&$nugetExe restore $builderPackageConfig -SolutionDirectory $solutionDir -configfile $nugetConfigFile

&$msbuildExe $builderProj /p:Configuration=Release
