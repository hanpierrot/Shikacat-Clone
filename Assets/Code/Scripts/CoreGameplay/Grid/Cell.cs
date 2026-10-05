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
    public bool HasClue => ClueValue > 0;
    public Room AssignedRoom { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }

    public void Init(int row, int col, int clueValue, CatColor clueColor, Sprite clueSprite)
    {
        Row = row;
        Column = col;
        ClueValue = clueValue;
        ClueColor = clueColor;
        AssignedRoom = null;
        
        bool hasClue = clueValue > 0;
        if(catIcon != null)
        {
            catIcon.enabled = hasClue;
            if(hasClue) catIcon.sprite = clueSprite;
        }
        if (valueLabel != null)
        {
            valueLabel.gameObject.SetActive(hasClue);
            if(hasClue) valueLabel.text = clueValue.ToString();
        }
    }

    public void SetAssignedRoom(Room room)
    {
        AssignedRoom = room;
    }
}
