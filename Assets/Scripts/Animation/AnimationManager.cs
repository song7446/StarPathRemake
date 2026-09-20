using System.Collections.Generic;
using System.Linq;
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
        if (animationDict.ContainsKey(id))
        {
            AnimationCommand animationCommand = animationDict[id];

            foreach (var command in animationCommand.commands)
            {
                GameObject gameObject = GameObject.FindWithTag(command.characterId);
                Destroy(gameObject);
                
                Instantiate(command.characterPrefab, command.position, Quaternion.Euler(command.rotation));
            }
        }
    }
}
