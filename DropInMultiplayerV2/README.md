# DropInMultiplayer
The drop in multiplayer mod for Risk of Rain 2!
This mod allows the host to have players join mid-game, and automatically gives them items to help them catch up!

If you have any bug reports, ping me on the modding discord (@niwith on https://discord.gg/5MbXZvd), or feel free to private message me.
Things I am working on for the mod may be listed on the project board: https://github.com/niwith/DropInMultiplayer/projects/1

Credit to https://thunderstore.io/package/SushiDev/DropinMultiplayer/ for originally creating this mod.

## Instructions
### How to Join Existing Game
1. Go to steam friends list
2. Click the little arrow next to your friend's name
3. Click the "Join Friend" button
4. You will now be loaded into spectator mode in Risk of Rain 2
5. Press enter to open chat and type your join_as command (with the '/' at the start), e.g.
    * /join_as Commando
    * /join_as Huntress
    * /join_as Captain

### More Chat Commands Examples
These commands should be sent into the game chat, can also be used in console with dim_ added to the start of the command name, e.g. dim_join_as
* **/join_as Commando** - Spawns you in as commando.
* **/join_as Huntress niwith** - Spawns niwith in as Huntress, replace niwith with whoever you'd like to spawn as huntress/whatever in the names list. 
* **/list_survivors** - Lists availible survivors for the join_as command
* **/list_bodies** - Lists availible bodies for join_as command (bodies will only work for join_as if "AllowJoinAsAnyBody" is enabled in config settings)

If you get stuck, try the help command: 
* **/help** - Lists availible commands
* **/help join_as** - Prints the help text for join_as command

### Important Configs to Check
* AllowRespawn (default = enabled): When enabled, dead players will be able to use the join_as command and it will cause them to respawn. I have enabled by default, because enabling this means that if an error does occur with rejoining existing players they can still use the join_as command to spawn in.
* GiveRejoiningPlayersCatchUpItems (default = enabled): When enabled, if an existing player leaves and rejoins the game they will be given items to catch up as well, however this does introduce an exploit if one player runs around the map collecting all items, then the other players all leave and rejoin to get the catchup items. 
* AllowJoinAsHiddenSurvivors (default = enabled): When enabled, allows players to join as characters marked by developers as "hidden", e.g. , heretic
* AllowJoinAsAllBodies (default = disabled): When enabled, the join_as command will check the BodyCatelog in addition to survivor list, which lets players join as basically anything (e.g. a beetle). 
* JoinAsRandomByDefault (default = disabled): When enabled, players will spawn as a random character as they spawn in. After this they will still be able to switch character with join_as.

Note that in config options "enabled" is true and "disabled" is false.
Also if updating from an old version of the mod, your old config may hang around, which may have different configraution, and some old no longer used config options. My suggestion is delete your old config file to avoid confusion.

# Changelog
### 2.0.1
* Fixed debug mode enabled, which interfered with some networking and could have been causing reported issues

### 2.0.0
* Rewrote the mod almost from scratch
* Fixed bug preventing rejoining player (who had not died in the current stage) from automatically spawning in
* Fixed bug preventing non-english languages from selecting characters, checkout the list_survivors command, either the internal name on the left, e.g. VoidSurvivor or the name in brackets, e.g. (MUL-T) should now work
* Change a lot of the config options, you may want to regenerate your file to avoid issues
* Changed the way catch up items work, now it the gives you random items across all tiers, with lower change at higher tier items, rather than the exact average that other players have for each tier
    * This now aligns with code the RoR2 devs have for giving a player random items and is based on the drop chance they had in configured in their code
* Added ability to join as any registered body prefab, gated behind the "AllowJoinAsAnyBody" config option
* Added chat commands for listing survivors and listing bodies
* Added better descriptions for config options
* Added a lot more logging, which should help track down bugs faster in future
* Removed drop item blacklist, the way this system was set up was causing some issues, all items for drops are now pulled directly from the run's drop table, e.g. Run.instance.availableTier1DropList, so there shouldn't need to be any item blacklisting
    * If for some reason I am incorrect and the black list was necessary somewhere let me know, it won't be a high priority to add back 
* Hopefully fixed compatiblity with other mods that muck around with the players bodyPrefab, since I now call to set the players body preference so when another mod switches the player back to their selected body preference it won't be wierd (hopefully)
* I believe fixed issue with joining as modded characters, tested with Enforcer (which was reported as broken) and didn't have an issue, although I''m not sure what I did which fixed this
* If you made a feature request, and it is not listed on this board (https://github.com/niwith/DropInMultiplayer/projects/1) in todo already, it is possible I forgot about your thing so feel free to message me and I'll add to the todo
* If there is a bug you reported, and it is not fixed in this patch, then I probably forgot about it so please let me know 
* Might be some other stuff I forgot

### 1.0.22
* Fixed a bug preventing players from joining

### 1.0.21
* Updated to point to the correct DLLs, hopefully this should fix some of the issues people were having
* Added code to ignore void items for the moment, I'll come back and fix properly at some point but at the moment it would have been causing exceptions
* There are likely still lots of issues in this patch as well, hopefully less issues than the previous

### 1.0.20
* Updated to work with Survivors of the Void DLC
* This is a quick patch to update to latest, it may have unkown issues I haven't done a huge amount of testing yet

### 1.0.19
* Fixed bug preventing setting another player's character when they first joined
* Fixed bug preventing changing another player's character when they had spaces in their username
* Added blacklist for drop items and count items in config file
* Fixed discord invite link in readme

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
