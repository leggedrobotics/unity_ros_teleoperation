using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LookingGlass;

public class QuiltRecordingHelper : MonoBehaviour {
    public RenderTexture rt;

    void Update() {
        if (rt == null) return;

        if (LookingGlass.HologramCamera.Instance.QuiltTexture != null) {
            Graphics.Blit(LookingGlass.HologramCamera.Instance.QuiltTexture, rt);
        }
    }
}
