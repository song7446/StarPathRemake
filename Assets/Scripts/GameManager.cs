using System;
using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    private void Start()
    {
        DialogueRepository.Instance.TryGetChapter("1_Front", out IReadOnlyList<DialogueData> dialogueDatas);
        UIManager.Instance.StartDialogue(dialogueDatas);
    }
}