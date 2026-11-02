using DG.Tweening;
using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    [SerializeField] private float scaleMultiplier = 1.12f;
    [SerializeField] private float halfCycleDuration = 1.5f;

    private Vector3 originalScale;
    private Tween pulseTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        pulseTween?.Kill();
        transform.localScale = originalScale;

        pulseTween = transform
            .DOScale(originalScale * scaleMultiplier, halfCycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        pulseTween?.Kill();
        pulseTween = null;

        transform.localScale = originalScale;
    }
}