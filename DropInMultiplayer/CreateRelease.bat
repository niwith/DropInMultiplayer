IF EXIST %~dp0DropInMultiplayer (rmdir /S /Y %~dp0ReleaseOutput)
IF EXIST %~dp0DropInMultiplayer.zip (del /F %~dp0DropInMultiplayer.zip)
mkdir %~dp0ReleaseOutput
xcopy /Y %~dp0bin\Release\DropinMultiplayer.dll %~dp0ReleaseOutput
xcopy /Y %~dp0bin\Release\manifest.json %~dp0ReleaseOutput
xcopy /Y %~dp0bin\Release\icon.png %~dp0ReleaseOutput
xcopy /Y %~dp0bin\Release\README.md %~dp0ReleaseOutput
powershell "Get-ChildItem .\ReleaseOutput\ | Compress-Archive -DestinationPath ReleaseOutput\DropInMultiplayer.zip -Update"
exit 0 