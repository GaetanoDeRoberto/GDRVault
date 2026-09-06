; GDRVault Installer
; GDR Design

#define MyAppName "GDRVault"
#define MyAppVersion "1.0"
#define MyAppPublisher "GDR Design"
#define MyAppURL "https://www.gdrdesign.it"
#define MyAppExeName "GDRVault.exe"

; Chrome extension ID attualmente utilizzato durante lo sviluppo.
; Verrà sostituito con l'ID definitivo del Chrome Web Store
; prima della release pubblica.
#define MyChromeExtensionId "kkhbegagejcbgajjhbdkpgafbcgckdc"

[Setup]
AppId={{58614EC6-F9CB-4597-B448-E2F9CB4F30C8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

PrivilegesRequired=admin

OutputBaseFilename=GDRVault-Setup
SetupIconFile=GDRVault.ico
SolidCompression=yes
WizardStyle=modern dynamic

UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; \
    Description: "{cm:CreateDesktopIcon}"; \
    GroupDescription: "{cm:AdditionalIcons}"; \
    Flags: unchecked

[Files]

; ============================================
; GDRVault desktop application
; ============================================

Source: "C:\Users\gaeta\Desktop\gdrvault-1.0\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

; ============================================
; GDRVault Bridge
; ============================================

Source: "C:\Users\gaeta\Desktop\gdrvault-bridge-1.0\*"; \
    DestDir: "{app}\Bridge"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]

Name: "{autoprograms}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"

Name: "{autodesktop}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"; \
    Tasks: desktopicon

[Registry]

; ============================================
; Chrome Native Messaging Host
; ============================================

Root: HKLM; \
    Subkey: "Software\Google\Chrome\NativeMessagingHosts\com.gdrvault.autofill"; \
    ValueType: string; \
    ValueName: ""; \
    ValueData: "{app}\NativeMessaging\com.gdrvault.autofill.chrome.json"; \
    Flags: uninsdeletekey

; ============================================
; Microsoft Edge Native Messaging Host
; ============================================

Root: HKLM; \
    Subkey: "Software\Microsoft\Edge\NativeMessagingHosts\com.gdrvault.autofill"; \
    ValueType: string; \
    ValueName: ""; \
    ValueData: "{app}\NativeMessaging\com.gdrvault.autofill.edge.json"; \
    Flags: uninsdeletekey

[Dirs]

Name: "{app}\NativeMessaging"

[UninstallDelete]

Type: filesandordirs; \
    Name: "{app}\NativeMessaging"

[Run]

Filename: "{app}\{#MyAppExeName}"; \
    Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
    Flags: nowait postinstall skipifsilent

[Code]

procedure CurStepChanged(CurStep: TSetupStep);
var
  ChromeManifest: String;
  EdgeManifest: String;
  BridgePath: String;
begin
  if CurStep = ssPostInstall then
  begin
    BridgePath := ExpandConstant('{app}\Bridge\GDRVault.Bridge.exe');
    StringChange(BridgePath, '\', '\\');

    // Chrome Native Messaging manifest
    ChromeManifest :=
      '{' + #13#10 +
      '  "name": "com.gdrvault.autofill",' + #13#10 +
      '  "description": "GDRVault Autofill Native Messaging Host",' + #13#10 +
      '  "path": "' + BridgePath + '",' + #13#10 +
      '  "type": "stdio",' + #13#10 +
      '  "allowed_origins": [' + #13#10 +
      '    "chrome-extension://{#MyChromeExtensionId}/"' + #13#10 +
      '  ]' + #13#10 +
      '}';

    SaveStringToFile(
      ExpandConstant('{app}\NativeMessaging\com.gdrvault.autofill.chrome.json'),
      ChromeManifest,
      False
    );

    // Edge Native Messaging manifest
    EdgeManifest :=
      '{' + #13#10 +
      '  "name": "com.gdrvault.autofill",' + #13#10 +
      '  "description": "GDRVault Autofill Native Messaging Host",' + #13#10 +
      '  "path": "' + BridgePath + '",' + #13#10 +
      '  "type": "stdio",' + #13#10 +
      '  "allowed_origins": [' + #13#10 +
      '    "chrome-extension://{#MyChromeExtensionId}/"' + #13#10 +
      '  ]' + #13#10 +
      '}';

    SaveStringToFile(
      ExpandConstant('{app}\NativeMessaging\com.gdrvault.autofill.edge.json'),
      EdgeManifest,
      False
    );
  end;
end;