using UnityEngine;

public class CarInteriorShake : MonoBehaviour
{
    [Header("흔들림 크기")]
    [SerializeField] private float horizontalAmount = 0.008f;
    [SerializeField] private float verticalAmount = 0.02f;

    [Header("흔들림 속도")]
    [SerializeField] private float shakeSpeed = 7f;

    [Header("시작 시 서서히 적용")]
    [SerializeField] private float fadeInDuration = 1f;

    private Vector3 originalPosition;
    private float elapsed;
    [SerializeField] private float seed = 10f;

    private void OnEnable()
    {
        originalPosition = transform.localPosition;
        elapsed = 0f;
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;

        float strength = fadeInDuration <= 0f
            ? 1f
            : Mathf.Clamp01(elapsed / fadeInDuration);

        float time = Time.time * shakeSpeed;

        float x = Mathf.PerlinNoise(seed, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(seed + 100f, time) * 2f - 1f;

        Vector3 offset = new Vector3(
            x * horizontalAmount,
            y * verticalAmount,
            0f
        );

        transform.localPosition = originalPosition + offset * strength;
    }

    private void OnDisable()
    {
        transform.localPosition = originalPosition;
    }
    
    public void StartShake()
    {
        enabled = true;
    }
    
    public void StopShake()
    {
        enabled = false;
    }
}