using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Toggleable debug overlay for the Director AI pressure system.
/// Press F3 to toggle visibility.
/// Shows live pressure score, intervention state, and per-factor breakdowns.
/// </summary>
public class DirectorDebugUI : MonoBehaviour
{
    public static DirectorDebugUI Instance { get; private set; }

    [Header("Toggle Settings")]
    [SerializeField] private bool startVisible = false;

    [Header("Panel Settings")]
    [SerializeField] private float panelWidth = 340f;
    [SerializeField] private float panelHeight = 300f;
    [SerializeField] private int fontSize = 12;

    private bool _isVisible;
    public bool IsVisible => _isVisible;

    private Canvas _canvas;
    private GameObject _panel;
    private TextMeshProUGUI _bodyText;

    private const char FilledChar = '\u2588';
    private const char EmptyChar = '\u2591';
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
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            Toggle();

        if (_isVisible && DirectorAI.Instance != null)
            UpdateDisplay();
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

        var d = DirectorAI.Instance;

        string levelColor = GetLevelColor(d.CurrentPressureLevel);
        string levelName = d.CurrentPressureLevel.ToString().ToUpper();

        string aggroMod = d.GetAggressionModifier().ToString("F2");
        string spawnBias = d.GetSpawnPositionBias().ToString("F0");
        int meleeMod = d.GetMeleeDiceModifier();
        int hitMod = d.GetHitThresholdModifier();
        string meleeModStr = meleeMod == 0 ? "±0" : $"{meleeMod:+#;-#}";
        string hitModStr = hitMod == 0 ? "±0" : $"{hitMod:+#;-#}";

        string interventionLabel = d.CurrentPressureLevel switch
        {
            PressureLevel.Low => "<color=#FF9800>Escalating</color>",
            PressureLevel.Moderate => "<color=#9E9E9E>Passive</color>",
            PressureLevel.Elevated => "<color=#42A5F5>Stabilizing</color>",
            PressureLevel.Critical => "<color=#2196F3>Restraining</color>",
            _ => "---"
        };

        string pressureBar = BuildBar(d.PressureScore);

        _bodyText.text =
            $"<b>Pressure Measurement</b>\n" +
            $"Level:    <color={levelColor}>{levelName}</color>\n" +
            $"Pressure: {pressureBar} {d.PressureScore:F2}  <color=#888>(raw {d.RawPressure:F2})</color>\n" +
            $"\n" +
            $"Proximity:      {d.ProximityScore:F2}  {BuildBar(d.ProximityScore)}\n" +
            $"Outnumbered:    {d.OutnumberedScore:F2}  {BuildBar(d.OutnumberedScore)}\n" +
            $"Hidden Threats: {d.HiddenThreatScore:F2}  {BuildBar(d.HiddenThreatScore)}\n" +
            $"Hero Losses:    {d.HeroLossScore:F2}  {BuildBar(d.HeroLossScore)}\n" +
            $"Combat:         {d.CombatScore:F2}  {BuildBar(d.CombatScore)}\n" +
            $"Exposure:       {d.ExposureScore:F2}  {BuildBar(d.ExposureScore)}\n" +
            $"\n" +
            $"<b>Active Interventions</b>\n" +
            $"Mode:       {interventionLabel}\n" +
            $"Aggression: x{aggroMod}  Bias: {spawnBias}\n" +
            $"Melee dice: {meleeModStr}  Hit threshold: {hitModStr}";
    }

    private string BuildBar(float value)
    {
        int filled = Mathf.RoundToInt(value * BarLength);
        int empty = BarLength - filled;

        string barColor = value < 0.3f ? "#4CAF50" :
                          value < 0.6f ? "#FFC107" :
                          value < 0.8f ? "#FF9800" :
                                         "#F44336";

        return $"<color={barColor}>{new string(FilledChar, filled)}</color><color=#333>{new string(EmptyChar, empty)}</color>";
    }

    private string GetLevelColor(PressureLevel level)
    {
        return level switch
        {
            PressureLevel.Low => "#4CAF50",
            PressureLevel.Moderate => "#FFC107",
            PressureLevel.Elevated => "#FF9800",
            PressureLevel.Critical => "#F44336",
            _ => "#FFFFFF"
        };
    }

    private void CreateUIProgrammatically()
    {
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

        _panel = new GameObject("DirectorDebugPanel", typeof(RectTransform));
        _panel.transform.SetParent(_canvas.transform, false);

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(10, 10);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image panelBg = _panel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.08f, 0.05f, 0.85f);

        _panel.AddComponent<DraggableResizablePanel>();

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

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(header.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.sizeDelta = Vector2.zero;
        titleRect.offsetMin = new Vector2(8, 0);
        titleRect.offsetMax = new Vector2(-8, 0);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Director AI (F3)";
        titleText.fontSize = 12;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

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
