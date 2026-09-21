using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;

public class AnimationManager : MonoBehaviourSingleton<AnimationManager>
{
    private Dictionary<string, AnimationCommand> animationDict = new Dictionary<string, AnimationCommand>();

    protected override void Awake()
    {
        base.Awake();

        CommandsToDictionary();
    }

    public void CommandsToDictionary()
    {
        AnimationCommand[] animationCommands = GameManager.Instance.chapterDefinition.animations;

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
                GameObject gameObject = GameObject.FindWithTag(command.characterId);
                gameObject.transform.GetComponent<SpriteRenderer>().DOFade(0, 2).OnComplete(() => Destroy(gameObject));

                GameObject newGameObject = Instantiate(command.characterPrefab, command.position, Quaternion.Euler(command.rotation));
                SpriteRenderer sr = newGameObject.transform.GetComponent<SpriteRenderer>();
                Color color = sr.color;
                color.a = 0;
                
                sr.color = color;
                
                sr.DOFade(1, 2);
            }
        }
    }
}