using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;

public class CutSceneCastManager : MonoBehaviourSingleton<CutSceneCastManager>
{
    private Dictionary<string, GameObject> AllObjectsDict = new Dictionary<string, GameObject>();

    public void CreateCharacterWithAnim(CutSceneCast cutSceneCast, Sequence sequence)
    {
        foreach (var objects in AllObjectsDict)
        {
            Destroy(objects.Value);
        }
        
        AllObjectsDict.Clear();

        float time = 0.5f;
        int i = 0;
        
        foreach (var objectInfo in cutSceneCast.objectInfos)
        {
            Vector3 position = objectInfo.position;
            position.y = objectInfo.position.y + 10;
            
            GameObject objectObject = Instantiate(objectInfo.objectPrefab, position,
                Quaternion.Euler(objectInfo.rotation));
            
            AllObjectsDict.TryAdd(objectInfo.key, objectObject);
            
            sequence.Insert(time * i,
                objectObject.transform.DOMoveY(objectInfo.position.y, 0.8f).SetEase(Ease.OutBack, 1f)
                    .SetLink(objectObject));

            i++;
        }
        
        foreach (var characterInfo in cutSceneCast.characterInfos)
        {
            Vector3 position = characterInfo.position;
            position.y = characterInfo.position.y + 10;
            
            GameObject character = Instantiate(characterInfo.characterPrefab, position,
                Quaternion.Euler(characterInfo.rotation));
            
            AllObjectsDict.TryAdd(characterInfo.key, character);
            
            sequence.Insert(time * i - 0.2f,
                character.transform.DOMoveY(characterInfo.position.y, 0.8f).SetEase(Ease.OutBack, 1f)
                    .SetLink(character));

            i++;
        }
    }

    public void CreateCharacter(CutSceneCast cutSceneCast)
    {
        foreach (var objects in AllObjectsDict)
        {
            Destroy(objects.Value);
        }
        
        AllObjectsDict.Clear();

        foreach (var characterInfo in cutSceneCast.characterInfos)
        {
            GameObject character = Instantiate(characterInfo.characterPrefab, characterInfo.position,
                Quaternion.Euler(characterInfo.rotation));
            
            AllObjectsDict.TryAdd(characterInfo.key, character);
        }

        foreach (var objectInfo in cutSceneCast.objectInfos)
        {
            GameObject objectObject = Instantiate(objectInfo.objectPrefab, objectInfo.position,
                Quaternion.Euler(objectInfo.rotation));
            
            AllObjectsDict.TryAdd(objectInfo.key, objectObject);
        }
    }
    
    public GameObject GetObjectByKey(string key)
    {
        AllObjectsDict.TryGetValue(key, out GameObject res);
        return res;
    }

    public void PushObject(string key, GameObject obj)
    {
        AllObjectsDict.TryAdd(key, obj);
    }

    public void RemoveObjectFromDictByKey(string key)
    {
        AllObjectsDict.Remove(key);
    }
}