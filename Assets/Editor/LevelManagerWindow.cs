using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LevelManagerWindow : EditorWindow
{
    private const string AssetPathKey = "Shikacat.LevelManager.AssetPath";
    
    private static readonly Color[] CatColors =
    {
        new Color(0.30f, 0.50f, 0.95f), new Color(0.90f, 0.30f, 0.30f), new Color(0.35f, 0.75f, 0.40f),
        new Color(1.00f, 0.55f, 0.75f), new Color(0.60f, 0.40f, 0.85f), new Color(0.25f, 0.70f, 0.70f),
        new Color(0.20f, 0.80f, 1.00f), new Color(0.70f, 0.90f, 0.25f), new Color(1.00f, 0.60f, 0.20f),
        new Color(1.00f, 0.90f, 0.30f), new Color(0.65f, 0.85f, 1.00f), new Color(1.00f, 0.70f, 0.60f)
    };
    
    private TextAsset levelsAsset;
    private LevelCollection collection;
    private string[] levelErrors = new string[0];
    private string loadError;
    private int levelNumber = 1;
    private bool dirty;
    private bool showSolution = true;
    private Vector2 detailScroll;
    private GUIStyle cellLabelStyle;
    
    [MenuItem("Shikacat/Level Manager")]
    public static void Open() => GetWindow<LevelManagerWindow>("Level Manager");

    private void OnEnable()
    {
        string path = EditorPrefs.GetString(AssetPathKey, "");
        if (!string.IsNullOrEmpty(path))
            levelsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

        if (levelsAsset != null) Reload();
    }

    private void OnGUI()
    {
        DrawHeader();

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
        
        EditorGUILayout.LabelField("Size", $"{level.rows} x {level.columns}");
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

        EditorGUILayout.Space();
        showSolution = EditorGUILayout.ToggleLeft("Show solution", showSolution);
        DrawGrid(level);
        EditorGUILayout.LabelField("Clues: value + H/V/S (shape), ? (hidden), L<n> (locked)", EditorStyles.miniLabel);

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
                    EditorGUI.DrawRect(rect, CatColors[(int)clue.color % CatColors.Length]);
                    GUI.Label(rect, ClueText(clue), cellLabelStyle);
                }
            }
        }

        if (!showSolution) return;

        foreach (LevelData.SolutionRoom room in level.solution)
        {
            var rect = new Rect(area.x + room.col * cell, area.y + room.row * cell,
                room.width * cell - 2f, room.height * cell - 2f);
            DrawOutline(rect, 2f, Color.black);
        }
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
