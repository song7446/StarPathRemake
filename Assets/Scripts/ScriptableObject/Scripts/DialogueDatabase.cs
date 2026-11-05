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

    [TextArea(2, 8)]
    public string Text_ZhHans;
    
    [TextArea(2, 8)]
    public string Text_Ja;
    
    [TextArea(2, 8)]
    public string Text_Es;
    
    [TextArea(2, 8)]
    public string Text_Pt;
    
    [TextArea(2, 8)]
    public string Text_Ru;

    // 현재 설정된 언어의 대사를 반환.
    public string GetText()
    {
        return GetText(SettingsUI.CurrentLanguage);
    }

    // 특정 언어의 대사를 반환.
    public string GetText(SettingsUI.GameLanguage language)
    {
        string translatedText;

        switch (language)
        {
            case SettingsUI.GameLanguage.English:
                translatedText = Text_En;
                break;

            case SettingsUI.GameLanguage.SimplifiedChinese:
                translatedText = Text_ZhHans;
                break;

            default:
                translatedText = Text_Kr;
                break;
        }

        // 번역이 없으면 한국어 원문 사용.
        return string.IsNullOrWhiteSpace(translatedText)
            ? Text_Kr ?? string.Empty
            : translatedText;
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