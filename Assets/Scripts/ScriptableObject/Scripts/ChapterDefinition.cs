using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(menuName = "StarPath/ChapterDefinition")]
public class ChapterDefinition : ScriptableObject
{
    [Header("Chapter Info")] public string chapterId;
    
    [Header("Characters")] public CutSceneCast cutSceneCasts;
    
    [Header("Constellation")] [CanBeNull] public ConstellationData constellationData;
    
    [Header("Animation")] public AnimationCommand[] animations;
}