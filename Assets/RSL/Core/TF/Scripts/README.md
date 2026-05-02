[back](/README.md)
# TF System
![TF System](/docs/images/tf.gif)


The TF System is responsible for managing the transformation tree in the scene. It is a singleton that is created at the start of the scene and persists throughout the scene. It is responsible for creating and updating the transformation tree based on the TF messages received from ROS. Unfortunately the default TF system is not very stable, so there is a rewrite ongoing here.