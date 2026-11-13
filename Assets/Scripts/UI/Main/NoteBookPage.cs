using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoteBookPage : MonoBehaviour
{
    [Header("가이드 사진 버튼")]
    [SerializeField] private Button changeGuideImageButton;

    [Header("별자리 데이터")]
    [SerializeField] private ConstellationData constellationData;

    [Header("페이지 텍스트")]
    [SerializeField] private TMP_Text constellationNameText;
    [SerializeField] private TMP_Text constellationExplanationText;

    [Header("페이지 안의 가이드 이미지")]
    [SerializeField] private TMP_Text guideImageNameText;
    
    private void Awake()
    {
        if (changeGuideImageButton != null)
        {
            changeGuideImageButton.onClick.AddListener(
                OnClickChangeGuideImage);
        }
    }

    private void OnEnable()
    {
        SettingsUI.LanguageChanged += RefreshTexts;

        RefreshTexts();
    }

    private void OnDisable()
    {
        SettingsUI.LanguageChanged -= RefreshTexts;
    }

    public void RefreshTexts()
    {
        if (constellationData == null)
            return;

        string constellationName = constellationData.GetName();

        if (constellationNameText != null)
            constellationNameText.text = constellationName;

        if (guideImageNameText != null)
            guideImageNameText.text = constellationName;

        if (constellationExplanationText != null)
            constellationExplanationText.text =
                constellationData.GetExplanation();
    }

    private void OnClickChangeGuideImage()
    {
        if (constellationData == null)
            return;

        UIManager.Instance.ReplaceGuideConstellation(
            constellationData);
    }

    private void OnDestroy()
    {
        if (changeGuideImageButton != null)
        {
            changeGuideImageButton.onClick.RemoveListener(
                OnClickChangeGuideImage);
        }
    }
}