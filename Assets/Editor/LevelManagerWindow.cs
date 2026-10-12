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
    
    private enum Brush { Select, Hole, Room, Clue, Erase }
    private enum Modifier { None, Locked, Hidden }
    
    private sealed class RoomDraft
    {
        public LevelData.SolutionRoom rect;
        public LevelData.ClueEntry clue;
    }
    
    private Brush brush = Brush.Clue;
    private Modifier brushModifier = Modifier.None;
    private ClueShape brushShape = ClueShape.None;
    private ClueType brushType = ClueType.Normal;
    private int brushUnlockCount = 1;
    private (int row, int col) lastPaintCell = (-1, -1);
    private bool draggingRoom; 
    private (int row, int col) dragStart, dragCurrent;
    private LevelData.SolutionRoom? pendingRoom;
    private int pendingLevelIndex = -1;
    private string statusMessage;
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
        
        if (pendingLevelIndex != index)
        {
            pendingLevelIndex = index;
            pendingRoom = null;
            draggingRoom = false;
            statusMessage = null;
        }
        
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

        showSolution = EditorGUILayout.ToggleLeft("Show room outlines", showSolution);
        DrawGrid(level);
        EditorGUILayout.LabelField("Room brush: drag a rectangle, then click a cell inside to place the clue. Clue text: value + H/V/S, ? (hidden), L<n> (locked)", EditorStyles.miniLabel); 

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
        int[,] owner = BuildOwnerMap(level);

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
                if (owner[r, c] >= 0) EditorGUI.DrawRect(rect, RoomTint(owner[r, c]));

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
                DrawRoomRect(area, cell, room, Color.black, 0f);
        }

        if (pendingRoom.HasValue) DrawRoomRect(area, cell, pendingRoom.Value, new Color(0.25f, 0.55f, 1f), 0.35f);
        if (draggingRoom) DrawRoomRect(area, cell, DragRect(), new Color(0.3f, 0.8f, 0.4f), 0.35f);

        HandleGridInput(level, area, cell);
    }
    
    private static void DrawRoomRect(Rect area, float cell, LevelData.SolutionRoom room, Color color, float fillAlpha)
    {
        var rect = new Rect(area.x + room.col * cell, area.y + room.row * cell,
            room.width * cell - 2f, room.height * cell - 2f);

        if (fillAlpha > 0f)
            EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, fillAlpha));
        DrawOutline(rect, 2f, color);
    }

    private static int[,] BuildOwnerMap(LevelData level)
    {
        var owner = new int[level.rows, level.columns];
        for (int r = 0; r < level.rows; r++)
        for (int c = 0; c < level.columns; c++)
            owner[r, c] = -1;

        for (int i = 0; i < level.solution.Length; i++)
        {
            LevelData.SolutionRoom room = level.solution[i];
            for (int r = room.row; r < room.row + room.height && r < level.rows; r++)
            for (int c = room.col; c < room.col + room.width && c < level.columns; c++)
                owner[r, c] = i;
        }
        return owner;
    }

    private static Color RoomTint(int index)
        => Color.HSVToRGB((index * 0.137f) % 1f, 0.25f, 0.95f);

    private LevelData.SolutionRoom DragRect()
    {
        int minRow = Mathf.Min(dragStart.row, dragCurrent.row);
        int minCol = Mathf.Min(dragStart.col, dragCurrent.col);
        return new LevelData.SolutionRoom
        {
            row = minRow,
            col = minCol,
            height = Mathf.Abs(dragStart.row - dragCurrent.row) + 1,
            width = Mathf.Abs(dragStart.col - dragCurrent.col) + 1
        };
    }
    
    private static string ClueText(LevelData.ClueEntry clue)
    {
        string text = clue.value.ToString();

        switch (RoomRules.GetShape(clue))
        {
            case ClueShape.Horizontal: text += "H"; break;
            case ClueShape.Vertical: text += "V"; break;
            case ClueShape.Square: text += "S"; break;
        }

        if (clue.type == ClueType.Hidden) text += "?";
        else if (clue.type == ClueType.Locked) text += "L" + clue.unlockRoomCount;

        return text;
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

        if (EditorGUI.EndChangeCheck() && (newRows != level.rows || newCols != level.columns))
        {
            bool consistent = level.solution.Length == level.clues.Length;
            List<RoomDraft> rooms = consistent ? ReadRooms(level) : null;

            level.rows = newRows;
            level.columns = newCols;
            level.holes = Array.FindAll(level.holes, h => h.row < newRows && h.col < newCols);

            if (consistent)
            {
                rooms.RemoveAll(r => r.rect.row + r.rect.height > newRows || r.rect.col + r.rect.width > newCols);   // Bỏ phòng nằm ngoài lưới mới
                WriteRooms(level, rooms);
            }
            else
            {
                level.clues = Array.FindAll(level.clues, c => c.row < newRows && c.col < newCols);
                level.solution = Array.Empty<LevelData.SolutionRoom>();
            }

            pendingRoom = null;
            OnLevelEdited(level);
        }
    }

    private void DrawBrushPanel()
    {
        brush = (Brush)GUILayout.Toolbar((int)brush, new[] { "Select", "Hole", "Room", "Clue", "Erase" });

        if (brush == Brush.Room || brush == Brush.Clue)
        {
            brushModifier = (Modifier)EditorGUILayout.EnumPopup("Modifier", brushModifier);
            brushShape = (ClueShape)EditorGUILayout.EnumPopup("Shape", brushShape);
            if (brushModifier == Modifier.Locked)
                brushUnlockCount = Mathf.Max(1, EditorGUILayout.IntField("Rooms to unlock", brushUnlockCount));
        }

        if (!string.IsNullOrEmpty(statusMessage))
            EditorGUILayout.HelpBox(statusMessage, MessageType.Warning);
    }
    
    private void HandleGridInput(LevelData level, Rect area, float cell)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseUp)
        {
            lastPaintCell = (-1, -1);
            if (draggingRoom) 
            {
                FinishRoomDrag(level);
                e.Use();
            }
            return;
        }

        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
        if (e.button != 0) return;

        bool inside = area.Contains(e.mousePosition);
        if (!inside && !draggingRoom) return;

        int col = Mathf.Clamp((int)Mathf.Floor((e.mousePosition.x - area.x) / cell), 0, level.columns - 1); 
        int row = Mathf.Clamp((int)Mathf.Floor((e.mousePosition.y - area.y) / cell), 0, level.rows - 1);

        if (e.type == EventType.MouseDrag)
        {
            if (draggingRoom)
            {
                dragCurrent = (row, col);
                Repaint();
                e.Use();
                return;
            }

            if (brush != Brush.Hole && brush != Brush.Erase) return;
        }

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
                foreach (RoomDraft draft in ReadRooms(level))
                {
                    if (!Contains(draft.rect, row, col)) continue;
                    LoadBrushFromClue(draft.clue);
                    brush = Brush.Clue;
                    break;
                }
                return;

            case Brush.Hole:
                if (level.IsHole(row, col)) RemoveHole(level, row, col);
                else
                {
                    RemoveRoomAt(level, row, col);
                    var holes = new List<LevelData.CellCoord>(level.holes)
                        { new LevelData.CellCoord { row = row, col = col } };
                    level.holes = holes.ToArray();
                }
                ClearPendingAt(row, col);
                break;

            case Brush.Room:
                HandleRoomPress(level, row, col);
                return;

            case Brush.Clue:
                MoveClue(level, row, col);
                return;

            case Brush.Erase:
                if (level.IsHole(row, col)) RemoveHole(level, row, col);
                else RemoveRoomAt(level, row, col);
                ClearPendingAt(row, col);
                break;
        }

        OnLevelEdited(level);
    }
    
    private void HandleRoomPress(LevelData level, int row, int col)
    {
        if (!EnsureRoomsAuthored(level)) return;
        statusMessage = null;

        if (pendingRoom.HasValue && Contains(pendingRoom.Value, row, col))
        {
            PlaceRoom(level, pendingRoom.Value, row, col);
            return;
        }

        pendingRoom = null;
        draggingRoom = true;
        dragStart = dragCurrent = (row, col);
        Repaint();
    }

    private void FinishRoomDrag(LevelData level)
    {
        draggingRoom = false;
        LevelData.SolutionRoom rect = DragRect();

        if (RectHasHole(level, rect))
        {
            statusMessage = "A room cannot cover a hole.";
            Repaint();
            return;
        }

        if (rect.width * rect.height == 1) 
        {
            PlaceRoom(level, rect, rect.row, rect.col);
            return;
        }

        pendingRoom = rect;
        Repaint();
    }
    
    private void PlaceRoom(LevelData level, LevelData.SolutionRoom rect, int clueRow, int clueCol)
    {
        LevelData.ClueEntry clue = BuildClue(clueRow, clueCol, rect.width * rect.height);
        if (!RoomRules.MatchesShape(RoomRules.GetShape(clue), rect.width, rect.height))
        {
            statusMessage = $"Shape {RoomRules.GetShape(clue)} does not fit a {rect.width}x{rect.height} room."; 
            Repaint();
            return;
        }

        List<RoomDraft> rooms = ReadRooms(level);
        rooms.RemoveAll(r => Overlaps(r.rect, rect));
        rooms.Add(new RoomDraft { rect = rect, clue = clue });
        WriteRooms(level, rooms);

        pendingRoom = null;
        statusMessage = null;
        OnLevelEdited(level);
    }
    
    private void MoveClue(LevelData level, int row, int col)
    {
        if (!EnsureRoomsAuthored(level)) return;

        List<RoomDraft> rooms = ReadRooms(level);
        RoomDraft target = rooms.Find(r => Contains(r.rect, row, col));
        if (target == null)
        {
            statusMessage = "Click a cell inside an existing room.";
            Repaint();
            return;
        }

        LevelData.ClueEntry clue = BuildClue(row, col, target.rect.width * target.rect.height);
        if (!RoomRules.MatchesShape(RoomRules.GetShape(clue), target.rect.width, target.rect.height))
        {
            statusMessage = $"Shape {RoomRules.GetShape(clue)} does not fit a {target.rect.width}x{target.rect.height} room.";
            Repaint();
            return;
        }

        target.clue = clue;
        statusMessage = null;
        WriteRooms(level, rooms);
        OnLevelEdited(level);
    }
    
    private LevelData.ClueEntry BuildClue(int row, int col, int area)
    {
        var clue = new LevelData.ClueEntry { row = row, col = col, value = area };

        if (brushModifier == Modifier.None)
        {
            switch (brushShape)
            {
                case ClueShape.Horizontal: clue.type = ClueType.Horizontal; break;
                case ClueShape.Vertical: clue.type = ClueType.Vertical; break;
                case ClueShape.Square: clue.type = ClueType.Square; break;
                default: clue.type = ClueType.Normal; break;
            }
            return clue;
        }

        clue.type = brushModifier == Modifier.Locked ? ClueType.Locked : ClueType.Hidden;
        clue.shape = brushShape;
        if (brushModifier == Modifier.Locked) clue.unlockRoomCount = brushUnlockCount;
        return clue;
    }

    private void LoadBrushFromClue(LevelData.ClueEntry clue)
    {
        brushModifier = Modifier.None;
        brushShape = ClueShape.None;

        switch (clue.type)
        {
            case ClueType.Horizontal: brushShape = ClueShape.Horizontal; break;
            case ClueType.Vertical: brushShape = ClueShape.Vertical; break;
            case ClueType.Square: brushShape = ClueShape.Square; break;
            case ClueType.Locked:
                brushModifier = Modifier.Locked;
                brushShape = clue.shape;
                brushUnlockCount = Mathf.Max(1, clue.unlockRoomCount);
                break;
            case ClueType.Hidden:
                brushModifier = Modifier.Hidden;
                brushShape = clue.shape;
                break;
        }
    }
    
    
    
    private static void RemoveHole(LevelData level, int row, int col)
        => level.holes = System.Array.FindAll(level.holes, h => !(h.row == row && h.col == col));
    
    private static List<RoomDraft> ReadRooms(LevelData level)
    {
        var rooms = new List<RoomDraft>();
        foreach (LevelData.SolutionRoom s in level.solution)
            if (s.clueIndex >= 0 && s.clueIndex < level.clues.Length)
                rooms.Add(new RoomDraft { rect = s, clue = level.clues[s.clueIndex] });
        return rooms;
    }

    private static void WriteRooms(LevelData level, List<RoomDraft> rooms)
    {
        rooms.Sort((a, b) => a.clue.row != b.clue.row ? a.clue.row.CompareTo(b.clue.row) : a.clue.col.CompareTo(b.clue.col));

        level.clues = new LevelData.ClueEntry[rooms.Count];
        level.solution = new LevelData.SolutionRoom[rooms.Count];
        for (int i = 0; i < rooms.Count; i++)
        {
            level.clues[i] = rooms[i].clue;
            LevelData.SolutionRoom room = rooms[i].rect;
            room.clueIndex = i;
            level.solution[i] = room;
        }
    }
    
    private static void RemoveRoomAt(LevelData level, int row, int col)
    {
        if (level.solution.Length != level.clues.Length)
        {
            RemoveClue(level, row, col);
            level.solution = Array.Empty<LevelData.SolutionRoom>();
            return;
        }

        List<RoomDraft> rooms = ReadRooms(level);
        rooms.RemoveAll(r => Contains(r.rect, row, col));
        WriteRooms(level, rooms);
    }
    
    private bool EnsureRoomsAuthored(LevelData level)
    {
        if (level.solution.Length == level.clues.Length) return true;

        statusMessage = "This level has clues without matching rooms. Use Solve > Write first solution, or Clear all rooms, before editing rooms.";
        Repaint();
        return false;
    }

    private void ClearPendingAt(int row, int col)
    {
        if (pendingRoom.HasValue && Contains(pendingRoom.Value, row, col))
            pendingRoom = null;
    }

    private static bool Contains(LevelData.SolutionRoom r, int row, int col)
        => row >= r.row && row < r.row + r.height && col >= r.col && col < r.col + r.width;

    private static bool Overlaps(LevelData.SolutionRoom a, LevelData.SolutionRoom b)
        => a.row < b.row + b.height && b.row < a.row + a.height
                                    && a.col < b.col + b.width && b.col < a.col + a.width;

    private static bool RectHasHole(LevelData level, LevelData.SolutionRoom rect)
    {
        for (int r = rect.row; r < rect.row + rect.height; r++)
        for (int c = rect.col; c < rect.col + rect.width; c++)
            if (level.IsHole(r, c)) return true;
        return false;
    }
    
    private static void RemoveClue(LevelData level, int row, int col)
        => level.clues = System.Array.FindAll(level.clues, c => !(c.row == row && c.col == col));
    
    private void OnLevelEdited(LevelData level)
    {
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

        if (GUILayout.Button("Clear all rooms"))
        {
            if (EditorUtility.DisplayDialog("Clear all rooms", "Remove every room and clue of this level?", "Clear", "Cancel"))
            {
                level.clues = System.Array.Empty<LevelData.ClueEntry>();
                level.solution = System.Array.Empty<LevelData.SolutionRoom>();
                pendingRoom = null;
                OnLevelEdited(level);
            }
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
            genOptions.EnableCombo = EditorGUILayout.Toggle("Combo (shape + Locked/Hidden)", genOptions.EnableCombo);
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
                          $"dir={info.Directional} hid={info.Hidden} lock={info.Locked} combo={info.Combo} {info.Mode} " +
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
                int unique = 0, failed = 0, attempts = 0, rooms = 0, holeLevels = 0, directional = 0, hidden = 0, locked = 0, combo = 0;
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
                    combo += info.Combo;
                }

                watch.Stop();
                int ok = count - failed;
                sb.AppendLine($"[{tier.name}] levels {tier.fromLevel}..{tier.fromLevel + count - 1}: ok={ok}, failed={failed}");
                if (ok > 0)
                {
                    sb.AppendLine($"  unique {unique}/{ok} ({100f * unique / ok:0}%), attempts avg {attempts / (float)ok:0.0}, rooms avg {rooms / (float)ok:0.0}");
                    sb.AppendLine($"  levels with holes {holeLevels}, directional clues {directional}, hidden {hidden}, locked {locked}, combo {combo}, {watch.ElapsedMilliseconds / (float)count:0} ms/level");
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
