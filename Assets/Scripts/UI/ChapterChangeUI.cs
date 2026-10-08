using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterChangeUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private CanvasGroup titleGroup;

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

    public void SetTitle(string title, string subtitle)
    {
        titleTMP.text = title;
        subtitleTMP.text = subtitle;
    }

    public void FadeInTitle(
        Action onFinished = null,
        Action onFadeOutStarted = null)
    {
        sequence?.Kill();
        sequence = null;

        if (titleGroup == null)
        {
            Debug.LogError("Title Group을 연결해주세요.", this);
            return;
        }

        // 켜기 전에 패널 전체를 투명하게 준비.
        titleGroup.alpha = 0f;
        titleGroup.interactable = false;
        titleGroup.blocksRaycasts = false;

        // 개별 요소는 불투명하게 두고,
        // 전체 투명도는 CanvasGroup으로 조절.
        titleTMP.alpha = 1f;
        titleTMP.raycastTarget = false;

        SetBackgroundAlpha(1f);
        SetMosaic(1f);

        gameObject.SetActive(true);

        float fadeInTime = Mathf.Max(0.01f, fadeInDuration);
        float fadeOutTime = Mathf.Max(0.01f, fadeOutDuration);

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        // 챕터 번호, 제목, 장식이 함께 천천히 등장.
        sequence.Append(
            titleGroup.DOFade(1f, fadeInTime)
                .SetEase(Ease.Linear)
        );

        // 블러도 함께 적용.
        if (mosaicMaterial != null)
        {
            sequence.Join(
                mosaicMaterial.DOFloat(
                    pixelSize,
                    PixelSizeId,
                    fadeInTime
                ).SetEase(Ease.InOutSine)
            );
        }

        // 완전히 나타난 상태로 유지.
        sequence.AppendInterval(Mathf.Max(0f, holdDuration));

        // 제목이 사라지기 시작할 때 오브젝트 등장.
        sequence.AppendCallback(() =>
        {
            onFadeOutStarted?.Invoke();
        });

        // 제목 패널 전체 퇴장.
        sequence.Append(
            titleGroup.DOFade(0f, fadeOutTime)
                .SetEase(Ease.InOutSine)
        );

        // 블러 해제.
        if (mosaicMaterial != null)
        {
            sequence.Join(
                mosaicMaterial.DOFloat(
                    1f,
                    PixelSizeId,
                    fadeOutTime
                ).SetEase(Ease.InOutSine)
            );
        }

        sequence.OnComplete(() =>
        {
            sequence = null;

            titleGroup.alpha = 0f;
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