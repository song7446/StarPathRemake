using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFadeController : MonoBehaviour
{
    public static SceneFadeController Instance { get; private set; }

    public bool IsTransitioning { get; private set; }

    [Header("화면 전체를 덮는 검은색 패널")]
    [SerializeField] private CanvasGroup fadeGroup;

    [Header("시간")]
    [SerializeField, Min(0f)] private float coverDuration = 0.5f;
    [SerializeField, Min(0f)] private float revealDuration = 0.7f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        fadeGroup.gameObject.SetActive(false);
        fadeGroup.alpha = 0f;
        fadeGroup.interactable = false;
        fadeGroup.blocksRaycasts = false;
    }

    public void LoadScene(string sceneName)
    {
        if (IsTransitioning)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"씬 이름과 빌드 씬 목록을 확인해주세요: {sceneName}",
                this);

            return;
        }

        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        fadeGroup.gameObject.SetActive(true);
        IsTransitioning = true;
        fadeGroup.blocksRaycasts = true;

        // 현재 화면을 검게 덮는다.
        yield return FadeTo(1f, coverDuration);

        // 완전히 가려진 뒤 씬을 전환한다.
        Time.timeScale = 1f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(
            sceneName,
            LoadSceneMode.Single);

        if (operation != null)
        {
            while (!operation.isDone)
                yield return null;
        }

        // 새 씬의 Start 초기화 이후 화면을 보여준다.
        yield return null;

        yield return FadeTo(0f, revealDuration);

        fadeGroup.blocksRaycasts = false;
        IsTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = fadeGroup.alpha;

        if (duration <= 0f)
        {
            fadeGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            fadeGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                easedProgress);

            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}