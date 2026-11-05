using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class EndingDialogueUI : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("대사 데이터")]
    [SerializeField] private string dialogueId = "Ending";

    [Header("자동 출력")]
    [SerializeField, Min(1f)]
    private float charactersPerSecond = 25f;

    [Tooltip("한 줄 출력 완료 후 유지 시간")]
    [SerializeField, Min(0f)]
    private float lineHoldDuration = 2f;

    [Tooltip("마지막 줄 출력 완료 후 유지 시간")]
    [SerializeField, Min(0f)]
    private float lastLineHoldDuration = 4f;

    [Header("완료")]
    [SerializeField] private UnityEvent onFinished = new();

    private Coroutine playCoroutine;
    private bool hasStarted;
    private bool ownsPlayback;

    private DialogueData currentLine;
    private int characterCount;
    private float visibleCount;
    private float holdElapsed;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";
    }

    private void OnEnable()
    {
        SettingsUI.LanguageChanged += OnLanguageChanged;
    }

    public void Play()
    {
        if (hasStarted || IsPlaying)
            return;

        if (panel == null || dialogueText == null)
        {
            Debug.LogError(
                "엔딩 대사 Panel과 Text를 연결해주세요.",
                this);
            return;
        }

        if (!DialogueRepository.Instance.TryGetChapter(
                dialogueId, out var dialogues) ||
            dialogues == null ||
            dialogues.Count == 0)
        {
            Debug.LogError(
                $"엔딩 대사를 찾을 수 없습니다: {dialogueId}",
                this);
            return;
        }

        hasStarted = true;
        ownsPlayback = true;
        IsPlaying = true;

        panel.SetActive(true);

        playCoroutine = StartCoroutine(PlayRoutine(dialogues));
    }

    private IEnumerator PlayRoutine(
        IReadOnlyList<DialogueData> dialogues)
    {
        for (int i = 0; i < dialogues.Count; i++)
        {
            currentLine = dialogues[i];

            ApplyCurrentLine(false);

            // 입력으로 넘길 수 없는 자동 타이핑.
            while (dialogueText.maxVisibleCharacters < characterCount)
            {
                if (PauseMenuUI.IsOpen)
                {
                    yield return null;
                    continue;
                }

                visibleCount +=
                    Mathf.Max(1f, charactersPerSecond) * Time.deltaTime;

                dialogueText.maxVisibleCharacters = Mathf.Min(
                    Mathf.FloorToInt(visibleCount),
                    characterCount);

                yield return null;
            }

            bool isLastLine = i == dialogues.Count - 1;

            float holdDuration = Mathf.Max(
                0f,
                isLastLine
                    ? lastLineHoldDuration
                    : lineHoldDuration);

            holdElapsed = 0f;

            while (holdElapsed < holdDuration || PauseMenuUI.IsOpen)
            {
                if (!PauseMenuUI.IsOpen)
                    holdElapsed += Time.deltaTime;

                yield return null;
            }
        }

        playCoroutine = null;
        currentLine = null;

        // 패널을 닫기 전에 재생 상태 해제.
        ReleasePlayback();

        dialogueText.text = "";
        panel.SetActive(false);

        onFinished?.Invoke();
    }

    private void ApplyCurrentLine(bool showImmediately)
    {
        dialogueText.text = currentLine.GetText();

        dialogueText.maxVisibleCharacters =
            showImmediately ? int.MaxValue : 0;

        dialogueText.ForceMeshUpdate();

        characterCount = dialogueText.textInfo.characterCount;
        visibleCount = showImmediately ? characterCount : 0f;
        holdElapsed = 0f;

        if (showImmediately)
            dialogueText.maxVisibleCharacters = characterCount;
    }

    private void OnLanguageChanged()
    {
        if (!ownsPlayback ||
            currentLine == null ||
            dialogueText == null)
        {
            return;
        }

        // 언어 변경 시 현재 문장을 전체 표시하고 유지 시간 초기화.
        ApplyCurrentLine(true);
    }

    private void ReleasePlayback()
    {
        if (!ownsPlayback)
            return;

        ownsPlayback = false;
        IsPlaying = false;
    }

    private void OnDisable()
    {
        SettingsUI.LanguageChanged -= OnLanguageChanged;

        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        currentLine = null;

        ReleasePlayback();

        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }
}