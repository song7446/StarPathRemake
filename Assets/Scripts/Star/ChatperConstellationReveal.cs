using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class ChapterConstellationReveal : MonoBehaviour
{
    [Header("기존 챕터 별")]
    // [SerializeField] private ChapterDrawingManager drawingManager;
    [SerializeField] private Canvas chapterCanvas;

    [Header("월드 별")]
    [Tooltip("작은 별의 외형만 있는 프리팹")]
    [SerializeField] private GameObject worldStarPrefab;

    [Tooltip("카메라의 자식이 아닌 월드 오브젝트")]
    [SerializeField] private Transform worldStarParent;

    [Tooltip("ChapterDrawingManager의 Line Camera와 동일한 카메라")]
    [SerializeField] private Camera lineCamera;

    [Tooltip("ChapterDrawingManager의 Line Distance와 동일한 값")]
    [SerializeField, Min(0.01f)] private float lineDistance = 10f;

    [Header("입력 차단")]
    [Tooltip("화면 전체를 덮는 투명 Image. Raycast Target 켜기")]
    [SerializeField] private GameObject inputBlocker;

    [Header("연출")]
    [SerializeField, Min(0f)] private float blinkWaitDuration = 1f;
    [SerializeField, Min(0.01f)] private float transformDuration = 1.5f;

    [Tooltip("기존 썸네일이 사라질 때 줄어드는 비율")]
    [SerializeField, Range(0.01f, 1f)]
    private float thumbnailShrinkRatio = 0.15f;

    [Tooltip("기존 선 굵기에 곱할 비율")]
    [SerializeField, Range(0.01f, 1f)]
    private float lineWidthRatio = 0.5f;

    [SerializeField] private Color finalLineColor =
        new Color(0.85f, 0.9f, 1f, 0.7f);

    [Header("변환 완료")]
    [SerializeField] private UnityEvent onRevealFinished = new();

    private readonly Dictionary<ChapterButton, Vector3>
        starPositions = new();

    private readonly List<ChapterDrawingManager.Connection>
        capturedConnections = new();

    private Sequence revealSequence;
    private bool hasStarted;

    // ChapterDrawingManager의 On Puzzle Completed에 연결.
    public void Play()
    {
        if (hasStarted)
            return;

        if (chapterCanvas == null ||
            lineCamera == null ||
            worldStarPrefab == null ||
            inputBlocker == null)
        {
            Debug.LogError(
                "별자리 변환 연출의 참조를 모두 연결해주세요.",
                this);
            return;
        }

        if (ChapterDrawingManager.Instance.Connections.Count == 0)
            return;

        hasStarted = true;

        // 드래그와 장면 보기 등 UI 입력 차단.
        inputBlocker.SetActive(true);
        ChapterSceneMenuUI[] menus =
            chapterCanvas.GetComponentsInChildren<ChapterSceneMenuUI>(true);

        foreach (ChapterSceneMenuUI menu in menus)
            menu.Close();

        ChapterDrawingManager.Instance.SetInputEnabled(false);

        revealSequence = DOTween.Sequence();

        // 마지막 정답 깜빡임이 끝난 뒤 시작.
        revealSequence.AppendInterval(blinkWaitDuration);
        revealSequence.AppendCallback(BeginTransformation);
        revealSequence.SetLink(gameObject);
    }

    private void BeginTransformation()
    {
        capturedConnections.Clear();
        starPositions.Clear();

        foreach (var connection in ChapterDrawingManager.Instance.Connections)
        {
            if (connection.From == null ||
                connection.To == null ||
                connection.Line == null)
            {
                continue;
            }

            capturedConnections.Add(connection);

            CacheStarPosition(connection.From);
            CacheStarPosition(connection.To);
        }

        // 이후 연결선이 UI 위치를 따라가지 않게 한다.
        // 완성된 선 오브젝트는 그대로 유지된다.
        ChapterDrawingManager.Instance.enabled = false;

        Sequence transformation = DOTween.Sequence();
        transformation.AppendInterval(transformDuration);

        foreach (var pair in starPositions)
        {
            ChapterButton chapterStar = pair.Key;
            Vector3 worldPosition = pair.Value;

            RectTransform rect =
                (RectTransform)chapterStar.transform;

            CanvasGroup group =
                chapterStar.GetComponent<CanvasGroup>();

            if (group == null)
                group = chapterStar.gameObject.AddComponent<CanvasGroup>();

            group.interactable = false;
            group.blocksRaycasts = false;

            // 썸네일을 중심 기준으로 줄인다.
            SetPivotToCenter(rect);

            rect.DOKill();
            group.DOKill();

            Vector3 originalScale = rect.localScale;

            transformation.Insert(
                0f,
                group.DOFade(0f, transformDuration)
                    .SetEase(Ease.InOutSine));

            transformation.Insert(
                0f,
                rect.DOScale(
                        originalScale * thumbnailShrinkRatio,
                        transformDuration)
                    .SetEase(Ease.InOutSine));

            CreateWorldStar(worldPosition, transformation);
        }

        foreach (var connection in capturedConnections)
        {
            LineRenderer line = connection.Line;

            line.useWorldSpace = true;
            line.SetPosition(0, starPositions[connection.From]);
            line.SetPosition(1, starPositions[connection.To]);

            float originalWidth = line.widthMultiplier;

            transformation.Insert(
                0f,
                DOTween.To(
                        () => line.widthMultiplier,
                        value =>
                        {
                            if (line != null)
                                line.widthMultiplier = value;
                        },
                        originalWidth * lineWidthRatio,
                        transformDuration)
                    .SetEase(Ease.InOutSine));

            Color startColor = line.startColor;
            Color endColor = line.endColor;

            transformation.Insert(
                0f,
                DOVirtual.Float(
                    0f, 1f, transformDuration,
                    progress =>
                    {
                        if (line == null)
                            return;

                        line.startColor = Color.Lerp(
                            startColor, finalLineColor, progress);

                        line.endColor = Color.Lerp(
                            endColor, finalLineColor, progress);
                    }));
        }

        revealSequence = transformation;

        transformation.SetLink(gameObject);
        transformation.OnComplete(() =>
        {
            revealSequence = null;

            // 썸네일は透明になった後で非アクティブ化。
            foreach (var pair in starPositions)
            {
                if (pair.Key != null)
                    pair.Key.gameObject.SetActive(false);
            }

            onRevealFinished.Invoke();
        });
    }

    private void CacheStarPosition(ChapterButton star)
    {
        if (starPositions.ContainsKey(star))
            return;

        RectTransform rect = (RectTransform)star.transform;
        Canvas rootCanvas = chapterCanvas.rootCanvas;

        Camera uiCamera =
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                rect.TransformPoint(rect.rect.center));

        Vector3 worldPosition = lineCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                lineDistance));

        starPositions.Add(star, worldPosition);
    }

    private void CreateWorldStar(
        Vector3 position,
        Sequence sequence)
    {
        GameObject star = Instantiate(
            worldStarPrefab,
            position,
            Quaternion.identity,
            worldStarParent);

        foreach (SpriteRenderer renderer in
                 star.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color originalColor = renderer.color;

            Color transparentColor = originalColor;
            transparentColor.a = 0f;
            renderer.color = transparentColor;

            sequence.Insert(
                0f,
                renderer.DOFade(
                        originalColor.a,
                        transformDuration)
                    .SetEase(Ease.InOutSine));
        }
    }

    private void SetPivotToCenter(RectTransform rect)
    {
        Vector3 centerBefore =
            rect.TransformPoint(rect.rect.center);

        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector3 centerAfter =
            rect.TransformPoint(rect.rect.center);

        rect.position += centerBefore - centerAfter;
    }

    private void OnDestroy()
    {
        revealSequence?.Kill();
    }
}