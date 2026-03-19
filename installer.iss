[Setup]
AppName=Bulk DDS Converter
AppVersion=1.0.9
DefaultDirName={pf}\BulkDDSConverter
DefaultGroupName=Bulk DDS Converter
OutputDir=dist
OutputBaseFilename=BulkDDSConverter_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
AppPublisher=Lughworks
AppPublisherURL=https://github.com/Lughworks
AppSupportURL=https://github.com/Lughworks
AppUpdatesURL=https://github.com/Lughworks
SetupIconFile=assets\icon.ico
UninstallDisplayIcon={app}\BulkDDSConverter.exe

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Bulk DDS Converter"; Filename: "{app}\BulkDDSConverter.exe"
Name: "{commondesktop}\Bulk DDS Converter"; Filename: "{app}\BulkDDSConverter.exe"

[Run]
Filename: "{app}\BulkDDSConverter.exe"; Description: "Launch Bulk DDS Converter"; Flags: nowait postinstall skipifsilent