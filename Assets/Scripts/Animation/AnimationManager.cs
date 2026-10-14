using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;

public class AnimationManager : MonoBehaviourSingleton<AnimationManager>
{
    private Dictionary<string, AnimationCommand> animationDict = new Dictionary<string, AnimationCommand>();

    private int activeTransitionCount;

    public bool IsTransitioning => activeTransitionCount > 0;

    public void CommandsToDictionary()
    {
        animationDict.Clear();

        AnimationCommand[] animationCommands = GameManager.Instance.CurrentChapterDefinition.animations;

        foreach (var animationCommand in animationCommands)
        {
            animationDict.TryAdd(animationCommand.dialogueId, animationCommand);
        }
    }

    public void PlayAnimation(string id)
    {
        if (animationDict.TryGetValue(id, out var animationCommand))
        {
            foreach (var command in animationCommand.commands)
            {
                GameObject oldObject =
                    CutSceneCastManager.Instance.GetObjectByKey(command.characterId);

                CutSceneCastManager.Instance.RemoveObjectFromDictByKey(
                    command.characterId);

                GameObject newGameObject = Instantiate(
                    command.characterPrefab,
                    command.position,
                    Quaternion.Euler(command.rotation));

                CutSceneCastManager.Instance.PushObject(
                    command.characterId, newGameObject);

                SpriteRenderer newRenderer =
                    newGameObject.GetComponent<SpriteRenderer>();

                Color originalColor = newRenderer.color;
                Color transparentColor = originalColor;
                transparentColor.a = 0f;
                newRenderer.color = transparentColor;

                activeTransitionCount++;

                Sequence transition = DOTween.Sequence();

                if (oldObject != null)
                {
                    SpriteRenderer oldRenderer =
                        oldObject.GetComponent<SpriteRenderer>();

                    if (oldRenderer != null)
                    {
                        transition.Insert(
                            0f,
                            oldRenderer.DOFade(0f, 1f)
                                .OnComplete(() =>
                                {
                                    if (oldObject != null)
                                        Destroy(oldObject);
                                })
                        );
                    }
                    else
                    {
                        Destroy(oldObject);
                    }
                }

                transition.Insert(
                    0f,
                    newRenderer.DOFade(originalColor.a, 2f)
                );

// 완료되거나 중간에 취소돼도 카운트 해제
                bool released = false;

                transition.OnKill(() =>
                {
                    if (released)
                        return;

                    released = true;
                    activeTransitionCount--;
                });

                transition.SetLink(newGameObject);
            }
        }
    }
}