[back](/README.md)

# VR Streamer (Deprecated)
![Streamer](/docs/images/streamer.jpg)

***Note:** This component is deprecated and will be removed in future versions. It is recommended to use the [ADB streamer](https://github.com/leggedrobotics/quest-streaming) instead as it is more efficient and captures passthrough AR view.*

The VR Streamer component is responsible for streaming the VR view to a ROS topic. This is useful for debugging and monitoring the VR view from a remote location. The streamer is a simple camera that captures the view and sends it to a ROS topic, however as it only stream uncompressed images it is not recommended for bandwidth limited connections. Currently it also does not display the AR view with passthrough and instead has a black background.