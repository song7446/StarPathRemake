using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public void OnAdvance(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        UIManager.Instance.OnAdvanceDialogue();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        DrawingManager.Instance.OnRightClick();
    }
}
