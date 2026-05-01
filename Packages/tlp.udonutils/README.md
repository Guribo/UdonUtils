# Udon Utils

[![Total downloads](https://img.shields.io/github/downloads/Guribo/UdonUtils/total?style=flat-square&logo=appveyor)](https://github.com/Guribo/UdonUtils/releases)

Contains the base scripts/tools for TLP packages as well as prefabs and potentially helpful scripts for VRChat worlds.

Please note that there is no explicit documentation available.
The code contains tooltips and descriptions of classes where it makes sense.
Theses are being added over time where it makes sense.

## Installation

1. Install/Add VRChat World SDK 3.10.3 or later to your project
2. Install/Add TLP UdonUtils to your project: https://guribo.github.io/TLP/

## Uninstall

### Via Tools (recommended)

1. Remove TLP UdonUtils from your project via VCC or [ALCOM](https://vrc-get.anatawa12.com/alcom/)
2. Go to **Edit > Project Settings > Player > Script Compilation** and remove `TLP_UDONUTILS` from the list
3. You may also remove anything else starting with `TLP_` 
   1. Only applies if you used other TLP packages or enabled debug/test mode

### Manually via Filesystem

1. Remove the `Packages/tlp.udonutils` folder from your project
2. Remove the following lines from `Packages/packages-lock.json`:
   ```json
    "tlp.udonutils": {
      "version": "file:tlp.udonutils",
      "depth": 0,
      "source": "embedded",
      "dependencies": {}
    },
   ```
3. Remove in a similar fassion from the `Packages/vpm-manifest.json` if present in there
4. In the Unity project go to **Edit > Project Settings > Player > Script Compilation** and remove `TLP_UDONUTILS` from the list
5. You may also remove anything else starting with `TLP_`
   1. Only applies if you used other TLP packages or enabled debug/test mode

## Setup

1. Add `TLP_Essentials` prefab to your scene to get the core components
   1. TLPLogger - *for logging anything TLP related (mandatory)*
   2. WorldVersionCheck - *Warns users if a player with a new world version joins*
   3. TLPNetworkTime - *Much more accurate VRC network time provider (sub-millisecond accuracy)*

## Enable Debug Mode

> Disclaimer: There is no negative performance impact when debug mode is disabled
> as the logging code is not compiled into builds.

Debug mode is disabled by default. When enabled it will log additional information to the console.
This logging is quite excessive and should only be enabled for debugging purposes.
If you see no debug logs in the console/log files, ensure that the 

1. Go to **Edit > Project Settings > Player > Script Compilation** and add `TLP_DEBUG` to the list
2. Find the `TLP_Logger` prefab in your scene 
3. Set to logging severity `Debug`, be default it is set to `Info`.

## Disable Debug Mode

1. Go to **Edit > Project Settings > Player > Script Compilation** and remove `TLP_DEBUG` from the list
2. Find the `TLP_Logger` prefab in your scene and set to logging severity `Info` or higher

## Test Mode

Similar to debug mode, test mode is disabled by default but can be enabled 
by adding `TLP_UNIT_TESTING` to the list of script compilation symbols.

Test mode is intended to be used for unit testing and should not be enabled in production builds.
It will enable certain workarounds for parts of the VRChat SDK that can not easily be mocked.

> Note: Releases of UdonUtils don't contain the unit tests used for development as they are not relevant for 99% of users.

## Versioning

This package is versioned using [Semantic Version](https://semver.org/).

The used pattern MAJOR.MINOR.PATCH indicates: 

1. MAJOR version: incompatible API changes occurred
   - Implication: after updating backup, check and update your scenes/scripts as needed
2. MINOR version: new functionality has been added in a backward compatible manner
   - Implication: after updating check and update your usages if needed
3. PATCH version: backward compatible bug fixes were implemented
   - Implication: after updating remove potential workarounds you added

### [14.0.0] - 2026-05-01

#### 🚀 Features

- *(UdonUtils)* Update TransformBacklog interpolation methods to use `double` for improved precision
- *(UdonUtils)* Expose `LastAdded` as public, add `DiscardAllButLatest` method to TimeBacklog and TransformBacklog
- *(UdonUtils)* Add `MaxBacklogAge` constant for configurable interpolation snapshot retention
- *(UdonUtils)* Add ECS Test materials and configuration assets
- *(UdonUtils)* Extend test framework with NSubstitute, Harmony patches, and additional DLL references
- *(UdonUtils)* Add `RigidbodyRecorder` and enhance `RigidbodyHistory` with new snapshot and acceleration handling logic
- *(UdonUtils)* Add `RigidbodyRecorder` asset for enhanced physics recording
- *(UdonUtils)* Add unit tests for `RigidBodyPhysicsState` and enhance with encoded/decoded flag handling logic
- *(UdonUtils)* Add `RigidbodyExtensions` with movement and rotation methods, integrate new tests, and improve serialization
- *(player-camera-tracker)* Add experimental support for player (handheld-)camera tracking

#### 🐛 Bug Fixes

- *(UdonUtils)* Simplify ownership transfer logic by reducing redundant parent component checks
- *(UdonUtils)* Refine execution order logic by excluding specific types and improving filtering of mock/test classes

#### 🚜 Refactor

- *(UdonUtils)* Change TimeSource field from internal to public in RigidbodyVelocityProvider
- *(UdonUtils)* [**breaking**] Remove `TimeSnapshot` and `TransformSnapshot` classes and assets, update tests to use substitutes for easier mocking
- *(UdonUtils)* Standardize method formatting, improve debug logging, and enhance snapshot prediction logic
- *(UdonUtils)* Rename and simplify deserialization method, adjust snapshot handling logic for consistency
- *(UdonUtils)* Remove `NetworkSnapshotBacklog` dependencies and update `RigidbodyHistory` with improved snapshot handling logic
- *(UdonUtils)* [**breaking**] Remove `SnapshotBacklog`, `TransformBacklog` tests, and related assets to streamline codebase and reduce redundancy
- *(UdonUtils)* Simplify prediction reduction logic and remove unused constants, usings, and code lines
- *(UdonUtils)* Update access modifiers in `RigidbodyRecorder`, adjust `RigidBodyPhysicsState` serialization

#### 🧪 Testing

- Different sphere collision scenarios
- *(RigidBodyCollision)* Add test for collision determinism with physics update offset
- *(UdonUtils)* Refine logger initialization and add hooks for time and frame count sources
- *(UdonUtils)* Refactor test setup and teardown for logger and environment initialization
- Add ownership transfer test and related assets

#### ⚙️ Miscellaneous Tasks

- Update assets
- *(UdonUtils)* Mark `PredictState` method as obsolete with guidance for merging
- Update PlayerBlackList and PlayerFollower assets with serialized data adjustments
- Update PlayerBlackList and PlayerFollower assets with serialized data adjustments
- *(UdonUtils)* Add RigidBodyPhysicsState asset and RigidbodyHistory script for physics state tracking and history
- *(UdonUtils)* Refactor and extend TimeBacklog and TransformBacklog with improved API regions and interpolation support
- *(UdonUtils)* Add debug logging for TransformBacklog interpolation method under TLP_DEBUG
- *(UdonUtils)* Add meta files for RigidBodyPhysicsState and RigidbodyHistory assets
- *(UdonUtils)* Update Chair and WorldVersionCheck assets with serialized data adjustments
- *(UdonUtils)* Update Chair and WorldVersionCheck assets with serialized field and data refinements
- *(UdonUtils)* Bump package version to 13.0.0, update VRChat Worlds SDK dependency to 3.10.0
- Bump version to 14.0.0
### [13.0.0] - 2026-04-12

#### 🚀 Features

- *(LatencyTester)* Use NetworkEvents by default, Manual Sync via settings still available
- *(Sync)* Add accesors for some backlog data
- *(TlpAccurateSyncBehaviour)* Refactor validation and expose new validation hook
- *(TransformBacklog)* [**breaking**] Add velocity
- *(Editor)* Add TLP_UDONUTILS define whenever UdonUtils is added to a project
- *(UdonUtils)* Add Harmony patch to disable UnityEventFilter during play mode testing when TLP_UNIT_TESTING is enabled
- *(Prefabs)* Add TLP_FixedUpdateRate and TLP_TaskScheduler prefabs
- *(UdonUtils)* Add Harmony patch to disable UdonSharpCompiler for unit testing
- *(Editor)* Add tool to fix serialized Udon program asset GUIDs in git repositories

#### 🐛 Bug Fixes

- *(TlpAccuracteSyncBehaviour)* Fix potential division by 0 when message with same server time arrives
- *(TlpNetworkTime)* Fix instant synchronization and ensure initialization/updates accross public accessors
- *(LatencyChecker)* Improve moving average calculation and add threshold handling

#### 📚 Documentation

- *(TlpFixedUpdateRate)* Enhance documentation and improve API clarity

#### ⚙️ Miscellaneous Tasks

- Add manual job to automate tag creation
- Ensure package version and git history match
- Improve tag existence checks and enhance logging
### [12.0.0] - 2025-11-09

#### 🚀 Features

- *(UdonTestUtils)* Implement some missing methods
- *(Adapters)* [**breaking**] Remove Cyan Object Pool adapter and dependencies (use VRChat player objects instead)

#### ⚙️ Miscellaneous Tasks

- Remove repeated docker image building
### [11.5.8] - 2025-10-17

#### 🐛 Bug Fixes

- *(package.json)* Correct cyan object pool version
### [11.5.7] - 2025-10-17

#### 🐛 Bug Fixes

- *(package.json)* Try yet another version pattern
### [11.5.6] - 2025-10-17

#### 🐛 Bug Fixes

- *(package.json)* Remove unsupported -0 which would support pre-release versions
### [11.5.5] - 2025-10-17

#### 🐛 Bug Fixes

- Switch to comprehensive version string
### [11.5.4] - 2025-10-13

#### 🐛 Bug Fixes

- Compiler errors on android builds, correct runtime test prefabs

#### ⚙️ Miscellaneous Tasks

- Add extendable default editor providing description on scripts
- Update to VRC SDK 3.9.0
- Correct new version
### [11.5.3] - 2025-10-03

#### 🧪 Testing

- Update test runner prefab

#### ⚙️ Miscellaneous Tasks

- Refresh demo worlds
- Update version
- Refresh license
### [11.5.2] - 2025-10-02

#### 🐛 Bug Fixes

- Wrong text prefab reference breaking some prefabs
### [11.5.1] - 2025-10-02

#### 🐛 Bug Fixes

- Compiler error in release configuration
- Stay on VRCSdk 8.x until the new version is released
### [11.5.0] - 2025-10-02

#### 🚀 Features

- Add TlpFixedUpdateRate for adjustable fixed update rate
- Add experimental Unity scene for NTP tests
- *(TestWithLogger)* Add TlpNetworkTime initialization helper
- *(TlpDefaultExecutionOrder)* Add validation for type consistency and duplicate target types
- *(UdonUtils)* Add EnableDuringOccupation component for trigger-based GameObject toggling
- *(UdonUtils)* Add PlayOnEnable for audio playback handling
- *(TlpDefaultExecutionOrder)* Add lower limit validation for execution order
- *(TlpDefaultExecutionOrder)* Add upper limit validation for execution order
- *(UdonUtils)* Add ResultHelper for standardized success/error handling
- *(UdonUtils)* Enhance ResultHelper with null checks and reference unwrapping
- *(PlayerDataRestoredEvent)* Add custom editor and enhance debugging
- *(UdonUtils)* Adjust fixed update rate constraints and cleanup unused code
- *(UdonUtils)* Add FrameRateRandomizer for fluctuating framerate simulation
- Add Domain Reload menu option for development

#### 🎨 Styling

- Improve execution order logs

#### 🧪 Testing

- Fix dependency issue caused by max int execution order in test setup

#### ⚙️ Miscellaneous Tasks

- Bump version and dependencies
### [11.4.3] - 2025-07-18

#### 🚀 Features

- *(TlpBaseBehaviour)* Add debug conditional for `_isEarlyStart` flag
- *(TlpDefaultExecutionOrder)* Add debug conditional around execution order tracking
- *(TlpBaseBehaviour)* Add debug conditional around `_isEarlyStart` flag

#### ⚙️ Miscellaneous Tasks

- Bump package version to 11.4.2
### [11.4.1] - 2025-07-18

#### 🐛 Bug Fixes

- *(Readme)* Update VRC SDK version in instructions
### [11.4.0] - 2025-07-18

#### 🚀 Features

- *(execution-order)* Add AiMotion constants to execution order
- *(logging)* Make logging methods public
- *(runtime)* Add LocalPlayer property and improve Start flow
- *(networking)* Add EnsureNetworkOwnership method
- *(network)* Add ownership validation to EnsureLocalOwnership
- *(udonutils)* Add StationState class for tracking station usage
- Add methods for player data component retrieval
- *(StationState)* Add validation methods and dependency checks
- *(UdonCommon)* Enhance player data component retrieval and logging
- *(UdonCommon)* Add component retrieval with callback support
- *(UdonUtils)* Add tool for fixing UdonSharp backing behaviours
- *(LateBoneFollower)* Enhance behavior with safety checks and refactor serialization
- *(TlpDefaultExecutionOrder)* Disable dev warnings and errors in releases

#### 🐛 Bug Fixes

- *(tracking)* Ensure TrackingDataFollower checks initialization state
- *(ownership)* Add error message for failed ownership assignment
- *(ui-event)* Update sync mode to prevent unintended behavior
- *(PlayerDataRestoredEvent)* Improve error logging for invalid local player and update tag usage
- *(TlpBaseBehaviour)* Improve error messages for HasStartedOk access violations
- *(TrackingDataFollower)* Ensure component respects enabled state

#### 🚜 Refactor

- *(UdonCommon)* Rename methods and improve logging consistency
- *(UdonCommon)* Remove GetPlayerComponentWithCallback method again as it doesn't work as intended in VRC

#### 📚 Documentation

- *(StationState)* Add usage remarks to clarify script behavior

#### 🧪 Testing

- *(UdonTestUtils)* Fix new players being added to list twice
- *(TestWithLogger)* Fix regression caused by introduced LocalPlayer variable

#### ⚙️ Miscellaneous Tasks

- Bump version and minimum VRC World SDK version to 3.8.2
### [11.3.1] - 2025-06-02

#### ⚙️ Miscellaneous Tasks

- Bump version and update VRChat worlds dependency
### [11.3.0] - 2025-01-12

#### 🚀 Features

- *(Tasks)* Add step delta time, improve scheduler task skipping, add optional run-task-once-per-frame limiter, improve player idle detection
- *(Task)* Rename new RunStep method to DoTask and keep old method for backward compatibility, add obsolete warning
- *(TlpNetworkTime)* Use unscaled game time instead of time since level load to prevent slowing down of networked objects
- *(TimeSources)* Add UnscaledTimeSinceGameStart
- *(TlpNetworkTime)* Add ReferenceTime to TlpNetworkTime for snapshot compression

#### 🐛 Bug Fixes

- *(TlpAccurateSyncBehaviour)* Fix very inaccurate network time stamps when using float variables
- *(NtpTime)* Fix noise from lack of floating point accuracy

#### ⚙️ Miscellaneous Tasks

- Revert change of deltaTime
- Update compiled assets
### [11.2.0] - 2025-01-04

#### 🚀 Features

- *(Task)* Expose DefaultScheduler, add DeInitTask() callback, add EstimatedStepDuration for better framepacing, add GetNeededSteps()
- *(TaskScheduler)* Add GetProgress() of largest task, add DeInit of tasks, add DynamicIdleLimit which speeds up task processing when player head is not moving
- *(SyncedEvent)* Add SyncedEventByteArray

#### 🐛 Bug Fixes

- *(UtcTimeSource)* Set sync mode to None to prevent continuous sync on spawned PlayerObjects

#### 🚜 Refactor

- *(NtpTime)* Migrate from CyanPlayerObjects to VRC PlayerObjects

#### ⚙️ Miscellaneous Tasks

- *(Model)* Add debug logs to Dirty flag
- *(TLP_Essentials)* Update TaskScheduler defaults
- Bump version
- Remove WorldVersionCheck from NTP example scene
- Update assets
### [11.1.0] - 2024-12-22

#### 🚀 Features

- *(EditorUtils)* Move ClearLog, add RecompileAll function with hotkey Ctrl+Shift+R
- *(UdonCommon)* Add PlayerDataInfosToReadableString function
- *(Task)* Add TryScheduleTask method to easily schedule a task for execution with the default TaskScheduler

#### 🐛 Bug Fixes

- *(TlpLogger)* Fix whitelisting not working as intended

#### ⚡ Performance

- *(UdonEvent)* Add support to optionally notify listeners in the background for reduced hitching

#### 🧪 Testing

- *(TestWithLogger)* Fix missing time sources
### [11.0.2] - 2024-12-19

#### 🐛 Bug Fixes

- *(Pool)* Fix exception when pool object has no UdonSharpBehaviour
- *(Model)* NotifyIfDirty fails if Model not yet initialized
- *(Task)* New tasks are in "Finished" state with result "Unknown"
- *(StateMachine)* Fix null error on invalid state in AllStates
- *(Model)* Ignore NotifyIfDirty errors if not dirty

#### 🚜 Refactor

- *(Task)* Magic strings to const variables
### [11.0.1] - 2024-12-13

#### 🐛 Bug Fixes

- *(TaskScheduler)* Ensure it turns off without tasks
### [11.0.0] - 2024-12-11

#### 🚀 Features

- *(TlpBaseBehaviour)* Add IsReady property that initializes scripts if needed
- *(UdonEvent)* Ensure listener is ready before event is invoked
- Ensure init runs
- Add warning to isReady check
- Add camera start and end section, refactor
- Add TlpSingleton
- *(ObjectSpawner)* Use setup and validate override
- *(Model)* [**breaking**] Update isReady function for MVC
- *(MvcBase)* [**breaking**] Removed isready property
- *(UdonEvent)* Adjust executionorder
- *(Player)* Update error handling
- *(Pool)* Update error handling
- *(NtpClient)* Update error handling and initialization
- *(TlpBaseBehaviour)* Add IsActiveAndEnabled
- *(Runtime)* Update assets and prefabs
- *(ExecutionOrderCheck)* Split logging of duplicates into multiple lines
- *(Persistence)* Add custom playerdata restored event, experimental executor for own lifecycle
- Make ExecutionOrderReadOnly public
- Add more changes
- [**breaking**] Change IsReady to HasStartedOk
- Add taskscheduler and task + example
- Add progress
- Fix using all available cpu time if vsync limited
- *(ExecutionOrderCheck)* Reduce logging and remove check on compilation
- *(WorldVersionGenerator)* Remove executionorder check added to scene saving
- *(UdonEditorUtils)* Add tool options to un-/lock framerate
- *(Comparer)* Update executionorder
- *(ImageDownloader)* Turn into controller
- *(UdonCommon)* Add ToReadableString and SecondsToLocalTime
- *(Controller)* [**breaking**] Change Initialized to IsControllerInitialized
- *(UiEvent)* [**breaking**] Disable sync
- *(Executor)* Add missing executionorders
- *(Tasks)* Add missing executionorders
- *(VRCPlayerApiUtils)* Add Set-/GetPlayerTagSafe
- *(Pool)* [**breaking**] Turn pool initialization into background task
- *(DemoBlackListToggle)* [**breaking**] Turn into controller
- *(UiButton)* Add UiEvent
- *(UiTextTMP)* Enable squishing of text before it is wrapped

#### 🐛 Bug Fixes

- *(Controller)* Update error handling
- *(View)* Updated error handling
- *(Factory)* Update error handling
- *(CenterOfMass)* Update error handling
- *(InertiaTensor)* Update error handling
- Compile error
- *(TlpBaseBehaviour)* Add check if player is valid during init
- *(Model)* Improve debug logs and fix init not working during start()
- *(PlayerDataRestoredEvent)* Support players with the same name
- *(TimeSources)* Fix tag reading, update executionorders

#### ⚡ Performance

- *(UdonEvent)* Fix slow RemoveListener method

#### 🧪 Testing

- *(MVC)* Update error setting in mocks
- *(TestController)* Update error handling
- *(TestMaxSendRateSender)* Update error handling
- Update and fix broken tests
- Update and fix broken tests

#### ⚙️ Miscellaneous Tasks

- Update assets
- *(View)* Migrate to new Controller
- Update assets
- Update assets
- Update assets
- Update assets
- Update assets
- Bump version
### [10.0.1] - 2024-11-02

#### 🐛 Bug Fixes

- *(ExecuteAfter)* Address compiler error caused by special exception
### [10.0.0] - 2024-11-02

#### 🚀 Features

- *(StateMachine)* Add transtion to delayed method to statemachine
- [**breaking**] Deterministic execution order of scripts to address know VRC-bug

#### 🐛 Bug Fixes

- *(RigidbodyVelocityProvider)* Add missing dependency validation
### [9.0.0] - 2024-08-20

#### 🚀 Features

- Update events and lists, improve error handling, set some missing execution orders
### [8.2.1] - 2024-08-18

#### 🐛 Bug Fixes

- *(TlpBaseBehaviour)* Correct gameobject path in setup error message

#### ⚙️ Miscellaneous Tasks

- Prevent error log spam when logger is missing
- Add support for com.vrchat.worlds 3.7.x
### [8.2.0] - 2024-06-02

#### 🚀 Features

- Add state machine implementation with optionally synchronized transition timing

#### 🐛 Bug Fixes

- *(UtcTimeSource)* Prevent usage of utc float time due to accuracy problems
### [8.1.1] - 2024-05-27

#### 🐛 Bug Fixes

- *(NtpTime)* Fix new master not maintaining current time offset when old master leaves
### [8.1.0] - 2024-05-25

#### 🚀 Features

- *(TlpAccurateSyncbehaviour)* Use float instead of double for timestamp

#### 🐛 Bug Fixes

- Move missed base files to TLP.UdonUtils.Runtime namespace

#### ⚙️ Miscellaneous Tasks

- Bump version
### [8.0.0] - 2024-05-25

#### 🚀 Features

- Add Stopwatch TimeSource
- *(TimeSource)* [**breaking**] Custom ntp servertime synchronization, move Runtime files to TLP.UdonUtils.Runtime namespace
- Add quaternion compression utils

#### 🚜 Refactor

- Remove unused code
### [7.0.0] - 2024-05-16

#### 🚀 Features

- [**breaking**] Upgrade logging, world version check, split/extend physics based prediction utils, increase accuracy of TLP network time to double, improved overall accuracy/robustness in low performance situations, added latency checker, various prefab and scene updates
- Add GetInstance function to TlpNetworkTime, expose various variables, add basic usage documentation

#### ⚙️ Miscellaneous Tasks

- Update test scene
### [6.1.2] - 2024-05-10

#### ⚙️ Miscellaneous Tasks

- Support com.vrchat.worlds 3.6.x and Unity 2022.3.22
### [6.1.1] - 2024-05-03

#### 🚀 Features

- *(Physics)* Add PhysicsUtils class with function CalculateAccelerationAndVelocity from positions

#### 🐛 Bug Fixes

- *(DemoBlackListToggle)* Add check for unset White-/Blacklist buttons

#### ⚙️ Miscellaneous Tasks

- Prevent creating new branches on Github
### [6.0.0] - 2024-04-19

#### 🚀 Features

- *(TlpNetworkTime)* Add ExactError property and instant Drift compensation with DriftThreshold
- *(Sync)* [**breaking**] Change network timestamp resolution from float to double
### [5.3.0] - 2024-04-14

#### 🚀 Features

- *(TlpNetworkTime)* Add ExactError property
### [5.2.4] - 2024-04-14

#### 🐛 Bug Fixes

- *(VrcNetworkTime)* Ensure that there is a single point of truth throughout a given frame for VRChats network time
### [5.2.3] - 2024-04-14

#### 🐛 Bug Fixes

- *(Build)* Fix compiler errors caused by test utils
### [5.2.2] - 2024-04-14

#### 🐛 Bug Fixes

- *(Logging)* Fix logs not mentioning script name correctly
### [5.2.1] - 2024-04-13

#### 🐛 Bug Fixes

- Split get scene path functions and fix C## error in test utils
### [5.2.0] - 2024-04-13

#### 🚀 Features

- *(testing)* Add base scripts for easy unit testing
### [5.1.0] - 2024-04-11

#### 🚀 Features

- *(Prefabs)* Add ui prefabs and fonts
### [5.0.0] - 2024-04-07

#### 🚀 Features

- *(TlpAccurateSyncBehaviour)* Extract Update methods into new child classes: TlpAccurateSyncBehaviourUpdate/TlpAccurateSyncBehaviourFixedUpdate
- *(TlpAccurateSyncBehaviour)* [**breaking**] Remove getter for synced send time, make network state private

#### 🧪 Testing

- *(TlpAccurateSyncBehaviour)* Verify update methods provide correct snapshot ages

#### ⚙️ Miscellaneous Tasks

- Bump version
### [4.0.1] - 2024-04-07

#### 🐛 Bug Fixes

- *(SanityTest)* Correct object name
### [4.0.0] - 2024-04-07

#### 🚀 Features

- *(TlpAccurateSyncBehaviour)* Make Start() method virtual
- *(Sync)* Make dependencies public
- *(UdonCommon)* Add extension function to get path in the scene of a transform
- *(UdonCommon)* Add extension function to get path in the scene of a component
- *(TlpBaseBehaviour)* [**breaking**] Switch to TimeSources, change logging of time and deltas, add SetupAndValidate method and call it in Start(), fix scenes and delete obsolete ones
- *(TransformBacklog)* Prevent adding of time values from the past, add boolean return value, add tests
- *(Testing)* Added new example runtime test GameTime vs DeltaTime, restructured Testing folder

#### 🚜 Refactor

- *(TlpAccurateSyncBehaviour)* Add some descriptions

#### 🧪 Testing

- *(TimeBacklog)* Ensure interpolatable check works
- *(TransformBacklog)* Fix failure due to floating point accuracy
- *(TransformBacklog)* Fix another floating point accuracy error

#### ⚙️ Miscellaneous Tasks

- Bump version
### [3.0.0] - 2024-03-30

#### ⚙️ Miscellaneous Tasks

- Bump version
### [3.0.0-rc.3] - 2024-03-30

#### 🚀 Features

- *(TimeSource)* Add DeltaTime getter, reformat codebase
### [3.0.0-rc.2] - 2024-03-29

#### 🚀 Features

- *(TlpAccurateSyncBehaviour)* Add setup checks to start
### [3.0.0-rc.1] - 2024-03-28

#### 🚀 Features

- [**breaking**] Add time sources

#### ⚙️ Miscellaneous Tasks

- Append changelog to readme
- Prepare release 3.0.0
- Revert deletion of Testing folder
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
### [0.0.1] - 2020-04-22
