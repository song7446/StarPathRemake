using System;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class GuideImageUI : MonoBehaviour
{
    [SerializeField] private GameObject guideImage;
    [SerializeField] private Vector2 guideShownPosition;

    private Vector3 beforePosition;
    public bool isGuideHidden = true;
    private bool isSliding;

    private Vector3 guideMouseOffset;
    private RectTransform guideParent;
    private Camera guideUICamera;
    private bool isDraggingGuide;

    [Header("가이드 사진")] 
    [SerializeField] private Image guidePhotoImage;
    [SerializeField] private TextMeshProUGUI guideImageName;

    public void OnBeginDragGuide(BaseEventData eventData)
    {
        if (isSliding || isGuideHidden)
            return;

        if (eventData is not PointerEventData pointer ||
            pointer.button != PointerEventData.InputButton.Left)
            return;

        RectTransform rect = guideImage.GetComponent<RectTransform>();
        guideParent = rect.parent as RectTransform;
        guideUICamera = pointer.pressEventCamera;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                guideParent, pointer.position, guideUICamera,
                out Vector3 mouseWorldPosition))
            return;

        guideMouseOffset = rect.position - mouseWorldPosition;
        isDraggingGuide = true;
    }

    public void OnDragGuide(BaseEventData eventData)
    {
        if (!isDraggingGuide || isSliding || isGuideHidden)
            return;

        if (eventData is not PointerEventData pointer ||
            pointer.button != PointerEventData.InputButton.Left)
            return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                guideParent, pointer.position, guideUICamera,
                out Vector3 mouseWorldPosition))
        {
            guideImage.transform.position =
                mouseWorldPosition + guideMouseOffset;
        }
    }

    public void OnEndDragGuide(BaseEventData eventData)
    {
        if (eventData is PointerEventData pointer &&
            pointer.button == PointerEventData.InputButton.Left)
        {
            isDraggingGuide = false;
        }
    }

    public void SlideGuideImage()
    {
        SlideGuideImage(null);
    }

    public void SlideGuideImage(Action onFinished)
    {
        if (isSliding)
            return;

        RectTransform rect =
            guideImage.GetComponent<RectTransform>();

        Vector2 targetPosition;

        if (isGuideHidden)
        {
            // 처음부터 밖에 있어도 지정한 위치로 등장.
            targetPosition = guideShownPosition;
        }
        else
        {
            // 드래그 등으로 변경된 현재 위치 기억.
            guideShownPosition = rect.anchoredPosition;

            Canvas canvas =
                rect.GetComponentInParent<Canvas>().rootCanvas;

            Camera uiCamera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

            Vector2 center =
                RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    rect.TransformPoint(rect.rect.center)
                );

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;

            foreach (Vector3 corner in corners)
            {
                float x = RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    corner
                ).x;

                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
            }

            float moveX = center.x < Screen.width * 0.5f
                ? -right - 200f
                : Screen.width - left + 200f;

            RectTransform parent = rect.parent as RectTransform;

            if (parent == null)
                return;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent,
                    center,
                    uiCamera,
                    out Vector3 from) ||
                !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent,
                    center + Vector2.right * moveX,
                    uiCamera,
                    out Vector3 to))
            {
                return;
            }

            // 월드 이동량을 부모 기준의 UI 이동량으로 변환.
            Vector3 localMovement =
                parent.InverseTransformVector(to - from);

            targetPosition =
                rect.anchoredPosition + (Vector2)localMovement;
        }

        bool targetHidden = !isGuideHidden;

        isSliding = true;

        rect.DOAnchorPos(targetPosition, 0.4f)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                isGuideHidden = targetHidden;
                isSliding = false;
            }).OnComplete(() =>
            {
                isGuideHidden = targetHidden;
                isSliding = false;

                onFinished?.Invoke();
            });
    }

    public void ChangeGuideImage(Sprite sprite)
    {
        if (sprite == null || guidePhotoImage == null)
            return;

        guidePhotoImage.sprite = sprite;
        guidePhotoImage.preserveAspect = true;
    }

    public void ChangeGuideName(string constellationName)
    {
        guideImageName.text = constellationName;
    }
    
    public void ReplaceGuideImage(Sprite sprite)
    {
        if (sprite == null || guidePhotoImage == null || isSliding)
            return;

        isDraggingGuide = false;

        if (isGuideHidden)
        {
            // 이미 숨겨져 있으면 바로 교체하고 등장
            ChangeGuideImage(sprite);
            SlideGuideImage();
        }
        else
        {
            // 나가기가 끝난 뒤 교체하고 다시 등장
            SlideGuideImage(() =>
            {
                ChangeGuideImage(sprite);
                SlideGuideImage();
            });
        }
    }
}