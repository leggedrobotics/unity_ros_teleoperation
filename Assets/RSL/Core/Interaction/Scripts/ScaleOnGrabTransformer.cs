using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace RSL.Core.Interaction
{
    public class ScaleOnButtonGrabTransformer : XRBaseGrabTransformer
    // This transformer lets you scale an object up or down while it's grabbed
    // by pressing designated buttons.
    {
        [Header("Scaling Settings")]
        public float scaleSpeed = 0.5f; // units per second
        public float minScale = 0.2f;
        public float maxScale = 3f;

        [Header("Input Actions")]
        public InputActionProperty increaseScaleAction;
        public InputActionProperty decreaseScaleAction;

        public override void OnGrab(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable)
        {
            // Enable input actions when grabbed
            increaseScaleAction.action?.Enable();
            decreaseScaleAction.action?.Enable();
        }

        public override void OnUnlink(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable)
        {
            // Disable input actions when released/unlinked
            increaseScaleAction.action?.Disable();
            decreaseScaleAction.action?.Disable();
        }

        public override void Process(
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable,
            XRInteractionUpdateOrder.UpdatePhase updatePhase,
            ref Pose targetPose,
            ref Vector3 localScale)
        {
            float delta = Time.deltaTime * scaleSpeed;

            if (increaseScaleAction.action != null && increaseScaleAction.action.IsPressed())
            {
                localScale *= (1f + delta);
            }
            else if (decreaseScaleAction.action != null && decreaseScaleAction.action.IsPressed())
            {
                localScale *= (1f - delta);
            }

            float clamped = Mathf.Clamp(localScale.x, minScale, maxScale);
            localScale = new Vector3(clamped, clamped, clamped);
        }
    }
}