[CmdletBinding()]
param(
    [string]$SiteName = "DBSLogs",
    [string]$AppPoolName = "DBSLogsPool",
    [string]$PhysicalPath = "C:\inetpub\DBSLogs",
    [int]$Port = 8081,
    [string]$SqlServer = ".",
    [string]$DatabaseName = "SOL_Credibox_PRD",
    [string]$SqlUser,
    [SecureString]$SqlPassword,
    [switch]$SkipWindowsFeatures,
    [switch]$KeepExistingFiles
)

$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell console."
    }
}

function Get-PlainTextPassword([SecureString]$Password) {
    if ($null -eq $Password) { return $null }
    $credential = New-Object System.Management.Automation.PSCredential("unused", $Password)
    return $credential.GetNetworkCredential().Password
}

function New-ConnectionString {
    if ([string]::IsNullOrWhiteSpace($SqlUser)) {
        return "Data Source=$SqlServer;Initial Catalog=$DatabaseName;Integrated Security=True;Application Name=DBS Logs;Connect Timeout=15;"
    }

    if ($null -eq $SqlPassword) {
        throw "When -SqlUser is provided, -SqlPassword must also be provided."
    }

    $plainPassword = Get-PlainTextPassword $SqlPassword
    return "Data Source=$SqlServer;Initial Catalog=$DatabaseName;User ID=$SqlUser;Password=$plainPassword;Application Name=DBS Logs;Connect Timeout=15;"
}

function Write-WebConfig([string]$Path, [string]$ConnectionString) {
    $escapedConnectionString = [Security.SecurityElement]::Escape($ConnectionString)
    $content = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <connectionStrings>
    <add name="Credibox" providerName="System.Data.SqlClient" connectionString="$escapedConnectionString" />
  </connectionStrings>
  <system.web>
    <compilation debug="false" targetFramework="4.5" />
    <httpRuntime targetFramework="4.5" executionTimeout="60" />
    <globalization requestEncoding="utf-8" responseEncoding="utf-8" fileEncoding="utf-8" culture="pt-PT" uiCulture="pt-PT" />
    <customErrors mode="Off" />
  </system.web>
  <system.webServer>
    <defaultDocument enabled="true">
      <files>
        <clear />
        <add value="Viewer.aspx" />
      </files>
    </defaultDocument>
    <directoryBrowse enabled="false" />
  </system.webServer>
</configuration>
"@

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText((Join-Path $Path "web.config"), $content, $utf8NoBom)
}

Assert-Administrator

Write-Host "[1/8] Validating IIS components..." -ForegroundColor Cyan
if (-not $SkipWindowsFeatures) {
    Import-Module ServerManager
    $features = @(
        "Web-Server",
        "Web-Default-Doc",
        "Web-Static-Content",
        "Web-Http-Errors",
        "Web-Net-Ext45",
        "Web-Asp-Net45",
        "Web-ISAPI-Ext",
        "Web-ISAPI-Filter",
        "Web-Mgmt-Tools"
    )

    $missing = Get-WindowsFeature $features | Where-Object { -not $_.Installed } | Select-Object -ExpandProperty Name
    if ($missing) {
        Install-WindowsFeature $missing -IncludeManagementTools | Out-Null
    }
}

Import-Module WebAdministration

Write-Host "[2/8] Creating the deployment folder..." -ForegroundColor Cyan
New-Item -Path $PhysicalPath -ItemType Directory -Force | Out-Null

if (-not $KeepExistingFiles) {
    Get-ChildItem -Path $PhysicalPath -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
}

Write-Host "[3/8] Copying application files..." -ForegroundColor Cyan
$requiredFiles = @(
    "Viewer.aspx",
    "Logs.aspx.cs",
    "Detail.aspx",
    "Detail.aspx.cs"
)

foreach ($file in $requiredFiles) {
    $source = Join-Path $PSScriptRoot $file
    if (-not (Test-Path $source)) { throw "Required file not found: $source" }
    Copy-Item $source (Join-Path $PhysicalPath $file) -Force
}

foreach ($folder in @("App_Code", "Content")) {
    $source = Join-Path $PSScriptRoot $folder
    if (-not (Test-Path $source)) { throw "Required folder not found: $source" }
    Copy-Item $source (Join-Path $PhysicalPath $folder) -Recurse -Force
}

Write-Host "[4/8] Creating web.config..." -ForegroundColor Cyan
$connectionString = New-ConnectionString
Write-WebConfig $PhysicalPath $connectionString

Write-Host "[5/8] Configuring the Application Pool..." -ForegroundColor Cyan
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value "v4.0"
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedPipelineMode -Value "Integrated"
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"

Write-Host "[6/8] Assigning read permissions to the deployment folder..." -ForegroundColor Cyan
& icacls.exe $PhysicalPath /grant "IIS AppPool\${AppPoolName}:(OI)(CI)(RX)" /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not assign permissions to $PhysicalPath." }

Write-Host "[7/8] Configuring the standalone IIS website..." -ForegroundColor Cyan
$existingBinding = Get-WebBinding -Protocol http -Port $Port -ErrorAction SilentlyContinue |
    Where-Object { $_.ItemXPath -notmatch "site\[@name='$SiteName'\]" }
if ($existingBinding) {
    throw "Port $Port is already in use by another IIS website. Choose a different port with -Port."
}

if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName -IPAddress "*" -Port $Port | Out-Null
}
else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PhysicalPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName

    $binding = Get-WebBinding -Name $SiteName -Protocol http -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $binding -or $binding.bindingInformation -notmatch ":${Port}:") {
        Get-WebBinding -Name $SiteName -Protocol http -ErrorAction SilentlyContinue | Remove-WebBinding
        New-WebBinding -Name $SiteName -Protocol http -IPAddress "*" -Port $Port
    }
}

Write-Host "[8/8] Starting the Application Pool and website..." -ForegroundColor Cyan
if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne "Started") {
    Start-WebAppPool -Name $AppPoolName
}
if ((Get-WebsiteState -Name $SiteName).Value -ne "Started") {
    Start-Website -Name $SiteName
}

Write-Host ""
Write-Host "Installation completed." -ForegroundColor Green
Write-Host "Website: http://localhost:$Port/"
Write-Host "Folder:  $PhysicalPath"
Write-Host "Pool:    $AppPoolName"
Write-Host ""
Write-Host "Note: when using Integrated Security, grant IIS AppPool\$AppPoolName read-only access to the SOL_Credibox_PRD and DBSPlatform databases." -ForegroundColor Yellow
