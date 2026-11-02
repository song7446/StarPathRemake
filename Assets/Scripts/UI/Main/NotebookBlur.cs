using DG.Tweening;
using UnityEngine;

public class NotebookBlur : MonoBehaviour
{
    [SerializeField] private Material blurMaterial;

    [SerializeField, Range(1f, 64f)]
    private float blurStrength = 30f;

    [SerializeField, Min(0.01f)]
    private float fadeInDuration = 0.25f;

    private static readonly int PixelSizeId =
        Shader.PropertyToID("_PixelSize");

    private Tween blurTween;

    private void OnEnable()
    {
        blurTween?.Kill();

        if (blurMaterial == null)
            return;

        blurMaterial.SetFloat(PixelSizeId, 1f);

        blurTween = blurMaterial
            .DOFloat(
                blurStrength,
                PixelSizeId,
                fadeInDuration
            )
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        blurTween?.Kill();
        blurTween = null;

        if (blurMaterial != null)
        {
            blurMaterial.SetFloat(PixelSizeId, 1f);
        }
    }
}