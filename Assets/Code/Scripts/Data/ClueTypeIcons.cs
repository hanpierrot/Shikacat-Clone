using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ClueTypeIcons", menuName = "Shikacat/Clue Type Icons")]
public class ClueTypeIcons : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public ClueType type;
        public Sprite icon;
    }
    
    [SerializeField] private Entry[] entries;

    public Sprite Get(ClueType type)
    {
        foreach (var e in entries)
        {
            if(e.type == type)
                return e.icon;
        }
        return null;
    }
}
