using System.Collections.Generic;
using UnityEngine;

public static class TutorialLevels
{
    public struct Step
    {
        public LevelData Level;
        public string Text;
        public int HandRepeat;   // Số lần bàn tay chỉ qua toàn bộ các phòng đáp án trước khi tắt hẳn
    }

    public static Step[] Create() => new[]
    {
        new Step
        {
            Level = Build(new[] { new[] { 1, 1, 3 } }, new[] { new[] { 1, 0, 3, 1 } }),
            Text = "First, drag to create a rectangle with 3 cells.",
            HandRepeat = 2
        },
        new Step
        {
            Level = Build(new[] { new[] { 1, 1, 3 } }, new[] { new[] { 0, 1, 1, 3 } }),
            Text = "Try another 3-cell rectangle in a different direction.",
            HandRepeat = 2
        },
        new Step
        {
            Level = Build(new[] { new[] { 0, 0, 3 }, new[] { 0, 1, 6 } },
                          new[] { new[] { 0, 0, 1, 3 }, new[] { 0, 1, 2, 3 } }),
            Text = "Each number tells you how many cells its rectangle should cover.",
            HandRepeat = 1
        },
        new Step
        {
            Level = Build(new[] { new[] { 0, 0, 2 }, new[] { 1, 2, 4 }, new[] { 2, 1, 3 } },
                          new[] { new[] { 0, 0, 1, 2 }, new[] { 0, 1, 2, 2 }, new[] { 2, 0, 3, 1 } }),
            Text = "Now, draw a rectangle around every number using the correct number of cells.",
            HandRepeat = 1
        }
    };

    // clues: { row, col, value }. solution: { row, col, width, height }, cùng thứ tự với clues.
    // Ô nào không thuộc phòng đáp án nào sẽ thành ô tắt (hole).
    private static LevelData Build(int[][] clues, int[][] solution)
    {
        var level = new LevelData
        {
            rows = 3,
            columns = 3,
            timeLimit = 0f,
            moveLimit = 0,
            clues = new LevelData.ClueEntry[clues.Length],
            solution = new LevelData.SolutionRoom[solution.Length]
        };

        var covered = new bool[3, 3];
        for (int i = 0; i < clues.Length; i++)
        {
            level.clues[i] = new LevelData.ClueEntry
            {
                row = clues[i][0], col = clues[i][1], value = clues[i][2],
                type = ClueType.Normal, shape = ClueShape.None, unlockRoomCount = 0
            };

            int[] s = solution[i];
            level.solution[i] = new LevelData.SolutionRoom
            {
                row = s[0], col = s[1], width = s[2], height = s[3], clueIndex = i
            };

            for (int r = s[0]; r < s[0] + s[3]; r++)
                for (int c = s[1]; c < s[1] + s[2]; c++)
                    covered[r, c] = true;
        }

        var holes = new List<LevelData.CellCoord>();
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (!covered[r, c]) holes.Add(new LevelData.CellCoord { row = r, col = c });

        level.holes = holes.ToArray();
        level.Prepare();
        return level;
    }
}
