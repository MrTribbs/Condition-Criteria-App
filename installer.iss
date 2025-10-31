[Setup]
AppName=Condition Criteria App
AppVersion=1.0.0
DefaultDirName={pf}\Condition Criteria App
OutputBaseFilename=ConditionCriteriaInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "*"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Condition Criteria App"; Filename: "{app}\Condition Criteria App.exe"

[Run]
Filename: "{app}\Condition Criteria App.exe"; Description: "Launch Condition Criteria App"; Flags: nowait postinstall skipifsilent