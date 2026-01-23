# CRS Visualization Guide for Meta Quest

This is a guide for using the Meta Quest - CRS Visualization implemented for the Mixed Reality Course HS25 at ETH. This document explains what we visualize and how to use the visualization system.

**Prerequisites:** This guide assumes you have already set up the connection between CRS and Unity, and Unity to Meta Quest as specified in `Mixed Reality - CRS setup.md`.

---

## Table of Contents

1. [Overview](#overview)
2. [Visualization Topics](#visualization-topics)
   - [Car State (CarStream)](#1-car-state-carstream)
   - [Estimator State (EstimatorCarStream)](#2-estimator-state-estimatorcarstream)
   - [Control Input (ControlInputStream)](#3-control-input-controlinputstream)
   - [Track Boundaries (TrackStream)](#4-track-boundaries-trackstream)
3. [Scene Setup](#scene-setup)
4. [Runtime Controls](#runtime-controls)

---

## Overview

The CRS visualization system consists of four main topics, each with its own Stream script and Manager:

| Topic | Stream Script | Manager Script | Purpose |
|-------|--------------|----------------|---------|
| Car State | `CarStream.cs` | `CarManager.cs` | Visualizes the actual car position and orientation (/gt_state)|
| Estimator State | `EstimatorCarStream.cs` | `EstimatorManager.cs` | Visualizes the estimated car state (/best_state)|
| Control Input | `ControlInputStream.cs` | `ControlInputManager.cs` | Visualizes steering and torque commands (/control_input)|
| Track Boundaries | `TrackStream.cs` | `TrackManager.cs` | Visualizes the racing track boundaries (/track)|

**Note:** All manager files (`*Manager.cs`) have nearly identical implementations. They all inherit from `SensorManager` and use a custom editor (`*ManagerEditor`) that inherits from `SensorManagerEditor`. The manager provides a unified interface in the Unity Inspector for controlling the associated stream component.

---

## Visualization Topics

### 1. Car State (CarStream)

**ROS Topic:** `/car_1/car_state` (default)  
**Message Type:** `crs_msgs/car_state_cart`

#### What it Visualizes
- The actual car model at its true position and orientation
- A trailing path showing where the car has been

#### Required Prefabs/Assets
- **Car Prefab**: A 3D model of the car (must be assigned in Inspector)
- **Car Material**: Material for the car (white by default, optional override)

#### Inspector Setup

1. **Add Components to GameObject:**
   - Create a new GameObject in your scene (e.g., "CarStreamObject")
   - Add `CarStream.cs` script component
   - Add `CarManager.cs` script component

2. **Configure Required Fields:**
   - **Topic Name**: `/car_1/ros_simulator/gt_state` (or your custom topic)
   - **Car Prefab**: Drag your car 3D model prefab here
   - **Car Scale**: Adjust scale (default: 0.4)
   - **Car Material**: (Optional) Custom material for the car

3. **Trail Settings:**
   - **Show Trail**: Check to enable trailing path
   - **Trail Time**: Duration trail remains visible (default: 3s)
   - **Trail Color**: Color of the trail (default: white)

#### Runtime Behavior
- Car instance is created at start with name "Car"
- Position and rotation update based on incoming ROS messages
- Trail follows the car's path and fades over time
- Car and trail visibility can be toggled in real-time using runtime controls (see [Runtime Controls](#runtime-controls))

---

### 2. Estimator State (EstimatorCarStream)

**ROS Topic:** `/car_1/estimator_state` (or custom)  
**Message Type:** `crs_msgs/car_state_cart`

#### What it Visualizes
- An estimated/predicted car model showing where the estimator thinks the car is
- A separate trailing path for the estimated trajectory
- Useful for comparing actual vs. estimated state

#### Required Prefabs/Assets
- **Car Prefab**: Same or different 3D model as actual car
- **Estimator Color**: Distinct color to differentiate from actual car

#### Inspector Setup

1. **Add Components to GameObject:**
   - Create a new GameObject (e.g., "EstimatorStreamObject")
   - Add `EstimatorCarStream.cs` script component
   - Add `EstimatorManager.cs` script component

2. **Configure Required Fields:**
   - **Topic Name**: Your estimator topic (e.g., `car_1/estimation_node/best_state`)
   - **Car Prefab**: Drag your car 3D model prefab
   - **Car Scale**: Adjust scale (default: 0.4)
   - **Estimator Color**: Choose a distinct color (default: blue). 

3. **Visibility Settings:**
   - **Show Estimator**: Toggle visibility of estimator car. This is added since showing both cars might look buggy when the estimation is good.
   - **Show Trail**: Toggle visibility of estimator trail

4. **Trail Settings:**
   - **Trail Time**: Duration trail remains visible (default: 3s)
   - **Trail Color**: Color of estimator trail (default: blue)

#### Runtime Behavior
- Separate car instance created with name "EstimatorCar"
- Blue color (or custom) distinguishes it from actual car
- Can be toggled on/off independently during runtime using runtime controls (see [Runtime Controls](#runtime-controls))
- Trail color matches estimator color for easy identification

---

### 3. Control Input (ControlInputStream)

**ROS Topic:** `/car_1/control_input` (default)  
**Message Type:** `crs_msgs/car_input`

#### What it Visualizes
- **Steering commands**: Direction and magnitude of steering input
- **Torque/throttle commands**: Acceleration and braking inputs

#### Visualization Modes
The Control Input visualization has **two modes** that can be toggled during runtime:

##### Mode 1: Follow Car (3D World Space)
- **Steering Arrow**: Red arrow showing steering direction
- **Torque Bar**: Vertical bar showing throttle (green up) or brake (red down)
- Both elements follow the car in 3D space

##### Mode 2: Screen HUD (UI Overlay)
- **Steering Wheel**: Rotates based on steering angle
- **F1-Style Torque Bar**: Vertical bar with throttle (green) and brake (red) indicators
- Fixed position on screen, does not follow car

#### Required Prefabs/Assets
- **Car Object**: Reference to the actual car GameObject (auto-finds "Car" or can be manually assigned)
- **UI Canvas**: Canvas with steering wheel and torque bar UI elements

#### Inspector Setup

1. **Add Components to GameObject:**
   - Create a new GameObject (e.g., "ControlInputStreamObject")
   - Add `ControlInputStream.cs` script component
   - Add `ControlInputManager.cs` script component

2. **Basic Settings:**
   - **Topic Name**: (Optional) `/car_1/control_input`
   - **Car Object**: (Optional) Manually assign, or script will auto-find car named "Car"
   - **Current Mode**: Choose initial mode (Follow Car or Screen HUD)

3. **Follow Car Mode Settings:**
   - **Arrow Length**: Length of steering arrow (default: 0.3)
   - **Arrow Height**: Height above car (default: 0.25)
   - **Bar Height**: Max height of torque bar (default: 0.5)
   - **Bar Offset**: Horizontal offset from car (default: 0.25)

4. **UI Setup (Screen HUD Mode):**

   Create a Canvas with the following UI elements:

   **Steering Wheel Panel:**
   - **Steering Wheel Panel**: Parent GameObject for steering UI
   - **Steering Wheel Base**: Image component (the wheel graphic)
   - **Steering Text**: TextMeshProUGUI showing angle value

   **Torque Bar Panel:**
   - **Torque Bar Panel**: Parent GameObject for torque UI
   - **Torque Bar Background**: Image for background
   - **Torque Bar Fill Positive**: Image (Fill Type: Vertical, Bottom origin) - green
   - **Torque Bar Fill Negative**: Image (Fill Type: Vertical, Top origin) - red
   - **Torque Center Line**: Image for center reference line
   - **Torque Label Top**: TextMeshProUGUI - "ACCEL" label
   - **Torque Label Bottom**: TextMeshProUGUI - "BRAKE" label
   - **Torque Value Text**: TextMeshProUGUI for percentage value

   *Drag all UI elements to their corresponding fields in the Inspector.*

#### Runtime Behavior
- **Toggle Mode**: Press 'V' key (desktop) or A button (Quest right controller)
- **Auto-Find Car**: Script searches for GameObject named "Car" on start
- **Follow Car Mode**: Arrows and bars move with the car in 3D space
- **HUD Mode**: Fixed UI overlay with F1-style visualization

---

### 4. Track Boundaries (TrackStream)

**ROS Topic:** `/track` (default)  
**Message Type:** `visualization_msgs/MarkerArray`

#### What it Visualizes
- Racing track boundary lines
- Inner and outer track limits
- Track layout in 3D space

#### Required Prefabs/Assets
- **Line Material**: Material for rendering track boundary lines (optional)

#### Inspector Setup

1. **Add Components to GameObject:**
   - Create a new GameObject (e.g., "TrackStreamObject")
   - Add `TrackStream.cs` script component
   - Add `TrackManager.cs` script component

2. **Configure Fields:**
   - **Topic Name**: `/track` (or your custom track topic)
   - **Line Material**: (Optional) Custom material for track lines

#### Runtime Behavior
- Subscribes to MarkerArray messages
- Filters for markers with namespace "track_boundary"
- Creates two lines: inner and outer boundaries
- Lines rendered in red with 0.01 width
- Track boundaries update when new messages received

#### Technical Details
- Automatically splits marker points array in half
- First half: One boundary line
- Second half: Other boundary line
- Uses LineRenderer for 3D line visualization
- Converts ROS FLU coordinates to Unity

---

## Runtime Controls

### Control Input Visualization Toggle
- **Desktop**: Press `V` key to toggle between Follow Car and Screen HUD modes
- **Meta Quest**: Press `A` button on right controller

### Car and Trail Visibility Toggles

All visibility toggles work in real-time during playmode without requiring restart. The control scheme is symmetric - left side controls the estimator, while right side controls the main car.

#### Estimator (Left Side Controls)
**Desktop Keyboard:**
- Press `F` to toggle estimator car visibility
- Press `G` to toggle estimator trail visibility

**Meta Quest Left Controller:**
- Press `Menu` button to toggle estimator car visibility
- Press `Grip` button to toggle estimator trail visibility

#### Main Car (Right Side Controls)
**Desktop Keyboard:**
- Press `J` to toggle main car visibility
- Press `H` to toggle main car trail visibility

**Meta Quest Right Controller:**
- Press `Menu` button to toggle main car visibility
- Press `Grip` button to toggle main car trail visibility

### Summary Table

| Control | Desktop Key | Quest Controller | Function |
|---------|------------|------------------|----------|
| Estimator Car | `F` | Left Menu | Toggle estimator car visibility |
| Estimator Trail | `G` | Left Grip | Toggle estimator trail visibility |
| Main Car | `J` | Right Menu | Toggle main car visibility |
| Main Trail | `H` | Right Grip | Toggle main car trail visibility |
| Control Input Mode | `V` | Right A | Toggle between Follow Car and HUD modes |

---

## Code Architecture Notes

Each visualization topic follows a consistent two-component pattern:

**Stream Component** (`*Stream.cs`):
- Handles ROS message subscription and processing
- Creates and updates visual elements (car models, trails, UI, etc.)
- Inherits from `SensorStream` base class
- Contains the core visualization logic

**Manager Component** (`*Manager.cs`):
- Provides Inspector interface for the stream
- Inherits from `SensorManager` base class
- Uses custom editor (`*ManagerEditor` inheriting from `SensorManagerEditor`)
- Enables unified control and configuration

---

*Last updated: November 2025*  
*Course: Mixed Reality HS25, ETH Zurich*