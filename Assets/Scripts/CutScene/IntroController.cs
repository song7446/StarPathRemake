using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroController : MonoBehaviour
{
    [Header("독백")]
    [SerializeField] private MonologueUI monologueUI;
    [SerializeField] private string introDialogueId = "Intro";
    [SerializeField] private CanvasGroup monologueUIGroup;

    [Header("조작 안내")]
    [SerializeField] private GameObject spaceHint;

    [Header("종료 페이드")]
    [SerializeField, Min(0.01f)]
    private float fadeOutDuration = 2f;

    [Header("안내 깜빡임")]
    [SerializeField] private CanvasGroup spaceHintGroup;

    [SerializeField, Min(0.01f)]
    private float hintBlinkDuration = 0.8f;

    [Header("인트로 종료 후 안내")]
    [SerializeField] private FirstChapterHintUI firstChapterHint;

    private bool isWaitingForFirstSpace;
    private bool isFinishing;

    private Tween fadeTween;
    private Tween hintBlinkTween;
    private Coroutine selectionReadyCoroutine;

    private void Awake()
    {
        if (spaceHint != null)
            spaceHint.SetActive(false);
    }

    private IEnumerator Start()
    {
        // GameManager의 진행 기록 불러오기가 끝난 뒤 확인한다.
        yield return null;

        bool skipIntro =
            GameManager.Instance.HasPlayedMonologue(introDialogueId);

        // 이어하기에서는 씬 전환 화면 뒤에서 인트로를 숨긴다.
        if (skipIntro)
        {
            isFinishing = true;
            HideSpaceHint();
            CloseIntroPanel();

            selectionReadyCoroutine =
                StartCoroutine(PrepareChapterSelection());

            yield break;
        }

        // 씬 전환과 일시정지가 끝난 뒤 인트로를 시작한다.
        while (IsSceneTransitioning() || PauseMenuUI.IsOpen)
            yield return null;

        if (monologueUI == null || monologueUIGroup == null)
        {
            Debug.LogError(
                "인트로의 MonologueUI와 CanvasGroup을 연결해주세요.",
                this);

            FinishIntro();
            yield break;
        }

        if (!DialogueRepository.Instance.TryGetChapter(
                introDialogueId, out var dialogues) ||
            dialogues == null ||
            dialogues.Count == 0)
        {
            Debug.LogWarning(
                $"인트로 대사를 찾을 수 없습니다: {introDialogueId}",
                this);

            FinishIntro();
            yield break;
        }

        // 다른 독백이 재생 중이라면 끝날 때까지 기다린다.
        while (MonologueUI.IsPlaying || PauseMenuUI.IsOpen)
            yield return null;

        monologueUI.Play(introDialogueId, OnIntroCompleted);

        ShowSpaceHint();
    }

    private void Update()
    {
        if (!isWaitingForFirstSpace ||
            isFinishing ||
            PauseMenuUI.IsOpen ||
            Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            HideSpaceHint();
    }

    // 실제로 인트로 독백을 끝까지 봤을 때만 저장한다.
    private void OnIntroCompleted()
    {
        GameManager.Instance.MarkMonologuePlayed(introDialogueId);

        FinishIntro();
    }

    private void FinishIntro()
    {
        if (isFinishing)
            return;

        isFinishing = true;
        HideSpaceHint();

        if (monologueUIGroup == null)
        {
            OnIntroHidden();
            return;
        }

        fadeTween = monologueUIGroup
            .DOFade(0f, fadeOutDuration)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                fadeTween = null;
                OnIntroHidden();
            });
    }

    private void OnIntroHidden()
    {
        CloseIntroPanel();

        if (selectionReadyCoroutine == null)
        {
            selectionReadyCoroutine =
                StartCoroutine(PrepareChapterSelection());
        }
    }

    private void CloseIntroPanel()
    {
        if (monologueUIGroup != null)
        {
            monologueUIGroup.alpha = 0f;
            monologueUIGroup.interactable = false;
            monologueUIGroup.blocksRaycasts = false;
        }

        if (monologueUI != null)
            monologueUI.CloseWithoutCallback();
    }

    private IEnumerator PrepareChapterSelection()
    {
        // 패널을 닫은 프레임 이후에 다음 독백과 입력을 복원한다.
        yield return null;

        while (IsSceneTransitioning() ||
               PauseMenuUI.IsOpen ||
               MonologueUI.IsPlaying)
        {
            yield return null;
        }

        GameManager manager = GameManager.Instance;

        // 미시청 독백 또는 해금된 시간대 퍼즐을 복원한다.
        manager.OnChapterSelectionReady();

        // 복귀 직후 독백이 시작됐다면 끝날 때까지 기다린다.
        while (MonologueUI.IsPlaying || PauseMenuUI.IsOpen)
            yield return null;

        // 첫 장면을 아직 완료하지 않았다면 조작 안내를 표시한다.
        bool needsFirstChapterHint =
            manager.isWatched != null &&
            manager.isWatched.Length > 0 &&
            !manager.isWatched[0];

        if (needsFirstChapterHint && firstChapterHint != null)
            firstChapterHint.Show();

        selectionReadyCoroutine = null;

        Destroy(gameObject);
    }

    private bool IsSceneTransitioning()
    {
        return SceneFadeController.Instance != null &&
               SceneFadeController.Instance.IsTransitioning;
    }

    private void ShowSpaceHint()
    {
        isWaitingForFirstSpace = true;

        hintBlinkTween?.Kill();
        hintBlinkTween = null;

        if (spaceHint == null)
            return;

        spaceHint.SetActive(true);

        if (spaceHintGroup == null)
            return;

        spaceHintGroup.alpha = 1f;
        spaceHintGroup.interactable = false;
        spaceHintGroup.blocksRaycasts = false;

        hintBlinkTween = spaceHintGroup
            .DOFade(0.25f, hintBlinkDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(spaceHint);
    }

    private void HideSpaceHint()
    {
        isWaitingForFirstSpace = false;

        hintBlinkTween?.Kill();
        hintBlinkTween = null;

        if (spaceHint != null)
            spaceHint.SetActive(false);
    }

    private void OnDestroy()
    {
        hintBlinkTween?.Kill();
        fadeTween?.Kill();
    }
}