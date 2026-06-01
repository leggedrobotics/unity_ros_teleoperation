[back](/README.md)

# Headset Publisher
![Headset Publisher](/docs/images/tf.gif)

The Headset Publisher component is responsible for publishing the headset and hand poses on the ROS system. This is done by creating a TF tree with the headset and hands as children under `vr_origin` which is the center of the Unity scene. `vr_origin` is then published relative to the odom frame (will be set to the root when TF system is updated).

For the headset x is forward, y is left, and z is up. For the hands/controller x is forward, y is down, and z is right.