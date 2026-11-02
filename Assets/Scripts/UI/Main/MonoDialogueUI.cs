using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MonologueUI : MonoBehaviour
{
    private static MonologueUI activeMonologue;

    public static bool IsPlaying => activeMonologue != null;

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
    
    [SerializeField] private bool keepPanelOnFinished;
    
    [Header("초기 화면")]
    [SerializeField] private bool showPanelOnAwake;
    
    [Header("타이핑 효과음")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingClip;

    [SerializeField, Range(0f, 1f)]
    private float typingVolume = 0.4f;

    [SerializeField, Range(0.5f, 2f)]
    private float typingPitch = 0.85f;

    [SerializeField, Min(1)]
    private int soundEveryCharacters = 2;

    private void Awake()
    {
        // 인트로는 처음부터 검은 배경을 표시한다.
        // 대사는 Play()를 호출할 때 출력한다.
        monologueText.text = "";
        monologueText.maxVisibleCharacters = 0;

        panel.SetActive(showPanelOnAwake);

        if (nextButton != null)
            nextButton.onClick.AddListener(Advance);
    }

    private void Update()
    {
        // 현재 재생 중인 UI만 입력 처리.
        if (activeMonologue != this || PauseMenuUI.IsOpen)
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

        activeMonologue = this;
        panel.SetActive(true);

        ShowCurrentLine();
    }

    public void Advance()
    {
        if (activeMonologue != this ||
            PauseMenuUI.IsOpen ||
            lines == null)
        {
            return;
        }

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
        int charactersSinceSound = 0;

        while (monologueText.maxVisibleCharacters < characterCount)
        {
            if (PauseMenuUI.IsOpen)
            {
                StopTypingSound();
                yield return null;
                continue;
            }

            int previousCount = monologueText.maxVisibleCharacters;

            visibleCount += Mathf.Max(1f, charactersPerSecond)
                            * Time.unscaledDeltaTime;

            int nextCount = Mathf.Min(
                Mathf.FloorToInt(visibleCount),
                characterCount
            );

            monologueText.maxVisibleCharacters = nextCount;

            bool shouldPlaySound = false;

            for (int i = previousCount; i < nextCount; i++)
            {
                char character =
                    monologueText.textInfo.characterInfo[i].character;

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

            // 여러 글자가 한 프레임에 출력돼도 소리는 한 번만 재생.
            if (shouldPlaySound)
                PlayTypingSound();

            yield return null;
        }

        StopTypingSound();

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
        StopTypingSound();
    }

    private void Finish()
    {
        Action callback = onFinished;
        onFinished = null;

        StopTyping();
        lines = null;

        // 인트로는 화면과 입력 차단 상태를 유지한 채
        // 외부에서 페이드아웃을 진행한다.
        if (!keepPanelOnFinished)
        {
            CloseWithoutCallback();
        }

        callback?.Invoke();
    }

    // 강제로 닫을 때는 완료 콜백을 실행하지 않는다.
    public void CloseWithoutCallback()
    {
        StopTyping();

        if (activeMonologue == this)
            activeMonologue = null;

        lines = null;
        onFinished = null;

        if (panel != null)
            panel.SetActive(false);
    }
    
    private void PlayTypingSound()
    {
        if (typingAudioSource == null || typingClip == null)
            return;

        typingAudioSource.Stop();
        typingAudioSource.clip = typingClip;
        typingAudioSource.loop = false;
        typingAudioSource.volume = typingVolume;
        typingAudioSource.pitch = typingPitch;
        typingAudioSource.Play();
    }

    private void StopTypingSound()
    {
        if (typingAudioSource != null)
            typingAudioSource.Stop();
    }

    private void OnDisable()
    {
        CloseWithoutCallback();
    }
}