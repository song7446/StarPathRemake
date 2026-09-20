using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private ToggleCameraUI toggleCameraUI;

    [SerializeField] private Button slideGuideImageButton;
    [SerializeField] private GameObject guideImage;
    
    private Vector3 beforePosition;
    private bool isGuideHidden;
    private bool isSliding;


    protected override void Awake()
    {
        base.Awake();

        slideGuideImageButton.onClick.AddListener(OnClickSlideGuideImage);
    }

    public void OnAdvanceDialogue()
    {
        dialogueUI.Advance();
    }

    public void StartDialogue(IReadOnlyList<DialogueData> dialogues)
    {
        dialogueUI.Play(dialogues);
    }

    private void OnClickSlideGuideImage()
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
