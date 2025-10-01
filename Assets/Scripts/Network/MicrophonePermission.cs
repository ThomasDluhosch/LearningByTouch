using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class MicrophonePermission : MonoBehaviour
{
    private void Start()
    {
        Permission.RequestUserPermission(Permission.Microphone);
        int deviceCount = Microphone.devices.Length;
    }
}
