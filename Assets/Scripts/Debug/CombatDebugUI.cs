using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Toggleable UI panel that displays combat debug information.
/// Press F1 to toggle visibility.
/// Shows attack rolls, hits, misses, damage, and ability usage.
/// </summary>
public class CombatDebugUI : MonoBehaviour
{
    public static CombatDebugUI Instance { get; private set; }

    [Header("Toggle Settings")]
    [SerializeField] private bool startVisible = false;

    [Header("Panel Settings")]
    [SerializeField] private float panelWidth = 450f;
    [SerializeField] private float panelHeight = 300f;
    [SerializeField] private int maxVisibleEntries = 50;
    [SerializeField] private int fontSize = 12;

    [Header("Optional References (auto-generated if null)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject entryPrefab;

    private bool _isVisible;
    public bool IsVisible => _isVisible;
    private List<GameObject> _entryObjects = new List<GameObject>();
    private Canvas _canvas;
    private GameObject _panel;
    private RectTransform _contentRect;
    private ScrollRect _scrollRect;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupUI();

        // Set initial visibility
        SetVisible(startVisible);
    }

    void OnEnable()
    {
        // Subscribe to log events
        CombatDebugLog.OnLogAdded -= OnLogAdded; // Remove first to prevent double subscription
        CombatDebugLog.OnLogCleared -= OnLogCleared;
        CombatDebugLog.OnLogAdded += OnLogAdded;
        CombatDebugLog.OnLogCleared += OnLogCleared;

        // Populate with any existing entries (delayed to ensure UI is ready)
        StartCoroutine(PopulateExistingEntriesDelayed());
    }

    void OnDisable()
    {
        CombatDebugLog.OnLogAdded -= OnLogAdded;
        CombatDebugLog.OnLogCleared -= OnLogCleared;
    }

    private System.Collections.IEnumerator PopulateExistingEntriesDelayed()
    {
        // Wait one frame to ensure UI is fully set up
        yield return null;

        if (_contentRect == null && contentContainer == null)
            yield break;

        // Clear existing UI entries first
        foreach (var obj in _entryObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        _entryObjects.Clear();

        // Populate with existing log entries
        foreach (var entry in CombatDebugLog.Entries)
        {
            CreateEntryUI(entry);
        }
        ScrollToBottom();
    }

    void OnDestroy()
    {
        CombatDebugLog.OnLogAdded -= OnLogAdded;
        CombatDebugLog.OnLogCleared -= OnLogCleared;
    }

    void Update()
    {
        // Toggle visibility with key press (F1)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    /// <summary>
    /// Toggles the debug panel visibility.
    /// </summary>
    public void Toggle()
    {
        SetVisible(!_isVisible);
    }

    /// <summary>
    /// Sets the debug panel visibility.
    /// </summary>
    public void SetVisible(bool visible)
    {
        _isVisible = visible;

        if (_panel != null)
        {
            _panel.SetActive(visible);
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }

        if (visible)
        {
            // Scroll to bottom when showing
            ScrollToBottom();
        }
    }

    private void SetupUI()
    {
        if (panelRoot != null)
        {
            // Use assigned references
            _panel = panelRoot;
            _scrollRect = scrollRect;
            if (contentContainer != null)
            {
                _contentRect = contentContainer.GetComponent<RectTransform>();
            }
            return;
        }

        // Create UI programmatically
        CreateUIProgrammatically();
    }

    private void CreateUIProgrammatically()
    {
        // Find or create canvas
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            _canvas = FindFirstObjectByType<Canvas>();
        }
        if (_canvas == null)
        {
            GameObject canvasObj = new GameObject("DebugCanvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100; // On top of everything
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create main panel
        _panel = new GameObject("CombatDebugPanel", typeof(RectTransform));
        _panel.transform.SetParent(_canvas.transform, false);

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        // Anchor to top-left
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(10, -10);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Add background
        Image panelBg = _panel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

        // Add draggable/resizable functionality
        DraggableResizablePanel dragResize = _panel.AddComponent<DraggableResizablePanel>();

        // Create header
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(_panel.transform, false);

        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0, 25);

        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.15f, 0.15f, 0.25f, 1f);

        // Header title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(header.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.sizeDelta = Vector2.zero;
        titleRect.offsetMin = new Vector2(8, 0);
        titleRect.offsetMax = new Vector2(-60, 0);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Combat Log (Press F1 to toggle)";
        titleText.fontSize = 12;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Clear button
        GameObject clearBtn = new GameObject("ClearButton", typeof(RectTransform));
        clearBtn.transform.SetParent(header.transform, false);

        RectTransform clearRect = clearBtn.GetComponent<RectTransform>();
        clearRect.anchorMin = new Vector2(1, 0);
        clearRect.anchorMax = new Vector2(1, 1);
        clearRect.pivot = new Vector2(1, 0.5f);
        clearRect.anchoredPosition = new Vector2(-5, 0);
        clearRect.sizeDelta = new Vector2(50, 0);

        Button clearButton = clearBtn.AddComponent<Button>();
        Image clearBg = clearBtn.AddComponent<Image>();
        clearBg.color = new Color(0.4f, 0.2f, 0.2f, 1f);

        GameObject clearTextObj = new GameObject("Text", typeof(RectTransform));
        clearTextObj.transform.SetParent(clearBtn.transform, false);
        clearTextObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        clearTextObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        clearTextObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        TextMeshProUGUI clearText = clearTextObj.AddComponent<TextMeshProUGUI>();
        clearText.text = "Clear";
        clearText.fontSize = 11;
        clearText.color = Color.white;
        clearText.alignment = TextAlignmentOptions.Center;

        clearButton.onClick.AddListener(() => CombatDebugLog.Clear());

        // Create scroll view
        GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform));
        scrollView.transform.SetParent(_panel.transform, false);

        RectTransform scrollViewRect = scrollView.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = Vector2.zero;
        scrollViewRect.anchorMax = Vector2.one;
        scrollViewRect.sizeDelta = Vector2.zero;
        scrollViewRect.offsetMin = new Vector2(5, 5);
        scrollViewRect.offsetMax = new Vector2(-5, -30);

        _scrollRect = scrollView.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 20f;

        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.02f, 0.02f, 0.05f, 0.5f);

