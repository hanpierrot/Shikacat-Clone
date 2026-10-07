using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.WSA;
using Random = System.Random;

public sealed class GenerationOptions
{
    public int Seed = 12345;
    public bool EnableHoles = true;
    public bool EnableDirectional = true;
    public bool EnableHidden = true;
    public bool EnableLocked = true;
    public bool OverrideGrid;
    public int OverrideMinGrid = 6;
    public int OverrideMaxGrid = 8;
    public int MaxAttemptsPerLevel = 60;
    public int SolverNodeLimit = 200000;
    public bool IgnoreMechanicSchedule;
}

public sealed class GenerationInfo
{
    public int LevelNumber;
    public string TierName;
    public LimitMode Mode;
    public int Attempts;
    public bool Unique;
    public int Rooms;
    public int HoleRooms;
    public int Directional;
    public int Hidden;
    public int Locked;
    public int Grid;
    public string Warning;
}

public static class LevelGenerator
{
    private sealed class RoomPlan
    {
        public int Row, Col, Width, Height;
        public int ClueRow, ClueCol;
        public ClueType Type;
        public int UnlockCount;
        public CatColor Color;
    }

    private struct Placement
    {
        public int Width, Height;
        public double Weight;
    }
    
    private struct ActiveMechanics
    {
        public bool Holes, Hidden, Directional, Locked;
        public bool ForceHoles, ForceHidden, ForceDirectional;
    }

    public static List<LevelData> GenerateRange(GeneratorConfig config, int fromLevel, int toLevel,
        GenerationOptions options, List<GenerationInfo> infos, Action<int, int> onProgress = null)
    {
        var result = new List<LevelData>();
        int total = toLevel - fromLevel;
        
        for (int level = fromLevel; level <= toLevel; level++)
        {
            result.Add(Generate(config, level, options, out GenerationInfo info));
            infos?.Add(info);
            onProgress?.Invoke(level - fromLevel + 1, total);
        }
        
        return result;
    }

    public static LevelData Generate(GeneratorConfig config, int levelNumber, GenerationOptions options,
        out GenerationInfo info)
    {
        GeneratorConfig.Tier tier = config.GetTier(levelNumber);
        LimitMode mode = config.ResolveMode(levelNumber, options.Seed);
        ActiveMechanics mech = ResolveMechanics(config, options, levelNumber);
        
        int minGrid = options.OverrideGrid ? options.OverrideMinGrid : tier.minGrid;
        int maxGrid = options.OverrideGrid ? options.OverrideMaxGrid : tier.maxGrid;
        if (maxGrid < minGrid) maxGrid = minGrid;

        LevelData fallback = null;
        GenerationInfo fallbackInfo = null;

        for (int attempt = 1; attempt <= options.MaxAttemptsPerLevel; attempt++)
        {
            var rng = new Random(MixSeed(options.Seed, levelNumber, attempt));
            int grid = PickGrid(tier, minGrid, maxGrid, levelNumber, options.OverrideGrid, rng);
            
            // Create grid filled with rooms
            List<RoomPlan> plans = Tile(config, rng, grid);
            if (plans == null) continue;
            
            // Place clue to each room
            PlaceClues(config, rng, plans);
            
            // Turn some rooms into holes
            var holes = new List<LevelData.CellCoord>();
            int holeRooms = 0;
            if (mech.Holes && (mech.ForceHoles || (tier.holeChance > 0 && rng.NextDouble() < tier.holeChance)))
                holeRooms = ConvertRoomsToHoles(config, rng, plans, holes);

            if (mech.ForceHoles && holeRooms == 0) continue;
            
            // Place ClueType and Color
            AssignTypes(tier, mech, rng, plans);
            AssignColors(rng, plans, grid);
            
            // Build level, use solver, edit duplicate
            LevelData level = BuildLevel(config, tier, mode, plans, holes, grid, out List<RoomPlan> ordered);
            LevelSolver.Result solved = LevelSolver.Solve(level, 2, options.SolverNodeLimit);
            if (!solved.IsSolvable) continue;
            
            for (int repair = 0; repair < tier.repairAttempts && solved.SolutionCount >= 2; repair++)
            {
                if (!TryRepair(mech.Directional, rng, ordered, level, solved)) break;

                level = BuildLevel(config, tier, mode, plans, holes, grid, out ordered);
                solved = LevelSolver.Solve(level, 2, options.SolverNodeLimit);
                if (!solved.IsSolvable) break;
            }
            if (!solved.IsSolvable) continue;
            
            GenerationInfo current = Describe(levelNumber, tier, mode, attempt, grid, level, solved, holeRooms);
            if (solved.IsUnique || !tier.requireUnique)
            {
                info = current;
                return level;
            }

            fallback = level;
            fallbackInfo = current;
        }
        
        info = fallbackInfo;
        if(info != null)
            info.Warning = $"Không đạt lời giải duy nhất sau {options.MaxAttemptsPerLevel} lần thử";
        return fallback;
    }

