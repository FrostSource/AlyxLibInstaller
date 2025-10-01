@echo off
setlocal

dotnet clean

echo Publishing x64 Single EXE...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -o bin\Release\Publish\x64SingleExe

echo Publishing x86 Single EXE...
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -o bin\Release\Publish\x86SingleExe

echo Publishing x64 Loose Files...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true -p:PublishReadyToRun=true -o bin\Release\Publish\x64LooseFiles

echo Creating output folder...
mkdir bin\Release\Ready >nul 2>&1

echo Copying single EXE builds to Ready folder...
copy /Y bin\Release\Publish\x64SingleExe\AlyxLibInstaller.exe bin\Release\Ready\AlyxLibInstaller-x64.exe
copy /Y bin\Release\Publish\x86SingleExe\AlyxLibInstaller.exe bin\Release\Ready\AlyxLibInstaller-x86.exe

echo Zipping x64 Loose Files...

set APPNAME=AlyxLibInstaller

REM powershell -nologo -noprofile -command ^
REM   "Compress-Archive -Path (Get-ChildItem -File -Recurse -Path 'bin\Release\Publish\x64LooseFiles\' | Where-Object { $_.Extension -notin '.pdb', '.xml', '.json' -or $_.Name -eq 'AlyxLibInstaller.runtimeconfig.json' -or $_.Name -eq 'AlyxLibInstaller.dll' -or $_.Name -eq 'AlyxLibInstaller.exe' } | Select-Object -ExpandProperty FullName) -DestinationPath 'bin\Release\Ready\x64LooseFiles.zip' -Force"


powershell -nologo -noprofile -command ^
  "Compress-Archive -Path (Get-ChildItem -Path 'bin\\Release\\Publish\\x64LooseFiles' -File | Where-Object { ($_.Name -eq '%APPNAME%.exe') -or ($_.Name -eq '%APPNAME%.dll') -or ($_.Name -like '*.dll') -or ($_.Name -eq '%APPNAME%.runtimeconfig.json') } | ForEach-Object { $_.FullName }) -DestinationPath 'bin\\Release\\Ready\\x64LooseFiles.zip' -Force"

echo Done.
REM pause
