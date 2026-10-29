using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("화면")]
    [SerializeField] private GameObject inputBlocker;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject dialogueHistoryPanel;

    [Header("메뉴 버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button chapterSelectButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitButton;

    [Header("뒤로 버튼")]
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button historyBackButton;

    [Header("씬")]
    [SerializeField] private string titleSceneName = "TitleScene";

    private bool isInChapter;
    private bool isLeavingScene;
    private float previousTimeScale = 1f;
    
    [SerializeField] private AudioClip openPauseMenuClip;
    [SerializeField] private AudioClip closePauseMenuClip;

    private enum MenuPage
    {
        Closed,
        Main,
        Settings,
        History
    }

    private MenuPage currentPage = MenuPage.Closed;

    private void Awake()
    {
        IsOpen = false;
        HideAll();

        resumeButton.onClick.AddListener(Resume);
        historyButton.onClick.AddListener(OpenHistory);
        settingsButton.onClick.AddListener(OpenSettings);
        chapterSelectButton.onClick.AddListener(ReturnToChapterSelect);
        titleButton.onClick.AddListener(ReturnToTitle);
        quitButton.onClick.AddListener(QuitGame);

        // settingsBackButton.onClick.AddListener(ShowMainMenu);
        // historyBackButton.onClick.AddListener(ShowMainMenu);

        SetChapterPlaying(false);
    }
    
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
        {
            AudioManager.Instance.PlaySFX(openPauseMenuClip);
        }
        
        if (!IsOpen || !isInChapter)
            return;

        chapterSelectButton.interactable =
            GameManager.Instance.CanReturnToChapterSelect;
    }

    // 챕터 시작과 챕터 선택 복귀 시 호출.
    public void SetChapterPlaying(bool playing)
    {
        isInChapter = playing;

        chapterSelectButton.gameObject.SetActive(playing);

        // 활성 여부는 메뉴를 열었을 때 다시 확인.
        chapterSelectButton.interactable = false;
    }

    private void ReturnToChapterSelect()
    {
        if (!isInChapter ||
            !GameManager.Instance.CanReturnToChapterSelect)
        {
            return;
        }

        Resume();

        // SetChapterPlaying(false)는 GameManager에서 처리.
        GameManager.Instance.BackToMainMenu();
    }

    // PlayerInput의 ESC 입력에서 호출.
    public void HandleEscape()
    {
        if (isLeavingScene)
            return;

        switch (currentPage)
        {
            case MenuPage.Closed:
                OpenMenu();
                break;

            case MenuPage.Main:
                Resume();
                break;

            case MenuPage.Settings:
            case MenuPage.History:
                ShowMainMenu();
                break;
        }
    }

    private void OpenMenu()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        IsOpen = true;
        inputBlocker.SetActive(true);

        ShowMainMenu();
        
        AudioManager.Instance.SetBGMPaused(true);
    }

    private void OpenSettings()
    {
        SetPage(MenuPage.Settings);
    }

    private void OpenHistory()
    {
        SetPage(MenuPage.History);
    }

    public void ShowMainMenu()
    {
        if (!IsOpen)
            return;

        // 설정이나 기록에서 돌아오는 경우에는 닫기 소리.
        bool isBack =
            currentPage == MenuPage.Settings ||
            currentPage == MenuPage.History;

        if (currentPage == MenuPage.Main)
            return;

        SetPage(MenuPage.Main, false);

        AudioManager.Instance.PlaySFX(
            isBack ? closePauseMenuClip : openPauseMenuClip
        );
    }

    private void SetPage(MenuPage page, bool playSound = true)
    {
        if (!IsOpen || currentPage == page)
            return;

        currentPage = page;

        pauseMenuPanel.SetActive(page == MenuPage.Main);
        settingsPanel.SetActive(page == MenuPage.Settings);
        dialogueHistoryPanel.SetActive(page == MenuPage.History);

        if (playSound)
            AudioManager.Instance.PlaySFX(openPauseMenuClip);
    }

    public void Resume()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        currentPage = MenuPage.Closed;

        Time.timeScale = previousTimeScale;

        HideAll();

        AudioManager.Instance.SetBGMPaused(false);
        AudioManager.Instance.PlaySFX(closePauseMenuClip);
    }

    private void ReturnToTitle()
    {
        if (isLeavingScene)
            return;

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError(
                $"타이틀 씬 이름과 빌드 씬 목록을 확인하세요: {titleSceneName}",
                this
            );
            return;
        }

        isLeavingScene = true;

        Resume();
        Time.timeScale = 1f;

        SceneManager.LoadSceneAsync(
            titleSceneName,
            LoadSceneMode.Single
        );
    }

    private void QuitGame()
    {
        Resume();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HideAll()
    {
        inputBlocker.SetActive(false);
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        dialogueHistoryPanel.SetActive(false);
    }

    private void OnDisable()
    {
        // 메뉴가 열린 채 오브젝트가 비활성화되어도
        // Time.timeScale이 0으로 남지 않도록 복구.
        Resume();
    }
}