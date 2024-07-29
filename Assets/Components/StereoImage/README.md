[back](/README.md)

# StereoImage
![Stereo](/docs/images/stereo.gif)

Stereo camera rendering, renders to each eye for human depth perception. It works best with stereo cameras whose baseline is close to that of the VR headset (around 6cm). The stereo camera is a simple prefab that can be dragged into the scene and configured with the appropriate topics. It is derived from the CameraView component (see [CameraView](/Assets/Components/CameraView)) and has the same settings for topic and image decoding. This will filter the possible topics to only include those with the word "left" which it will then swap for "right" to get the right image. The stereo camera will then render the left and right images to the left and right eyes respectively.

It expected ROS topics of the form `/<camera_name>/left/image` and `/<camera_name>/right/image` where `<camera_name>` is the name of the stereo camera.