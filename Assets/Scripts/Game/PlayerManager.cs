using Unity.Netcode;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    [SerializeField] private UIManager uiManager;
    private readonly float teleportZ = -15f;

    public NetworkPlayer painterNP { get; private set; }
    public NetworkPlayer canvasNP { get; private set; }


    public void AssignRoles(GameMode gameMode)
    {
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        if (gameMode == GameMode.SinglePlayer)
        {
            painterNP = players[0];
            canvasNP = painterNP;
            painterNP.SetRoleClientRpc(true, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { painterNP.OwnerClientId } } });
            uiManager.ShowRoleMALER();
        }
        else // gameMode == GameMode.MultiPlayer
        {
            if (players.Length < 2) return;

            if (painterNP != null && canvasNP != null)
            {
                (painterNP, canvasNP) = (canvasNP, painterNP);
            }
            else
            {
                bool flip = Random.Range(0, 2) == 0;
                painterNP = flip ? players[0] : players[1];
                canvasNP = flip ? players[1] : players[0];
            }

            painterNP.SetRoleClientRpc(true, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { painterNP.OwnerClientId } } });
            canvasNP.SetRoleClientRpc(false, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { canvasNP.OwnerClientId } } });
        }

        Debug.Log($"<b><color=#00ffcc>[PlayerManager]</color></b> Roles assigned");
    }


    public void TeleportAllPlayers()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            NetworkObject clientObject = client.Value.PlayerObject;
            if (clientObject == null) continue;

            Vector3 clientRootPos = clientObject.transform.position;
            clientObject.transform.position = new Vector3(clientRootPos.x, clientRootPos.y, teleportZ);

            var player = clientObject.GetComponent<NetworkPlayer>();
            player.TeleportLocalRigClientRpc(teleportZ,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
        }

        Debug.Log($"<b><color=#00ffcc>[PlayerManager]</color></b> Players teleported");
    }


    public void SetAllPlayerReferenceTextures(int muscleIndex, Texture2D referenceTexture)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkPlayer player = client.Value.PlayerObject.GetComponent<NetworkPlayer>();
            player.paintManager.SetReferenceTexture(referenceTexture);
            player.SetReferenceTextureClientRpc(muscleIndex);
        }

        Debug.Log($"<b><color=#00ffcc>[PlayerManager]</color></b> Set reftex of muscle {muscleIndex}");
    }


    public void ClearAllPlayerPaint()
    {
        foreach (var clients in NetworkManager.Singleton.ConnectedClients)
        {
            NetworkPlayer player = clients.Value.PlayerObject.GetComponent<NetworkPlayer>();
            player.paintManager.ClearFinalRT();
            player.ClearPaintClientRpc();

            Debug.Log($"<b><color=#00ffcc>[PlayerManager]</color></b> Client {clients.Key} paint was cleared");
        }

    }
}
