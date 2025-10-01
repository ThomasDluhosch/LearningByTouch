using UnityEngine;

[CreateAssetMenu(fileName = "MuscleData-", menuName = "Scriptable Objects/MuscleData")]
public class MuscleData : ScriptableObject
{
    public int Index;
    public string LatinName;
    public string GermanName;
    [TextArea(3, 10)] public string function;
    public Texture2D ReferenceTexture;
    public SpawnLocation spawnLocation;
    public Pose pose;
}