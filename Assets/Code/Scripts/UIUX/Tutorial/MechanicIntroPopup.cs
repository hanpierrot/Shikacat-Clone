using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup giải thích mechanic: tiêu đề, mô tả, hình minh họa tĩnh và nút Ok.
// Hình minh họa mô phỏng lưới ngoài thế giới: cùng sprite ô, cùng màu, khoảng cách giữa các ô = spacingRatio * cỡ ô
public class MechanicIntroPopup : MonoBehaviour
{
    [SerializeField] private RectTransform window;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descText;
    [SerializeField] private RectTransform illustration;   // Hộp minh họa (nền kem), các lưới được dựng bên trong
    [SerializeField] private Button okButton;

    [Header("Illustration assets")]
    [SerializeField] private Sprite blockSprite;           // Sprite thân ô giống Cell ngoài thế giới
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Sprite magnifierSprite;
    [SerializeField] private Font font;                    // Font cho nhãn Vertical/Square/Horizontal

    [Header("Cell label (khớp ValueLabel của Cell)")]
    [SerializeField] private TMP_FontAsset cellFont;       // Fredoka-Bold SDF, giống TextMeshPro trên Cell
    [SerializeField] private Sprite verticalSprite;        // Icon xương cá, nằm dưới số (giống ClueTypeIcon của Cell)
    [SerializeField] private Sprite horizontalSprite;
    [SerializeField] private Sprite squareSprite;
    [SerializeField, Range(0.3f, 1f)] private float iconScale = 0.8f;

    [Header("Grid look (khớp GridManager)")]
    [SerializeField] private float cellSize = 96f;
    [SerializeField, Range(0f, 1f)] private float spacingRatio = 0.1f;   // Cùng giá trị với GridManager.spacingRatio

    public event Action Closed;

    private readonly List<GameObject> spawned = new List<GameObject>();

    // Màu chữ giống ValueLabel của prefab Cell
    private static readonly Color ClueText = new Color(0.17f, 0.12f, 0.09f);
    private static readonly Color LabelBlue = new Color(0.2f, 0.28f, 0.85f);

    private float Gap => cellSize * spacingRatio;

    private void Awake()
    {
        okButton.onClick.AddListener(() => Closed?.Invoke());
    }

