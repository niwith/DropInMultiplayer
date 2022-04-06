IF EXIST %~dp0DropInMultiplayer (rmdir /S /Y %~dp0DebugOutput)
IF EXIST %~dp0DropInMultiplayer.zip (del /F %~dp0DropInMultiplayer.zip)
mkdir %~dp0DebugOutput
xcopy /Y %~dp0bin\Debug\DropinMultiplayer.dll %~dp0DebugOutput
xcopy /Y %~dp0bin\Debug\manifestv2.json %~dp0DebugOutput\manifest.json
xcopy /Y %~dp0bin\Debug\icon.png %~dp0DebugOutput
xcopy /Y %~dp0bin\Debug\README.md %~dp0DebugOutput
powershell "Get-ChildItem .\DebugOutput\ | Compress-Archive -DestinationPath DebugOutput\DropInMultiplayer.zip -Update"
pause