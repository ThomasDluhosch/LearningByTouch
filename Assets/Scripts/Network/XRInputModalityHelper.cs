using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRInputModalityHelper : MonoBehaviour
{
    [SerializeField] private XRInputModalityManager modalityManager;

    public XRInputModalityManager.InputMode GetRightInputMode()
    {
        var field = typeof(XRInputModalityManager).GetField("m_RightInputMode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return field != null ? (XRInputModalityManager.InputMode)field.GetValue(modalityManager)
                             : XRInputModalityManager.InputMode.None;
    }

    public XRInputModalityManager.InputMode GetLeftInputMode()
    {
        var field = typeof(XRInputModalityManager).GetField("m_LeftInputMode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return field != null ? (XRInputModalityManager.InputMode)field.GetValue(modalityManager)
                             : XRInputModalityManager.InputMode.None;
    }
}