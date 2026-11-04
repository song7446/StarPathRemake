using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

public class ChapterDrawingManager : SceneMonoBehaviourSingleton<ChapterDrawingManager>
{
    [Header("기존 LineRenderer 프리팹")] [SerializeField]
    private LineRenderer linePrefab;

    [Header("챕터 별이 있는 Canvas")] [SerializeField]
    private Canvas chapterCanvas;

    [Header("선을 렌더링하는 카메라")] [SerializeField]
    private Camera lineCamera;

    // 카메라 앞 어느 깊이에 선을 배치할지 설정.
    [SerializeField, Min(0.01f)] private float lineDistance = 10f;

    [Header("순서 퍼즐 입력")] [SerializeField] private bool inputEnabled;

    public bool InputEnabled => inputEnabled;
    public bool IsDrawing => currentLine != null;
    
    [Header("오답 선 퇴장")]
    [SerializeField, Min(0.01f)]
    private float wrongLineFadeDuration = 0.5f;

    private readonly List<Connection> fadingConnections = new();

    [SerializeField] private ChapterConstellationReveal chapterConstellationReveal;
    [SerializeField] private Transform constellationRoot;
    
    [Header("시간대 퍼즐 안내")]
    [SerializeField] private ChapterOrderHintUI chapterOrderHintUI;

    public sealed class Connection
    {
        public ChapterButton From { get; }
        public ChapterButton To { get; }
        public LineRenderer Line { get; }

        public bool IsLocked { get; private set; }

        public Connection(
            ChapterButton from,
            ChapterButton to,
            LineRenderer line)
        {
            From = from;
            To = to;
            Line = line;
        }

        public void Lock(Color color)
        {
            if (IsLocked)
                return;

            IsLocked = true;

            if (Line == null)
                return;

            Line.startColor = color;
            Line.endColor = color;

            float opacity = 1f;

            DOTween.To(
                    () => opacity,
                    value =>
                    {
                        opacity = value;

                        if (Line == null)
                            return;

                        Color blinkColor = color;
                        blinkColor.a = color.a * value;

                        Line.startColor = blinkColor;
                        Line.endColor = blinkColor;
                    },
                    0.15f,
                    0.15f)
                .SetEase(Ease.InOutSine)
                .SetLoops(6, LoopType.Yoyo)
                .SetLink(Line.gameObject)
                .OnComplete(() =>
                {
                    if (Line == null)
                        return;

                    Line.startColor = color;
                    Line.endColor = color;
                });
        }
    }

    [Header("부분 정답")] [SerializeField, Min(1)]
    private int lockThreshold = 4;

    [SerializeField] private Color lockedLineColor =
        new Color(1f, 0.85f, 0.5f, 1f);

    private readonly List<Connection> connections = new();
    public IReadOnlyList<Connection> Connections => connections;

    private readonly List<RaycastResult> raycastResults = new();

    private LineRenderer currentLine;
    private ChapterButton startStar;

    [SerializeField] private int[] correctChapterOrder =
    {
        8, // 5앞
        0, // 1앞
        2, // 2앞
        6, // 4앞
        9, // 5뒤
        4, // 3앞
        7, // 4뒤
        1, // 1뒤
        5, // 3뒤
        3, // 2뒤
        10, // 6앞
        11 // 6뒤
    };

    [SerializeField] private UnityEngine.Events.UnityEvent onPuzzleCompleted =
        new UnityEngine.Events.UnityEvent();

    private bool isCompleted;
    
    private void Start()
    {
        SetInputEnabled(false);
    }
    
    public void BeginOrderPuzzle()
    {
        if (isCompleted)
            return;

        SetInputEnabled(true);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            CancelDrawing();

            if (chapterOrderHintUI != null)
                chapterOrderHintUI.HideGuide();

            return;
        }

        if (!isCompleted && chapterOrderHintUI != null)
            chapterOrderHintUI.ShowGuide();
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
        LineRenderer line = Instantiate(linePrefab, constellationRoot);

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.loop = false;
        line.gameObject.SetActive(true);

