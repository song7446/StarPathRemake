using System;
using UnityEngine;

[CreateAssetMenu(menuName = "StarPath/Animation")]
public class AnimationCommand : ScriptableObject
{
    public string dialogueId;
    
    [System.Serializable]
    public class DialogueAnimationCommand
    {
        public string characterId;
        public string characterName;
        public Vector3 position;
        public Vector3 rotation;
        public GameObject characterPrefab;
    }
    
    public SoundCommand[] sounds;
    
    public DialogueAnimationCommand[] commands;
}

[Serializable]
public class SoundCommand
{
    public SFXAction action;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}
