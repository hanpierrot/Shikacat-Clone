using DG.Tweening;
using Spine.Unity;
using UnityEngine;

public class WinAnimController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonGraphic wellDone;
    [SerializeField] private SkeletonGraphic[] cats;
    [SerializeField] private SkeletonGraphic coin;

    [Header("Cats")]
    [SerializeField] private RectTransform[] catRoots;
    [SerializeField] private float startOffsetY = -1300f;
    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private float stagger = 0.1f;
    [SerializeField] private float hoverHeight = 40f;
    [SerializeField] private float hoverTime = 0.35f;
    [SerializeField] private float fallDistance = 1300f;
    [SerializeField] private float fallTime = 0.3f;

    [Header("Reward")]
    [SerializeField] private GameObject shotFx;
    [SerializeField] private GameObject dropFx;
    [SerializeField] private GameObject coinRoot;
    [SerializeField] private WinStarsPanel starsPanel;
    [SerializeField] private CollectButton collectButton;

    private Sequence sequence;
    private Vector2[] restPositions;
    private bool playing;
    private bool rewardShown;
    private int pendingReward;

    private void OnEnable()
    {
        if (collectButton != null) collectButton.Clicked += HandleCollectClicked;
    }

    private void OnDisable()
    {
        if (collectButton != null) collectButton.Clicked -= HandleCollectClicked;

        Stop();
        if (shotFx != null) shotFx.SetActive(false);
        if (dropFx != null) dropFx.SetActive(false);
        if (collectButton != null) collectButton.Hide();
    }

    private void CacheRest()
    {
        if (restPositions != null) return;

        restPositions = new Vector2[catRoots.Length];
        for (int i = 0; i < catRoots.Length; i++)
            restPositions[i] = catRoots[i].anchoredPosition;
    }

    private void Update()
    {
        if (!playing) return;

        // Chạm vào màn hình thì bỏ qua animation (chạm vào Collect cũng chỉ làm nó hoàn tất sớm hơn một chút)
        if (TapStarted())
            Skip();
    }

    public void Play(int reward, int stars)
    {
        CacheRest();
        Stop();

        gameObject.SetActive(true);
        playing = true;
        rewardShown = false;
        pendingReward = reward;

        if (dropFx != null) dropFx.SetActive(false);
        if (coinRoot != null) coinRoot.SetActive(false);
        if (collectButton != null) collectButton.Hide();

        // Bảng điều khiển vừa bật: ShotFX nổ, Well-Done mở và bảng sao hiện cùng lúc
        if (shotFx != null)
        {
            shotFx.SetActive(false);
            shotFx.SetActive(true);
        }
        if (starsPanel != null) starsPanel.Play(stars);

        wellDone.gameObject.SetActive(true);
        wellDone.AnimationState.SetAnimation(0, "levelpass_victory_open", false);
        wellDone.AnimationState.AddAnimation(0, "levelpass_victory_idle", true, 0f);

        for (int i = 0; i < catRoots.Length; i++)
        {
            catRoots[i].gameObject.SetActive(true);
            SetY(catRoots[i], restPositions[i].y + startOffsetY);
            cats[i].AnimationState.SetAnimation(0, "animation", true);
        }

        sequence = DOTween.Sequence().SetTarget(transform);

        // 1. Ba mèo nhảy từ dưới lên
        for (int i = 0; i < catRoots.Length; i++)
            sequence.Insert(i * stagger, MoveY(catRoots[i], restPositions[i].y, jumpTime).SetEase(Ease.OutBack));

        sequence.AppendCallback(() =>
        {
            if (dropFx != null) dropFx.SetActive(true);
        });

        // 2. Lơ lửng ngắn
        for (int i = 0; i < catRoots.Length; i++)
        {
            Tween hover = MoveY(catRoots[i], restPositions[i].y + hoverHeight, hoverTime).SetEase(Ease.OutSine);
            if (i == 0) sequence.Append(hover); else sequence.Join(hover);
        }

        // 3. Ngay khi mèo bắt đầu rơi: hiện coin và nút Collect (chạy chồng)
        sequence.AppendCallback(ShowReward);

        for (int i = 0; i < catRoots.Length; i++)
        {
            Tween fall = MoveY(catRoots[i], restPositions[i].y - fallDistance, fallTime).SetEase(Ease.InBack);
            if (i == 0) sequence.Append(fall); else sequence.Join(fall);
        }

        sequence.AppendCallback(() =>
        {
            foreach (RectTransform root in catRoots) root.gameObject.SetActive(false);
        });

        sequence.OnComplete(() =>
        {
            sequence = null;
            playing = false;
        });
    }

    private void ShowReward()
    {
        if (rewardShown) return;
        rewardShown = true;

        if (coinRoot != null)
        {
            coinRoot.SetActive(true);
            coinRoot.transform.localScale = Vector3.zero;
            coinRoot.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetTarget(coinRoot.transform);
        }

        coin.AnimationState.SetAnimation(0, "rotation", true);

        if (collectButton != null) collectButton.Show(pendingReward);
    }

    // Hoàn tất ngay mọi animation và hiện nút Collect
    public void Skip()
    {
        if (!playing) return;
        playing = false;

        if (sequence != null && sequence.IsActive())
        {
            sequence.Complete(true);   // true: chạy cả các AppendCallback (DropFX, ShowReward, tắt mèo)
            sequence = null;
        }

        // Chốt cuối: dù callback có bị bỏ lỡ thì trạng thái cuối vẫn đúng
        if (shotFx != null && !shotFx.activeSelf) shotFx.SetActive(true);
        ShowReward();
        foreach (RectTransform root in catRoots) root.gameObject.SetActive(false);
        if (dropFx != null) dropFx.SetActive(true);

        if (starsPanel != null) starsPanel.Complete();
    }

    private void HandleCollectClicked()
    {
        // Người chơi bấm Collect khi sao còn đang sáng: cho các sao về trạng thái cuối
        if (starsPanel != null) starsPanel.Complete();
    }

    public void Stop()
    {
        playing = false;

        if (sequence != null && sequence.IsActive()) sequence.Kill();
        sequence = null;

        if (coinRoot != null) coinRoot.transform.DOKill();
        if (starsPanel != null) starsPanel.Hide();
    }

    private static bool TapStarted()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).phase == TouchPhase.Began;
        return Input.GetMouseButtonDown(0);
    }

    private static void SetY(RectTransform rect, float y)
    {
        Vector2 p = rect.anchoredPosition;
        p.y = y;
        rect.anchoredPosition = p;
    }

    private static Tweener MoveY(RectTransform rect, float targetY, float duration)
        => DOTween.To(() => rect.anchoredPosition.y, y => SetY(rect, y), targetY, duration);
}
