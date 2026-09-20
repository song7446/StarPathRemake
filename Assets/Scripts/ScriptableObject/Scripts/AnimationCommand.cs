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

    public DialogueAnimationCommand[] commands;
}
