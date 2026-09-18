using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DialogueSheetImporter
{
    public static void Import(
        IReadOnlyList<string[]> rows,
        DialogueDatabase target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (!AssetDatabase.Contains(target))
            throw new InvalidOperationException(
                "프로젝트에 저장된 DialogueDatabase 에셋을 지정해주세요.");

        if (rows == null || rows.Count < 2 || rows[0] == null)
            throw new InvalidOperationException("시트에 대사 데이터가 없습니다.");

        // 첫 번째 행에서 컬럼 위치 찾기
        string[] header = rows[0];

        int idColumn = FindColumn(header, "ID");
        int chapterColumn = FindColumn(header, "Chapter_Id");
        int speakerColumn = FindColumn(header, "Speaker");
        int krColumn = FindColumn(header, "Text_Kr");
        int enColumn = FindColumn(header, "Text_En");

        var result = new List<DialogueData>();
        var ids = new HashSet<string>();

        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];

            if (IsEmptyRow(row))
                continue;

            string id = GetCell(row, idColumn).Trim();
            string chapterId = GetCell(row, chapterColumn).Trim();

            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException(
                    $"시트 {i + 1}행: ID가 없습니다.");

            if (string.IsNullOrEmpty(chapterId))
                throw new InvalidOperationException(
                    $"시트 {i + 1}행: Chapter_Id가 없습니다.");

            if (!ids.Add(id))
                throw new InvalidOperationException(
                    $"시트 {i + 1}행: 중복 ID '{id}'");

            result.Add(new DialogueData
            {
                ID = id,
                Chapter_Id = chapterId,
                Speaker = GetCell(row, speakerColumn).Trim(),

                // 대사의 공백과 줄바꿈은 그대로 보존
                Text_Kr = GetCell(row, krColumn),
                Text_En = GetCell(row, enColumn)
            });
        }

        if (result.Count == 0)
            throw new InvalidOperationException("가져올 대사가 없습니다.");

        // 모든 행을 정상적으로 변환한 뒤 기존 SO 갱신
        Undo.RecordObject(target, "Import Dialogue Sheet");

        target.ReplaceData(result);

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        Debug.Log($"대사 {result.Count}개 가져오기 완료", target);
    }

    private static int FindColumn(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
        {
            string value = (header[i] ?? "")
                .Trim()
                .TrimStart('\uFEFF');

            if (string.Equals(
                value, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"'{name}' 컬럼이 없습니다.");
    }

    private static string GetCell(string[] row, int index)
    {
        return index < row.Length ? row[index] ?? "" : "";
    }

    private static bool IsEmptyRow(string[] row)
    {
        if (row == null)
            return true;

        foreach (string cell in row)
        {
            if (!string.IsNullOrWhiteSpace(cell))
                return false;
        }

        return true;
    }
}