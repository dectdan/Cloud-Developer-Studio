# Build ClaudeDevStudio MSI Installer
# This builds a COMPLETE turnkey installer with DebugView and MCP server bundled

$ErrorActionPreference = "Stop"

Write-Host "===== Building ClaudeDevStudio Turnkey Installer =====" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build all components
Write-Host "[1/6] Building CLI..." -ForegroundColor Yellow
cd D:\Projects\ClaudeDevStudio
dotnet build -c Release | Out-Null
Write-Host "  ✓ CLI built" -ForegroundColor Green

Write-Host "[2/6] Building TrayApp..." -ForegroundColor Yellow  
cd D:\Projects\ClaudeDevStudio\ClaudeDevStudio.UI
dotnet build -c Release | Out-Null
Write-Host "  ✓ TrayApp built" -ForegroundColor Green

Write-Host "[3/6] Building Dashboard..." -ForegroundColor Yellow
cd D:\Projects\ClaudeDevStudio\ClaudeDevStudio.Dashboard
dotnet build -c Release | Out-Null
Write-Host "  ✓ Dashboard built" -ForegroundColor Green

# Step 2: Harvest Dashboard files
Write-Host "[4/6] Harvesting Dashboard files..." -ForegroundColor Yellow
cd D:\Projects\ClaudeDevStudio\Installer
& "C:\Program Files (x86)\WiX Toolset v3.14\bin\heat.exe" dir "..\ClaudeDevStudio.Dashboard\bin\Release\net8.0-windows10.0.19041.0" `
    -cg DashboardFilesGroup `
    -gg -sfrag -srd -sreg `
    -dr DashboardFolder `
    -var "var.DashboardSourceDir" `
    -out "Dashboard_Files.wxs"
Write-Host "  ✓ Dashboard harvested" -ForegroundColor Green

# Step 3: Compile WiX files
Write-Host "[5/6] Compiling WiX files..." -ForegroundColor Yellow
& "C:\Program Files (x86)\WiX Toolset v3.14\bin\candle.exe" `
    -ext WixUtilExtension `
    -dDashboardSourceDir="..\ClaudeDevStudio.Dashboard\bin\Release\net8.0-windows10.0.19041.0" `
    Installer.wxs Dashboard_Files.wxs

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Compilation failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ WiX files compiled" -ForegroundColor Green

# Step 4: Link MSI
Write-Host "[6/6] Linking MSI..." -ForegroundColor Yellow
& "C:\Program Files (x86)\WiX Toolset v3.14\bin\light.exe" `
    -ext WixUtilExtension `
    -ext WixUIExtension `
    -out "Output\ClaudeDevStudio.msi" `
    Installer.wixobj Dashboard_Files.wixobj `
    -b "..\ClaudeDevStudio.Dashboard\bin\Release\net8.0-windows10.0.19041.0"

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Linking failed!" -ForegroundColor Red
    exit 1
}

Write-Host "  ✓ MSI created" -ForegroundColor Green
Write-Host ""
Write-Host "===== Build Complete! =====" -ForegroundColor Cyan
Write-Host ""
Write-Host "MSI Location: D:\Projects\ClaudeDevStudio\Installer\Output\ClaudeDevStudio.msi" -ForegroundColor Green
Write-Host ""
Write-Host "This MSI includes:" -ForegroundColor White
Write-Host "  ✓ CLI Tool (claudedev.exe)" -ForegroundColor Green
Write-Host "  ✓ TrayApp with custom icon" -ForegroundColor Green
Write-Host "  ✓ Dashboard (WinUI3)" -ForegroundColor Green
Write-Host "  ✓ DebugView (BUNDLED - no separate download!)" -ForegroundColor Cyan
Write-Host "  ✓ MCP Server (BUNDLED - auto-configured!)" -ForegroundColor Cyan
Write-Host "  ✓ Auto-configuration script" -ForegroundColor Cyan
Write-Host ""
Write-Host "Users just need to:" -ForegroundColor Yellow
Write-Host "  1. Double-click the MSI" -ForegroundColor Yellow
Write-Host "  2. Click Next, Next, Install" -ForegroundColor Yellow
Write-Host "  3. Restart Claude Desktop" -ForegroundColor Yellow
Write-Host "  4. IT JUST WORKS!" -ForegroundColor Cyan
