//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Demos {
	public class DemoCaptureAnimator : MonoBehaviour {
		[SerializeField] private HologramCamera hologramCamera;
		[SerializeField] private bool animatePosition;
		[SerializeField] private bool animateScale;
		[SerializeField] private bool animateRotation;
		[SerializeField] private bool animateClippingPlane;
		[SerializeField] private bool animateFOV;

		private Vector3 positionDirection = Vector3.up;

		private float scaleDirection = 1;

		private float twistAmount = 0;
		private float twistDirection = 1;

		private float clippingPlaneDirection = 1;
		private float FOVAnimationDirection = 1;

		private void Update () {
			HologramCameraProperties cameraData = hologramCamera.CameraProperties;

			// Move the LookingGlass Capture up and down
			if (animatePosition) {
				if (hologramCamera.transform.position.y > 2f) {
					positionDirection = Vector3.down;
				} else if (hologramCamera.transform.position.y < -2f) {
					positionDirection = Vector3.up;
				}
				hologramCamera.transform.Translate(positionDirection * 0.02f);
			}

			// Move the LookingGlass Capture up and down
			if (animateScale) {
				float size = cameraData.Size;
				if (size > 7.5) {
					scaleDirection = -1;
				} else if (size < 2.5f) {
					scaleDirection = 1;
				}
				cameraData.Size += scaleDirection * 0.025f;
			}

			// Twist the LookingGlass Capture back and forth
			if (animateRotation) {
				if (twistAmount > 20f) {
					twistDirection = -1;
				} else if (twistAmount < -20f) {
					twistDirection = 1;
				}
				twistAmount += twistDirection * 0.5f;
				hologramCamera.transform.rotation = Quaternion.Euler(0, twistAmount, 0);
			}

			// Animate the far clipping plane
			if (animateClippingPlane) {
				if (cameraData.FarClipFactor > 1.5f) {
					clippingPlaneDirection = -1;
				} else if (cameraData.FarClipFactor < 0.1f) {
					clippingPlaneDirection = 1;
				}
				cameraData.FarClipFactor += clippingPlaneDirection * 0.01f;
			}

			// Bump FOV back and forth
			if (animateFOV) {
				if (cameraData.FieldOfView > 60) {
					FOVAnimationDirection = -1;
				} else if (cameraData.FieldOfView < 15) {
					FOVAnimationDirection = 1;
				}
				cameraData.FieldOfView += FOVAnimationDirection * 0.25f;
			}
		}
	}
}
