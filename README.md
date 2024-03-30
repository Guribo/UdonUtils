# Udon Utils

[![Total downloads](https://img.shields.io/github/downloads/Guribo/UdonUtils/total?style=flat-square&logo=appveyor)](https://github.com/Guribo/UdonUtils/releases)

Contains the base scripts/tools for TLP packages as well as prefabs and potentially helpful scripts for VRChat worlds.

Please note that there is no explicit documentation available. The code is changing less frequently now and relatively stableand where it made sense there is some documentation in the code.

# Dependencies

 - VRChat Creator Companion
	- VRC World SDK
 - [Cyan.PlayerObjectPool](https://cyanlaser.github.io/CyanPlayerObjectPool/) - repository needs to be added to VCC manually first via `Add to VCC` Button!
 
 
## Versioning

This package is versioned using [Semantic Version](https://semver.org/).

The used pattern MAJOR.MINOR.PATCH indicates: 

1. MAJOR version: incompatible API changes occurred
   - Implication: after updating backup, check and update your scenes/scripts as needed
2. MINOR version: new functionality has been added in a backward compatible manner
   - Implication: after updating check and update your usages if needed
3. PATCH version: backward compatible bug fixes were implemented
   - Implication: after updating remove potential workarounds you added

## Changelog

All notable changes to this project will be documented in this file.

### [3.0.0] - 2024-03-30

#### 🚀 Features

- [**breaking**] Add time sources
- *(TlpAccurateSyncBehaviour)* Add setup checks to start
- *(TimeSource)* Add DeltaTime getter, reformat codebase

#### ⚙️ Miscellaneous Tasks

- Append changelog to readme
- Prepare release 3.0.0
- Revert deletion of Testing folder
- Bump version

### [2.0.0] - 2023-11-16

#### 🚀 Features

- Add fallback position handling to tracking data tracker
- Create Test Result UI
- Create UI with overview
- Update status visibility
- Add test for eye height updates coming from master
- Update
- Improve error message by adding hint
- Add prediction reduction
- Create basic image downloader with automatic aspect ratio adjustment
- Add new demo world for uvu (wip)
- Prevent datalist access to invalid indices
- Add lights and sfx to light, add more visuals, add toggle
- Create slideshow with buttons
- Update version

#### 🚜 Refactor

- Reorder, rename

#### 📚 Documentation

- Add change notes to readme
- Add versioning info"

#### 🧪 Testing

- Add some missing method implementations

### [1.1.1] - 2023-10-05

#### 🚀 Features

- Update version

#### 🐛 Bug Fixes

- Namespace

### [1.1.0] - 2023-10-05

#### 🚀 Features

- Update to represent fix update that removed EditorOnly from release on github
- Move udon pool into utils
- Remove no longer needed asmdef
- Update version

### [1.0.1] - 2023-10-03

#### 🚀 Features

- Use any tag
- Remove old readme content, update exporter
- Add legacy folder
- Update asmdef after update to latest udonsharp version

### [1.0.0] - 2023-09-13

#### 🚀 Features

- Update assets to lfs
- Update serialization
- Add SetOwner functionality
- Fix stuttering at high speed
- Change layers and make objects static that can be
- Fix finding closest player
- Damage indicator UI
- Use distance check for player detection
- Add kill confirmation
- Start implementing leader boards
- Start AVL tree
- Continue implementing AVL tree
- Fix most AVL issues
- Improve comparer
- Add UdonPool and usage
- Fix build error, add executionorder to logger
- Create basic leader board entry
- Improve menu, add automatic scaling for content
- Add UdonEvent
- Add remove all listeners
- Add basic audio to gun
- Update logging
- Update to U## 1.0 and client sim
- Move logging to base class
- Refactor player state to have base class PlayerStats
- Moved
- Fix loglevels, assert and perf limit warning
- Fix entries with invalid names being added to leaderboard
- Add vehicle sync, update leader board (break it too)
- Initial (failed) version of better-tracking pickups
- Jitterfree pickups
- Make few functions static
- Start creating composite weapons
- Disallow multiple instances of same override in lists
- Add gamemode, update vr components, test improvements, add serialization retry to base behaviour
- Continue ai state machine
- Give each damage target a unique id
- Cleanup and add getTarget
- Start implementing damage to targets
- Update imported projects
- Add loose files
- Fix damage targets not receiving damage locally
- Add logging of all logs in frame to profiler
- Fix null error on player death
- Create chair scripts
- Add basic startup torque
- Add execution order, fix runtime tests
- Simplify comparison of behaviours
- Fix scene setups
- Update board (failing)
- Create player entry from controller
- Add indefinite patch
- Fix leaving player not updating entry
- Remove guribo references
- Add start event to deserialization
- Create events for ui
- Reserialize
- Convert recycling scroll view to udon
- Add TLP_UNIT_TESTING define, add companion version of VRWorldToolkit
- Change event handling and make it functionality of base class
- Fix up scenes and broken event callbacks
- Fix updates not being displayed
- Display data in leaderboard entry
- Use textmesh pro, add animation to leaderboard
- Start adding feature for delayed event invokation
- Implement delayed execution
- Extract custom player stats into own class
- Add integer utils
- Add variations
- Add mvc, test controller init/deinit
- Create init for model
- Test init of model and view
- Enforce init order
- Deinit controller
- Test deinit of view
- Add listening to model changes from view
- Change editor, expose variables
- Create vrcplayerapi extensions
- Add new util
- Start simplifying executionorder dependencies
- Add more events for different executionorder sections, refactor executionorder on most scripts
- Update qvpen to v3.2.5.1
- Update tribes scene, create leaderboard prefab
- Convert basic performance stats to model view controller
- Update version of world on save now, requires 2 saves to go through though for now...
- Add multiple sorting algorithms, update privacy zones example scene
- Add dirty property to event
- Create a factory for gameobjects
- Create instantiating factory
- Create factories for avl tree, factory with pool
- Add leaderboard model
- Add default categories
- Reduce type spam in logs, add execution order to logs
- Extend model to create avl trees for each category
- Create default factories for scenes, update logging
- Add data storing capability to LeaderModelEntryModel
- Add comparer creation, update exectionorders, move pooleable code to base behaviour
- Support adding players to model
- Fix finding of inactive gameobjects
- Add new data source using leaderboard model
- Deinit on destroy, selectable categories with view
- Add entries again, support ascending/descending sort direction (wip)
- Start with sync of entries with new model
- Fix category adding without custom categories
- Raise change event on dirty in entry
- Have entry synchronizer get notified when an entry changes
- Have synchronizer attach entry to dirty root
- Continue implementing new sync
- Create statemachine for synchronizer
- Continue working on sync (wip)
- Support playmode test
- Patch more vrc components for easier unit testing
- Add missed vrc script to patcher, complete idle state testing
- Remove Leaderboard, continue updating entrysynchronizer
- Synchronization of single dirty entry
- Convert syncing to use string
- Update test controller and test template to test case with inheritance
- Add max exec order, ensure tests don't respond after completion
- Add tracking of jumps and movement distance
- Made more utils static only
- Removed old prefab
- Create round robin sync
- Update runtime folder structure
- Add usagenote
- Update UVU exporter and readme
- Move some generic audio scripts here, add dummyview
- Update
- Remove vrc patcher
- Change folder structure for vpm
- Update dependencies
- Rename and add udonutils dependency
- Warn instead of error
- Create synced event class
- Raise on deserialization
- Send the number of calls to the event since the last synchronization
- Replace synchronize playerlist with new version
- Fix missing asset
- Update
- Use inheritance, improve execution order
- Add improved tracking scripts and first draft of a velocity provider
- Add improved tracking scripts and first draft of a velocity provider
- Test and improve rigidbody velocity provider
- Create network time script that grabs a shared time for the current frame
- Important update for smooth vehicle sync
- Replace time.time
- Extract accurate timing code into a parent class, add smooth error correction, add teleportation
- Update assets
- Update namespaces, update velocity provider
- Add networktime assets
- Create exporter
- Add missing assets to exporter
- Add support for collision detection during prediction
- Create white/blacklist
- Add whitelist mode and restricted usage
- Add demo toggle script for adding local player to blacklist
- Separate cyan dependency, update assets
- Update assets
- Fix some edge cases using unit testing
- Add more tests, refactoring
- Update model, add new ownership transfer function, add logs to base behaviour
- Update chair, refactoring
- Add gitconfig
- Change tests to verify floating point issue
- Fix relative velocity recording in velocity provider
- Turn string loading error message into error
- Revert severity
- Improve compile symbols
- Move development tools, update exporter
- Fix/improve exporting and versioning

#### 🐛 Bug Fixes

- Debug symbol
- Lfs for assets
- Remove all files to fix lfs issues
- Add all files again to get rid of lfs issues
- Setting references from editor script works again

#### 🚜 Refactor

- Cleanup and more test coverage
- Remove unused code and cleanup
- Improve contains checking
- Make playerstats inherit from cyanpoolable
- Fix naming
- Use Tlpbasebehaviour
- Fix roslyn warnings
- Extract some duplicate code to base class
- Cleanup model
- Cleanup

#### 📚 Documentation

- Update Readme
- Fix file link

#### 🧪 Testing

- Benchmark performance (ca. 0.4ms insertion time @ 100k elements)
- Fix tests with debug enabled
- Fix errors
- Ensure logasserts are on by default
- Show gameobjects in playtest
- Fix failures caused by missing log expects
- Update tests to use TestWithLogger, reduce log spam
- Added more values and log
- Initial setup and test of PlayerBlackList.cs
- Continue testing for full coverage
- Fix missing logger and test additional check
- Fix player test

#### ⚙️ Miscellaneous Tasks

- Add release template
- Add serialized programs
- Remove programs
- Update serialization

### [0.0.3] - 2021-01-08

#### 🚀 Features

- Cleanup, refactoring
- Add optional event listeners
- Add master only activation support
- Debug symbol toggle, cleanup
- Add voice range far
- Add transform recorder/player, add synced bool script that uses properties
- Add lateBoneFollower, refactor: SyncedInteger uses Properties now
- Add ToggleObject behaviour
- Support disabling behaviours
- Add synced string
- World version check, various new scripts
- Add more extensive path checking, replace Guribo with TLP
- Improve error handling, add hashing of created package
- Fix explorer opening at wrong position, update package.json

#### 🐛 Bug Fixes

- Missing assembly reference

#### 🚜 Refactor

- Restructure content
- Cleanup
- Test and unity 2019

#### ⚙️ Miscellaneous Tasks

- Update
- Add udonsharp as dependency

### [0.0.4] - 2021-06-12

#### 🚀 Features

- Improved scene checking, added editor udonbehaviour extensions
- Add test contoller template and gizmos
- Prepare for UnU
- Add test aborting
- Add libraries, add networking behaviours

#### 🐛 Bug Fixes

- Prevent detecting default values as null, fix build error

#### 🚜 Refactor

- Cleanup and add meta files

#### 📚 Documentation

- Add documentation, refactoring

#### ⚙️ Miscellaneous Tasks

- Change file structure

<!-- generated by git-cliff -->
