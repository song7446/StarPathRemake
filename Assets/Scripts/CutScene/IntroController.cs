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

    private bool isWaitingForFirstSpace;
    private bool isFinishing;
    private Tween fadeTween;
    
    [Header("안내 깜빡임")]
    [SerializeField] private CanvasGroup spaceHintGroup;

    [SerializeField, Min(0.01f)]
    private float hintBlinkDuration = 0.8f;

    private Tween hintBlinkTween;

    private void Awake()
    {
        if (spaceHint != null)
            spaceHint.SetActive(false);
    }

    private IEnumerator Start()
    {
        // 다른 컴포넌트의 Start 초기화 이후 실행.
        yield return null;

        // 씬 전환의 검은 화면이 걷힌 뒤 시작.
        while (SceneFadeController.Instance != null &&
               SceneFadeController.Instance.IsTransitioning)
        {
            yield return null;
        }

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

        monologueUI.Play(introDialogueId, FinishIntro);

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
    
    private void FinishIntro()
    {
        if (isFinishing)
            return;

        isFinishing = true;
        HideSpaceHint();

        if (monologueUIGroup == null)
        {
            if (monologueUI != null)
                monologueUI.CloseWithoutCallback();

            Destroy(gameObject);
            return;
        }

        fadeTween = monologueUIGroup
            .DOFade(0f, fadeOutDuration)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                if (monologueUI != null)
                    monologueUI.CloseWithoutCallback();

                Destroy(gameObject);
            });
    }
    
    private void ShowSpaceHint()
    {
        isWaitingForFirstSpace = true;

        hintBlinkTween?.Kill();

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