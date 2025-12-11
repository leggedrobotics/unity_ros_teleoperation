
# Unity ROS Teleoperation Project
[![Unity 2022.3.12f1](https://img.shields.io/badge/Unity-2022.3.12f1-blue?logo=unity)](https://unity.com/releases/editor/whats-new/2022.3.12f1)
[![Version 0.1.0](https://img.shields.io/badge/version-0.1.0-green)]()
[![License: BSD-3-Clause](https://img.shields.io/badge/License-BSD--3--Clause-blue.svg)](https://opensource.org/licenses/BSD-3-Clause)
[![Platform: Linux x64](https://img.shields.io/badge/platform-Linux%20x64-lightgrey?logo=linux)]()
[![Platform: Quest 3](https://img.shields.io/badge/platform-Quest%203-blueviolet?logo=oculus)]()
[![Project Page](https://img.shields.io/badge/Project%20Page-rffr.leggedrobotics.com-blue?logo=internet)](https://rffr.leggedrobotics.com/works/xr/)

This repo contains a series of components for Unity XR teleoperation with ROS integration. It is designed to be run on a Quest 3 VR headset, and support OpenXR, hand tracking, Unity input system, and is built with Unity 2022.3.12f1.

For information on setting up Unity and opening this project [Unity Quickstart](docs/unity.md), and for Quest information see [Quest Quickstart](docs/quest.md). To side load apps to the Quest see [SideQuest Quickstart](docs/sidequest.md), and for streaming the app to a linux device see [our streaming script](https://github.com/leggedrobotics/quest-streaming).


## Quickstart
On the ROS side, the custom [TCP Endpoint](https://github.com/leggedrobotics/ROS-TCP-Endpoint) needs to be run somewhere on the ROS network. This node is pretty lightweight so it can be run directly on a robot. Once the node is running the device running this app needs to connect to the ROS network, and the IP of the device running the TCP Endpoint needs to be set in the menu (see [Menu](Assets/Components/Menu) for more information). If everything works, the menu should turn green, and data should be streaming between the app and ROS. For custom robots, check out the documentation on [adding new robot models](Assets/Components/Robots/).


## Components

| Component | Description | Location | Preview |
| --- | --- | --- | --- |
| Audio Streamer | Implements bidirectional audio stream over ROS | [Assets/Components/AudioStreamer](Assets/Components/AudioStreamer) | ![](/docs/images/AudioStreamer.gif) |
| Camera Viewer | Renders a ROS image stream to a floating image window | [Assets/Components/CameraView](Assets/Components/CameraView) | ![](/docs/images/images.gif) |
| Grid Map | Renders a 2.5D grid map such as elevation maps from Anymal | [Assets/Components/GridMap](Assets/Components/GridMap) | ![](/docs/images/gridmap.gif) |
| Hands | Hand tracking and pose publishing over ROS, compatible with Ability hand models | [Assets/Components/Hands](Assets/Components/Hands) | ![](/docs/images/hands.gif) |
| Haptic | Bhaptic glove support  plus controller haptics | [Assets/Components/Haptics](Assets/Components/Haptics) | ![](/docs/images/haptics.png) | 
| Headset Publisher | Publishes headset and hand poses on TF and Pose | [Assets/Components/HeadsetPublisher](Assets/Components/HeadsetPublisher) | ![](/docs/images/tf.gif) |
| Lidar | GPU rendering for LiDAR and PointCloud2 point viz from ROS | [Assets/Components/Lidar](Assets/Components/Lidar) | ![](/docs/images/lidar.gif) |
| Markers | Displays various ROS markers in the scene (supports points, cubes, spheres) | [Assets/Components/Markers](Assets/Components/Markers) | ![](/docs/images/markers.gif) |
| Menu | Palm menu for interaction and toggling | [Assets/Components/Menu](Assets/Components/Menu) | ![](/docs/images/menu.gif) |
| NeRFViewer | Handheld viewer for rendering NeRFs and scene interaction | [Assets/Components/NeRFViewer](Assets/Components/NeRFViewer) | ![](/docs/images/nerf.gif) |
| PathStreamer | Displays nav paths with a line and series of arrows | [Assets/Components/PathStreaming](Assets/Components/PathStreaming) | ![](/docs/images/path.gif) |
| PosePublisher | Publishes poses and Joystick commands for locomotion | [Assets/Components/PosePublisher](Assets/Components/PosePublisher) | ![](/docs/images/posegoals.gif) |
| PoseStreamer | Displays stamped poses | [Assets/Components/PoseStreaming](Assets/Components/PoseStreaming) | ![](/docs/images/poses.gif) |
| Robots | Robot model manager, holds the meshes, materials and the scripts to swap between Anymal, ALMA, Tytan, Standalone Dynaarm and Franka Panda | [Assets/Components/Robots](Assets/Components/Robots) | ![](/docs/images/robots.jpg) |
| Service Caller | Creates a floating button that triggers an Empty service call when pressed | [Assets/Components/ServiceCaller](Assets/Components/ServiceCaller) | ![](/docs/images/service.png) |
| Stereo | Stereo camera rendering, renders to each eye for human depth perception | [Assets/Components/StereoImage](Assets/Components/StereoImage) | ![](/docs/images/stereo.gif) |
| TF | Updates for the Unity-ROS TF system for managing robots and reorientation including publishing headset and hands over TF | [Assets/Components/TFSystem](Assets/Components/TFSystem) | ![](/docs/images/tf.gif) |
| TF Viz | Debug viewer to show currently streamed TF links | [Assets/Components/TFViz](Assets/Components/TFViz) | ![](/docs/images/tfdebug.gif) |
| Voxblox | Voxel mesh rendering | [Assets/Components/VoxBlox](Assets/Components/VoxBlox) | ![](/docs/images/vox.gif) |
| VR Debug | Debugging tools for VR, namely a console | [Assets/Components/VRDebug](Assets/Components/VRDebug) | ![](/docs/images/vr_console.jpg) |
| VR Streamer | Streams a the VR view to a ROS topic (w/o AR view) | [Assets/Components/VRStreamer](Assets/Components/VRStreamer) | ![](/docs/images/streamer.jpg) |


## Scenes
In general the scenes should have a few objects by default:
- Light source (usually the default directional light)
- MR Interaction Setup (this enables AR/VR support and acts as a camera)
- Debug canvas (will autolink to the menu and shows debug messages)
- Palm Menu (menu to interact and toggle things with)
- Root (The roof of the TF/object tree and tagged at 'root')

---

## Minor Version History

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