using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueData
{
    public string ID;
    public string Chapter_Id;
    public string Speaker;

    [UnityEngine.TextArea(2, 8)]
    public string Text_Kr;

    [UnityEngine.TextArea(2, 8)]
    public string Text_En;

    [UnityEngine.TextArea(2, 8)]
    public string Text_ZhHans;

    [UnityEngine.TextArea(2, 8)]
    public string Text_Ja;

    [UnityEngine.TextArea(2, 8)]
    public string Text_Es;

    [UnityEngine.TextArea(2, 8)]
    public string Text_Pt;

    [UnityEngine.TextArea(2, 8)]
    public string Text_Ru;

    public string GetText()
    {
        return GetText(SettingsUI.CurrentLanguage);
    }

    public string GetText(SettingsUI.GameLanguage language)
    {
        string text = language switch
        {
            SettingsUI.GameLanguage.Korean => Text_Kr,
            SettingsUI.GameLanguage.English => Text_En,
            SettingsUI.GameLanguage.SimplifiedChinese => Text_ZhHans,
            SettingsUI.GameLanguage.Japanese => Text_Ja,
            SettingsUI.GameLanguage.Spanish => Text_Es,
            SettingsUI.GameLanguage.Portuguese => Text_Pt,
            SettingsUI.GameLanguage.Russian => Text_Ru,
            _ => Text_En
        };

        if (!string.IsNullOrWhiteSpace(text))
            return text;

        return string.IsNullOrWhiteSpace(Text_En)
            ? string.Empty
            : Text_En;
    }
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