[Setup]
AppName=Condition Criteria App
AppVersion=1.0.0
DefaultDirName={pf}\Condition Criteria App
OutputBaseFilename=ConditionCriteriaInstaller
Compression=lzma
SolidCompression=yes
DisableDirPage=yes
DisableProgramGroupPage=yes

[Files]
Source: "installer-build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Condition Criteria App"; Filename: "{app}\Condition Criteria App.exe"
Name: "{userdesktop}\Condition Criteria App"; Filename: "{app}\Condition Criteria App.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\Condition Criteria App.exe"; Description: "Launch Condition Criteria App"; Flags: nowait postinstall skipifsilent