using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer clueTypeIcon;
    [SerializeField] private SpriteRenderer lockIcon;
    [SerializeField] private TMPro.TextMeshPro valueLabel;
    
    public int ClueValue { get; private set; }
    public CatColor ClueColor { get; private set; }
    public ClueType ClueType { get; private set; }
    public bool HasClue => ClueValue > 0;
    public Room AssignedRoom { get; private set; }
    public int UnlockRoomCount { get; private set; }
    public bool IsLocked { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }

    public void Init(int row, int col, bool hasClue, LevelData.ClueEntry clue, Sprite typeIcon)
    {
        Row = row;
        Column = col;
        ClueValue = hasClue ? clue.value : 0;
        ClueColor = hasClue ? clue.color : default;
        ClueType = hasClue ? clue.type : ClueType.Normal;
        UnlockRoomCount = hasClue ? clue.unlockRoomCount : 0;
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
        IsLocked = HasClue && RoomRules.IsLocked(ClueType, UnlockRoomCount, AssignedRoom != null, roomCount);
        
        if (lockIcon != null) lockIcon.enabled = IsLocked;

        if (valueLabel == null || !HasClue) return;

        if (IsLocked) valueLabel.text = (UnlockRoomCount - roomCount).ToString();
        else valueLabel.text = ClueType == ClueType.Hidden ? "?" : ClueValue.ToString();
    }

    public void SetAssignedRoom(Room room)
    {
        AssignedRoom = room;
    }
}
