## [1.4.2] - Jousting Jackalope

### Highlights of this Release

This release is primarily bugfixes and stability improvements.
One of those bugfixes happens to get multiple Glasses output working on Linux.

### Changes in Detail

* Unity Plug-in (Developer SDK)
  * Fix issue where stale data would persist in the onscreen preview's
  letterbox/pillarbox empty space when using Vulkan #1156

* NDK Beta (Developer SDK)
  * Fix a resource leak when using OpenGL #1206
  * Reduce likelihood that crashed Glasses can stall the running client
  application #690

* Drivers
  * (Linux) Add support for multiple Glasses output #1106
  * Fix issues where service could crash under certain conditions #859, #1225
  * Avoid booting more Glasses than can be used simultaneously #982
  * (Linux) Improve .deb package compatibility #1033
  * (Windows) Fix an issue where uninstall would incorrectly report success
  when it failed to complete #1172
  * (Windows) Improve reboot-required check #1196
  * Improve behavior and error reporting if Glasses become unresponsive #1200
  * Fix bug where the set of known connected wands could be corrupted #1042
  * (Android) Fix issue where the set of app-handled USB device IDs was not
  correctly restricted to Tilt Five devices #923

* Control Panel
  * Adjust board ordering in control panel selector #1214
  * (Linux) Fix stored settings location #1202

* Glasses
  * Improve tracking in the "XE Raised" Gameboard configuration #1128
  * Logging fixes #1129
  * Fix an issue where Glasses would occasionally crash on wake #1158

## [1.4.1+pkg.1] - Jousting Jackalope (Developer Unity SDK Package)

### Highlights of this Release

This patch release is a repackaging of the 1.4.1 Unity Plugin with changes
required to conform to the Unity Package Manager format. This release is functionally equivalent to 1.4.1 and is different only for compatibility
with the Unity package specification.

New projects should consider including the SDK via Unity Package Manager
import from the git URL https://github.com/tiltfive/unity-sdk

### Changes in Detail

* Unity Plug-in (Developer SDK)
  * Added `package.json` to define the metadata for the Unity package;
  the Unity SDK Package's identifier is "com.tiltfive.sdk"
  * Provide new getters in `Runtime/Utility/MeshAssets.cs` to return the
  location of various mesh assets provided in the SDK which change location
  depending on whether you import the package as an Asset or include it
  using Package Manager into your project
  * Added `README.md` and license files to the root of the package
  * This changelog has been moved from Resources to the root and renamed
  `CHANGELOG.md`

## [1.4.1] - Jousting Jackalope

### Highlights of this Release

This release fixes issues found via developer feedback in the Unity and NDK clients.

### Changes in Detail

* Unity Plug-in (Developer SDK)
  * Fix a null object exception when using the object template **#1131**
  * Fix configuration of Vulkan pipeline **#1134**

* NDK Beta (Developer SDK)
  * Fix issues with Vulkan pipeline which had undefined behavior and would crash on Windows **#1134**

## [1.4.0] - Jousting Jackalope

### Highlights of this Release

* Significant Wand tracking and stability improvements
* Introducing a Projection Alignment Tool that allows tuning the height and registration of displayed objects on the Gameboard
* Developers for Unity and the NDK (and other integrations using the NDK) can now provide haptic feedback in games via the Wand rumble motor
* Vulkan rendering support in the Developer SDKs
* Unity Developer SDK improvements and new utility functions
* Minumum Unity version supported for 1.4.0 is 2019.4 LTS, prior releases supported 2018.4 LTS

### Changes in Detail

* Control Panel
  * Add new Home page to Control Panel, showing most functions in a compact single page format
  * Provide more information about Glasses health check issues and support information by clicking/tapping on any of the issue cards
  * Show health check issues for USB host-side failures such as a device missing error when the Glasses should still be connected (indicating possible USB power or connectivity issues)
  * Show a de-synchronized projector health check issue card to indicate trouble with projectors on the Glasses
  * (Windows) Fix cases where launching Control Panel will not show the notification icon as intended
  * Suggest trying new batteries when attempting to pair a Wand fails #504
  * Fix many cases where Glasses would sort by name incorrectly with characters not in ASCII code point range #941

* Glasses
  * Implement full sleep/wakeup cycle for Glasses significantly lowering idle power consumption and fan usage
  * Fix bug where Glasses could crash after an IR Camera session for the Tangible Tracking camera ends
  * Fix some startup ordering issues that would occasionally cause spurious or missing logging
  * Synchronize start of frame when starting to receive new frames to avoid a MIPI error ("can't begin frame when frex is low" is the symptom in logs)
  * Synchronize display of frames with USB transfer receipt time to avoid persistent judder in some content
  * Avoid occasional stalls in frame display when events from projectors are not received correctly
  * Improve tracking when the Glasses are viewing the Gameboard from a corner position
  * Fix a memory corruption bug that was causing missed tracking points on the Gameboard
  * Restart Glasses shortly after an unexpected condition is detected instead of signaling a crash state and halting until unplugged
  * Expose audio output main level control which was previously stuck at an extremely low volume resulting in inaudible experiences on Android

