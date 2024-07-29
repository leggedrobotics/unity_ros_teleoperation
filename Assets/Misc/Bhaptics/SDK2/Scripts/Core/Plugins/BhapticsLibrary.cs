using System.Collections.Generic;
using UnityEngine;

namespace Bhaptics.SDK2
{
    public class BhapticsLibrary
    {
        private static readonly object Lock = new object();
        private static readonly List<HapticDevice> EmptyDevices = new List<HapticDevice>();

        private static AndroidHaptic android = null;
        private static bool _initialized = false;
        private static bool isAvailable = false;
        private static bool isAvailableChecked = false;

        
        private static bool enableUniversalConf = true;
        private static RuntimePlatform[] excludeUniversalPlatforms =
        {
            RuntimePlatform.Android,
            RuntimePlatform.WindowsPlayer,
        };

        public static bool enableUniversal = UniversalEnabled();

        private static Universal.BhapticsTcpClient _client = new Universal.BhapticsTcpClient();


        private static bool UniversalEnabled()
        {
            if (!enableUniversalConf)
            {
                return false;
            }
            
            foreach (var excludeUniversalPlatform in excludeUniversalPlatforms)
            {
                if (Application.platform == excludeUniversalPlatform)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool IsBhapticsAvailable(bool isAutoRunPlayer)
        {
            if (isAvailableChecked)
            {
                return isAvailable;
            }

            return IsBhapticsAvailableForce(isAutoRunPlayer);
        }

        public static bool IsBhapticsAvailableForce(bool isAutoRunPlayer)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (android == null)
                {
                    BhapticsLogManager.LogErrorFormat("IsBhapticsAvailable() android object not initialized.");
                    isAvailable = false;
                    return isAvailable;
                }

                android.RefreshPairing();
                isAvailable = android.CheckBhapticsAvailable();
                isAvailableChecked = true;
                return isAvailable;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!bhaptics_library.isPlayerInstalled())
            {
                isAvailable = false;
                isAvailableChecked = true;
                return isAvailable;
            }

            if (!bhaptics_library.isPlayerRunning() && isAutoRunPlayer)
            {
                BhapticsLogManager.LogFormat("bHaptics Player(PC) is not running, so try launch it.");
                bhaptics_library.launchPlayer(true);
            }

#endif
            isAvailable = true;
            isAvailableChecked = true;

            return isAvailable;
        }



        public static bool Initialize(string appId, string apiKey, string json, bool autoRequestBluetoothPermission = true)
        {
            lock (Lock)
            {
                if (_initialized)
                {
                    return false;
                }
                _initialized = true;
            }

            if (enableUniversal)
            {
                _client.Initialize(appId, apiKey, json);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android == null)
                {
                    BhapticsLogManager.Log("BhapticsLibrary - Initialize ");
                    android = new AndroidHaptic();
                    android.InitializeWithPermission(appId, apiKey, json, autoRequestBluetoothPermission);
                    _initialized = true;
                    return true;
                }

                return false;
            }
            
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            
            if (bhaptics_library.wsIsConnected())
            {
                BhapticsLogManager.Log("BhapticsLibrary - connection already opened");
                //return false;       // NOTE-230117      Temporary comment out for IL2CPP
            }

            BhapticsLogManager.LogFormat("BhapticsLibrary - Initialize() {0} {1}", apiKey, appId);
            return bhaptics_library.registryAndInit(apiKey, appId, json);
#endif

            return false;
        }

        public static void Destroy()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.Dispose();
                    android = null;
                }

                return;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            BhapticsLogManager.LogFormat("Destroy()");
            bhaptics_library.wsClose();
#endif

            if (enableUniversal)
            {
                _client.Destroy();
            }

            _initialized = false;
        }

        public static bool IsConnect(PositionType type)
        {
            if (!isAvailable)
            {
                return false;
            }

            return GetConnectedDevices(type).Count > 0;
        }

        public static int Play(string eventId)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (eventId == null || eventId.Equals(string.Empty))
            {
                return -1;
            }

            if (enableUniversal)
            {
                _client.Play(eventId);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.Play(eventId);
                }

                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.play(eventId);
