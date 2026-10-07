using NUnit.Framework;

public class LevelSolverTests
{
    private static LevelData.ClueEntry Clue(int row, int col, int value, ClueType type = ClueType.Normal, int unlock = 0)
        => new LevelData.ClueEntry { row = row, col = col, value = value, type = type, unlockRoomCount = unlock };

    private static LevelData MakeLevel(int rows, int cols, params LevelData.ClueEntry[] clues)
    {
        var level = new LevelData { rows = rows, columns = cols, timeLimit = 60f, clues = clues };
        
        level.Prepare();
        return level;
    }
    
    [Test]
    public void UniqueSolution_TwoRowLevel()
    {
        var level = MakeLevel(2, 2, Clue(0, 0, 2), Clue(1, 0, 2));
        LevelSolver.Result result = LevelSolver.Solve(level);
        Assert.AreEqual(1, result.SolutionCount);
        Assert.IsTrue(result.IsUnique);
    }

    [Test]
    public void TwoSolutions_WhenTwoLayoutsFit()
    {
        var level = MakeLevel(2, 4, Clue(0, 0, 4), Clue(1, 3, 4));
        LevelSolver.Result result = LevelSolver.Solve(level, maxSolutions: 5);
        Assert.AreEqual(2, result.SolutionCount);
        Assert.IsFalse(result.IsUnique);
    }

    [Test]
    public void Unsolvable_WhenClueFitsNoRectangle()
    {
        var level = MakeLevel(2, 2, Clue(0, 0, 3), Clue(1, 1, 1));
        Assert.IsFalse(LevelSolver.Solve(level).IsSolvable);
    }

    [Test]
    public void Solver_RespectsClueShape()
    {
        Assert.IsFalse(LevelSolver.Solve(MakeLevel(2, 3, Clue(0, 0, 6, ClueType.Vertical))).IsSolvable);
        Assert.IsTrue(LevelSolver.Solve(MakeLevel(2, 3, Clue(0, 0, 6, ClueType.Horizontal))).IsSolvable);
    }

    [Test]
    public void LockSchedule_Feasibility()
    {
        Assert.IsTrue(LevelSolver.IsLockScheduleFeasible(
            MakeLevel(2, 2, Clue(0, 0, 2), Clue(1, 0, 2, ClueType.Locked, 1))));

        Assert.IsFalse(LevelSolver.IsLockScheduleFeasible(
            MakeLevel(2, 2, Clue(0, 0, 2, ClueType.Locked, 1), Clue(1, 0, 2, ClueType.Locked, 1))));
    }

    [Test]
    public void SolverOutput_PassesSolutionValidation()
    {
        var level = MakeLevel(2, 4, Clue(0, 0, 4), Clue(1, 3, 4));
        LevelSolver.Result result = LevelSolver.Solve(level);
        Assert.IsNotNull(result.FirstSolution);

        level.solution = result.FirstSolution;
        Assert.IsTrue(level.Validate(out string error), error);
    }
}