    public void Show(MechanicIntro.Mechanic mechanic)
    {
        gameObject.SetActive(true);
        Clear();

        switch (mechanic)
        {
            case MechanicIntro.Mechanic.Directional:
                titleText.text = "Orientation";
                descText.text = "Blocks must be vertical, square, or horizontal.";
                BuildOrientation();
                break;
            case MechanicIntro.Mechanic.Locked:
                titleText.text = "Sealed Number";
                descText.text = "Complete [X] correct divisions to reveal the number's value.";
                BuildSealed();
                break;
            default:
                titleText.text = "Mystery";
                descText.text = "Hidden number, deduce its sizes.";
                BuildMystery();
                break;
        }

        window.DOKill();
        window.localScale = Vector3.one * 0.9f;
        window.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).SetTarget(window);
    }

    public void Hide()
    {
        Clear();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        window.DOKill();
    }

    // Vertical (1x2, số 2 ở ô dưới), Square (2x2, số 4 ở ô dưới-phải), Horizontal (2x1, số 2 ở ô phải)
    private void BuildOrientation()
    {
        AddBlock("Vertical", -270f, 1, 2, new Vector2Int(0, 1), "2", verticalSprite);
        AddBlock("Square", 0f, 2, 2, new Vector2Int(1, 1), "4", squareSprite);
        AddBlock("Horizontal", 270f, 2, 1, new Vector2Int(1, 0), "2", horizontalSprite);
    }

    private void AddBlock(string label, float centerX, int cols, int rows, Vector2Int clueCell, string clue, Sprite icon)
    {
        AddText(illustration, label, new Vector2(centerX, 130f), 40, LabelBlue, new Vector2(240f, 60f));

        // Các khối cùng đáy với nhau (căn theo hàng dưới cùng, y = -100)
        float height = GridSize(rows);
        RectTransform[,] cells = AddGrid(cols, rows, new Vector2(centerX, -100f + height * 0.5f));
        RectTransform clueRect = cells[clueCell.y, clueCell.x];
        if (icon != null)
            AddImage(clueRect, icon, Color.white, Vector2.zero, new Vector2(cellSize * iconScale, cellSize * iconScale));   // Nằm dưới số
        AddCellLabel(clueRect, clue, Vector2.zero);
    }

    // Lưới 6x3: ô khóa ở (0,1), số 2 ở (1,4), số 1 ở (2,0)
    private void BuildSealed()
    {
        RectTransform[,] cells = AddGrid(6, 3, Vector2.zero);

        AddImage(cells[0, 1], lockSprite, Color.white, new Vector2(0f, cellSize * 0.04f), new Vector2(cellSize * 0.85f, cellSize * 0.85f));
        AddCellLabel(cells[0, 1], "x", new Vector2(0f, -cellSize * 0.08f), 0.35f);
        AddCellLabel(cells[1, 4], "2", Vector2.zero);
        AddCellLabel(cells[2, 0], "1", Vector2.zero);
    }

    // Lưới 6x3: dấu ? ở (1,4) và (2,0); kính lúp phóng to dấu ? ở (1,2)
    private void BuildMystery()
    {
        RectTransform[,] cells = AddGrid(6, 3, Vector2.zero);
        AddCellLabel(cells[1, 4], "?", Vector2.zero);
        AddCellLabel(cells[2, 0], "?", Vector2.zero);

        // Kính lúp đặt trực tiếp trong hộp minh họa (nằm trên mọi ô), vị trí tính từ tâm ô (1,2)
        Vector2 lensPos = CellCenter(6, 3, 1, 2, Vector2.zero);
        RectTransform lens = AddImage(illustration, magnifierSprite, Color.white, lensPos, new Vector2(150f, 150f));
        lens.pivot = new Vector2(0.4f, 0.6f);
        AddCellLabel(lens, "?", new Vector2(10f, 14f), 0.55f);
    }

    private float GridSize(int count) => count * cellSize + (count - 1) * Gap;

    private Vector2 CellCenter(int cols, int rows, int row, int col, Vector2 gridCenter)
    {
        return gridCenter + new Vector2(-GridSize(cols) * 0.5f + cellSize * 0.5f + col * (cellSize + Gap),
                                        GridSize(rows) * 0.5f - cellSize * 0.5f - row * (cellSize + Gap));
    }

    // Dựng một lưới bằng GridLayoutGroup: các ô cùng sprite, cùng màu, cách nhau đúng spacingRatio như ngoài thế giới
    private RectTransform[,] AddGrid(int cols, int rows, Vector2 center)
    {
        var container = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        var rect = (RectTransform)container.transform;
        rect.SetParent(illustration, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = new Vector2(GridSize(cols), GridSize(rows));
        spawned.Add(container);

        var layout = container.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(cellSize, cellSize);
        layout.spacing = new Vector2(Gap, Gap);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = cols;

        var cells = new RectTransform[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cell = new GameObject("Cell", typeof(RectTransform), typeof(Image));
                var cellRect = (RectTransform)cell.transform;
                cellRect.SetParent(rect, false);

                var image = cell.GetComponent<Image>();
                image.sprite = blockSprite;
                image.color = Color.white;
                image.raycastTarget = false;

                cells[r, c] = cellRect;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        return cells;
    }

    private RectTransform AddImage(RectTransform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Img", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        spawned.Add(go);
        return rect;
    }

    // Số/dấu ? trên ô: cùng font (TMP Fredoka-Bold SDF), kiểu Bold và màu với ValueLabel của Cell ngoài thế giới
    private void AddCellLabel(RectTransform parent, string content, Vector2 pos, float sizeRatio = 0.58f)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(cellSize, cellSize);

        var text = go.GetComponent<TextMeshProUGUI>();
        if (cellFont != null) text.font = cellFont;
        text.text = content;
        text.fontSize = cellSize * sizeRatio;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = ClueText;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        spawned.Add(go);
    }

    private void AddText(RectTransform parent, string content, Vector2 pos, int size, Color color, Vector2 box)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(Text));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = box;

        var text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;

        spawned.Add(go);
    }

    private void Clear()
    {
        foreach (GameObject go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
    }
}
