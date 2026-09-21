using System;
using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    public ChapterDefinition chapterDefinition;

    private Queue<String> _chapterIDs = new Queue<string>();
    public Queue<string> ChapterIDs => _chapterIDs;
    
    private string[] chapters =
    {
        "1_Front", "1_Back", "2_Front", "2_Back", "3_Front", "3_Back", "4_Front", "4_Back", "5_Front", "5_Back",
        "6_Front", "6_Back"
    };
    
    private bool _isFront = false;
    public bool IsFront => _isFront;

    private void Start()
    {
        foreach (var chapter in chapters)
        {
            _chapterIDs.Enqueue(chapter);
        }

        StartNextChapter();
    }

    public void StartNextChapter()
    {
        if (_chapterIDs.Count <= 0)
            return;

        DialogueRepository.Instance.TryGetChapter(_chapterIDs.Dequeue(), out IReadOnlyList<DialogueData> dialogueDatas);
        UIManager.Instance.StartDialogue(dialogueDatas);
        
        _isFront =  !_isFront;
    }
}