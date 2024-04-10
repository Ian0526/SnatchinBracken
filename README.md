# SnatchinBracken

SnatchinBracken modifies the behavior of Bracken enemies. Instead of instantly killing players on contact, Brackens now capture and drag players to a designated "favorite spot." This change allows teammates a chance to intervene and rescue the captured player.

## Bug Reports/Suggestions
Add me on Discord @readthisifbad

## Changelog

### 1.4.8
- [Bug Fix] Fix loading without LethalConfig

### 1.4.7
- [Dependency Adjustment] Remove dependency for Ozone Netcode patcher

### 1.4.6
- [Bug Fix] v50 & 49 compatibility
- [Regression] Removed power level since this field has been moved in v50, will add back when v50 is in release.

### 1.4.5
- [Bug Fix] Toggle Auto Kill Stuck option with config, this already existed but wasn't implemented properly.
- [Feature] Toggle Auto Kill on time based.
- [Feature] Toggle Auto Kill on distance from favorite location.
- [Note] I wouldn't leave all of three toggled off, players can potentially get stuck if they're the last player left. If you're disabling all three of these, enable the insta kill option for the last player.
- [Known Issue] Occasionally the Bracken moves incredibly slow, it seems to be about a 10% chance of this happening.

### 1.4.4
- [Bug Fix] No Lethal Config reference in main class.

### 1.4.3
- [Feature] Bracken Power Level config option.
- [Bug Fix] Gradual damage coroutine end after player is killed by other means outside of the Bracken.

### 1.4.2
- [Bug Fix] Log messages removed causing exception.
- [Bug Fix] Desync caused after melee & stun attacks.
- [Bug Fix] Insanity persisting due to extra death causes (E.g. Nutcracker shots)
- [Bug Fix] Exception caused by dropping body on clients.
- [QOL] Removed "Chaotic Tendencies" config option.
- [QOL] More cleanup for my sanity.

### 1.4.1
- [Bug Fix] Players killed prematurely while binded will properly clear their bound state.
- [QOL] Refactor reused lines into a Util.
- [QOL] Take majority of sections out of Patches, dedicate own directory for specific class types.

### 1.4.0
- [Feature] Insanity levels are now set to max as soon as player is grabbed & maintained.
- [Bug Fix] Gradual damage now correctly resets muffled voice state and properly clears entity states.

### 1.3.9
- [Bug Fix] Teleporters now work properly again. Apparent redundant line wasn't actually redundant.
- [Bug Fix] Removed config option to fight back. This needs more work.

### 1.3.8
- [QOL] Make insta kill when alone false.
- [Feature] Option for last person alive to keep their items & fight back.

### 1.3.7
- [Feature] Option to disallow TPs while being dragged.

### 1.3.6
- [Feature] Option to make players targetable by other enemies while being dragged.

### 1.3.5
- [Feature] Move player 0.8m away from Bracken so it appears as if they're being held.

### 1.3.4
- [Feature] Muffle voice like snare flea.

### 1.3.3
- [Bug Fix] Forgot to update the manifest.

### 1.3.2
- [QOL] Make LethalConfigAPI an optional dependency. Use BepInEx's config system by default.

### 1.3.1
- [Feature] Percent chance for insta kill.

### 1.3.0
- [Bug Fix] Added stuns support.

### 1.2.9
- [Bug Fix] Flushing maps at dropship leave, not actual ship leave.

### 1.2.8
- [Bug Fix] Forgotten boolean for Bracken Room check.

### 1.2.7
- [Feature] Config option to force the Bracken to make the famous "Bracken Room" its permanent favorite spot.

### 1.2.6
- [Feature] Gradual damage, similar to the snare flea (leech) & config options.
- [Feature] Kill the Bracken if it gets stuck somewhere & config options (disabled by default with gradual damage).
- [Bug Fix] Hitting players no longer does damage if they are being dragged.
- [Known Issue] Players still experience brief desync after hitting the Bracken. It fixes itself a few seconds later.
- [Known Issue] Brackens may choose a location that's behind a locked door. I've included a config option as stated above to automatically kill the player if the Bracken is stuck for more than 5 seconds.

### 1.2.5
- [QOL] Removed Warn log

### 1.2.4
- [Bug Fix] Teleporters now work perfectly fine.
- [Bug Fix] Fixed strange desync behavior after attacks.
- [Known Issue] After being striked, clients will see the Bracken desync for a moment, and vanish after a few moments. (I should have a fix out for this sometime in the next few days)

### 1.2.1 - 1.2.3
- [Bug Fix] Get furthest tile from where the player is grabbed at.
- [Feature] Added config option to adjust the kill distance from the favorite spot.
- [Feature] Added config option to immediately kill the final player.

### 1.2.0
- [Overhaul] Implemented proper networking, allow host to dictate procedures instead of duplicates with clients.
- [Bug Fix] Properly allow for re-attacks
- [Bug Fix] Dropped items are now sync'd
- [Feature] Tons of logic adjustment to make transitions more seamless, no animation in place yet though.
- [Known Issue] Teleporting will prevent the Bracken from attacking the same player again. Working on a fix.

### 1.1.3
- [QOL] Remove log.

### 1.1.2
- [Bug Fix] Remove isOwner check from testing.

### 1.1.1
- [Bug Fix] Minor adjustments from 1.1.0.

### 1.1.0
- [Feature] Added turret ignoring as a config option.
- [Feature] Added landmine ignoring as a config option.
- [Feature] Added time before next attack after interrupted as a config option.
- [(Potentially Done? Needs testing) Feature] Added teleport support to properly dismount player.
- [Bug Fix] Fixed Bracken becoming unable to attack after grabbing someone.

### 1.0.1
- [Feature] LethalConfig dependency added.
- [Feature] Can now change config options to drop items on grab & time until kill.
- [Bug Fix] Double handed objects will always drop so players can actually be killed.

### 1.0.0
- Initial release of SnatchinBracken.
- Basic functionality of altering Bracken behavior implemented.

## Dependencies
- LethalConfig

## Usage
Open the settings menu with ESC and edit it through the Mod Config button.

## Contributing
Feel free to contribute. If it improves the system, I'm more than happy to implement your changes.