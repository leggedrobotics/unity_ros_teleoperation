using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Haptic;

public class HandSubscriber : MonoBehaviour
{
    public string topicName = "/haptics";

    ROSConnection ros;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<HapticReadingsMsg>(topicName, ReceiveMessage);
    }

    int ForceMapper(double force)
    {
        int value = (int)(100 * force);
        value = Mathf.Clamp(value, 0, 100);
        return value;
    }

    void ReceiveMessage(HapticReadingsMsg msg)
    {
        bool isRight = msg.is_right_hand;

        int[] values = new int[6];
        values[0] = ForceMapper(msg.thumb_tip.intensity);
        values[1] = ForceMapper(msg.index_tip.intensity);
        values[2] = ForceMapper(msg.middle_tip.intensity);
        values[3] = ForceMapper(msg.ring_tip.intensity);
        values[4] = ForceMapper(msg.pinky_tip.intensity);
        values[5] = ForceMapper(msg.palm.intensity);


        HandManager.Instance.UpdateValues(isRight, values);
    }

}
