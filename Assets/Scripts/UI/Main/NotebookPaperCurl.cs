using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class NotebookPaperCurl : BaseMeshEffect
{
    [Header("휘어지는 모양")]
    [SerializeField, Range(0f, 1f)]
    private float progress;

    [SerializeField, Range(0.03f, 0.25f)]
    private float radiusRatio = 0.12f;

    [SerializeField, Range(12, 80)]
    private int subdivisions = 48;

    [SerializeField, Range(0.01f, 1f)]
    private float horizontalDirection = 0.35f;

    [Header("애니메이션")]
    [SerializeField, Min(0.1f)]
    private float duration = 0.9f;

    [SerializeField, Range(0.1f, 0.9f)]
    private float fadeStart = 0.5f;

    private float opacity = 1f;
    private Sequence sequence;

    private readonly List<UIVertex> vertices = new List<UIVertex>();
    private readonly List<Triangle> triangles = new List<Triangle>();
    
    private bool returnFromRight;

    private struct Triangle
    {
        public int a;
        public int b;
        public int c;
        public float depth;
    }

    public void Configure(
        float seconds,
        float radius,
        float horizontal,
        float fade)
    {
        duration = Mathf.Max(0.1f, seconds);
        radiusRatio = Mathf.Clamp(radius, 0.03f, 0.25f);
        horizontalDirection = Mathf.Clamp(horizontal, 0.01f, 1f);
        fadeStart = Mathf.Clamp(fade, 0.1f, 0.9f);

        ResetPage();
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);

        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }

    [ContextMenu("Play Turn (Play Mode)")]
    public void PlayTurn()
    {
        PlayTurn(null);
    }

    public void PlayTurn(Action onComplete)
    {
        if (!Application.isPlaying || !IsActive())
            return;
        
        returnFromRight = false;
        ResetPage();

        ResetPage();

        float seconds = Mathf.Max(0.1f, duration);
        float fadeStartTime =
            seconds * Mathf.Clamp(fadeStart, 0.1f, 0.9f);

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        sequence.Append(
            DOTween.To(
                () => progress,
                SetProgress,
                1f,
                seconds
            ).SetEase(Ease.InOutSine)
        );

        sequence.Insert(
            fadeStartTime,
            DOTween.To(
                () => opacity,
                value =>
                {
                    opacity = value;
                    graphic.SetVerticesDirty();
                },
                0f,
                seconds - fadeStartTime
            ).SetEase(Ease.InOutSine)
        );

        sequence.OnComplete(() =>
        {
            sequence = null;
            onComplete?.Invoke();
        });
    }
    
    public void PlayReverse(Action onComplete)
    {
        if (!Application.isPlaying || !IsActive())
            return;
        
        returnFromRight = true;
        ResetPage();

        ResetPage();

        // 넘어간 상태에서 시작.
        opacity = 0f;
        SetProgress(1f);

        float seconds = Mathf.Max(0.1f, duration);

        // 정방향 페이드아웃 구간을 거꾸로 재생.
        float fadeDuration =
            seconds * (1f - Mathf.Clamp(fadeStart, 0.1f, 0.9f));

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        sequence.Append(
            DOTween.To(
                () => progress,
                SetProgress,
                0f,
                seconds
            ).SetEase(Ease.InOutSine)
        );

        sequence.Insert(
            0f,
            DOTween.To(
                () => opacity,
                value =>
                {
                    opacity = value;
                    graphic.SetVerticesDirty();
                },
                1f,
                fadeDuration
            ).SetEase(Ease.InOutSine)
        );

        sequence.OnComplete(() =>
        {
            sequence = null;
            onComplete?.Invoke();
        });
    }

    [ContextMenu("Reset Page")]
    public void ResetPage()
    {
        sequence?.Kill();
        sequence = null;

        opacity = 1f;
        SetProgress(0f);
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount != 4)
            return;

        // Image 또는 RawImage의 원래 사각형 정보.
        UIVertex bottomLeft = new UIVertex();
        UIVertex topLeft = new UIVertex();
        UIVertex topRight = new UIVertex();
        UIVertex bottomRight = new UIVertex();

        vh.PopulateUIVertex(ref bottomLeft, 0);
        vh.PopulateUIVertex(ref topLeft, 1);
        vh.PopulateUIVertex(ref topRight, 2);
        vh.PopulateUIVertex(ref bottomRight, 3);

        float width = Vector3.Distance(
            bottomLeft.position,
            bottomRight.position
        );

        float height = Vector3.Distance(
            bottomLeft.position,
            topLeft.position
        );

        if (width < 0.01f || height < 0.01f)
            return;

        // 다음: 왼쪽 위로 넘어감.
// 이전: 오른쪽 위에서 돌아옴.
        float horizontal = returnFromRight
            ? -horizontalDirection
            : horizontalDirection;

        Vector2 direction =
            new Vector2(horizontal, -1f).normalized;

