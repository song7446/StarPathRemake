using System.Collections.Generic;
using SongLib.Core.Singleton;
using UnityEngine;

public class CutSceneCastManager : MonoBehaviourSingleton<CutSceneCastManager>
{
    List<GameObject> AllObjects = new List<GameObject>();

    public void CreateCharacter(CutSceneCast cutSceneCast)
    {
        foreach (var allObject in AllObjects)
        {
            Destroy(allObject);
        }
        
        AllObjects.Clear();

        foreach (var characterInfo in cutSceneCast.characterInfos)
        {
            GameObject character = Instantiate(characterInfo.characterPrefab, characterInfo.position,
                Quaternion.Euler(characterInfo.rotation));

            AllObjects.Add(character);
        }

        foreach (var objectInfo in cutSceneCast.objectInfos)
        {
            GameObject objectObject = Instantiate(objectInfo.objectPrefab, objectInfo.position,
                Quaternion.Euler(objectInfo.rotation));

            AllObjects.Add(objectObject);
        }
    }
}