# SnatchinBracken

SnatchinBracken modifies the behavior of Bracken enemies. Instead of instantly killing players on contact, Brackens now capture and drag players to a designated "favorite spot." This change allows teammates a chance to intervene and rescue the captured player.

## Changelog

### 1.2.0
- [Overhaul] Implement proper networking, allow host to dictate procedures instead of duplicates with clients.
- [Bug Fix] Properly allow for re-attacks
- [Bug Fix] Drop items are now sync'd

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