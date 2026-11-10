using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class EndingController : MonoBehaviour
{
    [Header("아버지 독백")] [SerializeField] private MonologueUI monologueUI;
    [SerializeField] private string endingDialogueId = "EndingMonologue";

    [Header("글리치 전환")] [SerializeField] private GlitchTransitionUI glitchTransition;

    [Header("장면")] [Tooltip("완성된 별들과 연결선을 함께 담는 부모")] [SerializeField]
    private GameObject constellationRoot;

    [Tooltip("경찰과 배경을 함께 담는 부모")] [SerializeField]
    private GameObject policeSceneRoot;

    [Header("경찰 자동 대사")] [SerializeField] private EndingDialogueUI endingDialogueUI;

    [Header("마지막 화면이 닫힌 후")] [SerializeField]
    private UnityEvent onEndingClosed = new();

    private bool hasStarted;
    private bool hasTransitionStarted;
    private bool policeDialogueStarted;
    private bool hasEndingClosed;

    [Header("경찰 장면 전환 대기")] [SerializeField, Min(0f)]
    private float blackScreenHoldDuration = 1.5f;

    private Tween blackScreenWait;

    [SerializeField] private AudioClip endingBGM;

    private void Awake()
    {
        // 경찰 장면은 최종적으로 보여줄 위치에 그대로 배치.
        if (policeSceneRoot != null)
            policeSceneRoot.SetActive(false);
    }

    // 별자리 변환의 On Reveal Finished에 연결.
    public void PlayEndingMonologue()
    {
        if (hasStarted || MonologueUI.IsPlaying)
            return;

        if (monologueUI == null ||
            glitchTransition == null ||
            constellationRoot == null ||
            policeSceneRoot == null ||
            endingDialogueUI == null)
        {
            Debug.LogError(
                "EndingController의 참조를 모두 연결해주세요.",
                this);
            return;
        }

        if (!DialogueRepository.Instance.TryGetChapter(
                endingDialogueId, out var dialogues) ||
            dialogues == null ||
            dialogues.Count == 0)
        {
            Debug.LogError(
                $"엔딩 독백을 찾을 수 없습니다: {endingDialogueId}",
                this);
            return;
        }

        hasStarted = true;
        
        AudioManager.Instance.PlayBGM(endingBGM, 1f);
        monologueUI.Play(
            endingDialogueId,
            ShowPoliceScene
        );

    }

    private void ShowPoliceScene()
    {
        if (hasTransitionStarted)
            return;

        hasTransitionStarted = true;

        glitchTransition.Close(() =>
        {
            // 화면이 완전히 검어진 상태에서 장면 교체.
            monologueUI.CloseWithoutCallback();

            constellationRoot.SetActive(false);
            policeSceneRoot.SetActive(true);

            // 검은 화면을 잠시 유지한 뒤 열기.
            blackScreenWait = DOVirtual.DelayedCall(
                blackScreenHoldDuration,
                () =>
                {
                    blackScreenWait = null;

                    glitchTransition.Open(() =>
                    {
                        policeDialogueStarted = true;
                        endingDialogueUI.Play();
                    });
                },
                ignoreTimeScale: true
            ).SetLink(gameObject);
        });
    }

    private void OnDestroy()
    {
        blackScreenWait?.Kill();
    }

    // EndingDialogueUI의 On Finished에 연결.
    public void CloseEndingScene()
    {
        if (!policeDialogueStarted || hasEndingClosed)
            return;

        hasEndingClosed = true;

        glitchTransition.Close(() =>
        {
            policeSceneRoot.SetActive(false);

            // 검은 화면을 유지하면서 크레딧 시작.
            onEndingClosed?.Invoke();
        });
    }
}