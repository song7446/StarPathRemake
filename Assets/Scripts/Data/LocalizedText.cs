using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Header("출력 대상")]
    [SerializeField] private TMP_Text targetText;

    [Header("한국어")]
    [SerializeField, TextArea(1, 5)]
    private string textKr;

    [Header("영어")]
    [SerializeField, TextArea(1, 5)]
    private string textEn;

    [Header("중국어 간체")]
    [SerializeField, TextArea(1, 5)]
    private string textZhHans;

    [Header("일본어")]
    [SerializeField, TextArea(1, 5)]
    private string textJa;

    [Header("스페인어")]
    [SerializeField, TextArea(1, 5)]
    private string textEs;

    [Header("브라질 포르투갈어")]
    [SerializeField, TextArea(1, 5)]
    private string textPt;

    [Header("러시아어")]
    [SerializeField, TextArea(1, 5)]
    private string textRu;

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();

        // 처음 추가할 때 기존 TMP 문구를 한국어로 보관한다.
        if (targetText != null)
            textKr = targetText.text;
    }

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        SettingsUI.LanguageChanged += RefreshText;
        RefreshText();
    }

    private void OnDisable()
    {
        SettingsUI.LanguageChanged -= RefreshText;
    }

    public void RefreshText()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (targetText == null)
            return;

        string text = SettingsUI.CurrentLanguage switch
        {
            SettingsUI.GameLanguage.Korean => textKr,
            SettingsUI.GameLanguage.English => textEn,
            SettingsUI.GameLanguage.SimplifiedChinese => textZhHans,
            SettingsUI.GameLanguage.Japanese => textJa,
            SettingsUI.GameLanguage.Spanish => textEs,
            SettingsUI.GameLanguage.Portuguese => textPt,
            SettingsUI.GameLanguage.Russian => textRu,
            _ => textEn
        };

        // 선택한 언어의 번역이 없으면 영어로 표시한다.
        targetText.text = string.IsNullOrWhiteSpace(text)
            ? textEn ?? string.Empty
            : text;
    }
}