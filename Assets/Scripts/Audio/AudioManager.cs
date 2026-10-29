using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;

public enum SFXAction
{
    None,
    PlayOneShot,
    StartLoop,
    StopLoop
}

public class AudioManager : MonoBehaviourSingleton<AudioManager>
{
    [Header("재생용 AudioSource")] [SerializeField]
    private AudioSource bgmSource;

    [SerializeField] private AudioSource sfxSource;

    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

    private Tween bgmFadeTween;
    
    [Header("반복 효과음")]
    [SerializeField] private AudioSource loopSfxSource;

    public void PlayBGM(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null)
            return;

        // 같은 음악이 재생 중이면 유지.
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmFadeTween?.Kill();

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.pitch = 1f;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();

        bgmFadeTween = bgmSource
            .DOFade(bgmVolume, Mathf.Max(0f, fadeDuration))
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
    
    public void FadeOutBGM(
        float duration = 1f,
        System.Action onFinished = null)
    {
        bgmFadeTween?.Kill();
        bgmFadeTween = null;

        if (!bgmSource.isPlaying)
        {
            bgmSource.clip = null;
            onFinished?.Invoke();
            return;
        }

        bgmFadeTween = bgmSource
            .DOFade(0f, Mathf.Max(0f, duration))
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                bgmSource.Stop();
                bgmSource.clip = null;
                bgmFadeTween = null;

                onFinished?.Invoke();
            });
    }
    
    public void PlayLoopSFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        loopSfxSource.volume = Mathf.Clamp01(volumeScale);

        // 같은 소리가 재생 중이면 유지.
        if (loopSfxSource.clip == clip && loopSfxSource.isPlaying)
            return;

        loopSfxSource.Stop();
        loopSfxSource.clip = clip;
        loopSfxSource.loop = true;
        loopSfxSource.Play();
    }

    public void StopLoopSFX()
    {
        loopSfxSource.Stop();
        loopSfxSource.clip = null;
    }

    public void StopBGM()
    {
        bgmFadeTween?.Kill();
        bgmFadeTween = null;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volumeScale)
        );
    }
}