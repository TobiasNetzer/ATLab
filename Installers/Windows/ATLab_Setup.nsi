!define PRODUCT_NAME "ATLab"
!define PRODUCT_PUBLISHER "Tobias Netzer"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"

; ATLab Analyzer is installed alongside ATLab, in its own subfolder (published to out\Analyzer)
!define ANALYZER_NAME "ATLab Analyzer"
!define ANALYZER_EXE "ATLab.Analyzer.exe"
!define ANALYZER_DIR "$INSTDIR\Analyzer"

; Both apps are published framework dependent and need this .NET runtime
!define DOTNET_MAJOR "10"
!define DOTNET_DOWNLOAD_URL "https://dotnet.microsoft.com/download/dotnet/10.0"

; List of the installed files in the program folder: uninstalling and updating delete exactly these files, so files
; the user added (e.g. a J-Link or FTDI DLL) stay and a program folder like C:\Program Files is never emptied
!define MANIFEST "install.manifest"

; Extract version from published ATLab.exe
!getdllversion "..\..\out\ATLab.exe" ATLAB_VER_
!define ATLAB_VERSION "${ATLAB_VER_1}.${ATLAB_VER_2}.${ATLAB_VER_3}.${ATLAB_VER_4}"

!system 'powershell -NoProfile -ExecutionPolicy Bypass -File "${__FILEDIR__}\gen-manifest.ps1" -Source "${__FILEDIR__}\..\..\out" -Output "${__FILEDIR__}\manifest.nsh"' = 0
!include "${__FILEDIR__}\manifest.nsh"

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"

!define MUI_ABORTWARNING
!define MUI_ICON "install.ico"
!define MUI_UNICON "uninstall.ico"
!define MUI_HEADERIMAGE_BITMAP "header.bmp"

!define MUI_WELCOMEPAGE_TITLE "Welcome to the ${PRODUCT_NAME} Installer"
!define MUI_WELCOMEPAGE_TITLE_3LINES
!define MUI_WELCOMEPAGE_TEXT "${PRODUCT_NAME} is a cross-platform automated testing toolkit for interfacing with hardware through a test interface adapter and controlling external test instruments through remote control interfaces.$\r$\n$\nClick Next to continue."

; Launched through a function, so ATLab does not inherit the installer's administrator rights
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "Launch ${PRODUCT_NAME}"
!define MUI_FINISHPAGE_RUN_FUNCTION LaunchATLab

!define MUI_FINISHPAGE_SHOWREADME
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Create a desktop shortcut"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION CreateDesktopShortcut

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Name "${PRODUCT_NAME}"
OutFile "..\..\Installers\Windows\ATLab-setup.exe"
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"
RequestExecutionLevel admin
BrandingText "${PRODUCT_NAME} Installer"

VIProductVersion "${ATLAB_VERSION}"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey "FileDescription" "${PRODUCT_NAME} Installer"
VIAddVersionKey "ProductVersion" "${ATLAB_VERSION}"
VIAddVersionKey "FileVersion" "${ATLAB_VERSION}"
VIAddVersionKey "LegalCopyright" "© 2026 ${PRODUCT_PUBLISHER}"

; ----- Shared by installer and uninstaller -----

; Sets $1 to 1 if the file exists and cannot be opened for writing, i.e. the program is running
!macro CHECK_LOCKED file
  ${If} ${FileExists} "${file}"
    ClearErrors
    FileOpen $0 "${file}" a
    ${If} ${Errors}
      StrCpy $1 1
    ${Else}
      FileClose $0
    ${EndIf}
  ${EndIf}
!macroend

!macro DEFINE_FUNCTIONS un

; Files of a running program cannot be replaced or deleted
Function ${un}EnsureAppsClosed
  check:
    StrCpy $1 0
    !insertmacro CHECK_LOCKED "$INSTDIR\${PRODUCT_NAME}.exe"
    !insertmacro CHECK_LOCKED "${ANALYZER_DIR}\${ANALYZER_EXE}"
    StrCmp $1 0 done
    MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "${PRODUCT_NAME} or ${ANALYZER_NAME} is still running.$\r$\n$\r$\nClose it and click Retry." /SD IDCANCEL IDRETRY check
    Quit
  done:
FunctionEnd

; Deletes the files and then the (empty) folders listed in the manifest of the installed version
Function ${un}RemoveInstalledFiles
  ClearErrors
  FileOpen $9 "$INSTDIR\${MANIFEST}" r
  IfErrors done

  loop:
    ClearErrors
    FileRead $9 $8
    IfErrors close

    ; Strip the line break
    StrCpy $7 $8 1 -1
    StrCmp $7 "$\n" 0 +2
      StrCpy $8 $8 -1
    StrCpy $7 $8 1 -1
    StrCmp $7 "$\r" 0 +2
      StrCpy $8 $8 -1

    StrCpy $7 $8 2
    StrCpy $8 $8 "" 2
    StrCmp $8 "" loop

    StrCmp $7 "F|" 0 +3
      Delete "$INSTDIR\$8"
      Goto loop
    StrCmp $7 "D|" 0 loop
      RMDir "$INSTDIR\$8"
      Goto loop

  close:
    FileClose $9
    Delete "$INSTDIR\${MANIFEST}"
  done:
FunctionEnd

!macroend

!insertmacro DEFINE_FUNCTIONS ""
!insertmacro DEFINE_FUNCTIONS "un."

