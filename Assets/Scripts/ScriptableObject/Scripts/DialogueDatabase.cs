using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueData
{
    public string ID;
    public string Chapter_Id;
    public string Speaker;

    [TextArea(2, 8)]
    public string Text_Kr;

    [TextArea(2, 8)]
    public string Text_En;
}

[CreateAssetMenu(
    fileName = "DialogueDatabase",
    menuName = "StarPath/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    [SerializeField]
    private List<DialogueData> dialogues = new();

    public IReadOnlyList<DialogueData> Dialogues => dialogues;

    public List<DialogueData> GetChapter(string chapterId)
    {
        return dialogues.FindAll(x => x.Chapter_Id == chapterId);
    }

#if UNITY_EDITOR
    public void ReplaceData(List<DialogueData> data)
    {
        dialogues = new List<DialogueData>(data);
    }
#endif
}