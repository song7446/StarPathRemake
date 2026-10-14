using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;

public class AnimationManager : MonoBehaviourSingleton<AnimationManager>
{
    private Dictionary<string, AnimationCommand> animationDict = new Dictionary<string, AnimationCommand>();

    private int activeTransitionCount;

    public bool IsTransitioning => activeTransitionCount > 0;

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
    if (!animationDict.TryGetValue(id, out var animationCommand))
        return;

    foreach (var command in animationCommand.commands)
    {
        GameObject oldObject =
            CutSceneCastManager.Instance.GetObjectByKey(command.characterId);

        CutSceneCastManager.Instance.RemoveObjectFromDictByKey(
            command.characterId);

        GameObject newObject = null;

        // 프리팹이 있을 때만 새 캐릭터 생성
        if (command.characterPrefab != null)
        {
            newObject = Instantiate(
                command.characterPrefab,
                command.position,
                Quaternion.Euler(command.rotation));

            CutSceneCastManager.Instance.PushObject(
                command.characterId, newObject);
        }

        if (oldObject == null && newObject == null)
            continue;

        activeTransitionCount++;

        Sequence transition = DOTween.Sequence();

        // 렌더러가 없어도 완료 처리 가능
        transition.AppendInterval(0.01f);

        // 기존 오브젝트 퇴장
        if (oldObject != null)
        {
            foreach (SpriteRenderer sr in
                     oldObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                transition.Insert(0f, sr.DOFade(0f, 1f));
            }

            transition.InsertCallback(1f, () =>
            {
                if (oldObject != null)
                    Destroy(oldObject);
            });
        }

        // 새 오브젝트 등장
        if (newObject != null)
        {
            foreach (SpriteRenderer sr in
                     newObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                float originalAlpha = sr.color.a;

                Color color = sr.color;
                color.a = 0f;
                sr.color = color;

                transition.Insert(
                    0f,
                    sr.DOFade(originalAlpha, 2f)
                );
            }
        }

        bool released = false;

        transition.OnKill(() =>
        {
            if (released)
                return;

            released = true;
            activeTransitionCount--;

            // 중간에 취소돼도 퇴장하던 오브젝트가 남지 않도록 정리
            if (oldObject != null)
                Destroy(oldObject);
        });

        // 정상 완료 시에도 OnKill에서 카운트 해제
        transition.SetAutoKill(true);

        // 삭제 전용이면 새 오브젝트가 없으므로 매니저에 연결
        transition.SetLink(
            newObject != null ? newObject : gameObject);
    }
}
}