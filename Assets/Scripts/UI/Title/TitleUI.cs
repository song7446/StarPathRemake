using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button freeDrawModeButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject inputBlocker;
    [SerializeField] private AudioClip gameStartClip;
    [SerializeField] private AudioClip openSettingMenuClip;

    [Header("이동할 게임 씬 이름")]
    [SerializeField] private string gameSceneName = "MainScene";

    private bool isLoading;

    private void Awake()
    {
        gameStartButton.onClick.AddListener(OnClickGameStart);
        
        settingButton.onClick.AddListener(OnClickSetting);
        quitButton.onClick.AddListener(OnClickQuit);
    }

    private void Update()
    {
        if (isLoading || !settingsPanel.activeSelf)
            return;

        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            SetOffSetting();
    }

    private void OnClickSetting()
    {
        settingsPanel.SetActive(true);
        inputBlocker.SetActive(true);
        AudioManager.Instance.PlaySFX(openSettingMenuClip);
    }

    private void SetOffSetting()
    {
        settingsPanel.SetActive(false);
        inputBlocker.SetActive(false);
        AudioManager.Instance.PlaySFX(openSettingMenuClip);
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnClickGameStart()
    {
        if (isLoading || SceneFadeController.Instance.IsTransitioning)
            return;

        isLoading = true;

        SceneFadeController.Instance.LoadScene(gameSceneName);
    }

    private void OnDestroy()
    {
        if (gameStartButton != null)
            gameStartButton.onClick.RemoveListener(OnClickGameStart);
    }
}