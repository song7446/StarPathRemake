using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

public class FontCoverageCheckWindow : EditorWindow
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private TextAsset csvFile;

    [SerializeField] private int languageIndex = 1;

    [SerializeField, TextArea(3, 10)]
    private string additionalText =
        "한국어 English 简体中文 日本語 Español Português (Brasil) Русский";

    private static readonly string[] LanguageNames =
    {
        "한국어",
        "영어",
        "중국어 간체",
        "일본어",
        "스페인어",
        "브라질 포르투갈어",
        "러시아어"
    };

    private static readonly string[] ColumnNames =
    {
        "Text_Kr",
        "Text_En",
        "Text_ZhHans",
        "Text_Ja",
        "Text_Es",
        "Text_Pt",
        "Text_Ru"
    };

    private string report = "";
    private Vector2 scrollPosition;

    private sealed class Sample
    {
        public string Location;
        public string Text;

        public Sample(string location, string text)
        {
            Location = location;
            Text = text;
        }
    }

    [MenuItem("Tools/StarPath/폰트 문자 검사")]
    private static void Open()
    {
        GetWindow<FontCoverageCheckWindow>("폰트 문자 검사");
    }

    private void OnGUI()
    {
        font = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "검사할 TMP 폰트",
            font,
            typeof(TMP_FontAsset),
            false);

        csvFile = (TextAsset)EditorGUILayout.ObjectField(
            "대사 CSV",
            csvFile,
            typeof(TextAsset),
            false);

        languageIndex = EditorGUILayout.Popup(
            "검사할 언어",
            languageIndex,
            LanguageNames);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "추가 검사 문구",
            EditorStyles.boldLabel);

        additionalText = EditorGUILayout.TextArea(
            additionalText,
            GUILayout.MinHeight(70f));

        EditorGUILayout.HelpBox(
            "CSV가 없으면 추가 문구만 검사합니다.\n" +
            "CSV에서 선택 언어의 번역이 비어 있으면 영어를 검사합니다.\n" +
            "Dynamic 폰트에는 검사 과정에서 글자가 추가될 수 있습니다.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(
                   font == null || EditorApplication.isPlaying))
        {
            if (GUILayout.Button("검사하기"))
                RunCheck();
        }

        if (GUILayout.Button("결과 복사"))
            EditorGUIUtility.systemCopyBuffer = report;

        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition);

        EditorGUILayout.TextArea(
            report,
            GUILayout.ExpandHeight(true));

        EditorGUILayout.EndScrollView();
    }

    private void RunCheck()
    {
        try
        {
            var samples = new List<Sample>();

            if (csvFile != null)
                AddCsvSamples(samples);

            if (!string.IsNullOrWhiteSpace(additionalText))
            {
                samples.Add(new Sample(
                    "추가 검사 문구",
                    additionalText));
            }

            if (samples.Count == 0)
                throw new InvalidOperationException(
                    "검사할 문구가 없습니다.");

            // 같은 문자는 한 번만 검사하고 첫 사용 위치를 기록한다.
            var checkedCharacters = new HashSet<int>();

            var fallbackReport = new StringBuilder();
            var missingReport = new StringBuilder();

            int mainCount = 0;
            int fallbackCount = 0;
            int missingCount = 0;

            foreach (Sample sample in samples)
            {
                string text = sample.Text ?? "";

                for (int i = 0; i < text.Length; i++)
                {
                    // 줄바꿈, 공백, 제어 문자는 검사 대상에서 제외한다.
                    if (char.IsWhiteSpace(text[i]) ||
                        char.IsControl(text[i]))
                    {
                        continue;
                    }

                    int codePoint;

                    if (char.IsHighSurrogate(text[i]) &&
                        i + 1 < text.Length &&
                        char.IsLowSurrogate(text[i + 1]))
                    {
                        codePoint = char.ConvertToUtf32(
                            text[i], text[i + 1]);

                        i++;
                    }
                    else if (char.IsSurrogate(text[i]))
                    {
                        throw new InvalidOperationException(
                            $"{sample.Location}: " +
                            "잘못된 유니코드 문자가 있습니다.");
                    }
                    else
                    {
                        codePoint = text[i];
                    }

                    if (!checkedCharacters.Add(codePoint))
                        continue;

                    string character =
                        char.ConvertFromUtf32(codePoint);

                    // 기본 폰트만 확인하며 Dynamic 추가도 시도한다.
                    if (CanDisplay(character, false))
                    {
                        mainCount++;
                        continue;
                    }

                    // 기본 폰트에서 실패한 경우 Fallback까지 확인한다.
                    if (CanDisplay(character, true))
                    {
                        fallbackCount++;

                        AppendResult(
                            fallbackReport,
                            character,
                            codePoint,
                            sample);
                    }
                    else
                    {
                        missingCount++;

                        AppendResult(
                            missingReport,
                            character,
                            codePoint,
                            sample);
                    }
                }
            }

            var builder = new StringBuilder();

            builder.AppendLine($"폰트: {font.name}");
            builder.AppendLine(
                $"Atlas Population Mode: {font.atlasPopulationMode}");
            builder.AppendLine(
                $"Multi Atlas: {font.isMultiAtlasTexturesEnabled}");

            builder.AppendLine();
            builder.AppendLine(
                $"검사한 서로 다른 문자: {checkedCharacters.Count}개");
            builder.AppendLine($"기본 폰트 지원: {mainCount}개");
            builder.AppendLine($"Fallback 지원: {fallbackCount}개");
            builder.AppendLine($"표시 불가: {missingCount}개");

            builder.AppendLine();
            builder.AppendLine("===== 표시 불가 =====");
            builder.AppendLine(
                missingCount == 0
                    ? "없음"
                    : missingReport.ToString());

            builder.AppendLine();
            builder.AppendLine("===== Fallback으로 표시 =====");
            builder.AppendLine(
                fallbackCount == 0
                    ? "없음"
                    : fallbackReport.ToString());

            report = builder.ToString();
        }
        catch (Exception exception)
        {
            report = $"검사 실패\n{exception.Message}";
            Debug.LogException(exception);
        }

        Repaint();
    }

    private bool CanDisplay(string text, bool searchFallbacks)
    {
        return font.HasCharacters(
            text,
            out uint[] missingCharacters,
            searchFallbacks: searchFallbacks,
            tryAddCharacter: true);
    }

    private void AddCsvSamples(List<Sample> samples)
    {
        var rows = SheetCsvParser.Parse(csvFile.text);

        if (rows == null || rows.Count < 2 || rows[0] == null)
            throw new InvalidOperationException(
                "CSV에 대사 데이터가 없습니다.");

        string[] header = rows[0];

        int textColumn = FindColumn(
            header, ColumnNames[languageIndex]);

        int englishColumn = FindColumn(header, "Text_En");
        int idColumn = FindColumn(header, "ID");

        if (textColumn < 0)
        {
            throw new InvalidOperationException(
                $"{ColumnNames[languageIndex]} 열이 없습니다.");
        }

        if (englishColumn < 0)
            throw new InvalidOperationException(
                "영어 대체 출력에 필요한 Text_En 열이 없습니다.");

        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];

            string text = GetCell(row, textColumn);
            bool usedEnglish = false;

            // 게임의 영어 대체 출력 정책과 맞춘다.
            if (string.IsNullOrWhiteSpace(text))
            {
                text = GetCell(row, englishColumn);
                usedEnglish = true;
            }

            if (string.IsNullOrWhiteSpace(text))
                continue;

            string id = GetCell(row, idColumn);

            string location =
                $"CSV 레코드 {i + 1}, ID: {id}";

            if (usedEnglish)
                location += " [영어 대체]";

            samples.Add(new Sample(location, text));
        }
    }

    private static int FindColumn(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
        {
            string value = (header[i] ?? "")
                .Trim()
                .TrimStart('\uFEFF');

            if (string.Equals(
                    value,
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetCell(string[] row, int index)
    {
        if (row == null || index < 0 || index >= row.Length)
            return "";

        return row[index] ?? "";
    }

    private static void AppendResult(
        StringBuilder builder,
        string character,
        int codePoint,
        Sample sample)
    {
        builder.AppendLine(
            $"문자: {character} / U+{codePoint:X4}");

        builder.AppendLine($"위치: {sample.Location}");

        builder.AppendLine(
            $"문구: {sample.Text.Replace("\r", "").Replace("\n", " ")}");

        builder.AppendLine();
    }
}