using UnityEngine;

public class BackGroundRotation : MonoBehaviour
{
    [SerializeField]
    private float degreesPerSecond = 0.5f;

    private void Update()
    {
        transform.Rotate(
            0f,
            0f,
            degreesPerSecond * Time.deltaTime,
            Space.Self
        );
    }
}