    private static int PickGrid(GeneratorConfig.Tier tier, int minGrid, int maxGrid, int levelNumber, bool overridden,
        Random rng)
    {
        if (maxGrid <= minGrid) return minGrid;

        int size;
        if (overridden)
        {
            size = rng.Next(minGrid, maxGrid + 1);
        }
        else
        {
            double t = Math.Max(0.0, Math.Min(1.0, (levelNumber - tier.fromLevel) / (double)tier.rampLevels));
            size = (int)Math.Round(minGrid + t * (maxGrid - minGrid)) + rng.Next(-1, 2);
        }

        return Math.Max(minGrid, Math.Min(maxGrid, size));
    }
    
    // Grid
    private static List<RoomPlan> Tile(GeneratorConfig config, Random rng, int grid)
    {
        config.GetRoomsRange(grid, out int minRooms, out int maxRooms);
        int target = rng.Next(minRooms, maxRooms + 1);
        int band = config.GetBand(grid);

        var occupied = new bool[grid, grid];
        var rooms = new List<RoomPlan>();
        int freeCount = grid * grid;
        int budget = 6000;

        bool Fill(int startCell)
        {
            int cell = startCell;
            while (cell < grid * grid && occupied[cell / grid, cell % grid]) cell++;

            if (cell == grid * grid)
                return rooms.Count >= minRooms && rooms.Count <= maxRooms;
            
            if(--budget < 0)
                return false;

            int row = cell / grid;
            int col = cell % grid;
            double desiredArea = (double)freeCount / Math.Max(1, target - rooms.Count);
            
            List<Placement> choices = BuildPlacements(config, band, occupied, row, col, grid, desiredArea);
            while (choices.Count > 0)
            {
                int pick = PickWeighted(choices, rng);
                Placement placement = choices[pick];
                choices.RemoveAt(pick);

                Mark(occupied, row, col, placement.Width, placement.Height, true);
                freeCount -= placement.Width * placement.Height;
                rooms.Add(new RoomPlan { Row = row, Col = col, Width = placement.Width, Height = placement.Height });

                if (Fill(cell + 1)) return true;

                rooms.RemoveAt(rooms.Count - 1);
                freeCount += placement.Width * placement.Height;
                Mark(occupied, row, col, placement.Width, placement.Height, false);

                if (budget < 0) return false;
            }
            return false;
        }
        
        return Fill(0) ? rooms : null;
    }

    private static List<Placement> BuildPlacements(GeneratorConfig config, int band, bool[,] occupied,
        int row, int col, int grid, double desiredArea)
    {
        var list = new List<Placement>();

        foreach (GeneratorConfig.Shape shape in config.shapes)
        {
            if(shape.area < config.minRoomArea) continue;
            
            double weight = config.GetWeight(shape, band)
                            * Math.Exp(-1.2 * Math.Abs(Math.Log(shape.area / desiredArea)));
            
            if (shape.minSide == shape.maxSide)
            {
                AddPlacement(list, occupied, row, col, grid, shape.minSide, shape.minSide, weight);
            }
            else
            {
                AddPlacement(list, occupied, row, col, grid, shape.minSide, shape.maxSide, weight * config.verticalBias);
                AddPlacement(list, occupied, row, col, grid, shape.maxSide, shape.minSide, weight * (1.0 - config.verticalBias));
            }
        }
        
        return list;
    }
    
