using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class ToggleCameraUI : MonoBehaviour
{
    [Header("가상 카메라 (Main Camera 제어용)")] [SerializeField]
    private CinemachineCamera ccGround;

    [SerializeField] private CinemachineCamera ccSky;
    [SerializeField] private CinemachineCamera ccStarSky;
    [SerializeField] private CinemachineCamera ccStarGround;

    [SerializeField] private Button _lookUpCameraButton;

    private bool IsSkyView = false;
    
    [SerializeField] private AudioClip toggleCameraAudioClip;


    public void Awake()
    {
        ccGround.Priority = 10;
        ccSky.Priority = 0;

        ccStarGround.Priority = 10;
        ccStarSky.Priority = 0;

        _lookUpCameraButton.onClick.AddListener(OnClickLookUpCameraButton);
    }

    /// <summary>
    /// UI 버튼 하나로 하늘 뷰와 땅 뷰를 토글(왕복)합니다.
    /// </summary>
    public void OnClickLookUpCameraButton()
    {
        if (AnimationManager.Instance.IsTransitioning)
            return;

        IsSkyView = !IsSkyView;

        FadeObjects(IsSkyView);

        if (IsSkyView)
        {
            ccSky.Priority = 20;
            ccStarSky.Priority = 20;

            UIManager.Instance.GetOutDialogueUI();
        }
        else
        {
            ccSky.Priority = 0;
            ccStarSky.Priority = 0;

            UIManager.Instance.GetInDialogueUI();
        }
        
        AudioManager.Instance.PlaySFX(toggleCameraAudioClip);
    }

    public bool GetIsOnSkyView()
    {
        return IsSkyView;
    }

    [SerializeField] private float fadeDuration = 0.4f;
    private readonly Dictionary<SpriteRenderer, float> originalAlphas = new();
    private readonly List<Tween> fadeTweens = new();

    private void FadeObjects(bool hide)
    {
        // 이전 카메라 페이드만 중단
        foreach (Tween tween in fadeTweens)
            tween.Kill();

        fadeTweens.Clear();

        GameObject[] targets =
            CutSceneCastManager.Instance.GetAllObjects();

        var currentRenderers = new HashSet<SpriteRenderer>();

        foreach (GameObject target in targets)
        {
            if (target == null)
                continue;

            foreach (SpriteRenderer sr in
                     target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                currentRenderers.Add(sr);
            }
        }

        // 삭제되거나 교체된 대상의 기록 정리
        var removedRenderers = new List<SpriteRenderer>();

        foreach (var pair in originalAlphas)
        {
            if (pair.Key == null || !currentRenderers.Contains(pair.Key))
                removedRenderers.Add(pair.Key);
        }

        foreach (SpriteRenderer sr in removedRenderers)
            originalAlphas.Remove(sr);

        // 활성화 상태는 유지하고 알파만 변경
        foreach (SpriteRenderer sr in currentRenderers)
        {
            if (!originalAlphas.TryGetValue(sr, out float originalAlpha))
            {
                originalAlpha = sr.color.a;
                originalAlphas.Add(sr, originalAlpha);
            }

            fadeTweens.Add(
                sr.DOFade(
                        hide ? 0f : originalAlpha,
                        Mathf.Max(0f, fadeDuration))
                    .SetEase(Ease.InOutSine)
                    .SetLink(sr.gameObject)
            );
        }
    }
}