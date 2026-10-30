using System;
using UnityEngine;


[CreateAssetMenu(
    fileName = "MonologueDefinition",
    menuName = "StarPath/Monologue Definition")]
public class MonologueDefinition : ScriptableObject
{
    [Tooltip("독백을 실행하기 위해 시청 완료해야 하는 장면 인덱스")]
    [Min(0)]
    public int requiredChapterIndex;

    [Tooltip("대사 CSV에 입력한 Chapter_Id")]
    public string dialogueId;

    // 현재 플레이 중 출력 완료 여부.
    [NonSerialized]
    public bool hasPlayed;
}