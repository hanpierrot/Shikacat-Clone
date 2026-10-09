using Spine.Unity;
using UnityEngine;

public class CatSkinSetter : MonoBehaviour
{
    private static readonly string[] JumpAnimations = { "jump1", "jump2", "jump3" };
    private static readonly string[] IdleAnimations = { "front_idle1", "front_idle2", "front_idle3", "front_idle4" };
    
    [SerializeField] private SkeletonAnimation spine;
    [SerializeField] private bool jumpOnSpawn = true;

    private void Reset()
    {
        spine = GetComponent<SkeletonAnimation>();
    }
    
    public void SetUp(CatColor color)
    {
        if (!spine.valid) spine.Initialize(false);
        
        spine.Skeleton.SetSkin(color.ToSkinName());
        spine.Skeleton.SetSlotsToSetupPose();
        spine.AnimationState.ClearTracks();
        
        string idle = IdleAnimations[Random.Range(0, IdleAnimations.Length)];
        if (jumpOnSpawn)
        {
            spine.AnimationState.SetAnimation(0, JumpAnimations[Random.Range(0, JumpAnimations.Length)], false);
            spine.AnimationState.AddAnimation(0, idle, true, 0f);
        }
        else
        {
            spine.AnimationState.SetAnimation(0, idle, true);
        }
        
        spine.Update(0);
        spine.LateUpdate();
    }

    public void Clear()
    {
        spine.AnimationState.ClearTracks();
    }
}
