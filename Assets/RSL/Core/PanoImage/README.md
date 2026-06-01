[back](/README.md)

# PanoImage
![Pano](/docs/images/pano.gif)

Panoramic camera rendering, renders a 360-degree equirectangular view for more immersive experiences. It works best with panoramic cameras that capture a full sphere of the environment. The panoramic camera is a simple prefab that can be dragged into the scene and configured with the appropriate topics. It is derived from the CameraView component (see [CameraView](/Assets/Components/CameraView)) and has the same settings for topic and image decoding. This will filter the possible topics to only include those with the word "left" which it will then swap for "right" to get the right image. The panoramic camera will then render the left and right images to the left and right eyes respectively.
