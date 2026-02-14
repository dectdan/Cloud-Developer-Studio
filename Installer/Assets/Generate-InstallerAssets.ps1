# Generate MSI Installer Assets from Your Logo
# Run this script to create all required installer graphics

Write-Host "Generating MSI Installer Assets..." -ForegroundColor Cyan
Write-Host ""

# Check for ImageMagick
$magick = Get-Command magick -ErrorAction SilentlyContinue
if (-not $magick) {
    Write-Host "ImageMagick not found. Installing..." -ForegroundColor Yellow
    winget install ImageMagick.ImageMagick
    Write-Host "ImageMagick installed. Please run this script again." -ForegroundColor Green
    Read-Host "Press Enter to close"
    exit 0
}

$assetsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$logo = Join-Path $assetsDir "Cloud Developer Studio 1080.png"

if (-not (Test-Path $logo)) {
    Write-Host "Logo file not found: $logo" -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

# Create .ico file for shortcuts and installer
Write-Host "Creating AppIcon.ico..." -ForegroundColor Yellow
$iconPath = Join-Path $assetsDir "AppIcon.ico"
& magick $logo -resize 256x256 -define icon:auto-resize=256,128,96,64,48,32,16 $iconPath

# Create installer banner (493x58 pixels)
Write-Host "Creating installer banner..." -ForegroundColor Yellow
$bannerPath = Join-Path $assetsDir "Banner.bmp"
& magick $logo -resize 493x58 -gravity center -background "#1a1a1a" -extent 493x58 $bannerPath

# Create installer dialog background (493x312 pixels)
Write-Host "Creating installer dialog..." -ForegroundColor Yellow
$dialogPath = Join-Path $assetsDir "Dialog.bmp"
& magick $logo -resize 300x300 -gravity center -background "#1a1a1a" -extent 493x312 $dialogPath

# Create file icon (32x32)
Write-Host "Creating file icon..." -ForegroundColor Yellow
$fileIconPath = Join-Path $assetsDir "FileIcon.png"
& magick $logo -resize 32x32 $fileIconPath

Write-Host ""
Write-Host "All installer assets created!" -ForegroundColor Green
Write-Host ""
Write-Host "Created files:" -ForegroundColor Cyan
Write-Host "  - AppIcon.ico (installer icon)" 
Write-Host "  - Banner.bmp (installer top banner)"
Write-Host "  - Dialog.bmp (installer welcome screen)"
Write-Host "  - FileIcon.png (backup file icon)"
Write-Host ""
Read-Host "Press Enter to close"
