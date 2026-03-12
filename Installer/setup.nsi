; ClaudeDevStudio Setup — NSIS Installer Script
; Single-EXE installer: downloads .NET 8 + Node.js if missing,
; installs all components, downloads Kokoro model, auto-configures Claude Desktop.
;
; Build: makensis setup.nsi   (from Installer\ directory after running build-setup.ps1)
; Output: Output\ClaudeDevStudio-Setup.exe

Unicode True
SetCompressor /SOLID lzma

; inetc plugin — bundled with NSIS, handles downloads with progress bars
; nsExec plugin — bundled with NSIS, runs commands silently

;---------------------------------------------------------------------------
; Metadata
;---------------------------------------------------------------------------
!define PRODUCT_NAME      "ClaudeDevStudio"
!define PRODUCT_VERSION   "1.1.0"
!define PRODUCT_PUBLISHER "Daniel E Gain"
!define PRODUCT_URL       "https://github.com/dectdan/Claude-Developer-Studio"
!define INSTALL_DIR       "$LocalAppData\ClaudeDevStudio"
!define MCP_INDEX         "$LocalAppData\ClaudeDevStudio\mcp-server\index.js"
!define VOICE_DIR         "$LocalAppData\ClaudeDevStudio\VoiceServer"
!define KOKORO_URL        "https://github.com/taylorchu/kokoro-onnx/releases/download/v0.2.0/kokoro.onnx"
!define DOTNET_URL        "https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-8.0-windows-x64-installer"
!define NODEJS_URL        "https://nodejs.org/dist/lts/win-x64/node.exe"
!define DOTNET_MIN        "8.0"
!define UNINSTALL_KEY     "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeDevStudio"

;---------------------------------------------------------------------------
; Includes
;---------------------------------------------------------------------------
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"

;---------------------------------------------------------------------------
; MUI Settings — clean modern wizard look
;---------------------------------------------------------------------------
!define MUI_ABORTWARNING
; Use custom icon if present — build-setup.ps1 copies it to Assets\AppIcon.ico
!ifdef HAVE_ICON
  !define MUI_ICON   "Assets\AppIcon.ico"
  !define MUI_UNICON "Assets\AppIcon.ico"
!endif
!define MUI_WELCOMEPAGE_TITLE  "Install ClaudeDevStudio v${PRODUCT_VERSION}"
!define MUI_WELCOMEPAGE_TEXT   "ClaudeDevStudio gives Claude AI persistent memory of your projects, voice alerts, and live Visual Studio integration.$\r$\n$\r$\nThis installer will:$\r$\n  • Install all required components$\r$\n  • Download .NET 8 and Node.js if needed$\r$\n  • Download the Kokoro voice model (~310 MB)$\r$\n  • Auto-configure Claude Desktop$\r$\n$\r$\nClick Install to continue."
!define MUI_FINISHPAGE_TITLE   "ClaudeDevStudio Installed!"
!define MUI_FINISHPAGE_TEXT    "Installation complete.$\r$\n$\r$\nRestart Claude Desktop to activate the AI tools.$\r$\n$\r$\nThe ClaudeDevStudio tray icon will appear in your system tray."
!define MUI_FINISHPAGE_RUN          "$LocalAppData\ClaudeDevStudio\TrayApp\ClaudeDevStudio.TrayApp.exe"
!define MUI_FINISHPAGE_RUN_TEXT     "Start ClaudeDevStudio now"
!define MUI_FINISHPAGE_LINK         "View on GitHub"
!define MUI_FINISHPAGE_LINK_LOCATION "${PRODUCT_URL}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

;---------------------------------------------------------------------------
; Installer attributes
;---------------------------------------------------------------------------
Name              "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile           "Output\ClaudeDevStudio-Setup.exe"
InstallDir        "${INSTALL_DIR}"
RequestExecutionLevel user   ; per-user install, no UAC prompt
ShowInstDetails   show

