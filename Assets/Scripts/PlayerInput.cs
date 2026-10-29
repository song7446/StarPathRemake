using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInput : MonoBehaviour
{
    private readonly List<RaycastResult> raycastResults = new();

    public void OnAdvance(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        UIManager.Instance.OnAdvanceDialogue();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // 챕터 별 위에서는 기존 장면 보기 입력만 처리한다.
        if (IsPointerOverChapterButton())
            return;

        DrawingManager.Instance.OnRightClick();
        ChapterDrawingManager.Instance.OnRightClick();
    }

    public void OnEsc(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        
        // ESC 메뉴가 이미 열려 있다면 기존 메뉴 처리를 우선한다.
        if (!PauseMenuUI.IsOpen &&
            UIManager.Instance.TryCloseNoteBook())
        {
            return;
        }
        
        UIManager.Instance.HandleEscape();
    }

    private bool IsPointerOverChapterButton()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null || Mouse.current == null)
            return false;

        var pointerData = new PointerEventData(eventSystem)
        {
            position = Mouse.current.position.ReadValue()
        };

        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            // UI 레이캐스트 결과만 확인한다.
            if (result.module is not GraphicRaycaster)
                continue;

            // 가장 위에 있는 UI 기준.
            // 별의 자식 이미지나 텍스트도 같은 별로 판단한다.
            return result.gameObject
                .GetComponentInParent<ChapterButton>() != null;
        }

        return false;
    }
}