    private static void AddPlacement(List<Placement> list, bool[,] occupied, int row, int col, int grid,
        int width, int height, double weight)
    {
        if (row + height > grid || col + width > grid) return;

        for (int r = row; r < row + height; r++)
            for (int c = col; c < col + width; c++)
                if (occupied[r, c]) return;

        list.Add(new Placement { Width = width, Height = height, Weight = weight });
    }

    private static void Mark(bool[,] occupied, int row, int col, int width, int height, bool value)
    {
        for (int r = row; r < row + height; r++)
            for (int c = col; c < col + width; c++)
                occupied[r, c] = value;
    }
    
    private static int PickWeighted(List<Placement> list, Random rng)
    {
        double total = 0;
        foreach (Placement p in list) total += p.Weight;

        double roll = rng.NextDouble() * total;
        for (int i = 0; i < list.Count; i++)
        {
            roll -= list[i].Weight;
            if (roll <= 0) return i;
        }

        return list.Count - 1;
    }
    
    // Place clues
    private static void PlaceClues(GeneratorConfig config, Random rng, List<RoomPlan> plans)
    {
        foreach (RoomPlan room in plans)
        {
            var corners = new List<(int row, int col)>();
            var others = new List<(int row, int col)>();
            
            for (int r = room.Row; r < room.Row + room.Height; r++)
            {
                for (int c = room.Col; c < room.Col + room.Width; c++)
                {
                    bool isCorner = (r == room.Row || r == room.Row + room.Height - 1)
                                    && (c == room.Col || c == room.Col + room.Width - 1);
                    (isCorner ? corners : others).Add((r, c));
                }
            }
            
            float bias = config.GetCornerBias(Math.Min(room.Width, room.Height));
            bool useCorner = others.Count == 0 || rng.NextDouble() < bias;
            List<(int row, int col)> pool = useCorner ? corners : others;

            (int row, int col) cell = pool[rng.Next(pool.Count)];
            room.ClueRow = cell.row;
            room.ClueCol = cell.col;
        }
    }
    
    // Turn room into hole
    private static int ConvertRoomsToHoles(GeneratorConfig config, Random rng, List<RoomPlan> plans,
        List<LevelData.CellCoord> holes)
    {
        GeneratorConfig.HoleSettings settings = config.holes;

        int maxByLeft = plans.Count - settings.minRoomsLeft;
        if (maxByLeft < 1) return 0;

        int totalCells = 0;
        foreach (RoomPlan room in plans) totalCells += room.Width * room.Height;

        int wanted = Math.Min(rng.Next(1, settings.maxRooms + 1), maxByLeft);
        int areaCap = (int)Math.Floor(totalCells * settings.maxAreaFraction);
        int areaUsed = 0;
        int removed = 0;

        var order = new List<RoomPlan>(plans);
        Shuffle(order, rng);

        foreach (RoomPlan room in order)
        {
            if (removed >= wanted) break;

            int area = room.Width * room.Height;
            if (areaUsed + area > areaCap) continue;

            plans.Remove(room);
            for (int r = room.Row; r < room.Row + room.Height; r++)
            for (int c = room.Col; c < room.Col + room.Width; c++)
                holes.Add(new LevelData.CellCoord { row = r, col = c });

            areaUsed += area;
            removed++;
        }

        return removed;
    }
    
