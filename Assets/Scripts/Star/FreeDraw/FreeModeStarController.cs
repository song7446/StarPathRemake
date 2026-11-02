using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FreeModeStarController : MonoBehaviour
{
    public enum EditMode
    {
        Move,
        Connect
    }

    [Header("별 생성")]
    [SerializeField] private FreeModeStar starPrefab;
    [SerializeField] private Transform starParent;
    [SerializeField] private Camera starCamera;
    [SerializeField] private float starPlaneZ;

    [Header("별 선택")]
    [SerializeField] private LayerMask starLayer;

    [SerializeField, Min(1f)]
    private float dragThreshold = 8f;

    [SerializeField] private Color selectionBoxColor =
        new Color(0.7f, 0.85f, 1f, 0.2f);

    [Header("현재 모드")]
    [SerializeField] private EditMode currentMode = EditMode.Move;

    public EditMode CurrentMode => currentMode;

    // 선 연결 기능에서 구독하면 삭제될 별의 선을 정리할 수 있음.
    public event Action<FreeModeStar> StarDeleting;

    private readonly List<FreeModeStar> stars = new();
    private readonly HashSet<FreeModeStar> selectedStars = new();

    private readonly Dictionary<FreeModeStar, Vector3>
        dragStartPositions = new();

    private int nextStarId;

    private bool isMoving;
    private bool isSelecting;
    private bool hasDragged;

    private Vector2 pressScreenPosition;
    private Vector2 currentScreenPosition;
    private Vector3 pressWorldPosition;

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || starCamera == null)
            return;

        if (!Application.isFocused || Time.timeScale <= 0f)
        {
            CancelGesture();
            return;
        }

        if (currentMode != EditMode.Move)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.deleteKey.wasPressedThisFrame &&
            !IsEditingText())
        {
            DeleteSelectedStars();
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame &&
            !IsPointerOverUI() &&
            TryScreenToWorld(screenPosition, out Vector3 worldPosition))
        {
            BeginSelectionOrMove(screenPosition, worldPosition);
        }

        if (mouse.leftButton.isPressed)
        {
            UpdateGesture(screenPosition);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            FinishGesture(screenPosition);
        }
    }

    // 「별 만들기」 버튼에 연결.
    public void CreateStarAtCenter()
    {
        if (starPrefab == null ||
            starCamera == null ||
            Time.timeScale <= 0f)
        {
            return;
        }

        Vector2 screenCenter = starCamera.pixelRect.center;

        if (!TryScreenToWorld(screenCenter, out Vector3 position))
            return;

        SetMoveMode();

        FreeModeStar star = Instantiate(
            starPrefab,
            position,
            starPrefab.transform.rotation,
            starParent);

        star.Initialize(nextStarId++);
        stars.Add(star);

        // 생성한 별을 바로 이동하거나 삭제할 수 있도록 선택.
        ClearSelection();
        SelectStar(star);
    }

    private void BeginSelectionOrMove(
        Vector2 screenPosition,
        Vector3 worldPosition)
    {
        CancelGesture();

        pressScreenPosition = screenPosition;
        currentScreenPosition = screenPosition;
        pressWorldPosition = worldPosition;

        FreeModeStar hitStar = FindStar(worldPosition);

        if (hitStar == null)
        {
            ClearSelection();
            isSelecting = true;
            return;
        }

        // 선택된 별을 잡으면 기존 다중 선택을 유지.
        if (!selectedStars.Contains(hitStar))
        {
            ClearSelection();
            SelectStar(hitStar);
        }

        dragStartPositions.Clear();

        foreach (FreeModeStar star in selectedStars)
        {
            if (star != null)
                dragStartPositions[star] = star.transform.position;
        }

        isMoving = true;
    }

    private void UpdateGesture(Vector2 screenPosition)
    {
        if (!isMoving && !isSelecting)
            return;

        currentScreenPosition = screenPosition;

        if (Vector2.Distance(pressScreenPosition, screenPosition)
            >= dragThreshold)
        {
            hasDragged = true;
        }

        if (!hasDragged)
            return;

        if (isSelecting)
        {
            ApplyBoxSelection();
            return;
        }

        // UI 위에서는 이동을 잠시 멈춘다.
        if (IsPointerOverUI() ||
            !TryScreenToWorld(screenPosition, out Vector3 position))
        {
            return;
        }

        Vector3 offset = position - pressWorldPosition;

        foreach (var pair in dragStartPositions)
        {
            if (pair.Key != null)
                pair.Key.transform.position = pair.Value + offset;
        }
    }

    private void FinishGesture(Vector2 screenPosition)
    {
        if (isSelecting && hasDragged)
        {
            currentScreenPosition = screenPosition;
            ApplyBoxSelection();
        }

        CancelGesture();
    }

    private void ApplyBoxSelection()
    {
        Rect selectionRect = GetSelectionRect();

        foreach (FreeModeStar star in stars)
        {
            if (star == null || !star.gameObject.activeInHierarchy)
                continue;

            Vector3 screenPosition =
                starCamera.WorldToScreenPoint(star.transform.position);

            bool selected =
                screenPosition.z > 0f &&
                starCamera.pixelRect.Contains((Vector2)screenPosition) &&
                selectionRect.Contains((Vector2)screenPosition);

            star.SetSelected(selected);

            if (selected)
                selectedStars.Add(star);
            else
                selectedStars.Remove(star);
        }
    }

    // 「선택 삭제」 버튼에 연결.
    public void DeleteSelectedStars()
    {
        if (Time.timeScale <= 0f)
            return;

        CancelGesture();

        // 삭제 이벤트 중 목록이 변경되어도 안전하게 복사.
        var targets = new List<FreeModeStar>(selectedStars);
        ClearSelection();

        foreach (FreeModeStar star in targets)
        {
            if (star == null)
                continue;

            StarDeleting?.Invoke(star);

            stars.Remove(star);
            star.gameObject.SetActive(false);
            Destroy(star.gameObject);
        }
    }

    private void SelectStar(FreeModeStar star)
    {
        selectedStars.Add(star);
        star.SetSelected(true);
    }

    public void ClearSelection()
    {
        foreach (FreeModeStar star in selectedStars)
        {
            if (star != null)
                star.SetSelected(false);
        }

        selectedStars.Clear();
    }

    public void SetMoveMode()
    {
        CancelGesture();
        currentMode = EditMode.Move;
    }

    public void SetConnectMode()
    {
        CancelGesture();
        ClearSelection();
        currentMode = EditMode.Connect;
    }

    private void CancelGesture()
    {
        isMoving = false;
        isSelecting = false;
        hasDragged = false;
        dragStartPositions.Clear();
    }

    private FreeModeStar FindStar(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(
            position, starLayer);

        foreach (Collider2D hit in hits)
        {
            FreeModeStar star =
                hit.GetComponentInParent<FreeModeStar>();

            if (star != null && stars.Contains(star))
                return star;
        }

        return null;
    }

    private bool TryScreenToWorld(
        Vector2 screenPosition,
        out Vector3 position)
    {
        position = default;

        if (!starCamera.pixelRect.Contains(screenPosition))
            return false;

        Ray ray = starCamera.ScreenPointToRay(screenPosition);

        Plane plane = new Plane(
            Vector3.forward,
            new Vector3(0f, 0f, starPlaneZ));

        if (!plane.Raycast(ray, out float distance))
            return false;

        position = ray.GetPoint(distance);
        position.z = starPlaneZ;

        return true;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsEditingText()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected =
            EventSystem.current.currentSelectedGameObject;

        return selected != null &&
               (selected.GetComponent<TMPro.TMP_InputField>() != null ||
                selected.GetComponent<UnityEngine.UI.InputField>() != null);
    }

    private Rect GetSelectionRect()
    {
        Vector2 min = Vector2.Min(
            pressScreenPosition, currentScreenPosition);

        Vector2 max = Vector2.Max(
            pressScreenPosition, currentScreenPosition);

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void OnGUI()
    {
        if (!isSelecting || !hasDragged)
            return;

        Rect screenRect = GetSelectionRect();

        // マウス座標は左下基準、GUIは左上基準。
        Rect guiRect = new Rect(
            screenRect.x,
            Screen.height - screenRect.yMax,
            screenRect.width,
            screenRect.height);

        Color previousColor = GUI.color;

        GUI.color = selectionBoxColor;
        GUI.DrawTexture(guiRect, Texture2D.whiteTexture);

        GUI.color = new Color(
            selectionBoxColor.r,
            selectionBoxColor.g,
            selectionBoxColor.b,
            1f);

        GUI.DrawTexture(
            new Rect(guiRect.x, guiRect.y, guiRect.width, 1f),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(guiRect.x, guiRect.yMax - 1f, guiRect.width, 1f),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(guiRect.x, guiRect.y, 1f, guiRect.height),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(guiRect.xMax - 1f, guiRect.y, 1f, guiRect.height),
            Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

    private void OnDisable()
    {
        CancelGesture();
        ClearSelection();
    }
}