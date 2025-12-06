[Home](../README.md)
# Sensors
Sensor streams and managers are the abstract interface to interact with visualization data within the system. 



## Components
### SensorManager
This is the central component for the new system. Each manager is responsible for a specific type of sensor and spawns a series of streams for topics of that type. They contain some global options such as delete all, change the tracking state on all streams or spawn a new stream. They also handle the serialization of the streams to make the configuration persistent. Each manager should have a single `SensorStream` prefab that is used to create the streams.

There are 4 public variables that need to be set:
- `name`: The name of the sensor manager, this should be unique and will be used when saving the layout as well as allowing for debug toggling.
- `tag`: The tag `string` specifies a group for this sensor, allowing things like Stereo and Image views to both be grouped under `Camera` for menu creation.
- `streamPrefab`: The prefab that will be used to create the streams. This should be a prefab that contains the extended `SensorStream` and is spawned in by the manager. 
- `count`: Which is the UI text that will display the number of streams this manager has.

The actual Manager `GameObject` should be a UI element (ideally 30 units wide) that has buttons to peform global actions and whatever text information is needed for this series of streams. The `MenuTemplate` will insert this manager into the appropriate section when populating menus.

### MenuTemplate
This component of the menu system allows menus to be dyanmically created based on `SensorManager` tags. On start or in the Editor via `Setup Rows`, this tool will alphabetically stack all Managers that have the same tag. 

### SensorStream

### ISensorData
This interface is meant to contain any information that needs to be serialized to recreate the state of the sensor. By default it contains the following:
- `position`: The `Vector3` position of the sensor in local coordinates (usually 0s for tracked object, but may be positions for floating frames like camera views).
- `rotation`: The `Quaternion` rotation of the sensor in local coordinates.
- `scale`: The `Vector3` scale of the sensor in local coordinates.
- `topicName`: The `string` name of the topic that we are subscribed to. 

These properties can be extended based on the needs of the sensor, such as a `stereo` flag for camera views. 