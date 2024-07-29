using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Bhaptics.SDK2
{
    public class bhaptics_library
    {
        private const string ModuleName = "bhaptics_library";
        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool registryAndInit(string sdkAPIKey, string workspaceId, string initData);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool registryAndInitHost(string sdkAPIKey, string workspaceId, string initData, string url);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool wsIsConnected();

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static void wsClose();

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool reInitMessage(string sdkAPIKey, string workspaceId, string initData);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static int play(string key);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static int playPosParam(string key, int position, float intensity, float duration, float angleX, float offsetY);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool stop(int key);
        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool stopByEventId(string eventId);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool stopAll();

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool isPlaying();
        
        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool isPlayingByRequestId(int key);
        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool isPlayingByEventId(string eventId);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool isbHapticsConnected(int position);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool ping(string address);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool pingAll();

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool swapPosition(string address);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static IntPtr getDeviceInfoJson();

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool isPlayerInstalled();

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool isPlayerRunning();

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static bool launchPlayer(bool b);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static IntPtr bHapticsGetHapticMessage(string apiKey, string appId, int lastVersion,
            out int status);
        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static IntPtr bHapticsGetHapticMappings(string apiKey, string appId, int lastVersion,
            out int status);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static int playDot(int position, int durationMillis, int[] motors, int size);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static int playWaveform(int position, int[] motorValues, int[] playTimeValues, int[] shapeValues, int motorLen);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int playPath(int position, float[] xValues, float[] yValues, int[] intensityValues, int Len);

        [DllImport(ModuleName, CallingConvention = CallingConvention.Cdecl)]
        extern public static int playLoop(string key, float intensity, float duration, float angleX, float offsetY, int interval, int maxCount);

        // https://stackoverflow.com/questions/36239705/serialize-and-deserialize-json-and-json-array-in-unity
        public static List<HapticDevice> GetDevices()
        {
            IntPtr ptr = getDeviceInfoJson();

            var devicesStr = PtrToStringUtf8(ptr);

            if (devicesStr.Length == 0)
            {
                BhapticsLogManager.LogFormat("GetDevices() empty. {0}", devicesStr);
                return new List<HapticDevice>();
            }
            var hapticDevices = JsonUtility.FromJson<DeviceListMessage>("{\"devices\":" + devicesStr + "}");

            return BhapticsHelpers.Convert(hapticDevices.devices);
        }

        public static List<MappingMetaData> EditorGetEventList(string appId, string apiKey, int lastVersion, out int status)
        {
            var bytes = bHapticsGetHapticMappings(apiKey, appId, lastVersion, out int code);
            status = code;
            if (code == 0)
            {
                string str = PtrToStringUtf8(bytes);
                var mappingMessage = MappingMessage.CreateFromJSON(str);
                return mappingMessage.message;
            }

            BhapticsLogManager.LogFormat("EditorGetEventList {0}", status);
            return new List<MappingMetaData>();
        }

        public static string EditorGetSettings(string appId, string apiKey, int lastVersion, out int status2)
        {
            var bytes = bHapticsGetHapticMessage(apiKey, appId, lastVersion, out int status);

            status2 = status;
            if (status == 0)
            {

                string str = PtrToStringUtf8(bytes);
                return str;
            }

            return "";
        }

        private static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }

            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
                len++;
            if (len == 0)
            {
                return "";
            }

            byte[] array = new byte[len];
            Marshal.Copy(ptr, array, 0, len);
            return System.Text.Encoding.UTF8.GetString(array);
        }
    }
}