using UnityEngine;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "CutSceneCast", menuName = "StarPath/CutSceneCast")]
public class CutSceneCast : ScriptableObject
{
    [System.Serializable]
    public class CharacterInfo
    {
        public string key;
        public string characterName;
        public Vector3 position;
        public Vector3 rotation;
        public GameObject characterPrefab;
    }
    
    [System.Serializable]
    public class ObjectInfo
    {
        public string key;
        public string objectName;
        public Vector3 position;
        public Vector3 rotation;
        public GameObject objectPrefab;
    }
    
    public CharacterInfo[] characterInfos;
    public ObjectInfo[] objectInfos;
}