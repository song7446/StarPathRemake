using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class NotebookPageTurn : MonoBehaviour
{
    [Header("페이지 순서")]
    [SerializeField] private RectTransform[] pages;

    [SerializeField, Min(0)]
    private int currentPageIndex;

// 현재 넘길 페이지. 배열에서 자동으로 지정.
    private RectTransform pageRoot;

    [Header("넘기는 연출")] [SerializeField, Min(0.1f)]
    private float duration = 0.9f;

    [SerializeField, Range(0.03f, 0.25f)] private float radiusRatio = 0.12f;

    [SerializeField, Range(0.01f, 1f)] private float horizontalDirection = 0.35f;

    [SerializeField, Range(0.1f, 0.9f)] private float fadeStart = 0.5f;

    [Header("캡처")] [SerializeField, Range(512, 2048)]
    private int textureHeight = 1024;

    [Tooltip("블러나 모자이크 Feature가 없는 Renderer의 목록 번호")] [SerializeField, Min(0)]
    private int captureRendererIndex;

    [Header("페이지가 사라진 순간 실행")] [SerializeField]
    private UnityEvent onPageHidden = new UnityEvent();

    public bool IsTurning { get; private set; }

    private CanvasGroup sourceGroup;

    private float savedAlpha;
    private bool savedInteractable;
    private bool savedBlocksRaycasts;
    private bool hasSavedState;

    private Coroutine captureRoutine;

    private RawImage proxy;
    private NotebookPaperCurl curl;

    private Canvas captureCanvas;
    private Camera captureCamera;
    private RenderTexture texture;

    private Action updatePage;
    private int generation;

    private void Awake()
    {
        InitPage();
    }

    private void InitPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        currentPageIndex = Mathf.Clamp(
            currentPageIndex,
            0,
            pages.Length - 1
        );

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].gameObject.SetActive(
                    i == currentPageIndex
                );
            }
        }

        pageRoot = pages[currentPageIndex];
    }

    // 버튼에서 호출하는 함수.
    public void TurnNext()
    {
        if (IsTurning || !isActiveAndEnabled)
            return;

        if (pages == null || pages.Length == 0)
            return;

        int nextIndex = currentPageIndex + 1;

        // 마지막 페이지에서는 넘기지 않음.
        if (nextIndex >= pages.Length)
            return;

        RectTransform nextPage = pages[nextIndex];

        if (nextPage == null)
        {
            Debug.LogError(
                $"{nextIndex + 1}번째 페이지가 연결되지 않았습니다.",
                this
            );

            return;
        }

        TurnNext(() =>
        {
            currentPageIndex = nextIndex;
            SetCurrentPage(nextPage);
        });
    }
    
    private void SetCurrentPage(RectTransform nextPage)
    {
        pageRoot = nextPage;
        pageRoot.gameObject.SetActive(true);

        // 이전 페이지의 캡처 이미지를 새 페이지 앞에 배치.
        if (proxy != null &&
            proxy.transform.parent == pageRoot.parent)
        {
            proxy.transform.SetSiblingIndex(
                pageRoot.GetSiblingIndex() + 1
            );
        }
    }
    
    private void TurnNext(Action replaceContents)
    {
        if (IsTurning || !isActiveAndEnabled)
            return;

        if (pageRoot == null ||
            !pageRoot.gameObject.activeInHierarchy ||
            pageRoot == transform ||
            transform.IsChildOf(pageRoot))
        {
            Debug.LogError(
                "Page Root에는 움직일 페이지 부모를 연결하고, " +
                "NotebookPageTurn은 고정된 수첩 루트에 붙여주세요.",
                this
            );

            return;
        }

        if (pageRoot.GetComponentInParent<Canvas>() == null ||
            pageRoot.GetComponentsInChildren<Canvas>(true).Length != 0)
        {
            Debug.LogError(
                "Page Root는 Canvas 아래의 일반 UI 부모여야 합니다. " +
                "Page Root와 그 자식에는 별도 Canvas를 넣지 마세요.",
                this
            );

            return;
        }

        foreach (NotebookPaperCurl effect in
                 pageRoot.GetComponentsInChildren<NotebookPaperCurl>(true))
        {
            if (!effect.enabled)
                continue;

            Debug.LogError(
                "실제 종이에 붙인 기존 NotebookPaperCurl을 " +
                "제거하거나 꺼주세요. 캡처 이미지에 자동으로 적용됩니다.",
                this
            );

            return;
        }

        sourceGroup = pageRoot.GetComponent<CanvasGroup>();

        if (sourceGroup == null)
        {
            sourceGroup =
                pageRoot.gameObject.AddComponent<CanvasGroup>();
        }

        savedAlpha = sourceGroup.alpha;
        savedInteractable = sourceGroup.interactable;
        savedBlocksRaycasts = sourceGroup.blocksRaycasts;
        hasSavedState = true;

        sourceGroup.interactable = false;
        sourceGroup.blocksRaycasts = false;

        updatePage = replaceContents;
        IsTurning = true;

        generation++;

        captureRoutine = StartCoroutine(
            CaptureAndTurn(generation)
        );
    }

    private IEnumerator CaptureAndTurn(int token)
    {
        // 일반 카메라 렌더링이 끝난 뒤 페이지를 캡처.
        yield return new WaitForEndOfFrame();

        captureRoutine = null;

        if (token != generation || !isActiveAndEnabled)
            yield break;

        try
        {
            Canvas.ForceUpdateCanvases();

            CreateResources();

            CopyRect(
                pageRoot,
                proxy.rectTransform
            );

            proxy.transform.SetSiblingIndex(
                pageRoot.GetSiblingIndex() + 1
            );

            CapturePage();

            // 실제 UI 대신 캡처한 페이지를 표시.
            // 넘길 캡처 이미지 준비.
            proxy.texture = texture;
            proxy.gameObject.SetActive(true);

            curl.Configure(
                duration,
                radiusRatio,
                horizontalDirection,
                fadeStart
            );

            // 원본 상태를 복구한 뒤 즉시 비활성화.
            RestoreSource();
            pageRoot.gameObject.SetActive(false);

            // 뒤에서 보여줄 다음 페이지 활성화 / 내용 갱신.
            Action showNextPage = updatePage;
            updatePage = null;

            showNextPage?.Invoke();

            if (token != generation || !isActiveAndEnabled)
                yield break;

            onPageHidden?.Invoke();

            if (token != generation || !isActiveAndEnabled)
                yield break;

            // 다음 페이지 위에서 캡처 이미지만 넘어감.
            curl.PlayTurn(() => FinishTurn(token));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            CancelTurn();
        }
    }

    private void CreateResources()
    {
        if (proxy == null)
        {
            GameObject go = new GameObject(
                "TurningPage (Snapshot)",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(LayoutElement)
            );

            go.SetActive(false);
            go.transform.SetParent(pageRoot.parent, false);
            go.layer = pageRoot.gameObject.layer;

            go.GetComponent<LayoutElement>().ignoreLayout = true;

            proxy = go.GetComponent<RawImage>();
            proxy.raycastTarget = false;

            curl = go.AddComponent<NotebookPaperCurl>();
        }

        if (captureCanvas == null)
        {
            GameObject canvasObject = new GameObject(
                "NotebookCaptureCanvas",
                typeof(RectTransform),
                typeof(Canvas)
            );

            captureCanvas = canvasObject.GetComponent<Canvas>();
            captureCanvas.renderMode = RenderMode.WorldSpace;

            canvasObject.transform.position =
                new Vector3(10000f, 10000f, 0f);

            canvasObject.layer = 5;

            GameObject cameraObject = new GameObject(
                "NotebookCaptureCamera",
                typeof(Camera)
            );

            captureCamera = cameraObject.GetComponent<Camera>();

            captureCamera.enabled = false;
            captureCamera.orthographic = true;

            captureCamera.clearFlags =
                CameraClearFlags.SolidColor;

            captureCamera.backgroundColor = Color.clear;
            captureCamera.cullingMask = 1 << 5;

            captureCamera.nearClipPlane = 0.1f;
            captureCamera.farClipPlane = 2000f;

            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            captureCamera.useOcclusionCulling = false;

            UniversalAdditionalCameraData cameraData =
                captureCamera.GetUniversalAdditionalCameraData();

            cameraData.renderType = CameraRenderType.Base;
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;
            cameraData.volumeLayerMask = 0;

            captureCanvas.worldCamera = captureCamera;
        }

        captureCamera
            .GetUniversalAdditionalCameraData()
            .SetRenderer(captureRendererIndex);

        Vector2 size = pageRoot.rect.size;

        if (size.x <= 0f || size.y <= 0f)
        {
            throw new InvalidOperationException(
                "페이지 RectTransform의 크기가 0입니다."
            );
        }

        int height = Mathf.Clamp(
            textureHeight,
            512,
            Mathf.Min(2048, SystemInfo.maxTextureSize)
        );

        int width = Mathf.Clamp(
            Mathf.RoundToInt(height * size.x / size.y),
            16,
            SystemInfo.maxTextureSize
        );

        if (texture == null ||
            texture.width != width ||
            texture.height != height)
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }

            texture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32
            )
            {
                name = "NotebookPageSnapshot",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.Create();
        }
    }

    private void CapturePage()
    {
        // 잠깐 캡처용 Canvas로 옮겼다가 같은 함수 안에서 복구.
        Transform parent = pageRoot.parent;
        int sibling = pageRoot.GetSiblingIndex();

        Vector2 anchorMin = pageRoot.anchorMin;
        Vector2 anchorMax = pageRoot.anchorMax;
        Vector2 sizeDelta = pageRoot.sizeDelta;

        Vector3 anchoredPosition = pageRoot.anchoredPosition3D;
        Quaternion rotation = pageRoot.localRotation;
        Vector3 scale = pageRoot.localScale;

        Vector2 size = pageRoot.rect.size;

        Transform[] children =
            pageRoot.GetComponentsInChildren<Transform>(true);

        int[] layers = new int[children.Length];

        for (int i = 0; i < children.Length; i++)
        {
            layers[i] = children[i].gameObject.layer;
        }

        RenderTexture oldActive = RenderTexture.active;

        try
        {
            RectTransform canvasRect =
                (RectTransform)captureCanvas.transform;

            canvasRect.sizeDelta = size;

            pageRoot.SetParent(canvasRect, false);

            pageRoot.anchorMin = new Vector2(0.5f, 0.5f);
            pageRoot.anchorMax = new Vector2(0.5f, 0.5f);

            pageRoot.sizeDelta = size;
            pageRoot.localScale = Vector3.one;
            pageRoot.localRotation = Quaternion.identity;

            pageRoot.anchoredPosition3D = new Vector3(
                (pageRoot.pivot.x - 0.5f) * size.x,
                (pageRoot.pivot.y - 0.5f) * size.y,
                0f
            );

            foreach (Transform child in children)
            {
                child.gameObject.layer = 5;
            }

            captureCamera.targetTexture = texture;

// 캡처용 Canvas로 옮긴 뒤 실제 배치부터 확정.
            Canvas.ForceUpdateCanvases();

// 실제 월드 좌표로 페이지의 크기와 중심을 구함.
            Vector3[] corners = new Vector3[4];
            pageRoot.GetWorldCorners(corners);

// 순서: 왼쪽 아래, 왼쪽 위, 오른쪽 위, 오른쪽 아래.
            Vector3 center = (corners[0] + corners[2]) * 0.5f;

            float worldHeight = Vector3.Distance(
                corners[0],
                corners[1]
            );

            float worldWidth = Vector3.Distance(
                corners[1],
                corners[2]
            );

// 페이지가 캡처 화면을 정확히 채우도록 맞춤.
            captureCamera.transform.SetPositionAndRotation(
                center - pageRoot.forward * 1000f,
                pageRoot.rotation
            );

            captureCamera.orthographicSize = worldHeight * 0.5f;
            captureCamera.aspect = worldWidth / worldHeight;

            var request =
                new UniversalRenderPipeline.SingleCameraRequest
                {
                    destination = texture
                };

            if (!RenderPipeline.SupportsRenderRequest(
                    captureCamera,
                    request))
            {
                throw new InvalidOperationException(
                    "선택한 URP Renderer가 " +
                    "SingleCameraRequest를 지원하지 않습니다."
                );
            }

            RenderPipeline.SubmitRenderRequest(
                captureCamera,
                request
            );
        }
        finally
        {
            // 캡처 실패 여부와 관계없이 원래 상태로 복구.
            pageRoot.SetParent(parent, false);
            pageRoot.SetSiblingIndex(sibling);

            pageRoot.anchorMin = anchorMin;
            pageRoot.anchorMax = anchorMax;
            pageRoot.sizeDelta = sizeDelta;

            pageRoot.anchoredPosition3D = anchoredPosition;
            pageRoot.localRotation = rotation;
            pageRoot.localScale = scale;

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                {
                    children[i].gameObject.layer = layers[i];
                }
            }

            captureCamera.targetTexture = null;
            RenderTexture.active = oldActive;

            Canvas.ForceUpdateCanvases();
        }
    }

    private static void CopyRect(
        RectTransform source,
        RectTransform destination)
    {
        destination.SetParent(source.parent, false);

        // Stretch나 Layout 설정 대신 실제 크기를 사용.
        destination.anchorMin = new Vector2(0.5f, 0.5f);
        destination.anchorMax = new Vector2(0.5f, 0.5f);

        destination.pivot = source.pivot;
        destination.sizeDelta = source.rect.size;

        destination.localPosition = source.localPosition;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private void FinishTurn(int token)
    {
        if (token != generation)
            return;

        if (proxy != null)
        {
            proxy.gameObject.SetActive(false);
        }

        updatePage = null;
        IsTurning = false;
    }

    private void RestoreSource()
    {
        if (!hasSavedState)
            return;

        if (sourceGroup != null)
        {
            sourceGroup.alpha = savedAlpha;
            sourceGroup.interactable = savedInteractable;
            sourceGroup.blocksRaycasts = savedBlocksRaycasts;
        }

        hasSavedState = false;
    }

    public void CancelTurn()
    {
        generation++;

        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
        }

        captureRoutine = null;

        if (curl != null)
        {
            curl.ResetPage();
        }

        if (proxy != null)
        {
            proxy.gameObject.SetActive(false);
        }

        RestoreSource();

        updatePage = null;
        IsTurning = false;
    }

    private void OnDisable()
    {
        CancelTurn();
    }

    private void OnDestroy()
    {
        CancelTurn();

        if (proxy != null)
        {
            Destroy(proxy.gameObject);
        }

        if (captureCanvas != null)
        {
            Destroy(captureCanvas.gameObject);
        }

        if (captureCamera != null)
        {
            Destroy(captureCamera.gameObject);
        }

        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    public void TurnPrevious()
    {
        if (IsTurning || !isActiveAndEnabled)
            return;

        if (pages == null || currentPageIndex <= 0)
            return;

        int previousIndex = currentPageIndex - 1;
        RectTransform previousPage = pages[previousIndex];

        if (previousPage == null ||
            pageRoot == null ||
            !pageRoot.gameObject.activeInHierarchy)
        {
            return;
        }

        sourceGroup = pageRoot.GetComponent<CanvasGroup>();

        if (sourceGroup == null)
        {
            sourceGroup =
                pageRoot.gameObject.AddComponent<CanvasGroup>();
        }

        savedAlpha = sourceGroup.alpha;
        savedInteractable = sourceGroup.interactable;
        savedBlocksRaycasts = sourceGroup.blocksRaycasts;
        hasSavedState = true;

        // 현재 페이지는 보이게 유지하고 입력만 차단.
        sourceGroup.interactable = false;
        sourceGroup.blocksRaycasts = false;

        updatePage = null;
        IsTurning = true;
        generation++;

        captureRoutine = StartCoroutine(
            CapturePreviousPage(previousIndex, generation)
        );
    }
    
    private IEnumerator CapturePreviousPage(
        int previousIndex,
        int token)
    {
        yield return new WaitForEndOfFrame();

        captureRoutine = null;

        if (token != generation || !isActiveAndEnabled)
            yield break;

        RectTransform currentPage = pageRoot;
        RectTransform previousPage = pages[previousIndex];

        try
        {
            // 이전 페이지를 잠깐 활성화해서 캡처.
            // 같은 프레임 안에서 다시 끄므로 화면에는 노출되지 않음.
            try
            {
                pageRoot = previousPage;
                previousPage.gameObject.SetActive(true);

                Canvas.ForceUpdateCanvases();

                CreateResources();

                CopyRect(
                    previousPage,
                    proxy.rectTransform
                );

                CapturePage();
            }
            finally
            {
                previousPage.gameObject.SetActive(false);
                pageRoot = currentPage;
            }

            // 현재 페이지 앞에 이전 페이지의 캡처 이미지를 배치.
            proxy.transform.SetSiblingIndex(
                currentPage.GetSiblingIndex() + 1
            );

            proxy.texture = texture;
            proxy.gameObject.SetActive(true);

            curl.Configure(
                duration,
                radiusRatio,
                horizontalDirection,
                fadeStart
            );

            curl.PlayReverse(() =>
            {
                if (token != generation)
                    return;

                // 이전 종이가 완전히 펼쳐진 뒤 실제 UI로 교체.
                RestoreSource();

                currentPage.gameObject.SetActive(false);

                currentPageIndex = previousIndex;
                SetCurrentPage(previousPage);

                FinishTurn(token);
            });
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            CancelTurn();
        }
    }
}