using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer catIcon;
    [SerializeField] private TMPro.TextMeshPro valueLabel;
    
    public int ClueValue { get; private set; }
    public CatColor ClueColor { get; private set; }
    public ClueType ClueType { get; private set; }
    public bool HasClue => ClueValue > 0;
    public Room AssignedRoom { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }

    public void Init(int row, int col, bool hasClue, LevelData.ClueEntry clue, Sprite clueSprite)
    {
        Row = row;
        Column = col;
        ClueValue = hasClue ? clue.value : 0;
        ClueColor = hasClue ? clue.color : default;
        ClueType = hasClue ? clue.type : ClueType.Normal;
        AssignedRoom = null;
        
        if(catIcon != null)
        {
            catIcon.enabled = hasClue;
            if(hasClue) catIcon.sprite = clueSprite;
        }
        if (valueLabel != null)
        {
            valueLabel.gameObject.SetActive(hasClue);
            if(hasClue) valueLabel.text = clue.value.ToString();
        }
    }

    public void SetAssignedRoom(Room room)
    {
        AssignedRoom = room;
    }
}
