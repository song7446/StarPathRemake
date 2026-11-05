using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LanguageFontSettings",
    menuName = "StarPath/Language Font Settings")]
public class LanguageFontSettings : ScriptableObject
{
    [Header("한국어")]
    public TMP_FontAsset koreanFont;

    [Header("영어")]
    public TMP_FontAsset englishFont;

    [Header("중국어 간체")]
    public TMP_FontAsset simplifiedChineseFont;

    [Header("일본어")]
    public TMP_FontAsset japaneseFont;

    [Header("스페인어")]
    public TMP_FontAsset spanishFont;

    [Header("브라질 포르투갈어")]
    public TMP_FontAsset portugueseFont;

    [Header("러시아어")]
    public TMP_FontAsset russianFont;

    public TMP_FontAsset GetFont(SettingsUI.GameLanguage language)
    {
        return language switch
        {
            SettingsUI.GameLanguage.Korean => koreanFont,
            SettingsUI.GameLanguage.English => englishFont,
            SettingsUI.GameLanguage.SimplifiedChinese =>
                simplifiedChineseFont,
            SettingsUI.GameLanguage.Japanese => japaneseFont,
            SettingsUI.GameLanguage.Spanish => spanishFont,
            SettingsUI.GameLanguage.Portuguese => portugueseFont,
            SettingsUI.GameLanguage.Russian => russianFont,
            _ => englishFont
        };
    }
}