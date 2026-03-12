# build-setup.ps1 - Build ClaudeDevStudio-Setup.exe
# Run from D:\Projects\ClaudeDevStudio\Installer\ as Administrator
# Requires: choco install nsis -y

$ErrorActionPreference = "Stop"
$Root      = "D:\Projects\ClaudeDevStudio"
$Installer = "$Root\Installer"
$Build     = "$Installer\build"
$Output    = "$Installer\Output"

Write-Host "===== ClaudeDevStudio NSIS Installer Build =====" -ForegroundColor Cyan

# ---- 0. Verify NSIS ----
$makensis = "C:\Program Files (x86)\NSIS\makensis.exe"
if (!(Test-Path $makensis)) {
    Write-Host "ERROR: NSIS not found. Run as admin: choco install nsis -y" -ForegroundColor Red
    exit 1
}
Write-Host "  NSIS found." -ForegroundColor Green

# ---- 1. Build .NET projects ----
Write-Host "`n[1/6] Building .NET projects..." -ForegroundColor Yellow

Write-Host "  Building VoiceServer..."
dotnet publish "$Root\VoiceServer\VoiceServer.csproj" -c Release -r win-x64 --self-contained false -o "$Build\VoiceServer" | Out-Null

Write-Host "  Building VS Bridge VSIX..."
dotnet build "$Root\VSExtension\CdsVsBridge\CdsVsBridge.csproj" -c Release | Out-Null
$vsix = Get-ChildItem "$Root\VSExtension\CdsVsBridge\bin\Release\*.vsix" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($vsix) {
    New-Item -ItemType Directory -Force -Path "$Build\VSExtension" | Out-Null
    Copy-Item $vsix.FullName "$Build\VSExtension\CdsVsBridge.vsix" -Force
    Write-Host "  VSIX: $($vsix.Name)" -ForegroundColor Green
} else {
    Write-Host "  WARNING: VSIX not found - VS Bridge won't be bundled" -ForegroundColor Yellow
}
Write-Host "  .NET builds done." -ForegroundColor Green

# ---- 2. Stage MCP server ----
Write-Host "`n[2/6] Staging MCP server..." -ForegroundColor Yellow
$mcpDest = "$Build\mcp-server"
New-Item -ItemType Directory -Force -Path $mcpDest | Out-Null
Copy-Item "$Root\mcp-server\index.js"     $mcpDest -Force
Copy-Item "$Root\mcp-server\package.json" $mcpDest -Force
Write-Host "  MCP server staged." -ForegroundColor Green

# ---- 3. Stage Kokoro model ----
Write-Host "`n[3/6] Staging Kokoro voice model..." -ForegroundColor Yellow
$kokoroSrc = "C:\Users\Big_D\AppData\Local\ClaudeDevStudio\VoiceServer\kokoro.onnx"
if (Test-Path $kokoroSrc) {
    $size = [int]((Get-Item $kokoroSrc).Length / 1MB)
    Write-Host "  Found kokoro.onnx ($size MB) - copying into build..."
    Copy-Item $kokoroSrc "$Build\VoiceServer\kokoro.onnx" -Force
    Write-Host "  Kokoro model bundled." -ForegroundColor Green
} else {
    Write-Host "  WARNING: kokoro.onnx not found - installer will download it instead." -ForegroundColor Yellow
}

# ---- 4. Copy config script + assets ----
Write-Host "`n[4/6] Copying installer assets..." -ForegroundColor Yellow
Copy-Item "$Installer\ConfigureClaudeDesktop.ps1" "$Build\" -Force
New-Item -ItemType Directory -Force -Path "$Installer\Assets" | Out-Null
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$icon = "$Root\VSExtension\CdsVsBridge\Resources\AppIcon.ico"
if (Test-Path $icon) {
    Copy-Item $icon "$Installer\Assets\AppIcon.ico" -Force
    Write-Host "  Icon staged." -ForegroundColor Green
}
Write-Host "  Assets ready." -ForegroundColor Green

# ---- 5. Run NSIS ----
Write-Host "`n[5/6] Compiling installer with NSIS..." -ForegroundColor Yellow
$nsisArgs = @("/V3")
if (Test-Path "$Installer\Assets\AppIcon.ico") { $nsisArgs += "/DHAVE_ICON" }
$nsisArgs += "setup.nsi"
Push-Location $Installer
& $makensis @nsisArgs
$nsisExit = $LASTEXITCODE
Pop-Location
if ($nsisExit -ne 0) {
    Write-Host "NSIS build FAILED (exit $nsisExit)" -ForegroundColor Red
    exit 1
}
Write-Host "  NSIS compile done." -ForegroundColor Green

# ---- 6. Report ----
$exe = "$Output\ClaudeDevStudio-Setup.exe"
if (Test-Path $exe) {
    $sizeMB = [int]((Get-Item $exe).Length / 1MB)
    Write-Host "`n===== BUILD COMPLETE =====" -ForegroundColor Cyan
    Write-Host "  Output : $exe" -ForegroundColor Green
    Write-Host "  Size   : $sizeMB MB" -ForegroundColor Green
    Write-Host "  Drop this EXE anywhere and double-click to install." -ForegroundColor White
} else {
    Write-Host "ERROR: Output EXE not found!" -ForegroundColor Red
    exit 1
}
