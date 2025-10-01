using UnityEngine;

public enum UITarget {TV, Locker}

[CreateAssetMenu(fileName = "InstructionStep", menuName = "Scriptable Objects/InstructionStep")]
public class InstructionStep : ScriptableObject
{
    [TextArea(3, 10)] public string text;
    public AudioClip audioClip;
    public float duration => audioClip ? audioClip.length + 1f : 0f;
    public UITarget targetDisplay = UITarget.TV;
}