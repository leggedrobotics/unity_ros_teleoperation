
# Unity ROS Teleoperation Project
[![Unity 6000.2.15f1](https://img.shields.io/badge/Unity-6000.2.15f1-blue?logo=unity)](https://unity.com/releases/editor/whats-new/6000.2.15f1)
[![Version 0.2.0](https://img.shields.io/badge/version-0.2.0-green)]()
[![License: BSD-3-Clause](https://img.shields.io/badge/License-BSD--3--Clause-blue.svg)](https://opensource.org/licenses/BSD-3-Clause)
[![Platform: Linux x64](https://img.shields.io/badge/platform-Linux%20x64-lightgrey?logo=linux)]()
[![Platform: Quest 3](https://img.shields.io/badge/platform-Quest%203-blueviolet?logo=oculus)]()
[![Project Page](https://img.shields.io/badge/Project%20Page-rffr.leggedrobotics.com-blue?logo=internet)](https://rffr.leggedrobotics.com/works/xr/)
[![Survey](https://img.shields.io/badge/Survey-Feedback%20Form-orange?logo=google-forms)](https://docs.google.com/forms/d/e/1FAIpQLSf1JQycwO8uBzeW0IydjNP1DJ7T1BoQXaRozAfCMekRT0Yvvw/viewform)

This repo contains a series of components for Unity XR teleoperation with ROS integration. It is designed to be run on a Quest 3 VR headset, and support OpenXR, hand tracking, Unity input system, and is built with Unity 6000.2.15f1. _Newer versions of Unity no longer work on ROS 1 versions of Ubuntu, so this will be the latest offically supported version of Unity for ROS 1.

For information on setting up Unity and opening this project [Unity Quickstart](docs/unity.md), and for Quest information see [Quest Quickstart](docs/quest.md). To install the git-hooks, and link this project to a Linux Unity Hub, run the `setup.sh` script. To side load apps to the Quest see [SideQuest Quickstart](docs/sidequest.md), and for streaming the app to a linux device see [our streaming script](https://github.com/leggedrobotics/quest-streaming).


## Quickstart
On the ROS side, the custom [TCP Endpoint](https://github.com/leggedrobotics/ROS-TCP-Endpoint) needs to be run somewhere on the ROS network (For ROS 2 systems, make sure to switch to the `main-ros2` branch, although the current Unity app supports both versions of ROS out of the box). This node is pretty lightweight so it can be run directly on a robot. Once the node is running the device running this app needs to connect to the ROS network, and the IP of the device running the TCP Endpoint needs to be set in the menu (see [Menu](Assets/Components/Menu) for more information). If everything works, the menu should turn green, and data should be streaming between the app and ROS. For custom robots, check out the documentation on [adding new robot models](Assets/Components/Robots/). If you are interested in contributing to the project, check out the [contributing guidelines](docs/contributing.md).


## Components

| Component | Description | Location | Preview |
| --- | --- | --- | --- |
| Audio Streamer | Implements bidirectional audio stream over ROS | [Assets/RSL/Sensors/Audio](Assets/RSL/Sensors/Audio) | ![](/docs/images/AudioStreamer.gif) |
| Camera Viewer | Renders a ROS image stream to a floating image window | [Assets/RSL/Sensors/Camera](Assets/RSL/Sensors/Camera) | ![](/docs/images/images.gif) |
| Grid Map | Renders a 2.5D grid map such as elevation maps from Anymal | [Assets/RSL/Sensors/GridMap](Assets/RSL/Sensors/GridMap) | ![](/docs/images/gridmap.gif) |
| Hands | Hand tracking and pose publishing over ROS, compatible with Ability hand models | [Assets/RSL/Telemetry/Hands](Assets/RSL/Telemetry/Hands) | ![](/docs/images/hands.gif) |
| Haptic | Bhaptic glove support  plus controller haptics | [Assets/RSL/Core/Haptics](Assets/RSL/Core/Haptics) | ![](/docs/images/haptics.png) | 
| Headset Publisher | Publishes headset and hand poses on TF and Pose | [Assets/RSL/Telemetry/Headset](Assets/RSL/Telemetry/Headset) | ![](/docs/images/tf.gif) |
| Lidar | GPU rendering for LiDAR and PointCloud2 point viz from ROS | [Assets/RSL/Sensors/Lidar](Assets/RSL/Sensors/Lidar) | ![](/docs/images/lidar.gif) |
| Markers | Displays various ROS markers in the scene (supports points, cubes, spheres) | [Assets/RSL/Sensors/Markers](Assets/RSL/Sensors/Markers) | ![](/docs/images/markers.gif) |
| Menu | Palm menu for interaction and toggling | [Assets/RSL/Core/Menu](Assets/RSL/Core/Menu) | ![](/docs/images/menu.gif) |
| NeRFViewer | Handheld viewer for rendering NeRFs and scene interaction | [Assets/RSL/Sensors/NeRF](Assets/RSL/Sensors/NeRF) | ![](/docs/images/nerf.gif) |
| PathStreamer | Displays nav paths with a line and series of arrows | [Assets/RSL/Sensors/Path](Assets/RSL/Sensors/Path) | ![](/docs/images/path.gif) |
| PosePublisher | Publishes poses and Joystick commands for locomotion | [Assets/RSL/Telemetry/Pose](Assets/RSL/Telemetry/Pose) | ![](/docs/images/posegoals.gif) |
| PoseStreamer | Displays stamped poses | [Assets/RSL/Sensors/Pose](Assets/RSL/Sensors/Pose) | ![](/docs/images/poses.gif) |
| Robots | Robot model manager, holds the meshes, materials and the scripts to swap between Anymal, ALMA, Tytan, Standalone Dynaarm and Franka Panda | [Assets/RSL/Core/Robots](Assets/RSL/Core/Robots) | ![](/docs/images/robots.jpg) |
| Service Caller | Creates a floating button that triggers an Empty service call when pressed | [Assets/RSL/Sensors/Service](Assets/RSL/Sensors/Service) | ![](/docs/images/service.png) |
| Stereo | Stereo camera rendering, renders to each eye for human depth perception | [Assets/RSL/Sensors/Camera](Assets/RSL/Sensors/Camera) | ![](/docs/images/stereo.gif) |
| TF | Updates for the Unity-ROS TF system for managing robots and reorientation including publishing headset and hands over TF | [Assets/RSL/Core/TF](Assets/RSL/Core/TF) | ![](/docs/images/tf.gif) |
| TF Viz | Debug viewer to show currently streamed TF links | [Assets/RSL/Core/TF](Assets/RSL/Core/TF) | ![](/docs/images/tfdebug.gif) |
| Voxblox | Voxel mesh rendering | [Assets/RSL/Core/VoxBlox](Assets/RSL/Core/VoxBlox) | ![](/docs/images/vox.gif) |
| VR Debug | Debugging tools for VR, namely a console | [Assets/RSL/Core/VRDebug](Assets/RSL/Core/VRDebug) | ![](/docs/images/vr_console.jpg) |
| VR Streamer | Streams the VR view to a ROS topic (w/o AR view) | [Assets/RSL/Telemetry/VRStreamer](Assets/RSL/Telemetry/VRStreamer) | ![](/docs/images/streamer.jpg) |


---

## Minor Version History

### 0.2.0
- Bumped to Unity 6.2 for improved OpenXR and Quest 3 support (note this is the last version of Unity that will support ROS 1 on Ubuntu 20.04, if you are looking for a pre Unity 6 build check out [v0.1.1](https://github.com/leggedrobotics/unity_ros_teleoperation/releases/tag/v0.1.1))
- Now supports ROS 1 and ROS 2 in the same build allowing dynamic switching with a new version of the ROS-TCP-Endpoint
- Added Gaussian Splatting as an option for PointCloud messages
- New Robot manager that moves robot models to external repos (see [unity_ros_robots](https://github.com/leggedrobotics/unity_ros_robots) for more info)
- Added support for spatial anchors ensuring persistent localization and mapping across sessions
- Improved package naming within the app for cross package imports
- Various bug fixes and improvements
- Added support for 360 stereo image streams


### 0.1.1
- Added setup scripts and git hooks for easier project setup
- Fixed some image rendering issues with Image Messages
- Added passthrough togglle
- Improved TF modes for Lidar
- Added State buttons for toggles
- Fixes for ROS 2
- Updated docs for setup and usage with streaming
- Added Robots
    - B2W
    - GR2

### 0.1.0
_May need to reset the repo as LFS has been removed from history_
- Added 2D version of the app
- Refactored new Manager-Streamer system for standardized message visualizations
- Added new visualizations
    - Markers (without meshes and text)
    - Paths
    - Stamped Poses
    - TF Visualization for frames
    - Grid Maps/Elevation Maps
- Added floating button to trigger Empty service calls
- Redid TF system so the root frame can be dynamic and not locked to odom (also allows functioning without a robot model)
- Improved Pose publishing and allows to work with 2D interface
- Added debug menus to display force
- Overhauled PointCloud rendering for better performance and shader keywords for customized coloring
- Added mesh shaders for efficently render large numbers of procedural meshes
- Improved layout serialization allowing for saving of custom configurations

### 0.0.9
- Simplified dynaarm model (down to 98 draw calls and 330k tris)
- Fixed TF pose publishing so it should work even without a model
- Removed some duplicated publishers/gameobjects

### 0.0.8
- Added versioning with display in debug mode
- Added tytan, standalone dynaarm, and franka panda robots
- Updated dynaarm for newer model (may still need simplification)
- Added robot manager for switching between robots

---

## Citing
If you use this project in your work please cite [this paper](https://arxiv.org/abs/2407.20194):
```bibtex
@INPROCEEDINGS{wildersmith2024rfteleoperation,
        author={Wilder-Smith, Maximum and Patil, Vaishakh and Hutter, Marco},
        booktitle={2024 IEEE/RSJ International Conference on Intelligent Robots and Systems (IROS)}, 
        title={Radiance Fields for Robotic Teleoperation}, 
        year={2024},
        pages={13861-13868},
        doi={10.1109/IROS58592.2024.10801345}
}
```
