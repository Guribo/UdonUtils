# Udon Utils

[![Total downloads](https://img.shields.io/github/downloads/Guribo/UdonUtils/total?style=flat-square&logo=appveyor)](https://github.com/Guribo/UdonUtils/releases)

Contains the base scripts/tools for TLP packages as well as prefabs and potentially helpful scripts for VRChat worlds.

Please note that there is no explicit documentation available. The code is changing less frequently now and relatively stableand where it made sense there is some documentation in the code.

# Dependencies

 - VRChat Creator Companion
	- VRC World SDK
 - [CyanPlayerObjectPool](https://cyanlaser.github.io/CyanPlayerObjectPool/)
 
 
## Versioning

This package is versioned using [Semantic Version](https://semver.org/).

The used pattern MAJOR.MINOR.PATCH indicates: 

1. MAJOR version: incompatible API changes occurred
   - Implication: after updating backup, check and update your scenes/scripts as needed
2. MINOR version: new functionality has been added in a backward compatible manner
   - Implication: after updating check and update your usages if needed
3. PATCH version: backward compatible bug fixes were implemented
   - Implication: after updating remove potential workarounds you added
 
## 2.0.0 Change Notes

### Additions
* Add `ImageDownloader`
* Add `GetMaster` from list of players helper function to `VRCPlayerUtils`
* Add optional position smoothing to `PlayerFollower`
* Add timestamp backlog of received Deserializations and `PredictionReduction` parameter to `TlpAccurateSyncBehaviour`
* Add UI prefabs for runtime test system
* Add demo scene for new runtime test system
### Fixes
- Improve error handling on `PackageExporter`
- Fix `TrackingDataFollower` crashing if local player is not yet valid
### Deletions
- Remove `FormerlySerializedAs` entries from `TlpLogger`
### Other
- Rename `useLocalPlayerByDefault` to  `UseLocalPlayerByDefault` in `ObjectSpawner`
- Complete overhaul of runtime test system
