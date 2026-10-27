using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ChapterDrawingManager : MonoBehaviourSingleton<ChapterDrawingManager>
{
    [Header("기존 LineRenderer 프리팹")]
    [SerializeField] private LineRenderer linePrefab;

    [Header("챕터 별이 있는 Canvas")]
    [SerializeField] private Canvas chapterCanvas;

    [Header("선을 렌더링하는 카메라")]
    [SerializeField] private Camera lineCamera;

    // 카메라 앞 어느 깊이에 선을 배치할지 설정.
    [SerializeField, Min(0.01f)]
    private float lineDistance = 10f;

    [Header("순서 퍼즐 입력")]
    [SerializeField] private bool inputEnabled;

    public bool InputEnabled => inputEnabled;
    public bool IsDrawing => currentLine != null;

    public sealed class Connection
    {
        public ChapterButton From { get; }
        public ChapterButton To { get; }
        public LineRenderer Line { get; }

        public Connection(
            ChapterButton from,
            ChapterButton to,
            LineRenderer line)
        {
            From = from;
            To = to;
            Line = line;
        }
    }

    private readonly List<Connection> connections = new();
    public IReadOnlyList<Connection> Connections => connections;

    private readonly List<RaycastResult> raycastResults = new();

    private LineRenderer currentLine;
    private ChapterButton startStar;
    
    [SerializeField]
    private int[] correctChapterOrder =
    {
        8,  // 5앞
        0,  // 1앞
        2,  // 2앞
        6,  // 4앞
        9,  // 5뒤
        4,  // 3앞
        7,  // 4뒤
        1,  // 1뒤
        5,  // 3뒤
        3,  // 2뒤
        10, // 6앞
        11  // 6뒤
    };
    
    [SerializeField]
    private UnityEngine.Events.UnityEvent onPuzzleCompleted =
        new UnityEngine.Events.UnityEvent();

    private bool isCompleted;

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
            CancelDrawing();
    }

    // ChapterButton의 On Left Click에 연결.
    public void OnStarClicked(ChapterButton star)
    {
        if (!inputEnabled || star == null)
            return;

        if (!IsDrawing)
        {
            startStar = star;
            currentLine = CreateLine();

            Vector3 position = GetStarWorldPosition(star);

            currentLine.SetPosition(0, position);
            currentLine.SetPosition(1, position);
            return;
        }

        // 시작 별을 다시 누르면 현재 선 취소.
        if (star == startStar)
        {
            CancelDrawing();
            return;
        }

        // 같은 두 별 사이의 중복 연결 방지.
        foreach (Connection connection in connections)
        {
            bool sameDirection =
                connection.From == startStar &&
                connection.To == star;

            bool reverseDirection =
                connection.From == star &&
                connection.To == startStar;

            if (sameDirection || reverseDirection)
            {
                CancelDrawing();
                return;
            }
        }

        currentLine.SetPosition(
            0, GetStarWorldPosition(startStar));

        currentLine.SetPosition(
            1, GetStarWorldPosition(star));

        connections.Add(
            new Connection(startStar, star, currentLine)
        );

        currentLine = null;
        startStar = null;
        
        CheckAnswer();
    }

    private LineRenderer CreateLine()
    {
        LineRenderer line = Instantiate(linePrefab, transform);

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.loop = false;
        line.gameObject.SetActive(true);

        return line;
    }

    private void LateUpdate()
    {
        // 완성한 선도 매 프레임 두 별의 위치를 따라간다.
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            Connection connection = connections[i];

            if (connection.From == null ||
                connection.To == null ||
                connection.Line == null)
            {
                if (connection.Line != null)
                    Destroy(connection.Line.gameObject);

                connections.RemoveAt(i);
                continue;
            }

            connection.Line.SetPosition(
                0, GetStarWorldPosition(connection.From));

            connection.Line.SetPosition(
                1, GetStarWorldPosition(connection.To));
        }

        if (!IsDrawing)
            return;

        if (startStar == null || Mouse.current == null)
        {
            CancelDrawing();
            return;
        }

        // 그리는 도중 시작 별을 움직여도 선이 따라간다.
        currentLine.SetPosition(
            0, GetStarWorldPosition(startStar));

        ChapterButton hoveredStar = GetHoveredStar();

        if (hoveredStar != null && hoveredStar != startStar)
        {
            // 다른 별에 마우스를 올리면 중앙으로 스냅.
            currentLine.SetPosition(
                1, GetStarWorldPosition(hoveredStar));
        }
        else
        {
            currentLine.SetPosition(
                1,
                ScreenToLineWorld(
                    Mouse.current.position.ReadValue()
                )
            );
        }
    }

    private Vector3 GetStarWorldPosition(ChapterButton star)
    {
        RectTransform rect = (RectTransform)star.transform;

        Canvas canvas = chapterCanvas.rootCanvas;

        Camera uiCamera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        Vector3 worldCenter =
            rect.TransformPoint(rect.rect.center);

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera, worldCenter);

        return ScreenToLineWorld(screenPosition);
    }

    private Vector3 ScreenToLineWorld(Vector2 screenPosition)
    {
        return lineCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                lineDistance
            )
        );
    }

    private ChapterButton GetHoveredStar()
    {
        if (EventSystem.current == null || Mouse.current == null)
            return null;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, raycastResults);

        if (raycastResults.Count == 0)
            return null;

        return raycastResults[0].gameObject
            .GetComponentInParent<ChapterButton>();
    }

    public void OnRightClick()
    {
        if (!inputEnabled)
            return;

        // 미완성 선 취소.
        if (IsDrawing)
        {
            CancelDrawing();
            return;
        }

        // 마지막 완성 선과 연결 기록을 함께 제거.
        if (connections.Count == 0)
            return;

        int lastIndex = connections.Count - 1;
        Connection connection = connections[lastIndex];

        if (connection.Line != null)
            Destroy(connection.Line.gameObject);

        connections.RemoveAt(lastIndex);
    }

    private void CancelDrawing()
    {
        if (currentLine != null)
            Destroy(currentLine.gameObject);

        currentLine = null;
        startStar = null;
    }

    public void ClearAllLines()
    {
        CancelDrawing();

        foreach (Connection connection in connections)
        {
            if (connection.Line != null)
                Destroy(connection.Line.gameObject);
        }

        connections.Clear();
        isCompleted = false;
    }
    
    public bool CheckAnswer()
    {
        if (isCompleted)
            return true;

        if (correctChapterOrder == null ||
            correctChapterOrder.Length < 2)
        {
            return false;
        }

        // 정답에 필요한 선 개수와 정확히 같아야 한다.
        if (connections.Count != correctChapterOrder.Length - 1)
            return false;

        var actualConnections = new HashSet<(int, int)>();

        foreach (Connection connection in connections)
        {
            if (connection.From == null ||
                connection.To == null ||
                connection.Line == null)
            {
                return false;
            }

            int from = connection.From.ChapterIdx;
            int to = connection.To.ChapterIdx;

            if (from == to)
                return false;

            // 작은 번호를 앞에 넣어 양방향을 동일하게 취급.
            var key = (Mathf.Min(from, to), Mathf.Max(from, to));

            // 중복 연결이 있다면 정답으로 처리하지 않는다.
            if (!actualConnections.Add(key))
                return false;
        }

        for (int i = 0; i < correctChapterOrder.Length - 1; i++)
        {
            int from = correctChapterOrder[i];
            int to = correctChapterOrder[i + 1];

            var key = (Mathf.Min(from, to), Mathf.Max(from, to));

            if (!actualConnections.Contains(key))
                return false;
        }

        isCompleted = true;

        // 성공 후 추가 연결이나 우클릭 삭제 방지.
        SetInputEnabled(false);

        Debug.Log("시간대 퍼즐 완성!");

        onPuzzleCompleted?.Invoke();

        return true;
    }

    public void OnPuzzleCompleted()
    {
        Debug.Log("시간대 퍼즐 정답");
    }

    private void OnDisable()
    {
        CancelDrawing();
    }
}