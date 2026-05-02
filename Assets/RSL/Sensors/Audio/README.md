[back](/README.md)

# Audio Streamer
The Audio Streamer component integrates sport for a incomming and a outcomming audio stream via the ROS TCP enpoint.
![Audio Streamer](/docs/images/AudioStreamer.gif)

## Audio Streaming Overview

The system utilizes the ROS [audio_common](https://wiki.ros.org/audio_common) package, specifically the `audio_capture` and `audio_play` nodes, to publish and subscribe to audio streams.

In order to get the correct audio format fot both nodes, the following parameters need to be used.:

- **Audio Format**: "wave", Uncompressed PCM (Pulse-Code Modulation)
- **Bit Depth**: 16-bit
- **Sample Rate**: 16 kHz

The default audio packet chunk size (320 bytes) caused buffer underruns in Unity, producing periodic clicking noise. This was resolved by modifying the `audio_capture` node source code to increase the chunk size to **1024 bytes**.

## Receiver Script

The receiver script converts incoming audio data (16-bit signed integers, little-endian) into a float array with values in the range [-1.0, 1.0], as required by Unity’s `AudioClip`. Samples are stored in a circular buffer, which is pre-filled with 0.5 seconds of audio before playback begins to mitigate underruns caused by transmission jitter. This startup delay is configurable.

In case of buffer underruns, the system fills missing samples with the last received value to reduce audible artifacts. Additionally, a band-pass filter (150 Hz – 8,000 Hz) is applied.

## Transmitter Script

The transmitter script on the Unity side performs minimal preprocessing, relying on the ROS `audio_common` playback node for audio handling. Microphone samples are periodically extracted from Unity's audio buffer in the `Update()` loop, converted from floats to 16-bit signed integers, and serialized into a little-endian byte array. The resulting data is published to the corresponding ROS topic for transmission.

## VR User Interface Integration

Microphone and speaker control was integrated into the VR environment via toggle buttons, allowing the operator to enable or disable each component individually. These controls were added as an additional row in the sensor manager section of the palm menu. Each toggle visually indicates the current state, and an extra delete button is provided to disable both audio components simultaneously. The menu can be integrated by simply adding the 'AudioManger' prefab to the sensor menu item in the palm menu.

## Unity Audio Settings

For some VR paltforms it can help change the `DSP Buffer Size` to `Best Performance` in the Project Settigns, to increase aduio streaming quality.
![Unity Audio Settings](/docs/images/unity-audio-settings.png)