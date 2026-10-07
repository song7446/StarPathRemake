using System;
using UnityEngine;
using UnityEngine.UI;

public class NoteUI : MonoBehaviour
{
    public Button nextButton;
    public Button preButton;
    
    public NotebookPageTurn notebookPageTurn;

    private void Awake()
    {
        nextButton.onClick.AddListener(notebookPageTurn.TurnNext);
        preButton.onClick.AddListener(notebookPageTurn.TurnPrevious);
    }
}
