; coursee installer (Inno Setup 6.3 or later).
; Built by build\installer.cmd, which publishes the app and passes the version:
;   ISCC.exe /DAppVersion=<version> /DPublishDir=<published app folder> build\installer\coursee.iss
;
; Per-user install: no administrator rights, installed in %LOCALAPPDATA%\Programs\coursee.
; User data (%LOCALAPPDATA%\coursee: settings, recents, logs) is left in place on uninstall.

#ifndef AppVersion
  #error AppVersion is not defined. Run build\installer.cmd instead of compiling this file directly.
#endif
#ifndef PublishDir
  #error PublishDir is not defined. Run build\installer.cmd instead of compiling this file directly.
#endif

#define AppName "coursee"
#define AppExe "coursee.exe"
; Must match the value written by the app (RegistryAutoStart) so uninstall can remove it.
#define RunKey "Software\Microsoft\Windows\CurrentVersion\Run"

[Setup]
; Fixed forever: it is how Windows recognizes an upgrade of the same application.
AppId={{6F3E2B1A-8C4D-4E7F-9A2B-C0D1E2F3A4B5}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=dyze
VersionInfoVersion={#AppVersion}
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; The app creates this mutex (SingleInstance.cs): setup and uninstall ask to quit coursee first.
AppMutex=Local\coursee-single-instance
SetupIconFile=..\..\src\coursee.App\Assets\coursee.ico
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=..\..\artifacts\installer
OutputBaseFilename=coursee-setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "autostart"; Description: "Start coursee when I sign in to Windows"
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Same command the app writes from Settings (AutoStartCommand.Build), so the "Start with Windows"
; checkbox shows it as on. --hidden: at sign-in coursee stays in the notification area.
Root: HKCU; Subkey: "{#RunKey}"; ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\{#AppExe}"" --hidden"; Tasks: autostart

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start coursee now"; Flags: nowait postinstall skipifsilent

[Code]
// Auto start can also be switched on later from coursee's Settings, without this installer
// knowing; remove the value on uninstall in every case, but only if it points at this copy
// (with or without arguments: older versions wrote the quoted path alone).
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command: String;
  Quoted: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    Quoted := '"' + ExpandConstant('{app}\{#AppExe}') + '"';
    if RegQueryStringValue(HKEY_CURRENT_USER, '{#RunKey}', '{#AppName}', Command)
      and (CompareText(Copy(Trim(Command), 1, Length(Quoted)), Quoted) = 0) then
      RegDeleteValue(HKEY_CURRENT_USER, '{#RunKey}', '{#AppName}');
  end;
end;
