using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    [Serializable]
    public struct ClueEntry
    {
        public int row;
        public int col;
        public int value;
        public CatColor color;
        public ClueType type;
        public int unlockRoomCount;
    }

    [Serializable]
    public struct CellCoord
    {
        public int row;
        public int col;
    }

    [Serializable]
    public struct SolutionRoom
    {
        public int row;
        public int col;
        public int height;
        public int width;
        public int clueIndex;
    }

    public int rows = 8;
    public int columns = 8;
    public float timeLimit = 0f;
    public int moveLimit = 0;
    public ClueEntry[] clues = Array.Empty<ClueEntry>();
    public CellCoord[] holes = Array.Empty<CellCoord>();
    public SolutionRoom[] solution = Array.Empty<SolutionRoom>();
    
    public bool HasTimeLimit => timeLimit > 0f;
    public bool HasMoveLimit => moveLimit > 0;
    
    [NonSerialized] private bool[,] holeMask;
    [NonSerialized] private int[,] clueIndexMap;

    public int ActiveCellCount { get; private set; }
    
    public void Prepare()
    {
        holeMask = new bool[rows, columns];
        clueIndexMap = new int[rows, columns];
        for(int r = 0; r < rows; r++)
            for(int c = 0; c < columns; c++)
                clueIndexMap[r, c] = -1;
        
        foreach(var h in holes)
            if(InRange(h.row, h.col))
                holeMask[h.row, h.col] = true;
        
        for(int i = 0; i < clues.Length; i++)
            if(InRange(clues[i].row, clues[i].col))
                clueIndexMap[clues[i].row, clues[i].col] = i;

        ActiveCellCount = 0;
        for(int r = 0; r < rows; r++)
            for(int c = 0; c < columns; c++)
                if(!holeMask[r, c])
                    ActiveCellCount++;
    }
    
    public bool IsHole(int row, int col) => holeMask[row, col];

    public bool TryGetClue(int row, int col, out ClueEntry clue)
    {
        int index = clueIndexMap[row, col];
        if (index >= 0)
        {
            clue = clues[index];
            return true;
        }
        
        clue = default;
        return false;
    }
    
    public bool Validate(out string error)
    {
        if (rows < 1 || columns < 1) { error = "rows/columns phải >= 1"; return false; }

        if (timeLimit < 0f || moveLimit < 0) { error = "timeLimit/moveLimit không được âm"; return false; }
        if (!HasTimeLimit && !HasMoveLimit) { error = "timeLimit = moveLimit = 0"; return false; }
        if (HasMoveLimit && moveLimit < clues.Length) { error = $"moveLimit ({moveLimit}) < ({clues.Length})"; return false; }
        
        foreach (var h in holes)
            if (!InRange(h.row, h.col)) { error = $"hole ({h.row},{h.col}) nằm ngoài grid"; return false; }

        int clueAreaSum = 0;
        var seen = new bool[rows, columns];
        foreach (var clue in clues)
        {
            if (!InRange(clue.row, clue.col)) { error = $"clue ({clue.row},{clue.col}) nằm ngoài grid"; return false; }
            if (holeMask[clue.row, clue.col]) { error = $"clue ({clue.row},{clue.col}) nằm trên ô tắt"; return false; }
            if (seen[clue.row, clue.col]) { error = $"clue ({clue.row},{clue.col}) bị trùng vị trí"; return false; }
            if (clue.value < 1) { error = $"clue ({clue.row},{clue.col}) có value < 1"; return false; }
            if (!Enum.IsDefined(typeof(ClueType), clue.type)) { error = $"Không tồn tại TypeClue ({(int)clue.type}) tại ({clue.row},{clue.col}) "; return false; }
            if (clue.type == ClueType.Locked && (clue.unlockRoomCount < 1 || clue.unlockRoomCount > clues.Length - 1))
            {
                error = $"clue ({clue.row},{clue.col}) kiểu Locked cần 1 <= unlockRoomCount <= {clues.Length - 1}";
                return false;
            }

            if (clue.type == ClueType.Square)
            {
                int side = Mathf.RoundToInt(Mathf.Sqrt(clue.value));
                if (side * side != clue.value)
                {
                    error = $"clue ({clue.row},{clue.col}) kiểu Square cần value là số chính phương (hiện {clue.value})";
                    return false;
                }
            }
            
            seen[clue.row, clue.col] = true;
            clueAreaSum += clue.value;
        }

        if (clueAreaSum != ActiveCellCount)
        {
            error = $"Tổng value các clue ({clueAreaSum}) khác số ô hoạt động ({ActiveCellCount})";
            return false;
        }

        error = null;
        return true;
    }
    
    private bool InRange(int row, int col) => row >= 0 && row < rows && col >= 0 && col < columns;
}
