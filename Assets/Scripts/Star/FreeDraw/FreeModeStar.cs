using UnityEngine;

public class FreeModeStar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer selectionMarker;

    public int StarId { get; private set; }

    private void Awake()
    {
        SetSelected(false);
    }

    public void Initialize(int id)
    {
        StarId = id;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionMarker != null)
        {
            selectionMarker.color = selected ? Color.red : Color.white;
        }
    }
}