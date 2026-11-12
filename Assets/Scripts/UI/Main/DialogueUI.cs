using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DialogueUI : MonoBehaviour
{
    [Header("UI")] [SerializeField] private TMP_Text dialogueText;

    [Header("출력")] [SerializeField, Min(1f)]
    private float charactersPerSecond = 25f;
    
    [Header("완료 이벤트")] [SerializeField] private UnityEvent onDialogueFinished;
    [Header("완료 이벤트")] [SerializeField] private UnityEvent onDialogueFinishedFront;

    private IReadOnlyList<DialogueData> lines;
    private int currentIndex;
    private int characterCount;
    private Coroutine typingCoroutine;

    private bool isPlaying;
    private bool isTyping;

    public bool IsPlaying => isPlaying;

    [SerializeField] private DialogueHistoryUI dialogueHistoryUI;

    [Header("대사 효과음")] [SerializeField] private AudioSource dialogueSoundSource;
    [SerializeField] private AudioClip dialogueSound;

    [SerializeField, Range(0f, 1f)] private float dialogueSoundVolume = 0.4f;

    [SerializeField, Min(1)] private int soundEveryCharacters = 2;
    
    [Header("화자별 대사음 피치")]
    [SerializeField, Range(0.1f, 3f)]
    private float daughterPitch = 1.15f;

    [SerializeField, Range(0.1f, 3f)]
    private float fatherPitch = 0.85f;

    [SerializeField, Range(0.1f, 3f)]
    private float policePitch = 0.95f;

    [SerializeField, Range(0.1f, 3f)]
    private float defaultPitch = 1f;
    
    // 화면에 실제로 표시한 대사의 인덱스.
    private int displayedLineIndex = -1;

    public void Play(IReadOnlyList<DialogueData> dialogues)
    {
        StopTyping();
        isPlaying = false;

        // 이전 대사 표시 정보 초기화.
        displayedLineIndex = -1;

        if (dialogueText == null)
        {
            Debug.LogError("Dialogue Text를 연결해주세요.", this);
            return;
        }

        if (dialogues == null || dialogues.Count == 0)
        {
            lines = null;
            dialogueText.text = "";

            Debug.LogWarning("출력할 대사가 없습니다.", this);
            return;
        }

        lines = dialogues;
        currentIndex = 0;
        isPlaying = true;

        ShowCurrentLine();
    }

    // 다음 버튼의 OnClick에 연결
    public void Advance()
    {
        if (!isPlaying)
            return;

        if (isTyping)
        {
            StopTyping();
            dialogueText.maxVisibleCharacters = characterCount;
            return;
        }

        currentIndex++;

        if (currentIndex >= lines.Count)
        {
            isPlaying = false;

            if (GameManager.Instance.isWatched[GameManager.Instance.currentChapterIdx])
            {
                onDialogueFinished?.Invoke();
                return;
            }

            if (!GameManager.Instance.IsFront)
            {
                GameManager.Instance.MarkCurrentChapterWatched();
                onDialogueFinished?.Invoke();
            }
            else
            {
                // GameManager.Instance.isWatched[GameManager.Instance.currentChapterIdx] = true;
                onDialogueFinishedFront?.Invoke();
            }

            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        StopTyping();

        displayedLineIndex = currentIndex;

        DialogueData line = lines[currentIndex];
        
        SetDialoguePitch(line.Speaker);

        dialogueText.text = line.GetText();
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        characterCount = dialogueText.textInfo.characterCount;

        // 번역된 문자열 대신 대사 데이터 자체를 기록.
        if (dialogueHistoryUI != null)
            dialogueHistoryUI.RecordLine(line);

        AnimationManager.Instance.PlayAnimation(line.ID);

        if (characterCount > 0)
        {
            isTyping = true;
            typingCoroutine = StartCoroutine(TypeCharacters());
        }
    }

    private IEnumerator TypeCharacters()
    {
        float visibleCount = 0f;
        int charactersSinceSound = 0;

        while (dialogueText.maxVisibleCharacters < characterCount)
        {
            if (PauseMenuUI.IsOpen)
            {
                StopDialogueSound();
                yield return null;
                continue;
            }

            int previousCount = dialogueText.maxVisibleCharacters;

            visibleCount += Mathf.Max(1f, charactersPerSecond)
                            * Time.unscaledDeltaTime;

            int nextCount = Mathf.Min(
                Mathf.FloorToInt(visibleCount),
                characterCount
            );

            dialogueText.maxVisibleCharacters = nextCount;

            bool shouldPlaySound = false;

            // 이번 프레임에 새로 표시된 글자만 확인.
            for (int i = previousCount; i < nextCount; i++)
            {
                char character =
                    dialogueText.textInfo.characterInfo[i].character;

                if (char.IsWhiteSpace(character) ||
                    char.IsPunctuation(character) ||
                    char.IsControl(character))
                {
                    continue;
                }

                charactersSinceSound++;

                if (charactersSinceSound >= Mathf.Max(1, soundEveryCharacters))
                {
                    charactersSinceSound = 0;
                    shouldPlaySound = true;
                }
            }

            // 한 프레임에 여러 글자가 나와도 소리는 한 번만 재생.
            if (shouldPlaySound)
                PlayDialogueSound();

            yield return null;
        }

        StopDialogueSound();

        isTyping = false;
        typingCoroutine = null;
    }

    private void PlayDialogueSound()
    {
        if (dialogueSoundSource == null || dialogueSound == null)
            return;

        // 이전 대사음과 겹치지 않도록 처음부터 재생.
        dialogueSoundSource.Stop();
        dialogueSoundSource.clip = dialogueSound;
        dialogueSoundSource.loop = false;
        dialogueSoundSource.volume = dialogueSoundVolume;
        dialogueSoundSource.Play();
    }

    private void StopDialogueSound()
    {
        if (dialogueSoundSource != null)
            dialogueSoundSource.Stop();
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        StopDialogueSound();
    }

    private void SetDialoguePitch(string speaker)
    {
        if (dialogueSoundSource == null)
            return;

        switch (speaker?.Trim().ToLowerInvariant())
        {
            case "daughter":
                dialogueSoundSource.pitch = daughterPitch;
                break;

            case "father":
                dialogueSoundSource.pitch = fatherPitch;
                break;

            case "police":
                dialogueSoundSource.pitch = policePitch;
                break;

            default:
                dialogueSoundSource.pitch = defaultPitch;
                break;
        }
    }
    
    private void OnEnable()
    {
        SettingsUI.LanguageChanged += OnLanguageChanged;

        // 비활성 상태에서 언어가 변경됐어도 다시 켜질 때 반영.
        OnLanguageChanged();
    }

    private void OnLanguageChanged()
    {
        if (dialogueText == null ||
            lines == null ||
            displayedLineIndex < 0 ||
            displayedLineIndex >= lines.Count)
        {
            return;
        }

        StopTyping();

        // 재생이 끝났어도 화면에 남아 있는 대사를 번역.
        DialogueData line = lines[displayedLineIndex];

        dialogueText.text = line.GetText();
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueText.ForceMeshUpdate();

        characterCount = dialogueText.textInfo.characterCount;

        // 진행 인덱스와 재생 상태는 변경하지 않는다.
        // 애니메이션, 기록 추가, 완료 이벤트도 실행하지 않는다.
    }

    private void OnDisable()
    {
        SettingsUI.LanguageChanged -= OnLanguageChanged;

        StopTyping();
        isPlaying = false;
    }
}