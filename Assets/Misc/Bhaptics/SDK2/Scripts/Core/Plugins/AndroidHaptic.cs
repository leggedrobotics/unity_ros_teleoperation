using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bhaptics.SDK2
{
    public class AndroidHaptic
    {
        protected static AndroidJavaObject androidJavaObject;
        
        private readonly Dictionary<string, int> eventDictionary = new Dictionary<string, int>();

        private static readonly object[] GetEventIdParams = new object[1];
        private static readonly object[] PlayGloveParams = new object[5];
        private static readonly jvalue[] PlayEventParams = new jvalue[6];
        private static readonly jvalue[] PlayLoopParams = new jvalue[8];
        private static readonly jvalue[] EmptyParams = new jvalue[0];
        private static readonly object[] PlayMotorsParams = new object[3];
        private static readonly jvalue[] IsPlayingParams = new jvalue[1];
        private static readonly jvalue[] IsPlayingByEventIdParams = new jvalue[1];
        private static readonly jvalue[] StopByRequestIdParams = new jvalue[1];
        private static readonly jvalue[] StopByEventIdParams = new jvalue[1];
        private static readonly object[] PingParams = new object[1];


        private List<HapticDevice> deviceList;

        private readonly IntPtr bhapticsWrapperObjectPtr;
        private readonly IntPtr bhapticsWrapperClassPtr;

        private readonly IntPtr initializeRequestPermissionPtr;

        private readonly IntPtr playEventPtr;
        private readonly IntPtr playMotorsPtr;
        private readonly IntPtr playLoopPtr;
        private readonly IntPtr playGlovePtr;
        
        private readonly IntPtr getEventIdPtr;

        private readonly IntPtr stopIntPtr;
        private readonly IntPtr stopByEventIdPtr;
        private readonly IntPtr stopAllPtr;
        private readonly IntPtr pingPtr;
        private readonly IntPtr pingAllPtr;

        // bool methods
        private readonly IntPtr isPlayingAnythingPtr;
        private readonly IntPtr isBhapticsUserPtr;
        private readonly IntPtr isPlayingByEventIdPtr;
        private readonly IntPtr isPlayingByRequestIdPtr;

        private readonly IntPtr refreshPairingInfoPtr;
        private readonly IntPtr getDeviceListPtr;

        public AndroidHaptic()
        {
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                androidJavaObject =
                    new AndroidJavaObject("com.bhaptics.bhapticsunity.BhapticsManagerWrapper", currentActivity);

                bhapticsWrapperObjectPtr = androidJavaObject.GetRawObject();
                bhapticsWrapperClassPtr = androidJavaObject.GetRawClass();

                initializeRequestPermissionPtr = AndroidJNIHelper.GetMethodID(
                    androidJavaObject.GetRawClass(), 
                    "initializeWithPermissionOption", 
                    "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;I)V");

                
                playEventPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "playEvent", "(IIFFFF)I");
                playGlovePtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "playGlove", "(I[I[I[II)I");
                getEventIdPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "getEventId", "(Ljava/lang/String;)I");
                playLoopPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "playLoop", "(IIFFFFII)I");
                playMotorsPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "playMotors", "(II[I)I");
                stopIntPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "stopInt");
                stopByEventIdPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "stopByEventId", "(I)Z");
                stopAllPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "stopAll");

                pingPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "ping", "(Ljava/lang/String;)V");
                pingAllPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "pingAll");

                isPlayingAnythingPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "isAnythingPlaying", "()Z");
                isPlayingByEventIdPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "isPlayingByEventId", "(I)Z");
                isPlayingByRequestIdPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "isPlayingByRequestId", "(I)Z");
                isBhapticsUserPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "isBhapticsUser");

                refreshPairingInfoPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "refreshPairing");
                getDeviceListPtr = AndroidJNIHelper.GetMethodID(bhapticsWrapperClassPtr, "getDeviceListString", "()Ljava/lang/String;");
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("AndroidHaptic {0} {1} ", e.Message, e);
            }

            deviceList = GetDevices();
        }

        public bool CheckBhapticsAvailable()
        {
            if (androidJavaObject == null)
            {
                return false;
            }
            return AndroidUtils.CallNativeBoolMethod(bhapticsWrapperObjectPtr, isBhapticsUserPtr, EmptyParams);
        }

        public void RefreshPairing()
        {
            if (androidJavaObject == null)
            {
                return;
            }

            AndroidUtils.CallNativeVoidMethod(bhapticsWrapperObjectPtr, refreshPairingInfoPtr, EmptyParams);

        }
        private int GetEventId(string eventId)
        {
            if (androidJavaObject == null)
            {
                return -1;
            }

            GetEventIdParams[0] = eventId;
            return AndroidUtils.CallNativeIntMethod(bhapticsWrapperObjectPtr, getEventIdPtr, GetEventIdParams);
        }

        public List<HapticDevice> GetDevices()
        {
            try
            {
                string result = AndroidUtils.CallNativeStringMethod(bhapticsWrapperObjectPtr, getDeviceListPtr, EmptyParams);
                deviceList = BhapticsHelpers.ConvertToBhapticsDevices(result);

                return deviceList;
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] GetDevices() {0}", e.Message);
            }

            return new List<HapticDevice>();
        }

        public void InitializeWithPermission(string workspaceId, string sdkKey, string json, bool requestPermission)
        {
            BhapticsLogManager.LogFormat("[bHaptics] InitializeWithPermission() {0} {1}", workspaceId, json);
            AndroidUtils.CallNativeVoidMethod(
                bhapticsWrapperObjectPtr, initializeRequestPermissionPtr, 
                new object[] { workspaceId, sdkKey, json, requestPermission ? 1 : 0 });
        }

        public bool IsConnect()
        {
            return false;
        }

        public bool IsPlaying()
        {
            if (androidJavaObject == null)
            {
                return false;
            }

            return AndroidUtils.CallNativeBoolMethod(bhapticsWrapperObjectPtr, isPlayingAnythingPtr, EmptyParams);
        }

        public bool IsPlayingByEventId(string eventId)
        {
            if (androidJavaObject == null)
            {
                return false;
            }
            int eventIntValue = TryGetEventIntValue(eventId);
            IsPlayingByEventIdParams[0].i = eventIntValue;
            return AndroidUtils.CallNativeBoolMethod(bhapticsWrapperObjectPtr, isPlayingByEventIdPtr, IsPlayingParams);
        }

        public bool IsPlayingByRequestId(int requestId)
        {
            if (androidJavaObject == null)
            {
                return false;
            }

            IsPlayingParams[0].i = requestId;
            return AndroidUtils.CallNativeBoolMethod(bhapticsWrapperObjectPtr, isPlayingByRequestIdPtr, IsPlayingParams);
        }

        public void RefreshPairingInfo()
        {
            if(androidJavaObject == null)
            {
                return;
            }

            AndroidUtils.CallNativeVoidMethod(bhapticsWrapperObjectPtr, refreshPairingInfoPtr, EmptyParams);
        }



        public int Play(string eventId)
        {
            return PlayParam(eventId, 1f, 1f, 0f, 0f);
        }

        public int PlayParam(string eventId, float intensity, float duration, float angleX, float offsetY)
        {
            if (androidJavaObject == null)
            {
                return -1;
            }

            int eventIntValue = TryGetEventIntValue(eventId);
            int requestId = UnityEngine.Random.Range(0, int.MaxValue);

            PlayEventParams[0].i = eventIntValue;
            PlayEventParams[1].i = requestId;
            PlayEventParams[2].f = intensity;
            PlayEventParams[3].f = duration;
            PlayEventParams[4].f = angleX;
            PlayEventParams[5].f = offsetY;

            return AndroidUtils.CallNativeIntMethod(bhapticsWrapperObjectPtr, playEventPtr, PlayEventParams);
            
        }

        public int PlayMotors(int position, int[] motors, int durationMillis)
        {
            if (androidJavaObject == null)
            {
                return -1;
            }
            
            PlayMotorsParams[0] = position;
            PlayMotorsParams[1] = durationMillis;
            PlayMotorsParams[2] = motors;

            return AndroidUtils.CallNativeIntMethod(bhapticsWrapperObjectPtr, playMotorsPtr, PlayMotorsParams);
        }

        public int PlayGlove(int position, int[] motors, int[] playTimeValues, int[] shapeValues)
        {
            if (androidJavaObject == null)
            {
                return -1;
            }

            PlayGloveParams[0] = position;
            PlayGloveParams[1] = motors;
            PlayGloveParams[2] = playTimeValues;
            PlayGloveParams[3] = shapeValues;
            PlayGloveParams[4] = 6;

            return AndroidUtils.CallNativeIntMethod(bhapticsWrapperObjectPtr, playGlovePtr, PlayGloveParams);
        }

        public int PlayPath(int position, float[] xValues, float[] yValues, int[] intensityValues, int duration)
        {
            if (androidJavaObject == null)
            {
                return -1;
            }

            androidJavaObject.Call("submitPath", "key?", position, xValues, yValues, intensityValues, duration);

            return -1;
        }

        public int PlayLoop(string eventId, float intensity, float duration, float angleX, float offsetY, int interval, int maxCount)
        {
            if(androidJavaObject == null)
            {
                return -1;
            }
            int eventIntValue = TryGetEventIntValue(eventId);
            int requestId = UnityEngine.Random.Range(0, int.MaxValue);

            PlayLoopParams[0].i = eventIntValue;
            PlayLoopParams[1].i = requestId;
            PlayLoopParams[2].f = intensity;
            PlayLoopParams[3].f = duration;
            PlayLoopParams[4].f = angleX;
            PlayLoopParams[5].f = offsetY;
            PlayLoopParams[6].i = interval;
            PlayLoopParams[7].i = maxCount;
            return AndroidUtils.CallNativeIntMethod(bhapticsWrapperObjectPtr, playLoopPtr, PlayLoopParams);
        }

        public bool StopByRequestId(int key)
        {
            if (androidJavaObject == null)
            {
                return false;
            }

            StopByRequestIdParams[0].i = key;
            return AndroidUtils.CallNativeBoolMethod(bhapticsWrapperObjectPtr, stopIntPtr, StopByRequestIdParams);
        }

        public bool StopByEventId(string eventId)
        {
            if (androidJavaObject == null)
            {
                return false;
            }

            try
            {
                int eventIntValue = TryGetEventIntValue(eventId);
                StopByEventIdParams[0].i = eventIntValue;
                return AndroidUtils.CallNativeBoolMethod(bhapticsWrapperObjectPtr, stopByEventIdPtr, StopByEventIdParams);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] StopByEventId() : {0}", e.Message);
            }

            return false;
        }

        private int TryGetEventIntValue(string eventId)
        {
            int eventIntValue = -1;
            if (eventDictionary.TryGetValue(eventId, out var value))
            {
                eventIntValue = value;
            }
            else
            {
                eventIntValue = GetEventId(eventId);
                eventDictionary[eventId] = eventIntValue;
            }

            return eventIntValue;
        }

        public bool Stop()
        {
            if (androidJavaObject != null)
            {
                try
                {
                    return AndroidUtils.CallNativeBoolMethod(bhapticsWrapperObjectPtr, stopAllPtr, EmptyParams);
                }
                catch (Exception e)
                {
                    BhapticsLogManager.LogErrorFormat("[bHaptics] Stop() : {0}", e.Message);
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (androidJavaObject != null)
            {
                androidJavaObject.Call("quit");
                androidJavaObject = null;
            }
        }

        public void TogglePosition(string address)
        {
            if (androidJavaObject == null)
            {
                return;
            }


            if (androidJavaObject != null)
            {
                androidJavaObject.Call("togglePosition", address);
            }
        }

        public void PingAll()
        {
            if (androidJavaObject == null)
            {
                return;
            }

            AndroidUtils.CallNativeVoidMethod(bhapticsWrapperObjectPtr, pingAllPtr, EmptyParams);
        }

        public void Ping(string address)
        {
            if (androidJavaObject == null)
            {
                return;
            }

            PingParams[0] = address;
            AndroidUtils.CallNativeVoidMethod(bhapticsWrapperObjectPtr, pingPtr, PingParams);
        }
    }

    internal static class AndroidUtils
    {
        internal static void CallNativeVoidMethod(IntPtr androidObjPtr, IntPtr methodPtr, object[] param)
        {
            jvalue[] args = AndroidJNIHelper.CreateJNIArgArray(param);
            try
            {
                CallNativeVoidMethod(androidObjPtr, methodPtr, args);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] CallNativeVoidMethod() : {0}", e.Message);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(param, args);
            }
        }
        internal static void CallNativeVoidMethod(IntPtr androidObjPtr, IntPtr methodPtr, jvalue[] param)
        {
            try
            {
                AndroidJNI.CallVoidMethod(androidObjPtr, methodPtr, param);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] CallNativeVoidMethod() : {0}", e.Message);
            }
        }

        internal static string CallNativeStringMethod(IntPtr androidObjPtr, IntPtr methodPtr, jvalue[] param)
        {
            try
            {
                return AndroidJNI.CallStringMethod(androidObjPtr, methodPtr, param);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] CallNativeStringMethod() : {0}", e.Message);
            }

            return "";
        }


        internal static bool CallNativeBoolMethod(IntPtr androidObjPtr, IntPtr methodPtr, object[] param)
        {
            jvalue[] args = AndroidJNIHelper.CreateJNIArgArray(param);
            bool res = false;
            try
            {
                res = CallNativeBoolMethod(androidObjPtr, methodPtr, args);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] CallNativeBoolMethod() : {0}", e.Message);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(param, args);
            }

            return res;
        }
        internal static bool CallNativeBoolMethod(IntPtr androidObjPtr, IntPtr methodPtr, jvalue[] param)
        {   
            bool res = false;
            try
            {
                res = AndroidJNI.CallBooleanMethod(androidObjPtr, methodPtr, param);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] CallNativeBoolMethod() : {0}", e.Message);
            }

            return res;
        }

        internal static int CallNativeIntMethod(IntPtr androidObjPtr, IntPtr methodPtr, object[] param)
        {
            jvalue[] args = AndroidJNIHelper.CreateJNIArgArray(param);
            int res = -1;
            try
            {
                res = CallNativeIntMethod(androidObjPtr, methodPtr, args);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] CallNativeIntMethod() : {0}", e.Message);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(param, args);
            }

            return res;
        }
        internal static int CallNativeIntMethod(IntPtr androidObjPtr, IntPtr methodPtr, jvalue[] param)
        {
            int res = -1;
            try
            {
                res = AndroidJNI.CallIntMethod(androidObjPtr, methodPtr, param);
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogErrorFormat("[bHaptics] CallNativeIntMethod() : {0}", e.Message);
            }

            return res;
        }
    }
}