using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueHistoryUI : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_Text historyText;

    private const int MaxHistoryCount = 100;

    private readonly Queue<string> history = new();

    private void OnEnable()
    {
        Refresh();
    }

    public void RecordLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        history.Enqueue(text);

        while (history.Count > MaxHistoryCount)
            history.Dequeue();

        if (isActiveAndEnabled)
            Refresh();
    }

    public void Refresh()
    {
        bool hasHistory = history.Count > 0;
        
        scrollRect.gameObject.SetActive(hasHistory);

        if (!hasHistory)
        {
            historyText.text = "";
            return;
        }

        var builder = new StringBuilder();

        foreach (string line in history)
        {
            if (builder.Length > 0)
                builder.Append("\n\n");

            builder.Append(line);
        }

        historyText.text = builder.ToString();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            scrollRect.content
        );

        // 열 때 가장 최근 대사가 있는 맨 아래로 이동.
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void ClearHistory()
    {
        history.Clear();

        if (isActiveAndEnabled)
            Refresh();
    }
}