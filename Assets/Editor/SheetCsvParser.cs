using System;
using System.Collections.Generic;
using System.Text;

public static class SheetCsvParser
{
    public static List<string[]> Parse(string csv)
    {
        var rows = new List<string[]>();

        if (string.IsNullOrEmpty(csv))
            return rows;

        // 파일 시작의 BOM만 제거
        csv = csv.TrimStart('\uFEFF');

        var cells = new List<string>();
        var cell = new StringBuilder();

        bool inQuotes = false;
        bool afterQuote = false;
        bool rowStarted = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];

            // 큰따옴표로 감싸진 셀 내부
            if (inQuotes)
            {
                if (c == '"')
                {
                    // ""는 셀 내용에 들어가는 큰따옴표 하나
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                        afterQuote = true;
                    }
                }
                else
                {
                    // 셀 내부 쉼표와 줄바꿈은 그대로 보존
                    cell.Append(c);
                }

                continue;
            }

            // 닫는 따옴표 뒤에는 구분자 또는 줄바꿈만 허용
            if (afterQuote && c != ',' && c != '\r' && c != '\n')
            {
                throw new FormatException(
                    $"CSV 형식 오류: 닫는 따옴표 뒤에 잘못된 문자가 있습니다. 위치: {i}");
            }

            if (c == '"')
            {
                if (cell.Length > 0)
                    throw new FormatException(
                        $"CSV 형식 오류: 셀 중간에 큰따옴표가 있습니다. 위치: {i}");

                inQuotes = true;
                rowStarted = true;
            }
            else if (c == ',')
            {
                cells.Add(cell.ToString());
                cell.Clear();

                afterQuote = false;
                rowStarted = true;
            }
            else if (c == '\r' || c == '\n')
            {
                cells.Add(cell.ToString());
                rows.Add(cells.ToArray());

                cells.Clear();
                cell.Clear();

                afterQuote = false;
                rowStarted = false;

                // Windows 줄바꿈 CRLF는 한 번만 처리
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    i++;
            }
            else
            {
                cell.Append(c);
                rowStarted = true;
            }
        }

        if (inQuotes)
            throw new FormatException("CSV의 큰따옴표가 닫히지 않았습니다.");

        // 마지막 행에 줄바꿈이 없어도 추가
        if (rowStarted)
        {
            cells.Add(cell.ToString());
            rows.Add(cells.ToArray());
        }

        return rows;
    }
}