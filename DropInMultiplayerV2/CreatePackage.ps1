$compress = @{
	LiteralPath= ".\DropInMultiplayer.dll", ".\manifest.json", ".\icon.png", ".\README.md"
	DestinationPath = "DropInMultiplayer.zip"
}
Compress-Archive -Force @compress
