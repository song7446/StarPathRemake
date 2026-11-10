using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [Header("메뉴 버튼")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button freeDrawModeButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;

    [Header("설정")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject inputBlocker;

    [Header("새로 시작 확인")]
    [SerializeField] private GameObject newGameConfirmPanel;
    [SerializeField] private Button newGameYesButton;
    [SerializeField] private Button newGameNoButton;

    [Header("효과음")]
    [SerializeField] private AudioClip gameStartClip;
    [SerializeField] private AudioClip openSettingMenuClip;

    [Header("타이틀 배경 음악")]
    [SerializeField] private AudioClip titleBGM;

    [SerializeField, Min(0f)]
    private float bgmFadeInDuration = 2f;

    [Tooltip("SceneFadeController의 Cover Duration과 맞춰주세요.")]
    [SerializeField, Min(0f)]
    private float bgmFadeOutDuration = 0.5f;

    [Header("이동할 씬 이름")]
    [SerializeField] private string gameSceneName = "MainScene";
    [SerializeField] private string freeDrawSceneName = "FreeDrawScene";

    private bool isLoading;

    private bool IsPopupOpen =>
        settingsPanel.activeSelf ||
        newGameConfirmPanel.activeSelf;

    private void Awake()
    {
        settingsPanel.SetActive(false);
        newGameConfirmPanel.SetActive(false);
        inputBlocker.SetActive(false);

        continueButton.onClick.AddListener(OnClickContinue);
        gameStartButton.onClick.AddListener(OnClickGameStart);
        freeDrawModeButton.onClick.AddListener(OnClickFreeDrawMode);
        settingButton.onClick.AddListener(OnClickSetting);
        quitButton.onClick.AddListener(OnClickQuit);

        newGameYesButton.onClick.AddListener(OnClickNewGameYes);
        newGameNoButton.onClick.AddListener(CloseNewGameConfirm);
    }

    private void Start()
    {
        if (titleBGM != null)
        {
            AudioManager.Instance.PlayBGM(
                titleBGM,
                bgmFadeInDuration);
        }
    }

    private void OnEnable()
    {
        RefreshContinueButton();
    }

    private void Update()
    {
        if (isLoading ||
            Keyboard.current?.escapeKey.wasPressedThisFrame != true)
        {
            return;
        }

        // ESC도 아니오와 동일하게 확인창만 닫는다.
        if (newGameConfirmPanel.activeSelf)
        {
            CloseNewGameConfirm();
            return;
        }

        if (settingsPanel.activeSelf)
            SetOffSetting();
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(
                GameProgressSave.TryLoad(out _));
        }
    }

    private void OnClickContinue()
    {
        if (IsPopupOpen || !CanLoadScene(gameSceneName))
            return;

        if (!GameProgressSave.TryLoad(out _))
        {
            RefreshContinueButton();
            return;
        }

        BeginSceneTransition(gameSceneName);
    }

    private void OnClickGameStart()
    {
        if (IsPopupOpen || !CanLoadScene(gameSceneName))
            return;

        if (GameProgressSave.TryLoad(out _))
        {
            // 확인 전에는 기존 기록을 삭제하지 않는다.
            inputBlocker.SetActive(true);
            newGameConfirmPanel.SetActive(true);

            PlayMenuSound();
            return;
        }

        StartNewGame();
    }

    private void OnClickNewGameYes()
    {
        if (isLoading || !newGameConfirmPanel.activeSelf)
            return;

        StartNewGame();
    }

    private void StartNewGame()
    {
        // 씬 전환이 가능한지 확인한 뒤 기록을 삭제한다.
        if (!CanLoadScene(gameSceneName))
            return;

        GameProgressSave.DeleteProgress();

        newGameConfirmPanel.SetActive(false);

        BeginSceneTransition(gameSceneName);
    }

    private void CloseNewGameConfirm()
    {
        if (isLoading || !newGameConfirmPanel.activeSelf)
            return;

        // 진행 기록은 그대로 유지한다.
        newGameConfirmPanel.SetActive(false);
        RefreshInputBlocker();

        PlayMenuSound();
    }

    private void OnClickFreeDrawMode()
    {
        if (IsPopupOpen || !CanLoadScene(freeDrawSceneName))
            return;

        BeginSceneTransition(freeDrawSceneName);
    }

    private void BeginSceneTransition(string sceneName)
    {
        // 페이드 시작 전에 중복 입력을 차단한다.
        isLoading = true;
        inputBlocker.SetActive(true);

        if (gameStartClip != null)
            AudioManager.Instance.PlaySFX(gameStartClip);

        // 음악이 끝나기를 기다리지 않고 화면 전환도 함께 시작한다.
        AudioManager.Instance.FadeOutBGM(bgmFadeOutDuration);
        SceneFadeController.Instance.LoadScene(sceneName);
    }

    private bool CanLoadScene(string sceneName)
    {
        if (isLoading)
            return false;

        if (SceneFadeController.Instance == null)
        {
            Debug.LogError(
                "SceneFadeController를 확인해주세요.",
                this);

            return false;
        }

        if (SceneFadeController.Instance.IsTransitioning)
            return false;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"씬 이름과 빌드 씬 목록을 확인해주세요: {sceneName}",
                this);

            return false;
        }

        return true;
    }

    private void OnClickSetting()
    {
        if (isLoading || IsPopupOpen)
            return;

        inputBlocker.SetActive(true);
        settingsPanel.SetActive(true);

        PlayMenuSound();
    }

    private void SetOffSetting()
    {
        settingsPanel.SetActive(false);
        RefreshInputBlocker();

        PlayMenuSound();
    }

    private void RefreshInputBlocker()
    {
        inputBlocker.SetActive(isLoading || IsPopupOpen);
    }

    private void PlayMenuSound()
    {
        if (openSettingMenuClip != null)
            AudioManager.Instance.PlaySFX(openSettingMenuClip);
    }

    private void OnClickQuit()
    {
        if (isLoading || IsPopupOpen)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnClickContinue);

        if (gameStartButton != null)
            gameStartButton.onClick.RemoveListener(OnClickGameStart);

        if (freeDrawModeButton != null)
            freeDrawModeButton.onClick.RemoveListener(OnClickFreeDrawMode);

        if (settingButton != null)
            settingButton.onClick.RemoveListener(OnClickSetting);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnClickQuit);

        if (newGameYesButton != null)
            newGameYesButton.onClick.RemoveListener(OnClickNewGameYes);

        if (newGameNoButton != null)
            newGameNoButton.onClick.RemoveListener(CloseNewGameConfirm);
    }
}