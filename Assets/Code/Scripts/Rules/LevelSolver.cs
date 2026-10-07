using System.Collections.Generic;

public static class LevelSolver
{
    public sealed class Result
    {
        public string Error; 
        public bool LockScheduleFeasible = true;
        public int SolutionCount;
        public bool NodeLimitReached;
        public LevelData.SolutionRoom[] FirstSolution;
        public readonly List<LevelData.SolutionRoom[]> Solutions = new List<LevelData.SolutionRoom[]>();

        public bool IsSolvable => SolutionCount > 0;
        public bool IsUnique => SolutionCount == 1 && !NodeLimitReached;
    }

    private struct RoomRect
    {
        public int Row, Col, Width, Height;
    }
    
    private static readonly IRoomBounds[] NoRooms = new IRoomBounds[0];

    public static Result Solve(LevelData level, int maxSolutions = 2, int maxNodes = 2000000)
    {
        var result = new Result();
        
        level.Prepare();
        if (!level.ValidateStructure(out string error))
        {
            result.Error = error;
            return result;
        }
        
        result.LockScheduleFeasible = IsLockScheduleFeasible(level);
        if(!result.LockScheduleFeasible)
            return result;
        
        int clueCount = level.clues.Length;
        var candidates = new List<RoomRect>[clueCount];
        for (int i = 0; i < clueCount; i++)
        {
            candidates[i] = BuildCandidates(level, level.clues[i]);
            if (candidates[i].Count == 0) return result;
        }
        
        var occupied = new bool[level.rows, level.columns];
        var chosen = new RoomRect[clueCount];
        var assigned = new bool[clueCount];
        int nodes = 0;
        bool stop = false;

        void Search(int remaining)
        {
            if (stop) return;

            if (remaining == 0)
            {
                result.SolutionCount++;
                LevelData.SolutionRoom[] found = ToSolution(chosen);
                result.Solutions.Add(found);
                if (result.FirstSolution == null) result.FirstSolution = found;
                if (result.SolutionCount >= maxSolutions) stop = true;
                return;
            }

            if (++nodes > maxNodes)
            {
                result.NodeLimitReached = true;
                stop = true;
                return;
            }

            int best = -1;
            int bestCount = int.MaxValue;
            for (int i = 0; i < clueCount; i++)
            {
                if(assigned[i]) continue;

                int count = 0;
                foreach (RoomRect rect in candidates[i])
                {
                    if(!Fits(rect, occupied)) continue;
                    count++;
                    if(count >= bestCount) break;
                }

                if (count == 0) return;
                if (count < bestCount)
                {
                    bestCount = count;
                    best = i;
                }
            }
            
            assigned[best] = true;
            foreach(RoomRect rect in candidates[best])
            {
                if(!Fits(rect, occupied)) continue;
                
                Mark(rect, occupied, true);
                chosen[best] = rect;
                Search(remaining - 1);
                Mark(rect, occupied, false);
                
                if(stop) break;
            }
            assigned[best] = false;
        }
        
        Search(clueCount);
        return result;
    }
    
    public static bool IsLockScheduleFeasible(LevelData level)
    {
        var thresholds = new List<int>();
        foreach(LevelData.ClueEntry clue in level.clues)
            if(clue.type == ClueType.Locked)
                thresholds.Add(clue.unlockRoomCount);
        
        thresholds.Sort();
        int nonLocked = level.clues.Length - thresholds.Count;
        
        for(int k = 0; k < thresholds.Count; k++)
            if(nonLocked + k < thresholds[k])
                return false;
        
        return true;
    }
    
    private static List<RoomRect> BuildCandidates(LevelData level, LevelData.ClueEntry clue)
    {
        var list = new List<RoomRect>();

        for (int height = 1; height <= clue.value; height++)
        {
            if(clue.value % height != 0) continue;
            int width = clue.value / height;

            for (int top = clue.row - height + 1; top <= clue.row; top++)
            {
                for (int left = clue.col - width + 1; left <= clue.col; left++)
                {
                    RoomCheckResult check = RoomRules.Check(
                    level, NoRooms, top, top + height - 1, left, left + width - 1, out _);
                    
                    if(check == RoomCheckResult.Valid || check == RoomCheckResult.Locked)
                        list.Add(new RoomRect {Row = top, Col = left, Width = width, Height = height});
                }
            }
        }

        return list;
    }

    private static LevelData.SolutionRoom[] ToSolution(RoomRect[] chosen)
    {
        var solution = new LevelData.SolutionRoom[chosen.Length];
        for (int i = 0; i < chosen.Length; i++)
        {
            solution[i] = new LevelData.SolutionRoom
            {
                row = chosen[i].Row,
                col = chosen[i].Col,
                width = chosen[i].Width,
                height = chosen[i].Height,
                clueIndex = i
            };
        }
        
        return solution;
    }
    
    private static bool Fits(RoomRect rect, bool[,] occupied)
    {
        for (int r = rect.Row; r < rect.Row + rect.Height; r++)
            for (int c = rect.Col; c < rect.Col + rect.Width; c++)
                if (occupied[r, c]) 
                    return false;
        
        return true;
    }
    
    private static void Mark(RoomRect rect, bool[,] occupied, bool value)
    {
        for (int r = rect.Row; r < rect.Row + rect.Height; r++)
            for (int c = rect.Col; c < rect.Col + rect.Width; c++)
                occupied[r, c] = value;
    }
}
