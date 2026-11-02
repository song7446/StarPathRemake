using System.Collections;
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
    [SerializeField] private bool useEnglish;

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

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";
    }

    public void Play()
    {
        if (hasStarted || IsPlaying)
            return;

        if (panel == null || dialogueText == null)
        {
            Debug.LogError("엔딩 대사 Panel과 Text를 연결해주세요.", this);
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
        System.Collections.Generic.IReadOnlyList<DialogueData> dialogues)
    {
        for (int i = 0; i < dialogues.Count; i++)
        {
            DialogueData line = dialogues[i];

            string text = useEnglish &&
                          !string.IsNullOrEmpty(line.Text_En)
                ? line.Text_En
                : line.Text_Kr;

            dialogueText.text = text ?? "";
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int characterCount = dialogueText.textInfo.characterCount;
            float visibleCount = 0f;

            // 입력으로 즉시 출력하거나 넘길 수 없는 자동 타이핑.
            while (dialogueText.maxVisibleCharacters < characterCount)
            {
                if (PauseMenuUI.IsOpen)
                {
                    yield return null;
                    continue;
                }

                visibleCount +=
                    charactersPerSecond * Time.deltaTime;

                dialogueText.maxVisibleCharacters = Mathf.Min(
                    Mathf.FloorToInt(visibleCount),
                    characterCount
                );

                yield return null;
            }

            bool isLastLine = i == dialogues.Count - 1;

            float holdDuration = isLastLine
                ? lastLineHoldDuration
                : lineHoldDuration;

            float elapsed = 0f;

            while (elapsed < holdDuration || PauseMenuUI.IsOpen)
            {
                if (!PauseMenuUI.IsOpen)
                    elapsed += Time.deltaTime;

                yield return null;
            }
        }

        playCoroutine = null;

        dialogueText.text = "";
        panel.SetActive(false);

        ReleasePlayback();

        onFinished?.Invoke();
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
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        ReleasePlayback();

        if (panel != null)
            panel.SetActive(false);
    }
}