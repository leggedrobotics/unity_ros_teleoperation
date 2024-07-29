using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#endif

[System.Serializable]
public class Robot
{

    public string name;
    public GameObject modelRoot;
    public Sprite RobotSprite;
    public readonly int id;
    private static int nextId = 0;

    public Robot()
    {
        id = nextId++;
    }

    public static implicit operator int(Robot robot)
    {
        return robot.id;
    }

    public override string ToString()
    {
        return name;
    }
}