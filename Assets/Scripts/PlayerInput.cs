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

        if (MonologueUI.IsPlaying || PauseMenuUI.IsOpen)
            return;

        UIManager.Instance.OnAdvanceDialogue();
    }
    
    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (MonologueUI.IsPlaying || PauseMenuUI.IsOpen)
            return;

        if (SceneFadeController.Instance != null &&
            SceneFadeController.Instance.IsTransitioning)
        {
            return;
        }

        GameManager manager = GameManager.Instance;

        if (manager.IsChapterActive)
        {
            // 챕터 준비와 복귀 중에는 처리하지 않는다.
            if (!manager.IsChapterReady)
                return;

            // 챕터 안에서는 하늘을 보며 퍼즐을 푸는 동안만
            // 해당 별자리의 선을 취소하거나 삭제한다.
            if (!UIManager.Instance.GetIsOnSkyView() ||
                UIManager.Instance.IsPlayingDialogue())
            {
                return;
            }

            DrawingManager.Instance.OnRightClick();
            return;
        }

        // 챕터 선택 화면의 별 위에서는 장면 보기 메뉴만 처리.
        if (IsPointerOverChapterButton())
            return;

        // 챕터 선택 화면에서는 시간 순서 퍼즐만 처리한다.
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