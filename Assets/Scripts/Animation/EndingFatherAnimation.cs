using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EndingFatherAnimation : MonoBehaviour
{
    [Serializable]
    public class InitialObject
    {
        [Tooltip("AnimationCommand의 characterId와 동일하게 입력")]
        public string characterId;

        [Tooltip("씬에 미리 배치되어 있는 그림 오브젝트")]
        public GameObject sceneObject;
    }

    [Serializable]
    public class AnimationStep
    {
        public AnimationCommand command;

        [Header("페이드 시간")]
        [Min(0.01f)]
        public float fadeOutDuration = 1f;

        [Min(0.01f)]
        public float fadeInDuration = 2f;

        [Tooltip("페이드가 모두 끝난 후 다음 단계까지 대기할 시간")]
        [Min(0f)]
        public float holdDuration = 0.5f;
    }

    [Header("엔딩 장면")]
    [Tooltip("EndingController의 Father Scene Root와 같은 오브젝트")]
    [SerializeField] private Transform fatherSceneRoot;

    [Header("씬에 미리 배치한 교체 대상")]
    [SerializeField] private InitialObject[] initialObjects;

    [Header("자동 실행 순서")]
    [SerializeField] private AnimationStep[] steps;

    [Header("시작 전 대기")]
    [SerializeField, Min(0f)]
    private float startDelay = 0.5f;

    [Header("전체 명령 완료")]
    [SerializeField] private UnityEvent onFinished = new();

    private bool hasStarted;
    private Coroutine playRoutine;

    public void Play()
    {
        if (hasStarted)
            return;

        if (fatherSceneRoot == null ||
            AnimationManager.Instance == null ||
            CutSceneCastManager.Instance == null)
        {
            Debug.LogError(
                "엔딩 장면과 애니메이션 매니저를 확인해주세요.",
                this);

            return;
        }

        if (!fatherSceneRoot.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "아버지 장면이 활성화된 후 연출을 시작해주세요.",
                this);

            return;
        }

        if (steps == null || steps.Length == 0)
        {
            Debug.LogError(
                "엔딩 자동 실행 순서를 등록해주세요.",
                this);

            return;
        }

        // 실행 전에 모든 단계를 검사한다.
        foreach (AnimationStep step in steps)
        {
            if (step == null || step.command == null)
            {
                Debug.LogError(
                    "엔딩 실행 순서에 비어 있는 명령이 있습니다.",
                    this);

                return;
            }
        }

        if (!ValidateInitialObjects())
            return;

        RegisterInitialObjects();

        hasStarted = true;
        playRoutine = StartCoroutine(PlayRoutine());
    }

    private bool ValidateInitialObjects()
    {
        if (initialObjects == null)
            return true;

        var ids = new System.Collections.Generic.HashSet<string>();

        foreach (InitialObject entry in initialObjects)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.characterId) ||
                entry.sceneObject == null)
            {
                Debug.LogError(
                    "초기 오브젝트의 ID와 대상을 모두 연결해주세요.",
                    this);

                return false;
            }

            if (!ids.Add(entry.characterId))
            {
                Debug.LogError(
                    $"초기 오브젝트 ID가 중복됩니다: {entry.characterId}",
                    this);

                return false;
            }

            // 장면 전체를 교체 대상으로 등록하면 안 된다.
            if (entry.sceneObject.transform == fatherSceneRoot ||
                !entry.sceneObject.transform.IsChildOf(fatherSceneRoot))
            {
                Debug.LogError(
                    $"초기 오브젝트는 Father Scene Root의 자식이어야 합니다: " +
                    entry.characterId,
                    this);

                return false;
            }

            GameObject existing =
                CutSceneCastManager.Instance.GetObjectByKey(
                    entry.characterId);

            if (existing != null && existing != entry.sceneObject)
            {
                Debug.LogError(
                    $"이미 다른 오브젝트가 사용하는 ID입니다: " +
                    $"{entry.characterId}. 엔딩 전용 ID를 사용해주세요.",
                    this);

                return false;
            }
        }

        return true;
    }

    private void RegisterInitialObjects()
    {
        if (initialObjects == null)
            return;

        foreach (InitialObject entry in initialObjects)
        {
            // 삭제된 오브젝트의 키가 남아 있어도 다시 등록한다.
            CutSceneCastManager.Instance.RemoveObjectFromDictByKey(
                entry.characterId);

            CutSceneCastManager.Instance.PushObject(
                entry.characterId,
                entry.sceneObject);
        }
    }

    private IEnumerator PlayRoutine()
    {
        yield return WaitForSecondsUnpaused(startDelay);

        foreach (AnimationStep step in steps)
        {
            // 이전 그림 교체와 일시정지가 끝난 뒤 실행한다.
            while (AnimationManager.Instance.IsTransitioning ||
                   PauseMenuUI.IsOpen)
            {
                yield return null;
            }

            AnimationManager.Instance.PlayCommand(
                step.command,
                fatherSceneRoot,
                step.fadeOutDuration,
                step.fadeInDuration);

            // 명령 실행과 완료 확인 사이에 한 프레임을 둔다.
            yield return null;

            while (AnimationManager.Instance.IsTransitioning ||
                   PauseMenuUI.IsOpen)
            {
                yield return null;
            }

            yield return WaitForSecondsUnpaused(step.holdDuration);
        }

        playRoutine = null;
        onFinished?.Invoke();
    }

    private IEnumerator WaitForSecondsUnpaused(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration || PauseMenuUI.IsOpen)
        {
            if (!PauseMenuUI.IsOpen)
                elapsed += Time.deltaTime;

            yield return null;
        }
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }
}