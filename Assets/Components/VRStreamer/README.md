[back](/README.md)

# VR Streamer
![Streamer](/docs/images/streamer.jpg)

The VR Streamer component is responsible for streaming the VR view to a ROS topic. This is useful for debugging and monitoring the VR view from a remote location. The streamer is a simple camera that captures the view and sends it to a ROS topic, however as it only stream uncompressed images it is not recommended for bandwidth limited connections. Currently it also does not display the AR view with passthrough and instead has a black background.