    // Place ClueType
    private static void AssignTypes(GeneratorConfig.Tier tier, ActiveMechanics mech, Random rng, List<RoomPlan> plans)
    {
        foreach (RoomPlan room in plans)
        {
            room.Type = ClueType.Normal;
            room.UnlockCount = 0;
        }

        var free = new List<RoomPlan>(plans);

        // Locked Room
        if (mech.Locked && tier.lockedMax > 0 && plans.Count >= 4)
        {
            int lockedCount = Math.Min(rng.Next(1, tier.lockedMax + 1), plans.Count - 1);
            int nonLocked = plans.Count - lockedCount;
            int cap = Math.Max(1, tier.lockedMaxUnlock);

            Shuffle(free, rng);
            for (int k = 0; k < lockedCount; k++)
            {
                free[k].Type = ClueType.Locked;
                free[k].UnlockCount = rng.Next(1, Math.Min(nonLocked + k, cap) + 1);
            }
            free.RemoveRange(0, lockedCount);
        }
        
        // Hidden Room
        if (mech.Hidden && tier.hiddenMax > 0 && free.Count > 0)
        {
            int cap = Math.Min(tier.hiddenMax, (int)Math.Ceiling(tier.hiddenFraction * plans.Count));
            int count = Math.Min(Math.Max(mech.ForceHidden ? 1 : 0, rng.Next(0, cap + 1)), free.Count);

            Shuffle(free, rng);
            for (int i = 0; i < count; i++) free[i].Type = ClueType.Hidden;
            free.RemoveRange(0, count);
        }
        
        // Shaped Room
        if (mech.Directional && tier.directionalChance > 0 || mech.ForceDirectional)
        {
            int assigned = 0;
            foreach (RoomPlan room in free)
            {
                if (rng.NextDouble() >= tier.directionalChance) continue;
                room.Type = PickDirectionalType(room, rng);
                assigned++;
            }

            // Level giới thiệu: đảm bảo có ít nhất một clue hình dạng
            if (mech.ForceDirectional && assigned == 0 && free.Count > 0)
            {
                RoomPlan room = free[rng.Next(free.Count)];
                room.Type = PickDirectionalType(room, rng);
            }
        }
    }
    
    private static ClueType PickDirectionalType(RoomPlan room, Random rng)
    {
        var allowed = new List<ClueType>();
        if (room.Width == room.Height) allowed.Add(ClueType.Square);
        if (room.Width >= room.Height) allowed.Add(ClueType.Horizontal);
        if (room.Height >= room.Width) allowed.Add(ClueType.Vertical);

        return allowed[rng.Next(allowed.Count)];
    }
    
    private static ActiveMechanics ResolveMechanics(GeneratorConfig config, GenerationOptions options, int level)
    {
        GeneratorConfig.MechanicUnlocks unlocks = config.mechanics;
        bool useSchedule = !options.IgnoreMechanicSchedule;
        bool intro = useSchedule && unlocks.introduceAtUnlock;

        return new ActiveMechanics
        {
            Holes = options.EnableHoles && (!useSchedule || level >= unlocks.holesFromLevel),
            Hidden = options.EnableHidden && (!useSchedule || level >= unlocks.hiddenFromLevel),
            Directional = options.EnableDirectional && (!useSchedule || level >= unlocks.directionalFromLevel),
            Locked = options.EnableLocked && (!useSchedule || level >= unlocks.lockedFromLevel),

            ForceHoles = intro && level == unlocks.holesFromLevel,
            ForceHidden = intro && level == unlocks.hiddenFromLevel,
            ForceDirectional = intro && level == unlocks.directionalFromLevel
        };
    }
    
