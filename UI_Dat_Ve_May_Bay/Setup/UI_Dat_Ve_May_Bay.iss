#define MyAppName      "UI Đặt Vé Máy Bay"
#define MyAppVersion   "1.0"
#define MyAppPublisher "Your Company"
#define MyAppExeName   "UI_Dat_Ve_May_Bay.exe"
#define MyAppSourceDir "..\bin\Release\net8.0-windows"

[Setup]
AppId={{F1A2B3C4-D5E6-7890-ABCD-EF1234567890}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=Setup_UI_Dat_Ve_May_Bay
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Tạo shortcut Desktop (mặc định tích sẵn)
Name: "desktopicon"; Description: "Tạo biểu tượng trên màn hình Desktop"; GroupDescription: "Biểu tượng:"; Flags: checkedonce

[Files]
; Copy toàn bộ thư mục Release vào nơi cài đặt
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Shortcut trong Start Menu
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Gỡ cài đặt {#MyAppName}"; Filename: "{uninstallexe}"
; Shortcut ngoài Desktop (chỉ tạo nếu user tích vào)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Hỏi người dùng có muốn chạy app ngay sau khi cài không
Filename: "{app}\{#MyAppExeName}"; Description: "Chạy {#MyAppName} ngay bây giờ"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Xóa file log và snapshot khi gỡ cài đặt (tuỳ chọn)
; Type: filesandordirs; Name: "{localappdata}\UI_Dat_Ve_May_Bay"
