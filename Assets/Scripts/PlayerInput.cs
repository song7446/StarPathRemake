using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public void OnAdvance(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        UIManager.Instance.OnAdvanceDialogue();
    }
}
