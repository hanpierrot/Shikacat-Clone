using Spine.Unity;
using UnityEngine;

public class WinAnimController : MonoBehaviour
{
    [SerializeField] SkeletonGraphic wellDone;
    [SerializeField] SkeletonGraphic[] cats;   // Yellow, Pink, Aqua
    [SerializeField] SkeletonGraphic coin;

    public void Play()
    {
        gameObject.SetActive(true);
        // open (1 lần) rồi chuyển sang idle (lặp)
        wellDone.AnimationState.SetAnimation(0, "levelpass_victory_open", false);
        wellDone.AnimationState.AddAnimation(0, "levelpass_victory_idle", true, 0f);

        foreach (var c in cats)
            c.AnimationState.SetAnimation(0, "animation", true);

        coin.AnimationState.SetAnimation(0, "rotation", true);
    }
}
