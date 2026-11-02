using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FreeDrawUIManager : SceneMonoBehaviourSingleton<FreeDrawUIManager>
{
    [SerializeField] private Button createStarButton;
    [SerializeField] private Button moveStarButton;
    [SerializeField] private Button connectStarButton;
    [SerializeField] private Button deleteStarButton;
    [SerializeField] private FreeModeStarController freeModeStarController;

    private void Start()
    {
        createStarButton.onClick.AddListener(OnClickCreateStar);
        moveStarButton.onClick.AddListener(OnClickMoveStar);
        deleteStarButton.onClick.AddListener(OnClickDeleteStar);
        connectStarButton.onClick.AddListener(OnClickConnectStar);
    }

    private void OnClickCreateStar()
    {
        freeModeStarController.CreateStarAtCenter();
    }

    private void OnClickMoveStar()
    {
        freeModeStarController.SetMoveMode();
    }
    
    private void OnClickConnectStar()
    {
        freeModeStarController.SetConnectMode();
    }

    private void OnClickDeleteStar()
    {
        freeModeStarController.DeleteSelectedStars();
    }
}
