using DG.Tweening;
using UnityEngine;

public class PassingStreetLight : MonoBehaviour
{
    [Header("시작 위치 (부모 기준)")]
    [SerializeField] private Vector3 startPosition =
        new Vector3(1f, 1f, 0f);

    [Header("이동 거리")]
    [SerializeField] private float horizontalDistance = 9f;
    [SerializeField] private float downwardDistance = 5f;

    [Header("크기 (원래 크기 대비)")]
    [SerializeField] private float startScaleRatio = 0.001f;
    [SerializeField] private float endScaleRatio = 1.5f;

    [Header("시간")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float moveDuration = 3f;
    [SerializeField] private float repeatDelay = 1f;

    private Vector3 originalScale;
    private Sequence sequence;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        Play();
    }

    private void Play()
    {
        sequence?.Kill();

        float direction = startPosition.x < 0f ? -1f : 1f;

        Vector3 endPosition = startPosition + new Vector3(
            Mathf.Abs(horizontalDistance) * direction,
            -Mathf.Abs(downwardDistance),
            0f
        );

        float duration = Mathf.Max(0.01f, moveDuration);

        ResetStreetLight();

// 시작 딜레이 동안에는 보이지 않게
        transform.localScale = Vector3.zero;

        sequence = DOTween.Sequence();

// 딜레이가 끝나고 실제 출발할 때 시작 크기 적용
        sequence.AppendCallback(() =>
        {
            transform.localScale = originalScale * startScaleRatio;
        });

        sequence.Append(
            transform.DOLocalMove(endPosition, duration)
                .SetEase(Ease.InQuad)
        );

        sequence.Join(
            transform.DOScale(originalScale * endScaleRatio, duration)
                .From(originalScale * startScaleRatio, setImmediately: false)
                .SetEase(Ease.InQuad)
        );
        
        // 完전히 화면 밖으로 나간 위치에서 대기
        sequence.AppendInterval(Mathf.Max(0f, repeatDelay));

        // 반복 시 이동과 크기가 시작값으로 돌아간다.
        sequence.SetLoops(-1, LoopType.Restart);

        // 최초 시작에만 적용
        sequence.SetDelay(Mathf.Max(0f, startDelay), false);
    }

    private void ResetStreetLight()
    {
        transform.localPosition = startPosition;
        transform.localScale = originalScale * startScaleRatio;
    }

    private void OnDisable()
    {
        sequence?.Kill();
        sequence = null;

        transform.localScale = originalScale;
    }
}