[back](/README.md)

# Haptics
This houses anything related to haptic information that needs to be conveyed to the user. The two main systems right now are BHaptics and the Oculus haptic system. The Oculus haptics are used for the controllers and the BHaptics are used for the [gloves](https://www.bhaptics.com/shop/tactglove/). 

## ROS message
The haptic systems subscribe to the `/quest/haptics` topic which is a custom message type `HapticReadings` from the [Tactile tools repo](https://bitbucket.org/leggedrobotics/tactile_tools/src/master/). To use haptics please build and source this message. 

The `HapticReadings` msg is as follows:

```
Header header

Taxel index_tip
Taxel middle_tip
Taxel ring_tip
Taxel pinky_tip
Taxel thumb_tip

Taxel palm

bool is_right_hand
```

With the `Taxel` msg as follows:

```
float64 intensity
float64 force
```

The intensities are meant to be from 0-1 with force being ignored by the haptic system for the moment.
