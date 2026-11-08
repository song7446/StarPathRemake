using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class LocalizedFont : MonoBehaviour
{
    [SerializeField] private LanguageFontSettings fontSettings;

    private TMP_Text targetText;

    private bool hasCapturedBaseSize;
    private float baseFontSize;
    private float baseFontSizeMin;
    private float baseFontSizeMax;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        SettingsUI.LanguageChanged += RefreshFont;
        RefreshFont();
    }

    private void OnDisable()
    {
        SettingsUI.LanguageChanged -= RefreshFont;
    }

    private void Initialize()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (targetText == null || hasCapturedBaseSize)
            return;

        // 처음 설정된 크기를 한 번만 저장한다.
        // 비활성화 후 다시 켜져도 기준 크기는 바뀌지 않는다.
        baseFontSize = targetText.fontSize;
        baseFontSizeMin = targetText.fontSizeMin;
        baseFontSizeMax = targetText.fontSizeMax;

        hasCapturedBaseSize = true;
    }

    public void RefreshFont()
    {
        Initialize();

        if (targetText == null || fontSettings == null)
            return;

        SettingsUI.GameLanguage language = SettingsUI.CurrentLanguage;

        TMP_FontAsset font = fontSettings.GetFont(language);

        if (font == null)
        {
            Debug.LogWarning(
                $"언어별 폰트가 연결되지 않았습니다: {language}",
                this);

            return;
        }

        if (targetText.font != font)
        {
            targetText.font = font;

            // 이전 폰트의 머티리얼이 남지 않도록 함께 교체한다.
            targetText.fontSharedMaterial = font.material;
        }

        // 같은 폰트를 쓰는 언어끼리 변경해도 크기는 다시 적용한다.
        float multiplier = fontSettings.GetSizeMultiplier(language);

        if (targetText.enableAutoSizing)
        {
            targetText.fontSizeMin = baseFontSizeMin * multiplier;
            targetText.fontSizeMax = baseFontSizeMax * multiplier;
        }
        else
        {
            targetText.fontSize = baseFontSize * multiplier;
        }
    }
}