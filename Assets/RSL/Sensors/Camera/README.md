[back](/README.md)

# Camera View
![Camera View](/docs/images/images.gif)

The Camera View component is responsible for visualizing the camera feed in the scene. It can be spawned from the camera menu:

![Camera Menu](/docs/images/settings_img.jpg)

with the plus button. This will spawn a white window with text which can be clicked to open settings. On the right is a dropdown of all image topics (compressed are preferred) and the bottom will display the name of the selected topic. 

![Camera Settings](/docs/images/cameraview.jpg)

On the top is the menu which allows changing the tracking type:

| Tracking Mode | Purpose | Icon |
| ------------- | ------- | ---- |
| Free          | Allows free movement of the camera | ![Free Icon](/Assets/Materials/mdi/axis-arrow.svg) |
| TF Locked     | Locks the camera to its location on the robot | ![TF Locked Icon](/Assets/Materials/mdi/axis-arrow-lock.svg) |
| Head Locked   | Locks the camera relative to the headset following the user | ![Head Locked Icon](/Assets/Materials/mdi/head-snowflake-outline.svg) |

The trash button will delete the image view, and for some topics the image is flipped horizontally which can be fixed with the middle button. The plus and minus change the size of the window, and the window can be moved around by dragging on the name display at the bottom.

# StereoImage
![Stereo](/docs/images/stereo.gif)

Stereo camera rendering, renders to each eye for human depth perception. It works best with stereo cameras whose baseline is close to that of the VR headset (around 6cm). The stereo camera is a simple prefab that can be dragged into the scene and configured with the appropriate topics. It is derived from the CameraView component above and has the same settings for topic and image decoding. This will filter the possible topics to only include those with the word "left" which it will then swap for "right" to get the right image. The stereo camera will then render the left and right images to the left and right eyes respectively.

It expected ROS topics of the form `/<camera_name>/left/image` and `/<camera_name>/right/image` where `<camera_name>` is the name of the stereo camera.