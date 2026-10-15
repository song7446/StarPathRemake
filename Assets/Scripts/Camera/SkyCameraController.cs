using UnityEngine;
using UnityEngine.InputSystem;

public class SkyCameraController : MonoBehaviour
{
    [Header("이동할 대상")]
    [Tooltip("확대용 Cinemachine 카메라인 ccStarSky를 연결")]
    [SerializeField] private Transform panTarget;

    [Header("좌우 이동")]
    [SerializeField] private float panSpeed = 3f;

    [Tooltip("처음 위치를 기준으로 왼쪽 이동 가능 거리")]
    [SerializeField, Min(0f)] private float leftDistance = 5f;

    [Tooltip("처음 위치를 기준으로 오른쪽 이동 가능 거리")]
    [SerializeField, Min(0f)] private float rightDistance = 5f;

    [Header("마우스 가장자리 이동")]
    [SerializeField] private bool enableEdgePanning = true;
    [SerializeField, Min(0f)] private float edgePanBorderThickness = 20f;

    private Vector3 originalLocalPosition;

    private void Awake()
    {
        if (panTarget == null)
        {
            Debug.LogError("Pan Target에 ccStarSky를 연결해주세요.", this);
            enabled = false;
            return;
        }

        originalLocalPosition = panTarget.localPosition;
    }

    private void Update()
    {
        if (!Application.isFocused)
            return;

        // 별을 확대해서 보는 동안에만 이동
        if (!UIManager.Instance.GetIsOnSkyView())
            return;

        float direction = GetHorizontalInput();

        if (Mathf.Approximately(direction, 0f))
            return;

        Vector3 position = panTarget.localPosition;

        position.x += direction * panSpeed * Time.deltaTime;

        position.x = Mathf.Clamp(
            position.x,
            originalLocalPosition.x - leftDistance,
            originalLocalPosition.x + rightDistance
        );

        panTarget.localPosition = position;
    }

    private float GetHorizontalInput()
    {
        float direction = 0f;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                direction -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                direction += 1f;
        }

        // 키보드 입력이 있으면 우선 사용
        if (!Mathf.Approximately(direction, 0f))
            return direction;

        if (!enableEdgePanning || Mouse.current == null)
            return 0f;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // 게임 화면 밖에 있는 마우스는 무시
        if (mousePosition.x < 0f || mousePosition.x > Screen.width ||
            mousePosition.y < 0f || mousePosition.y > Screen.height)
        {
            return 0f;
        }

        if (mousePosition.x <= edgePanBorderThickness)
            direction = -1f;
        else if (mousePosition.x >= Screen.width - edgePanBorderThickness)
            direction = 1f;

        return direction;
    }

    public void ResetToCenter()
    {
        if (panTarget == null)
            return;

        Vector3 position = panTarget.localPosition;
        position.x = originalLocalPosition.x;
        panTarget.localPosition = position;
    }
}