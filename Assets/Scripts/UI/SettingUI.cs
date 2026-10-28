using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    private const string MasterKey = "Settings.MasterVolume";
    private const string BgmKey = "Settings.BGMVolume";
    private const string SfxKey = "Settings.SFXVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string WidthKey = "Settings.ResolutionWidth";
    private const string HeightKey = "Settings.ResolutionHeight";
    private const string LanguageKey = "Settings.Language";

    public enum GameLanguage
    {
        Korean = 0,
        English = 1
    }

    // 다른 컴포넌트의 초기화 순서에 관계없이 저장값 확인.
    public static GameLanguage CurrentLanguage =>
        (GameLanguage)Mathf.Clamp(
            PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);

    public static bool UseEnglish =>
        CurrentLanguage == GameLanguage.English;

    public static event System.Action LanguageChanged;

    [Header("오디오")]
    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [Header("화면")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("언어")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private readonly List<Vector2Int> resolutions = new();

    private bool hasPendingChanges;
    private bool initialized;

    private void Awake()
    {
        ConfigureVolumeSlider(masterSlider);
        ConfigureVolumeSlider(bgmSlider);
        ConfigureVolumeSlider(sfxSlider);

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBgmVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        languageDropdown.onValueChanged.AddListener(SetLanguage);
    }

    private void Start()
    {
        LoadVolume(masterSlider, masterValueText,
            MasterKey, "MasterVolume");

        LoadVolume(bgmSlider, bgmValueText,
            BgmKey, "BGMVolume");

        LoadVolume(sfxSlider, sfxValueText,
            SfxKey, "SFXVolume");

        bool fullscreen = PlayerPrefs.GetInt(
            FullscreenKey,
            Screen.fullScreen ? 1 : 0
        ) == 1;

        fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        InitializeResolutions();

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string>
        {
            "한국어",
            "English"
        });

        languageDropdown.SetValueWithoutNotify(
            (int)CurrentLanguage);

        languageDropdown.RefreshShownValue();

        initialized = true;

        ApplyDisplaySettings();
    }

    private void ConfigureVolumeSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void LoadVolume(
        Slider slider,
        TMP_Text label,
        string saveKey,
        string mixerParameter)
    {
        float value = Mathf.Clamp01(
            PlayerPrefs.GetFloat(saveKey, 1f));

        slider.SetValueWithoutNotify(value);

        ApplyVolume(mixerParameter, value);
        UpdateVolumeText(label, value);
    }

    private void SetMasterVolume(float value)
    {
        SetVolume(value, MasterKey, "MasterVolume", masterValueText);
    }

    private void SetBgmVolume(float value)
    {
        SetVolume(value, BgmKey, "BGMVolume", bgmValueText);
    }

    private void SetSfxVolume(float value)
    {
        SetVolume(value, SfxKey, "SFXVolume", sfxValueText);
    }

    private void SetVolume(
        float value,
        string saveKey,
        string mixerParameter,
        TMP_Text label)
    {
        ApplyVolume(mixerParameter, value);
        UpdateVolumeText(label, value);

        PlayerPrefs.SetFloat(saveKey, value);
        hasPendingChanges = true;
    }

    private void ApplyVolume(string parameter, float value)
    {
        if (audioMixer == null)
            return;

        float decibels = value <= 0f
            ? -80f
            : Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(parameter, decibels);
    }

    private void UpdateVolumeText(TMP_Text label, float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    private void InitializeResolutions()
    {
        resolutions.Clear();

        // 주사율만 다른 동일 해상도는 한 번만 등록.
        foreach (Resolution resolution in Screen.resolutions)
            AddResolution(resolution.width, resolution.height);

        // 에디터나 목록이 없는 환경에서도 현재 크기는 사용 가능.
        AddResolution(Screen.width, Screen.height);

        resolutions.Sort((a, b) =>
        {
            int widthCompare = a.x.CompareTo(b.x);

            return widthCompare != 0
                ? widthCompare
                : a.y.CompareTo(b.y);
        });

        Vector2Int savedResolution = new Vector2Int(
            PlayerPrefs.GetInt(WidthKey, Screen.width),
            PlayerPrefs.GetInt(HeightKey, Screen.height)
        );

        int selectedIndex = resolutions.IndexOf(savedResolution);

        // 모니터 변경 등으로 저장된 해상도가 없다면 현재 크기 사용.
        if (selectedIndex < 0)
        {
            selectedIndex = resolutions.IndexOf(
                new Vector2Int(Screen.width, Screen.height));
        }

        var options = new List<string>();

        foreach (Vector2Int resolution in resolutions)
            options.Add($"{resolution.x} × {resolution.y}");

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);

        resolutionDropdown.SetValueWithoutNotify(
            Mathf.Max(0, selectedIndex));

        resolutionDropdown.RefreshShownValue();
    }

    private void AddResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        Vector2Int resolution = new Vector2Int(width, height);

        if (!resolutions.Contains(resolution))
            resolutions.Add(resolution);
    }

    private void SetResolution(int index)
    {
        if (!initialized ||
            index < 0 ||
            index >= resolutions.Count)
        {
            return;
        }

        SaveDisplaySettings();
        ApplyDisplaySettings();
    }

    private void SetFullscreen(bool fullscreen)
    {
        if (!initialized)
            return;

        SaveDisplaySettings();
        ApplyDisplaySettings();
    }

    private void SaveDisplaySettings()
    {
        Vector2Int resolution =
            resolutions[resolutionDropdown.value];

        PlayerPrefs.SetInt(WidthKey, resolution.x);
        PlayerPrefs.SetInt(HeightKey, resolution.y);

        PlayerPrefs.SetInt(
            FullscreenKey,
            fullscreenToggle.isOn ? 1 : 0);

        hasPendingChanges = true;
    }

    private void ApplyDisplaySettings()
    {
        if (resolutions.Count == 0)
            return;

        Vector2Int resolution =
            resolutions[resolutionDropdown.value];

        FullScreenMode mode = fullscreenToggle.isOn
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(resolution.x, resolution.y, mode);
    }

    private void SetLanguage(int index)
    {
        if (!initialized)
            return;

        index = Mathf.Clamp(index, 0, 1);

        if ((int)CurrentLanguage == index)
            return;

        PlayerPrefs.SetInt(LanguageKey, index);
        hasPendingChanges = true;

        LanguageChanged?.Invoke();
    }

    public void SaveSettings()
    {
        if (!hasPendingChanges)
            return;

        PlayerPrefs.Save();
        hasPendingChanges = false;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveSettings();
    }

    private void OnDisable()
    {
        SaveSettings();
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(SetBgmVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(SetResolution);

        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(SetLanguage);
    }
}