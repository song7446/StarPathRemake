using DG.Tweening;
using TMPro;
using UnityEngine;

public class StarConnectionHintUI : MonoBehaviour
{
    public static StarConnectionHintUI Instance { get; private set; }

    [Header("공통 안내 그룹")]
    [SerializeField] private CanvasGroup hintGroup;

    [Header("안내 텍스트")]
    [SerializeField] private TMP_Text connectHintText;
    [SerializeField] private TMP_Text cancelHintText;
    [SerializeField] private TMP_Text notebookHintText;

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
    private TMP_Text currentHint;

    private void Awake()
    {
        Instance = this;

        connectHintText.raycastTarget = false;
        cancelHintText.raycastTarget = false;
        notebookHintText.raycastTarget = false;

        hintGroup.interactable = false;
        hintGroup.blocksRaycasts = false;

        Hide();
    }

    private void Update()
    {
        RefreshHint();
    }

    private void RefreshHint()
    {
        if (UIManager.Instance == null ||
            GameManager.Instance == null)
        {
            Hide();
            return;
        }

        // 챕터 플레이 중이 아니거나 준비 중이면 안내를 숨긴다.
        if (!GameManager.Instance.IsChapterReady ||
            !UIManager.Instance.GetIsOnSkyView() ||
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
                ? cancelHintText
                : connectHintText);

            return;
        }

        if (chapterIndex == secondChapterIndex &&
            !hasOpenedNotebook)
        {
            Show(notebookHintText);
            return;
        }

        Hide();
    }

    // 실제로 새로운 선이 완성됐을 때 호출.
    public void NotifyLineConnected()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.currentChapterIdx != firstChapterIndex ||
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
        if (GameManager.Instance == null ||
            GameManager.Instance.currentChapterIdx != firstChapterIndex)
        {
            return;
        }

        firstPuzzleCompleted = true;
        Hide();
    }

    // 챕터와 관계없이 수첩을 처음 열었을 때부터 기록.
    public void NotifyNotebookOpened()
    {
        hasOpenedNotebook = true;
        RefreshHint();
    }

    private void Show(TMP_Text targetHint)
    {
        if (targetHint == null)
        {
            Hide();
            return;
        }

        if (isShowing && currentHint == targetHint)
            return;

        hintTween?.Kill();
        hintTween = null;

        isShowing = true;
        currentHint = targetHint;

        hintGroup.alpha = 0f;

        // 문구와 폰트는 각 텍스트의 현지화 컴포넌트가 처리한다.
        connectHintText.gameObject.SetActive(
            targetHint == connectHintText);

        cancelHintText.gameObject.SetActive(
            targetHint == cancelHintText);

        notebookHintText.gameObject.SetActive(
            targetHint == notebookHintText);

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
        isShowing = false;
        currentHint = null;

        hintTween?.Kill();
        hintTween = null;

        if (connectHintText != null)
            connectHintText.gameObject.SetActive(false);

        if (cancelHintText != null)
            cancelHintText.gameObject.SetActive(false);

        if (notebookHintText != null)
            notebookHintText.gameObject.SetActive(false);

        if (hintGroup != null)
        {
            hintGroup.alpha = 0f;
            hintGroup.gameObject.SetActive(false);
        }
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