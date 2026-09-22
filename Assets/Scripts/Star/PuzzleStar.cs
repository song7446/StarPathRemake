using System;
using UnityEngine;

public class PuzzleStar : MonoBehaviour
{
    public int starID; // 노트의 정답과 비교할 고유 번호
    public bool isCorrect;

    public void SetupStar(int id, bool correct)
    {
        starID = id;
        isCorrect = correct;

        if (!isCorrect)
        {
            /* 방해 별 설정 */
        }
    }

    private void OnMouseDown()
    {
        if (!UIManager.Instance.GetIsOnSkyView())
            return;
        
        if (!DrawingManager.Instance.IsDrawing)
        {
            DrawingManager.Instance.StartDrawing(this);
        }
        else
        {
            DrawingManager.Instance.EndDrawing();
        }
    }

    private void OnMouseEnter()
    {
        if (DrawingManager.Instance.IsDrawing)
        {
            DrawingManager.Instance.HoveredStar(this);
        }
    }

    private void OnMouseExit()
    {
        if (DrawingManager.Instance.IsDrawing)
        {
            DrawingManager.Instance.EndHoveredStar(this);
        }
    }
}