using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridInputHandler : MonoBehaviour
{
    public event Action<Vector2> PressStarted;
    public event Action<Vector2> PressMoved;
    public event Action<bool> PressEnded;
    
    [Range(0f, 50f)]
    [SerializeField] private float dragThreshold = 10f;

    private bool isPressed;
    private bool isDragging;
    private Vector2 pressPosition;
    private int activeFingerId = -1;
    
    private void Update()
    {
        if (Input.touchCount > 0)
            HandleTouchInput();
        else
            HandleMouseInput();
    }

    private void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (IsPointerOverUI(touch.fingerId)) break;
                activeFingerId = touch.fingerId;
                BeginPress(touch.position);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (isPressed && touch.fingerId == activeFingerId)
                    UpdatePress(touch.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isPressed && touch.fingerId == activeFingerId)
                    EndPress();
                activeFingerId = -1;
                break;
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;
            BeginPress(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isPressed)
        {
            UpdatePress(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isPressed)
        {
            EndPress();
        }
    }
    
    private void BeginPress(Vector2 position)
    {
        isPressed = true;
        isDragging = false;
        pressPosition = position;
        PressStarted?.Invoke(position);
    }

    private void UpdatePress(Vector2 position)
    {
        if (!isDragging && Vector2.Distance(pressPosition, position) >= dragThreshold)
            isDragging = true;
        
        PressMoved?.Invoke(position);
    }
    
    private void EndPress()
    {
        isPressed = false;

        PressEnded?.Invoke(isDragging);
        
        isDragging = false;
    }
    
    private static bool IsPointerOverUI(int touchFingerId = -1)
    {
        if (EventSystem.current == null) return false;
        return touchFingerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(touchFingerId);
    }
}
