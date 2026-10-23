using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private RoomBuildController buildController;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private TutorialPanel panel;
    [SerializeField] private GameObject[] hideWhileActive;   // HUD, BoosterBar
    [SerializeField] private float stepPause = 0.9f;

    private TutorialLevels.Step[] steps;
    private Action onFinished;
    private Coroutine routine;
    private bool running;
    private bool stepDone;
    private int stepIndex;

    private readonly List<Vector3> handFrom = new List<Vector3>();
    private readonly List<Vector3> handTo = new List<Vector3>();

    public bool IsRunning => running;

    public void Begin(Action finished)
    {
        if (running) return;

        onFinished = finished;
        steps = TutorialLevels.Create();
        running = true;

        GameManager.Instance.IsTutorial = true;
        foreach (GameObject go in hideWhileActive)
            if (go != null) go.SetActive(false);

        panel.gameObject.SetActive(true);
        panel.SkipClicked += Skip;
        GridManager.Instance.RoomCommitted += HandleRoomCommitted;
        buildController.CellPressed += HandleCellPressed;

        routine = StartCoroutine(Run());
    }

    public void Skip()
    {
        if (running) Finish();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private IEnumerator Run()
    {
        for (stepIndex = 0; stepIndex < steps.Length; stepIndex++)
        {
            TutorialLevels.Step step = steps[stepIndex];

            panel.SetStep(stepIndex, step.Text);
            GridManager.Instance.BuildGrid(step.Level);
            yield return null;

            ShowHand(step);

            stepDone = false;
            while (!stepDone) yield return null;

            panel.StopHand();
            yield return new WaitForSeconds(stepPause);
        }

        Finish();
    }

    // Bàn tay chỉ lần lượt qua từng phòng đáp án (ô đầu -> ô cuối), 1-2 lần rồi tắt hẳn
    private void ShowHand(TutorialLevels.Step step)
    {
        GridManager grid = GridManager.Instance;
        handFrom.Clear();
        handTo.Clear();

        foreach (LevelData.SolutionRoom sol in step.Level.solution)
        {
            handFrom.Add(grid.GridToWorld(sol.row, sol.col));
            handTo.Add(grid.GridToWorld(sol.row + sol.height - 1, sol.col + sol.width - 1));
        }

        panel.PlayHand(handFrom, handTo, worldCamera, step.HandRepeat);
    }

    private void HandleRoomCommitted(Room room)
    {
        if (!running) return;

        // Mọi bước đều cần lưới kín và mọi phòng đúng số ô
        if (GridManager.Instance.IsGridFull() && GridManager.Instance.AreAllRoomsCorrect())
            stepDone = true;
    }

    private void HandleCellPressed()
    {
        if (running) panel.StopHand();   // Người chơi bắt đầu thao tác thì bàn tay tắt hẳn
    }

    private void Finish()
    {
        running = false;
        if (routine != null) StopCoroutine(routine);
        routine = null;

        Unsubscribe();
        panel.StopHand();
        panel.gameObject.SetActive(false);
        foreach (GameObject go in hideWhileActive)
            if (go != null) go.SetActive(true);

        PlayerProgress.SetTutorialDone();
        GameManager.Instance.IsTutorial = false;
        onFinished?.Invoke();
    }

    private void Unsubscribe()
    {
        if (panel != null) panel.SkipClicked -= Skip;
        if (GridManager.Instance != null) GridManager.Instance.RoomCommitted -= HandleRoomCommitted;
        if (buildController != null) buildController.CellPressed -= HandleCellPressed;
    }
}
