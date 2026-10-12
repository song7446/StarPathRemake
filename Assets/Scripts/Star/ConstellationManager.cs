using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SongLib.Core.Singleton;
using UnityEngine;

public class ConstellationManager : MonoBehaviourSingleton<ConstellationManager>
{
    [SerializeField] private List<ConstellationData> constellations = new List<ConstellationData>();
    private ConstellationData _currentConstellationData;
    private List<PuzzleStar> _currentConstellationStars = new List<PuzzleStar>();
    private List<PuzzleStar> allStars = new List<PuzzleStar>();

    private bool isWrongLineDrawn;

    [SerializeField] private float starAppearInterval = 0.3f;
    private Coroutine starAppearCoroutine;

    [SerializeField, Min(0.01f)] private float starFadeInDuration = 0.8f;

    public void CreateConstellation(ConstellationData constellationData)
    {
        if (starAppearCoroutine != null)
        {
            StopCoroutine(starAppearCoroutine);
            starAppearCoroutine = null;
        }

        _currentConstellationStars.Clear();
        allStars.Clear();
        _currentConstellationData = constellationData;

        GameObject constellation = Instantiate(constellationData.constellationPrefab, constellationData.CreatePosition,
            Quaternion.identity);
        List<PuzzleStar> stars =
            constellation.GetComponentsInChildren<PuzzleStar>(true).ToList();

        allStars = stars;

        PuzzleStar[] childStars = constellation.GetComponentsInChildren<PuzzleStar>();

        foreach (PuzzleStar childStar in childStars)
        {
            childStar.gameObject.SetActive(false);
            _currentConstellationStars.Add(childStar);
        }

        starAppearCoroutine = StartCoroutine(FadeInStars(stars));
    }

    private IEnumerator FadeInStars(List<PuzzleStar> stars)
    {
        var renderers = new List<SpriteRenderer>();
        var originalColors = new List<Color>();

        // 모든 별을 활성화하고 투명하게 준비.
        foreach (PuzzleStar star in stars)
        {
            if (star == null)
                continue;

            star.gameObject.SetActive(true);

            foreach (SpriteRenderer renderer in
                     star.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // 중복 등록 방지.
                if (renderers.Contains(renderer))
                    continue;

                Color originalColor = renderer.color;

                renderers.Add(renderer);
                originalColors.Add(originalColor);

                Color transparentColor = originalColor;
                transparentColor.a = 0f;
                renderer.color = transparentColor;
            }
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, starFadeInDuration);

        // 같은 진행률로 모든 별을 동시에 표시.
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color color = originalColors[i];
                color.a *= progress;

                renderers[i].color = color;
            }

            yield return null;
        }

        // 원래 색상과 투명도로 정확히 복구.
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = originalColors[i];
            }
        }

        starAppearCoroutine = null;
    }

    public void FadeOutStarts()
    {
        StartCoroutine(FadeOutStars(allStars, 1f));
    }

    private IEnumerator FadeOutStars(
        List<PuzzleStar> stars,
        float duration = 0.5f)
    {
        var renderers = new List<SpriteRenderer>();
        var originalColors = new List<Color>();

        foreach (PuzzleStar star in stars)
        {
            if (star == null)
                continue;

            foreach (SpriteRenderer renderer in
                     star.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderers.Contains(renderer))
                    continue;

                renderers.Add(renderer);
                originalColors.Add(renderer.color);
            }
        }

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color color = originalColors[i];
                color.a *= 1f - progress;

                renderers[i].color = color;
            }

            yield return null;
        }

        foreach (PuzzleStar star in stars)
        {
            if (star != null)
                star.gameObject.SetActive(false);
        }

        // 다음 페이드 인을 위해 비활성화 상태에서 색상 복구
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originalColors[i];
        }
    }

    private IEnumerator ShowStars(PuzzleStar[] stars)
    {
        WaitForSeconds delay = new WaitForSeconds(starAppearInterval);

        foreach (PuzzleStar star in stars)
        {
            if (star == null)
                continue;

            star.gameObject.SetActive(true);
            yield return delay;
        }

        starAppearCoroutine = null;
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

            GameManager.Instance.StartChapter(GameManager.Instance.currentChapterIdx + 1);

            UIManager.Instance.ToggleCamera();
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

            foreach (var star in _currentConstellationStars)
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