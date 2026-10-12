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
    }

    public void GetOutDialogueUI()
    {
        dialogueRectTransform.DOAnchorPos(
            dialogueOutPostition,
            0.5f
        ).SetEase(Ease.InOutSine).SetDelay(3f);
    }

    public void GetInButtons()
    {
        buttonsRectTransform.DOAnchorPos(
            buttonsInPosition,
            0.5f
        ).SetEase(Ease.InOutSine);
    }

    public void GetOutButtons()
    {
        buttonsRectTransform.DOAnchorPos(
            buttonsOutPosition,
            0.5f
        ).SetEase(Ease.InOutSine);
    }

    public void GetInSubTitle()
    {
        chapterChangeUI.GetInSubTitle();   
    }
    
    public void GetOutSubTitle()
    {
        chapterChangeUI.GetOutSubTitle();   
    }

    public void SlideOutChapterButtons()
    {
        foreach (var button in chapterButtons)
        {
            if (button == null || !button.activeSelf)
                continue;

            RectTransform rect = button.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            CanvasGroup group = button.GetComponent<CanvasGroup>();

            if (group == null)
                group = button.AddComponent<CanvasGroup>();

            group.DOKill();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            rect.DOKill();

            Vector3 originalPosition = rect.localPosition;

            Bounds bounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    canvasRect, rect);

            // 가장 아래쪽까지 화면 위로 완전히 빠지도록 이동
            float moveY = Mathf.Max(
                0f,
                canvasRect.rect.yMax - bounds.min.y + 100f);

            Vector3 worldOffset = canvasRect.TransformVector(
                new Vector3(0f, moveY, 0f));

            rect.DOMove(rect.position + worldOffset, 0.7f)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    button.SetActive(false);

                    rect.localPosition = originalPosition;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                });
        }
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