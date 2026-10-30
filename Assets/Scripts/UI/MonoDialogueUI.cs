using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MonologueUI : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text monologueText;
    [SerializeField] private Button nextButton;

    [Header("출력")]
    [SerializeField, Min(1f)]
    private float charactersPerSecond = 25f;

    [SerializeField] private bool useEnglish;

    private IReadOnlyList<DialogueData> lines;
    private int currentIndex;
    private int characterCount;

    private bool isTyping;
    private Coroutine typingCoroutine;
    private Action onFinished;

    private void Awake()
    {
        IsPlaying = false;
        panel.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(Advance);
    }

    private void Update()
    {
        if (!IsPlaying || PauseMenuUI.IsOpen)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Advance();
        }
    }

    // Inspector의 버튼이나 UnityEvent에서 호출할 때 사용.
    public void Play(string chapterId)
    {
        Play(chapterId, null);
    }

    // 코드에서 종료 후 실행할 작업까지 전달할 때 사용.
    public void Play(string chapterId, Action finished)
    {
        if (IsPlaying)
            return;

        if (panel == null || monologueText == null)
        {
            Debug.LogError("독백 Panel과 Text를 연결해주세요.", this);
            return;
        }

        if (!DialogueRepository.Instance.TryGetChapter(
                chapterId,
                out IReadOnlyList<DialogueData> dialogues) ||
            dialogues.Count == 0)
        {
            Debug.LogWarning(
                $"독백 데이터를 찾을 수 없습니다: {chapterId}",
                this);

            return;
        }

        lines = dialogues;
        currentIndex = 0;
        onFinished = finished;

        IsPlaying = true;
        panel.SetActive(true);

        ShowCurrentLine();
    }

    public void Advance()
    {
        if (!IsPlaying || PauseMenuUI.IsOpen)
            return;

        // 출력 중 누르면 현재 문장을 즉시 완성.
        if (isTyping)
        {
            StopTyping();
            monologueText.maxVisibleCharacters = characterCount;
            return;
        }

        currentIndex++;

        if (currentIndex >= lines.Count)
        {
            Finish();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        StopTyping();

        DialogueData line = lines[currentIndex];

        string text = useEnglish &&
                      !string.IsNullOrEmpty(line.Text_En)
            ? line.Text_En
            : line.Text_Kr;

        monologueText.text = text ?? "";
        monologueText.maxVisibleCharacters = 0;
        monologueText.ForceMeshUpdate();

        characterCount = monologueText.textInfo.characterCount;

        if (characterCount > 0)
        {
            isTyping = true;
            typingCoroutine = StartCoroutine(TypeCharacters());
        }
    }

    private IEnumerator TypeCharacters()
    {
        float visibleCount = 0f;

        while (monologueText.maxVisibleCharacters < characterCount)
        {
            if (PauseMenuUI.IsOpen)
            {
                yield return null;
                continue;
            }

            visibleCount += charactersPerSecond * Time.unscaledDeltaTime;

            monologueText.maxVisibleCharacters = Mathf.Min(
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

    private void Finish()
    {
        Action callback = onFinished;

        CloseWithoutCallback();

        callback?.Invoke();
    }

    // 강제로 닫을 때는 완료 콜백을 실행하지 않는다.
    public void CloseWithoutCallback()
    {
        StopTyping();

        IsPlaying = false;
        lines = null;
        onFinished = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDisable()
    {
        CloseWithoutCallback();
    }
}