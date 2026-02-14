# Build MSIX Package for Microsoft Store
# Run this script to create the Store-ready package

param(
    [string]$Version = "1.0.0.0",
    [string]$Publisher = "CN=YourPublisherName"
)

Write-Host "🚀 Building Claude Developer Studio MSIX Package" -ForegroundColor Cyan
Write-Host ""

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$BinPath = Join-Path $ProjectRoot "bin\Debug\net8.0"
$PackageDir = Join-Path $ScriptDir "Package"
$OutputDir = Join-Path $ScriptDir "Output"

# Clean and create directories
Write-Host "📁 Preparing directories..." -ForegroundColor Yellow
if (Test-Path $PackageDir) { Remove-Item $PackageDir -Recurse -Force }
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Copy executable and dependencies
Write-Host "📦 Copying files..." -ForegroundColor Yellow
Copy-Item "$BinPath\claudedev.exe" $PackageDir
Copy-Item "$BinPath\claudedev.dll" $PackageDir -ErrorAction SilentlyContinue
Copy-Item "$BinPath\*.json" $PackageDir -ErrorAction SilentlyContinue

# Copy manifest and assets
Copy-Item "$ScriptDir\Package.appxmanifest" $PackageDir
Copy-Item "$ScriptDir\Assets" $PackageDir -Recurse

# Update version in manifest
$manifestPath = Join-Path $PackageDir "Package.appxmanifest"
$manifest = Get-Content $manifestPath -Raw
$manifest = $manifest -replace 'Version="[^"]*"', "Version=`"$Version`""
$manifest = $manifest -replace 'Publisher="[^"]*"', "Publisher=`"$Publisher`""
$manifest | Set-Content $manifestPath -Encoding UTF8

# Create MSIX package
Write-Host "🔨 Creating MSIX package..." -ForegroundColor Yellow
$msixPath = Join-Path $OutputDir "ClaudeDevStudio_$Version.msix"

try {
    # Use makeappx.exe (comes with Windows SDK)
    $makeappx = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe"
    
    if (-not (Test-Path $makeappx)) {
        # Try to find any version
        $makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Filter "makeappx.exe" -Recurse -ErrorAction SilentlyContinue | 
                    Select-Object -First 1 -ExpandProperty FullName
    }
    
    if (Test-Path $makeappx) {
        & $makeappx pack /d $PackageDir /p $msixPath /o
        Write-Host "✅ MSIX package created: $msixPath" -ForegroundColor Green
    } else {
        Write-Host "❌ makeappx.exe not found. Install Windows SDK." -ForegroundColor Red
        Write-Host "   Download: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ Error creating MSIX: $_" -ForegroundColor Red
    exit 1
}

# Sign the package (required for Store)
Write-Host ""
Write-Host "📝 Next steps:" -ForegroundColor Cyan
Write-Host "1. Sign the MSIX with your Store certificate"
Write-Host "2. Upload to Microsoft Partner Center"
Write-Host "3. Submit for Store review"
Write-Host ""
Write-Host "Package location: $msixPath" -ForegroundColor Green
