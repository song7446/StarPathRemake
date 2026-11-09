using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FreeModeDrawingManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private FreeModeStarController starController;
    [SerializeField] private Camera starCamera;
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Transform lineParent;

    [Header("별 선택")]
    [SerializeField] private LayerMask starLayer;
    [SerializeField] private float starPlaneZ;

    private class Connection
    {
        public FreeModeStar start;
        public FreeModeStar end;
        public LineRenderer line;
    }

    private readonly List<Connection> connections = new();

    private FreeModeStar startStar;
    private LineRenderer currentLine;

    private void OnEnable()
    {
        if (starController != null)
            starController.StarDeleting += OnStarDeleting;
    }

    private void OnDisable()
    {
        if (starController != null)
            starController.StarDeleting -= OnStarDeleting;

        CancelDrawing();
    }

    private bool IsConnectMode()
    {
        return starController != null &&
               starController.CurrentMode ==
               FreeModeStarController.EditMode.Connect;
    }

    private void Update()
    {
        // 모드를 바꾸면 그리고 있던 임시 선만 취소.
        if (!IsConnectMode())
        {
            CancelDrawing();
            return;
        }

        if (!Application.isFocused ||
            Time.timeScale <= 0f ||
            Mouse.current == null ||
            starCamera == null)
        {
            return;
        }

        // UI 버튼을 눌렀을 때 선 연결이나 취소가 발생하지 않게 함.
        if (IsPointerOverUI())
            return;

        Mouse mouse = Mouse.current;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            Undo();
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        if (!TryGetMouseWorldPosition(out Vector3 position))
            return;

        FreeModeStar clickedStar = FindStar(position);

        if (clickedStar == null)
            return;

        if (startStar == null)
        {
            StartDrawing(clickedStar);
        }
        else
        {
            FinishDrawing(clickedStar);
        }
    }

    private void LateUpdate()
    {
        // 이동 모드에서도 완성된 선이 별을 따라간다.
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            Connection connection = connections[i];

            if (connection.start == null ||
                connection.end == null ||
                connection.line == null)
            {
                RemoveConnectionAt(i);
                continue;
            }

            connection.line.SetPosition(
                0, connection.start.transform.position);

            connection.line.SetPosition(
                1, connection.end.transform.position);
        }

        if (!IsConnectMode())
        {
            CancelDrawing();
            return;
        }

        if (currentLine == null)
            return;

        if (startStar == null)
        {
            CancelDrawing();
            return;
        }

        currentLine.SetPosition(0, startStar.transform.position);

        if (!Application.isFocused ||
            Time.timeScale <= 0f ||
            Mouse.current == null ||
            starCamera == null ||
            IsPointerOverUI())
        {
            return;
        }

        if (!TryGetMouseWorldPosition(out Vector3 position))
            return;

        FreeModeStar hoveredStar = FindStar(position);

        // 별 위에 마우스를 올리면 중심에 붙인다.
        currentLine.SetPosition(
            1,
            hoveredStar != null
                ? hoveredStar.transform.position
                : position);
    }

    private void StartDrawing(FreeModeStar star)
    {
        if (linePrefab == null)
            return;

        startStar = star;

        currentLine = Instantiate(
            linePrefab,
            lineParent != null ? lineParent : transform);

        currentLine.useWorldSpace = true;
        currentLine.positionCount = 2;

        currentLine.SetPosition(0, star.transform.position);
        currentLine.SetPosition(1, star.transform.position);
    }

    private void FinishDrawing(FreeModeStar endStar)
    {
        // 자기 자신을 누르면 취소.
        if (endStar == startStar)
        {
            CancelDrawing();
            return;
        }

        // 같은 두 별 사이의 중복 연결 방지.
        foreach (Connection connection in connections)
        {
            bool sameConnection =
                (connection.start == startStar &&
                 connection.end == endStar) ||
                (connection.start == endStar &&
                 connection.end == startStar);

            if (sameConnection)
            {
                CancelDrawing();
                return;
            }
        }

        if (currentLine == null)
        {
            CancelDrawing();
            return;
        }

        currentLine.SetPosition(0, startStar.transform.position);
        currentLine.SetPosition(1, endStar.transform.position);

        connections.Add(new Connection
        {
            start = startStar,
            end = endStar,
            line = currentLine
        });

        currentLine = null;
        startStar = null;
    }

    private void Undo()
    {
        // 그리는 중이라면 임시 선만 취소.
        if (currentLine != null || startStar != null)
        {
            CancelDrawing();
            return;
        }

        // 그리는 중이 아니면 마지막 완성된 선 삭제.
        if (connections.Count > 0)
            RemoveConnectionAt(connections.Count - 1);
    }

    private void CancelDrawing()
    {
        if (currentLine != null)
            Destroy(currentLine.gameObject);

        currentLine = null;
        startStar = null;
    }

    private void OnStarDeleting(FreeModeStar star)
    {
        if (startStar == star)
            CancelDrawing();

        // 삭제되는 별과 연결된 선만 제거.
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            Connection connection = connections[i];

            if (connection.start == star || connection.end == star)
                RemoveConnectionAt(i);
        }
    }

    private void RemoveConnectionAt(int index)
    {
        LineRenderer line = connections[index].line;

        if (line != null)
            Destroy(line.gameObject);

        connections.RemoveAt(index);
    }

    private FreeModeStar FindStar(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(
            position, starLayer);

        foreach (Collider2D hit in hits)
        {
            FreeModeStar star =
                hit.GetComponentInParent<FreeModeStar>();

            if (star != null)
                return star;
        }

        return null;
    }

    private bool TryGetMouseWorldPosition(out Vector3 position)
    {
        position = default;

        Vector2 screenPosition = Mouse.current.position.ReadValue();

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
    
    public void OnPauseMenuOpened()
    {
        CancelDrawing();
    }
}