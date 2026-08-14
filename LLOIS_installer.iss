[Setup]
AppName=LLOIS
AppVersion=1.0
DefaultDirName={autopf}\LLOIS
DefaultGroupName=LLOIS
OutputBaseFilename=LLOIS_Setup
Compression=lzma
SolidCompression=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\LLOIS"; Filename: "{app}\LLOIS.exe"
Name: "{commondesktop}\LLOIS"; Filename: "{app}\LLOIS.exe"

[Run]
Filename: "{app}\LLOIS.exe"; Description: "Launch LLOIS"; Flags: nowait postinstall skipifsilent