using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer clueTypeIcon;
    [SerializeField] private SpriteRenderer lockIcon;
    [SerializeField] private TMPro.TextMeshPro valueLabel;
    
    public int ClueValue { get; private set; }
    public CatColor ClueColor { get; private set; }
    public ClueType ClueType { get; private set; }
    public bool HasClue => ClueValue > 0;
    public Room AssignedRoom { get; private set; }
    private int unlockRoomCount;

    public void Init(bool hasClue, LevelData.ClueEntry clue, Sprite typeIcon)
    {
        ClueValue = hasClue ? clue.value : 0;
        ClueColor = hasClue ? clue.color : default;
        ClueType = hasClue ? clue.type : ClueType.Normal;
        unlockRoomCount = hasClue ? clue.unlockRoomCount : 0;
        AssignedRoom = null;
        
        if(clueTypeIcon != null)
        {
            bool showTypeIcon = hasClue && typeIcon != null;
            clueTypeIcon.enabled = showTypeIcon;
            if(showTypeIcon) clueTypeIcon.sprite = typeIcon;
        }
        
        if (valueLabel != null)
            valueLabel.gameObject.SetActive(hasClue);
        
        RefreshLock(0);
    }

    public void RefreshLock(int roomCount)
    {
        bool isLocked = HasClue && RoomRules.IsLocked(ClueType, unlockRoomCount, AssignedRoom != null, roomCount);

        if (lockIcon != null) lockIcon.enabled = isLocked;

        if (valueLabel == null || !HasClue) return;

        if (isLocked) valueLabel.text = (unlockRoomCount - roomCount).ToString();
        else valueLabel.text = ClueType == ClueType.Hidden ? "?" : ClueValue.ToString();
    }

    public void SetAssignedRoom(Room room)
    {
        AssignedRoom = room;
    }
}
