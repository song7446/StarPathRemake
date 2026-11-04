using DG.Tweening;
using UnityEngine;

public class FirstChapterHintUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup hintGroup;

    [Header("등장")]
    [SerializeField, Min(0.01f)]
    private float fadeInDuration = 0.6f;

    [Header("깜빡임")]
    [SerializeField, Min(0.01f)]
    private float blinkDuration = 0.8f;

    private Tween hintTween;

    private bool isCompleted;
    private bool isShowing;

    private void Awake()
    {
        if (hintGroup == null)
            return;

        hintGroup.interactable = false;
        hintGroup.blocksRaycasts = false;
        hintGroup.alpha = 0f;
        hintGroup.gameObject.SetActive(false);
    }

    public void Show()
    {
        if (isCompleted || isShowing || hintGroup == null)
            return;

        isShowing = true;

        hintTween?.Kill();

        // 활성화 전에 투명하게 만들어 순간적으로 보이는 것 방지.
        hintGroup.alpha = 0f;
        hintGroup.gameObject.SetActive(true);

        hintTween = hintGroup
            .DOFade(1f, fadeInDuration)
            .SetEase(Ease.OutSine)
            .SetUpdate(true)
            .SetLink(hintGroup.gameObject)
            .OnComplete(StartBlink);
    }

    private void StartBlink()
    {
        if (isCompleted || !isShowing || hintGroup == null)
            return;

        hintTween = hintGroup
            .DOFade(0.25f, blinkDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(hintGroup.gameObject);
    }

    // 장면 보기 메뉴를 열었을 때 호출.
    public void Complete()
    {
        isCompleted = true;
        Hide();
    }

    private void Hide()
    {
        isShowing = false;

        hintTween?.Kill();
        hintTween = null;

        if (hintGroup != null)
        {
            hintGroup.alpha = 0f;
            hintGroup.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Hide();
    }
}