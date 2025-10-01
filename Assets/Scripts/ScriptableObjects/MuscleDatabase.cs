using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MuscleDatabase", menuName = "Scriptable Objects/MuscleDatabase")]
public class MuscleDatabase : ScriptableObject
{
    public List<MuscleData> muscles;
    public IReadOnlyList<MuscleData> Muscles => muscles;
    // public MuscleData GetRandom() =>
    //     muscles[Random.Range(0, muscles.Count)];
}
