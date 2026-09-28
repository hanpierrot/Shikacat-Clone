using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Shikacat/Grid Config")]
public class GridConfig : ScriptableObject
{
    [Serializable]
    public struct ClueEntry
    {
        public int row;
        public int col;
        [Min(1)] public int value;
    }
    
    [Min(1)] public int rows = 8;
    [Min(1)] public int columns = 8;
    public float cellSize = 1f;
    
    public ClueEntry[] clues = Array.Empty<ClueEntry>();

    public bool TryGetClue(int row, int col, out int value)
    {
        foreach (var clue in clues)
        {
            if (clue.row == row && clue.col == col)
            {
                value = clue.value;
                return true;
            }
        }
        
        value = 0;
        return false;
    }
}