;---------------------------------------------------------------------------
; Section: Main Install
;---------------------------------------------------------------------------
Section "Install" SecMain

  SetOutPath "${INSTALL_DIR}"
  SetOverwrite on

  ;--- Step 1: Check + install .NET 8 Runtime ---
  DetailPrint "Checking .NET 8 Runtime..."
  nsExec::ExecToStack 'dotnet --list-runtimes'
  Pop $0  ; exit code
  Pop $1  ; stdout
  ${If} $0 != 0
    Goto InstallDotNet
  ${EndIf}
  ${If} $1 !~ "Microsoft.NETCore.App 8\."
    Goto InstallDotNet
  ${EndIf}
  DetailPrint ".NET 8 already installed. Skipping."
  Goto DotNetDone

  InstallDotNet:
    DetailPrint "Downloading .NET 8 Runtime (~60 MB)..."
    inetc::get /CAPTION "Downloading .NET 8 Runtime" /BANNER "Required for ClaudeDevStudio..." \
      "https://aka.ms/dotnet/8.0/dotnet-runtime-win-x64.exe" \
      "$TEMP\dotnet8-runtime.exe" /END
    Pop $0
    ${If} $0 != "OK"
      MessageBox MB_ICONEXCLAMATION "Could not download .NET 8 Runtime.$\r$\nPlease install it manually from https://dotnet.microsoft.com/download/dotnet/8.0"
    ${Else}
      DetailPrint "Installing .NET 8 Runtime silently..."
      ExecWait '"$TEMP\dotnet8-runtime.exe" /quiet /norestart' $0
      DetailPrint ".NET 8 install exit code: $0"
      Delete "$TEMP\dotnet8-runtime.exe"
    ${EndIf}

  DotNetDone:

  ;--- Step 2: Check + install Node.js ---
  DetailPrint "Checking Node.js..."
  nsExec::ExecToStack 'node --version'
  Pop $0
  Pop $1
  ${If} $0 != 0
    Goto InstallNode
  ${EndIf}
  DetailPrint "Node.js already installed ($1). Skipping."
  Goto NodeDone

  InstallNode:
    DetailPrint "Downloading Node.js LTS (~30 MB)..."
    inetc::get /CAPTION "Downloading Node.js LTS" /BANNER "Required for MCP server..." \
      "https://nodejs.org/dist/lts/node-v22.18.0-x64.msi" \
      "$TEMP\nodejs-lts.msi" /END
    Pop $0
    ${If} $0 != "OK"
      MessageBox MB_ICONEXCLAMATION "Could not download Node.js.$\r$\nPlease install it manually from https://nodejs.org"
    ${Else}
      DetailPrint "Installing Node.js silently..."
      ExecWait 'msiexec /i "$TEMP\nodejs-lts.msi" /quiet /norestart' $0
      DetailPrint "Node.js install exit code: $0"
      Delete "$TEMP\nodejs-lts.msi"
    ${EndIf}

  NodeDone:

  ;--- Step 3: Install CDS application files ---
  DetailPrint "Installing ClaudeDevStudio files..."

  ; CLI
  SetOutPath "${INSTALL_DIR}\CLI"
  File /r "..\build\CLI\*.*"

  ; TrayApp
  SetOutPath "${INSTALL_DIR}\TrayApp"
  File /r "..\build\TrayApp\*.*"

  ; Dashboard
  SetOutPath "${INSTALL_DIR}\Dashboard"
  File /r "..\build\Dashboard\*.*"

  ; MCP server
  SetOutPath "${INSTALL_DIR}\mcp-server"
  File /r "..\build\mcp-server\*.*"

  ; VoiceServer (without kokoro.onnx — downloaded separately)
  SetOutPath "${INSTALL_DIR}\VoiceServer"
  File /r "..\build\VoiceServer\*.*"

  ; VSIX extension
  SetOutPath "${INSTALL_DIR}\VSExtension"
  File "..\build\CdsVsBridge.vsix"

  ; Config script
  SetOutPath "${INSTALL_DIR}"
  File "..\Installer\ConfigureClaudeDesktop.ps1"

  ;--- Step 4: npm install (get node_modules) ---
  DetailPrint "Installing MCP server dependencies..."
  nsExec::ExecToLog '"$SYSDIR\cmd.exe" /c cd /d "${INSTALL_DIR}\mcp-server" && npm install --production 2>&1'

  ;--- Step 5: Download Kokoro voice model ---
  DetailPrint "Downloading Kokoro voice model (~310 MB)..."
  DetailPrint "This gives ClaudeDevStudio on-machine voice — no API cost, no data sent anywhere."
  ${IfNot} ${FileExists} "${VOICE_DIR}\kokoro.onnx"
    inetc::get /CAPTION "Downloading Voice Model" \
               /BANNER "Kokoro TTS model (~310 MB) — one-time download..." \
               "${KOKORO_URL}" \
               "${VOICE_DIR}\kokoro.onnx" /END
    Pop $0
    ${If} $0 != "OK"
      DetailPrint "Voice model download failed ($0) — voice features disabled until downloaded."
      DetailPrint "Run Start-VoiceServer.ps1 later to retry."
    ${Else}
      DetailPrint "Voice model downloaded successfully."
    ${EndIf}
  ${Else}
    DetailPrint "Voice model already present. Skipping download."
  ${EndIf}

  ;--- Step 6: Install VSIX into Visual Studio (if VS 2022 present) ---
  DetailPrint "Checking for Visual Studio 2022..."
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\VisualStudio\17.0" "InstallDir"
  ${If} $0 != ""
    DetailPrint "Visual Studio 2022 found — installing VS Bridge extension..."
    nsExec::ExecToLog '"$0\VSIXInstaller.exe" /quiet "${INSTALL_DIR}\VSExtension\CdsVsBridge.vsix"'
    DetailPrint "VS Bridge extension installed."
  ${Else}
    DetailPrint "Visual Studio 2022 not found — skipping VS Bridge extension."
  ${EndIf}

  ;--- Step 7: Configure Claude Desktop ---
  DetailPrint "Configuring Claude Desktop..."
  nsExec::ExecToLog 'powershell.exe -ExecutionPolicy Bypass -NoProfile -File "${INSTALL_DIR}\ConfigureClaudeDesktop.ps1" -MCPServerPath "${MCP_INDEX}"'

  ;--- Step 8: Create data directories ---
  CreateDirectory "$DOCUMENTS\ClaudeDevStudio\Projects"
  CreateDirectory "$DOCUMENTS\ClaudeDevStudio\Backups"
  CreateDirectory "$APPDATA\Claude"

  ;--- Step 9: Registry + shortcuts ---
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName"      "${PRODUCT_NAME}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion"   "${PRODUCT_VERSION}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher"        "${PRODUCT_PUBLISHER}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "URLInfoAbout"     "${PRODUCT_URL}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation"  "${INSTALL_DIR}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString"  '"${INSTALL_DIR}\Uninstall.exe"'
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair"  1

  ; Autostart TrayApp
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" \
    "ClaudeDevStudio" '"${INSTALL_DIR}\TrayApp\ClaudeDevStudio.TrayApp.exe"'

  ; Start Menu shortcut
  CreateDirectory "$SMPROGRAMS\ClaudeDevStudio"
  CreateShortcut "$SMPROGRAMS\ClaudeDevStudio\ClaudeDevStudio.lnk" \
    "${INSTALL_DIR}\Dashboard\ClaudeDevStudio.Dashboard.exe"
  CreateShortcut "$SMPROGRAMS\ClaudeDevStudio\Uninstall.lnk" \
    "${INSTALL_DIR}\Uninstall.exe"

  ; Write uninstaller
  WriteUninstaller "${INSTALL_DIR}\Uninstall.exe"

  DetailPrint "Installation complete!"

