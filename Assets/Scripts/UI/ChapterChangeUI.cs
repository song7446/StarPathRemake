using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterChangeUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleTMP;

    // 제목 뒤에 표시할 장식 이미지. 없어도 됨.
    [SerializeField] private Image backGroundImage;

    [Header("모자이크")]
    [SerializeField] private Material mosaicMaterial;
    [SerializeField, Range(1f, 64f)] private float pixelSize = 20f;

    [Header("시간")]
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.5f;
    [SerializeField, Min(0f)] private float holdDuration = 1f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 1f;

    private static readonly int PixelSizeId =
        Shader.PropertyToID("_PixelSize");

    private Sequence sequence;

    public void SetTitle(string title)
    {
        titleTMP.text = title;
    }

    public void FadeInTitle(
        Action onFinished = null,
        Action onFadeOutStarted = null)
    {
        sequence?.Kill();
        sequence = null;

        gameObject.SetActive(true);

        titleTMP.alpha = 0f;
        titleTMP.raycastTarget = false;

        SetMosaic(1f);
        SetBackgroundAlpha(0f);

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        // 제목 등장
        sequence.Append(
            titleTMP.DOFade(1f, fadeInDuration)
                .SetEase(Ease.InOutSine)
        );

        // 제목과 함께 화면에 모자이크 적용
        if (mosaicMaterial != null)
        {
            sequence.Join(
                mosaicMaterial
                    .DOFloat(pixelSize, PixelSizeId, fadeInDuration)
                    .SetEase(Ease.InOutSine)
            );
        }

        if (backGroundImage != null)
        {
            sequence.Join(
                backGroundImage.DOFade(1f, fadeInDuration)
                    .SetEase(Ease.InOutSine)
            );
        }

        // 제목 유지
        sequence.AppendInterval(holdDuration);

        // 다음 챕터 오브젝트 등장 시작
        sequence.AppendCallback(() =>
        {
            onFadeOutStarted?.Invoke();
        });

        // 제목 퇴장
        sequence.Append(
            titleTMP.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InOutSine)
        );

        // 모자이크가 작아지면서 원래 화면으로 복귀
        if (mosaicMaterial != null)
        {
            sequence.Join(
                mosaicMaterial
                    .DOFloat(1f, PixelSizeId, fadeOutDuration)
                    .SetEase(Ease.InOutSine)
            );
        }

        if (backGroundImage != null)
        {
            sequence.Join(
                backGroundImage.DOFade(0f, fadeOutDuration)
                    .SetEase(Ease.InOutSine)
            );
        }

        sequence.OnComplete(() =>
        {
            sequence = null;
            SetMosaic(1f);

            gameObject.SetActive(false);

            onFinished?.Invoke();
        });
    }

    private void SetMosaic(float size)
    {
        if (mosaicMaterial != null)
        {
            mosaicMaterial.SetFloat(PixelSizeId, size);
        }
    }

    private void SetBackgroundAlpha(float alpha)
    {
        if (backGroundImage == null)
            return;

        Color color = backGroundImage.color;
        color.a = alpha;
        backGroundImage.color = color;
        backGroundImage.raycastTarget = false;
    }

    private void OnDisable()
    {
        sequence?.Kill();
        sequence = null;

        SetMosaic(1f);
    }
}