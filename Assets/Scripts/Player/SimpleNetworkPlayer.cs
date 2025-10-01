using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class SimpleNetworkPlayer : NetworkBehaviour
{
    GameObject myXRRig;
    XRInputModalityManager HaCM;
    Transform leftHand, rightHand, leftController, rightController, myXRCam;
    Transform avatarHead, avatarBody, avatarLeft, avatarRight;

    [SerializeField] private Vector3 avatarLeftPositionOffset, avatarRightPositionOffset;
    [SerializeField] private Quaternion avatarLeftRotationOffset, avatarRightRotationOffset;
    [SerializeField] private Vector3 avatarHeadPositionOffset;
    [SerializeField] private Quaternion avatarHeadRotationOffset;

    private XRInputModalityHelper modalityHelper;

    public override void OnNetworkSpawn()
    {
        var myID = transform.GetComponent<NetworkObject>().NetworkObjectId;
        if (IsOwnedByServer) transform.name = "Host: " + myID;
        else transform.name = "Client: " + myID;

        if (!IsOwner) return;

        myXRRig = GameObject.Find("XR Origin (XR Rig)");
        if (myXRRig) Debug.Log("Found XR Origin");
        else Debug.Log("XR Origin not found!");

        modalityHelper = myXRRig.GetComponent<XRInputModalityHelper>();

        // pointers to xr rig
        HaCM = myXRRig.GetComponent<XRInputModalityManager>();
        leftHand = HaCM.leftHand.transform.Find("Poke Interactor");
        rightHand = HaCM.rightHand.transform.Find("Poke Interactor");
        leftController = HaCM.leftController.transform;
        rightController = HaCM.rightController.transform;
        myXRCam = GameObject.Find("Main Camera").transform;

        // pointers to avatar
        avatarBody = transform.Find("Body");
        avatarHead = transform.Find("Head");
        avatarLeft = transform.Find("L_Arm");
        avatarRight = transform.Find("R_Arm");
    }


    void Update()
    {
        if (!IsOwner || modalityHelper == null) return;
        if (!myXRRig) return;


        switch (modalityHelper.GetLeftInputMode())
        {
            case XRInputModalityManager.InputMode.MotionController:
                if (avatarLeft)
                {
                    avatarLeft.position = leftController.position + avatarLeftPositionOffset;
                    avatarLeft.rotation = leftController.rotation * avatarLeftRotationOffset;
                }
                break;

            case XRInputModalityManager.InputMode.TrackedHand:
                if (avatarLeft)
                {
                    avatarLeft.position = leftHand.position + avatarLeftPositionOffset;
                    avatarLeft.rotation = leftHand.rotation * avatarLeftRotationOffset;
                }
                break;

            case XRInputModalityManager.InputMode.None:
                break;
        }


        switch (modalityHelper.GetRightInputMode())
        {
            case XRInputModalityManager.InputMode.MotionController:
                if (avatarRight)
                {
                    avatarRight.position = rightController.position + avatarLeftPositionOffset;
                    avatarRight.rotation = rightController.rotation * avatarLeftRotationOffset;
                }
                break;

            case XRInputModalityManager.InputMode.TrackedHand:
                if (avatarRight)
                {
                    avatarRight.position = rightHand.position + avatarLeftPositionOffset;
                    avatarRight.rotation = rightHand.rotation * avatarLeftRotationOffset;
                }
                break;

            case XRInputModalityManager.InputMode.None:
                break;
        }

        if (avatarHead)
        {
            avatarHead.position = myXRCam.position + avatarHeadPositionOffset;
            avatarHead.rotation = myXRCam.rotation * avatarHeadRotationOffset;
        }

        if (avatarBody)
        {
            avatarBody.position = avatarHead.position + new Vector3(0, -0.5f, 0);
        }
    }
}