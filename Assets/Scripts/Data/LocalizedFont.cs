using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class LocalizedFont : MonoBehaviour
{
    [SerializeField] private LanguageFontSettings fontSettings;

    private TMP_Text targetText;

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();
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

    public void RefreshFont()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (targetText == null || fontSettings == null)
            return;

        TMP_FontAsset font = fontSettings.GetFont(
            SettingsUI.CurrentLanguage);

        if (font == null)
        {
            Debug.LogWarning(
                $"언어별 폰트가 연결되지 않았습니다: " +
                $"{SettingsUI.CurrentLanguage}",
                this);

            return;
        }

        if (targetText.font == font)
            return;

        targetText.font = font;

        // 이전 폰트의 머티리얼이 남지 않도록 함께 교체한다.
        targetText.fontSharedMaterial = font.material;
    }
}