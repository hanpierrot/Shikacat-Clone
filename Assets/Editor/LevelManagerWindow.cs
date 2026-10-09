using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LevelManagerWindow : EditorWindow
{
    private const string AssetPathKey = "Shikacat.LevelManager.AssetPath";
    
    private static readonly Color ClueCellColor = new Color(0.95f, 0.85f, 0.45f);
    
    private TextAsset levelsAsset;
    private LevelCollection collection;
    private string[] levelErrors = new string[0];
    private string loadError;
    private int levelNumber = 1;
    private bool dirty;
    private bool showSolution = true;
    private Vector2 detailScroll;
    private GUIStyle cellLabelStyle;
    
    private enum Brush { Select, Hole, Clue, Erase }
    
    private Brush brush = Brush.Clue;
    private int brushValue = 2;
    private ClueType brushType = ClueType.Normal;
    private int brushUnlockCount = 1;
    private (int row, int col) lastPaintCell = (-1, -1);
    private LevelSolver.Result solveResult;
    private int solvedLevelIndex = -1;
    private long solveMilliseconds;
    
    private const string OutputFolder = "Assets/Data/";
    private const string GeneratorAssetPathKey = "Shikacat.LevelManager.GeneratorConfigPath";

    private TextAsset generatorConfigAsset;
    private bool showGenerator;
    private string outputFileName = "levels.json";
    private int genFromLevel = 1;
    private int genToLevel = 10;
    private readonly GenerationOptions genOptions = new GenerationOptions();
    private int batchPerTier = 20;
    private string reportText = "";
    private Vector2 reportScroll;
    
    [MenuItem("Shikacat/Level Manager")]
    public static void Open() => GetWindow<LevelManagerWindow>("Level Manager");

    private void OnEnable()
    {
        string path = EditorPrefs.GetString(AssetPathKey, "");
        if (!string.IsNullOrEmpty(path))
            levelsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

        if (levelsAsset != null) Reload();
        
        string generatorPath = EditorPrefs.GetString(GeneratorAssetPathKey, "");
        if (!string.IsNullOrEmpty(generatorPath))
            generatorConfigAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(generatorPath);
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawGeneratorPanel();

        if (collection == null)
        {
            if (loadError != null)
                EditorGUILayout.HelpBox($"Cannot read file: {loadError}", MessageType.Error);
            else
                EditorGUILayout.HelpBox("Choose levels.json to start.", MessageType.Info);
            return;
        }

        if (collection.levels.Length == 0)
        {
            EditorGUILayout.HelpBox("File has no level.", MessageType.Info);
            if (GUILayout.Button("Add level")) AddLevel();
            return;
        }

        levelNumber = Mathf.Clamp(levelNumber, 1, collection.levels.Length);

        DrawLevelSelector();
        levelNumber = Mathf.Clamp(levelNumber, 1, collection.levels.Length);
        DrawManageButtons();
        DrawLevelDetail(levelNumber - 1);
    }
    
    // Header: Choose file, Reload, Save

    private void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            var picked = (TextAsset)EditorGUILayout.ObjectField(levelsAsset, typeof(TextAsset), false, GUILayout.Width(260));
            if (EditorGUI.EndChangeCheck() && ConfirmDiscard() && picked != null)
            {
                levelsAsset = picked;
                EditorPrefs.SetString(AssetPathKey, AssetDatabase.GetAssetPath(picked));
                Reload();
            }

            using (new EditorGUI.DisabledScope(levelsAsset == null))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)) && ConfirmDiscard())
                    Reload();
            }

            using (new EditorGUI.DisabledScope(collection == null || !dirty))
            {
                if (GUILayout.Button(dirty ? "Save *" : "Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    Save();
            }

            GUILayout.FlexibleSpace();

            if (collection != null)
            {
                int invalid = 0;
                foreach (string e in levelErrors) if (e != null) invalid++;
                GUILayout.Label($"{collection.levels.Length} level, {invalid} lỗi");
            }
        }
    }
    
    // Choose level by index + manager

    private void DrawLevelSelector()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("◄", GUILayout.Width(28))) levelNumber--;

            levelNumber = EditorGUILayout.DelayedIntField("Level", levelNumber);
            GUILayout.Label($"/ {collection.levels.Length}", GUILayout.Width(50));

            if (GUILayout.Button("►", GUILayout.Width(28))) levelNumber++;
        }
    }
    
    private void DrawManageButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add")) AddLevel();
            if (GUILayout.Button("Duplicate")) DuplicateLevel();
            if (GUILayout.Button("Delete")) DeleteLevel();
            if (GUILayout.Button("Up")) MoveLevel(-1);
            if (GUILayout.Button("Down")) MoveLevel(1);
        }
    }
    
    // Selected level details

    private void DrawLevelDetail(int index)
    {
        LevelData level = collection.levels[index];
        
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Level {index + 1}", EditorStyles.boldLabel);

        if (levelErrors[index] != null)
            EditorGUILayout.HelpBox(levelErrors[index], MessageType.Error);
        else
            EditorGUILayout.HelpBox("Valid Level.", MessageType.Info);
        
        EditorGUILayout.LabelField("Active / Inactive cells", $"{level.ActiveCellCount} / {level.holes.Length}");
        EditorGUILayout.LabelField("Clues / Rooms in Solution", $"{level.clues.Length} / {level.solution.Length}");

        EditorGUI.BeginChangeCheck();
        level.timeLimit = Mathf.Max(0f, EditorGUILayout.FloatField("Time Limit (0 = off)", level.timeLimit));
        level.moveLimit = Mathf.Max(0, EditorGUILayout.IntField("Move Limit (0 = off)", level.moveLimit));
        if (EditorGUI.EndChangeCheck())
        {
            dirty = true;
            RefreshValidation();
        }

        DrawResizeFields(level);
        
        EditorGUILayout.Space();
        DrawBrushPanel();

        showSolution = EditorGUILayout.ToggleLeft("Show solution", showSolution);
        DrawGrid(level);
        EditorGUILayout.LabelField("Clues: value + H/V/S (shape), ? (hidden), L<n> (locked)", EditorStyles.miniLabel);

        DrawSolvePanel(index, level);

        EditorGUILayout.Space();
        DrawLoadButtons(index);

        EditorGUILayout.EndScrollView();
    }
    
    private void DrawGrid(LevelData level)
    {
        if (level.rows < 1 || level.columns < 1) return;

        if (cellLabelStyle == null)
        {
            cellLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            cellLabelStyle.normal.textColor = Color.black;
        }

        float cell = Mathf.Min(56f, 420f / level.columns, 420f / level.rows);
        Rect area = GUILayoutUtility.GetRect(cell * level.columns, cell * level.rows, GUILayout.ExpandWidth(false));

        for (int r = 0; r < level.rows; r++)
        {
            for (int c = 0; c < level.columns; c++)
            {
                var rect = new Rect(area.x + c * cell, area.y + r * cell, cell - 2f, cell - 2f);

                if (level.IsHole(r, c))
                {
                    EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                    continue;
                }

                EditorGUI.DrawRect(rect, new Color(0.85f, 0.85f, 0.85f));

                if (level.TryGetClue(r, c, out LevelData.ClueEntry clue))
                {
                    EditorGUI.DrawRect(rect, ClueCellColor);
                    GUI.Label(rect, ClueText(clue), cellLabelStyle);
                }
            }
        }

        if (showSolution)
        {
            foreach (LevelData.SolutionRoom room in level.solution)
            {
                var rect = new Rect(area.x + room.col * cell, area.y + room.row * cell,
                    room.width * cell - 2f, room.height * cell - 2f);
                DrawOutline(rect, 2f, Color.black);
            }
        }

        HandleGridInput(level, area, cell); 
    }
    
    private static string ClueText(LevelData.ClueEntry clue)
    {
        switch (clue.type)
        {
            case ClueType.Horizontal: return clue.value + "H";
            case ClueType.Vertical: return clue.value + "V";
            case ClueType.Square: return clue.value + "S";
            case ClueType.Hidden: return clue.value + "?";
            case ClueType.Locked: return clue.value + "L" + clue.unlockRoomCount;
            default: return clue.value.ToString();
        }
    }
    
    private static void DrawOutline(Rect rect, float thickness, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }
    
    // Level Editor

    private void DrawResizeFields(LevelData level)
    {
        EditorGUI.BeginChangeCheck();
        
        int newRows = Mathf.Clamp(EditorGUILayout.DelayedIntField("Rows", level.rows), 1, 20);
        int newCols = Mathf.Clamp(EditorGUILayout.DelayedIntField("Columns", level.columns), 1, 20);
        
        if(EditorGUI.EndChangeCheck() && (newRows != level.rows || newCols != level.columns))
        {
            level.rows = newRows;
            level.columns = newCols;
            level.clues = Array.FindAll(level.clues, c => c.row < newRows && c.col < newCols);
            level.holes = Array.FindAll(level.holes, h => h.row < newRows && h.col < newCols);
            OnLevelEdited(level);
        }
    }

    private void DrawBrushPanel()
    {
        brush = (Brush)GUILayout.Toolbar((int)brush, new[] { "Select", "Hole", "Clue", "Erase" });
        if(brush != Brush.Clue) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            brushValue = Mathf.Max(1, EditorGUILayout.IntField("Value", brushValue));
        }
        
        brushType = (ClueType)EditorGUILayout.EnumPopup("Type", brushType);
        if (brushType == ClueType.Locked)
            brushUnlockCount = Mathf.Max(1, EditorGUILayout.IntField("Rooms to unlock", brushUnlockCount));
    }
    
    private void HandleGridInput(LevelData level, Rect area, float cell)
    {
        Event e = Event.current;
        if(e.type == EventType.MouseUp)
        {
            lastPaintCell = (-1, -1);
            return;
        }
        
        if(e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
        if (e.button != 0 || !area.Contains(e.mousePosition)) return;
        
        int col = (int)((e.mousePosition.x - area.x) / cell);
        int row = (int)((e.mousePosition.y - area.y) / cell);
        if (row < 0 || row >= level.rows || col < 0 || col >= level.columns) return;
        
        if (e.type == EventType.MouseDrag && brush != Brush.Hole && brush != Brush.Erase) return;
        if (lastPaintCell == (row, col)) return;
        lastPaintCell = (row, col);

        ApplyBrush(level, row, col);
        e.Use();
    }

    private void ApplyBrush(LevelData level, int row, int col)
    {
        switch (brush)
        {
            case Brush.Select:
                if (level.TryGetClue(row, col, out LevelData.ClueEntry picked))
                {
                    brushValue = picked.value;
                    brushType = picked.type;
                    brushUnlockCount = Mathf.Max(1, picked.unlockRoomCount);
                    brush = Brush.Clue;
                }
                return;
            
            case Brush.Hole:
                if(level.IsHole(row, col)) RemoveHole(level, row, col);
                else
                {
                    RemoveClue(level, row, col);
                    var holes = new List<LevelData.CellCoord>(level.holes)
                        { new LevelData.CellCoord { row = row, col = col } };
                    level.holes = holes.ToArray();
                }
                break;
            
            case Brush.Clue:
                RemoveHole(level, row, col);
                RemoveClue(level, row, col);
                var clues = new List<LevelData.ClueEntry>(level.clues)
                {
                    new LevelData.ClueEntry
                    {
                        row = row, col = col, value = brushValue, type = brushType,
                        unlockRoomCount = brushType == ClueType.Locked ? brushUnlockCount : 0
                    }
                };
                level.clues = clues.ToArray();
                break;
            
            case Brush.Erase:
                RemoveClue(level, row, col);
                RemoveHole(level, row, col);
                break;
        }
        
        OnLevelEdited(level);
    }
    
    private static void RemoveHole(LevelData level, int row, int col)
        => level.holes = System.Array.FindAll(level.holes, h => !(h.row == row && h.col == col));

    private static void RemoveClue(LevelData level, int row, int col)
        => level.clues = System.Array.FindAll(level.clues, c => !(c.row == row && c.col == col));
    
    private void OnLevelEdited(LevelData level)
    {
        level.solution = Array.Empty<LevelData.SolutionRoom>();
        Array.Sort(level.clues, (a, b) => a.row != b.row ? a.row.CompareTo(b.row) : a.col.CompareTo(b.col));
        solveResult = null;
        dirty = true;
        RefreshValidation();
        Repaint();
    }
    
    // Solver

    private void DrawSolvePanel(int index, LevelData level)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Solver", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Solve"))
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            solveResult = LevelSolver.Solve(level);
            watch.Stop();
            solveMilliseconds = watch.ElapsedMilliseconds;
            solvedLevelIndex = index;
        }

        if (solveResult == null || solvedLevelIndex != index) return;

        if (solveResult.Error != null)
        {
            EditorGUILayout.HelpBox($"Cannot solve: {solveResult.Error}", MessageType.Error);
            return;
        }

        if (!solveResult.LockScheduleFeasible)
            EditorGUILayout.HelpBox("Locked clues can never be unlocked (not enough rooms to reach their thresholds).", MessageType.Error);
        else if (solveResult.NodeLimitReached)
            EditorGUILayout.HelpBox($"Search stopped at the node limit ({solveMilliseconds} ms); result is incomplete.", MessageType.Warning);
        else if (!solveResult.IsSolvable)
            EditorGUILayout.HelpBox($"No solution ({solveMilliseconds} ms).", MessageType.Error);
        else if (solveResult.IsUnique)
            EditorGUILayout.HelpBox($"Exactly 1 solution ({solveMilliseconds} ms).", MessageType.Info);
        else
            EditorGUILayout.HelpBox($"2 or more solutions ({solveMilliseconds} ms). Any valid layout wins in game.", MessageType.Info);

        using (new EditorGUI.DisabledScope(solveResult.FirstSolution == null))
        {
            if (GUILayout.Button("Write first solution"))
            {
                level.solution = solveResult.FirstSolution;
                dirty = true;
                RefreshValidation();
            }
        }

        if (GUILayout.Button("Clear solution"))
        {
            level.solution = System.Array.Empty<LevelData.SolutionRoom>();
            dirty = true;
            RefreshValidation();
        }
    }
    
    // Generator
    private void DrawGeneratorPanel()
    {
        showGenerator = EditorGUILayout.Foldout(showGenerator, "Generator", true);
        if (!showGenerator) return;

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUI.BeginChangeCheck();
            generatorConfigAsset = (TextAsset)EditorGUILayout.ObjectField("Config (json)", generatorConfigAsset, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(GeneratorAssetPathKey, generatorConfigAsset != null ? AssetDatabase.GetAssetPath(generatorConfigAsset) : "");

            outputFileName = EditorGUILayout.TextField($"Output file ({OutputFolder})", outputFileName);

            using (new EditorGUILayout.HorizontalScope())
            {
                genFromLevel = Mathf.Max(1, EditorGUILayout.IntField("From level", genFromLevel));
                genToLevel = Mathf.Max(genFromLevel, EditorGUILayout.IntField("To level", genToLevel));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                genOptions.Seed = EditorGUILayout.IntField("Seed", genOptions.Seed);
                if (GUILayout.Button("Random", GUILayout.Width(70)))
                    genOptions.Seed = UnityEngine.Random.Range(0, int.MaxValue);
            }

            genOptions.EnableHoles = EditorGUILayout.Toggle("Holes", genOptions.EnableHoles);
            genOptions.EnableDirectional = EditorGUILayout.Toggle("Directional / Square", genOptions.EnableDirectional);
            genOptions.EnableHidden = EditorGUILayout.Toggle("Hidden", genOptions.EnableHidden);
            genOptions.EnableLocked = EditorGUILayout.Toggle("Locked", genOptions.EnableLocked);
            genOptions.IgnoreMechanicSchedule = EditorGUILayout.Toggle("Ignore mechanic unlock levels", genOptions.IgnoreMechanicSchedule);

            genOptions.OverrideGrid = EditorGUILayout.Toggle("Override grid size", genOptions.OverrideGrid);
            if (genOptions.OverrideGrid)
            {
                genOptions.OverrideMinGrid = Mathf.Clamp(EditorGUILayout.IntField("  Min grid", genOptions.OverrideMinGrid), 4, 12);
                genOptions.OverrideMaxGrid = Mathf.Clamp(EditorGUILayout.IntField("  Max grid", genOptions.OverrideMaxGrid), genOptions.OverrideMinGrid, 12);
            }

            genOptions.MaxAttemptsPerLevel = Mathf.Max(1, EditorGUILayout.IntField("Max attempts / level", genOptions.MaxAttemptsPerLevel));

            if (GUILayout.Button("Generate and write to file"))
                GenerateToFile();

            using (new EditorGUILayout.HorizontalScope())
            {
                batchPerTier = Mathf.Max(1, EditorGUILayout.IntField("Levels per tier", batchPerTier));
                if (GUILayout.Button("Batch report", GUILayout.Width(100)))
                    RunBatchReport();
            }

            if (!string.IsNullOrEmpty(reportText))
            {
                reportScroll = EditorGUILayout.BeginScrollView(reportScroll, GUILayout.Height(160));
                EditorGUILayout.TextArea(reportText);
                EditorGUILayout.EndScrollView();
            }
        }
    }
    
    private bool TryLoadGeneratorConfig(out GeneratorConfig config)
    {
        config = null;
        if (generatorConfigAsset == null)
        {
            EditorUtility.DisplayDialog("Generator", "Hãy gán file generator_config.json trước.", "OK");
            return false;
        }

        if (!GeneratorConfig.TryParse(generatorConfigAsset.text, out config, out string error))
        {
            EditorUtility.DisplayDialog("Generator", $"File cấu hình không hợp lệ: {error}", "OK");
            return false;
        }

        return true;
    }

    private void GenerateToFile()
    {
        if (!TryLoadGeneratorConfig(out GeneratorConfig config)) return;

        string fileName = outputFileName.Trim();
        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Generator", "Hãy nhập tên file đầu ra.", "OK");
            return;
        }
        if (!fileName.EndsWith(".json")) fileName += ".json";

        string assetPath = OutputFolder + fileName;
        string fullPath = Path.GetFullPath(assetPath);

        LevelCollection target;
        if (File.Exists(fullPath))
        {
            if (!LevelLoader.TryParse(File.ReadAllText(fullPath), out target, out string parseError))
            {
                EditorUtility.DisplayDialog("Generator", $"Không đọc được file đích: {parseError}", "OK");
                return;
            }
        }
        else
        {
            target = new LevelCollection();
        }

        int from = genFromLevel;
        int to = genToLevel;

        if (from - 1 > target.levels.Length)
        {
            EditorUtility.DisplayDialog("Generator",
                $"File {fileName} đang có {target.levels.Length} level. Khoảng sinh phải bắt đầu từ level {target.levels.Length + 1} trở xuống để không tạo khoảng trống.", "OK");
            return;
        }

        int replaced = Mathf.Max(0, Mathf.Min(to, target.levels.Length) - from + 1);
        if (replaced > 0 && !EditorUtility.DisplayDialog("Ghi đè level",
                $"Level {from}-{from + replaced - 1} trong {fileName} sẽ bị thay thế ({replaced} level). Bản sao lưu được lưu vào thư mục LevelBackups.",
                "Generate", "Huỷ"))
            return;

        bool isOpenFile = levelsAsset != null && AssetDatabase.GetAssetPath(levelsAsset) == assetPath;
        if (isOpenFile && !ConfirmDiscard()) return;

        var infos = new List<GenerationInfo>();
        List<LevelData> generated;
        try
        {
            generated = LevelGenerator.GenerateRange(config, from, to, genOptions, infos,
                (done, total) => EditorUtility.DisplayProgressBar("Generating levels", $"Level {from + done - 1}/{to}", done / (float)total));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (generated.Contains(null))
        {
            EditorUtility.DisplayDialog("Generator", "Có level sinh thất bại, chưa ghi gì vào file. Thử đổi seed hoặc tăng Max attempts.", "OK");
            return;
        }

        var merged = new List<LevelData>(target.levels);
        for (int i = 0; i < generated.Count; i++)
        {
            int index = from - 1 + i;
            if (index < merged.Count) merged[index] = generated[i];
            else merged.Add(generated[i]);
        }
        target.levels = merged.ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        if (File.Exists(fullPath))
        {
            string backupDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "LevelBackups");
            Directory.CreateDirectory(backupDir);
            string backupName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            File.Copy(fullPath, Path.Combine(backupDir, backupName), true);
        }

        File.WriteAllText(fullPath, LevelLoader.ToJson(target));
        AssetDatabase.ImportAsset(assetPath);

        reportText = BuildSummary(infos);

        if (isOpenFile || levelsAsset == null)
        {
            levelsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            EditorPrefs.SetString(AssetPathKey, assetPath);
            Reload();
        }

        GUIUtility.ExitGUI();
    }

    private static string BuildSummary(List<GenerationInfo> infos)
    {
        var sb = new StringBuilder();
        int unique = 0;
        int warned = 0;

        foreach (GenerationInfo info in infos)
        {
            if (info == null) continue;
            if (info.Unique) unique++;
            if (info.Warning != null) warned++;

            sb.AppendLine($"#{info.LevelNumber} {info.TierName} {info.Grid}x{info.Grid} rooms={info.Rooms} holes={info.HoleRooms} " +
                          $"dir={info.Directional} hid={info.Hidden} lock={info.Locked} {info.Mode} " +
                          $"{(info.Unique ? "unique" : "multi")} attempts={info.Attempts}{(info.Warning != null ? " !" : "")}");
        }

        sb.Insert(0, $"Generated {infos.Count} levels: {unique} unique, {warned} warnings\n");
        return sb.ToString();
    }

    private void RunBatchReport()
    {
        if (!TryLoadGeneratorConfig(out GeneratorConfig config)) return;

        var sb = new StringBuilder();
        try
        {
            for (int t = 0; t < config.tiers.Length; t++)
            {
                GeneratorConfig.Tier tier = config.tiers[t];
                int count = batchPerTier;
                int unique = 0, failed = 0, attempts = 0, rooms = 0, holeLevels = 0, directional = 0, hidden = 0, locked = 0;
                var watch = System.Diagnostics.Stopwatch.StartNew();

                for (int i = 0; i < count; i++)
                {
                    EditorUtility.DisplayProgressBar("Batch report", $"{tier.name} {i + 1}/{count}",
                        (t + (i + 1f) / count) / config.tiers.Length);

                    LevelData level = LevelGenerator.Generate(config, tier.fromLevel + i, genOptions, out GenerationInfo info);
                    if (level == null || info == null)
                    {
                        failed++;
                        continue;
                    }

                    if (info.Unique) unique++;
                    attempts += info.Attempts;
                    rooms += info.Rooms;
                    if (info.HoleRooms > 0) holeLevels++;
                    directional += info.Directional;
                    hidden += info.Hidden;
                    locked += info.Locked;
                }

                watch.Stop();
                int ok = count - failed;
                sb.AppendLine($"[{tier.name}] levels {tier.fromLevel}..{tier.fromLevel + count - 1}: ok={ok}, failed={failed}");
                if (ok > 0)
                {
                    sb.AppendLine($"  unique {unique}/{ok} ({100f * unique / ok:0}%), attempts avg {attempts / (float)ok:0.0}, rooms avg {rooms / (float)ok:0.0}");
                    sb.AppendLine($"  levels with holes {holeLevels}, directional clues {directional}, hidden {hidden}, locked {locked}, {watch.ElapsedMilliseconds / (float)count:0} ms/level");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        reportText = sb.ToString();
        GUIUtility.ExitGUI();
    }
    
    // Load Level
    
    private void DrawLoadButtons(int index)
    {
        if (dirty)
            EditorGUILayout.HelpBox("Load to Scene button using showed data, \"Play from this level\" read saved file and need to save first.", MessageType.Warning);
        
        string loadLabel = Application.isPlaying ? "Load this level into game (Play Mode)" : "Build this level to Scene (Edit Mode)";
        if (GUILayout.Button(loadLabel))
            LoadIntoScene(index);

        using (new EditorGUI.DisabledScope(dirty || Application.isPlaying))
        {
            if (GUILayout.Button("Play from this level (Play Mode)"))
            {
                EditorPrefs.SetInt(GameManager.EditorStartLevelKey, index);
                EditorApplication.EnterPlaymode();
            }
        }
    }
    
    private void LoadIntoScene(int index)
    {
        if (Application.isPlaying && GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevelAt(index);
            return;
        }

        LevelData level = collection.levels[index];
        if (level.rows < 1 || level.columns < 1)
        {
            EditorUtility.DisplayDialog("Cannot build", "rows/columns must >= 1.", "OK");
            return;
        }

        var grid = FindFirstObjectByType<GridManager>();
        if (grid == null)
        {
            EditorUtility.DisplayDialog("Cannot build", "GridManager not found.", "OK");
            return;
        }

        level.Prepare();
        grid.BuildGrid(level);
        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
    }

    private void Reload()
    {
        if (!LevelLoader.TryParse(levelsAsset.text, out LevelCollection parsed, out string error))
        {
            collection = null;
            loadError = error;
            dirty = false;
            return;
        }

        loadError = null;
        collection = parsed;
        levelNumber = 1;
        dirty = false;
        RefreshValidation();
    }

    private void Save()
    {
        string path = AssetDatabase.GetAssetPath(levelsAsset);
        File.WriteAllText(Path.GetFullPath(path), LevelLoader.ToJson(collection));
        AssetDatabase.ImportAsset(path);
        dirty = false;
    }

    private void RefreshValidation()
    {
        levelErrors = new string[collection.levels.Length];
        for (int i = 0; i < collection.levels.Length; i++)
        {
            collection.levels[i].Prepare();
            levelErrors[i] = collection.levels[i].Validate(out string error) ? null : error;
        }
    }

    private void AddLevel()
    {
        var level = new LevelData { rows = 4, columns = 4, timeLimit = 60f };
        level.Prepare();
        InsertLevel(collection.levels.Length, level);
    }

    private void DuplicateLevel()
    {
        var copy = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(collection.levels[levelNumber - 1]));
        copy.Prepare();
        InsertLevel(levelNumber, copy);
    }
    
    private void DeleteLevel()
    {
        if (!EditorUtility.DisplayDialog("Delete level", $"Delete level {levelNumber}?", "Delete", "Cancel")) return;

        var list = new List<LevelData>(collection.levels);
        list.RemoveAt(levelNumber - 1);
        collection.levels = list.ToArray();
        levelNumber = Mathf.Clamp(levelNumber, 1, Mathf.Max(1, collection.levels.Length));
        MarkChanged();
    }

    private void MoveLevel(int direction)
    {
        int from = levelNumber - 1;
        int to = from + direction;
        if (to < 0 || to >= collection.levels.Length) return;

        (collection.levels[from], collection.levels[to]) = (collection.levels[to], collection.levels[from]);
        levelNumber = to + 1;
        MarkChanged();
    }

    private void InsertLevel(int index, LevelData level)
    {
        var list = new List<LevelData>(collection.levels);
        list.Insert(index, level);
        collection.levels = list.ToArray();
        levelNumber = index + 1;
        MarkChanged();
    }

    private void MarkChanged()
    {
        dirty = true;
        RefreshValidation();
        GUIUtility.ExitGUI();
    }
    
    private bool ConfirmDiscard() => !dirty || EditorUtility.DisplayDialog("Unsaved changes", "Discard changes?", "Confirm", "Cancel");
}
