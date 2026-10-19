using Spine.Unity;
using UnityEngine;

public class LoseAnimController : MonoBehaviour
{
    [SerializeField] SkeletonGraphic levelFail;   // thua do hết lượt
    [SerializeField] SkeletonGraphic timeOut;     // thua do hết giờ

    public void Play(bool byTimeOut)
    {
        gameObject.SetActive(true);

        // Chỉ hiện đúng một hiệu ứng
        levelFail.gameObject.SetActive(!byTimeOut);
        timeOut.gameObject.SetActive(byTimeOut);

        if (byTimeOut)
            timeOut.AnimationState.SetAnimation(0, "boom_fail", true);
        else
            levelFail.AnimationState.SetAnimation(0, "animation", true);
    }
}
