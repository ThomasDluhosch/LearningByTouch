using Unity.Netcode;
using UnityEngine;

public class AvatarManager : NetworkBehaviour
{
    [Header("Avatar References")]
    [SerializeField] private GameObject hintAvatarPrefab;
    [SerializeField] private GameObject paintableAvatarPrefab;

    [Header("Spawn Locations")]
    [SerializeField] private Transform[] hintAvatarSpawnLocations;
    [SerializeField] private Transform paintableAvatarSpawnLocation;

    private NetworkObject activeHintAvatar;
    private NetworkObject activePaintableAvatar;
    private PaintManager paintableAvatarPM;


    public PaintManager getPaintableAvatarPM => paintableAvatarPM;


    public void SpawnHintAvatar(MuscleData muscle)
    {
        if (!IsServer) return;

        Transform spawnPoint = hintAvatarSpawnLocations[(int)muscle.spawnLocation];

        GameObject go = Instantiate(hintAvatarPrefab, spawnPoint.position, spawnPoint.rotation);
        activeHintAvatar = go.GetComponent<NetworkObject>();

        var hintAvatar = go.GetComponent<HintAvatar>();
        hintAvatar.MuscleIndex.Value = muscle.Index;
        hintAvatar.pose.Value = (int)muscle.pose;

        activeHintAvatar.Spawn(true);

        Debug.Log($"<b><color=#00ff8c>[AvatarManager]</color></b> HintAvatar spawned with muscle {muscle.LatinName}");
    }


    public void DespawnHintAvatar()
    {
        if (!IsServer || activeHintAvatar == null || !activeHintAvatar.IsSpawned) return;

        activeHintAvatar.Despawn(true);
        activeHintAvatar = null;

        Debug.Log($"<b><color=#00ff8c>[AvatarManager]</color></b> HintAvatar despawned");
    }


    public void SpawnPaintableAvatar()
    {
        if (!IsServer) return;

        Vector3 spawnPos = paintableAvatarSpawnLocation.transform.position;
        Quaternion spawnRot = paintableAvatarSpawnLocation.transform.rotation;

        GameObject go = Instantiate(paintableAvatarPrefab, spawnPos, spawnRot);
        activePaintableAvatar = go.GetComponent<NetworkObject>();

        activePaintableAvatar.Spawn(true);
        paintableAvatarPM = activePaintableAvatar.GetComponent<PaintManager>();

        Debug.Log($"<b><color=#00ff8c>[AvatarManager]</color></b> PaintableAvatar spawned");
    }


    public void DespawnPaintableAvatar()
    {
        if (!IsServer || activePaintableAvatar == null || !activePaintableAvatar.IsSpawned) return;

        activePaintableAvatar.Despawn(true);
        activePaintableAvatar = null;
        paintableAvatarPM = null;

        Debug.Log($"<b><color=#00ff8c>[AvatarManager]</color></b> PaintableAvatar despawned");
    }
}
