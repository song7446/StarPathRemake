using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EndingCreditsController : MonoBehaviour
{
    [Serializable]
    public class Stroke
    {
        [Tooltip("선을 이어갈 별을 순서대로 넣어주세요.")]
        public Transform[] stars;
    }

    [Serializable]
    public class Letter
    {
        public string letterName;

        [Tooltip("한 글자를 구성하는 획들")]
        public Stroke[] strokes;
    }

    [Header("출력")]
    [SerializeField] private RawImage creditsImage;
    [SerializeField] private Camera creditsCamera;

    [Header("별")]
    [Tooltip("제목의 별들만 들어 있는 부모")]
    [SerializeField] private Transform starsRoot;

    [Header("연결선")]
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Transform linesRoot;

    [Header("글자 순서")]
    [SerializeField] private Letter[] letters;

    [Header("시간")]
    [SerializeField, Min(0f)] private float startDelay = 1f;
    [SerializeField, Min(0.01f)] private float starFadeDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float letterDrawDuration = 0.6f;
    [SerializeField, Min(0f)] private float letterInterval = 0.05f;
    [SerializeField, Min(0f)] private float titleHoldDuration = 3f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 2f;
    [SerializeField, Min(0f)] private float endDelay = 0.5f;

    [Header("완료 후 타이틀로 이동")]
    [SerializeField] private UnityEvent onFinished = new();

    private SpriteRenderer[] starRenderers;
    private Color[] originalColors;

    private readonly List<LineRenderer> createdLines = new();

    private Coroutine playCoroutine;
    private bool hasStarted;

    private void Awake()
    {
        if (starsRoot != null)
        {
            starRenderers =
                starsRoot.GetComponentsInChildren<SpriteRenderer>(true);

            originalColors = new Color[starRenderers.Length];

            for (int i = 0; i < starRenderers.Length; i++)
                originalColors[i] = starRenderers[i].color;
        }

        if (creditsImage != null)
        {
            creditsImage.raycastTarget = false;
            creditsImage.gameObject.SetActive(false);
        }

        if (creditsCamera != null)
            creditsCamera.enabled = false;
    }

    // EndingController의 On Ending Closed에 연결.
    public void Play()
    {
        if (hasStarted)
            return;

        if (creditsImage == null ||
            creditsCamera == null ||
            starsRoot == null ||
            linesRoot == null ||
            linePrefab == null ||
            starRenderers == null)
        {
            Debug.LogError("크레딧 참조를 모두 연결해주세요.", this);
            return;
        }

        hasStarted = true;
        playCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        SetStarOpacity(0f);

        starsRoot.gameObject.SetActive(true);
        linesRoot.gameObject.SetActive(true);

        creditsImage.color = Color.white;

        // 이전 텍스처 화면이 순간적으로 나타나지 않도록
        // 먼저 카메라가 현재 상태를 렌더링하게 한다.
        creditsImage.gameObject.SetActive(false);
        creditsCamera.enabled = true;

        yield return new WaitForEndOfFrame();

        creditsImage.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(startDelay);

        float elapsed = 0f;

        while (elapsed < starFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / starFadeDuration);
            SetStarOpacity(Mathf.SmoothStep(0f, 1f, progress));

            yield return null;
        }

        SetStarOpacity(1f);

        if (letters != null)
        {
            foreach (Letter letter in letters)
            {
                if (letter == null || letter.strokes == null)
                    continue;

                int segmentCount = CountSegments(letter);

                if (segmentCount == 0)
                    continue;

                float segmentDuration =
                    letterDrawDuration / segmentCount;

                foreach (Stroke stroke in letter.strokes)
                {
                    if (!IsValidStroke(stroke))
                        continue;

                    yield return DrawStroke(stroke, segmentDuration);
                }

                yield return new WaitForSecondsRealtime(letterInterval);
            }
        }

        yield return new WaitForSecondsRealtime(titleHoldDuration);

// 선의 원래 색상과 투명도를 저장.
        var fadeLines = new List<LineRenderer>();
        var originalGradients = new List<Gradient>();

        foreach (LineRenderer line in createdLines)
        {
            if (line == null)
                continue;

            fadeLines.Add(line);
            originalGradients.Add(line.colorGradient);
        }

// RawImage는 불투명하게 유지.
// 별과 선만 사라지고 카메라의 검은 배경은 남는다.
        creditsImage.color = Color.white;

        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            float opacity = 1f - Mathf.SmoothStep(0f, 1f, progress);

            SetStarOpacity(opacity);

            for (int i = 0; i < fadeLines.Count; i++)
            {
                LineRenderer line = fadeLines[i];

                if (line == null)
                    continue;

                Gradient original = originalGradients[i];
                GradientAlphaKey[] alphaKeys = original.alphaKeys;

                for (int j = 0; j < alphaKeys.Length; j++)
                    alphaKeys[j].alpha *= opacity;

                Gradient faded = new Gradient
                {
                    mode = original.mode
                };

                faded.SetKeys(original.colorKeys, alphaKeys);
                line.colorGradient = faded;
            }

            yield return null;
        }

// 잔상이 남지 않도록 확실히 숨김.
        SetStarOpacity(0f);

        foreach (LineRenderer line in fadeLines)
        {
            if (line != null)
                line.enabled = false;
        }

        yield return new WaitForSecondsRealtime(endDelay);

// RawImage와 CreditsCamera는 켜둔 채 씬 전환.
// 제목이 사라진 검은 화면을 계속 보여준다.
        playCoroutine = null;
        onFinished?.Invoke();
    }

    private IEnumerator DrawStroke(Stroke stroke, float duration)
    {
        LineRenderer line = Instantiate(linePrefab, linesRoot);
        
        line.gameObject.layer = starsRoot.gameObject.layer;
        line.useWorldSpace = true;
        line.loop = false;
        line.enabled = true;
        line.gameObject.SetActive(true);

        createdLines.Add(line);

        line.positionCount = 1;
        line.SetPosition(0, stroke.stars[0].position);

        for (int i = 1; i < stroke.stars.Length; i++)
        {
            Vector3 from = stroke.stars[i - 1].position;
            Vector3 to = stroke.stars[i].position;

            line.positionCount = i + 1;
            line.SetPosition(i, from);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                line.SetPosition(
                    i,
                    Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration))
                );

                yield return null;
            }

            line.SetPosition(i, to);
        }
    }

    private int CountSegments(Letter letter)
    {
        int count = 0;

        foreach (Stroke stroke in letter.strokes)
        {
            if (IsValidStroke(stroke))
                count += stroke.stars.Length - 1;
        }

        return count;
    }

    private bool IsValidStroke(Stroke stroke)
    {
        if (stroke == null ||
            stroke.stars == null ||
            stroke.stars.Length < 2)
        {
            return false;
        }

        foreach (Transform star in stroke.stars)
        {
            if (star == null)
                return false;
        }

        return true;
    }

    private void SetStarOpacity(float opacity)
    {
        for (int i = 0; i < starRenderers.Length; i++)
        {
            if (starRenderers[i] == null)
                continue;

            Color color = originalColors[i];
            color.a *= opacity;

            starRenderers[i].color = color;
        }
    }

    private void OnDisable()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        if (creditsImage != null)
            creditsImage.gameObject.SetActive(false);

        if (creditsCamera != null)
            creditsCamera.enabled = false;
    }

    private void OnDestroy()
    {
        foreach (LineRenderer line in createdLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
    }
}