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
    [SerializeField] private PauseMenuUI pauseMenuUI;

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
    
    private bool isDialogueHidden = true;
    
    private Vector3 chapterButtonsExitOffset;

    protected override void Awake()
    {
        base.Awake();

        slideGuideImageButton.onClick.AddListener(OnClickSlideGuideImage);
        onNoteBookButton.onClick.AddListener(OnClickOnNoteBook);
    }

    public void InitializeChapterButtons()
    {
        chapterOriginalPositions.Clear();

        foreach (GameObject button in chapterButtons)
        {
            if (button == null)
                continue;

            RectTransform rect = button.GetComponent<RectTransform>();

            if (rect != null)
                chapterOriginalPositions[rect] = rect.localPosition;
        }

        for (int i = 0; i < chapterButtons.Length; i++)
        {
            if (chapterButtons[i] == null)
                continue;

            chapterButtons[i].SetActive(
                GameManager.Instance.CanShowChapterButton(i));
        }
    }

    private void OnClickOnNoteBook()
    {
        isNoteBookOpen = !isNoteBookOpen;
        noteBookUI.gameObject.SetActive(isNoteBookOpen);
    }

    public void OnAdvanceDialogue()
    {
        if (GetIsOnSkyView() || isDialogueHidden)
            return;

        dialogueUI.Advance();
    }

    public void StartDialogue(IReadOnlyList<DialogueData> dialogues)
    {
        dialogueUI.Play(dialogues);

        if (GetIsOnSkyView())
            GetOutDialogueUI();
        else
            GetInDialogueUI();
    }

    public void OnClickSlideGuideImage()
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
        GetOutDialogueUI();
        
        guideImageUI.HideGuideImage();
    }

    public void GetOutDialogueUI()
    {
        // 예약된 퇴장이나 기존 이동을 취소
        dialogueRectTransform.DOKill();

        isDialogueHidden = true;

        dialogueRectTransform
            .DOAnchorPos(dialogueOutPostition, 0.5f)
            .SetEase(Ease.InOutSine);
    }
    public void GetInDialogueUI()
    {
        dialogueRectTransform.DOKill();

        isDialogueHidden = false;

        dialogueRectTransform
            .DOAnchorPos(dialoguePostition, 0.5f)
            .SetEase(Ease.InOutSine);
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
        
        chapterButtonsExitOffset = moveOffset;

        // 시작할 때 설정해야 퇴장 도중에도 복귀 가능
        chapterButtonsAreOut = true;
        chapterButtonsSequence = DOTween.Sequence();

        foreach (var pair in chapterOriginalPositions)
        {
            RectTransform rect = pair.Key;

            if (rect == null || !rect.gameObject.activeSelf)
                continue;

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

        chapterButtonsSequence?.Kill();
        chapterButtonsSequence = DOTween.Sequence();

        // 표시할 별이 없어도 완료 처리가 실행되도록 유지.
        chapterButtonsSequence.AppendInterval(0.7f);

        var shownGroups = new List<CanvasGroup>();

        for (int i = 0; i < chapterButtons.Length; i++)
        {
            GameObject button = chapterButtons[i];

            if (button == null)
                continue;

            if (!GameManager.Instance.CanShowChapterButton(i))
            {
                button.SetActive(false);
                continue;
            }
            
            ChapterButton chapterButton = chapterButtons[i].GetComponent<ChapterButton>();
            if (GameManager.Instance.isWatched[i])
            {
                chapterButton.SetThumbnail();
            }

            RectTransform rect = button.GetComponent<RectTransform>();

            if (rect == null ||
                !chapterOriginalPositions.TryGetValue(rect, out Vector3 originalPosition))
            {
                continue;
            }

            CanvasGroup group = GetChapterCanvasGroup(rect);

            rect.DOKill();
            group.DOKill();

            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // 비활성화된 별은 저장된 위치의 위쪽에서 등장.
            // 새로 해금된 별도 동일하게 처리.
            if (!button.activeSelf)
            {
                rect.localPosition = originalPosition;
                rect.position += chapterButtonsExitOffset;
            }

            button.SetActive(true);
            shownGroups.Add(group);

            chapterButtonsSequence.Insert(
                0f,
                rect.DOLocalMove(originalPosition, 0.7f)
                    .SetEase(Ease.OutCubic)
            );
        }

        chapterButtonsSequence.OnComplete(() =>
        {
            foreach (CanvasGroup group in shownGroups)
            {
                if (group == null)
                    continue;

                group.interactable = true;
                group.blocksRaycasts = true;
            }

            chapterButtonsAreOut = false;
            chapterButtonsSequence = null;
        });
    }
    
    public void ReplaceGuideImage(Sprite sprite)
    {
        guideImageUI.ReplaceGuideImage(sprite);
    }

    public void ChangeGuideImage(Sprite sprite)
    {
        guideImageUI.ChangeGuideImage(sprite);
    }

    public bool IsGuideHidden()
    {
        return guideImageUI.isGuideHidden;
    }

    public void ChangeGuideName(string constellationName)
    {
        guideImageUI.ChangeGuideName(constellationName);
    }

    private CanvasGroup GetChapterCanvasGroup(RectTransform rect)
    {
        CanvasGroup group = rect.GetComponent<CanvasGroup>();

        if (group == null)
            group = rect.gameObject.AddComponent<CanvasGroup>();

        return group;
    }

    public void HandleEscape()
    {
        pauseMenuUI.HandleEscape();
    }

    public void SetChapterPlaying(bool isPlaying)
    {
        pauseMenuUI.SetChapterPlaying(isPlaying);
    }
}