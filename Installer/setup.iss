; StartMenuMGR Inno Setup Script
; 基準書準拠: インストール/アンインストール完備

#define MyAppName "StartMenuMGR"
#define MyAppVersion "01.01"
#define MyAppPublisher "DesktopKit"
#define MyAppExeName "StartMenuMGR.exe"
#define MyAppSource "..\bin\Publish"
#define MyOutputDir "..\bin\Installer"

[Setup]
AppId={{A3F7B2D1-8E4C-4A9F-B6D3-7C2E1F5A8B90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyAppName}_setup_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにアイコンを作成"; GroupDescription: "追加タスク:"; Flags: unchecked

[Files]
Source: "{#MyAppSource}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSource}\StartMenuMGR.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSource}\StartMenuMGR.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSource}\StartMenuMGR.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName} をアンインストール"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を起動"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; アンインストール時にプロセスを停止
Filename: "taskkill"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillApp"

[UninstallDelete]
; アンインストール時にアプリフォルダを完全削除
Type: filesandordirs; Name: "{app}"
