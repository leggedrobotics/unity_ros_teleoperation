# Gaussian Splatting Point Cloud Publishing

This guide follows our example setup and is ordered as:
1. ROS message type and message construction (**main focus**)
2. Nerfstudio environment setup
3. How we integrated publishing into Nerfstudio Splatfacto

## 1) ROS Message Type and Construction (Main Focus)

In our example, the core output is `sensor_msgs/PointCloud2` on topic `splat`, plus a training step topic `splat/step` (`std_msgs/UInt16`).

### Why `PointCloud2`
- Efficient binary layout for large gaussian sets.
- Extensible schema through custom `PointField` definitions.
- Works well with ROS tooling and custom Unity parsers/shaders.

### Message schema used in our example
Each gaussian is packed into one point with `point_step = 68` bytes.

Field layout:
- `x`, `y`, `z` (position)
- `scale_0`, `scale_1`, `scale_2` (anisotropic scales)
- `rot_w`, `rot_x`, `rot_y`, `rot_z` (quaternion rotation)
- `nx`, `ny`, `nz` (normal placeholder, zeros)
- `f_dc_0`, `f_dc_1`, `f_dc_2` (DC color features)
- `opacity` (alpha)

Offsets in bytes:
- `x:0`, `y:4`, `z:8`
- `scale_0:12`, `scale_1:16`, `scale_2:20`
- `rot_w:24`, `rot_x:28`, `rot_y:32`, `rot_z:36`
- `nx:40`, `ny:44`, `nz:48`
- `f_dc_0:52`, `f_dc_1:56`, `f_dc_2:60`
- `opacity:64`

### Construction flow inside the node
1. Read tensors from `gauss_params` (for nerfstudio / gsplat).
2. Convert to CPU `float32` numpy arrays.
3. Build `PointCloud2` header (`stamp`, `frame_id`, `height=1`, `width=count`).
4. Define `PointField[]` in the exact order/offsets above.
5. Set `point_step`, `row_step`, endianness, density flags.
6. Stack attributes via `np.hstack([...])` and assign `splat.data = data.tobytes()`.
7. Publish point cloud and step message.

### Most important implementation details
- Keep array order identical to field order; otherwise decoding breaks.
- Keep everything `float32` so offsets and `point_step` stay valid.
- Ensure quaternion order is consumed as published: `rot_w, rot_x, rot_y, rot_z`.
- `UInt16` step can overflow in long runs; use `UInt32` if needed.
- Hard-coded debug file logging is optional and should usually be configurable.

## Full ROS2 Node Example

Below is the full `GaussianPublisher` node from our example with Nerfstudio Splatfacto

```python
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import PointCloud2, PointField
from std_msgs.msg import UInt16
import torch
import numpy as np

class GaussianPublisher(Node):
    def __init__(
            self,
            base_frame="world",
            pcd_topic="splat"
    ):
        super().__init__('gaussian_publisher')
        self.pcd_topic = pcd_topic
        self.base_frame = base_frame

        self.gaussian_pub = self.create_publisher(PointCloud2, self.pcd_topic, 1)
        self.step_pub = self.create_publisher(UInt16, f"{self.pcd_topic}/step", 1)
        self.get_logger().info("Gaussian Publisher Node has been started.")

    def publish_pointcloud(
        self,
        gauss_params: torch.nn.ParameterDict,
        step: int
    ):
        try:
            splat = PointCloud2()
            splat.header.stamp = self.get_clock().now().to_msg()
            splat.header.frame_id = self.base_frame
            splat.height = 1

            with torch.no_grad():
                positions = gauss_params["means"].cpu().numpy().astype(np.float32)\

                count = positions.shape[0]
                splat.width = count

                normals = np.zeros_like(positions, dtype=np.float32)
                features_dc = gauss_params["features_dc"].cpu().numpy().astype(np.float32)
                opacities = gauss_params["opacities"].data.cpu().numpy().astype(np.float32)
                scales = gauss_params["scales"].data.cpu().numpy().astype(np.float32)
                quats = gauss_params["quats"].data.cpu().numpy().astype(np.float32)

                # Write the sizes to a file
                with open("/home/jannick/Projects/GaussianSplatting/nsWorkspace/gaussian_count.txt", "a") as f:
                    f.write(f"{step},{count}\n")
                    f.write(f"positions: {positions.shape}\n")
                    f.write(f"scales: {scales.shape}\n")
                    f.write(f"quats: {quats.shape}\n")
                    f.write(f"normals: {normals.shape}\n")
                    f.write(f"features_dc: {features_dc.shape}\n")
                    f.write(f"opacities: {opacities.shape}\n")

                splat.fields = [
                    PointField(name='x', offset=0, datatype=PointField.FLOAT32, count=1),
                    PointField(name='y', offset=4, datatype=PointField.FLOAT32, count=1),
                    PointField(name='z', offset=8, datatype=PointField.FLOAT32, count=1),
                    PointField(name='scale_0', offset=12, datatype=PointField.FLOAT32, count=1),
                    PointField(name='scale_1', offset=16, datatype=PointField.FLOAT32, count=1),
                    PointField(name='scale_2', offset=20, datatype=PointField.FLOAT32, count=1),
                    PointField(name='rot_w', offset=24, datatype=PointField.FLOAT32, count=1),
                    PointField(name='rot_x', offset=28, datatype=PointField.FLOAT32, count=1),
                    PointField(name='rot_y', offset=32, datatype=PointField.FLOAT32, count=1),
                    PointField(name='rot_z', offset=36, datatype=PointField.FLOAT32, count=1),
                    PointField(name='nx', offset=40, datatype=PointField.FLOAT32, count=1),
                    PointField(name='ny', offset=44, datatype=PointField.FLOAT32, count=1),
                    PointField(name='nz', offset=48, datatype=PointField.FLOAT32, count=1),
                    PointField(name='f_dc_0', offset=52, datatype=PointField.FLOAT32, count=1),
                    PointField(name='f_dc_1', offset=56, datatype=PointField.FLOAT32, count=1),
                    PointField(name='f_dc_2', offset=60, datatype=PointField.FLOAT32, count=1),
                    PointField(name='opacity', offset=64, datatype=PointField.FLOAT32, count=1),
                ]

                splat.point_step = 68
                splat.row_step = splat.point_step * count
                splat.is_dense = True
                splat.is_bigendian = False

                data = np.hstack([positions, scales, quats, normals, features_dc, opacities])
                splat.data = data.tobytes()

                self.gaussian_pub.publish(splat)
                step_msg = UInt16()
                step_msg.data = step
                self.step_pub.publish(step_msg)
                self.get_logger().info(f"Published pointcloud at step {step} with {count} gaussians.")
        except Exception as e:
            self.get_logger().error(f"Failed to publish pointcloud: {e}")

def main(args=None):
    rclpy.init(args=args)
    gaussian_publisher = GaussianPublisher()

    try:
        rclpy.spin(gaussian_publisher)
    except KeyboardInterrupt:
        pass

    gaussian_publisher.destroy_node()
    rclpy.shutdown()

if __name__ == '__main__':
    main()
```

