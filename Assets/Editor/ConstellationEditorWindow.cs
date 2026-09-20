using UnityEditor;
using UnityEngine;

public class ConstellationEditorWindow : EditorWindow
{
    [MenuItem("Tools/StarPath/Constellation")]
    private static void Open()
    {
        GetWindow<ConstellationEditorWindow>("별자리 테스트");
    }

    private void OnGUI()
    {
        GUILayout.Label("별자리 테스트", EditorStyles.boldLabel);

        // 플레이 모드에서만 버튼 활성화
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("별자리 완성"))
            {
                CompleteConstellation();
            }
        }
    }

    private void CompleteConstellation()
    {
        ConstellationManager.Instance.CompleteConstellation();
    }
}