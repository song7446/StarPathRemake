using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DialogueCsvImportWindow : EditorWindow
{
    [SerializeField] private TextAsset _csvFile;
    [SerializeField] private DialogueDatabase _database;

    [MenuItem("Tools/StarPath/Import Dialogue CSV")]
    private static void Open()
    {
        GetWindow<DialogueCsvImportWindow>("Dialogue Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "CSV → 대사 SO",
            EditorStyles.boldLabel
        );

        _csvFile = (TextAsset)EditorGUILayout.ObjectField(
            "CSV 파일",
            _csvFile,
            typeof(TextAsset),
            false
        );

        _database = (DialogueDatabase)EditorGUILayout.ObjectField(
            "저장할 대사 SO",
            _database,
            typeof(DialogueDatabase),
            false
        );

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "CSV의 전체 대사로 대상 SO를 갱신합니다.",
            MessageType.Info
        );

        using (new EditorGUI.DisabledScope(
            _csvFile == null || _database == null))
        {
            if (GUILayout.Button("대사 가져오기"))
            {
                Import();
            }
        }
    }

    private void Import()
    {
        try
        {
            string path = AssetDatabase.GetAssetPath(_csvFile);

            if (!string.Equals(
                Path.GetExtension(path),
                ".csv",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "CSV 파일을 지정해주세요.");
            }

            var rows = SheetCsvParser.Parse(_csvFile.text);

            DialogueSheetImporter.Import(rows, _database);

            EditorGUIUtility.PingObject(_database);

            ShowNotification(new GUIContent("대사 가져오기 완료"));
        }
        catch (Exception e)
        {
            Debug.LogException(e);

            EditorUtility.DisplayDialog(
                "대사 가져오기 실패",
                e.Message,
                "확인"
            );
        }
    }
}