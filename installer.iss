[Setup]
AppName=Bulk DDS Converter
AppVersion=1.0.0
DefaultDirName={pf}\BulkDDSConverter
DefaultGroupName=Bulk DDS Converter
OutputDir=dist
OutputBaseFilename=BulkDDSConverter_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Bulk DDS Converter"; Filename: "{app}\BulkDDSConverter.exe"
Name: "{commondesktop}\Bulk DDS Converter"; Filename: "{app}\BulkDDSConverter.exe"

[Run]
Filename: "{app}\BulkDDSConverter.exe"; Description: "Launch Bulk DDS Converter"; Flags: nowait postinstall skipifsilent