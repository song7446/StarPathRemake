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
            Name_ZhHans);
    }

    public string GetExplanation()
    {
        return GetLocalizedText(
            Explain_Kr,
            Explain_En,
            Explain_ZhHans);
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

        // 번역이 비어 있으면 한국어 원문 사용.
        return string.IsNullOrWhiteSpace(translatedText)
            ? korean ?? string.Empty
            : translatedText;
    }
}