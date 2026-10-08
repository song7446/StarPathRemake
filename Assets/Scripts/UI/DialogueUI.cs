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

            if (!GameManager.Instance.IsFront)
            {
                onDialogueFinished?.Invoke();
            }
            else
            {
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

        AnimationManager.Instance.PlayAnimation(line.ID);

        string text = useEnglish && !string.IsNullOrEmpty(line.Text_En)
            ? line.Text_En
            : line.Text_Kr;

        dialogueText.text = text ?? "";
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        characterCount = dialogueText.textInfo.characterCount;

        if (characterCount > 0)
        {
            isTyping = true;
            typingCoroutine = StartCoroutine(TypeCharacters());
        }
    }

    private IEnumerator TypeCharacters()
    {
        float visibleCount = 0f;

        while (dialogueText.maxVisibleCharacters < characterCount)
        {
            visibleCount += Mathf.Max(1f, charactersPerSecond)
                            * Time.unscaledDeltaTime;

            dialogueText.maxVisibleCharacters = Mathf.Min(
                Mathf.FloorToInt(visibleCount),
                characterCount
            );

            yield return null;
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    private void OnDisable()
    {
        StopTyping();
        isPlaying = false;
    }
}