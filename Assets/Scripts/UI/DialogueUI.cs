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

    [SerializeField] private bool useEnglish;

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

    public void Play(IReadOnlyList<DialogueData> dialogues)
    {
        StopTyping();
        isPlaying = false;

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
                GameManager.Instance.isWatched[GameManager.Instance.currentChapterIdx] = true;
                onDialogueFinished?.Invoke();
            }
            else
            {
                GameManager.Instance.isWatched[GameManager.Instance.currentChapterIdx] = true;
                onDialogueFinishedFront?.Invoke();
            }

            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        StopTyping();

        DialogueData line = lines[currentIndex];

        // 예시: 실제 화자 필드 이름이 Speaker인 경우
        SetDialoguePitch(line.Speaker);

        string text = useEnglish && !string.IsNullOrEmpty(line.Text_En)
            ? line.Text_En
            : line.Text_Kr;

        dialogueText.text = text ?? "";
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        characterCount = dialogueText.textInfo.characterCount;

        // 재생한 순서대로 추가. 같은 대사도 다시 보면 추가한다.
        if (dialogueHistoryUI != null)
            dialogueHistoryUI.RecordLine(text);

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

    private void OnDisable()
    {
        StopTyping();
        isPlaying = false;
    }
}