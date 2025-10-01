using Unity.Netcode;
using UnityEngine;

public class HintAvatar : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer avatarRenderer;
    [SerializeField] private Animator animator;
    public NetworkVariable<int> MuscleIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> pose = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        var data = GameManager.Instance.getMuscleDB().Muscles[MuscleIndex.Value];
        var mat = avatarRenderer.material;
        mat.SetTexture("_FinalTex", data.ReferenceTexture);
        ApplyPose((Pose)pose.Value);
    }

    private void ApplyPose(Pose id)
    {
        string state = id switch
        {
            Pose.Bauch => "Pose_Bauch",
            Pose.Beine => "Pose_Beine",
            Pose.Bizeps => "Pose_Bizeps",
            Pose.Brachioradialis => "Pose_Brachioradialis",
            Pose.Butterfly => "Pose_Butterfly",
            Pose.Deadlift => "Pose_Deadlift",
            Pose.Neutral => "Pose_Neutral",
            Pose.Rudern => "Pose_Rudern",
            Pose.Schultern => "Pose_Schultern",
            Pose.Trizeps => "Pose_Trizeps",
            _ => "Pose_Neutral"
        };
        animator.Play(state, 0, 0f);
    }
}
