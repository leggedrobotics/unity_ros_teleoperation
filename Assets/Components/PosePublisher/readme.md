[back](/README.md)

# Pose Publisher
![Pose Publisher](/docs/images/posegoals.gif)

The Pose Publisher component is responsible for publishing a pose from select and drag interaction and publishes as a ActiveMission for Anymal locomotion. 


# Joystick Publishing

![Joystick](/docs/images/joysticks.png)
The Joy message contains axis (0-1 floats) and buttons (integers) which are mapped to the controllers as shown above. These are published whenever the settings panel has the odom pose locked, meaning if the origin is being moved, commands are not published.