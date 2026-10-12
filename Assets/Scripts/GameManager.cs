using System;
using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    public ChapterDefinition[] ChapterDefinitions;
    public Queue<ChapterDefinition> ChapterDefinitionsQueue = new Queue<ChapterDefinition>();

    private ChapterDefinition _currentChapterDefinition;
    public ChapterDefinition CurrentChapterDefinition => _currentChapterDefinition;

    private Queue<String> _chapterIDs = new Queue<string>();
    public Queue<string> ChapterIDs => _chapterIDs;

    private string[] chapters =
    {
        "1_Front", "1_Back", "2_Front", "2_Back", "3_Front", "3_Back", "4_Front", "4_Back", "5_Front", "5_Back",
        "6_Front", "6_Back"
    };

    private bool _isFront = false;
    public bool IsFront => _isFront;
    
    private bool _isShowingChapterTitle;
    
    private void Start()
    {
        foreach (var chapter in chapters)
        {
            _chapterIDs.Enqueue(chapter);
        }

        foreach (var chapterDefinintion in ChapterDefinitions)
        {
            ChapterDefinitionsQueue.Enqueue(chapterDefinintion);
        }

        // StartNextChapter();
    }

    public void StartNextChapter()
    {
        if (_isShowingChapterTitle)
            return;

        if (_chapterIDs.Count <= 0 ||
            ChapterDefinitionsQueue.Count <= 0)
        {
            return;
        }

        _isFront = !_isFront;

        _currentChapterDefinition =
            ChapterDefinitionsQueue.Dequeue();

        AnimationManager.Instance.CommandsToDictionary();

        UIManager.Instance.SetTitle(
            _currentChapterDefinition.chapterNumber,
            $"{_currentChapterDefinition.chapterTitle} - " +
            $"{_currentChapterDefinition.chapterSubtitle}"
        );

        if (_isFront)
        {
            _isShowingChapterTitle = true;

            UIManager.Instance.FadeInTitle(
                onFinished: () =>
                {
                    try
                    {
                        StartChapterDialogue();
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
            StartChapterDialogue();
        }
    }

    private void CreateChapterObjects()
    {
        if (_isFront)
        {
            Sequence sequence = DOTween.Sequence();

            AnimationManager.Instance.ChapterChangeAnimation(
                _currentChapterDefinition.cutSceneCasts,
                sequence
            );

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
            CutSceneCastManager.Instance.CreateCharacter(
                _currentChapterDefinition.cutSceneCasts
            );
        }
    }

    private void StartChapterDialogue()
    {
        DialogueRepository.Instance.TryGetChapter(
            _chapterIDs.Dequeue(),
            out IReadOnlyList<DialogueData> dialogueDatas
        );

        UIManager.Instance.StartDialogue(dialogueDatas);
    }
}