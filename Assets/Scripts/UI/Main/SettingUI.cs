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
        English = 1,
        SimplifiedChinese = 2,
        Japanese = 3,
        Spanish = 4,
        Portuguese = 5,
        Russian = 6
    }

    public static GameLanguage CurrentLanguage
    {
        get
        {
            int savedLanguage = PlayerPrefs.GetInt(
                LanguageKey,
                (int)GameLanguage.English);

            return System.Enum.IsDefined(
                typeof(GameLanguage), savedLanguage)
                ? (GameLanguage)savedLanguage
                : GameLanguage.English;
        }
    }

    public static bool UseEnglish =>
        CurrentLanguage == GameLanguage.English;

    public static event System.Action LanguageChanged;

    // 씬이 바뀌어도 유지한다.
    private static bool displaySettingsApplied;

    // 해상도 변경은 즉시 완료되지 않을 수 있으므로
    // 적용 직후에는 실제 화면 상태 동기화를 잠시 기다린다.
    private static bool waitingForDisplayChange;
    private static bool requestedFullscreen;
    private static int displayRequestFrame;
    private static float displayRequestTime;

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

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDisplayInitialization()
    {
        displaySettingsApplied = false;
        waitingForDisplayChange = false;
        requestedFullscreen = false;
        displayRequestFrame = 0;
        displayRequestTime = 0f;
    }

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
        RefreshVolumeControls();

        bool firstInitialization = !displaySettingsApplied;

        bool fullscreen = firstInitialization
            ? PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0) == 1
            : GetCurrentFullscreenState();

        fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        InitializeResolutions();

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string>
        {
            "한국어",
            "English",
            "中文",
            "日本語",
            "Español",
            "Português",
            "Русский"
        });

        RefreshLanguageControl();

        initialized = true;

        if (firstInitialization)
        {
            // 최초 실행 때만 저장된 화면 설정을 적용한다.
            displaySettingsApplied = true;

            SaveDisplaySettings();
            ApplyDisplaySettings();
        }
        else
        {
            // 씬 이동으로 새 설정창이 생성되어도
            // 화면 모드를 다시 적용하지 않는다.
            SynchronizeFullscreenState();
        }
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        RefreshVolumeControls();
        RefreshLanguageControl();
        InitializeResolutions();

        fullscreenToggle.SetIsOnWithoutNotify(
            GetCurrentFullscreenState());

        SynchronizeFullscreenState();
    }

    private void Update()
    {
        if (!initialized)
            return;

        // 설정창이 열린 상태에서 Alt+Enter를 눌러도 반영한다.
        SynchronizeFullscreenState();
    }

    private static bool GetCurrentFullscreenState()
    {
        return waitingForDisplayChange
            ? requestedFullscreen
            : Screen.fullScreen;
    }

    private void SynchronizeFullscreenState()
    {
        if (waitingForDisplayChange)
        {
            // 요청한 프레임 직후의 이전 상태를 저장하지 않는다.
            if (Time.frameCount <= displayRequestFrame + 1)
                return;

            bool requestCompleted =
                Screen.fullScreen == requestedFullscreen;

            bool timedOut =
                Time.realtimeSinceStartup - displayRequestTime >= 2f;

            if (!requestCompleted && !timedOut)
                return;

            waitingForDisplayChange = false;
        }

        bool actualFullscreen = Screen.fullScreen;

        fullscreenToggle.SetIsOnWithoutNotify(actualFullscreen);

        int actualValue = actualFullscreen ? 1 : 0;

        if (!PlayerPrefs.HasKey(FullscreenKey) ||
            PlayerPrefs.GetInt(FullscreenKey) != actualValue)
        {
            PlayerPrefs.SetInt(FullscreenKey, actualValue);
            hasPendingChanges = true;
        }
    }

    private void ConfigureVolumeSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void RefreshVolumeControls()
    {
        LoadVolume(
            masterSlider,
            masterValueText,
            MasterKey,
            "MasterVolume");

        LoadVolume(
            bgmSlider,
            bgmValueText,
            BgmKey,
            "BGMVolume");

        LoadVolume(
            sfxSlider,
            sfxValueText,
            SfxKey,
            "SFXVolume");
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
        SetVolume(
            value, MasterKey, "MasterVolume", masterValueText);
    }

    private void SetBgmVolume(float value)
    {
        SetVolume(
            value, BgmKey, "BGMVolume", bgmValueText);
    }

    private void SetSfxVolume(float value)
    {
        SetVolume(
            value, SfxKey, "SFXVolume", sfxValueText);
    }

    private void SetVolume(
        float value,
        string saveKey,
        string mixerParameter,
        TMP_Text label)
    {
        value = Mathf.Clamp01(value);

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

        // 주사율이 달라도 동일한 해상도는 한 번만 등록.
        // 16:9가 아닌 해상도는 AddResolution에서 제외한다.
        foreach (Resolution resolution in Screen.resolutions)
            AddResolution(resolution.width, resolution.height);

        AddResolution(Screen.width, Screen.height);

        // 해상도 목록이 없는 환경의 대체 해상도.
        if (resolutions.Count == 0)
        {
            int unit = Mathf.Max(
                1,
                Mathf.Min(Screen.width / 16, Screen.height / 9));

            AddResolution(unit * 16, unit * 9);
        }

        resolutions.Sort((a, b) =>
        {
            int widthCompare = a.x.CompareTo(b.x);

            return widthCompare != 0
                ? widthCompare
                : a.y.CompareTo(b.y);
        });

        Vector2Int savedResolution = new Vector2Int(
            PlayerPrefs.GetInt(WidthKey, Screen.width),
            PlayerPrefs.GetInt(HeightKey, Screen.height));

        int selectedIndex = resolutions.IndexOf(savedResolution);

        if (selectedIndex < 0)
        {
            long closestDistance = long.MaxValue;
            selectedIndex = 0;

            for (int i = 0; i < resolutions.Count; i++)
            {
                long widthDifference =
                    (long)resolutions[i].x - savedResolution.x;

                long heightDifference =
                    (long)resolutions[i].y - savedResolution.y;

                long distance =
                    widthDifference * widthDifference +
                    heightDifference * heightDifference;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    selectedIndex = i;
                }
            }
        }

        var options = new List<string>();

        foreach (Vector2Int resolution in resolutions)
            options.Add($"{resolution.x} × {resolution.y}");

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        resolutionDropdown.RefreshShownValue();

        Vector2Int selectedResolution = resolutions[selectedIndex];

        PlayerPrefs.SetInt(WidthKey, selectedResolution.x);
        PlayerPrefs.SetInt(HeightKey, selectedResolution.y);

        hasPendingChanges = true;
    }

    private void AddResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        // 정확한 16:9 해상도만 허용.
        if ((long)width * 9 != (long)height * 16)
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

        // 해상도를 바꿀 때도 실제 전체화면 상태를 유지한다.
        fullscreenToggle.SetIsOnWithoutNotify(
            GetCurrentFullscreenState());

        SaveDisplaySettings();
        ApplyDisplaySettings();
    }

    private void SetFullscreen(bool fullscreen)
    {
        if (!initialized)
            return;

        fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        SaveDisplaySettings();
        ApplyDisplaySettings();
    }

    private void SaveDisplaySettings()
    {
        if (resolutions.Count == 0)
            return;

        int index = Mathf.Clamp(
            resolutionDropdown.value,
            0,
            resolutions.Count - 1);

        Vector2Int resolution = resolutions[index];

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

        int index = Mathf.Clamp(
            resolutionDropdown.value,
            0,
            resolutions.Count - 1);

        Vector2Int resolution = resolutions[index];

        requestedFullscreen = fullscreenToggle.isOn;
        waitingForDisplayChange = true;
        displayRequestFrame = Time.frameCount;
        displayRequestTime = Time.realtimeSinceStartup;

        FullScreenMode mode = requestedFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(resolution.x, resolution.y, mode);
    }

    private void RefreshLanguageControl()
    {
        languageDropdown.SetValueWithoutNotify(
            (int)CurrentLanguage);

        languageDropdown.RefreshShownValue();
    }

    private void SetLanguage(int index)
    {
        if (!initialized)
            return;

        if (!System.Enum.IsDefined(typeof(GameLanguage), index))
            return;

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
        if (!paused)
            return;

        if (initialized)
            SynchronizeFullscreenState();

        SaveSettings();
    }

    private void OnDisable()
    {
        if (initialized)
            SynchronizeFullscreenState();

        SaveSettings();
    }

    private void OnApplicationQuit()
    {
        if (initialized)
            SynchronizeFullscreenState();

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