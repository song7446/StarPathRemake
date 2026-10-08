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

    public void OutDialogueUI()
    {
        dialogueRectTransform.DOAnchorPos(
            dialogueOutPostition,
            0.5f
        ).SetEase(Ease.InOutSine).SetDelay(3f);
    }
}