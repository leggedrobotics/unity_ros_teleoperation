using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bhaptics.SDK2.Universal
{

    public static class MessageType
    {
        public const string ServerTokenMessage = "ServerTokenMessage";

        public const string SdkPlay = "SdkPlay";
        public const string SdkPing = "SdkPing";
        public const string SdkPingToServer = "SdkPingToServer";
        public const string SdkPingAll = "SdkPingAll";
        public const string SdkPlayDotMode = "SdkPlayDotMode";
        public const string SdkStopByRequestId = "SdkStopByRequestId";
        public const string SdkStopByEventId = "SdkStopByEventId";
        public const string SdkStopAll = "SdkStopAll";
        public const string SdkRequestAuthInit = "SdkRequestAuthInit";


        public const string SdkPlayLoop = "SdkPlayLoop";
        public const string SdkPlayGloveMode = "SdkPlayGloveMode";
    }

    [Serializable]
    public class PlayMessage
    {
        public string eventName;
        public int requestId;
        public float intensity = 1f;
        public float duration = 1f;
        public float offsetAngleX = 0f;
        public float offsetY = 0f;
    }

    [Serializable]
    public class AuthenticationMessage
    {
        public string sdkApiKey;
        public string applicationId;
    }

    [Serializable]
    public class TokenMessage
    {
        public string token;
        public string tokenKey;
    }

    [Serializable]
    public class TactHubMessage
    {
        public string type;
        public string message;

        public static TactHubMessage SdkPlay(PlayMessage message)
        {
            return new TactHubMessage()
            {
                message = JsonUtility.ToJson(message),
                type =  MessageType.SdkPlay
            };
        }
        public static TactHubMessage InitializeMessage(string appId, string apiKey)
        {
            return new TactHubMessage()
            {
                message = JsonUtility.ToJson(new AuthenticationMessage()
                {
                    applicationId = appId,
                    sdkApiKey = apiKey,
                }),
                type =  MessageType.SdkRequestAuthInit
            };
        }

        public static TactHubMessage SdkStopByRequestId(int requestId)
        {
            return new TactHubMessage()
            {
                message = requestId.ToString(),
                type = MessageType.SdkStopByRequestId,
            };
        }
        public static TactHubMessage SdkStopByEventId(string eventId)
        {
            return new TactHubMessage()
            {
                message = eventId,
                type = MessageType.SdkStopByEventId,
            };
        }

        public static TactHubMessage SdkPingServer()
        {
            return new TactHubMessage()
            {
                message = "",
                type = MessageType.SdkPingToServer,
            };
        }
        public static TactHubMessage SdkPing(string address)
        {
            return new TactHubMessage()
            {
                message = address,
                type = MessageType.SdkPing,
            };
        }
        public static TactHubMessage SdkPingAll()
        {
            return new TactHubMessage()
            {
                type = MessageType.SdkPingAll,
            };
        }
    

        public static readonly TactHubMessage StopAll = new TactHubMessage()
        {
            type = MessageType.SdkStopAll,
        };

    }

}