    // Set room color
    private static void AssignColors(Random rng, List<RoomPlan> plans, int grid)
    {
        int colorCount = Enum.GetValues(typeof(CatColor)).Length;
        int n = plans.Count;

        var owner = new int[grid, grid];
        for (int r = 0; r < grid; r++)
            for (int c = 0; c < grid; c++)
                owner[r, c] = -1;

        for (int i = 0; i < n; i++)
            for (int r = plans[i].Row; r < plans[i].Row + plans[i].Height; r++)
                for (int c = plans[i].Col; c < plans[i].Col + plans[i].Width; c++)
                    owner[r, c] = i;

        var neighbors = new HashSet<int>[n];
        for (int i = 0; i < n; i++) neighbors[i] = new HashSet<int>();

        for (int r = 0; r < grid; r++)
        {
            for (int c = 0; c < grid; c++)
            {
                int a = owner[r, c];
                if (a < 0) continue;

                if (c + 1 < grid && owner[r, c + 1] >= 0 && owner[r, c + 1] != a)
                {
                    neighbors[a].Add(owner[r, c + 1]);
                    neighbors[owner[r, c + 1]].Add(a);
                }
                if (r + 1 < grid && owner[r + 1, c] >= 0 && owner[r + 1, c] != a)
                {
                    neighbors[a].Add(owner[r + 1, c]);
                    neighbors[owner[r + 1, c]].Add(a);
                }
            }
        }

        var order = new List<int>();
        for (int i = 0; i < n; i++) order.Add(i);
        Shuffle(order, rng);
        order.Sort((x, y) => neighbors[y].Count.CompareTo(neighbors[x].Count));

        var color = new int[n];
        for (int i = 0; i < n; i++) color[i] = -1;
        var usage = new int[colorCount];

        foreach (int i in order)
        {
            var candidates = new List<int>();
            int bestUsage = int.MaxValue;

            for (int col = 0; col < colorCount; col++)
            {
                bool taken = false;
                foreach (int neighbor in neighbors[i])
                {
                    if (color[neighbor] == col) { taken = true; break; }
                }
                if (taken) continue;

                if (usage[col] < bestUsage)
                {
                    bestUsage = usage[col];
                    candidates.Clear();
                }
                if (usage[col] == bestUsage) candidates.Add(col);
            }

            int chosen = candidates.Count > 0 ? candidates[rng.Next(candidates.Count)] : rng.Next(colorCount);
            color[i] = chosen;
            usage[chosen]++;
            plans[i].Color = (CatColor)chosen;
        }
    }
    
    // Build LevelData
    private static LevelData BuildLevel(GeneratorConfig config, GeneratorConfig.Tier tier, LimitMode mode,
        List<RoomPlan> plans, List<LevelData.CellCoord> holes, int grid, out List<RoomPlan> ordered)
    {
        ordered = new List<RoomPlan>(plans);
        ordered.Sort((a, b) => a.ClueRow != b.ClueRow ? a.ClueRow.CompareTo(b.ClueRow) : a.ClueCol.CompareTo(b.ClueCol));

        int n = ordered.Count;
        var sortedHoles = new List<LevelData.CellCoord>(holes);
        sortedHoles.Sort((a, b) => a.row != b.row ? a.row.CompareTo(b.row) : a.col.CompareTo(b.col));

        var level = new LevelData
        {
            rows = grid,
            columns = grid,
            holes = sortedHoles.ToArray(),
            clues = new LevelData.ClueEntry[n],
            solution = new LevelData.SolutionRoom[n]
        };

        for (int i = 0; i < n; i++)
        {
            RoomPlan plan = ordered[i];
            level.clues[i] = new LevelData.ClueEntry
            {
                row = plan.ClueRow,
                col = plan.ClueCol,
                value = plan.Width * plan.Height,
                color = plan.Color,
                type = plan.Type,
                unlockRoomCount = plan.Type == ClueType.Locked ? plan.UnlockCount : 0
            };
            level.solution[i] = new LevelData.SolutionRoom
            {
                row = plan.Row,
                col = plan.Col,
                width = plan.Width,
                height = plan.Height,
                clueIndex = i
            };
        }

        int moves = n + tier.moveSlack;
        float time = RoundToFive(tier.timeBase + tier.timePerRoom * n);
        GeneratorConfig.LimitSchedule schedule = config.limitSchedule;

        switch (mode)
        {
            case LimitMode.Timer:
                level.timeLimit = time;
                level.moveLimit = 0;
                break;
            case LimitMode.Moves:
                level.timeLimit = 0f;
                level.moveLimit = moves;
                break;
            default:
                level.timeLimit = RoundToFive(time * schedule.bothTimeFactor);
                level.moveLimit = moves + schedule.bothMoveExtra;
                break;
        }

        level.Prepare();
        return level;
    }
    
