using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;

public class ConstellationManager : MonoBehaviourSingleton<ConstellationManager>
{
    [SerializeField] private List<ConstellationData> constellations = new List<ConstellationData>();
    private ConstellationData _currentConstellationData;
    [SerializeField] private List<PuzzleStar> currentConstellationStars;

    private bool isWrongLineDrawn;

    public void CreateConstellation(ConstellationData constellationData)
    {
        _currentConstellationData = constellationData;
        
        Instantiate(constellationData.constellationPrefab, constellationData.CreatePosition, Quaternion.identity);
    }

    public void CheckAnswer(List<StarConnection> currentConnections)
    {
        foreach (var drawnConn in currentConnections)
        {
            bool matchFound = false;
            foreach (var answerConn in _currentConstellationData.correctConnections)
            {
                // IsSameConnection이 방향 상관없이 같은 연결인지(A-B == B-A) 체크해준다고 가정합니다.
                if (answerConn.IsSameConnection(drawnConn.starA, drawnConn.starB))
                {
                    matchFound = true;
                    break;
                }
            }

            // 정답에 없는 엄한 선을 그었다면 즉시 오답
            if (!matchFound)
            {
                isWrongLineDrawn = true;
                break;
            }
        }

        if (isWrongLineDrawn)
        {
            Debug.Log("오답: 잘못된 연결이 포함되어 있습니다!");
            // (선택) 여기서 모든 선을 초기화하거나, 플레이어에게 틀렸다는 피드백을 줄 수 있습니다.
        }
        else if (currentConnections.Count == _currentConstellationData.correctConnections.Count)
        {
            // 잘못된 선도 없고, 그은 선의 개수도 정답과 똑같다면? (모양 완벽 일치!)
            Debug.Log("★ 정답! 별자리를 완벽하게 완성했습니다! ★");

            //  TODO:스테이지 클리어 이벤트 호출

            GameManager.Instance.StartNextChapter();
        }
        else
        {
            Debug.Log($"진행 중... ({currentConnections.Count}/{_currentConstellationData.correctConnections.Count})");
        }
    }

    public void CompleteConstellation()
    {
        foreach (var connection in _currentConstellationData.correctConnections)
        {
            int id1 = connection.starA;
            int id2 = connection.starB;

            PuzzleStar starA = gameObject.AddComponent<PuzzleStar>();
            PuzzleStar starB = gameObject.AddComponent<PuzzleStar>();

            foreach (var star in currentConstellationStars)
            {
                if (star.starID == id1)
                {
                    starA = star;
                }

                if (star.starID == id2)
                {
                    starB = star;
                }
            }

            DrawingManager.Instance.DrawEditorLine(starA, starB);
        }
    }
}