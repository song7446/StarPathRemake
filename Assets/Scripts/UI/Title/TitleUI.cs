using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [SerializeField] private Button gameStartButton;

    [Header("이동할 게임 씬 이름")]
    [SerializeField] private string gameSceneName = "MainScene";

    private bool isLoading;

    private void Awake()
    {
        gameStartButton.onClick.AddListener(OnClickGameStart);
    }

    private void OnClickGameStart()
    {
        if (isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError(
                $"씬을 불러올 수 없습니다. 씬 이름과 빌드 씬 목록을 확인하세요: {gameSceneName}",
                this
            );

            return;
        }

        isLoading = true;
        gameStartButton.interactable = false;

        // 일시정지 상태에서 타이틀로 돌아왔더라도 정상 시작.
        Time.timeScale = 1f;

        SceneManager.LoadSceneAsync(
            gameSceneName,
            LoadSceneMode.Single
        );
    }

    private void OnDestroy()
    {
        if (gameStartButton != null)
            gameStartButton.onClick.RemoveListener(OnClickGameStart);
    }
}