    private static float RoundToFive(double seconds) => (float)(Math.Max(15.0, Math.Round(seconds / 5.0) * 5.0));
    
    // Fix duplicated solution
    private static bool TryRepair(bool allowDirectional, Random rng, List<RoomPlan> ordered,
        LevelData level, LevelSolver.Result solved)
    {
        LevelData.SolutionRoom[] alternative = null;
        foreach (LevelData.SolutionRoom[] candidate in solved.Solutions)
        {
            if (!SameSolution(candidate, level.solution))
            {
                alternative = candidate;
                break;
            }
        }
        if (alternative == null) return false;

        var differing = new List<int>();
        for (int i = 0; i < alternative.Length; i++)
            if (!SameRoom(alternative[i], level.solution[i])) differing.Add(i);
        if (differing.Count == 0) return false;

        int index = differing[rng.Next(differing.Count)];
        RoomPlan plan = ordered[index];
        LevelData.SolutionRoom alt = alternative[index];

        // Cách 1: thêm ràng buộc hình dạng mà hình thật thoả còn hình thay thế thì không
        if (allowDirectional && plan.Type == ClueType.Normal)
        {
            var useful = new List<ClueType>();
            if (plan.Width == plan.Height && alt.width != alt.height) useful.Add(ClueType.Square);
            if (plan.Width >= plan.Height && alt.width < alt.height) useful.Add(ClueType.Horizontal);
            if (plan.Height >= plan.Width && alt.height < alt.width) useful.Add(ClueType.Vertical);

            if (useful.Count > 0 && rng.NextDouble() < 0.6)
            {
                plan.Type = useful[rng.Next(useful.Count)];
                return true;
            }
        }

        // Cách 2: dời clue sang ô khác trong cùng phòng
        var cells = new List<(int row, int col)>();
        for (int r = plan.Row; r < plan.Row + plan.Height; r++)
            for (int c = plan.Col; c < plan.Col + plan.Width; c++)
                if (!(r == plan.ClueRow && c == plan.ClueCol)) cells.Add((r, c));

        if (cells.Count == 0) return false;

        (int row, int col) cell = cells[rng.Next(cells.Count)];
        plan.ClueRow = cell.row;
        plan.ClueCol = cell.col;
        return true;
    }

    private static bool SameRoom(LevelData.SolutionRoom a, LevelData.SolutionRoom b)
        => a.row == b.row && a.col == b.col && a.width == b.width && a.height == b.height;

    private static bool SameSolution(LevelData.SolutionRoom[] a, LevelData.SolutionRoom[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (!SameRoom(a[i], b[i])) return false;

        return true;
    }
    
    // Utility functions
    private static GenerationInfo Describe(int levelNumber, GeneratorConfig.Tier tier, LimitMode mode, int attempt,
        int grid, LevelData level, LevelSolver.Result solved, int holeRooms)
    {
        var info = new GenerationInfo
        {
            LevelNumber = levelNumber,
            TierName = tier.name,
            Mode = mode,
            Attempts = attempt,
            Unique = solved.IsUnique,
            Rooms = level.clues.Length,
            HoleRooms = holeRooms,
            Grid = grid
        };

        foreach (LevelData.ClueEntry clue in level.clues)
        {
            switch (clue.type)
            {
                case ClueType.Horizontal:
                case ClueType.Vertical:
                case ClueType.Square: info.Directional++; break;
                case ClueType.Hidden: info.Hidden++; break;
                case ClueType.Locked: info.Locked++; break;
            }
        }

        return info;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    
    private static int MixSeed(int seed, int levelNumber, int attempt)
    {
        unchecked
        {
            int hash = seed;
            hash = hash * 486187739 + levelNumber;
            hash = hash * 486187739 + attempt;
            return hash;
        }
    }
}
