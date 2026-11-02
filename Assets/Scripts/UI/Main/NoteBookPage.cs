using System;
using UnityEngine;
using UnityEngine.UI;

public class NoteBookPage : MonoBehaviour
{
    [SerializeField] private Button changeGuideImageButton;
    [SerializeField] private ConstellationData constellationData;

    private void Start()
    {
        changeGuideImageButton.onClick.AddListener(OnClickChangeGuideImage);
    }

    private void OnClickChangeGuideImage()
    {
        if (constellationData == null)
            return;
        
        UIManager.Instance.ChangeGuideName(constellationData.Name_Kr);

        UIManager.Instance.ReplaceGuideImage(
            constellationData.constellationGuideImage);
    }
}