; ----- Installer -----

Function .onInit
  ; For all users: Start menu and desktop shortcuts of the public profile, not of the admin account that installs
  SetShellVarContext all
  ; The installer is a 32-bit program; without this, its registry entries would land in WOW6432Node
  SetRegView 64

  Call CheckDotNetRuntime
FunctionEnd

Function CheckDotNetRuntime
  IfSilent done
  FindFirst $0 $1 "$PROGRAMFILES64\dotnet\shared\Microsoft.NETCore.App\${DOTNET_MAJOR}.*"
  FindClose $0
  StrCmp $1 "" 0 done

  MessageBox MB_YESNO|MB_ICONEXCLAMATION "${PRODUCT_NAME} and ${ANALYZER_NAME} need the .NET ${DOTNET_MAJOR} Runtime (x64), which is not installed on this computer.$\r$\n$\r$\nOpen the download page now? Setup continues either way; install the runtime before you start ${PRODUCT_NAME}." IDNO done
  ; Through Explorer, so the browser does not run with the installer's administrator rights
  Exec '"$WINDIR\explorer.exe" "${DOTNET_DOWNLOAD_URL}"'
  done:
FunctionEnd

Function LaunchATLab
  ; Through Explorer, so ATLab runs with the user's normal rights: settings go to the user's profile and drag & drop
  ; from Explorer works
  Exec '"$WINDIR\explorer.exe" "$INSTDIR\${PRODUCT_NAME}.exe"'
FunctionEnd

Function CreateDesktopShortcut
    CreateShortcut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\${PRODUCT_NAME}.exe"
    CreateShortcut "$DESKTOP\${ANALYZER_NAME}.lnk" "${ANALYZER_DIR}\${ANALYZER_EXE}"
FunctionEnd

; Earlier installers created the shortcuts for the installing user only and wrote the uninstall entry to the 32-bit
; registry; remove those, so an update leaves no duplicates
Function RemoveLegacyEntries
  SetShellVarContext current
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\${ANALYZER_NAME}.lnk"
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
  Delete "$DESKTOP\${ANALYZER_NAME}.lnk"
  SetShellVarContext all

  SetRegView 32
  DeleteRegKey HKLM "${UNINSTALL_KEY}"
  SetRegView 64
FunctionEnd

Section "Install ATLab"

  SetOutPath "$INSTDIR"

  Call EnsureAppsClosed

  ; Update: remove the files of the installed version first, so files the new version no longer has do not stay
  Call RemoveInstalledFiles
  Call RemoveLegacyEntries

  File /r "..\..\out\*.*"
  ${GetFileVersion} "$INSTDIR\ATLab.exe" $R0

  FileOpen $9 "$INSTDIR\${MANIFEST}" w
  !insertmacro WRITE_MANIFEST
  FileClose $9

  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortcut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\${PRODUCT_NAME}.exe"

  WriteRegStr HKLM "Software\Classes\.atlab" "" "${PRODUCT_NAME}File"
  WriteRegStr HKLM "Software\Classes\${PRODUCT_NAME}File" "" "${PRODUCT_NAME} File"
  WriteRegStr HKLM "Software\Classes\${PRODUCT_NAME}File\DefaultIcon" "" "$INSTDIR\${PRODUCT_NAME}.exe,0"
  WriteRegStr HKLM "Software\Classes\${PRODUCT_NAME}File\shell\open\command" "" '"$INSTDIR\${PRODUCT_NAME}.exe" "%1"'

  ; ATLab Analyzer: Start menu and App Paths (found by name, e.g. Win+R)
  CreateShortcut "$SMPROGRAMS\${PRODUCT_NAME}\${ANALYZER_NAME}.lnk" "${ANALYZER_DIR}\${ANALYZER_EXE}"

  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\App Paths\${ANALYZER_EXE}" "" "${ANALYZER_DIR}\${ANALYZER_EXE}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\App Paths\${ANALYZER_EXE}" "Path" "${ANALYZER_DIR}"

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayVersion" "$R0"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\${PRODUCT_NAME}.exe"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "UninstallString" "$INSTDIR\Uninstall.exe"

  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair" 1

SectionEnd

Section -Post

    ${GetSize} "$INSTDIR" "/S=0B" $0 $1 $2
    IntOp $0 $0 / 1024
    WriteRegDWORD HKLM "${UNINSTALL_KEY}" "EstimatedSize" $0

SectionEnd

; ----- Uninstaller -----

Function un.onInit
  SetShellVarContext all
  SetRegView 64
FunctionEnd

Section "Uninstall"

  Call un.EnsureAppsClosed

  Delete "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\${ANALYZER_NAME}.lnk"
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
  Delete "$DESKTOP\${ANALYZER_NAME}.lnk"

  ; Only the installed files; the program folder itself only if nothing else is left in it
  Call un.RemoveInstalledFiles
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  DeleteRegKey HKLM "${UNINSTALL_KEY}"
  DeleteRegKey HKLM "Software\Classes\.atlab"
  DeleteRegKey HKLM "Software\Classes\${PRODUCT_NAME}File"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\App Paths\${ANALYZER_EXE}"

  ; Uninstall entry of earlier installers in the 32-bit registry
  SetRegView 32
  DeleteRegKey HKLM "${UNINSTALL_KEY}"
  SetRegView 64

SectionEnd
