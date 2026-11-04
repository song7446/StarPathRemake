using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzleStar : MonoBehaviour
{
    public int starID;
    public bool isCorrect;

    public void SetupStar(int id, bool correct)
    {
        starID = id;
        isCorrect = correct;

        if (!isCorrect)
        {
            // 방해 별 설정
        }
    }

    private void OnMouseDown()
    {
        if (PauseMenuUI.IsOpen || MonologueUI.IsPlaying)
            return;

        if (!UIManager.Instance.GetIsOnSkyView())
            return;

        if (UIManager.Instance.IsPlayingDialogue())
            return;

        // 노트나 버튼 등 UI를 클릭할 때 뒤의 별이 눌리는 것 방지.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        DrawingManager drawingManager = DrawingManager.Instance;

        if (!drawingManager.IsDrawing)
        {
            drawingManager.StartDrawing(this);
        }
        else
        {
            drawingManager.HoveredStar(this);
            drawingManager.EndDrawing();
        }
    }

    private void OnMouseEnter()
    {
        if (DrawingManager.Instance.IsDrawing)
            DrawingManager.Instance.HoveredStar(this);
        
    }

    private void OnMouseExit()
    {
        if (DrawingManager.Instance.IsDrawing)
            DrawingManager.Instance.EndHoveredStar(this);
    }
}