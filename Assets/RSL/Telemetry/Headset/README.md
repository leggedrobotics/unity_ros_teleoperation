[back](/README.md)

# Headset Publisher
![Headset Publisher](/docs/images/tf.gif)

The Headset Publisher component is responsible for publishing the headset and hand poses on the ROS system. This is done by creating a TF tree with the headset and hands as children under `vr_origin` which is the center of the Unity scene. `vr_origin` is then published relative to the odom frame (will be set to the root when TF system is updated).

The palm menu's connection pane has three independent **Pose Publishing** checkboxes:

- **TF** (on by default): publishes the origin, headset, and both hand transforms on `/tf`.
- **poses** (off by default): publishes `geometry_msgs/PoseStamped` on `/quest/pose/headset`, `/quest/pose/hand_left`, and `/quest/pose/hand_right`, with timestamps and `vr_origin` as the frame.
- **mano** (off by default): publishes both hands' 21 joint landmarks and point clouds; see [Hands](../Hands/README.md).

Enabling poses or MANO opens the corresponding network routes without requiring an existing ROS subscriber. Turning a checkbox off stops sending that representation. The decimator applies to TF and stamped poses.

For the headset x is forward, y is left, and z is up. For the hands/controller x is forward, y is down, and z is right.
