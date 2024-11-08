; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "SuVi Player"
#define MyAppVersion "1.0.0.0"
#define MyAppPublisher "Ahmed Ismail"
#define MyAppURL "elcoder01@gmail.com"
#define MyAppContact "elcoder01@gmail.com"
#define MyAppExeName "SuViPlayer.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{49C11D35-B445-419B-8CB3-FA510690FA76}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppContact={#MyAppContact}
AppPublisherURL={#MyAppURL}
;AppSupportURL={#MyAppURL}
;AppUpdatesURL={#MyAppURL}
AppCopyright=© 2024 SuVi Player. Ahmed Ismail.
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users.)
PrivilegesRequired=admin
OutputDir=C:\Users\elcoder9\Desktop\suvi player installer project
OutputBaseFilename=SuViPlayer-{#MyAppVersion}-Setup
SetupIconFile=C:\Users\elcoder9\Desktop\suvi player installer project\suvi_high_resolution_logo_modified.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
;Compression=lzma
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\LibVLCSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\LibVLCSharp.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\LibVLCSharp.WinForms.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\LibVLCSharp.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\SuViPlayer.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\SuViPlayer.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Buffers.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Buffers.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Memory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Memory.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Numerics.Vectors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Numerics.Vectors.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\System.Runtime.CompilerServices.Unsafe.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\elcoder01\Desktop\suvi player installer project\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

