[back](/README.md)

# Hands
![Hands](/docs/images/hands.gif)

This component is responsible for publishing the hand poses to ROS. They are published as ManoLandmarksMsg which maps the Quest hand joints to the mano hand as shown below:

Enable **mano** under **Pose Publishing** in the palm menu's connection pane. It is off by default. Each tracked hand publishes independently:

| Hand | Landmarks (`vr_haptic_msgs/ManoLandmarks`) | Points (`sensor_msgs/PointCloud`) |
| --- | --- | --- |
| Left | `/quest/hand_pose/left` | `/quest/hand_points/left` |
| Right | `/quest/hand_pose/right` | `/quest/hand_points/right` |

The existing `/quest/hand_pose` and `/quest/hand_points` topics remain aliases for the right hand. Messages use timestamped `vr_origin` headers and ROS forward/left/up coordinates. Point clouds include an `intensity` channel indicating which joints have valid poses. If hand tracking starts after the component, it reconnects to the running hand subsystem automatically.

![Mano](/docs/images/mano.png)

For the control it also publishes a [HandGestureMsg](https://github.com/leggedrobotics/vr_haptic_msgs) which simply outputs whether the left hand is pinched. This is tied to InputSystem references so is easy to change if need be.

For haptic feedback and other hand related messages add [VR haptic messages](https://github.com/leggedrobotics/vr_haptic_msgs) to your workspace. 

The message is defined simply as:

```msg
std_msgs/Header header

geometry_msgs/Point[] landmarks
```