#endif

            return -1;
        }

        public static int PlayParam(string eventId, float intensity, float duration, float angleX, float offsetY)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (eventId == null || eventId.Equals(string.Empty))
            {
                return -1;
            }


            if (enableUniversal)
            {
                _client.Play(eventId, intensity, duration, angleX, offsetY);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.PlayParam(eventId, intensity, duration, angleX, offsetY);
                }

                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playPosParam(eventId, 0, intensity, duration, angleX, offsetY);
#endif

            return -1;
        }

        public static int PlayMotors(int position, int[] motors, int durationMillis)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (enableUniversal)
            {
                // TODO
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.PlayMotors(position, motors, durationMillis);
                }

                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playDot(position, durationMillis, motors, motors.Length);
#endif
            return -1;
        }

        public static int PlayWaveform(PositionType positionType, int[] motorValues, GlovePlayTime[] playTimeValues, GloveShapeValue[] shapeValues)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (motorValues.Length != 6 || playTimeValues.Length != 6 || shapeValues.Length != 6)
            {
                BhapticsLogManager.LogError("[bHaptics] BhapticsLibrary - PlayWaveform() 'motorValues, playTimeValues, shapeValues' necessarily require 6 values each.");
                return -1;
            }


            var playTimes = new int[playTimeValues.Length];
            var shapeVals = new int[shapeValues.Length];

            for (int i = 0; i < playTimes.Length; i++)
            {
                playTimes[i] = (int)playTimeValues[i];
            }
            for (int i = 0; i < shapeVals.Length; i++)
            {
                shapeVals[i] = (int)shapeValues[i];
            }

            if (enableUniversal)
            {
                // TODO
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.PlayGlove((int)positionType, motorValues, playTimes, shapeVals);
                }
                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playWaveform((int)positionType, motorValues, playTimes, shapeVals, 6);
#endif
            return -1;
        }

        public static int PlayPath(int position, float[] xValues, float[] yValues, int[] intensityValues, int duration)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (enableUniversal)
            {
                // TODO
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.PlayPath(position, xValues, yValues, intensityValues, duration);
                }

                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playPath(position, xValues, yValues, intensityValues, duration);
