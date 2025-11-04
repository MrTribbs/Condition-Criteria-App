[Setup]
AppName=Condition Criteria App
AppVersion=2.1.2
DefaultDirName={pf}\Condition Criteria App
OutputBaseFilename=ConditionCriteriaInstaller
Compression=lzma
SolidCompression=yes
DisableDirPage=yes
DisableProgramGroupPage=yes
SetupIconFile=installer-build\app.ico

[Files]
Source: "installer-build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "installer-build\app.ico"; DestDir: "{app}"

[Icons]
Name: "{group}\Condition Criteria App"; Filename: "{app}\Condition Criteria App.exe"
Name: "{userdesktop}\Condition Criteria App"; Filename: "{app}\Condition Criteria App.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\Condition Criteria App.exe"; Description: "Launch Condition Criteria App"; Flags: nowait postinstall skipifsilent