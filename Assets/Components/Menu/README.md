[back](/README.md)

# Menu
![Menu](/docs/images/menu.gif)

The menu is main way of interacting with the system. It is a floating menu that can be summoned with the 'B' button the right controller and contains 4 submenus as well as status indicators for ROS connection. When the network connection is active the edges and robot icon will turn green. To toggle debug mode, click on the robot icon which will display the VR debug console (see [VR Debug](/Assets/Components/VRDebug)).

## PointClouds
![PointClouds](/docs/images/settings_pcl.jpg)

This menu contains controls for interaction with point cloud displays such as RGBD streams and LiDAR data. By default there are two pointcloud displays spawned into the scene, one displays RGBD streams and has a special shader for decoding RGB data from a PointCloud2 message, and one for LiDAR which displays the color map based on the intensity. Each type can be set by the respective topic name setting in this menu. When selected, a dropdown will display all the pointcloud topics. 

The trash icon at the bottom will destroy and clear both pointcloud displays. The circular button toggles display of LiDAR, and the 3D camera icon toggle display of the RGBD stream.

For more info see [Lidar](/Assets/Components/Lidar)

## General Settings
![General Settings](/docs/images/settings_general.jpg)

## Connection Settings
![Connection Settings](/docs/images/settings_wifi.jpg)

## Images
![Images](/docs/images/settings_img.jpg)