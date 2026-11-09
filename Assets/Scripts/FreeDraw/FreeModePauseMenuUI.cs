using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FreeModePauseMenuUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject inputBlocker;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject exitConfirmPanel;

    [Header("메뉴 버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitButton;
    

    [Header("나가기 확인 버튼")]
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;

    [Header("자유모드 입력")]
    [SerializeField] private FreeModeStarController starController;
    [SerializeField] private FreeModeDrawingManager drawingManager;

    [Header("효과음")]
    [SerializeField] private AudioClip openMenuClip;
    [SerializeField] private AudioClip closeMenuClip;

    [Header("타이틀 씬")]
    [SerializeField] private string titleSceneName = "TitleScene";

    private enum MenuPage
    {
        Closed,
        Main,
        Settings,
        ConfirmExit
    }

    private enum ExitAction
    {
        Title,
        Quit
    }

    private MenuPage currentPage = MenuPage.Closed;
    private ExitAction pendingExit;

    private bool isLeavingScene;
    private float previousTimeScale = 1f;

    public bool IsOpen => currentPage != MenuPage.Closed;

    private void Awake()
    {
        HideAll();

        resumeButton.onClick.AddListener(Resume);
        settingsButton.onClick.AddListener(OpenSettings);
        titleButton.onClick.AddListener(RequestReturnToTitle);
        quitButton.onClick.AddListener(RequestQuit);
        
        exitYesButton.onClick.AddListener(ConfirmExit);
        exitNoButton.onClick.AddListener(ShowMainMenu);
    }

    private void Update()
    {
        if (isLeavingScene)
            return;

        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            HandleEscape();
    }

    public void HandleEscape()
    {
        if (isLeavingScene)
            return;

        // 다른 씬으로 전환하는 동안 메뉴를 열지 않는다.
        if (SceneFadeController.Instance != null &&
            SceneFadeController.Instance.IsTransitioning)
        {
            return;
        }

        switch (currentPage)
        {
            case MenuPage.Closed:
                OpenMenu();
                break;

            case MenuPage.Main:
                Resume();
                break;

            case MenuPage.Settings:
            case MenuPage.ConfirmExit:
                ShowMainMenu();
                break;
        }
    }

    private void OpenMenu()
    {
        if (IsOpen || isLeavingScene)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 완료한 작업은 유지하고 진행 중인 조작만 종료한다.
        if (starController != null)
            starController.OnPauseMenuOpened();

        if (drawingManager != null)
            drawingManager.OnPauseMenuOpened();

        inputBlocker.SetActive(true);
        SetPage(MenuPage.Main);

        AudioManager.Instance.SetBGMPaused(true);
        PlaySound(openMenuClip);
    }

    public void Resume()
    {
        if (!IsOpen || isLeavingScene)
            return;

        CloseMenu();
        PlaySound(closeMenuClip);
    }

    private void OpenSettings()
    {
        if (currentPage != MenuPage.Main || isLeavingScene)
            return;

        SetPage(MenuPage.Settings);
        PlaySound(openMenuClip);
    }

    private void ShowMainMenu()
    {
        if (!IsOpen ||
            currentPage == MenuPage.Main ||
            isLeavingScene)
        {
            return;
        }

        SetPage(MenuPage.Main);
        PlaySound(closeMenuClip);
    }

    private void RequestReturnToTitle()
    {
        RequestExit(ExitAction.Title);
    }

    private void RequestQuit()
    {
        RequestExit(ExitAction.Quit);
    }

    private void RequestExit(ExitAction action)
    {
        if (currentPage != MenuPage.Main || isLeavingScene)
            return;

        pendingExit = action;

        SetPage(MenuPage.ConfirmExit);
        PlaySound(openMenuClip);
    }

    private void ConfirmExit()
    {
        if (currentPage != MenuPage.ConfirmExit || isLeavingScene)
            return;

        if (pendingExit == ExitAction.Title)
        {
            if (SceneFadeController.Instance == null)
            {
                Debug.LogError(
                    "SceneFadeController를 확인해주세요.",
                    this);

                return;
            }

            if (SceneFadeController.Instance.IsTransitioning)
                return;

            if (string.IsNullOrWhiteSpace(titleSceneName) ||
                !Application.CanStreamedLevelBeLoaded(titleSceneName))
            {
                Debug.LogError(
                    $"타이틀 씬 이름과 빌드 씬 목록을 확인해주세요: " +
                    titleSceneName,
                    this);

                return;
            }
        }

        isLeavingScene = true;

        // 씬을 떠나기 전에 일시정지 상태를 복원한다.
        CloseMenu();
        Time.timeScale = 1f;

        // 전환이 끝나기 전까지 UI 클릭을 막는다.
        inputBlocker.SetActive(true);

        PlaySound(closeMenuClip);

        if (pendingExit == ExitAction.Title)
        {
            SceneFadeController.Instance.LoadScene(titleSceneName);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPage(MenuPage page)
    {
        currentPage = page;

        pauseMenuPanel.SetActive(page == MenuPage.Main);
        settingsPanel.SetActive(page == MenuPage.Settings);
        exitConfirmPanel.SetActive(page == MenuPage.ConfirmExit);
    }

    private void CloseMenu()
    {
        if (!IsOpen)
            return;

        currentPage = MenuPage.Closed;
        Time.timeScale = previousTimeScale;

        HideAll();

        AudioManager.Instance.SetBGMPaused(false);
    }

    private void HideAll()
    {
        inputBlocker.SetActive(false);
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        exitConfirmPanel.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
            AudioManager.Instance.PlaySFX(clip);
    }

    private void OnDisable()
    {
        // 메뉴가 열린 채 비활성화되어도 시간을 복원한다.
        CloseMenu();
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettings);

        if (titleButton != null)
            titleButton.onClick.RemoveListener(RequestReturnToTitle);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(RequestQuit);
        
        if (exitYesButton != null)
            exitYesButton.onClick.RemoveListener(ConfirmExit);

        if (exitNoButton != null)
            exitNoButton.onClick.RemoveListener(ShowMainMenu);
    }
}