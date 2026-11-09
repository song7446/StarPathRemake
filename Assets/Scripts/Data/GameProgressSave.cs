using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameProgressData
{
    public int version = 1;

    public bool[] watchedScenes = Array.Empty<bool>();
    public List<string> playedMonologueIds = new();
}

public static class GameProgressSave
{
    private const string SaveKey = "StarPath.Progress";

    public static bool TryLoad(out GameProgressData data)
    {
        data = null;

        if (!PlayerPrefs.HasKey(SaveKey))
            return false;

        try
        {
            string json = PlayerPrefs.GetString(SaveKey, "");

            if (string.IsNullOrWhiteSpace(json))
                return false;

            data = JsonUtility.FromJson<GameProgressData>(json);

            if (data == null || data.version != 1)
            {
                data = null;
                Debug.LogWarning("지원하지 않는 진행 저장 데이터입니다.");
                return false;
            }

            data.watchedScenes ??= Array.Empty<bool>();
            data.playedMonologueIds ??= new List<string>();

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"진행 기록을 불러오지 못했습니다: {exception.Message}");

            data = null;
            return false;
        }
    }

    public static void Save(
        bool[] watchedScenes,
        IEnumerable<string> playedMonologueIds)
    {
        if (watchedScenes == null)
            return;

        var data = new GameProgressData
        {
            watchedScenes = (bool[])watchedScenes.Clone(),

            playedMonologueIds = playedMonologueIds != null
                ? new List<string>(playedMonologueIds)
                : new List<string>()
        };

        PlayerPrefs.SetString(
            SaveKey,
            JsonUtility.ToJson(data));

        PlayerPrefs.Save();
    }

    // 새 게임에서 호출한다. 설정 저장값은 유지한다.
    public static void DeleteProgress()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}