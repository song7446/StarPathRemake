using System;
using System.Collections.Generic;
using DG.Tweening;
using SongLib.Core.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

public class DrawingManager : MonoBehaviourSingleton<DrawingManager>
{
    [Header("선 긋기 설정")] public LineRenderer linePrefab;

    // 상태 추적용 변수들
    private LineRenderer currentLine;
    private PuzzleStar startStar;
    private PuzzleStar hoveredStar;
    private bool isDrawing = false;
    public bool IsDrawing => isDrawing;

    // 완성된 선들을 모아둘 리스트
    private Stack<LineRenderer> permanentLines = new Stack<LineRenderer>();

    private ConstellationData currentAnswerData;
    public List<StarConnection> currentConnections = new List<StarConnection>();

    [SerializeField] private Camera starCamera;
    
    private bool isFadingOut;

    [SerializeField] private AudioClip connectStarAudioClip;


    private void Update()
    {
        if (!isDrawing || currentLine == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = starCamera.ScreenToWorldPoint(mousePos);

        if (hoveredStar != null && hoveredStar != startStar)
        {
            // 마우스가 다른 별 위에 있으면 마우스 좌표가 아니라 그 별의 정중앙에 선이 딱 붙게 만듭니다.
            currentLine.SetPosition(1, hoveredStar.transform.position);
        }
        else
        {
            currentLine.SetPosition(1, worldPos);
        }
    }

    public void StartDrawing(PuzzleStar star)
    {
        if (isFadingOut)
            return;
        
        isDrawing = true;
        startStar = star;

        // 1. 새로운 선 생성
        currentLine = Instantiate(linePrefab, transform);
        currentLine.positionCount = 2; // 선의 점 개수 (시작점, 끝점)

        // 2. 시작점(0)과 끝점(1)을 일단 클릭한 별의 위치로 고정
        currentLine.SetPosition(0, startStar.transform.position);
        currentLine.SetPosition(1, startStar.transform.position);
    }

    public void EndDrawing()
    {
        isDrawing = false;

        if (hoveredStar != null)
        {
            // 수정 1: 전역 변수 재사용 금지! 매번 새로운 객체를 찍어내야 합니다.
            StarConnection newConnection = new StarConnection();
            newConnection.starA = startStar.starID;
            newConnection.starB = hoveredStar.starID;

            Debug.Log($"{startStar.starID}번 별과 {hoveredStar.starID}번 별 연결 완료!");

            // [추가 팁] 이미 그은 선(중복)이라면 무시하는 방어 로직
            bool alreadyExists =
                currentConnections.Exists(c => c.IsSameConnection(newConnection.starA, newConnection.starB));
            if (!alreadyExists)
            {
                currentConnections.Add(newConnection);
                permanentLines.Push(currentLine);

                // 정답 체크 실행
                ConstellationManager.Instance.CheckAnswer(currentConnections);
                
                AudioManager.Instance.PlaySFX(connectStarAudioClip);
            }
            else
            {
                // 이미 연결된 선이면 방금 그은 시각적 선 파괴
                Destroy(currentLine.gameObject);
            }
        }
        else
        {
            // 아무 별에도 안 닿고 마우스를 뗐으면 선 파괴
            Destroy(currentLine.gameObject);
        }

        currentLine = null; // 선 참조 초기화

        // 상태 초기화
        startStar = null;
        hoveredStar = null;
    }

    public void HoveredStar(PuzzleStar star)
    {
        hoveredStar = star;
    }

    public void EndHoveredStar(PuzzleStar star)
    {
        hoveredStar = null;
    }

    public void OnRightClick()
    {
        // 그리는 중인 선 취소.
        // EndDrawing()을 호출하지 않아서 연결을 확정하지 않음.
        if (isDrawing)
        {
            if (currentLine != null)
                Destroy(currentLine.gameObject);

            currentLine = null;
            startStar = null;
            hoveredStar = null;
            isDrawing = false;

            return;
        }

        // 마지막으로 완성한 선 취소.
        if (permanentLines.TryPop(out LineRenderer lastLine))
        {
            if (lastLine != null)
                Destroy(lastLine.gameObject);

            // 선과 함께 연결 기록도 제거.
            if (currentConnections.Count > 0)
            {
                currentConnections.RemoveAt(
                    currentConnections.Count - 1);
            }
        }
    }

    public void DrawEditorLine(PuzzleStar editorStartStar, PuzzleStar editorEndStar)
    {
        if (isFadingOut)
            return;
        
        StarConnection newConnection = new StarConnection();
        newConnection.starA = editorStartStar.starID;
        newConnection.starB = editorEndStar.starID;
        
        bool alreadyExists =
            currentConnections.Exists(c => c.IsSameConnection(newConnection.starA, newConnection.starB));
        if (!alreadyExists)
        {
            currentConnections.Add(newConnection);
            
            LineRenderer editorLine = Instantiate(linePrefab, transform);
            editorLine.positionCount = 2;
        
            permanentLines.Push(editorLine);
            
            editorLine.SetPosition(0, editorStartStar.transform.position);
            editorLine.SetPosition(1, editorEndStar.transform.position);

            // 정답 체크 실행
            ConstellationManager.Instance.CheckAnswer(currentConnections);
        }
    }
    
    public void FadeOutAndReset(
        float duration = 0.5f,
        Action onFinished = null)
    {
        if (isFadingOut)
            return;

        isFadingOut = true;
        isDrawing = false;

        // 현재 생성된 선을 모두 수집한다.
        LineRenderer[] lines = GetComponentsInChildren<LineRenderer>(true);

        currentLine = null;
        startStar = null;
        hoveredStar = null;

        permanentLines.Clear();
        currentConnections.Clear();

        duration = Mathf.Max(0.01f, duration);

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(duration);

        foreach (LineRenderer line in lines)
        {
            if (line == null)
                continue;

            // 선의 그라데이션과 기존 투명도를 유지하며 페이드
            Gradient originalGradient = line.colorGradient;
            GradientColorKey[] colorKeys = originalGradient.colorKeys;
            GradientAlphaKey[] originalAlphaKeys = originalGradient.alphaKeys;
            GradientAlphaKey[] fadedAlphaKeys =
                new GradientAlphaKey[originalAlphaKeys.Length];

            Gradient fadedGradient = new Gradient
            {
                mode = originalGradient.mode
            };

            sequence.Insert(
                0f,
                DOVirtual.Float(1f, 0f, duration, opacity =>
                {
                    if (line == null)
                        return;

                    for (int i = 0; i < originalAlphaKeys.Length; i++)
                    {
                        fadedAlphaKeys[i] = new GradientAlphaKey(
                            originalAlphaKeys[i].alpha * opacity,
                            originalAlphaKeys[i].time
                        );
                    }

                    fadedGradient.SetKeys(colorKeys, fadedAlphaKeys);
                    line.colorGradient = fadedGradient;
                }).SetEase(Ease.InOutSine)
            );
        }

        sequence.OnComplete(() =>
        {
            foreach (LineRenderer line in lines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }

            isFadingOut = false;
            onFinished?.Invoke();
        });
    }
}