using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class EndingLetterboxUI : MonoBehaviour
{
    [Header("검은 바")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;

    [Header("연출")]
    [SerializeField, Min(0.01f)]
    private float duration = 0.8f;

    [SerializeField, Min(0f)]
    private float holdBeforeCutscene = 0.3f;

    [Header("등장 완료 후 컷씬 시작")]
    [SerializeField] private UnityEvent onShown = new();

    private Sequence sequence;
    private bool hasStarted;

    private void Awake()
    {
        HideImmediately();
    }

    public void Play()
    {
        if (hasStarted)
            return;

        if (topBar == null || bottomBar == null)
        {
            Debug.LogError(
                "위아래 레터박스 이미지를 연결해주세요.",
                this);

            return;
        }

        hasStarted = true;

        // 실제 UI 높이를 계산한 뒤 화면 바깥에서 시작한다.
        Canvas.ForceUpdateCanvases();
        SetOutsidePositions();

        topBar.gameObject.SetActive(true);
        bottomBar.gameObject.SetActive(true);

        sequence = DOTween.Sequence();

        sequence.Append(
            topBar.DOAnchorPosY(0f, duration)
                .SetEase(Ease.InOutSine));

        sequence.Join(
            bottomBar.DOAnchorPosY(0f, duration)
                .SetEase(Ease.InOutSine));

        sequence.AppendInterval(holdBeforeCutscene);

        sequence.SetLink(gameObject);

        sequence.OnComplete(() =>
        {
            sequence = null;
            onShown?.Invoke();
        });
    }

    // 글리치로 화면이 가려졌을 때 호출한다.
    public void HideImmediately()
    {
        sequence?.Kill();
        sequence = null;

        if (topBar != null)
            topBar.gameObject.SetActive(false);

        if (bottomBar != null)
            bottomBar.gameObject.SetActive(false);
    }

    private void SetOutsidePositions()
    {
        Vector2 topPosition = topBar.anchoredPosition;
        topPosition.y = topBar.rect.height;
        topBar.anchoredPosition = topPosition;

        Vector2 bottomPosition = bottomBar.anchoredPosition;
        bottomPosition.y = -bottomBar.rect.height;
        bottomBar.anchoredPosition = bottomPosition;
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}