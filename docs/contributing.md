[Home](../README.md)
# Contributing to Unity ROS Teleoperation
We welcome contributions from the community to help improve Unity ROS Teleoperation! Whether you're fixing bugs, adding new features, or improving documentation, your contributions are valuable to us. Here are some guidelines to help you get started.

## Bug Fixes and Performance Improvements
If you find a bug in the project or wish to contribute performance improvements, please open a PR with a clear description of the issue and how your changes address it. Make sure to include any relevant tests or examples that demonstrate the fix or improvement. If possible including a rosbag that can be used to reproduce the issue is very helpful. Make sure to only update the files that are necessary to fix the issue. If this fix requires changes to the network endpoint, please also link that PR in the description.

## New Features
We happily welcome new features and messages in the project! When working on new components try to keep them modular and self-contained. This will make it easier to review and integrate your changes. Follow the structure of most components in the project, with a test scene, scripts, materials and prefabs folders as needed. If possible, try to use the [SensorManager system](./sensors/README.md) to integrate new sensors. This will help keep the codebase consistent and easier to maintain. When updating the menu, make sure to update the PalmMenu prefab, so its varients automatically inherit the changes.

### Adding New Robot Models
To add a new robot model check the [robot model documentation](../Assets/Components/Robots/) for instructions on how to add new robot models to the project. Make sure to clean up the repo after importing a new robot model, by making sure all materials, meshes and prefabs have been moved into the robot subfolders, and that the robot is linked properly in the RobotManager.

### Scenes
In general the scenes should have a few objects by default:
- Light source (usually the default directional light)
- MR Interaction Setup (this enables AR/VR support and acts as a camera)
- Debug canvas (will autolink to the menu and shows debug messages)
- Palm Menu (menu to interact and toggle things with)
- Root (The roof of the TF/object tree and tagged at 'root')
