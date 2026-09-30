using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridInputHandler : MonoBehaviour
{
    public event Action<Vector2> PressStarted;
    public event Action<Vector2> PressMoved;
    public event Action<Vector2, bool> PressEnded;
    
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
                    EndPress(touch.position);
                activeFingerId = -1;
                break;
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginPress(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isPressed)
        {
            UpdatePress(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isPressed)
        {
            EndPress(Input.mousePosition);
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
    
    private void EndPress(Vector2 position)
    {
        isPressed = false;

        PressEnded?.Invoke(position, isDragging);
        
        isDragging = false;
    }
}
