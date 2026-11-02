using UnityEngine;
using UnityEngine.Events;

public class EndingController : MonoBehaviour
{
    [Header("기존 독백 UI")]
    [SerializeField] private MonologueUI monologueUI;

    [SerializeField] private string endingDialogueId = "EndingMonologue";

    [Header("독백 종료 후")]
    [SerializeField] private UnityEvent onMonologueFinished = new();

    private bool hasStarted;

    // 별자리 변환의 On Reveal Finished에 연결.
    public void PlayEndingMonologue()
    {
        if (hasStarted || MonologueUI.IsPlaying)
            return;

        if (monologueUI == null)
        {
            Debug.LogError("엔딩에 사용할 MonologueUI를 연결해주세요.", this);
            return;
        }

        // 데이터가 없으면 시작 상태로 잠기지 않도록 먼저 확인.
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

        monologueUI.Play(endingDialogueId, FinishMonologue);
    }

    private void FinishMonologue()
    {
        onMonologueFinished?.Invoke();
    }
}