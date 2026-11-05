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

    // 문자열 대신 대사 데이터를 보관.
    private readonly Queue<DialogueData> history = new();

    private void OnEnable()
    {
        SettingsUI.LanguageChanged += OnLanguageChanged;

        Refresh();
    }

    private void OnDisable()
    {
        SettingsUI.LanguageChanged -= OnLanguageChanged;
    }

    public void RecordLine(DialogueData dialogue)
    {
        if (dialogue == null)
            return;

        // 어느 언어에도 내용이 없는 대사는 기록하지 않음.
        if (string.IsNullOrWhiteSpace(dialogue.Text_Kr) &&
            string.IsNullOrWhiteSpace(dialogue.Text_En) &&
            string.IsNullOrWhiteSpace(dialogue.Text_ZhHans))
        {
            return;
        }

        // 같은 대사도 다시 재생하면 새로운 기록으로 추가.
        history.Enqueue(dialogue);

        while (history.Count > MaxHistoryCount)
            history.Dequeue();

        if (isActiveAndEnabled)
            Refresh();
    }

    private void OnLanguageChanged()
    {
        // 언어 변경 시 읽고 있던 스크롤 위치를 유지.
        float scrollPosition = scrollRect != null
            ? scrollRect.verticalNormalizedPosition
            : 0f;

        Refresh();

        if (scrollRect != null && history.Count > 0)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = scrollPosition;
        }
    }

    public void Refresh()
    {
        if (scrollRect == null || historyText == null)
            return;

        bool hasHistory = history.Count > 0;

        scrollRect.gameObject.SetActive(hasHistory);

        if (!hasHistory)
        {
            historyText.text = "";
            return;
        }

        var builder = new StringBuilder();

        foreach (DialogueData line in history)
        {
            if (builder.Length > 0)
                builder.Append("\n\n");

            // 기록을 보여주는 시점의 언어로 출력.
            builder.Append(line.GetText());
        }

        historyText.text = builder.ToString();

        Canvas.ForceUpdateCanvases();

        if (scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                scrollRect.content);
        }

        // 기록창을 열 때 가장 최근 대사로 이동.
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