using DG.Tweening;
using UnityEngine;

public class PassingStreetLight : MonoBehaviour
{
    [Header("위치 (부모 기준)")] 
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;

    [Header("이동 거리")]
    [SerializeField] private float horizontalDistance = 9f;
    [SerializeField] private float downwardDistance = 5f;

    [Header("크기 (원래 크기 대비)")]
    [SerializeField] private float startScaleRatio = 0.15f;
    [SerializeField] private float endScaleRatio = 1.5f;

    [Header("시간")]
    [SerializeField] private float moveDuration = 3f;
    [SerializeField] private float repeatDelay = 1f;

    private Vector3 originalScale;
    private Sequence sequence;
    
    [Header("등장 페이드")]
    [SerializeField] private float fadeInDuration = 0.6f;
    
    [SerializeField] private float startDelay = 1f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private void Awake()
    {
        originalScale = transform.localScale;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    private void OnEnable()
    {
        Play();
    }

    private void Play()
    {
        sequence?.Kill();

        float direction = startPosition.x < 0f ? -1f : 1f;

        // Vector3 endPosition = startPosition + new Vector3(
        //     Mathf.Abs(horizontalDistance) * direction,
        //     -Mathf.Abs(downwardDistance),
        //     0f
        // );

        float duration = Mathf.Max(0.01f, moveDuration);
        float fadeDuration = Mathf.Clamp(fadeInDuration, 0.01f, duration);

        ResetStreetLight();
        SetOpacity(0f);

        sequence = DOTween.Sequence();

        sequence.AppendCallback(() =>
        {
            SetOpacity(0f);
            ResetStreetLight();
        });

        sequence.Insert(
            0f,
            transform.DOLocalMove(endPosition, duration)
                .SetEase(Ease.InQuad)
        );

        sequence.Insert(
            0f,
            transform.DOScale(endScaleRatio, duration)
                .SetEase(Ease.InQuad)
        );

        sequence.Insert(
            0f,
            DOVirtual.Float(0f, 1f, fadeDuration, SetOpacity)
                .SetEase(Ease.InOutSine)
        );

        sequence.AppendInterval(Mathf.Max(0f, repeatDelay));
        sequence.SetLoops(-1, LoopType.Restart);

// 반복마다 적용하지 않고, 최초 시작에만 적용
        sequence.SetDelay(Mathf.Max(0f, startDelay), false);
    }

    private void SetOpacity(float opacity)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            Color color = originalColors[i];
            color.a *= opacity;
            spriteRenderers[i].color = color;
        }
    }

    private void ResetStreetLight()
    {
        transform.localPosition = startPosition;
        transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        sequence?.Kill();
        sequence = null;

        transform.localScale = originalScale;
        SetOpacity(1f);
    }
}