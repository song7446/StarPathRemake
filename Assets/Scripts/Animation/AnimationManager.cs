using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;

public class AnimationManager : SceneMonoBehaviourSingleton<AnimationManager>
{
    [Header("접히는 UI 그림을 생성할 부모")]
    [SerializeField] private RectTransform animationUIParent;

    private readonly Dictionary<string, AnimationCommand> animationDict
        = new Dictionary<string, AnimationCommand>();

    private int activeTransitionCount;

    public bool IsTransitioning => activeTransitionCount > 0;

    public void CommandsToDictionary()
    {
        animationDict.Clear();

        AnimationCommand[] animationCommands =
            GameManager.Instance.CurrentChapterDefinition.animations;

        foreach (var animationCommand in animationCommands)
        {
            animationDict.TryAdd(
                animationCommand.dialogueId,
                animationCommand);
        }
    }

    public void PlayAnimation(string id)
    {
        if (!animationDict.TryGetValue(id, out var animationCommand))
            return;
        
        foreach (SoundCommand sound in animationCommand.sounds)
        {
            if (sound == null)
                continue;

            switch (sound.action)
            {
                case SFXAction.PlayOneShot:
                    AudioManager.Instance.PlaySFX(
                        sound.clip,
                        sound.volume
                    );
                    break;

                case SFXAction.StartLoop:
                    AudioManager.Instance.PlayLoopSFX(
                        sound.clip,
                        sound.volume
                    );
                    break;

                case SFXAction.StopLoop:
                    AudioManager.Instance.StopLoopSFX();
                    break;
            }
        }
        
        foreach (var command in animationCommand.commands)
        {
            
            bool isFoldedUI =
                command.characterPrefab != null &&
                command.characterPrefab.GetComponent<FoldedPictureUI>() != null;

            if (isFoldedUI && animationUIParent == null)
            {
                Debug.LogError(
                    "Animation UI Parent를 연결해주세요.",
                    this);

                continue;
            }

            GameObject oldObject =
                CutSceneCastManager.Instance.GetObjectByKey(
                    command.characterId);

            CutSceneCastManager.Instance.RemoveObjectFromDictByKey(
                command.characterId);

            GameObject newObject = null;

            // 프리팹이 null이면 생성 없이 기존 오브젝트만 퇴장.
            if (command.characterPrefab != null)
            {
                if (isFoldedUI)
                {
                    newObject = Instantiate(
                        command.characterPrefab,
                        animationUIParent,
                        false);

                    RectTransform rect =
                        newObject.GetComponent<RectTransform>();

                    rect.anchoredPosition3D = command.position;
                    rect.localRotation =
                        Quaternion.Euler(command.rotation);
                }
                else
                {
                    newObject = Instantiate(
                        command.characterPrefab,
                        command.position,
                        Quaternion.Euler(command.rotation));
                }

                CutSceneCastManager.Instance.PushObject(
                    command.characterId,
                    newObject);
            }

            if (oldObject == null && newObject == null)
                continue;

            activeTransitionCount++;

            Sequence transition = DOTween.Sequence();
            transition.AppendInterval(0.01f);

            float transitionDuration = 0.01f;

            // 기존 오브젝트 퇴장.
            if (oldObject != null)
            {
                FoldedPictureUI oldPicture =
                    oldObject.GetComponent<FoldedPictureUI>();

                float exitDuration;

                if (oldPicture != null)
                {
                    exitDuration = Mathf.Max(0f, oldPicture.FoldDuration);

                    // 접기 애니메이션은 FoldedPictureUI에서 실행.
                    oldPicture.Hide();
                }
                else
                {
                    exitDuration = 1f;

                    foreach (SpriteRenderer sr in
                             oldObject.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        transition.Insert(
                            0f,
                            sr.DOFade(0f, exitDuration));
                    }
                }

                transitionDuration =
                    Mathf.Max(transitionDuration, exitDuration);

                transition.InsertCallback(exitDuration, () =>
                {
                    if (oldObject != null)
                        Destroy(oldObject);
                });
            }

            // 새 오브젝트 등장.
            if (newObject != null)
            {
                FoldedPictureUI newPicture =
                    newObject.GetComponent<FoldedPictureUI>();

                if (newPicture != null)
                {
                    newPicture.Show();

                    transitionDuration = Mathf.Max(
                        transitionDuration,
                        newPicture.UnfoldDuration);
                }
                else
                {
                    const float enterDuration = 2f;

                    foreach (SpriteRenderer sr in
                             newObject.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        float originalAlpha = sr.color.a;

                        Color color = sr.color;
                        color.a = 0f;
                        sr.color = color;

                        transition.Insert(
                            0f,
                            sr.DOFade(originalAlpha, enterDuration));
                    }

                    transitionDuration = Mathf.Max(
                        transitionDuration,
                        enterDuration);
                }
            }

            // 별도 스크립트에서 실행하는 펼치기/접기도 기다림.
            float remainingDuration =
                transitionDuration - transition.Duration();

            if (remainingDuration > 0f)
                transition.AppendInterval(remainingDuration);

            // FoldedPictureUI의 SetUpdate(true)와 시간 기준을 맞춤.
            bool usesFoldedUI =
                isFoldedUI ||
                (oldObject != null &&
                 oldObject.GetComponent<FoldedPictureUI>() != null);

            transition.SetUpdate(usesFoldedUI);

            bool released = false;

            transition.OnKill(() =>
            {
                if (released)
                    return;

                released = true;
                activeTransitionCount--;

                // 중간 취소 시에도 퇴장하던 대상은 정리.
                if (oldObject != null)
                    Destroy(oldObject);
            });

            transition.SetAutoKill(true);
            transition.SetLink(
                newObject != null ? newObject : gameObject);
        }
    }
}