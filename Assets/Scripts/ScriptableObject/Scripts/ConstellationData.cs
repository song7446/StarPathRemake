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

[System.Serializable]
public class FakeConstellationPlacement
{
    [Tooltip("가짜로 사용할 이전 별자리 데이터")]
    public ConstellationData constellationData;

    [Tooltip("현재 정답 별자리의 생성 위치를 기준으로 한 상대 위치")]
    public Vector3 localPosition;

    public Vector3 rotation;

    [Min(0.01f)]
    public float scale = 1f;
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
    public Vector3 CreatePosition;
    
    [Header("정답 연결 리스트 (예: 1-2, 2-3, 3-4, 3-5)")]
    public List<StarConnection> correctConnections;
    
    [Header("정답 별자리")]
    public GameObject constellationPrefab;
    
    public Sprite constellationGuideImage;
    
    [Header("주변에 배치할 가짜 별자리")]
    public List<FakeConstellationPlacement> fakeConstellations
        = new List<FakeConstellationPlacement>();

    [Header("낱개로 배치할 가짜 별")]
    public PuzzleStar fakeStarPrefab;

    [Tooltip("현재 정답 별자리의 생성 위치 기준 상대 위치")]
    public List<Vector3> fakeStarPositions
        = new List<Vector3>();
}