* Wands
  * Add ability to continue to visually track Wands when one tracking LED is not recognized in the pattern
  * Don't use visual updates when the wand is pointing directly away from the Glasses #1019
  * Fix thresholds for matching visual blob tracking, more smoothly matching between frames
  * Reject bootstrap poses that are not aligned closely enough with tracked orientation
  * Change how Wand pattern changes are communicated and synchronized to avoid occasional loss of visual update #1010 #1031
  * Fix some lag/jitter when IMU data is a frame behind visual, integrate in next frame #963
  * Reduce incidents of "fly-off" behavior during motion of Wands when gyroscopic prediction was fitting too closely to incorrect tracking points in subsequent frames
  * Fix occasional loss of Wand events by using specific Wand timestamp epochs per Wand instead of one epoch for both
  * Fix Wand tracking stability problems for both Wands which persisted 5 to 60 seconds after a second Wand was powered on
  * Fix an issue when Wand would become erratic or unresponsive after 35 minutes #1041

* Drivers
  * (Windows) Add the *Tilt Five Projection Alignment* application that allows for per-Glasses fine-tuning to account for mismatches between perceived and expected virtual object positions; this can help if the Gameboard position appears to be offset from real-world perception of objects if they appear shifted sideways, deeper inside, or were raised above the board
  * Add "Express" installation option to install all components to the default install path
  * Prompt to close open files during uninstall so active components can be removed
  * Fix "Reset Alignment" functionality in the Control Panel so that Glasses reset without having to be unplugged/re-plugged #1018
  * (Linux) Fix occasional errors sending data to Glasses
  * Wake the Glasses only for commands that need to bring up all Glasses components
  * Explicitly sleep Glasses when the Drivers determine they are not in use
  * Store settings in a SQLite database file on all platforms instead of the prior platform-specific methods
  * Fix issue where connected Glasses which had crashed or disconnected from USB were made available to applications
  * Check if Glasses are responsive on USB before handing off to Applications #1078
  * Avoid Glasses startup issues on some USB host controllers by sending Glasses firmware in chunks of 1MB or less
  * Write logs to disk asynchronously to avoid timing side-effects from event logging
  * Improve reliability of message processing when service connection pipe fills
  * (Linux) Removed legacy dependencies on GLFW

* Client Libraries (Developer SDK)
  * Improve performance slightly on Direct3D pipelines by waiting for events after flushing the device context

* Developer Tools (Developer SDK)
  * Gameboard transforms applied to Glasses are stored as settings for those Glasses on the host where they were set, until changed again with the `gameboard_transform` tool
  * Added GUI to `gameboard_transform` tool to set or edit previously supplied transform parameters
  * (Windows) Relaunch `gameboard_transform` tool from the service, avoiding the need to run it as an Administrator account, and add `--pause` command line option to see its output which opens in another window
  * Don't draw a Gameboard outline when there is no pose in Glasses Camera Viewer

* NDK Beta (Developer SDK)
  * Expose Wand Haptics to NDK
  * Add Vulkan graphics API support
  * Add VkImageView support to the NDK for multi-pass rendering texture support
  * Add **GL_OVR_multiview2** support to the OpenGL graphics API for single-pass rendering
  * Add support for getting a (per-Glasses) projection matrix and framebuffer dimension
  * Prevent a client application from incorrectly terminating its service connection if the service responds slowly (possibly due to system load)
  * Add **t5GetGameboardTransform()** function to retrieve the current Gameboard transform set by the `gameboard_transform` tool for the Glasses

* Unity Plug-in (Developer SDK)
  * Minimum supported Unity Engine version is 2019.4 LTS (prior support level was 2018 LTS)
  * Add Wand Haptics API to Unity
  * Automatically apply common hand usages to incoming Wand devices
  * Implement 60fps target by default
  * Add Vulkan graphics API support
  * Introduce spatial transformation functions to Gameboard.cs analogous to Transform.TransformPoint, Transform.InverseTransformPoint, etc., except instead of converting between world and local space they convert between world space and Gameboard space
  * No longer require AndroidJNI unless the project is built for Android #1022
  * Increase deprecation severity of prior `primary`/`secondary` Wand enum value
  * Deprecate and reorganize single player functions in Glasses.cs
  * Deprecate `Gameboard.TryGetDimensions()` in favor of `Gameboard.TryGetGameboardExtents()` which provides additional information
  * Add enums that identify Gameboard corners and edges
  * Prevent ControllerIndex enum collision in the Unity Editor
  * Add SystemControl.cs with new `IsTiltFiveUIRequestingAttention` property and move `SetApplicationInfo()` and `SetPlatformContext()` into this module #1022
  * Don't apply DontDestroyOnLoad to non-root Singletons
  * Change Wand Availability to match documentation
  * Make native library calls and native structures `internal` instead of `public`
  * Add custom yield instructions for player & Wand connection
  * Change the default for the legacy scaling option of the Gameboard to **false**; building with this version of the SDK will require a change in how your content uses the Gameboard scale, or to explicitly enable the legacy option
  * Fix an issue where Glasses would be lost by the application despite still being connected, resulting in the user losing visual and input updates; this more heavily impacts users with lower-performance machines who may see more disconnect events

* Changes to features as shipped in 1.4.0-beta.6 Public Beta
  * Improve accuracy of warp mesh computation during Projector Alignment
  * Improved sleep mode for Glasses to reduce or eliminate need for fan when plugged in and not in use
  * Changes made for Developer SDKs and Drivers in 1.3.3 are included in the final 1.4.0 SDKs and Drivers

### Earlier releases

Release notes, known issues and downloads for this and earlier releases can be found online at docs.tiltfive.com
