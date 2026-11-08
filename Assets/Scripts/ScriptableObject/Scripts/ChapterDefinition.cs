using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(menuName = "StarPath/ChapterDefinition")]
public class ChapterDefinition : ScriptableObject
{
    [Header("챕터 ID")]
    public string chapterId;

    [Header("한국어")]
    public string chapterNumber;
    public string chapterTitle;
    public string chapterSubtitle;

    [Header("영어")]
    public string chapterNumber_En;
    public string chapterTitle_En;
    public string chapterSubtitle_En;

    [Header("중국어 간체")]
    public string chapterNumber_ZhHans;
    public string chapterTitle_ZhHans;
    public string chapterSubtitle_ZhHans;

    [Header("일본어")]
    public string chapterNumber_Ja;
    public string chapterTitle_Ja;
    public string chapterSubtitle_Ja;

    [Header("스페인어")]
    public string chapterNumber_Es;
    public string chapterTitle_Es;
    public string chapterSubtitle_Es;

    [Header("포르투갈어 브라질")]
    public string chapterNumber_Pt;
    public string chapterTitle_Pt;
    public string chapterSubtitle_Pt;

    [Header("러시아어")]
    public string chapterNumber_Ru;
    public string chapterTitle_Ru;
    public string chapterSubtitle_Ru;

    [Header("등장 오브젝트")]
    public CutSceneCast cutSceneCasts;

    [Header("별자리")]
    [CanBeNull]
    public ConstellationData constellationData;

    [Header("애니메이션")]
    public AnimationCommand[] animations;

    [Header("썸네일")]
    public Sprite thumbnail;

    [Header("배경 음악")]
    public AudioClip bgm;

    public string GetChapterNumber()
    {
        return GetLocalizedText(
            chapterNumber,
            chapterNumber_En,
            chapterNumber_ZhHans,
            chapterNumber_Ja,
            chapterNumber_Es,
            chapterNumber_Pt,
            chapterNumber_Ru);
    }

    public string GetChapterTitle()
    {
        return GetLocalizedText(
            chapterTitle,
            chapterTitle_En,
            chapterTitle_ZhHans,
            chapterTitle_Ja,
            chapterTitle_Es,
            chapterTitle_Pt,
            chapterTitle_Ru);
    }

    public string GetChapterSubtitle()
    {
        return GetLocalizedText(
            chapterSubtitle,
            chapterSubtitle_En,
            chapterSubtitle_ZhHans,
            chapterSubtitle_Ja,
            chapterSubtitle_Es,
            chapterSubtitle_Pt,
            chapterSubtitle_Ru);
    }

    public string GetDisplaySubtitle()
    {
        string title = GetChapterTitle();
        string subtitle = GetChapterSubtitle();

        if (string.IsNullOrWhiteSpace(title))
            return subtitle;

        if (string.IsNullOrWhiteSpace(subtitle))
            return title;

        return $"{title} - {subtitle}";
    }

    private static string GetLocalizedText(
        string korean,
        string english,
        string simplifiedChinese,
        string japanese,
        string spanish,
        string portuguese,
        string russian)
    {
        string translatedText = SettingsUI.CurrentLanguage switch
        {
            SettingsUI.GameLanguage.Korean => korean,
            SettingsUI.GameLanguage.English => english,
            SettingsUI.GameLanguage.SimplifiedChinese => simplifiedChinese,
            SettingsUI.GameLanguage.Japanese => japanese,
            SettingsUI.GameLanguage.Spanish => spanish,
            SettingsUI.GameLanguage.Portuguese => portuguese,
            SettingsUI.GameLanguage.Russian => russian,
            _ => english
        };

        if (!string.IsNullOrWhiteSpace(translatedText))
            return translatedText;

        // 선택한 언어의 번역이 없으면 영어로 표시한다.
        return string.IsNullOrWhiteSpace(english)
            ? string.Empty
            : english;
    }
}