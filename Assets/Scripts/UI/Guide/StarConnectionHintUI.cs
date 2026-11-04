using DG.Tweening;
using TMPro;
using UnityEngine;

public class StarConnectionHintUI : MonoBehaviour
{
    public static StarConnectionHintUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup hintGroup;
    [SerializeField] private TMP_Text hintText;

    [Header("챕터 인덱스")]
    [SerializeField] private int firstChapterIndex = 0;
    [SerializeField] private int secondChapterIndex = 2;

    [Header("등장과 깜빡임")]
    [SerializeField, Min(0.01f)]
    private float fadeInDuration = 0.4f;

    [SerializeField, Min(0.01f)]
    private float blinkDuration = 0.8f;

    private Tween hintTween;

    private bool hasConnectedLine;
    private bool firstPuzzleCompleted;
    private bool hasOpenedNotebook;

    private bool isShowing;
    private string currentMessage;

    private void Awake()
    {
        Instance = this;

        hintGroup.alpha = 0f;
        hintGroup.interactable = false;
        hintGroup.blocksRaycasts = false;
        hintGroup.gameObject.SetActive(false);

        hintText.raycastTarget = false;
    }

    private void Update()
    {
        RefreshHint();
    }

    private void RefreshHint()
    {
        // 땅을 보거나 대사가 진행 중이면 안내 숨김.
        if (!UIManager.Instance.GetIsOnSkyView() ||
            UIManager.Instance.IsPlayingDialogue() ||
            MonologueUI.IsPlaying ||
            PauseMenuUI.IsOpen)
        {
            Hide();
            return;
        }

        int chapterIndex = GameManager.Instance.currentChapterIdx;

        if (chapterIndex == firstChapterIndex &&
            !firstPuzzleCompleted)
        {
            Show(hasConnectedLine
                ? "우클릭으로 그리던 선을 취소하거나 마지막 선을 지울 수 있습니다."
                : "별을 차례로 클릭해서 연결하세요.");

            return;
        }

        if (chapterIndex == secondChapterIndex &&
            !hasOpenedNotebook)
        {
            Show(
                "수첩에서 현재 챕터에 해당하는 별자리 사진을 꺼내 참고하세요."
            );

            return;
        }

        Hide();
    }

    // 실제로 새로운 선이 완성됐을 때 호출.
    public void NotifyLineConnected()
    {
        if (GameManager.Instance.currentChapterIdx != firstChapterIndex ||
            firstPuzzleCompleted)
        {
            return;
        }

        hasConnectedLine = true;
        RefreshHint();
    }

    // 첫 챕터 별자리 완성 시 호출.
    public void Complete()
    {
        if (GameManager.Instance.currentChapterIdx != firstChapterIndex)
            return;

        firstPuzzleCompleted = true;
        Hide();
    }

    // 챕터와 관계없이 수첩을 처음 열었을 때부터 기록.
    public void NotifyNotebookOpened()
    {
        hasOpenedNotebook = true;
        RefreshHint();
    }

    private void Show(string message)
    {
        if (isShowing && currentMessage == message)
            return;

        hintTween?.Kill();

        isShowing = true;
        currentMessage = message;

        // 안내가 바뀔 때도 페이드 인.
        hintGroup.alpha = 0f;
        hintText.text = message;
        hintGroup.gameObject.SetActive(true);

        hintTween = hintGroup
            .DOFade(1f, fadeInDuration)
            .SetEase(Ease.OutSine)
            .SetUpdate(true)
            .SetLink(hintGroup.gameObject)
            .OnComplete(() =>
            {
                hintTween = hintGroup
                    .DOFade(0.3f, blinkDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(hintGroup.gameObject);
            });
    }

    private void Hide()
    {
        if (!isShowing)
            return;

        isShowing = false;
        currentMessage = null;

        hintTween?.Kill();
        hintTween = null;

        hintGroup.alpha = 0f;
        hintGroup.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        hintTween?.Kill();

        if (Instance == this)
            Instance = null;
    }
}