#endif
            return -1;
        }

        public static int PlayLoop(string eventId, float intensity, float duration, float angleX, float offsetY, int interval, int maxCount)
        {
            if (!isAvailable)
            {
                return -1;
            }

            if (eventId == null || eventId.Equals(string.Empty))
            {
                return -1;
            }

            if (enableUniversal)
            {
                // TODO
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.PlayLoop(eventId, intensity, duration, angleX, offsetY, interval, maxCount);
                }

                return -1;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.playLoop(eventId, intensity, duration, angleX, offsetY, interval, maxCount);
#endif
            return -1;
        }

        public static bool StopByEventId(string eventId)
        {
            if (!isAvailable)
            {
                return false;
            }

            if (enableUniversal)
            {
                _client.StopByEventId(eventId);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.StopByEventId(eventId);
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.stopByEventId(eventId);
#endif
            return false;
        }

        public static bool StopInt(int requestId)
        {
            if (!isAvailable)
            {
                return false;
            }

            if (enableUniversal)
            {
                _client.StopByRequestId(requestId);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.StopByRequestId(requestId);
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.stop(requestId);
#endif
            return false;
        }

        public static bool StopAll()
        {
            if (!isAvailable)
            {
                return false;
            }

            if (enableUniversal)
            {
                _client.StopAll();
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.Stop();
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.stopAll();
#endif
            return false;
        }

        public static bool IsPlaying()
        {
            if (!isAvailable)
            {
                return false;
            }


            if (enableUniversal)
            {
                // TODO ;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.IsPlaying();
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.isPlaying();
#endif
            return false;
        }
        public static bool IsPlayingByEventId(string eventId)
        {
            if (!isAvailable)
            {
                return false;
            }


            if (enableUniversal)
            {
                // TODO ;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.IsPlayingByEventId(eventId);
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.isPlayingByEventId(eventId);
#endif
            return false;
        }

        public static bool IsPlayingByRequestId(int requestId)
        {
            if (!isAvailable)
            {
                return false;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.IsPlayingByRequestId(requestId);
                }

                return false;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.isPlayingByRequestId(requestId);
#endif
            return false;
        }

        public static List<HapticDevice> GetDevices()
        {
            if (!isAvailable)
            {
                return EmptyDevices;
            }


            if (enableUniversal)
            {
                // TODO ;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    return android.GetDevices();
                }

                return EmptyDevices;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return bhaptics_library.GetDevices();
#endif

            return EmptyDevices;
        }

        public static List<HapticDevice> GetConnectedDevices(PositionType pos)
        {
            if (!isAvailable)
            {
                return EmptyDevices;
            }

            var pairedDeviceList = new List<HapticDevice>();
            var devices = GetDevices();
            foreach (var device in devices)
            {
                if (device.IsPaired && device.Position == pos && device.IsConnected)
                {
                    pairedDeviceList.Add(device);
                }
            }

            return pairedDeviceList;
        }

        public static List<HapticDevice> GetPairedDevices(PositionType pos)
        {
            if (!isAvailable)
            {
                return EmptyDevices;
            }

            var res = new List<HapticDevice>();
            var devices = GetDevices();
            foreach (var device in devices)
            {
                if (device.IsPaired && device.Position == pos)
                {
                    res.Add(device);
                }
            }

            return res;
        }

        public static void Ping(PositionType pos)
        {
            if (!isAvailable)
            {
                return;
            }

            var currentDevices = GetConnectedDevices(pos);

            foreach (var device in currentDevices)
            {
                Ping(device);
            }
        }

        public static void Ping(HapticDevice targetDevice)
        {
            if (!isAvailable)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.Ping(targetDevice.Address);
                }

                return;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.ping(targetDevice.Address);
#endif
        }

        public static void PingAll()
        {
            if (!isAvailable)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.PingAll();
                }

                return;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.pingAll();
#endif
        }

        public static void TogglePosition(HapticDevice targetDevice)
        {
            if (!isAvailable)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                if (android != null)
                {
                    android.TogglePosition(targetDevice.Address);
                }

                return;
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bhaptics_library.swapPosition(targetDevice.Address);
#endif
        }

        public static void OnApplicationFocus()
        {
            IsBhapticsAvailableForce(false);
        }

        public static void OnApplicationPause()
        {
            StopAll();
        }

        public static void OnApplicationQuit()
        {
            Destroy();
        }

#if UNITY_EDITOR
        public static List<MappingMetaData> EditorGetEventList(string appId, string apiKey, int lastVersion, out int status)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                status = 0;
                return new List<MappingMetaData>();
            }

            var res = bhaptics_library.EditorGetEventList(appId, apiKey, lastVersion, out int code);
            status = code;
            return res;
        }

        public static string EditorGetSettings(string appId, string apiKey, int lastVersion, out int status)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                status = 0;
                return "";
            }

            var bytes = bhaptics_library.EditorGetSettings(appId, apiKey, lastVersion, out int code);
            BhapticsLogManager.LogFormat("EditorGetSettings {0} {1}", code, bytes);
            status = code;
            return bytes;
        }

        public static bool EditorReInitialize(string appId, string apiKey, string json)
        {
            lock (Lock)
            {
                _initialized = true;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                return false;
            }

            BhapticsLogManager.LogFormat("[bHaptics] BhapticsLibrary - ReInitialize() {0} {1}", apiKey, appId);
            return bhaptics_library.reInitMessage(apiKey, appId, json);
        }
#endif
    }
}
