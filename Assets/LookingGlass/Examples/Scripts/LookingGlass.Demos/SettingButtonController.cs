using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingButtonController : MonoBehaviour
{
    public static event Action<bool> TogglePairPopUp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => TogglePairPopUp?.Invoke(true));
    }
}
