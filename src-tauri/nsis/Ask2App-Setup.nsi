; =====================================================================
; Ask2App | My Platform -- Modern NSIS Deployment Package Installer
; =====================================================================

Unicode True

!define PRODUCT_NAME "Ask2App | My Platform"
!define PRODUCT_BRANDING "Ask2App"
!define PRODUCT_PUBLISHER "Ask2App"
!define PRODUCT_VERSION "1.0.0.0"
!define PRODUCT_WEB_SITE "https://ask2app.local"
!define PRODUCT_DIR_REGKEY "Software\Ask2App\MyPlatform"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Ask2App"
!define PRODUCT_EXE "Ask2App.exe"

; Modern UI 2
!include "MUI2.nsh"
!include "FileFunc.nsh"

; Interface Configuration
!define MUI_ABORTWARNING
!define MUI_ICON "..\icons\icon.ico"
!define MUI_UNICON "..\icons\icon.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "..\icons\nsis-header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "..\icons\nsis-sidebar.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "..\icons\nsis-sidebar.bmp"

; Welcome page
!define MUI_WELCOMEPAGE_TITLE "Welcome to Ask2App | My Platform Setup"
!define MUI_WELCOMEPAGE_TEXT "This wizard will install Ask2App | My Platform on your computer.$\r$\n$\r$\nAsk2App is a free, modern platform designed to replace enterprise systems with high-velocity .NET 10 Minimal APIs and MudBlazor 8 WebAssembly.$\r$\n$\r$\nClick Next to continue."
!insertmacro MUI_PAGE_WELCOME

; Directory selection page
!define MUI_DIRECTORYPAGE_TEXT_TOP "Setup will install Ask2App | My Platform in the following folder. To install in a different folder, click Browse and select another folder."
!insertmacro MUI_PAGE_DIRECTORY

; Installation progress page
!insertmacro MUI_PAGE_INSTFILES

; Finish page with launch checkbox
!define MUI_FINISHPAGE_RUN "$INSTDIR\${PRODUCT_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Launch Ask2App | My Platform now"
!define MUI_FINISHPAGE_SHOWREADME ""
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Create Desktop Shortcut"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION CreateDesktopShortcutFunc
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

; Language files
!insertmacro MUI_LANGUAGE "English"

; Installer General Attributes
Name "${PRODUCT_NAME}"
OutFile "..\..\Ask2App-Setup.exe"
InstallDir "$PROGRAMFILES64\Ask2App"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show
RequestExecutionLevel admin

Section "MainSection" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer

  ; Copy Application Binaries & Assets
  File /r "..\..\dist\*.*"
  File /r "..\..\src-tauri\binaries\*.*"
  
  ; Write registration and uninstaller
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\${PRODUCT_EXE}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"

  ; Create Start Menu Shortcuts
  CreateDirectory "$SMPROGRAMS\Ask2App"
  CreateShortcut "$SMPROGRAMS\Ask2App\${PRODUCT_NAME}.lnk" "$INSTDIR\${PRODUCT_EXE}"
  CreateShortcut "$SMPROGRAMS\Ask2App\Uninstall Ask2App.lnk" "$INSTDIR\uninst.exe"
SectionEnd

Function CreateDesktopShortcutFunc
  CreateShortcut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\${PRODUCT_EXE}"
FunctionEnd

Section Uninstall
  ; Remove shortcuts
  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
  Delete "$SMPROGRAMS\Ask2App\${PRODUCT_NAME}.lnk"
  Delete "$SMPROGRAMS\Ask2App\Uninstall Ask2App.lnk"
  RMDir "$SMPROGRAMS\Ask2App"

  ; Remove files and installation folder
  RMDir /r "$INSTDIR"

  ; Clean registry
  DeleteRegKey HKLM "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
SectionEnd