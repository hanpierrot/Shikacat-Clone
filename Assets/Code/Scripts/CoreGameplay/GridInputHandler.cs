using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridInputHandler : MonoBehaviour
{
    public event Action<Vector2> DragStarted;
    public event Action<Vector2> DragMoved;
    public event Action DragEnded;

    private bool isDragging;
    private int activeFingerId = -1;
    
    private void Update()
    {
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
    }

    private void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                activeFingerId = touch.fingerId;
                isDragging = true;
                DragStarted?.Invoke(touch.position);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (isDragging && touch.fingerId == activeFingerId)
                    DragMoved?.Invoke(touch.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isDragging && touch.fingerId == activeFingerId)
                {
                    isDragging = false;
                    DragEnded?.Invoke();
                }
                activeFingerId = -1;
                break;
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            DragStarted?.Invoke(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            DragMoved?.Invoke(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            DragEnded?.Invoke();
        }
    }
}