## 2) Nerfstudio Setup for ROS2 Jazzy

Incase you are also interested in setting up Nerfstudio to work with your ROS2 Jazzy, follow these slightly changed setup steps from the official nerfstudio installation guide.

```bash
conda create -n nerfstudio-ros python=3.12.9 -y
conda activate nerfstudio-ros
python -m pip install --upgrade pip

# Install PyTorch 2.6.0 with CUDA 11.8:
pip install torch==2.6.0+cu118 torchvision==0.21.0+cu118 --extra-index-url https://download.pytorch.org/whl/cu118

# Install Toolkit
conda install -c "nvidia/label/cuda-11.8.0" cuda-toolkit

#  Install tiny-cuda-nn and ninja
pip install ninja
pip install --no-build-isolation "git+https://github.com/NVlabs/tiny-cuda-nn/#subdirectory=bindings/torch"

# Install Nerfstudio
pip install nerfstudio==1.1.3
```

## 3) How We Added This to Nerfstudio Splatfacto

We implemented a custom model that extends Nerfstudio `SplatfactoModel` and publishes gaussians during training.

### Callback hook into training steps (core pattern)

```python
import time
import rclpy
from typing import List
from nerfstudio.models.splatfacto import SplatfactoModel
from nerfstudio.engine.callbacks import (
    TrainingCallback,
    TrainingCallbackAttributes,
    TrainingCallbackLocation,
)

class SplatfactoROSModel(SplatfactoModel):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.last_publish_time = time.time()
        self.node = GaussianPublisher(
            base_frame=self.config.base_frame,
            pcd_topic=self.config.pcd_topic,
        )

    def publish_newest_pcd(self, step: int):
        now = time.time()
        if now - self.last_publish_time < 1.0 / self.config.publish_frequency:
            return
        self.last_publish_time = now

        self.node.publish_pointcloud(
            gauss_params=self.gauss_params,
            step=step,
        )
        rclpy.spin_once(self.node, timeout_sec=0.0)

    def get_training_callbacks(
        self, training_callback_attributes: TrainingCallbackAttributes
    ) -> List[TrainingCallback]:
        callbacks = super().get_training_callbacks(training_callback_attributes)
        callbacks.append(
            TrainingCallback(
                [TrainingCallbackLocation.AFTER_TRAIN_ITERATION],
                self.publish_newest_pcd,
            )
        )
        return callbacks
```

### What matters most in this snippet
- `AFTER_TRAIN_ITERATION` is the hook that gives access to each training `step`.
- The callback receives `step: int` directly and forwards it to ROS.
- `publish_frequency` throttles output, so you do not publish every single iteration.
- `self.gauss_params` is used as the source of current gaussian tensors.
- `spin_once(..., timeout_sec=0.0)` keeps ROS callbacks serviced without blocking training.

### Why callback-based publishing
- Publishes newest gaussians directly from the training loop.
- No separate background process required.
- Rate limiting is controlled by `publish_frequency`.

## Practical Notes

- `UInt16` step topic can overflow for long training runs (>65535); switch to `UInt32` if needed.
- Writing debug metadata to a hard-coded file path is useful for local debugging, but make it optional/configurable for shared environments.
- Ensure subscribers interpret quaternion component order exactly as published (`rot_w, rot_x, rot_y, rot_z`).
