using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioHooks : MonoBehaviour
{
    [SerializeField] private RoomBuildController buildController;
    [SerializeField] private HintController hint;
    [SerializeField] private RevealController reveal;

    private readonly List<RaycastResult> hits = new List<RaycastResult>();

    private void Start()
    {
        GameManager gm = GameManager.Instance;
        gm.LevelLoaded += () => AudioManager.PlaySfx(Sfx.LoadLevel);
        gm.WinShown += HandleWin;
        gm.LoseShown += HandleLose;
        gm.LowTimeWarning += () => AudioManager.PlaySfx(Sfx.EndTimeWarning);

        GridManager.Instance.RoomCommitted += HandleRoomCommitted;

        BoosterManager.Instance.Used += HandleBoosterUsed;
        BoosterManager.Instance.Unlocked += _ => AudioManager.PlaySfx(Sfx.EarnReward);

        AudioManager.Instance.PlayMusic();
    }
    
    private void HandleRoomCommitted(Room room)
    {
        if (GridManager.Instance.IsRevealing) return;
        AudioManager.PlaySfx(Sfx.BlockTrue);
    }
    
    private void HandleBoosterUsed(BoosterType type)
    {
        if (type == BoosterType.Freeze) return;
        AudioManager.PlaySfx(Sfx.Booster2);
    }

    private void HandleWin()
    {
        AudioManager.PlaySfx(Sfx.Win);
        AudioManager.Instance.DuckMusic(0.35f, 0.2f, 3f);
    }

    private void HandleLose()
    {
        AudioManager.PlaySfx(Sfx.Failed);
        AudioManager.Instance.DuckMusic(0.35f, 0.2f, 2f);
    }
    
    private void Update()
    {
        if (!PressedThisFrame(out Vector2 position) || EventSystem.current == null) return;

        var data = new PointerEventData(EventSystem.current) { position = position };
        hits.Clear();
        EventSystem.current.RaycastAll(data, hits);
        if (hits.Count == 0) return;

        Button button = hits[0].gameObject.GetComponentInParent<Button>();
        if (button != null && button.IsInteractable()) AudioManager.PlaySfx(Sfx.Click);
    }
    
    private static bool PressedThisFrame(out Vector2 position)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        position = Input.mousePosition;
        return Input.touchCount == 0 && Input.GetMouseButtonDown(0);
    }
}
