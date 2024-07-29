[back](/README.md)
# NeRF Viewer
![NeRF Viewer](/docs/images/nerf.gif)

The NeRF Viewer component is responsible for visualizing radiance field renders in the scene. It is a handheld prefab which renders a 2.5D view from the [rf_ros](https://github.com/leggedrobotics/rf_ros) node. It sends render goals based on the position of the prefab in the world, and will use the depth data for occlusion as well as parallax.