        // Add mask
        Mask scrollMask = scrollView.AddComponent<Mask>();
        scrollMask.showMaskGraphic = true;

        // Create viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollView.transform, false);

        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0, 1);

        // Create content container
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        _contentRect = content.GetComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0, 1);
        _contentRect.anchorMax = new Vector2(1, 1);
        _contentRect.pivot = new Vector2(0, 1);
        _contentRect.anchoredPosition = Vector2.zero;
        _contentRect.sizeDelta = new Vector2(0, 0);

        // Add vertical layout group
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.spacing = 2;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Add content size fitter
        ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.viewport = viewportRect;
        _scrollRect.content = _contentRect;

        // Create scrollbar
        GameObject scrollbar = new GameObject("Scrollbar", typeof(RectTransform));
        scrollbar.transform.SetParent(scrollView.transform, false);

        RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(0, 0);
        scrollbarRect.sizeDelta = new Vector2(8, 0);

        Image scrollbarBg = scrollbar.AddComponent<Image>();
        scrollbarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        Scrollbar sb = scrollbar.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        // Create scrollbar handle
        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(scrollbar.transform, false);

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = Vector2.zero;

        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);

        sb.handleRect = handleRect;
        sb.targetGraphic = handleImage;

        _scrollRect.verticalScrollbar = sb;
        _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        contentContainer = content.transform;
    }

    private void OnLogAdded(CombatDebugLog.LogEntry entry)
    {
        CreateEntryUI(entry);
        ScrollToBottom();
    }

    private void OnLogCleared()
    {
        foreach (var obj in _entryObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        _entryObjects.Clear();
    }

    private void CreateEntryUI(CombatDebugLog.LogEntry entry)
    {
        Transform parent = contentContainer != null ? contentContainer : _contentRect;
        if (parent == null)
            return;

        GameObject entryObj;

        if (entryPrefab != null)
        {
            entryObj = Instantiate(entryPrefab, parent);
        }
        else
        {
            entryObj = CreateEntryProgrammatically(parent);
        }

        // Set text and color
        TextMeshProUGUI text = entryObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            float timestamp = entry.Timestamp;
            int minutes = Mathf.FloorToInt(timestamp / 60f);
            int seconds = Mathf.FloorToInt(timestamp % 60f);
            text.text = $"[{minutes:00}:{seconds:00}] {entry.Message}";
            text.color = entry.Color;
        }

        _entryObjects.Add(entryObj);

        // Trim old entries
        while (_entryObjects.Count > maxVisibleEntries)
        {
            if (_entryObjects[0] != null)
            {
                Destroy(_entryObjects[0]);
            }
            _entryObjects.RemoveAt(0);
        }
    }

    private GameObject CreateEntryProgrammatically(Transform parent)
    {
        GameObject entryObj = new GameObject("LogEntry", typeof(RectTransform));
        entryObj.transform.SetParent(parent, false);

        RectTransform entryRect = entryObj.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, fontSize + 4);

        TextMeshProUGUI text = entryObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Truncate;

        // Add layout element for proper sizing
        LayoutElement layoutElement = entryObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = fontSize + 4;
        layoutElement.preferredHeight = fontSize + 4;
        layoutElement.flexibleWidth = 1;

        return entryObj;
    }

    private void ScrollToBottom()
    {
        if (_scrollRect != null)
        {
            // Force layout update then scroll
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
