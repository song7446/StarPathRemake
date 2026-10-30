using System;
using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class GameManager : SceneMonoBehaviourSingleton<GameManager>
{
    public ChapterDefinition[] ChapterDefinitions;
    public bool[] isWatched;

    private ChapterDefinition _currentChapterDefinition;
    public ChapterDefinition CurrentChapterDefinition => _currentChapterDefinition;

    private string[] chapterIds =
    {
        "1_Front", "1_Back", "2_Front", "2_Back", "3_Front", "3_Back", "4_Front", "4_Back", "5_Front", "5_Back",
        "6_Front", "6_Back"
    };

    public bool IsFront => currentChapterIdx % 2 == 0;
    private bool _fromFront;

    private bool _isShowingChapterTitle;

    public int currentChapterIdx = 0;

    [SerializeField] private PauseMenuUI pauseMenuUI;

    private bool isPreparingChapter;
    private bool isChapterActive;
    private bool isReturningToMenu;

    private bool titleFinished;
    private bool objectsFinished;

    private Sequence chapterEntranceSequence;
    private Coroutine chapterReadyCoroutine;
    private Coroutine returnCoroutine;

    // 챕터 진입 완료 여부.
    public bool IsChapterReady =>
        isChapterActive &&
        !isPreparingChapter &&
        !isReturningToMenu;

    // 대사 중 캐릭터 교체 애니메이션도 함께 검사.
    public bool CanReturnToChapterSelect =>
        IsChapterReady &&
        !AnimationManager.Instance.IsTransitioning;

    [SerializeField] private AudioClip baseBGM;
    
    [SerializeField] private MonologueUI monologueUI;

    [SerializeField]
    private MonologueDefinition[] chapterMonologues;

// 현재 플레이에서 끝까지 본 독백 ID.
    private readonly HashSet<string> playedMonologueIds = new();

    private void Start()
    {
        isWatched = new bool[ChapterDefinitions.Length];

        UIManager.Instance.InitializeChapterButtons();

        AudioManager.Instance.PlayBGM(baseBGM, 5);
    }

    public void StartChapter(int idx, bool fromFront = false)
    {
        if (isPreparingChapter || isReturningToMenu)
            return;

        if (idx < 0 ||
            idx >= ChapterDefinitions.Length ||
            idx >= chapterIds.Length ||
            ChapterDefinitions[idx] == null)
        {
            return;
        }

        // 시작 요청을 받는 순간부터 복귀 차단.
        isPreparingChapter = true;
        isChapterActive = true;

        titleFinished = fromFront;
        objectsFinished = false;

        _currentChapterDefinition = ChapterDefinitions[idx];
        currentChapterIdx = idx;
        _fromFront = fromFront;

        pauseMenuUI.SetChapterPlaying(true);

        AnimationManager.Instance.CommandsToDictionary();

        UIManager.Instance.SetTitle(
            _currentChapterDefinition.chapterNumber,
            $"{_currentChapterDefinition.chapterTitle} - " +
            $"{_currentChapterDefinition.chapterSubtitle}"
        );

        if (!fromFront)
        {
            UIManager.Instance.FadeInTitle(
                onFinished: () => { titleFinished = true; },
                onFadeOutStarted: CreateChapterObjects
            );
            
            AudioManager.Instance.FadeOutBGM(2f,
                () => { AudioManager.Instance.PlayBGM(_currentChapterDefinition.bgm, 2); });
        }
        else
        {
            CreateChapterObjects();
        }
        
        chapterReadyCoroutine =
            StartCoroutine(WaitForChapterReady(idx, fromFront));
    }

    private void CreateChapterObjects()
    {
        if (_fromFront)
        {
            CutSceneCastManager.Instance.CreateCharacterWithNoAnim(
                _currentChapterDefinition.cutSceneCasts
            );

            objectsFinished = true;
            return;
        }

        chapterEntranceSequence = DOTween.Sequence();

        CutSceneCastManager.Instance.CreateCharacterWithAnim(
            _currentChapterDefinition.cutSceneCasts,
            chapterEntranceSequence
        );

        if (_currentChapterDefinition.constellationData != null)
        {
            ConstellationManager.Instance.CreateConstellation(
                _currentChapterDefinition.constellationData
            );
        }

        // 등장할 오브젝트가 없는 챕터도 처리.
        if (chapterEntranceSequence.Duration() <= 0f)
        {
            chapterEntranceSequence.Kill();
            chapterEntranceSequence = null;

            objectsFinished = true;
            return;
        }

        chapterEntranceSequence.OnComplete(() =>
        {
            chapterEntranceSequence = null;
            objectsFinished = true;
        });
    }

    private IEnumerator WaitForChapterReady(int idx, bool fromFront)
    {
        // 두 연출 중 늦게 끝나는 쪽까지 기다린다.
        while (!titleFinished || !objectsFinished)
            yield return null;

        // ESC 메뉴가 열려 있으면 대사 시작은 복귀 후에 진행.
        while (PauseMenuUI.IsOpen)
            yield return null;

        StartChapterDialogue(idx);

        if (!fromFront)
            UIManager.Instance.GetInUIs();

        // 현재 UIManager의 대사창과 버튼 등장 시간이 0.5초.
        // 이 부분은 완료 콜백이 없어서 기존 시간만큼 기다린다.
        yield return new WaitForSeconds(0.5f);

        // 첫 대사에서 실행된 캐릭터 교체도 기다린다.
        while (AnimationManager.Instance.IsTransitioning)
            yield return null;

        isPreparingChapter = false;
        chapterReadyCoroutine = null;
    }

    public void BackToMainMenu()
    {
        if (!isChapterActive || returnCoroutine != null)
            return;

        returnCoroutine = StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        // 챕터 준비, 캐릭터 교체, 일시정지가 끝날 때까지 대기.
        while (isPreparingChapter ||
               AnimationManager.Instance.IsTransitioning ||
               PauseMenuUI.IsOpen)
        {
            yield return null;
        }

        isReturningToMenu = true;

        pauseMenuUI.SetChapterPlaying(false);

        CutSceneCastManager.Instance.FadeOutCharacters(1f);
        ConstellationManager.Instance.FadeOutStarts();
        UIManager.Instance.GetOutUIs();
        DrawingManager.Instance.FadeOutAndReset(1f);

        yield return new WaitForSeconds(1f);

        UIManager.Instance.SlideInChapterButtons();

        AudioManager.Instance.FadeOutBGM(2f, () =>
        {
            AudioManager.Instance.PlayBGM(baseBGM, 2f);
        });

        // 현재 챕터 별 복귀 애니메이션 시간.
        yield return new WaitForSeconds(0.7f);

        // 복귀 도중 ESC 메뉴를 열었다면 닫은 뒤 독백 시작.
        while (PauseMenuUI.IsOpen)
            yield return null;

        isChapterActive = false;

        // 독백을 먼저 열어 입력 차단 패널을 활성화한다.
        TryPlayChapterMonologue();

        isReturningToMenu = false;
        returnCoroutine = null;
    }
    
    private void TryPlayChapterMonologue()
    {
        if (monologueUI == null ||
            MonologueUI.IsPlaying ||
            PauseMenuUI.IsOpen ||
            isWatched == null ||
            chapterMonologues == null)
        {
            return;
        }

        foreach (MonologueDefinition definition in chapterMonologues)
        {
            if (definition == null)
                continue;

            string dialogueId = definition.dialogueId;

            if (string.IsNullOrWhiteSpace(dialogueId) ||
                playedMonologueIds.Contains(dialogueId))
            {
                continue;
            }

            int index = definition.requiredChapterIndex;

            if (index < 0 || index >= isWatched.Length)
                continue;

            if (!isWatched[index])
                continue;

            monologueUI.Play(dialogueId, () =>
            {
                playedMonologueIds.Add(dialogueId);
            });

            // 한 번 복귀할 때 독백 하나만 출력.
            return;
        }
    }

    private void StartChapterDialogue(int idx)
    {
        DialogueRepository.Instance.TryGetChapter(
            chapterIds[idx],
            out IReadOnlyList<DialogueData> dialogueDatas
        );

        UIManager.Instance.StartDialogue(dialogueDatas);
    }

    public bool CanShowChapterButton(int idx)
    {
        if (isWatched == null || idx < 0 || idx >= isWatched.Length)
            return false;

        // 첫 번째 챕터는 처음부터 선택 가능.
        if (idx == 0)
            return true;

        // 이미 본 장면은 다시 보기 가능.
        if (isWatched[idx])
            return true;

        // 다음 챕터 앞장면은 이전 챕터 뒷장면을 보면 해금.
        return idx % 2 == 0 && isWatched[idx - 1];
    }

    public void MarkCurrentChapterWatched()
    {
        if (currentChapterIdx < 0 || currentChapterIdx >= isWatched.Length)
            return;

        isWatched[currentChapterIdx] = true;
    }
}