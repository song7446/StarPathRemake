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
        
        foreach (var backGroundInfoInfo in cutSceneCast.backGroundInfos)
        {
            Vector3 position = backGroundInfoInfo.position;
            position.y = backGroundInfoInfo.position.y + 20;
            
            GameObject backGroundGO = Instantiate(backGroundInfoInfo.BackGroundPrefab, position,
                Quaternion.Euler(backGroundInfoInfo.rotation));
            
            AllObjectsDict.TryAdd(backGroundInfoInfo.key, backGroundGO);
            
            sequence.Insert(
                time * i - 0.2f,
                backGroundGO.transform
                    .DOMoveY(backGroundInfoInfo.position.y, 0.8f)
                    .SetEase(Ease.OutBack, 1f)
                    .OnComplete(() => EnableInteriorShake(backGroundGO))
                    .SetLink(backGroundGO)
            );


            i++;
        }
        
        foreach (var objectInfo in cutSceneCast.objectInfos)
        {
            Vector3 position = objectInfo.position;
            position.y = objectInfo.position.y + 20;
            
            GameObject objectObject = Instantiate(objectInfo.objectPrefab, position,
                Quaternion.Euler(objectInfo.rotation));
            
            AllObjectsDict.TryAdd(objectInfo.key, objectObject);
            
            sequence.Insert(
                time * i - 0.2f,
                objectObject.transform
                    .DOMoveY(objectInfo.position.y, 0.8f)
                    .SetEase(Ease.OutBack, 1f)
                    .OnComplete(() => EnableInteriorShake(objectObject))
                    .SetLink(objectObject)
            );

            i++;
        }
        
        foreach (var characterInfo in cutSceneCast.characterInfos)
        {
            Vector3 position = characterInfo.position;
            position.y = characterInfo.position.y + 20;
            
            GameObject character = Instantiate(characterInfo.characterPrefab, position,
                Quaternion.Euler(characterInfo.rotation));
            
            AllObjectsDict.TryAdd(characterInfo.key, character);
            
            sequence.Insert(
                time * i - 0.2f,
                character.transform
                    .DOMoveY(characterInfo.position.y, 0.8f)
                    .SetEase(Ease.OutBack, 1f)
                    .OnComplete(() => EnableInteriorShake(character))
                    .SetLink(character)
            );

            i++;
        }
    }
    
    public void FadeOutCharacters(
        float duration = 1f)
    {
        // 지금 퇴장할 오브젝트만 따로 보관
        var targets = new System.Collections.Generic.List<GameObject>(
            AllObjectsDict.Values);

        AllObjectsDict.Clear();

        Sequence fadeSequence = DOTween.Sequence();
        fadeSequence.AppendInterval(duration);

        foreach (GameObject target in targets)
        {
            if (target == null)
                continue;

            SpriteRenderer[] renderers =
                target.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                fadeSequence.Join(
                    spriteRenderer.DOFade(0f, duration)
                        .SetEase(Ease.InOutSine)
                );
            }
        }

        fadeSequence.OnComplete(() =>
        {
            foreach (GameObject target in targets)
            {
                if (target != null)
                    Destroy(target);
            }
        });
    }

    public void CreateCharacterWithNoAnim(CutSceneCast cutSceneCast)
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

        foreach (var backGroundInfoInfo in cutSceneCast.backGroundInfos)
        {
            GameObject backGroundGO = Instantiate(backGroundInfoInfo.BackGroundPrefab, backGroundInfoInfo.position,
                Quaternion.Euler(backGroundInfoInfo.rotation));
            
            AllObjectsDict.TryAdd(backGroundInfoInfo.key, backGroundGO);
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
    
    private void EnableInteriorShake(GameObject target)
    {
        if (target == null)
            return;

        foreach (var shake in
                 target.GetComponentsInChildren<CarInteriorShake>(true))
        {
            shake.enabled = true;
        }
    }
}