using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bhaptics.SDK2
{
    [Serializable]
    internal class Device
    {
        public bool paired;
        public string deviceName;
        public int position;
        public bool connected;
        public string address;
        public int battery;
        public bool audioJackIn;
    }

    public enum PositionType
    {
        Vest, ForearmL, ForearmR, Head, HandL, HandR, FootL, FootR, GloveL, GloveR
    }
    public enum GloveShapeValue
    {
        Constant = 0,
        Decreasing = 1,
        Increasing = 2
    }
    public enum GlovePlayTime
    {
        None = 0,
        FiveMS = 1,
        TenMS = 2,
        TwentyMS = 4,
        ThirtyMS = 6,
        FortyMS = 8
    }

    [Serializable]
    public class MappingMetaData
    {
        public int durationMillis;
        public string key;
        public string description;
        public bool isAudio;
        public long updateTime;
        public string[] positions;
    }

    [Serializable]
    internal class MappingMessage
    {
        public bool status;
        public List<MappingMetaData> message;

        public static MappingMessage CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<MappingMessage>(jsonString);
        }
    }

    [Serializable]
    public class DeployMessage
    {
        public string name;
        public int version;
    }

    [Serializable]
    public class DeployHttpMessage
    {
        public bool status;
        public DeployMessage message;

        public static DeployHttpMessage CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<DeployHttpMessage>(jsonString);
        }
    }

    [Serializable]
    public class HapticDevice
    {
        public bool IsPaired;
        public bool IsConnected;
        public string DeviceName;
        public PositionType Position;
        public string Address;
        public PositionType[] Candidates;
        public bool IsAudioJack;
        public int Battery;
    }

    [Serializable]
    class DeviceListMessage
    {
        public Device[] devices;
    }

    public class BhapticsHelpers
    {
        [Serializable]
        private class AndroidDeviceMessage
        {
            public Device[] items;
        }

        private static readonly HapticDevice[] HapticDevices =
        {
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
            new HapticDevice(),
        };
        
        public static float Angle(Vector3 fwd, Vector3 targetDir)
        {
            var fwd2d = new Vector3(fwd.x, 0, fwd.z);
            var targetDir2d = new Vector3(targetDir.x, 0, targetDir.z);

            float angle = Vector3.Angle(fwd2d, targetDir2d);

            if (AngleDir(fwd, targetDir, Vector3.up) == -1)
            {
                angle = 360.0f - angle;
                if (angle > 359.9999f)
                    angle -= 360.0f;
                return angle;
            }

            return angle;
        }

        private static int AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
        {
            Vector3 perp = Vector3.Cross(fwd, targetDir);
            float dir = Vector3.Dot(perp, up);

            if (dir > 0.0)
            {
                return 1;
            }

            if (dir < 0.0)
            {
                return -1;
            }

            return 0;
        }

        public static string ErrorCodeToMessage(int code)
        {
            switch (code)
            {
                case 0:
                    return "BHAPTICS_SETTINGS_SUCCESS";
                case 1:
                    return "NETWORK_ERROR";
                case 2:
                    return "API_KEY_INVALID";
                case 3:
                    return "APP_ID_INVALID";
                case 4:
                    return "APPLICATION_NOT_DEPLOY";
                case 100:
                    return "NOT_CHANGED";
                case 999:
                    return "UNKNOWN_ISSUES";
            }

            return "UNKNOWN CODE";
        }

        public static List<HapticDevice> ConvertToBhapticsDevices(string deviceJson)
        {
            var res = new List<HapticDevice>();

            var devices = JsonUtility.FromJson<AndroidDeviceMessage>(deviceJson);
            for (var i = 0; i < devices.items.Length; i++)
            {
                var hDevice = HapticDevices[i];
                var d = devices.items[i];
                if (i >= 15)
                {
                    break;
                }

                hDevice.IsPaired = d.paired;
                hDevice.IsConnected = d.connected;
                hDevice.Address = d.address;
                hDevice.Position = ToDeviceType(d.position);
                hDevice.DeviceName = d.deviceName;
                hDevice.Candidates = ToCandidates(d.position);
                hDevice.Battery = d.battery;
                hDevice.IsAudioJack = d.audioJackIn;
                res.Add(hDevice);
            }

            return res;
        }

        internal static List<HapticDevice> Convert(Device[] deviceJson)
        {
            var res = new List<HapticDevice>();

            for (var i = 0; i < deviceJson.Length; i++)
            {
                res.Add(Convert(deviceJson[i]));
            }

            return res;
        }

        private static HapticDevice Convert(Device d)
        {
            var isConnected = d.connected;

            return new HapticDevice()
            {
                IsPaired = d.paired,
                IsConnected = isConnected,
                Address = d.address,
                Position = ToDeviceType(d.position),
                DeviceName = d.deviceName,
                Candidates = ToCandidates(d.position),
                Battery = d.battery,
                IsAudioJack = d.audioJackIn,
            };
        }

        private static readonly PositionType[] HeadCandidates = { PositionType.Head };
        private static readonly PositionType[] VestCandidates = { PositionType.Vest };
        private static readonly PositionType[] ArmCandidates = { PositionType.ForearmL, PositionType.ForearmR };
        private static readonly PositionType[] HandCandidates = { PositionType.HandL, PositionType.HandR };
        private static readonly PositionType[] FootCandidates = { PositionType.FootR, PositionType.FootL };
        private static readonly PositionType[] GloveLCandidates = { PositionType.GloveL };
        private static readonly PositionType[] GloveRCandidates = { PositionType.GloveR };
        private static readonly PositionType[] EmptyCandidates = {  };

        private static PositionType[] ToCandidates(int type)
        {
            switch (type)
            {
                case 3:
                    return HeadCandidates;
                case 0:
                    return VestCandidates;
                case 1:
                    return ArmCandidates;
                case 2:
                    return ArmCandidates;
                case 4:
                    return HandCandidates;
                case 5:
                    return HandCandidates;
                case 6:
                    return FootCandidates;
                case 7:
                    return FootCandidates;
                case 8:
                    return GloveLCandidates;
                case 9:
                    return GloveRCandidates;

            }

            return EmptyCandidates;
        }

        private static PositionType ToDeviceType(int type)
        {
            switch (type)
            {
                case 0:
                    return PositionType.Vest;

                case 1:
                    return PositionType.ForearmL;
                case 2:
                    return PositionType.ForearmR;
                case 3:
                    return PositionType.Head;
                case 4:
                    return PositionType.HandL;
                case 5:
                    return PositionType.HandR;
                case 6:
                    return PositionType.FootL;
                case 7:
                    return PositionType.FootR;
                case 8:
                    return PositionType.GloveL;
                case 9:
                    return PositionType.GloveR;

            }

            return PositionType.Vest;
        }
    }

}
