/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using UnityEngine;
using TiltFive.Logging;
using System;

namespace TiltFive
{
    public static class SystemControl
    {
        public static bool IsTiltFiveUIRequestingAttention()
        {
            T5_Bool attentionRequested = false;
            try
            {
                var result = NativePlugin.IsTiltFiveUIRequestingAttention(ref attentionRequested);

                // Return false if the native client reports an internal error while running this query.
                if (result != NativePlugin.T5_RESULT_SUCCESS)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }

            return attentionRequested;
        }

        internal static ServiceCompatibility GetServiceCompatibility()
        {
            var compatibility = ServiceCompatibility.Unknown;

            try
            {
                compatibility = NativePlugin.GetServiceCompatibility();
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
            }

            return compatibility;
        }

        internal static bool SetApplicationInfo()
        {
            string applicationName = Application.productName;
#if UNITY_EDITOR
            // TODO: Localize
            applicationName = $"Unity Editor: {applicationName}";
#endif
            string applicationId = Application.identifier;
            string productVersion = Application.version;
            string engineVersion = Application.unityVersion;
            TextAsset pluginVersionAsset = (TextAsset)Resources.Load("pluginversion");
            string applicationVersionInfo = $"App: {productVersion}, Engine: {engineVersion}, T5 SDK: {pluginVersionAsset.text}";

            int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;

            try
            {
                using (T5_StringUTF8 appName = applicationName)
                using (T5_StringUTF8 appId = applicationId)
                using (T5_StringUTF8 appVersion = applicationVersionInfo)
                {
                    result = NativePlugin.SetApplicationInfo(appName, appId, appVersion);
                }
            }
            catch (System.DllNotFoundException e)
            {
                Log.Info("Could not connect to Tilt Five plugin to register project info: {0}", e);
            }
            catch (Exception)
            {
                Log.Error("Failed to register project info with the Tilt Five service.");
            }

            return result == NativePlugin.T5_RESULT_SUCCESS;
        }

        internal static bool SetPlatformContext()
        {
#if UNITY_ANDROID
            if (Application.platform == RuntimePlatform.Android)
            {
                // Ensure the current thread is attached to the JVM
                AndroidJNI.AttachCurrentThread();

                IntPtr unityPlayerClazz = AndroidJNI.FindClass("com/unity3d/player/UnityPlayer");
                if (unityPlayerClazz == IntPtr.Zero)
                {
                    Log.Error("Failed to obtain UnityPlayer class via JNI");
                    return false;
                }

                IntPtr currentActivityFieldId = AndroidJNI.GetStaticFieldID(unityPlayerClazz, "currentActivity", "Landroid/app/Activity;");
                if (currentActivityFieldId == IntPtr.Zero)
                {
                    Log.Error("Failed to obtain UnityPlayer/currentActivity field via JNI");
                    return false;
                }

                IntPtr currentActivity = AndroidJNI.GetStaticObjectField(unityPlayerClazz, currentActivityFieldId);
                if (currentActivity == IntPtr.Zero)
                {
                    Log.Error("Failed to obtain UnityPlayer/currentActivity instance via JNI");
                    return false;
                }

                IntPtr t5ActivityClazz = AndroidJNI.FindClass("com/tiltfive/client/TiltFiveActivity");
                if (t5ActivityClazz == IntPtr.Zero)
                {
                    Log.Error("Failed to obtain TiltFive activity class via JNI");
                    return false;
                }

                IntPtr getPlatformContextMethodId = AndroidJNI.GetMethodID(t5ActivityClazz, "getT5PlatformContext", "()J");
                if (getPlatformContextMethodId == IntPtr.Zero)
                {
                    Log.Error("Failed to obtain TiltFive getT5PlatformContext() method via JNI");
                    return false;
                }

                var context = AndroidJNI.CallLongMethod(currentActivity, getPlatformContextMethodId, new jvalue[] { });
                if (context == 0)
                {
                    Log.Error("Failed to obtain TiltFive platform context via JNI");
                    return false;
                }

                // If we obtained a context from Java, send it to native
                try
                {
                    int result = NativePlugin.SetPlatformContext(new IntPtr(context));
                    if (result != NativePlugin.T5_RESULT_SUCCESS)
                    {
                        Log.Error("Tilt Five platform context set returned error: {0}", result);
                    }
                }
                catch (System.DllNotFoundException e)
                {
                    Log.Info("Tilt Five plugin unavailable for set platform context: {0}", e);
                    return false;
                }
                catch (Exception e)
                {
                    Log.Error("Failed to set Tilt Five platform context: {0}", e);
                    return false;
                }
            }
#endif  // UNITY_ANDROID

            return true;
        }
    }
}
