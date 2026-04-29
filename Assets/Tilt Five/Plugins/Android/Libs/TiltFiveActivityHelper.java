package com.tiltfive.client;
import android.content.Context;
import com.tiltfive.client.TiltFiveClient;

public class TiltFiveActivityHelper implements TiltFiveActivity {
    Context mAndroidContext;

    static TiltFiveClient tiltFiveClient = null;
    static long tiltFivePlatformContext;

    public TiltFiveActivityHelper(Context context) {
        mAndroidContext = context;
    }

    public long getT5PlatformContext() {
        return staticGetT5PlatformContext(mAndroidContext);
    }

    synchronized static long staticGetT5PlatformContext(Context context) {
        if (tiltFiveClient == null) {
            tiltFiveClient = new TiltFiveClient(context, "TiltFiveUnity");
            tiltFivePlatformContext = tiltFiveClient.newPlatformContext();
        }
        return tiltFivePlatformContext;
    }
}
