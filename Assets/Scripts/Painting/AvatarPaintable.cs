using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(PaintManager))]
public class AvatarPaintable : NetworkBehaviour
{
    [SerializeField] PaintManager paintMgr;

    void Awake() => paintMgr = GetComponent<PaintManager>();

    [ServerRpc(RequireOwnership = false)]
    public void SubmitStrokeServerRpc(PaintStroke stroke, ServerRpcParams _ = default)
    {
        ApplyStrokeLocally(stroke);
        BroadcastStrokeClientRpc(stroke);
    }

    [ClientRpc]
    void BroadcastStrokeClientRpc(PaintStroke stroke, ClientRpcParams _ = default)
    {
        Debug.Log($"RPC {OwnerClientId} → {NetworkManager.Singleton.LocalClientId}");
        Debug.Log("UV: " + stroke.uv + " | Brush Color: " + stroke.color + " | Brush Radius: " + stroke.radius + " | Eraser active: " + stroke.isErase);
        // fix das eigener user auch die striche von anderen auf seinem avatar sieht
        //if (IsOwner) return;
        ApplyStrokeLocally(stroke);
    }

    void ApplyStrokeLocally(PaintStroke s) => paintMgr.DrawStroke(s.uv, s.radius, s.color, s.hard, s.isErase);
}
