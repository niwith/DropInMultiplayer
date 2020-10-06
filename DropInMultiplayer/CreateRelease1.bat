IF EXIST %~dp0DropInMultiplayer (rmdir /S /Y %~dp0DropInMultiplayer)
IF EXIST %~dp0DropInMultiplayer.zip (del /F %~dp0DropInMultiplayer.zip)
mkdir %~dp0DropInMultiplayer
xcopy /Y %~dp0bin\Debug\DropinMultiplayer.dll %~dp0DropInMultiplayer
xcopy /Y %~dp0bin\Debug\manifest.json %~dp0DropInMultiplayer
xcopy /Y %~dp0bin\Debug\icon.png %~dp0DropInMultiplayer
xcopy /Y %~dp0bin\Debug\README.md %~dp0DropInMultiplayer
set src=%~dp0DropInMultiplayer
powershell "Compress-Archive -Force %src% DropInMultiplayer.zip"
exit 0