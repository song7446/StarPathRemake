using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;

public class AnimationManager : MonoBehaviourSingleton<AnimationManager>
{
    private Dictionary<string, AnimationCommand> animationDict = new Dictionary<string, AnimationCommand>();

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
                GameObject oldObject = CutSceneCastManager.Instance.GetObjectByKey(command.characterId);
                CutSceneCastManager.Instance.RemoveObjectFromDictByKey(command.characterId);

                if (oldObject != null)
                {
                    SpriteRenderer oldRenderer = oldObject.GetComponent<SpriteRenderer>();

                    oldRenderer.DOFade(0, 1)
                        .SetLink(oldObject)
                        .OnComplete(() => { Destroy(oldObject); });
                }


                GameObject newGameObject = Instantiate(command.characterPrefab, command.position,
                    Quaternion.Euler(command.rotation));
                CutSceneCastManager.Instance.PushObject(command.characterId, newGameObject);
                SpriteRenderer sr = newGameObject.transform.GetComponent<SpriteRenderer>();
                Color color = sr.color;
                color.a = 0;

                sr.color = color;
                sr.DOFade(1, 2).SetLink(newGameObject);
            }
        }
    }

    public void ChapterChangeAnimation(CutSceneCast cutSceneCast , Sequence sequence)
    {
        GameObject groundObject = Instantiate(cutSceneCast.BackGroundInfos[0].BackGroundPrefab, new Vector3(0, -10, 0),
            Quaternion.Euler(cutSceneCast.BackGroundInfos[0].rotation));
        sequence.Insert(0f,
            groundObject.transform.DOMoveY(cutSceneCast.BackGroundInfos[0].position.y, 0.8f).SetEase(Ease.OutBack, 1f)
                .SetLink(groundObject));
        
        GameObject mountainObject = Instantiate(cutSceneCast.BackGroundInfos[1].BackGroundPrefab, new Vector3(0, -10, 0),
            Quaternion.Euler(cutSceneCast.BackGroundInfos[1].rotation));
        sequence.Insert(0.5f,
            mountainObject.transform.DOMoveY(cutSceneCast.BackGroundInfos[1].position.y, 0.8f).SetEase(Ease.OutBack, 1f)
                .SetLink(mountainObject));
    }
}