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
    
    [Header("정답 연결 리스트 (예: 1-2, 2-3, 3-4, 3-5)")]
    public List<StarConnection> correctConnections;
    
    public GameObject constellationPrefab;
    
    public Sprite constellationGuideImage;
}