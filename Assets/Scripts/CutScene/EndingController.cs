using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class EndingController : MonoBehaviour
{
    [Header("아버지 독백")]
    [SerializeField] private MonologueUI monologueUI;
    [SerializeField] private string endingDialogueId =
        "EndingMonologue";

    [Header("아버지 장면")]
    [Tooltip("아빠와 배경을 함께 담는 부모")]
    [SerializeField] private GameObject fatherSceneRoot;

    [Header("독백 종료 후 자동 연출")]
    [Tooltip("독백이 끝나면 죽음과 카메라 연출을 시작")]
    [SerializeField] private UnityEvent onFatherAnimationStarted =
        new();

    [Header("글리치 전환")]
    [SerializeField] private GlitchTransitionUI glitchTransition;

    [Header("장면")]
    [Tooltip("완성된 별들과 연결선을 함께 담는 부모")]
    [SerializeField] private GameObject constellationRoot;

    [Tooltip("경찰과 배경을 함께 담는 부모")]
    [SerializeField] private GameObject policeSceneRoot;

    [Header("경찰 자동 대사")]
    [SerializeField] private EndingDialogueUI endingDialogueUI;

    [Header("경찰 장면 전환 대기")]
    [SerializeField, Min(0f)]
    private float blackScreenHoldDuration = 1.5f;

    [Header("마지막 화면이 닫힌 후")]
    [SerializeField] private UnityEvent onEndingClosed = new();

    [Header("배경 음악")]
    [SerializeField] private AudioClip endingBGM;

    private bool hasStarted;
    private bool hasFatherAnimationStarted;
    private bool hasTransitionStarted;
    private bool policeDialogueStarted;
    private bool hasEndingClosed;

    private Tween blackScreenWait;
    
    [Header("아버지 장면 등장")]
    [SerializeField, Min(0.01f)]
    private float fatherFadeInDuration = 1.5f;

    private Sequence fatherEntranceSequence;

    private void Awake()
    {
        if (fatherSceneRoot != null)
            fatherSceneRoot.SetActive(false);

        if (policeSceneRoot != null)
            policeSceneRoot.SetActive(false);
    }

    // EndingSkyTransition의 On Transition Finished에 연결.
    public void PlayEndingMonologue()
    {
        if (hasStarted || MonologueUI.IsPlaying)
            return;

        if (monologueUI == null ||
            fatherSceneRoot == null ||
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

        if (endingBGM != null)
            AudioManager.Instance.PlayBGM(endingBGM, 1f);

        ShowFatherScene();

        monologueUI.Play(
            endingDialogueId,
            StartFatherAnimation);
    }
    
    private void ShowFatherScene()
    {
        fatherEntranceSequence = DOTween.Sequence();

        // 렌더러가 없어도 지정한 시간이 지난 뒤 독백을 시작한다.
        fatherEntranceSequence.AppendInterval(fatherFadeInDuration);

        foreach (SpriteRenderer renderer in
                 fatherSceneRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            float originalAlpha = renderer.color.a;

            // 활성화하기 전에 투명하게 준비한다.
            Color color = renderer.color;
            color.a = 0f;
            renderer.color = color;

            fatherEntranceSequence.Insert(
                0f,
                renderer.DOFade(originalAlpha, fatherFadeInDuration)
                    .SetEase(Ease.InOutSine));
        }

        fatherSceneRoot.SetActive(true);

        fatherEntranceSequence.SetLink(gameObject);

        fatherEntranceSequence.OnComplete(() =>
        {
            fatherEntranceSequence = null;

            monologueUI.Play(
                endingDialogueId,
                StartFatherAnimation);
        });
    }

    private void StartFatherAnimation()
    {
        if (!hasStarted ||
            hasFatherAnimationStarted ||
            hasTransitionStarted)
        {
            return;
        }

        hasFatherAnimationStarted = true;

        // 대사창을 닫고 이후에는 연출만 자동으로 진행한다.
        monologueUI.CloseWithoutCallback();

        onFatherAnimationStarted?.Invoke();
    }

    // 죽음과 카메라 연출이 모두 끝났을 때 호출.
    public void FinishFatherAnimation()
    {
        if (!hasFatherAnimationStarted ||
            hasTransitionStarted)
        {
            return;
        }

        ShowPoliceScene();
    }

    private void ShowPoliceScene()
    {
        if (hasTransitionStarted)
            return;

        hasTransitionStarted = true;

        AudioManager.Instance.StopLoopSFX();

        glitchTransition.Close(() =>
        {
            // 완전히 검어진 뒤 아빠 장면을 정리한다.
            monologueUI.CloseWithoutCallback();

            fatherSceneRoot.SetActive(false);
            constellationRoot.SetActive(false);

            policeSceneRoot.SetActive(true);

            // 검은 화면을 잠시 유지한 뒤 경찰 장면을 보여준다.
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

    // EndingDialogueUI의 On Finished에 연결.
    public void CloseEndingScene()
    {
        if (!policeDialogueStarted || hasEndingClosed)
            return;

        hasEndingClosed = true;

        glitchTransition.Close(() =>
        {
            policeSceneRoot.SetActive(false);

            // 검은 화면을 유지하면서 크레딧을 시작한다.
            onEndingClosed?.Invoke();
        });
    }

    private void OnDestroy()
    {
        fatherEntranceSequence?.Kill();
        blackScreenWait?.Kill();
    }
}