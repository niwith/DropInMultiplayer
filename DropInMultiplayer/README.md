# DropinMultiplayer
The drop in multiplayer mod for Risk of Rain 2!
This mod allows the host to have players join mid-game, and automatically gives them items to help them catch up!

If you have any bug reports, ping me on the modding discord (https://discord.gg/5MbXZvd), you can make an issue on the github if you want, but I'll level with you, I'll probably forget to check it.

Credit to https://thunderstore.io/package/SushiDev/DropinMultiplayer/ for originally creating this mod.


### How to Join Existing Game
  1. Go to steam friends list
  2. Click the little arrow next to your friend's name
  3. Click the "Join Friend" button
  4. You will now be loaded into spectator mode in Risk of Rain 2
  5. Press enter to open chat and type your join as command, e.g.
	* join_as Commando
	* join_as Huntress
	* join_as Captain

### Commands Examples
These commands should be sent into the game chat (not the console)
  1. join_as Commando = Spawns you in as commando.
  2. join_as Huntress niwith = Spawns niwith in as Huntress, replace niwith with whoever you'd like to spawn as huntress/whatever in the names list. This seems to be bugged at the moment, hopefully I can find a fix in next release for it

# Changelog


### 1.0.19
* Fixed discord invite link in readme
*

### 1.0.18
* Added option to spawn as random character - join_as Random

### 1.0.17
* Fixed bug which sometimes occured when installing DropInMultiplayer with other mods, notably Starstorm2
* Removed requriement for developers to manually pass invalid items along to DropInMultiplayer, items are now taken directly from the availible drop items in a run
* Added config option to enable join as heretic (off by default)

### 1.0.16
* Added dependancy back for R2API
* Only host should need the mod now, and this should work with clients playing vanilla or modded. If it doesn't work, shoot @niwith a ping on the modding discord and I will cry and then fix it

### 1.0.15
* Removed dependancy on R2API
* Removed support for FallenFriends (since the mod has been depricated)

### 1.0.14
* Fixed players being able to join on final stage and soft locking the game

### 1.0.13
* Fixed networking bug (don't use 1.0.12 its very broken)

### 1.0.12
* Hotfix for Risk of Rain 2 - Anniversary Update - March 25, 2021 

### 1.0.10
* Made the code a little more friendly for other mods to interface with
* Devs looking to block certain items from dropping should check out the DropInMultiplayer.AddInvalidItems(...) method (and/or send me a message) 
* Devs looking to temporily block the join_as command should check out the DropInMultiplayer.BlockJoinAs(...) method (and/or send me a message)

### 1.0.9
* Filtered out illegal items from possible drops on joining game, big thanks to paddywaan (https://github.com/paddywaan) for submitting the pull request

### 1.0.8
* Added support for custom characters which have spaces in their names (just type the name without a space, e.g. join_as ClayTemplar)

### 1.0.7
* Fixed bug which broke Captain's R ability and PlayerBots mod

### 1.0.6
* Added ability for other mods to temporarily block join_as if needed, shoot me a message if you need help setting that up
* Added temporary block for join_as on the final stage to prevent soft lock

### 1.0.5
* Fixed issue forcing all players to have the mod installed, you should now be able to have players without the mod use the join_as command in chat
* Removed a debug log I left in, whoops

### 1.0.4
* Fixed issue preventing some modded characters from being selected (specifically BanditClassic)

### 1.0.3
* Fix for join_as from captain to any other class keeping his unique item
* Fix for boss items preventing joining
* Fix (hopefully) for join_as working while dead if you are controlling a drone (FallenFriends)

### 1.0.2
* Fix for interaction with FallenFriends, not longer breaks join_as if you have FallenFriends installed

### 1.0.1
* Added option for join_as after character select (letting players change characters). Defaults to false

### 1.0.0
* Release a probably broken build
