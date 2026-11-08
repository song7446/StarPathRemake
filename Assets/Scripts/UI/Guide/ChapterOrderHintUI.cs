using DG.Tweening;
using TMPro;
using UnityEngine;

public class ChapterOrderHintUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup hintGroup;
    [SerializeField] private TMP_Text orderText;
    [SerializeField] private TMP_Text partialResultText;

    [Header("연출")]
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.4f;

    [SerializeField, Min(0.01f)]
    private float blinkDuration = 0.8f;

    [SerializeField, Min(0f)]
    private float resultHoldDuration = 5f;

    private Tween hintTween;
    private Tween hideTimer;

    private bool hasShownResult;
    private bool isFinished;
    private bool isShowing;

    private void Awake()
    {
        hintGroup.alpha = 0f;
        hintGroup.interactable = false;
        hintGroup.blocksRaycasts = false;

        orderText.raycastTarget = false;
        partialResultText.raycastTarget = false;

        orderText.gameObject.SetActive(false);
        partialResultText.gameObject.SetActive(false);

        hintGroup.gameObject.SetActive(false);
    }

    public void ShowGuide()
    {
        if (isFinished || isShowing || hasShownResult)
            return;

        ShowMessage(false);
    }

    // 첫 부분 정답 때만 안내를 변경한다.
    public void ShowPartialResult()
    {
        if (isFinished || hasShownResult)
            return;

        hasShownResult = true;

        ShowMessage(true);

        hideTimer = DOVirtual.DelayedCall(
            fadeDuration + resultHoldDuration,
            FinishGuide,
            ignoreTimeScale: false
        ).SetLink(gameObject);
    }

    private void ShowMessage(bool showResult)
    {
        KillTweens();

        isShowing = true;
        hintGroup.alpha = 0f;

        // 번역은 각 오브젝트의 LocalizedText에서 처리한다.
        orderText.gameObject.SetActive(!showResult);
        partialResultText.gameObject.SetActive(showResult);

        hintGroup.gameObject.SetActive(true);

        hintTween = hintGroup
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.OutSine)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                hintTween = hintGroup
                    .DOFade(0.3f, blinkDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            });
    }

    private void FinishGuide()
    {
        isFinished = true;
        hideTimer = null;

        hintTween?.Kill();
        hintTween = null;

        hintTween = hintGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.InSine)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                hintTween = null;
                isShowing = false;

                hintGroup.gameObject.SetActive(false);
            });
    }

    public void HideGuide()
    {
        KillTweens();

        if (hasShownResult)
            isFinished = true;

        isShowing = false;

        if (hintGroup != null)
        {
            hintGroup.alpha = 0f;
            hintGroup.gameObject.SetActive(false);
        }
    }

    public void Complete()
    {
        isFinished = true;
        HideGuide();
    }

    private void KillTweens()
    {
        hintTween?.Kill();
        hideTimer?.Kill();

        hintTween = null;
        hideTimer = null;
    }

    private void OnDisable()
    {
        HideGuide();
    }
}