using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ChapterSceneMenuUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject sceneViewButton;

    // 모든 별 중 현재 열려 있는 메뉴 하나.
    private static ChapterSceneMenuUI openedMenu;

    private readonly List<RaycastResult> raycastResults
        = new List<RaycastResult>();

    private void Awake()
    {
        if (sceneViewButton != null)
            sceneViewButton.SetActive(false);
    }

    private void Update()
    {
        if (openedMenu != this || Mouse.current == null)
            return;

        bool clicked =
            Mouse.current.leftButton.wasPressedThisFrame ||
            Mouse.current.rightButton.wasPressedThisFrame ||
            Mouse.current.middleButton.wasPressedThisFrame;

        if (!clicked)
            return;

        // 자신의 별이나 장면 보기 버튼을 누른 게 아니면 닫기.
        if (!IsPointerOverThisStar())
            Close();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (sceneViewButton == null)
            return;

        // 이미 열린 별을 다시 우클릭하면 닫기.
        if (openedMenu == this)
        {
            Close();
            return;
        }

        // 다른 별의 메뉴부터 닫기.
        if (openedMenu != null)
            openedMenu.Close();

        openedMenu = this;

        // 선택한 별과 메뉴를 다른 별보다 앞으로.
        transform.SetAsLastSibling();

        sceneViewButton.SetActive(true);
    }

    private bool IsPointerOverThisStar()
    {
        if (EventSystem.current == null)
            return false;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, raycastResults);

        if (raycastResults.Count == 0)
            return false;

        // 실제로 가장 앞에서 클릭을 받는 UI 기준.
        Transform hit = raycastResults[0].gameObject.transform;

        return hit == transform || hit.IsChildOf(transform);
    }

    public void Close()
    {
        if (openedMenu == this)
            openedMenu = null;

        if (sceneViewButton != null)
            sceneViewButton.SetActive(false);
    }

    private void OnDisable()
    {
        Close();
    }
}