using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [SerializeField] private DialogueUI dialogueUI;

    public void OnAdvanceDialogue()
    {
        dialogueUI.Advance();
    }

    public void StartDialogue(IReadOnlyList<DialogueData> dialogues)
    {
        dialogueUI.Play(dialogues);
    }
}
