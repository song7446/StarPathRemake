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
            chapterNumber_ZhHans);
    }

    public string GetChapterTitle()
    {
        return GetLocalizedText(
            chapterTitle,
            chapterTitle_En,
            chapterTitle_ZhHans);
    }

    public string GetChapterSubtitle()
    {
        return GetLocalizedText(
            chapterSubtitle,
            chapterSubtitle_En,
            chapterSubtitle_ZhHans);
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
        string simplifiedChinese)
    {
        string translatedText;

        switch (SettingsUI.CurrentLanguage)
        {
            case SettingsUI.GameLanguage.English:
                translatedText = english;
                break;

            case SettingsUI.GameLanguage.SimplifiedChinese:
                translatedText = simplifiedChinese;
                break;

            default:
                translatedText = korean;
                break;
        }

        // 번역이 없으면 기존 한국어 사용.
        return string.IsNullOrWhiteSpace(translatedText)
            ? korean ?? string.Empty
            : translatedText;
    }
}