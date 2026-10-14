using System;
using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviourSingleton<GameManager>
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

    private void Start()
    {
        isWatched = new bool[ChapterDefinitions.Length];
    }
    
    public void StartChapter(int idx, bool fromFront = false)
    {
        if (_isShowingChapterTitle)
            return;

        if (idx >= ChapterDefinitions.Length)
        {
            return;
        }
        

        _currentChapterDefinition = ChapterDefinitions[idx];
        currentChapterIdx = idx;

        AnimationManager.Instance.CommandsToDictionary();

        UIManager.Instance.SetTitle(
            _currentChapterDefinition.chapterNumber,
            $"{_currentChapterDefinition.chapterTitle} - " +
            $"{_currentChapterDefinition.chapterSubtitle}"
        );
        
        _fromFront = fromFront;

        if (!fromFront)
        {
            _isShowingChapterTitle = true;

            UIManager.Instance.FadeInTitle(
                onFinished: () =>
                {
                    try
                    {
                        StartChapterDialogue(currentChapterIdx);
                        UIManager.Instance.GetInUIs();
                    }
                    finally
                    {
                        _isShowingChapterTitle = false;
                    }
                },
                onFadeOutStarted: CreateChapterObjects
            );
        }
        else
        {
            CreateChapterObjects();
            StartChapterDialogue(currentChapterIdx);
        }
    }

    private void CreateChapterObjects()
    {
        if (!_fromFront)
        {
            Sequence sequence = DOTween.Sequence();

            CutSceneCastManager.Instance.CreateCharacterWithAnim(
                _currentChapterDefinition.cutSceneCasts,
                sequence
            );

            ConstellationManager.Instance.CreateConstellation(
                _currentChapterDefinition.constellationData
            );
        }
        else
        {
            CutSceneCastManager.Instance.CreateCharacterWithNoAnim(
                _currentChapterDefinition.cutSceneCasts
            );
        }
    }

    public void BackToMainMenu()
    {
        CutSceneCastManager.Instance.FadeOutCharacters(1f);
        ConstellationManager.Instance.FadeOutStarts();
        UIManager.Instance.GetOutUIs();
        DrawingManager.Instance.FadeOutAndReset(1);

        DOVirtual.DelayedCall(1f, () => { UIManager.Instance.SlideInChapterButtons(); }, ignoreTimeScale: false);
    }

    private void StartChapterDialogue(int idx)
    {
        DialogueRepository.Instance.TryGetChapter(
            chapterIds[idx],
            out IReadOnlyList<DialogueData> dialogueDatas
        );

        UIManager.Instance.StartDialogue(dialogueDatas);
    }
}