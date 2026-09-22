using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;

public class CutSceneCastManager : MonoBehaviourSingleton<CutSceneCastManager>
{
    List<GameObject> AllObjects = new List<GameObject>();
    private Dictionary<string, GameObject> AllObjectsDict = new Dictionary<string, GameObject>();

    public void CreateCharacter(CutSceneCast cutSceneCast)
    {
        foreach (var allObject in AllObjects)
        {
            Destroy(allObject);
        }
        
        AllObjects.Clear();
        AllObjectsDict.Clear();

        foreach (var characterInfo in cutSceneCast.characterInfos)
        {
            GameObject character = Instantiate(characterInfo.characterPrefab, characterInfo.position,
                Quaternion.Euler(characterInfo.rotation));

            AllObjects.Add(character);
            AllObjectsDict.Add(characterInfo.key, character);
        }

        foreach (var objectInfo in cutSceneCast.objectInfos)
        {
            GameObject objectObject = Instantiate(objectInfo.objectPrefab, objectInfo.position,
                Quaternion.Euler(objectInfo.rotation));

            AllObjects.Add(objectObject);
            AllObjectsDict.Add(objectInfo.key, objectObject);
        }
    }
    
    public GameObject GetObjectByKey(string key)
    {
        AllObjectsDict.TryGetValue(key, out GameObject res);
        return res;
    }
}