using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class NetworkPlayer : NetworkBehaviour
{
    // XR Origin
    private GameObject XROriginGO;
    private XROrigin xrOrigin;
    private XRInputModalityHelper inputHelper;
    private XRInputModalityManager modalityManager;
    private GameObject XRHandVisualizer;
    private Transform leftXRHand, rightXRHand, leftXRController, rightXRController, mainCamera;
    private Transform leftXRFingerTip, rightXRFingerTip;

    // Avatar
    private Transform avatarHeadIK, avatarBody, avatarLeftIK, avatarRightIK;
    private Transform avatarHipPosition, avatarHeadPosition, avatarLeftFootPosition, avatarRightFootPosition;
    private Vector3 bodyOffset;
    private float avatarHipHeadDistance, avatarHeadFootDistance;
    private Transform leftAvatarHand;
    private Transform rightAvatarHand;

    [Header("References")]
    [SerializeField] private GameObject popUpCanvas;
    [SerializeField] private TMP_Text popUpText;
    [SerializeField] public PaintManager paintManager;
    [SerializeField] private MenuManager menuManager;

    [Header("Avatar Position Offsets")]
    [SerializeField] private Vector3 avatarLeftPositionOffset;
    [SerializeField] private Vector3 avatarRightPositionOffset;
    [SerializeField] private Vector3 avatarHeadPositionOffset;
    [SerializeField] float scaleCorrectionFactor = 0.9f;

    [Header("Avatar Rotation Offsets")]
    [SerializeField] private Quaternion avatarLeftRotationOffset;
    [SerializeField] private Quaternion avatarRightRotationOffset;
    [SerializeField] private Quaternion avatarHeadRotationOffset;

    [Header("Avatar Turn Settings")]
    [SerializeField] private float bodyTurnThreshold = 45f;
    [SerializeField] private float bodyTurnSpeed = 125f;

    // Network
    public string PlayerName => $"Client_{OwnerClientId}";
    private readonly NetworkVariable<bool> isPainter = new(false);


    #region Player Setup

    private void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindFirstObjectByType<XROrigin>();
        if (xrOrigin != null) Debug.Log("<color=#ffa500><b>[NetworkPlayer]</b></color> [Awake] XROrigin found!\n");
        popUpCanvas.SetActive(false);
    }


    public override void OnNetworkSpawn()
    {

        if (!IsOwner) return;

        int avatarBodyLayer = LayerMask.NameToLayer("AvatarBody");
        int localAvatarLayer = LayerMask.NameToLayer("LocalAvatar");

        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col.gameObject.layer == avatarBodyLayer)
                col.gameObject.layer = localAvatarLayer;
        }

        InitializeXRig();
        InitializeAvatar();

        paintManager.setIndexFingerTip(rightXRFingerTip, leftXRFingerTip);
        menuManager.InitializeButtons();

        StartCoroutine(ScaleAvatarToUserHeight());
    }


    private void InitializeXRig()
    {
        XROriginGO = GameObject.Find("XR Origin (XR Rig)");
        if (XROriginGO) Debug.Log("<color=#ffa500><b>[NetworkPlayer]</b></color> [InitializeXRig] Found XR Origin GO\n");
        else Debug.Log("<color=#ffa500><b>[NetworkPlayer]</b></color> [InitializeXRig] XR Origin GO not found!\n");

        inputHelper = XROriginGO.GetComponent<XRInputModalityHelper>();

        modalityManager = XROriginGO.GetComponent<XRInputModalityManager>();
        leftXRController = modalityManager.leftController.transform;
        rightXRController = modalityManager.rightController.transform;
        mainCamera = GameObject.Find("Main Camera").transform;

        XRHandVisualizer = GameObject.Find("Left Hand Interaction Visual");
        if (!XRHandVisualizer) Debug.LogError("<color=#ffa500><b>[NetworkPlayer]</b></color> [InitializeXRig] No Controller/Hands active!\n");
        else
        {
            leftXRHand = GameObject.Find("L_Wrist").transform;
            rightXRHand = GameObject.Find("R_Wrist").transform;
            Debug.Log($"<color=#ffa500><b>[NetworkPlayer]</b></color> [InitializeXRig] myXRLH: {leftXRHand} | myXRRH: {rightXRHand} \n");
        }

        leftXRFingerTip = leftXRHand.Find("L_IndexMetacarpal/L_IndexProximal/L_IndexIntermediate/L_IndexDistal/L_IndexTip");
        rightXRFingerTip = rightXRHand.Find("R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip");
    }


    private void InitializeAvatar()
    {
        avatarLeftIK = transform.Find("XR IK Rig").Find("Left Arm IK").Find("Left Arm IK_target");
        avatarRightIK = transform.Find("XR IK Rig").Find("Right Arm IK").Find("Right Arm IK_target");
        avatarHeadIK = transform.Find("XR IK Rig").Find("Head IK").Find("Head IK_target");
        avatarBody = transform;

        Animator anim = GetComponentInChildren<Animator>(true);

        avatarHipPosition = anim.GetBoneTransform(HumanBodyBones.Hips);
        avatarHeadPosition = anim.GetBoneTransform(HumanBodyBones.Head);
        avatarLeftFootPosition = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        avatarRightFootPosition = anim.GetBoneTransform(HumanBodyBones.RightFoot);

        // use XR hands so avatar hands scale = 0
        leftAvatarHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        rightAvatarHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        if (leftAvatarHand) leftAvatarHand.localScale = Vector3.zero;
        if (rightAvatarHand) rightAvatarHand.localScale = Vector3.zero;
    }


    private IEnumerator ScaleAvatarToUserHeight()
    {
        yield return new WaitForSeconds(.2f);

        if (mainCamera == null || avatarHeadIK == null) yield break;

        float lowerFoot = Mathf.Min(avatarLeftFootPosition.position.y, avatarRightFootPosition.position.y);
        avatarHipHeadDistance = avatarHeadPosition.position.y - avatarHipPosition.position.y;
        avatarHeadFootDistance = avatarHeadPosition.position.y - lowerFoot;

        float scale = mainCamera.position.y / avatarHeadFootDistance;
        scale *= scaleCorrectionFactor;

        avatarBody.localScale = Vector3.one * scale;
        bodyOffset = new Vector3(0, avatarHipHeadDistance * scale, 0);

        Debug.Log($"<color=#ffa500><b>[NetworkPlayer]</b></color> [ScaleAvatarToUserHeight]\n" +
                  $"LeftFoot: {avatarLeftFootPosition.position.y} | RightFoot: {avatarRightFootPosition} | lowerFoot: {lowerFoot} \n" +
                  $"avatarHeadPos: {avatarHeadPosition.position.y} | avatarHipPos: {avatarHipPosition.position.y} \n" +
                  $"cam: {mainCamera.position.y} / avatarHeadFootDis: {avatarHeadFootDistance} = scale: {scale} \n" +
                  $"bodyOffset: {bodyOffset}\n");
    }

    #endregion

    #region Update IK

    void Update()
    {
        if (!IsOwner || inputHelper == null || !XROriginGO || !XRHandVisualizer) return;

        UpdateHandIK(avatarLeftIK, leftXRController, leftXRHand, avatarLeftRotationOffset, inputHelper.GetLeftInputMode());
        UpdateHandIK(avatarRightIK, rightXRController, rightXRHand, avatarRightRotationOffset, inputHelper.GetRightInputMode());
        UpdateHeadIK();
        UpdateBody();
    }


    private void UpdateHandIK(Transform ikTarget, Transform motionController, Transform trackedHand, Quaternion rotationOffset, XRInputModalityManager.InputMode inputMode)
    {
        if (ikTarget == null) return;

        Transform src = inputMode == XRInputModalityManager.InputMode.TrackedHand
                    ? trackedHand
                    : motionController;

        ikTarget.localPosition = avatarBody.InverseTransformPoint(src.position);
        ikTarget.localRotation = Quaternion.Inverse(avatarBody.rotation) * src.rotation * rotationOffset;
    }


    private void UpdateHeadIK()
    {
        if (!avatarHeadIK) return;

        avatarHeadIK.localPosition = avatarBody.InverseTransformPoint(mainCamera.position + avatarHeadPositionOffset);
        avatarHeadIK.localRotation = Quaternion.Inverse(avatarBody.rotation) * mainCamera.rotation * avatarHeadRotationOffset;
    }


    private void UpdateBody()
    {
        if (!avatarBody) return;

        avatarBody.position = mainCamera.position - bodyOffset;

        Vector3 camFwd = mainCamera.forward; camFwd.y = 0;
        Vector3 bodyFwd = avatarBody.forward; bodyFwd.y = 0;

        if (camFwd != Vector3.zero)
        {
            float yawDelta = Vector3.SignedAngle(bodyFwd, camFwd, Vector3.up);

            if (Mathf.Abs(yawDelta) > bodyTurnThreshold)
            {
                Quaternion targetRot = Quaternion.LookRotation(camFwd);
                avatarBody.rotation = Quaternion.RotateTowards(avatarBody.rotation, targetRot, bodyTurnSpeed * Time.deltaTime);
            }
        }
    }

    #endregion

    #region Player Control

    void ShowPopup(string richText)
    {
        popUpText.text = richText;

        Transform cam = mainCamera.transform;
        popUpCanvas.transform.SetParent(cam, false);
        popUpCanvas.transform.localPosition = new Vector3(0f, 0f, 0.7f);
        popUpCanvas.transform.localRotation = Quaternion.identity;

        popUpCanvas.SetActive(true);
        StartCoroutine(HideAfterSeconds(5f));
    }


    [ClientRpc]
    public void TeleportLocalRigClientRpc(float teleportLocationZ, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        Vector3 newPos = xrOrigin.transform.position;
        newPos.z = teleportLocationZ;

        var cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            xrOrigin.transform.position = newPos;
            cc.enabled = true;
        }
        else
        {
            xrOrigin.transform.position = newPos;
        }
    }


    [ClientRpc]
    public void SetRoleClientRpc(bool painter, ClientRpcParams _ = default)
    {
        if (!IsOwner) return;
        if (IsHost || IsServer) return;

        isPainter.Value = painter;

        UIManager ui = FindFirstObjectByType<UIManager>();
        if (painter) ui.ShowRoleMALER();
        else ui.ShowRoleANGEMALT();

        popUpText.text = painter
            ? "Du bist <color=#ffa500>MALER</color>!"
            : "Du wirst <color=#ffa500>ANGEMALT</color>!";

        ShowPopup(popUpText.text);
        StartCoroutine(HideAfterSeconds(5f));
    }


    [ClientRpc]
    public void SetReferenceTextureClientRpc(int muscleIndex)
    {
        var data = GameManager.Instance.getMuscleDB().Muscles[muscleIndex];
        paintManager.SetReferenceTexture(data.ReferenceTexture);
    }


    [ClientRpc]
    public void ClearPaintClientRpc(ClientRpcParams _ = default)
    {
        paintManager.ClearFinalRT();
    }


    [ServerRpc]
    public void SetReferenceTextureServerRpc(int muscleIndex)
    {
        var muscle = GameManager.Instance.getMuscleDB().Muscles[muscleIndex];
        paintManager.SetReferenceTexture(muscle.ReferenceTexture);
    }


    IEnumerator HideAfterSeconds(float sec)
    {
        yield return new WaitForSeconds(sec);
        popUpCanvas.SetActive(false);
    }

    #endregion

}
