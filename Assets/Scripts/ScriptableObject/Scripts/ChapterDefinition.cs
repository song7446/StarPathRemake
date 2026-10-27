using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(menuName = "StarPath/ChapterDefinition")]
public class ChapterDefinition : ScriptableObject
{
    [Header("Chapter Info")] public string chapterId;
    
    [Header("Chapter Number")] public string chapterNumber;

    [Header("Chapter Title")] public string chapterTitle;
    
    [Header("Chapter Subtitle")] public string chapterSubtitle;

    [Header("Characters")] public CutSceneCast cutSceneCasts;

    [Header("Constellation")] [CanBeNull] public ConstellationData constellationData;

    [Header("Animation")] public AnimationCommand[] animations;
    
    [Header("Thumbnail")] public Sprite thumbnail;
}