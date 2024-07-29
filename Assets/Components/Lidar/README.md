[back](/README.md)

# LiDAR and General PointCloud2 Rendering

![Lidar video](/docs/images/lidar.gif)

This component uses shaders to quickly and efficently render pointcloud 2 methods. There is a dropdown in the inspector for `Viz Type` which allows you to select which type of pointcloud you want to view. The main difference is that `Lidar` will look for an intensity field and render colors based on that, while the `RGBD` will use the colors from the RGB field. For dense pointclouds, it can be helpful to downsample the number of points rendered, this can be done via the `Display Pts` field which has a delegate for dynamically changing this value in runtime.

![Lidar Inspector](/docs/images/lidar_inspector.png)