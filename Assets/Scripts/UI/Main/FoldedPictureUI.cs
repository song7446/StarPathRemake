using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FoldedPictureUI : MonoBehaviour
{
    [Header("실제 그림을 자식으로 가진 액자 Image")]
    [SerializeField] private RectTransform pictureContent;

    [Header("애니메이션")]
    [SerializeField, Min(0.01f)]
    private float unfoldDuration = 0.8f;

    [SerializeField, Min(0.01f)]
    private float foldDuration = 0.5f;

    public float UnfoldDuration => unfoldDuration;
    public float FoldDuration => foldDuration;

    private RectTransform topHalf;
    private RectTransform bottomHalf;

    private Sequence animationSequence;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private bool Initialize()
    {
        if (initialized)
            return true;

        if (pictureContent == null)
        {
            Debug.LogError(
                "Picture Content에 액자 Image를 연결해주세요.",
                this);

            return false;
        }

        // 레이아웃이 적용된 실제 크기 확인.
        Canvas.ForceUpdateCanvases();

        Vector2 size = pictureContent.rect.size;

        if (size.x <= 0f || size.y <= 0f)
        {
            Debug.LogError(
                "액자 Image의 Width와 Height를 설정해주세요.",
                this);

            return false;
        }

        // 액자의 중심 위치와 회전, 크기를 유지할 부모.
        GameObject foldRootObject = new GameObject(
            "GeneratedFoldRoot",
            typeof(RectTransform));

        RectTransform foldRoot =
            foldRootObject.GetComponent<RectTransform>();

        foldRoot.SetParent(pictureContent.parent, false);
        foldRoot.SetSiblingIndex(pictureContent.GetSiblingIndex());

        foldRoot.sizeDelta = size;
        foldRoot.pivot = new Vector2(0.5f, 0.5f);

        foldRoot.position =
            pictureContent.TransformPoint(pictureContent.rect.center);

        foldRoot.localRotation = pictureContent.localRotation;
        foldRoot.localScale = pictureContent.localScale;

        topHalf = CreateHalf(
            "TopHalf",
            foldRoot,
            size,
            true);

        bottomHalf = CreateHalf(
            "BottomHalf",
            foldRoot,
            size,
            false);

        // 원본은 숨기고 생성한 두 복사본으로 표시.
        pictureContent.gameObject.SetActive(false);

        initialized = true;
        return true;
    }

    private RectTransform CreateHalf(
        string objectName,
        RectTransform parent,
        Vector2 fullSize,
        bool isTop)
    {
        GameObject halfObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Mask));

        RectTransform half = halfObject.GetComponent<RectTransform>();

        half.SetParent(parent, false);
        half.anchorMin = new Vector2(0.5f, 0.5f);
        half.anchorMax = new Vector2(0.5f, 0.5f);

        // 위쪽은 아래 모서리, 아래쪽은 위 모서리를 기준으로 회전.
        half.pivot = isTop
            ? new Vector2(0.5f, 0f)
            : new Vector2(0.5f, 1f);

        half.sizeDelta =
            new Vector2(fullSize.x, fullSize.y * 0.5f);

        half.anchoredPosition3D = Vector3.zero;

        Image maskImage = halfObject.GetComponent<Image>();
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;

        // 회전하는 UI이므로 사각 Image + Mask 사용.
        Mask mask = halfObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // 액자와 그 안의 실제 그림을 함께 복제.
        RectTransform copy = Instantiate(
            pictureContent,
            half,
            false);

        copy.name = "PictureCopy";

        copy.anchorMin = isTop
            ? new Vector2(0.5f, 0f)
            : new Vector2(0.5f, 1f);

        copy.anchorMax = copy.anchorMin;
        copy.pivot = new Vector2(0.5f, 0.5f);

        copy.sizeDelta = fullSize;
        copy.anchoredPosition3D = Vector3.zero;
        copy.localRotation = Quaternion.identity;
        copy.localScale = Vector3.one;

        foreach (Graphic graphic in
                 copy.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        copy.gameObject.SetActive(true);

        return half;
    }

    public void Show()
    {
        if (!Initialize())
            return;

        animationSequence?.Kill();
        gameObject.SetActive(true);

        topHalf.localRotation =
            Quaternion.Euler(90f, 0f, 0f);

        bottomHalf.localRotation =
            Quaternion.Euler(-90f, 0f, 0f);

        animationSequence = DOTween.Sequence();
        animationSequence.SetUpdate(true);

        animationSequence.Append(
            topHalf
                .DOLocalRotate(Vector3.zero, unfoldDuration)
                .SetEase(Ease.OutCubic)
        );

        animationSequence.Join(
            bottomHalf
                .DOLocalRotate(Vector3.zero, unfoldDuration)
                .SetEase(Ease.OutCubic)
        );

        animationSequence.OnComplete(() =>
        {
            animationSequence = null;
        });
    }

    public void Hide()
    {
        if (!Initialize())
            return;

        animationSequence?.Kill();

        if (!gameObject.activeInHierarchy)
            return;

        animationSequence = DOTween.Sequence();
        animationSequence.SetUpdate(true);

        animationSequence.Append(
            topHalf
                .DOLocalRotate(
                    new Vector3(90f, 0f, 0f),
                    foldDuration)
                .SetEase(Ease.InCubic)
        );

        animationSequence.Join(
            bottomHalf
                .DOLocalRotate(
                    new Vector3(-90f, 0f, 0f),
                    foldDuration)
                .SetEase(Ease.InCubic)
        );

        animationSequence.OnComplete(() =>
        {
            animationSequence = null;
            gameObject.SetActive(false);
        });
    }

    private void OnDisable()
    {
        animationSequence?.Kill();
        animationSequence = null;
    }
}