; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

; EDIT THESE LINES
#define RRExtPluginName "RRPoi"
#define RRExtPluginFriendlyName "RRPoi"
#define RRExtPluginVersion "V1.0.0"
#define RRExtPluginPublisher "pierrotm777"
#define RRExtURL "pierrotm777@gmail.com"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{7C95C495-DD05-4789-B923-5813C1EB66F3}
AppName=RR Plugin for {#RRExtPluginFriendlyName}
AppVerName=RR Plugin for {#RRExtPluginFriendlyName} {#RRExtPluginVersion}
AppPublisher={#RRExtPluginPublisher}
AppPublisherURL={#RRExtURL}
AppSupportURL={#RRExtURL}
AppUpdatesURL={#RRExtURL}
DefaultDirName={reg:HKLM\Software\RideRunner,Path|{pf}\RideRunner}\Plugins\{#RRExtPluginName}
OutputDir=..\Installer Output
OutputBaseFilename=RR Plugin {#RRExtPluginFriendlyName} {#RRExtPluginVersion} Setup
SourceDir=Source
Compression=lzma
SolidCompression=yes
Uninstallable=yes
;LicenseFile=License.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: SSCDL; Description: Install the Sample Skin into the Carwings_Dynamic_Lite Skin folder

[Files]
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "RRPoi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "RRPoi Skin Commands.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "Readme.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "add to ExecTBL.ini"; DestDir: "{app}"; Flags: ignoreversion
Source: "add to skin.ini"; DestDir: "{app}"; Flags: ignoreversion


; Folders
Source: "Languages\*"; DestDir: "{code:DataPath}\Plugins\{#RRExtPluginName}\Languages"; Flags: recursesubdirs onlyifdoesntexist confirmoverwrite
Source: "GPSExec\*"; DestDir: "{code:DataPath}\Plugins\{#RRExtPluginName}\GPSExec"; Flags: recursesubdirs onlyifdoesntexist confirmoverwrite

; skin
Source: "Sample Skin\*"; DestDir: "{app}\Sample Skin"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Sample Skin\Carwings_Dynamic_Lite\*"; DestDir: "{code:DataPath}\Skins\Carwings_Dynamic_Lite"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: SSCDL

[Run]
; Regasm
Filename: {dotnet20}\Regasm.exe; Parameters: /tlb /codebase RRPoi.dll; WorkingDir: {app}; StatusMsg: Registering Plugin...; Flags: runhidden


[UninstallRun]
; Regasm
Filename: {dotnet20}\Regasm.exe; Parameters: /u /tlb RRPoi.dll; WorkingDir: {app}; StatusMsg: Un-Registering Plugin...; Flags: runhidden


[UninstallDelete]
Name: {app}\RRPoi.tlb; Type: files

[Code]
//////////////////////////////////////////////////////////////
// Get PLUG-IN Installation Path, based on Profile Mode
//////////////////////////////////////////////////////////////
function InstallPath(p: String): String;
begin
	// look for rr.ini?
	if FileExists(ExpandConstant('{userdocs}\RideRunner\Config\rr.ini')) then
	begin
		if DirExists(ExpandConstant('{userdocs}\RideRunner\Plugins')) then
			Result := ExpandConstant('{userdocs}\RideRunner\Plugins\{#RRExtPluginName}')
		else
			Result := ExpandConstant('{app}')
	end
	else
		Result := ExpandConstant('{app}')
end;


//////////////////////////////////////////////////////////////
// Get DATA Path, based on Profile Mode
//////////////////////////////////////////////////////////////
function DataPath(p: String): String;
begin
	// look for rr.ini?
	if FileExists(ExpandConstant('{userdocs}\RideRunner\Config\rr.ini')) then
	begin
		if DirExists(ExpandConstant('{userdocs}\RideRunner')) then
			Result := ExpandConstant('{userdocs}\RideRunner')
		else
			Result := ExpandConstant('{reg:HKLM\Software\RideRunner,Path|{pf}\RideRunner}')
	end
	else
		Result := ExpandConstant('{reg:HKLM\Software\RideRunner,Path|{pf}\RideRunner}')
end;