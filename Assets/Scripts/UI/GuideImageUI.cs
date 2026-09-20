using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class GuideImageUI : MonoBehaviour
{
    [SerializeField] private GameObject guideImage;
    
    private Vector3 beforePosition;
    private bool isGuideHidden;
    private bool isSliding;
    
    private Vector3 guideMouseOffset;
    private RectTransform guideParent;
    private Camera guideUICamera;
    private bool isDraggingGuide;
    
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
        if (isSliding)
            return;

        RectTransform rect = guideImage.GetComponent<RectTransform>();
        Vector3 targetPosition;

        if (isGuideHidden)
        {
            targetPosition = beforePosition;
        }
        else
        {
            beforePosition = rect.position;

            Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                uiCamera, rect.TransformPoint(rect.rect.center));

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;

            foreach (Vector3 corner in corners)
            {
                float x = RectTransformUtility.WorldToScreenPoint(
                    uiCamera, corner).x;

                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
            }

            float moveX = center.x < Screen.width * 0.5f
                ? -right - 200f
                : Screen.width - left + 200f;

            RectTransform parent = rect.parent as RectTransform;

            // 화면상 이동 거리를 실제 월드 좌표의 이동량으로 변환
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent, center, uiCamera, out Vector3 from) ||
                !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent, center + Vector2.right * moveX,
                    uiCamera, out Vector3 to))
                return;

            targetPosition = beforePosition + (to - from);
        }

        isSliding = true;

        rect.DOMove(targetPosition, 0.4f)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                isGuideHidden = !isGuideHidden;
                isSliding = false;
            });
    }
}
