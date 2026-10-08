using System;
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
        public int width;
        public int height;
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
        holeMask = new bool[Mathf.Max(rows, 0), Mathf.Max(columns, 0)];
        clueIndexMap = new int[Mathf.Max(rows, 0), Mathf.Max(columns, 0)];
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
    
    public bool ValidateStructure(out string error)
    {
        if (rows < 1 || columns < 1) { error = "rows/columns phải >= 1"; return false; }

        if (timeLimit < 0f || moveLimit < 0) { error = "timeLimit/moveLimit không được âm"; return false; }

        if (!HasTimeLimit && !HasMoveLimit)
        {
            error = "Cần ít nhất một trong timeLimit hoặc moveLimit lớn hơn 0 (hiện cả hai đều bằng 0)"; 
            return false;
        }

        if (HasMoveLimit && moveLimit < clues.Length)
        {
            error = $"moveLimit ({moveLimit}) ít hơn số phòng tối thiểu cần dựng ({clues.Length}, mỗi clue một phòng)"; 
            return false;
        }
        
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
            if (!Enum.IsDefined(typeof(ClueType), clue.type)) { error = $"clue ({clue.row},{clue.col}) có type không tồn tại ({(int)clue.type})"; return false; }
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

    private bool ValidateSolution(out string error)
    {
        error = null;
        if (solution.Length == 0) return true;

        if (solution.Length != clues.Length)
        {
            error = $"solution có {solution.Length} phòng nhưng level có {clues.Length} clue (mỗi clue đúng một phòng)";
            return false;
        }

        var occupied = new bool[rows, columns];
        for(int i = 0; i < solution.Length; i++)
        {
            SolutionRoom room = solution[i];
            int maxRow = room.row + room.height - 1;
            int maxCol = room.col + room.width - 1;

            if (room.width < 1 || room.height < 1 || !InRange(room.row, room.col) || !InRange(maxRow, maxCol))
            {
                error = $"solution[{i}] có kích thước < 1 hoặc nằm ngoài grid";
                return false;
            }
            
            if (room.clueIndex < 0 || room.clueIndex >= clues.Length)
            {
                error = $"solution[{i}] có clueIndex {room.clueIndex} không tồn tại";
                return false;
            }
            
            ClueEntry expected = clues[room.clueIndex];
            int clueCount = 0;
            bool containsExpected = false;
            
            for(int r = room.row; r <= maxRow; r++)
            {
                for (int c = room.col; c <= maxCol; c++)
                {
                    if (holeMask[r, c]) { error = $"solution[{i}] phủ ô tắt ({r},{c})"; return false; }
                    if (occupied[r, c]) { error = $"solution[{i}] chồng lấn tại ô ({r},{c})"; return false; }
                    
                    occupied[r, c] = true;

                    if (!TryGetClue(r, c, out ClueEntry found)) continue;

                    clueCount++;
                    if (found.row == expected.row && found.col == expected.col) 
                        containsExpected = true;
                }
            }
            
            if (clueCount != 1 || !containsExpected)
            {
                error = $"solution[{i}] phải chứa đúng một clue và đó là clueIndex {room.clueIndex}";
                return false;
            }

            if (room.width * room.height != expected.value)
            {
                error = $"solution[{i}] có diện tích {room.width * room.height} khác giá trị clue ({expected.value})";
                return false;
            }

            if (!RoomRules.MatchesShape(expected.type, room.width, room.height))
            {
                error = $"solution[{i}] sai hình dạng so với loại clue {expected.type}";
                return false;
            }
        }
        
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (!holeMask[r, c] && !occupied[r, c])
                {
                    error = $"solution chưa phủ ô ({r},{c})";
                    return false;
                }
            }
        }

        return true;
    }

    public bool Validate(out string error)
    {
        if (!ValidateStructure(out error)) return false;
        return ValidateSolution(out error);
    }
    
    private bool InRange(int row, int col) => row >= 0 && row < rows && col >= 0 && col < columns;
}
