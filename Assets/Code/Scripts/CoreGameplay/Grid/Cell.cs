using DG.Tweening;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshPro valueLabel;
    
    [Header("Clue Type")]
    [SerializeField] private SpriteRenderer clueTypeIcon;
    [SerializeField] private Sprite horizontalSprite;
    [SerializeField] private Sprite verticalSprite;
    [SerializeField] private Sprite squareSprite;
    
    [Header("Lock")]
    [SerializeField] private SpriteRenderer lockIcon;
    [SerializeField] private Transform lockShackle;
    [SerializeField] private TMPro.TextMeshPro lockCountLabel;
    
    [Header("Unlock Animation")]
    [SerializeField] private float unlockDelay = 0.35f; 
    [SerializeField] private float shackleLift = 0.4f;
    [SerializeField] private float liftTime = 0.25f;
    [SerializeField] private float holdTime = 0.08f;
    [SerializeField] private float revealPunch = 0.25f;
    
    [Header("Highlight")]
    [SerializeField] private SpriteRenderer body;   // Để trống thì lấy SpriteRenderer trên chính Cell
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.4f, 1f);
    [SerializeField] private float highlightPulse = 0.5f;

    public int ClueValue { get; private set; }
    public ClueType ClueType { get; private set; }
    public bool HasClue => ClueValue > 0;
    public Room AssignedRoom { get; private set; }
    private int unlockRoomCount;
    
    private bool lockShown;
    private Sequence unlockSequence;
    private Vector3 shackleRestPosition;
    private Vector3 valueLabelRestScale;
    private bool restCached;

    private Color bodyRestColor;
    private bool bodyCached;
    private Tween highlightTween;

    private void CacheRest()
    {
        if (restCached) return;
        if (lockShackle != null) shackleRestPosition = lockShackle.localPosition;
        if (valueLabel != null) valueLabelRestScale = valueLabel.transform.localScale;
        restCached = true;
    }
    
    public void SetHighlight(bool on)
    {
        if (body == null) body = GetComponent<SpriteRenderer>();
        if (body == null) return;

        if (!bodyCached)
        {
            bodyRestColor = body.color;
            bodyCached = true;
        }

        if (highlightTween != null && highlightTween.IsActive()) highlightTween.Kill();
        highlightTween = null;
        body.color = bodyRestColor;
        if (!on) return;

        highlightTween = DOTween.To(() => 0f, v => body.color = Color.Lerp(bodyRestColor, highlightColor, v), 1f, highlightPulse)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void Init(bool hasClue, LevelData.ClueEntry clue)
    {
        CacheRest();
        KillUnlock();
        SetHighlight(false);
        lockShown = false;
        
        ClueValue = hasClue ? clue.value : 0;
        ClueType = hasClue ? clue.type : ClueType.Normal;
        unlockRoomCount = hasClue ? clue.unlockRoomCount : 0;
        AssignedRoom = null;
        
        if(clueTypeIcon != null)
        {
            Sprite icon = hasClue ? GetShapeIcon(RoomRules.GetShape(clue)) : null;
            clueTypeIcon.enabled = icon != null;
            if (icon != null) clueTypeIcon.sprite = icon;
        }
        
        RefreshLock(0, false);
    }
    
    private Sprite GetShapeIcon(ClueShape shape)
    {
        switch (shape)
        {
            case ClueShape.Horizontal: return horizontalSprite;
            case ClueShape.Vertical: return verticalSprite;
            case ClueShape.Square: return squareSprite;
            default: return null;
        }
    }

    public void RefreshLock(int roomCount, bool animate = true)
    {
        CacheRest();

        if (!HasClue)
        {
            KillUnlock();
            SetLockVisible(false);
            if (lockCountLabel != null) lockCountLabel.gameObject.SetActive(false);
            if (valueLabel != null) valueLabel.gameObject.SetActive(false);
            lockShown = false;
            return;
        }
        
        bool isLocked = RoomRules.IsLocked(ClueType, unlockRoomCount, AssignedRoom != null, roomCount);

        if (valueLabel != null)
            valueLabel.text = ClueType == ClueType.Hidden ? "?" : ClueValue.ToString();

        if (isLocked)
        {
            KillUnlock();
            ShowLocked(unlockRoomCount - roomCount);
            return;
        }

        if (unlockSequence != null && unlockSequence.IsActive()) return;
        if (lockShown && animate) PlayUnlock();
        else ShowUnlocked();
    }

    private void ShowLocked(int remaining)
    {
        lockShown = true;
        SetLockVisible(true);
        if (lockShackle != null) lockShackle.localPosition = shackleRestPosition;

        if (lockCountLabel != null)
        {
            lockCountLabel.gameObject.SetActive(true);
            lockCountLabel.text = remaining.ToString();
        }
        
        if (valueLabel != null) valueLabel.gameObject.SetActive(false);
    }
    
    private void ShowUnlocked()
    {
        lockShown = false;
        SetLockVisible(false);
        if (lockCountLabel != null) lockCountLabel.gameObject.SetActive(false);
        if (valueLabel != null) valueLabel.gameObject.SetActive(true);
    }

    private void PlayUnlock()
    {
        AudioManager.PlaySfx(Sfx.UnlockObstacle);
        lockShown = false;
        if (lockCountLabel != null) lockCountLabel.gameObject.SetActive(false);
        
        if (lockShackle == null)
        {
            ShowUnlocked();
            return;
        }
        
        unlockSequence = DOTween.Sequence().SetTarget(transform);
        unlockSequence.AppendInterval(unlockDelay);
        unlockSequence.Append(lockShackle.DOLocalMoveY(shackleRestPosition.y + shackleLift, liftTime).SetEase(Ease.OutBack));
        unlockSequence.AppendInterval(holdTime);
        unlockSequence.AppendCallback(() =>
        {
            SetLockVisible(false);
            lockShackle.localPosition = shackleRestPosition;
            if (valueLabel != null) valueLabel.gameObject.SetActive(true);
        });
        
        if (valueLabel != null)
            unlockSequence.Append(valueLabel.transform.DOPunchScale(valueLabelRestScale * revealPunch, 0.2f, 6, 0.6f));

        unlockSequence.OnKill(() =>
        {
            if (valueLabel != null) valueLabel.transform.localScale = valueLabelRestScale;
            unlockSequence = null;
        });
    }
    
    private void SetLockVisible(bool visible)
    {
        lockIcon.enabled = true;
        
        if (lockIcon != null) lockIcon.gameObject.SetActive(visible);
    }

    private void KillUnlock()
    {
        if (unlockSequence != null && unlockSequence.IsActive())
            unlockSequence.Kill();
    }

    private void OnDisable()
    {
        KillUnlock();
        SetHighlight(false);
    }


    public void SetAssignedRoom(Room room)
    {
        AssignedRoom = room;
    }
}
