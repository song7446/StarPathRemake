using System.Collections;
using DG.Tweening;
using UnityEngine;

public class IntroController : MonoBehaviour
{
    [Header("독백")] [SerializeField] private MonologueUI monologueUI;
    [SerializeField] private string introDialogueId = "Intro";

    [SerializeField] private CanvasGroup monologueUIGroup;

    private IEnumerator Start()
    {
        // GameManager 등 다른 컴포넌트의 Start 초기화 이후 실행.
        yield return null;
        
        // 씬 전환 화면이 걷힌 뒤 인트로 시작.
        while (SceneFadeController.Instance != null &&
               SceneFadeController.Instance.IsTransitioning)
        {
            yield return null;
        }

        if (monologueUI == null)
        {
            Debug.LogError("MonologueUI를 연결해주세요.", this);
            FinishIntro();
            yield break;
        }

        // 데이터가 없을 때 화면이 계속 가려지는 것 방지.
        if (!DialogueRepository.Instance.TryGetChapter(
                introDialogueId, out var dialogues) ||
            dialogues.Count == 0)
        {
            Debug.LogWarning(
                $"인트로 대사를 찾을 수 없습니다: {introDialogueId}",
                this);

            FinishIntro();
            yield break;
        }

        monologueUI.Play(introDialogueId, FinishIntro);
    }

    private void FinishIntro()
    {
        monologueUIGroup.DOKill();

        monologueUIGroup
            .DOFade(0f, 2f)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                // 페이드가 끝난 뒤 Panel과 입력 차단 해제.
                if (monologueUI != null)
                    monologueUI.CloseWithoutCallback();

                Destroy(gameObject);
            });
    }
}