SectionEnd

;---------------------------------------------------------------------------
; Uninstall Section
;---------------------------------------------------------------------------
Section "Uninstall"

  ; Stop TrayApp if running
  nsExec::ExecToLog 'taskkill /IM ClaudeDevStudio.TrayApp.exe /F'
  nsExec::ExecToLog 'taskkill /IM VoiceServer.exe /F'

  ; Remove files (keep user data in Documents)
  RMDir /r "${INSTALL_DIR}\CLI"
  RMDir /r "${INSTALL_DIR}\TrayApp"
  RMDir /r "${INSTALL_DIR}\Dashboard"
  RMDir /r "${INSTALL_DIR}\mcp-server"
  RMDir /r "${INSTALL_DIR}\VoiceServer"
  RMDir /r "${INSTALL_DIR}\VSExtension"
  Delete   "${INSTALL_DIR}\ConfigureClaudeDesktop.ps1"
  Delete   "${INSTALL_DIR}\Uninstall.exe"
  RMDir    "${INSTALL_DIR}"

  ; Remove autostart + registry
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "ClaudeDevStudio"
  DeleteRegKey   HKCU "${UNINSTALL_KEY}"
  DeleteRegKey   HKCU "Software\ClaudeDevStudio"

  ; Remove Start Menu
  Delete "$SMPROGRAMS\ClaudeDevStudio\ClaudeDevStudio.lnk"
  Delete "$SMPROGRAMS\ClaudeDevStudio\Uninstall.lnk"
  RMDir  "$SMPROGRAMS\ClaudeDevStudio"

  MessageBox MB_ICONINFORMATION "ClaudeDevStudio has been uninstalled.$\r$\nYour project data in Documents\ClaudeDevStudio has been kept."

SectionEnd
