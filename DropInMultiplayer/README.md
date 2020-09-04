# DropinMultiplayer
The drop in multiplayer mod for Risk of Rain 2, now updated for the 1.0 update!
This mod allows the host to have players join mid-game, and automatically gives them items to help them catch up!

If you have any bug reports, ping me on the modding discord (https://discord.gg/MfQtGYj), you can make an issue on the github if you want, but I'll level with you, I'll probably forget to check it.

I updated this mod from the original https://thunderstore.io/package/SushiDev/DropinMultiplayer/ mod as it has not been updated for 1.0. If the original mod creator, or anyone else who is better at modding than I am, is putting up a better version of this mod let me know and I'll deprecate this version. 


### Commands Examples
These commands should be sent into the game chat (not the console)
  1. join_as Commando = Spawns you in as commando.
  2. join_as Huntress niwith = Spawns niwith in as Huntress, replace niwith with whoever you'd like to spawn as huntress/whatever in the names list
  
# Installation (Mod Manager)
 1. Click the install with Mod Manager button
 2. Done
 
  
# Installation (Manual)
 1. Extract "DropInMultiplayer.dll" from the zip file and place it into  "/Risk of Rain 2/BepInEx/plugins/" folder.
 2. Done

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
