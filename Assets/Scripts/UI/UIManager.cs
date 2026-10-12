using System;
using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private RectTransform dialogueRectTransform;

    [SerializeField] private ToggleCameraUI toggleCameraUI;
    [SerializeField] private Button slideGuideImageButton;
    [SerializeField] private GuideImageUI guideImageUI;
    [SerializeField] private Button onNoteBookButton;
    [SerializeField] private NoteUI noteBookUI;
    [SerializeField] private ChapterChangeUI chapterChangeUI;

    Vector2 dialoguePostition = new Vector2(0, 15);
    Vector2 dialogueOutPostition = new Vector2(0, -120);

    [SerializeField] private RectTransform buttonsRectTransform;
    [SerializeField] private Vector2 buttonsInPosition;
    [SerializeField] private Vector2 buttonsOutPosition;

    [SerializeField] private GameObject[] chapterButtons;

    private readonly Dictionary<RectTransform, Vector3> chapterOriginalPositions
        = new Dictionary<RectTransform, Vector3>();

    private Sequence chapterButtonsSequence;
    private bool chapterButtonsAreOut;

    private bool isNoteBookOpen = false;

    protected override void Awake()
    {
        base.Awake();

        slideGuideImageButton.onClick.AddListener(OnClickSlideGuideImage);
        onNoteBookButton.onClick.AddListener(OnClickOnNoteBook);
    }

    private void OnClickOnNoteBook()
    {
        isNoteBookOpen = !isNoteBookOpen;
        noteBookUI.gameObject.SetActive(isNoteBookOpen);
    }

    public void OnAdvanceDialogue()
    {
        dialogueUI.Advance();
    }

    public void StartDialogue(IReadOnlyList<DialogueData> dialogues)
    {
        dialogueRectTransform.DOAnchorPos(
            dialoguePostition,
            0.5f
        ).SetEase(Ease.InOutSine);

        dialogueUI.Play(dialogues);
    }

    private void OnClickSlideGuideImage()
    {
        guideImageUI.SlideGuideImage();
    }

    public bool GetIsOnSkyView()
    {
        return toggleCameraUI.GetIsOnSkyView();
    }

    public void ToggleCamera()
    {
        toggleCameraUI.OnClickLookUpCameraButton();
    }

    public bool IsPlayingDialogue()
    {
        return dialogueUI.IsPlaying;
    }

    public void SetTitle(string title, string subtitle)
    {
        chapterChangeUI.SetTitle(title, subtitle);
    }

    public void FadeInTitle(Action onFinished = null,
        Action onFadeOutStarted = null)
    {
        chapterChangeUI.FadeInTitle(onFinished, onFadeOutStarted);
    }

    public void GetInUIs()
    {
        GetInButtons();
        GetInSubTitle();
    }

    public void GetOutUIs()
    {
        GetOutButtons();
        GetOutSubTitle();
        GetOutDialogueUI(0f);
    }

    public void GetOutDialogueUI(float delay = 0f)
    {
        dialogueRectTransform.DOAnchorPos(
            dialogueOutPostition,
            0.5f
        ).SetEase(Ease.InOutSine).SetDelay(delay);
    }

    private void GetInButtons()
    {
        buttonsRectTransform.DOAnchorPos(
            buttonsInPosition,
            0.5f
        ).SetEase(Ease.InOutSine);
    }

    private void GetOutButtons()
    {
        buttonsRectTransform.DOAnchorPos(
            buttonsOutPosition,
            0.5f
        ).SetEase(Ease.InOutSine);
    }

    private void GetInSubTitle()
    {
        chapterChangeUI.GetInSubTitle();
    }

    private void GetOutSubTitle()
    {
        chapterChangeUI.GetOutSubTitle();
    }

    public void SlideOutChapterButtons()
    {
        if (chapterButtonsAreOut)
            return;

        chapterButtonsSequence?.Kill();
        chapterOriginalPositions.Clear();

        Vector3 moveOffset = Vector3.zero;

        // 원래 위치 저장 + 공통 이동 거리 계산
        foreach (var button in chapterButtons)
        {
            if (button == null || !button.activeSelf)
                continue;

            RectTransform rect = button.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            chapterOriginalPositions[rect] = rect.localPosition;

            Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            Bounds bounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    canvasRect, rect);

            float moveY = Mathf.Max(
                0f,
                canvasRect.rect.yMax - bounds.min.y + 100f);

            Vector3 offset = canvasRect.TransformVector(
                new Vector3(0f, moveY, 0f));

            if (offset.sqrMagnitude > moveOffset.sqrMagnitude)
                moveOffset = offset;
        }

        if (chapterOriginalPositions.Count == 0)
            return;

        // 시작할 때 설정해야 퇴장 도중에도 복귀 가능
        chapterButtonsAreOut = true;
        chapterButtonsSequence = DOTween.Sequence();

        foreach (var pair in chapterOriginalPositions)
        {
            RectTransform rect = pair.Key;
            CanvasGroup group = GetChapterCanvasGroup(rect);

            rect.DOKill();
            group.DOKill();

            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            chapterButtonsSequence.Join(
                rect.DOMove(rect.position + moveOffset, 0.7f)
                    .SetEase(Ease.InCubic)
            );
        }

        chapterButtonsSequence.OnComplete(() =>
        {
            foreach (var pair in chapterOriginalPositions)
            {
                if (pair.Key != null)
                    pair.Key.gameObject.SetActive(false);
            }

            chapterButtonsSequence = null;
        });
    }

    public void SlideInChapterButtons()
    {
        if (!chapterButtonsAreOut)
            return;

        // 나가는 도중 호출해도 현재 위치에서 돌아온다.
        chapterButtonsSequence?.Kill();
        chapterButtonsSequence = DOTween.Sequence();

        foreach (var pair in chapterOriginalPositions)
        {
            RectTransform rect = pair.Key;

            if (rect == null)
                continue;

            CanvasGroup group = GetChapterCanvasGroup(rect);

            rect.DOKill();
            group.DOKill();

            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            rect.gameObject.SetActive(true);

            chapterButtonsSequence.Join(
                rect.DOLocalMove(pair.Value, 0.7f)
                    .SetEase(Ease.OutCubic)
            );
        }

        chapterButtonsSequence.OnComplete(() =>
        {
            foreach (var pair in chapterOriginalPositions)
            {
                if (pair.Key == null)
                    continue;

                CanvasGroup group = GetChapterCanvasGroup(pair.Key);
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            chapterButtonsAreOut = false;
            chapterButtonsSequence = null;
        });
    }

    private CanvasGroup GetChapterCanvasGroup(RectTransform rect)
    {
        CanvasGroup group = rect.GetComponent<CanvasGroup>();

        if (group == null)
            group = rect.gameObject.AddComponent<CanvasGroup>();

        return group;
    }
}