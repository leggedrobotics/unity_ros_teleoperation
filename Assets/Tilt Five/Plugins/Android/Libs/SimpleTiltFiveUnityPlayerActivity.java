package com.tiltfive.client;
import com.unity3d.player.UnityPlayerActivity;
import com.tiltfive.client.TiltFiveActivityHelper;

public class SimpleTiltFiveUnityPlayerActivity extends UnityPlayerActivity implements TiltFiveActivity {
    TiltFiveActivityHelper mTiltFiveActivityHelper;

    @SuppressWarnings("unused")
    public long getT5PlatformContext() {
        if (mTiltFiveActivityHelper == null) {
            mTiltFiveActivityHelper = new TiltFiveActivityHelper(getApplicationContext());
        }
        return mTiltFiveActivityHelper.getT5PlatformContext();
    }
}