// 좌우 방향이 달라져도 올바른 시작점과 끝점을 계산.
        float bottomLeftProjection = Vector2.Dot(
            (Vector2)bottomLeft.position,
            direction
        );

        float topLeftProjection = Vector2.Dot(
            (Vector2)topLeft.position,
            direction
        );

        float topRightProjection = Vector2.Dot(
            (Vector2)topRight.position,
            direction
        );

        float bottomRightProjection = Vector2.Dot(
            (Vector2)bottomRight.position,
            direction
        );

        float minimum = Mathf.Min(
            bottomLeftProjection,
            topLeftProjection,
            topRightProjection,
            bottomRightProjection
        );

        float maximum = Mathf.Max(
            bottomLeftProjection,
            topLeftProjection,
            topRightProjection,
            bottomRightProjection
        );

        float radius =
            Mathf.Min(width, height) *
            Mathf.Clamp(radiusRatio, 0.03f, 0.25f);

        // 휘어지는 경계가 오른쪽 아래에서 왼쪽 위로 이동.
        float fold = Mathf.Lerp(
            maximum + 0.01f,
            minimum - Mathf.PI * radius,
            Mathf.Clamp01(progress)
        );

        int count = Mathf.Clamp(subdivisions, 12, 80);
        int row = count + 1;

        vertices.Clear();
        triangles.Clear();

        for (int y = 0; y <= count; y++)
        {
            float v = y / (float)count;

            for (int x = 0; x <= count; x++)
            {
                float u = x / (float)count;

                UIVertex vertex = bottomLeft;

                vertex.position = Vector3.Lerp(
                    Vector3.Lerp(
                        bottomLeft.position,
                        bottomRight.position,
                        u
                    ),
                    Vector3.Lerp(
                        topLeft.position,
                        topRight.position,
                        u
                    ),
                    v
                );

                vertex.uv0 = Vector4.Lerp(
                    Vector4.Lerp(
                        bottomLeft.uv0,
                        bottomRight.uv0,
                        u
                    ),
                    Vector4.Lerp(
                        topLeft.uv0,
                        topRight.uv0,
                        u
                    ),
                    v
                );

                Color tint = Color.Lerp(
                    Color.Lerp(
                        (Color)bottomLeft.color,
                        (Color)bottomRight.color,
                        u
                    ),
                    Color.Lerp(
                        (Color)topLeft.color,
                        (Color)topRight.color,
                        u
                    ),
                    v
                );

                float distance = Vector2.Dot(
                    (Vector2)vertex.position,
                    direction
                ) - fold;

                if (distance > 0f)
                {
                    // 둥근 곡면을 지난 부분은 뒤집힌 종이로 이어짐.
                    float angle = Mathf.Min(
                        distance / radius,
                        Mathf.PI
                    );

                    float along =
                        distance <= Mathf.PI * radius
                            ? radius * Mathf.Sin(angle)
                            : -(distance - Mathf.PI * radius);

                    Vector2 shift =
                        direction * (along - distance);

                    vertex.position += new Vector3(
                        shift.x,
                        shift.y,
                        -radius * (1f - Mathf.Cos(angle))
                    );

                    // 곡면을 알아볼 수 있도록 약한 음영 적용.
                    float shade =
                        1f - 0.28f * Mathf.Sin(angle);

                    if (angle > Mathf.PI * 0.5f)
                    {
                        shade *= 0.9f;
                    }

                    tint.r *= shade;
                    tint.g *= shade;
                    tint.b *= shade;
                }

                tint.a *= opacity;
                vertex.color = tint;

                vertices.Add(vertex);
            }
        }

        for (int y = 0; y < count; y++)
        {
            for (int x = 0; x < count; x++)
            {
                int index = y * row + x;

                AddTriangle(
                    index,
                    index + row,
                    index + row + 1
                );

                AddTriangle(
                    index,
                    index + row + 1,
                    index + 1
                );
            }
        }

        // 들어 올려진 부분을 나중에 그려서 앞에 표시.
        triangles.Sort(
            (a, b) => b.depth.CompareTo(a.depth)
        );

        vh.Clear();

        foreach (UIVertex vertex in vertices)
        {
            UIVertex output = vertex;

            // 화면상의 휘어진 모양은 유지하고,
            // 출력 깊이는 원래 종이 평면에 맞춤.
            output.position.z = bottomLeft.position.z;

            vh.AddVert(output);
        }

        foreach (Triangle triangle in triangles)
        {
            vh.AddTriangle(
                triangle.a,
                triangle.b,
                triangle.c
            );
        }
    }

    private void AddTriangle(int a, int b, int c)
    {
        triangles.Add(new Triangle
        {
            a = a,
            b = b,
            c = c,

            depth = (
                vertices[a].position.z +
                vertices[b].position.z +
                vertices[c].position.z
            ) / 3f
        });
    }

    protected override void OnDisable()
    {
        sequence?.Kill();
        sequence = null;

        opacity = 1f;
        progress = 0f;

        base.OnDisable();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }
#endif
}