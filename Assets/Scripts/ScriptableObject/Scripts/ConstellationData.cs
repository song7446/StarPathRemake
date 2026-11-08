using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct StarConnection
{
    public int starA;
    public int starB;

    // 연결 방향과 관계없이 같은 두 별인지 확인.
    public bool IsSameConnection(int id1, int id2)
    {
        return (starA == id1 && starB == id2) ||
               (starA == id2 && starB == id1);
    }
}

[CreateAssetMenu(
    fileName = "NewConstellation",
    menuName = "StarPath/ConstellationData")]
public class ConstellationData : ScriptableObject
{
    [Header("별자리 식별자")]
    public string constellationId;

    [Header("한국어")]
    public string Name_Kr;

    [TextArea(2, 8)]
    public string Explain_Kr;

    [Header("영어")]
    public string Name_En;

    [TextArea(2, 8)]
    public string Explain_En;

    [Header("중국어 간체")]
    public string Name_ZhHans;

    [TextArea(2, 8)]
    public string Explain_ZhHans;

    [Header("일본어")]
    public string Name_Ja;

    [TextArea(2, 8)]
    public string Explain_Ja;

    [Header("스페인어")]
    public string Name_Es;

    [TextArea(2, 8)]
    public string Explain_Es;

    [Header("포르투갈어 브라질")]
    public string Name_Pt;

    [TextArea(2, 8)]
    public string Explain_Pt;

    [Header("러시아어")]
    public string Name_Ru;

    [TextArea(2, 8)]
    public string Explain_Ru;

    [Header("정답 연결 리스트 (예: 1-2, 2-3, 3-4, 3-5)")]
    public List<StarConnection> correctConnections;

    [Header("정답 별자리")]
    public GameObject constellationPrefab;
    public Vector3 CreatePosition;

    [Header("가이드 이미지")]
    public Sprite constellationGuideImage;

    [Header("가짜 별들을 배치해 둔 프리팹")]
    public GameObject fakeStarsPrefab;

    [Tooltip("가짜 별 프리팹을 생성할 월드 좌표")]
    public Vector3 fakeStarsCreatePosition;

    public string GetName()
    {
        return GetLocalizedText(
            Name_Kr,
            Name_En,
            Name_ZhHans,
            Name_Ja,
            Name_Es,
            Name_Pt,
            Name_Ru);
    }

    public string GetExplanation()
    {
        return GetLocalizedText(
            Explain_Kr,
            Explain_En,
            Explain_ZhHans,
            Explain_Ja,
            Explain_Es,
            Explain_Pt,
            Explain_Ru);
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