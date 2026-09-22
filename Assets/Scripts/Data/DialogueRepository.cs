using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;

public class DialogueRepository : MonoBehaviourSingleton<DialogueRepository>
{
    [SerializeField] public DialogueDatabase database;
    
    private readonly Dictionary<string, List<DialogueData>>
        chapters = new();

    private bool initialized;

    private void Initialize()
    {
        if (initialized)
            return;

        if (database == null)
        {
            Debug.LogError("DialogueDatabase를 연결해주세요.", this);
            return;
        }
        
        chapters.Clear();

        foreach (DialogueData dialogue in database.Dialogues)
        {
            if (!chapters.TryGetValue(dialogue.Chapter_Id, out var lines))
            {
                lines = new List<DialogueData>();
                chapters.Add(dialogue.Chapter_Id, lines);
            }

            lines.Add(dialogue);
        }

        initialized = true;
    }

    public bool TryGetChapter(
        string chapterId,
        out IReadOnlyList<DialogueData> dialogues)
    {
        // 다른 컴포넌트의 Awake에서 호출해도 초기화 보장
        Initialize();

        if (chapterId != null &&
            chapters.TryGetValue(chapterId, out var lines))
        {
            dialogues = lines.AsReadOnly();
            return true;
        }

        dialogues = System.Array.Empty<DialogueData>();
        return false;
    }
}