        return line;
    }

    private void LateUpdate()
    {
        UpdateFadingConnections();
        
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

        // 미완성 선부터 취소.
        if (IsDrawing)
        {
            CancelDrawing();
            return;
        }

        for (int i = connections.Count - 1; i >= 0; i--)
        {
            if (connections[i].IsLocked)
                continue;

            RemoveConnectionAt(i);
            return;
        }
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

        foreach (Connection connection in fadingConnections)
        {
            if (connection.Line != null)
                Destroy(connection.Line.gameObject);
        }

        connections.Clear();
        fadingConnections.Clear();

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

        // 정답에 포함되는 연결 목록.
        var correctKeys = new HashSet<(int, int)>();

        for (int i = 0; i < correctChapterOrder.Length - 1; i++)
        {
            correctKeys.Add(GetConnectionKey(
                correctChapterOrder[i],
                correctChapterOrder[i + 1]));
        }

        var connectedCorrectKeys = new HashSet<(int, int)>();
        var lockedCorrectKeys = new HashSet<(int, int)>();

        foreach (Connection connection in connections)
        {
            if (!IsValidConnection(connection))
                continue;

            var key = GetConnectionKey(
                connection.From.ChapterIdx,
                connection.To.ChapterIdx);

            if (!correctKeys.Contains(key))
                continue;

            connectedCorrectKeys.Add(key);

            if (connection.IsLocked)
                lockedCorrectKeys.Add(key);
        }

        // 아직 고정하지 않은 정답 선 개수.
        int newCorrectCount =
            connectedCorrectKeys.Count - lockedCorrectKeys.Count;

        bool allCorrectConnected =
            connectedCorrectKeys.SetEquals(correctKeys);

        // 기준 미달이면 피드백 없이 연결을 유지.
        if (!allCorrectConnected &&
            newCorrectCount < Mathf.Max(1, lockThreshold))
        {
            return false;
        }

        // 정답 선은 고정하고 오답 선은 페이드 아웃.
        LockCorrectAndRemoveWrong(correctKeys);

        if (!allCorrectConnected)
        {
            // 첫 부분 정답에서만 안내 문구 변경.
            if (chapterOrderHintUI != null)
                chapterOrderHintUI.ShowPartialResult();

            Debug.Log(
                $"정답 연결 {newCorrectCount}개 확정! " +
                $"전체 {connectedCorrectKeys.Count}/{correctKeys.Count}");

            return false;
        }

        isCompleted = true;

        // 엔딩 연출 전에 안내를 완전히 종료.
        if (chapterOrderHintUI != null)
            chapterOrderHintUI.Complete();

        SetInputEnabled(false);

        onPuzzleCompleted?.Invoke();

        return true;
    }

    private (int, int) GetConnectionKey(int from, int to)
    {
        // 기존처럼 양방향을 동일한 연결로 취급.
        return (Mathf.Min(from, to), Mathf.Max(from, to));
    }

    private bool IsValidConnection(Connection connection)
    {
        return connection != null &&
               connection.From != null &&
               connection.To != null &&
               connection.Line != null &&
               connection.From.ChapterIdx != connection.To.ChapterIdx;
    }

    private void LockCorrectAndRemoveWrong(
        HashSet<(int, int)> correctKeys)
    {
        var retainedKeys = new HashSet<(int, int)>();

        int index = 0;

        while (index < connections.Count)
        {
            Connection connection = connections[index];

            if (!IsValidConnection(connection))
            {
                RemoveConnectionAt(index);
                continue;
            }

            var key = GetConnectionKey(
                connection.From.ChapterIdx,
                connection.To.ChapterIdx);

            if (!correctKeys.Contains(key) || !retainedKeys.Add(key))
            {
                FadeOutConnectionAt(index);
                continue;
            }

            connection.Lock(lockedLineColor);
            index++;
        }
    }
    
    private void FadeOutConnectionAt(int index)
    {
        Connection connection = connections[index];

        // 정답 검사와 우클릭 취소 대상에서는 즉시 제외.
        connections.RemoveAt(index);

        if (connection.Line == null)
            return;

        fadingConnections.Add(connection);

        LineRenderer line = connection.Line;

        Gradient originalGradient = line.colorGradient;
        GradientColorKey[] colorKeys = originalGradient.colorKeys;
        GradientAlphaKey[] originalAlphaKeys = originalGradient.alphaKeys;

        GradientAlphaKey[] fadedAlphaKeys =
            new GradientAlphaKey[originalAlphaKeys.Length];

        Gradient fadedGradient = new Gradient
        {
            mode = originalGradient.mode
        };

        DOVirtual.Float(
                1f,
                0f,
                Mathf.Max(0.01f, wrongLineFadeDuration),
                opacity =>
                {
                    if (line == null)
                        return;

                    for (int i = 0; i < originalAlphaKeys.Length; i++)
                    {
                        fadedAlphaKeys[i] = new GradientAlphaKey(
                            originalAlphaKeys[i].alpha * opacity,
                            originalAlphaKeys[i].time);
                    }

                    fadedGradient.SetKeys(colorKeys, fadedAlphaKeys);
                    line.colorGradient = fadedGradient;
                })
            .SetEase(Ease.InOutSine)
            .SetLink(line.gameObject)
            .OnComplete(() =>
            {
                fadingConnections.Remove(connection);

                if (line != null)
                    Destroy(line.gameObject);
            });
    }
    
    private void UpdateFadingConnections()
    {
        for (int i = fadingConnections.Count - 1; i >= 0; i--)
        {
            Connection connection = fadingConnections[i];

            if (connection.From == null ||
                connection.To == null ||
                connection.Line == null)
            {
                if (connection.Line != null)
                    Destroy(connection.Line.gameObject);

                fadingConnections.RemoveAt(i);
                continue;
            }

            connection.Line.SetPosition(
                0, GetStarWorldPosition(connection.From));

            connection.Line.SetPosition(
                1, GetStarWorldPosition(connection.To));
        }
    }

    private void RemoveConnectionAt(int index)
    {
        Connection connection = connections[index];

        if (connection != null && connection.Line != null)
            Destroy(connection.Line.gameObject);

        connections.RemoveAt(index);
    }

    public void OnPuzzleCompleted()
    {
        chapterConstellationReveal.Play();
    }

    private void OnDisable()
    {
        CancelDrawing();
    }
}