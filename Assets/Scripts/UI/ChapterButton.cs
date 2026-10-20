using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChapterButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private Button sceneViewButton;

    [Header("왼쪽 클릭")]
    [SerializeField] private UnityEvent onLeftClick = new UnityEvent();

    private RectTransform rect;
    private RectTransform parentRect;

    private Vector2 pointerStartPosition;
    private Vector3 originalLocalPosition;

    private bool isDragging;
    private bool draggedThisPress;
    private bool canDrag;

    [SerializeField] private int chapterIdx;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        sceneViewButton.onClick.AddListener(OnClickSceneViewButton);
    }

    private void OnClickSceneViewButton()
    {
        GameManager.Instance.StartChapter(chapterIdx);
        UIManager.Instance.SlideOutChapterButtons();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isDragging = false;
        draggedThisPress = false;

        parentRect = rect.parent as RectTransform;
        originalLocalPosition = rect.localPosition;

        canDrag = parentRect != null
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out pointerStartPosition
            );
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // 드래그하고 놓은 경우에는 선 연결을 시작하지 않는다.
        if (isDragging || draggedThisPress || eventData.dragging)
            return;

        onLeftClick.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        draggedThisPress = true;

        // Unity의 클릭 처리도 취소한다.
        eventData.eligibleForClick = false;

        if (!canDrag)
            return;

        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 currentPointerPosition))
        {
            return;
        }

        Vector2 offset = currentPointerPosition - pointerStartPosition;

        rect.localPosition = originalLocalPosition
            + new Vector3(offset.x, offset.y, 0f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isDragging = false;
        canDrag = false;

        // draggedThisPress는 다음 PointerDown에서 초기화한다.
    }
    

    private void OnDisable()
    {
        isDragging = false;
        draggedThisPress = false;
        canDrag = false;
    }
}