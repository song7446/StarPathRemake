using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FreeModeCameraZoom : MonoBehaviour
{
    [Header("카메라")]
    [SerializeField] private CinemachineCamera targetCamera;

    [Tooltip("Cinemachine Brain이 붙어 실제 화면을 출력하는 Camera")]
    [SerializeField] private Camera outputCamera;

    [Header("확대 버튼")]
    [SerializeField] private Button zoomButton;

    [Header("확대 / 축소")]
    [SerializeField, Min(0.01f)] private float normalSize = 10f;
    [SerializeField, Min(0.01f)] private float zoomSize = 5f;
    [SerializeField, Min(0f)] private float zoomDuration = 0.5f;

    [Header("카메라 이동")]
    [SerializeField, Min(0f)] private float panSpeed = 10f;
    [SerializeField] private bool enableEdgePanning = true;
    [SerializeField, Min(0f)] private float edgeThickness = 20f;

    public bool IsZoomed { get; private set; }

    private Vector3 originalPosition;
    private Sequence zoomSequence;
    private bool isZoomTransitioning;

    private void Awake()
    {
        if (targetCamera == null || outputCamera == null)
        {
            Debug.LogError(
                "Target Camera와 Output Camera를 연결해주세요.",
                this);

            enabled = false;
            return;
        }

        originalPosition = targetCamera.transform.position;
        SetCameraSize(normalSize);

        if (zoomButton != null)
            zoomButton.onClick.AddListener(ToggleZoom);
    }

    private void Update()
    {
        if (!IsZoomed ||
            isZoomTransitioning ||
            !Application.isFocused ||
            Time.timeScale <= 0f)
        {
            return;
        }

        // 별을 드래그하는 동안에는 화면이 움직이지 않게 한다.
        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            return;
        }

        Vector2 direction = GetMoveDirection();

        if (direction.sqrMagnitude > 0f)
        {
            Vector3 movement = new Vector3(
                direction.x,
                direction.y,
                0f
            );

            targetCamera.transform.position +=
                movement.normalized * panSpeed * Time.deltaTime;
        }

        ClampPosition();
    }

    private Vector2 GetMoveDirection()
    {
        Vector2 direction = Vector2.zero;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                direction.x -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                direction.x += 1f;

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                direction.y -= 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                direction.y += 1f;
        }

        if (!enableEdgePanning || Mouse.current == null)
            return direction;

        // UI 위에서는 마우스 가장자리 이동을 막는다.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return direction;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Rect screenRect = outputCamera.pixelRect;

        // 게임 화면 바깥에서는 이동하지 않는다.
        if (!screenRect.Contains(mousePosition))
            return direction;

        if (mousePosition.x <= screenRect.xMin + edgeThickness)
            direction.x -= 1f;
        else if (mousePosition.x >= screenRect.xMax - edgeThickness)
            direction.x += 1f;

        if (mousePosition.y <= screenRect.yMin + edgeThickness)
            direction.y -= 1f;
        else if (mousePosition.y >= screenRect.yMax - edgeThickness)
            direction.y += 1f;

        return direction;
    }

    public void ToggleZoom()
    {
        if (targetCamera == null || outputCamera == null)
            return;

        zoomSequence?.Kill();

        IsZoomed = !IsZoomed;
        isZoomTransitioning = true;

        float targetSize = IsZoomed
            ? Mathf.Min(zoomSize, normalSize)
            : normalSize;

        zoomSequence = DOTween.Sequence();

        zoomSequence.Append(
            DOTween.To(
                    () => targetCamera.Lens.OrthographicSize,
                    SetCameraSize,
                    targetSize,
                    zoomDuration)
                .SetEase(Ease.InOutSine)
        );

        // 축소할 때는 크기와 위치를 함께 복구.
        if (!IsZoomed)
        {
            zoomSequence.Join(
                targetCamera.transform
                    .DOMove(originalPosition, zoomDuration)
                    .SetEase(Ease.InOutSine)
            );
        }

        zoomSequence.SetLink(gameObject);

        zoomSequence.OnComplete(() =>
        {
            zoomSequence = null;
            isZoomTransitioning = false;

            ClampPosition();
        });
    }

    private void ClampPosition()
    {
        // 기본 화면 영역에서 확대 화면의 반크기를 뺀 만큼 이동 가능.
        float currentSize = targetCamera.Lens.OrthographicSize;

        float verticalRange = Mathf.Max(0f, normalSize - currentSize);
        float horizontalRange = verticalRange * outputCamera.aspect;

        Vector3 position = targetCamera.transform.position;

        position.x = Mathf.Clamp(
            position.x,
            originalPosition.x - horizontalRange,
            originalPosition.x + horizontalRange);

        position.y = Mathf.Clamp(
            position.y,
            originalPosition.y - verticalRange,
            originalPosition.y + verticalRange);

        position.z = originalPosition.z;

        targetCamera.transform.position = position;
    }

    public void ResetZoom()
    {
        zoomSequence?.Kill();
        zoomSequence = null;

        IsZoomed = false;
        isZoomTransitioning = false;

        if (targetCamera == null)
            return;

        SetCameraSize(normalSize);
        targetCamera.transform.position = originalPosition;
    }

    private void SetCameraSize(float size)
    {
        var lens = targetCamera.Lens;
        lens.OrthographicSize = Mathf.Max(0.01f, size);
        targetCamera.Lens = lens;
    }

    private void OnDisable()
    {
        if (targetCamera != null && outputCamera != null)
            ResetZoom();
    }

    private void OnDestroy()
    {
        zoomSequence?.Kill();

        if (zoomButton != null)
            zoomButton.onClick.RemoveListener(ToggleZoom);
    }
}