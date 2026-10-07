using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private ToggleCameraUI toggleCameraUI;
    [SerializeField] private Button slideGuideImageButton;
    [SerializeField] private GuideImageUI guideImageUI;
    [SerializeField] private Button onNoteBookButton;
    [SerializeField] private NoteUI noteBookUI;
    
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
}
