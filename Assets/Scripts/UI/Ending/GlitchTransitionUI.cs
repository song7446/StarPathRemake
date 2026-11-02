using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GlitchTransitionUI : MonoBehaviour
{
    [Header("전환 시간")]
    [SerializeField, Min(0.01f)]
    private float closeDuration = 0.6f;

    [SerializeField, Min(0.01f)]
    private float openDuration = 0.6f;

    [Header("초기 상태")]
    [SerializeField] private bool startClosed;

    private static readonly int ProgressId =
        Shader.PropertyToID("_Progress");

    private static readonly int NoiseTimeId =
        Shader.PropertyToID("_NoiseTime");

    private Image overlay;
    private Material originalMaterial;
    private Material runtimeMaterial;

    private Tween transitionTween;
    private float progress;
    private float noiseTime;

    public bool IsTransitioning { get; private set; }
    public bool IsClosed => progress >= 1f;

    private void Awake()
    {
        overlay = GetComponent<Image>();
        originalMaterial = overlay.material;

        if (originalMaterial == null ||
            !originalMaterial.HasProperty(ProgressId))
        {
            Debug.LogError(
                "GlitchTransition 머티리얼을 Image에 연결해주세요.",
                this);

            enabled = false;
            return;
        }

        runtimeMaterial = new Material(originalMaterial);
        overlay.material = runtimeMaterial;

        // 셰이더의 검은 화면이 확실히 불투명하도록 설정.
        overlay.color = Color.white;

        SetImmediate(startClosed);
    }

    private void Update()
    {
        if (runtimeMaterial == null)
            return;

        noiseTime += Time.unscaledDeltaTime;
        runtimeMaterial.SetFloat(NoiseTimeId, noiseTime);
    }

    // Inspector UnityEvent에서 호출.
    public void Close()
    {
        Close(null);
    }

    public void Open()
    {
        Open(null);
    }

    // 코드에서 완료 콜백과 함께 호출.
    public void Close(Action onFinished)
    {
        PlayTransition(1f, closeDuration, onFinished);
    }

    public void Open(Action onFinished)
    {
        PlayTransition(0f, openDuration, onFinished);
    }

    private void PlayTransition(
        float target,
        float duration,
        Action onFinished)
    {
        if (runtimeMaterial == null || !isActiveAndEnabled)
            return;

        transitionTween?.Kill();
        transitionTween = null;

        if (Mathf.Approximately(progress, target))
        {
            SetImmediate(target >= 1f);
            onFinished?.Invoke();
            return;
        }

        IsTransitioning = true;

        overlay.enabled = true;
        overlay.raycastTarget = true;

        transitionTween = DOTween.To(
                () => progress,
                ApplyProgress,
                target,
                Mathf.Max(0.01f, duration)
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                transitionTween = null;
                IsTransitioning = false;

                ApplyProgress(target);

                // 검게 닫혀 있는 동안에는 클릭 차단 유지.
                bool closed = target >= 1f;

                overlay.enabled = closed;
                overlay.raycastTarget = closed;

                onFinished?.Invoke();
            });
    }

    public void SetImmediate(bool closed)
    {
        if (runtimeMaterial == null)
            return;

        transitionTween?.Kill();
        transitionTween = null;

        IsTransitioning = false;

        ApplyProgress(closed ? 1f : 0f);

        overlay.enabled = closed;
        overlay.raycastTarget = closed;
    }

    private void ApplyProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        runtimeMaterial.SetFloat(ProgressId, progress);
    }

    private void OnDisable()
    {
        transitionTween?.Kill();
        transitionTween = null;
        IsTransitioning = false;
    }

    private void OnDestroy()
    {
        transitionTween?.Kill();

        if (overlay != null)
            overlay.material = originalMaterial;

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}