using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LanguageFontSettings",
    menuName = "StarPath/Language Font Settings")]
public class LanguageFontSettings : ScriptableObject
{
    [Header("한국어")]
    public TMP_FontAsset koreanFont;

    [Min(0.1f)]
    public float koreanSizeMultiplier = 1f;

    [Header("영어")]
    public TMP_FontAsset englishFont;

    [Min(0.1f)]
    public float englishSizeMultiplier = 1f;

    [Header("중국어 간체")]
    public TMP_FontAsset simplifiedChineseFont;

    [Min(0.1f)]
    public float simplifiedChineseSizeMultiplier = 1f;

    [Header("일본어")]
    public TMP_FontAsset japaneseFont;

    [Min(0.1f)]
    public float japaneseSizeMultiplier = 1f;

    [Header("스페인어")]
    public TMP_FontAsset spanishFont;

    [Min(0.1f)]
    public float spanishSizeMultiplier = 1f;

    [Header("브라질 포르투갈어")]
    public TMP_FontAsset portugueseFont;

    [Min(0.1f)]
    public float portugueseSizeMultiplier = 1f;

    [Header("러시아어")]
    public TMP_FontAsset russianFont;

    [Min(0.1f)]
    public float russianSizeMultiplier = 1f;

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

    public float GetSizeMultiplier(SettingsUI.GameLanguage language)
    {
        float multiplier = language switch
        {
            SettingsUI.GameLanguage.Korean => koreanSizeMultiplier,
            SettingsUI.GameLanguage.English => englishSizeMultiplier,
            SettingsUI.GameLanguage.SimplifiedChinese =>
                simplifiedChineseSizeMultiplier,
            SettingsUI.GameLanguage.Japanese => japaneseSizeMultiplier,
            SettingsUI.GameLanguage.Spanish => spanishSizeMultiplier,
            SettingsUI.GameLanguage.Portuguese => portugueseSizeMultiplier,
            SettingsUI.GameLanguage.Russian => russianSizeMultiplier,
            _ => englishSizeMultiplier
        };

        return Mathf.Max(0.1f, multiplier);
    }
}