using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(NetworkObject))]
public class SubmitButton : NetworkBehaviour
{
    private bool _pressed;

    public void OnSelectEntered(SelectEnterEventArgs _)
    {
        if (_pressed) return;
        _pressed = true;

        Debug.Log("[SubmitButton] Button pressed.");
        RequestSubmitServerRpc();
        Invoke(nameof(ResetDebounce), 2f);
    }

    private void ResetDebounce() => _pressed = false;


    [ServerRpc(RequireOwnership = false)]
    private void RequestSubmitServerRpc()
    {
        GameManager.Instance.ProcessSubmit();
    }
}
