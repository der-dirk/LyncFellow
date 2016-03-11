!include "FileFunc.nsh"
!include "MUI2.nsh"

!define APPNAME "LyncFellow+"
!define APPNAMESHORT "LyncFellow+"
!define APPVERSION "1.0.0.0"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAMESHORT}"

!define MUI_ICON "..\Global\LyncFellow.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP "..\Global\LyncFellowInstaller.bmp"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_UNABORTWARNING
!define MUI_UNFINISHPAGE_NOAUTOCLOSE
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APPNAMESHORT}.exe"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Name "${APPNAME}"
Caption "${APPNAME} Setup (Version ${APPVERSION})"
OutFile "${APPNAMESHORT}Setup.exe"
RequestExecutionLevel user
InstallDir "$LOCALAPPDATA\${APPNAMESHORT}"

Section
	Call CheckIsDotNetInstalled
	Call CheckIsRunning
	SetOutPath "$INSTDIR"
	File "..\LyncFellow\bin\Release\${APPNAMESHORT}.exe"
	File "..\LyncFellow\bin\Release\Microsoft.Lync.Model.dll"
	File "..\LyncFellow\bin\Release\Microsoft.Office.Uc.dll"
	WriteUninstaller "$INSTDIR\uninstall.exe"
	WriteRegStr HKCU "${UNINST_KEY}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
	WriteRegStr HKCU "${UNINST_KEY}" "DisplayName" "${APPNAME}"
	WriteRegStr HKCU "${UNINST_KEY}" "Publisher" "Gl�ck & Kanja Consulting AG"
	WriteRegStr HKCU "${UNINST_KEY}" "DisplayIcon" "$\"$INSTDIR\${APPNAMESHORT}.exe$\""
	WriteRegStr HKCU "${UNINST_KEY}" "DisplayVersion" "${APPVERSION}"
	${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
	IntFmt $0 "0x%08X" $0
	WriteRegDWORD HKCU "${UNINST_KEY}" "EstimatedSize" "$0"
	CreateDirectory "$SMPROGRAMS\${APPNAME}"
	CreateShortCut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${APPNAMESHORT}.exe"
	CreateShortCut "$SMPROGRAMS\${APPNAME}\Uninstalll ${APPNAME}.lnk" "$\"$INSTDIR\uninstall.exe$\""
	CreateShortCut "$SMSTARTUP\${APPNAME}.lnk" "$INSTDIR\${APPNAMESHORT}.exe"
SectionEnd

Section "uninstall"
	Call un.CheckIsRunning
	Delete "$SMSTARTUP\${APPNAME}.lnk"
	RMDir /r /REBOOTOK "$SMPROGRAMS\${APPNAME}"
	DeleteRegKey HKCU "${UNINST_KEY}"
	RMDir /r /REBOOTOK "$INSTDIR"
SectionEnd


Function CheckIsDotNetInstalled
	ReadRegDWORD $R0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727" Install
	IntCmp $R0 1 dotNetInstalled
	    MessageBox MB_OK|MB_ICONEXCLAMATION "Microsoft .NET Framework v2.0, v3.0 or v3.5 is required for this application.$\r$\n$\r$\nPlease download and install it before installing this application." /SD IDOK
	    Abort
	dotNetInstalled:
FunctionEnd

!macro SharedInstallUninstallFunctions un

Function ${un}CheckIsRunning
	System::Call 'kernel32::OpenMutex(i 0x100000, b 0, t "LyncFellowApplication") i .R0'
	IntCmp $R0 0 notRunning
	    System::Call 'kernel32::CloseHandle(i $R0)'
	    MessageBox MB_OK|MB_ICONEXCLAMATION "${APPNAME} is running.$\r$\n$\r$\nPlease close it before installing or uninstalling." /SD IDOK
	    Abort
	notRunning:
FunctionEnd

!macroend
 
!insertmacro SharedInstallUninstallFunctions ""
!insertmacro SharedInstallUninstallFunctions "un."
