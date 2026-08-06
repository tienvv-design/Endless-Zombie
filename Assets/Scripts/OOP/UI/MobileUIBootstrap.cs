using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MobileUIBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        GameObject bootstrap = new GameObject(nameof(MobileUIBootstrap));
        DontDestroyOnLoad(bootstrap);
        bootstrap.AddComponent<MobileUIBootstrap>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.WorldSpace || canvas.transform.parent != null)
                continue;

            if (!canvas.TryGetComponent<ResponsiveCanvasController>(out _))
                canvas.gameObject.AddComponent<ResponsiveCanvasController>();
        }
    }
}

public class ResponsiveCanvasController : MonoBehaviour
{
    private Canvas m_Canvas;
    private CanvasScaler m_Scaler;
    private Rect m_LastSafeArea;
    private Vector2Int m_LastScreen;
    private RectTransform[] m_Rects;
    private Vector2[] m_BasePositions;
    private Vector2[] m_BaseSizes;

    private void Awake()
    {
        m_Canvas = GetComponent<Canvas>();
        m_Scaler = GetComponent<CanvasScaler>();
        ConfigureCanvas();
        CacheLayout();
        ApplySafeArea();
    }

    private void Start()
    {
        // CanvasScaler calculates its final scale after Awake.
        ApplySafeArea();
    }

    private void ConfigureCanvas()
    {
        if (m_Scaler == null) return;
        m_Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        m_Scaler.referenceResolution = new Vector2(1920f, 1080f);
        m_Scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        m_Scaler.matchWidthOrHeight = 0.5f;
    }

    private void CacheLayout()
    {
        m_Rects = GetComponentsInChildren<RectTransform>(true);
        m_BasePositions = new Vector2[m_Rects.Length];
        m_BaseSizes = new Vector2[m_Rects.Length];
        for (int i = 0; i < m_Rects.Length; i++)
        {
            m_BasePositions[i] = m_Rects[i].anchoredPosition;
            m_BaseSizes[i] = m_Rects[i].sizeDelta;
        }
    }

    private void Update()
    {
        if (m_LastSafeArea != Screen.safeArea || m_LastScreen.x != Screen.width || m_LastScreen.y != Screen.height)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (m_Canvas == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        m_LastSafeArea = safe;
        m_LastScreen = new Vector2Int(Screen.width, Screen.height);

        float scale = Mathf.Max(0.01f, m_Canvas.scaleFactor);
        float left = safe.xMin / scale;
        float right = (Screen.width - safe.xMax) / scale;
        float bottom = safe.yMin / scale;
        float top = (Screen.height - safe.yMax) / scale;
        float safeWidth = safe.width / scale;
        float margin = 16f;

        for (int i = 0; i < m_Rects.Length; i++)
        {
            RectTransform rect = m_Rects[i];
            if (rect == null || rect == transform) continue;
            Vector2 position = m_BasePositions[i];
            bool leftAnchored = rect.anchorMax.x <= 0.25f;
            bool rightAnchored = rect.anchorMin.x >= 0.75f;
            bool bottomAnchored = rect.anchorMax.y <= 0.25f;
            bool topAnchored = rect.anchorMin.y >= 0.75f;
            if (leftAnchored) position.x += left;
            if (rightAnchored) position.x -= right;
            if (bottomAnchored) position.y += bottom;
            if (topAnchored) position.y -= top;
            rect.anchoredPosition = position;

            Vector2 size = m_BaseSizes[i];
            if (rect.anchorMin.x == rect.anchorMax.x && size.x > safeWidth - margin * 2f)
                size.x = Mathf.Max(120f, safeWidth - margin * 2f);
            rect.sizeDelta = size;
        }
    }
}
