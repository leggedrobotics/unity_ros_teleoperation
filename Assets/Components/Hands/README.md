[back](/README.md)

# Hands
![Hands](/docs/images/hands.gif)

This component is responsible for publishing the hand poses to ROS. They are published as ManoLandmarksMsg which maps the Quest hand joints to the mano hand as shown below:

![Mano](/docs/images/mano.png)

For the control it also publishes a [HandGestureMsg](https://bitbucket.org/leggedrobotics/psyonic_ability_hand/src/main/retargeting_ros) which simply outputs whether the left hand is pinched. This is tied to InputSystem references so is easy to change if need be.

The message is defined simply as:

```msg
std_msgs/Header header

geometry_msgs/Point[] landmarks
```
