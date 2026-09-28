using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer catIcon;
    [SerializeField] private TMPro.TextMeshPro valueLabel;
    
    public int Row { get; private set; }
    public int Column { get; private set; }
    public int ClueValue { get; private set; }
    public bool HasClue => ClueValue > 0;
    public Room AssignedRoom { get; private set; }

    public void Init(int row, int col, int clueValue)
    {
        Row = row;
        Column = col;
        ClueValue = clueValue;
        
        bool hasClue = clueValue > 0;
        if(catIcon != null) catIcon.enabled = hasClue;
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
    
    public void SetPreviewHighlight(bool isOn, bool isValidPreview)
    {
        if (background == null) return;
        if (!isOn)
        {
            background.color = Color.white;
            return;
        }
        
        background.color = isValidPreview ? new Color(0.2f, 0.5f, 0.9f) : new Color(1f, 0.6f, 0.6f);
    }
}
