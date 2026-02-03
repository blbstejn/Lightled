using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Toggleable debug overlay for the Director AI.
/// Press F3 to toggle visibility.
/// Shows live tension score, director state, and per-factor breakdowns.
/// </summary>
public class DirectorDebugUI : MonoBehaviour
{
    public static DirectorDebugUI Instance { get; private set; }

    [Header("Toggle Settings")]
    [SerializeField] private bool startVisible = false;

    [Header("Panel Settings")]
    [SerializeField] private float panelWidth = 320f;
    [SerializeField] private float panelHeight = 220f;
    [SerializeField] private int fontSize = 12;

    private bool _isVisible;
    private Canvas _canvas;
    private GameObject _panel;
    private TextMeshProUGUI _bodyText;

    // Bar characters for visual tension display
    private const char FilledChar = '\u2588'; // █
    private const char EmptyChar = '\u2591';  // ░
    private const int BarLength = 10;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateUIProgrammatically();
        SetVisible(startVisible);
    }

    void Update()
    {
        // Toggle visibility with F3
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
        {
            Toggle();
        }

        // Update the display every frame if visible
        if (_isVisible && DirectorAI.Instance != null)
        {
            UpdateDisplay();
        }
    }

    public void Toggle()
    {
        SetVisible(!_isVisible);
    }

    public void SetVisible(bool visible)
    {
        _isVisible = visible;
        if (_panel != null)
            _panel.SetActive(visible);
    }

    private void UpdateDisplay()
    {
        if (_bodyText == null || DirectorAI.Instance == null)
            return;

        var director = DirectorAI.Instance;
        string stateColor = GetStateColor(director.CurrentState);
        string stateName = director.CurrentState.ToString().ToUpper();

        string tensionBar = BuildBar(director.TensionScore);

        _bodyText.text =
            $"State:   <color={stateColor}>{stateName}</color>\n" +
            $"Tension: {tensionBar} {director.TensionScore:F2}  <color=#888>(raw {director.RawTension:F2})</color>\n" +
            $"\n" +
            $"Proximity:      {director.ProximityScore:F2}  {BuildBar(director.ProximityScore)}\n" +
            $"Outnumbered:    {director.OutnumberedScore:F2}  {BuildBar(director.OutnumberedScore)}\n" +
            $"Hidden Threats: {director.HiddenThreatScore:F2}  {BuildBar(director.HiddenThreatScore)}\n" +
            $"Hero Losses:    {director.HeroLossScore:F2}  {BuildBar(director.HeroLossScore)}\n" +
            $"Resources:      {director.ResourceScore:F2}  {BuildBar(director.ResourceScore)}\n" +
            $"Recent Combat:  {director.CombatScore:F2}  {BuildBar(director.CombatScore)}";
    }

    /// <summary>
    /// Builds a visual bar string from a 0-1 value.
    /// Example: 0.7 -> "███████░░░"
    /// </summary>
    private string BuildBar(float value)
    {
        int filled = Mathf.RoundToInt(value * BarLength);
        int empty = BarLength - filled;

        string barColor = value < 0.3f ? "#4CAF50" :  // Green
                          value < 0.6f ? "#FFC107" :  // Yellow
                          value < 0.8f ? "#FF9800" :  // Orange
                                         "#F44336";   // Red

        return $"<color={barColor}>{new string(FilledChar, filled)}</color><color=#333>{new string(EmptyChar, empty)}</color>";
    }

    /// <summary>
    /// Returns the TMP color tag for a given director state.
    /// </summary>
    private string GetStateColor(DirectorState state)
    {
        return state switch
        {
            DirectorState.Calm => "#4CAF50",        // Green
            DirectorState.Building => "#FFC107",     // Yellow
            DirectorState.Escalating => "#FF9800",   // Orange
            DirectorState.Critical => "#F44336",     // Red
            _ => "#FFFFFF"
        };
    }

    private void CreateUIProgrammatically()
    {
        // Find or create canvas
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
            _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null)
        {
            GameObject canvasObj = new GameObject("DebugCanvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create main panel — anchored to bottom-left to avoid overlap with
        // CombatDebugUI (top-left, F1) and AIDebugUI (top-right, F2)
        _panel = new GameObject("DirectorDebugPanel", typeof(RectTransform));
        _panel.transform.SetParent(_canvas.transform, false);

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(10, 10);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Background
        Image panelBg = _panel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.08f, 0.05f, 0.85f);

        // Draggable
        _panel.AddComponent<DraggableResizablePanel>();

        // Header
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(_panel.transform, false);

        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0, 25);

        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.15f, 0.2f, 0.15f, 1f);

        // Header title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(header.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.sizeDelta = Vector2.zero;
        titleRect.offsetMin = new Vector2(8, 0);
        titleRect.offsetMax = new Vector2(-8, 0);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Director AI (Press F3 to toggle)";
        titleText.fontSize = 12;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Body text — single TextMeshProUGUI that gets updated every frame
        GameObject bodyObj = new GameObject("Body", typeof(RectTransform));
        bodyObj.transform.SetParent(_panel.transform, false);

        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.sizeDelta = Vector2.zero;
        bodyRect.offsetMin = new Vector2(10, 10);
        bodyRect.offsetMax = new Vector2(-10, -30);

        _bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        _bodyText.fontSize = fontSize;
        _bodyText.color = new Color(0.9f, 0.9f, 0.9f);
        _bodyText.alignment = TextAlignmentOptions.TopLeft;
        _bodyText.enableWordWrapping = false;
        _bodyText.overflowMode = TextOverflowModes.Truncate;
        _bodyText.richText = true;
        _bodyText.text = "Waiting for Director AI...";
    }
}
