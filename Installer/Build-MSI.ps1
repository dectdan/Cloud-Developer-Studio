# Build MSI Installer for GitHub Distribution
# Requires WiX Toolset v3.11+ or v4+
# Download: https://wixtoolset.org/

param(
    [string]$Version = "1.0.0.0",
    [string]$Configuration = "Release"
)

Write-Host "🚀 Building Claude Developer Studio MSI Installer" -ForegroundColor Cyan
Write-Host ""

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir  
$WixSource = Join-Path $ScriptDir "Installer.wxs"
$OutputDir = Join-Path $ScriptDir "Output"

# Check for WiX Toolset - try v3.14, then v3.11, then v3.10
$wixPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.14\bin"
if (-not (Test-Path $wixPath)) {
    $wixPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin"
}
if (-not (Test-Path $wixPath)) {
    $wixPath = "${env:ProgramFiles}\WiX Toolset v3.14\bin"
}
if (-not (Test-Path $wixPath)) {
    $wixPath = "${env:ProgramFiles}\WiX Toolset v3.11\bin"
}

if (-not (Test-Path $wixPath)) {
    Write-Host "❌ WiX Toolset not found!" -ForegroundColor Red
    Write-Host "   Download from: https://wixtoolset.org/" -ForegroundColor Yellow
    Write-Host "   Or use winget: winget install --id WixToolset.WixToolset" -ForegroundColor Yellow
    exit 1
}

$candle = Join-Path $wixPath "candle.exe"
$light = Join-Path $wixPath "light.exe"

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Build the project first
Write-Host "📦 Building project..." -ForegroundColor Yellow
$csproj = Join-Path $ProjectRoot "ClaudeDevStudio.csproj"
dotnet build $csproj -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Compile WiX source
Write-Host "🔨 Compiling WiX source..." -ForegroundColor Yellow
$wixObj = Join-Path $OutputDir "Installer.wixobj"

& $candle $WixSource `
    -dProductVersion=$Version `
    -dConfiguration=$Configuration `
    -out $wixObj `
    -ext WixUIExtension `
    -ext WixUtilExtension

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ WiX compilation failed!" -ForegroundColor Red
    exit 1
}

# Link MSI
Write-Host "🔗 Linking MSI package..." -ForegroundColor Yellow
$msiPath = Join-Path $OutputDir "ClaudeDevStudio_$Version.msi"

& $light $wixObj `
    -out $msiPath `
    -ext WixUIExtension `
    -ext WixUtilExtension `
    -sval

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ MSI linking failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ MSI installer created successfully!" -ForegroundColor Green
Write-Host "   Location: $msiPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Next steps:" -ForegroundColor Yellow
Write-Host "1. Test the installer on a clean machine"
Write-Host "2. Sign the MSI (optional but recommended)"
Write-Host "3. Upload to GitHub Releases"
Write-Host "4. Create release notes"
Write-Host ""

# Show file info
$msiFile = Get-Item $msiPath
Write-Host "Package Details:" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($msiFile.Length / 1MB, 2)) MB"
Write-Host "  Version: $Version"
Write-Host "  Created: $($msiFile.LastWriteTime)"
