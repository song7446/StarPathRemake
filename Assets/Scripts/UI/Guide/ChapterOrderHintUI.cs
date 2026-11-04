using DG.Tweening;
using TMPro;
using UnityEngine;

public class ChapterOrderHintUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup hintGroup;
    [SerializeField] private TMP_Text hintText;

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
        hintGroup.gameObject.SetActive(false);

        hintText.raycastTarget = false;
    }

    public void ShowGuide()
    {
        if (isFinished || isShowing || hasShownResult)
            return;

        ShowMessage("사건이 일어난 순서대로 장면의 별을 이어보세요.");
    }

    // 첫 부분 정답 때만 문구 변경.
    public void ShowPartialResult()
    {
        if (isFinished || hasShownResult)
            return;

        hasShownResult = true;

        ShowMessage(
            "올바른 연결은 고정되고,\n틀린 연결은 사라집니다."
        );

        hideTimer = DOVirtual.DelayedCall(
            fadeDuration + resultHoldDuration,
            FinishGuide,
            ignoreTimeScale: false
        ).SetLink(gameObject);
    }

    private void ShowMessage(string message)
    {
        hintTween?.Kill();
        hideTimer?.Kill();
        hideTimer = null;

        isShowing = true;

        hintGroup.alpha = 0f;
        hintText.text = message;
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

        hintTween = hintGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.InSine)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                isShowing = false;
                hintGroup.gameObject.SetActive(false);
            });
    }
    
    public void HideGuide()
    {
        hintTween?.Kill();
        hideTimer?.Kill();

        hintTween = null;
        hideTimer = null;

        if (hasShownResult)
            isFinished = true;

        isShowing = false;
        hintGroup.alpha = 0f;
        hintGroup.gameObject.SetActive(false);
    }
    
    public void Complete()
    {
        isFinished = true;
        HideGuide();
    }

    private void OnDisable()
    {
        HideGuide();
    }
}