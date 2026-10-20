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

    private GameObject currentConstellationObject;
    private Coroutine starDisappearCoroutine;

    public void CreateConstellation(ConstellationData constellationData)
    {
        if (constellationData == null ||
            constellationData.constellationPrefab == null)
        {
            Debug.LogError("별자리 데이터 또는 프리팹이 없습니다.", this);
            return;
        }

        if (starAppearCoroutine != null)
        {
            StopCoroutine(starAppearCoroutine);
            starAppearCoroutine = null;
        }

        if (starDisappearCoroutine != null)
        {
            StopCoroutine(starDisappearCoroutine);
            starDisappearCoroutine = null;
        }

        if (currentConstellationObject != null)
        {
            currentConstellationObject.SetActive(false);
            Destroy(currentConstellationObject);
        }

        _currentConstellationStars.Clear();
        allStars.Clear();

        _currentConstellationData = constellationData;
        isWrongLineDrawn = false;

        // 정답과 가짜 별을 함께 관리하는 부모.
        currentConstellationObject =
            new GameObject("ConstellationGroup");

        Transform root = currentConstellationObject.transform;
        root.position = new Vector2(0, 0);

        // 정답 별자리 생성.
        GameObject answerObject = Instantiate(
            constellationData.constellationPrefab,
            constellationData.CreatePosition,
            constellationData.constellationPrefab.transform.localRotation,
            root);

        PuzzleStar[] answerStars =
            answerObject.GetComponentsInChildren<PuzzleStar>(true);

        // 정답에 사용되는 번호를 먼저 확보.
        var usedIDs = new HashSet<int>();

        foreach (PuzzleStar star in answerStars)
        {
            usedIDs.Add(star.starID);

            _currentConstellationStars.Add(star);
            allStars.Add(star);
        }

        // 데이터에 적힌 정답 번호도 가짜 별에 배정하지 않음.
        if (constellationData.correctConnections != null)
        {
            foreach (StarConnection connection in
                     constellationData.correctConnections)
            {
                usedIDs.Add(connection.starA);
                usedIDs.Add(connection.starB);
            }
        }

        int nextFakeID = 0;

        // 이전 별자리 프리팹을 가짜로 생성.
        if (constellationData.fakeConstellations != null)
        {
            foreach (FakeConstellationPlacement placement in
                     constellationData.fakeConstellations)
            {
                if (placement == null ||
                    placement.constellationData == null ||
                    placement.constellationData.constellationPrefab == null)
                {
                    continue;
                }

                // 해당 데이터의 프리팹만 사용.
                // 그 데이터에 등록된 가짜 별자리까지 생성하지는 않음.
                GameObject fakeObject = Instantiate(
                    placement.constellationData.constellationPrefab,
                    root,
                    false);

                fakeObject.name =
                    "Fake_" + placement.constellationData.name;

                fakeObject.transform.localPosition =
                    placement.localPosition;

                fakeObject.transform.localRotation =
                    Quaternion.Euler(placement.rotation);

                fakeObject.transform.localScale *=
                    Mathf.Max(0.01f, placement.scale);

                foreach (PuzzleStar star in
                         fakeObject.GetComponentsInChildren<PuzzleStar>(true))
                {
                    RegisterFakeStar(star, usedIDs, ref nextFakeID);
                }
            }
        }

        // 낱개 가짜 별 생성.
        if (constellationData.fakeStarPrefab != null &&
            constellationData.fakeStarPositions != null)
        {
            foreach (Vector3 position in
                     constellationData.fakeStarPositions)
            {
                PuzzleStar fakeStar = Instantiate(
                    constellationData.fakeStarPrefab,
                    root,
                    false);

                fakeStar.transform.localPosition = position;

                RegisterFakeStar(fakeStar, usedIDs, ref nextFakeID);
            }
        }

        // 정답과 가짜 별 모두 같은 방식으로 등장.
        foreach (PuzzleStar star in allStars)
        {
            if (star != null)
                star.gameObject.SetActive(false);
        }

        starAppearCoroutine = StartCoroutine(FadeInStars(allStars));
    }

    private void RegisterFakeStar(
        PuzzleStar star,
        HashSet<int> usedIDs,
        ref int nextFakeID)
    {
        while (usedIDs.Contains(nextFakeID))
            nextFakeID++;

        int assignedID = nextFakeID;

        usedIDs.Add(assignedID);
        nextFakeID++;

        star.SetupStar(assignedID, false);
        star.gameObject.name = $"FakeStar_{assignedID}";

        allStars.Add(star);
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
        if (currentConstellationObject == null ||
            starDisappearCoroutine != null)
            return;

        // 등장 페이드와 충돌하지 않도록 중단
        if (starAppearCoroutine != null)
        {
            StopCoroutine(starAppearCoroutine);
            starAppearCoroutine = null;
        }

        starDisappearCoroutine = StartCoroutine(
            FadeOutStars(currentConstellationObject, 1f));
    }

    private IEnumerator FadeOutStars(
        GameObject target,
        float duration = 0.5f)
    {
        SpriteRenderer[] renderers =
            target.GetComponentsInChildren<SpriteRenderer>(true);

        Color[] startColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        // 사라지는 동안 별 클릭 방지
        foreach (Collider2D col in
                 target.GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = false;
        }

        foreach (Collider col in
                 target.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color color = startColors[i];
                color.a *= 1f - progress;
                renderers[i].color = color;
            }

            yield return null;
        }

        if (target != null)
            Destroy(target);

        if (currentConstellationObject == target)
        {
            currentConstellationObject = null;
            _currentConstellationStars.Clear();
            allStars.Clear();
            _currentConstellationData = null;
            isWrongLineDrawn = false;
        }

        starDisappearCoroutine = null;
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
        if (_currentConstellationData == null ||
            _currentConstellationData.correctConnections == null ||
            currentConnections == null ||
            starDisappearCoroutine != null)
        {
            return;
        }

        isWrongLineDrawn = false;

        List<StarConnection> answers =
            _currentConstellationData.correctConnections;

        foreach (StarConnection drawnConnection in currentConnections)
        {
            bool matchFound = false;

            foreach (StarConnection answerConnection in answers)
            {
                if (answerConnection.IsSameConnection(
                        drawnConnection.starA,
                        drawnConnection.starB))
                {
                    matchFound = true;
                    break;
                }
            }

            if (!matchFound)
            {
                isWrongLineDrawn = true;
                break;
            }
        }

        if (isWrongLineDrawn)
        {
            Debug.Log("오답: 잘못된 연결이 포함되어 있습니다!");
            return;
        }

        // 모든 정답 연결이 실제로 존재하는지도 확인.
        bool allAnswersDrawn = answers.Count > 0;

        foreach (StarConnection answerConnection in answers)
        {
            bool exists = currentConnections.Exists(connection =>
                connection.IsSameConnection(
                    answerConnection.starA,
                    answerConnection.starB));

            if (!exists)
            {
                allAnswersDrawn = false;
                break;
            }
        }

        if (allAnswersDrawn &&
            currentConnections.Count == answers.Count)
        {
            Debug.Log("★ 정답! 별자리를 완벽하게 완성했습니다! ★");

            GameManager.Instance.StartChapter(
                GameManager.Instance.currentChapterIdx + 1,
                true);

            UIManager.Instance.ToggleCamera();
        }
        else
        {
            Debug.Log(
                $"진행 중... ({currentConnections.Count}/{answers.Count})");
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