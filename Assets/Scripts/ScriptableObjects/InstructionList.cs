using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstructionList", menuName = "Scriptable Objects/InstructionList")]
public class InstructionList : ScriptableObject
{
    public List<InstructionStep> steps;
}
