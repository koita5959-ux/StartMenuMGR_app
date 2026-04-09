; StartMenuMGR Inno Setup Script
; 基準書準拠: インストール/アンインストール完備 + .NETランタイムチェック

#define MyAppName "StartMenuMGR"
#define MyAppVersion "01.02"
#define MyAppPublisher "DesktopKit"
#define MyAppExeName "StartMenuMGR.exe"
#define MyAppSource "StartMenuMGR\bin\Publish"
#define MyOutputDir "."
#define MyAppURL "https://github.com/koita5959-ux/StartMenuMGR_app"

[Setup]
AppId={{A3F7B2D1-8E4C-4A9F-B6D3-7C2E1F5A8B90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyAppName}_setup_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
SetupIconFile=Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
LicenseFile=ご利用にあたって.txt

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにアイコンを作成"; GroupDescription: "追加タスク:"

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

[Code]
{ .NET Desktop Runtime 8.0 の存在チェック }
function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('cmd.exe', '/c dotnet --list-runtimes 2>nul | findstr "Microsoft.WindowsDesktop.App 8." >nul', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  if not IsDotNet8Installed then
  begin
    if MsgBox(
      '{#MyAppName} の実行には .NET Desktop Runtime 8.0 が必要です。' + Chr(13) + Chr(10) +
      Chr(13) + Chr(10) +
      'お使いの環境にはインストールされていないようです。' + Chr(13) + Chr(10) +
      'Microsoft のダウンロードページを開きますか？' + Chr(13) + Chr(10) +
      Chr(13) + Chr(10) +
      '（インストール後に再度セットアップを実行してください）',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end;
end;
