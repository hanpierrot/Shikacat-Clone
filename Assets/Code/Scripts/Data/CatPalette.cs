using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatPalette", menuName = "Shikacat/Cat Palette")]
public class CatPalette : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public CatColor color;
        public Sprite panelBoxSprite;
        public Sprite panelBoxWallSprite;
    }
    
    [SerializeField] private Entry[] entries;

    public bool TryGet(CatColor color, out Entry entry)
    {
        foreach (var e in entries)
        {
            if (e.color == color)
            {
                entry = e;
                return true;
            }
        }
        
        entry = default;
        return false;
    }
    
    public void GetColors(List<CatColor> buffer)
    {
        buffer.Clear();
        foreach (var e in entries)
            if (Enum.IsDefined(typeof(CatColor), e.color))  
                buffer.Add(e.color);
    }
}
