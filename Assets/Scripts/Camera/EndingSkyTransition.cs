using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class EndingSkyTransition : MonoBehaviour
{
    [Header("별 카메라")]
    [Tooltip("실제 별 카메라에 붙어 있는 CinemachineBrain")]
    [SerializeField] private CinemachineBrain starCameraBrain;

    [Tooltip("Size 30으로 설정한 엔딩용 가상 카메라")]
    [SerializeField] private CinemachineCamera endingStarCamera;

    [Header("완성된 별자리 감상")]
    [SerializeField, Min(0f)]
    private float holdDuration = 2.5f;

    [Header("카메라 전환 완료")]
    [SerializeField] private UnityEvent onTransitionFinished = new();

    private bool hasStarted;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        // 오브젝트는 켜두고 우선순위로 전환한다.
        if (endingStarCamera != null)
            endingStarCamera.Priority = 0;
    }

    // ChapterConstellationReveal의 On Reveal Finished에 연결.
    public void Play()
    {
        if (hasStarted)
            return;

        if (starCameraBrain == null || endingStarCamera == null)
        {
            Debug.LogError(
                "별 카메라의 Brain과 엔딩 가상 카메라를 연결해주세요.",
                this);

            return;
        }

        hasStarted = true;
        transitionCoroutine = StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        // 완성된 별자리를 잠깐 보여준다.
        yield return new WaitForSeconds(holdDuration);

        while (PauseMenuUI.IsOpen)
            yield return null;

        endingStarCamera.Priority = 30;

        // Cinemachine이 새 카메라를 반영할 시간을 준다.
        yield return null;
        yield return new WaitForEndOfFrame();

        // 엔딩 카메라로 전환되고 블렌드가 끝날 때까지 기다린다.
        while (starCameraBrain.ActiveVirtualCamera != endingStarCamera ||
               starCameraBrain.IsBlending ||
               PauseMenuUI.IsOpen)
        {
            yield return null;
        }

        transitionCoroutine = null;
        onTransitionFinished?.Invoke();
    }

    private void OnDestroy()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
    }
}