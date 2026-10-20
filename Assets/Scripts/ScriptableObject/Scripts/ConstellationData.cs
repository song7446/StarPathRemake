using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct StarConnection
{
    public int starA;
    public int starB;

    // 순서 상관없이 동일한 연결인지 확인하는 메서드
    public bool IsSameConnection(int id1, int id2)
    {
        return (starA == id1 && starB == id2) || (starA == id2 && starB == id1);
    }
}

[CreateAssetMenu(fileName = "NewConstellation", menuName = "StarPath/ConstellationData")]
public class ConstellationData : ScriptableObject
{
    [Header("다국어 번역 키")]
    public string constellationId;

    public string Name_Kr;
    public string Explain_Kr;
    public string Name_En;
    public string Explain_En;
    
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
}