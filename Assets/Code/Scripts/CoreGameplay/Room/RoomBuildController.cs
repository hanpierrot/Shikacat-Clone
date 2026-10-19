using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(GridInputHandler))]
public class RoomBuildController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GridInputHandler inputHandler;
    [SerializeField] private RoomRemoveController roomRemoveController;
    [SerializeField] private int maxPrewarmPanels = 16;
    
    [Header("Preview Text")]
    [SerializeField] private TMPro.TMP_Text previewAreaText;
    [SerializeField, Range(0.02f, 0.3f)] private float previewTextOffset = 0.08f;
    private const float PreviewTextScreenMargin = 40f; 
    
    [Header("Preview Overlay")]
    [SerializeField] private SpriteRenderer dragPreviewOverlay;
    [SerializeField] private Sprite validPreviewSprite;
    [SerializeField] private Sprite invalidPreviewSprite;
    [SerializeField] private RoomVisual roomPanelPrefab;
    [SerializeField] private Transform roomVisualRoot;
    [SerializeField] private CatPalette catPalette;
    [SerializeField] private float referenceCellSize = 1f;
    [SerializeField] private float invalidPreviewHoldDuration = 0.25f;

    public Vector3 LastReleaseWorldPosition { get; private set; }
    
    private static bool IsInputLocked => GameManager.Instance != null && GameManager.Instance.IsGameOver;
    
    private readonly List<RoomVisual> roomPanelPool = new List<RoomVisual>();
    private readonly List<CatColor> colorBuffer = new List<CatColor>();
    private readonly List<CatColor> candidateBuffer = new List<CatColor>();
    
    public event Action PlacementFailed;
    public event Action<Room> RoomAnimationFinished; // remove if no one use
    public event Action CellPressed;

    private int shownMinRow = -1, shownMaxRow, shownMinCol, shownMaxCol;
    private Sprite shownSprite;
    
    private Vector2 pointerScreenPosition;
    private int startRow, startCol;
    private int currentMinRow, currentMaxRow, currentMinCol, currentMaxCol;
    private int lastCellRow, lastCellCol; 
    private bool isPressValid;
    private bool isResolving;
    private Coroutine prewarmRoutine;


    private void Awake()
    {
        if (inputHandler == null) inputHandler = GetComponent<GridInputHandler>();
        if (dragPreviewOverlay != null) dragPreviewOverlay.gameObject.SetActive(false);
        if (previewAreaText != null) previewAreaText.gameObject.SetActive(false);

        if (roomVisualRoot != null)
        {
            var existingPanels = roomVisualRoot.GetComponentsInChildren<RoomVisual>(true);
            roomPanelPool.AddRange(existingPanels);
            foreach (var panel in existingPanels)
                panel.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        inputHandler.PressStarted += HandlePressStarted;
        inputHandler.PressMoved += HandlePressMoved;
        inputHandler.PressEnded += HandlePressEnded;
        GridManager.Instance.RoomRemoved += ReleaseRoomPanel;
        GameManager.Instance.LevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        inputHandler.PressStarted -= HandlePressStarted;
        inputHandler.PressMoved -= HandlePressMoved;
        inputHandler.PressEnded -= HandlePressEnded;
        if (GridManager.Instance != null)
            GridManager.Instance.RoomRemoved -= ReleaseRoomPanel;
        if (GameManager.Instance != null) GameManager.Instance.LevelLoaded -= HandleLevelLoaded;

        StopAllCoroutines();
        isResolving = false;
        FinishPress();
    }

    private void HandlePressStarted(Vector2 screenPosition)
    {
        if (isResolving || IsInputLocked) return;
        
        Vector3 world = targetCamera.ScreenToWorldPoint(screenPosition);
        isPressValid = GridManager.Instance.WorldToGrid(world, out startRow, out startCol);
        if (!isPressValid) return;

        if (GridManager.Instance.IsCellInPlacingRoom(startRow, startCol)) 
        {
            isPressValid = false;
            return;
        }
        
        CellPressed?.Invoke();
        
        lastCellRow = startRow;
        lastCellCol = startCol;
        
        pointerScreenPosition = screenPosition;
        SetPreviewBounds(startRow, startRow, startCol, startCol);
    }
    
    private void HandlePressMoved(Vector2 screenPosition)
    {
        if (!isPressValid || isResolving || IsInputLocked) return;
        
        pointerScreenPosition = screenPosition;
        UpdateAreaTextPosition();

        Vector3 world = targetCamera.ScreenToWorldPoint(screenPosition);
        if (!GridManager.Instance.WorldToGrid(world, out int row, out int col)) return;
        
        lastCellRow = row;
        lastCellCol = col;

        SetPreviewBounds(
            Mathf.Min(startRow, row), Mathf.Max(startRow, row),
            Mathf.Min(startCol, col), Mathf.Max(startCol, col));
    }

    private void UpdateAreaTextPosition()
    {
        if (previewAreaText == null) return;

        Vector2 screen = pointerScreenPosition + Vector2.up * (Screen.height * previewTextOffset);
        screen.x = Mathf.Clamp(screen.x, PreviewTextScreenMargin, Screen.width - PreviewTextScreenMargin);
        screen.y = Mathf.Min(screen.y, Screen.height - PreviewTextScreenMargin);
        
        Vector3 world = targetCamera.ScreenToWorldPoint(screen);
        world.z = previewAreaText.transform.position.z;
        previewAreaText.transform.position = world;
    }

    private void HandlePressEnded(bool wasDrag)
    {
        if (!isPressValid || isResolving) return;
        
        if (IsInputLocked)
        {
            FinishPress();
            return;
        }
        
        if (!wasDrag && roomRemoveController != null && roomRemoveController.TryRemoveAt(startRow, startCol))
        {
            FinishPress();
            return;
        }

        if (!TryValidateSelection(out _))
        {
            if (!GridManager.Instance.OverlapsPlacingRoom(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol))
                PlacementFailed?.Invoke();
            
            UpdatePreviewOverlay(invalidPreviewSprite);
            isResolving = true;
            StartCoroutine(ResolveAfterDelay());
            return;
        }

        TryCommitSelection();
        FinishPress();
    }
    
    private void HandleLevelLoaded()
    {
        if (prewarmRoutine != null) StopCoroutine(prewarmRoutine);
        prewarmRoutine = StartCoroutine(PrewarmRoutine(Mathf.Min(GridManager.Instance.ClueCount, maxPrewarmPanels)));
    }
    
    private IEnumerator PrewarmRoutine(int needed)
    {
        if (roomPanelPrefab == null || catPalette == null) yield break;

        catPalette.GetColors(colorBuffer);
        if (colorBuffer.Count == 0) yield break;
        CatColor color = colorBuffer[0];

        yield return null;

        foreach (RoomVisual existing in roomPanelPool)
        {
            if (existing.IsPrewarmed || existing.gameObject.activeSelf) continue;
            existing.Prewarm(color);
            yield return null;
        }

        while (roomPanelPool.Count < needed)
        {
            RoomVisual panel = Instantiate(roomPanelPrefab, roomVisualRoot);
            panel.Prewarm(color);
            roomPanelPool.Add(panel);
            yield return null;
        }

        prewarmRoutine = null;
    }
    
    private IEnumerator ResolveAfterDelay()
    {
        yield return new WaitForSeconds(invalidPreviewHoldDuration);
        FinishPress();
        isResolving = false;
    }
    
    private void FinishPress()
    {
        if (dragPreviewOverlay != null) dragPreviewOverlay.gameObject.SetActive(false);
        if (previewAreaText != null) previewAreaText.gameObject.SetActive(false);
        ResetBounds();
        isPressValid = false;
    }
    
    private void TryCommitSelection()
    {
        if (!TryValidateSelection(out _)) return;
        
        LastReleaseWorldPosition = GridManager.Instance.GridToWorld(lastCellRow, lastCellCol);
        Room room = GridManager.Instance.CommitRoom(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol);
        SpawnRoomPanel(room);
    }

    private void SetPreviewBounds(int minRow, int maxRow, int minCol, int maxCol)
    {
        if (GridManager.Instance.ContainsHole(minRow, maxRow, minCol, maxCol))
        {
            minRow = maxRow = startRow;
            minCol = maxCol = startCol;
        }
        
        currentMinRow = minRow;
        currentMaxRow = maxRow;
        currentMinCol = minCol;
        currentMaxCol = maxCol;
        
        UpdatePreviewOverlay(validPreviewSprite);
    }

    private void UpdatePreviewOverlay(Sprite overlaySprite)
    {
        if (dragPreviewOverlay == null) return;

        bool unchanged = overlaySprite == shownSprite
                         && currentMinRow == shownMinRow && currentMaxRow == shownMaxRow
                         && currentMinCol == shownMinCol && currentMaxCol == shownMaxCol
                         && dragPreviewOverlay.gameObject.activeSelf;

        UpdateAreaTextPosition();
        
        if (unchanged) return;

        shownSprite = overlaySprite;
        shownMinRow = currentMinRow; shownMaxRow = currentMaxRow;
        shownMinCol = currentMinCol; shownMaxCol = currentMaxCol;
        
        GridManager.Instance.GetWorldRect(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol, out Vector3 center, out Vector2 size);
        
        float scaleFactor = GridManager.Instance.CellSize / referenceCellSize;
        dragPreviewOverlay.transform.position = center;
        dragPreviewOverlay.transform.localScale = Vector3.one * scaleFactor;
        dragPreviewOverlay.sprite = overlaySprite;
        dragPreviewOverlay.size = size / scaleFactor;

        if (previewAreaText != null)
        {
            int area = (currentMaxRow - currentMinRow + 1) * (currentMaxCol - currentMinCol + 1);
            previewAreaText.text = area.ToString();
            previewAreaText.gameObject.SetActive(true);
            UpdateAreaTextPosition();
        }
        
        dragPreviewOverlay.gameObject.SetActive(true);
    }

    private void SpawnRoomPanel(Room room)
    {
        if (roomPanelPrefab == null || catPalette == null)
        {
            Debug.LogWarning("RoomBuildController: thiếu roomPanelPrefab hoặc catPalette, phòng sẽ không có hình.");
            RoomAnimationFinished?.Invoke(room);
            return;
        }
        
        room.Color = PickColor(room); 
        if (!catPalette.TryGet(room.Color, out var entry))
        {
            Debug.LogWarning($"RoomBuildController: CatPalette thiếu màu {room.Color}, phòng sẽ không có hình.");
            RoomAnimationFinished?.Invoke(room);
            return;
        }
        
        RoomVisual panel = GetPooledPanel();
        GridManager.Instance.GetWorldRect(room.MinRow, room.MaxRow, room.MinColumn, room.MaxColumn, out Vector3 center, out Vector2 size);
        
        float scaleFactor = GridManager.Instance.CellSize / referenceCellSize;
        panel.SetUp(center, size, entry.panelBoxSprite, entry.panelBoxWallSprite, scaleFactor);
        panel.SetCat(null);
        panel.gameObject.SetActive(true);
        room.Visual = panel;
        room.IsPlacing = true;
        panel.PlayPlaceAnimation(() =>
        {
            room.IsPlacing = false;
            panel.SetCat(room.Color, () => RoomAnimationFinished?.Invoke(room));
        });
    }
    
    private CatColor PickColor(Room room)
    {
        catPalette.GetColors(colorBuffer);
        if (colorBuffer.Count == 0) return default;

        candidateBuffer.Clear();
        foreach (CatColor color in colorBuffer)
        {
            bool usedByNeighbor = false;
            foreach (Room other in GridManager.Instance.Rooms)
            {
                if (other == room || other.Color != color || !room.IsAdjacentTo(other)) continue;
                usedByNeighbor = true;
                break;
            }

            if (!usedByNeighbor) candidateBuffer.Add(color);
        }

        List<CatColor> pool = candidateBuffer.Count > 0 ? candidateBuffer : colorBuffer;
        return pool[Random.Range(0, pool.Count)];
    }

    private RoomVisual GetPooledPanel()
    {
        foreach (var panel in roomPanelPool)
            if (!panel.gameObject.activeSelf) return panel;
        
        RoomVisual newPanel = Instantiate(roomPanelPrefab, roomVisualRoot);
        roomPanelPool.Add(newPanel);
        return newPanel;
    }

    private void ReleaseRoomPanel(Room room)
    {
        if (room?.Visual == null) return;
        room.Visual.Release();  
        room.Visual.gameObject.SetActive(false);
        room.Visual = null;
    }
    
    private void ResetBounds()
    {
        currentMinRow = 0;
        currentMaxRow = -1;
        currentMinCol = 0;
        currentMaxCol = -1;
    }

    private bool TryValidateSelection(out LevelData.ClueEntry clue)
    {
        RoomCheckResult result = GridManager.Instance.CheckRoom(
            currentMinRow, currentMaxRow, currentMinCol, currentMaxCol, out clue);
        if (result != RoomCheckResult.Valid) return false;
        
        return !GridManager.Instance.OverlapsPlacingRoom(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol);
    }
}
