using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the in-game UI including hero stats display and ability buttons.
/// Shows HP, AP, and non-contextual abilities for the selected hero.
/// Also handles victory and defeat screens.
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance;

    [Header("Hero Stats Display")]
    [SerializeField] private GameObject heroStatsPanel;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI heroHPText;
    [SerializeField] private TextMeshProUGUI heroAPText;
    [SerializeField] private TextMeshProUGUI heroFacingText;

    [Header("Ability Buttons Container")]
    [SerializeField] private Transform abilityButtonContainer;
    [SerializeField] private GameObject abilityButtonPrefab; // Optional: can be null for auto-generation

    [Header("Turn Controls (Bottom Right)")]
    [SerializeField] private GameObject turnControlsPanel; // Optional: parent panel for bottom-right controls
    [SerializeField] private Button endTurnButton; // Optional: can be null for auto-generation
    [SerializeField] private TextMeshProUGUI commandPointsText; // Optional: can be null for auto-generation

    [Header("Game End Screens (Optional - auto-generated if null)")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Unit Tooltip (Optional - auto-generated if null)")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipNameText;
    [SerializeField] private TextMeshProUGUI tooltipHPText;
    [SerializeField] private TextMeshProUGUI tooltipAPText;

    private BaseHero _currentHero;
    private GameObject _tooltipPanel;
    private TextMeshProUGUI _tooltipNameText;
    private TextMeshProUGUI _tooltipHPText;
    private TextMeshProUGUI _tooltipAPText;
    private BaseUnit _tooltipTarget;
    private List<Button> _abilityButtons = new List<Button>();
    private Button _endTurnButton;
    private TextMeshProUGUI _commandPointsText;
    private GameObject _turnControlsPanel;
    private GameObject _victoryPanel;
    private GameObject _defeatPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Hide stats panel initially
        if (heroStatsPanel != null)
        {
            heroStatsPanel.SetActive(false);
        }

        // Setup ability button container if not assigned
        SetupAbilityButtonContainer();

        // Setup End Turn button
        SetupEndTurnButton();

        // Setup tooltip
        SetupTooltip();
    }

    void Update()
    {
        // Continuously update hero stats if a hero is selected
        if (_currentHero != null && heroStatsPanel != null && heroStatsPanel.activeSelf)
        {
            UpdateHeroStats();
        }

        // Update command points display
        UpdateCommandPointsDisplay();
    }

    /// <summary>
    /// Updates the UI to show the selected hero's stats and abilities.
    /// Called when a hero is selected.
    /// </summary>
    public void ShowHeroUI(BaseHero hero)
    {
        _currentHero = hero;

        if (hero == null)
        {
            HideHeroUI();
            return;
        }

        // Show stats panel
        if (heroStatsPanel != null)
        {
            heroStatsPanel.SetActive(true);
        }

        // Update stats
        UpdateHeroStats();

        // Generate ability buttons
        GenerateAbilityButtons();
    }

    /// <summary>
    /// Updates the hero's HP and AP display.
    /// Call this whenever hero stats change.
    /// </summary>
    public void UpdateHeroStats()
    {
        if (_currentHero == null) return;

        // Update hero name
        if (heroNameText != null)
        {
            heroNameText.text = _currentHero.UnitName;
        }

        // Update HP
        if (heroHPText != null)
        {
            heroHPText.text = $"HP: {_currentHero.CurrentHP}/{_currentHero.maxHP}";
        }

        // Update AP
        if (heroAPText != null)
        {
            heroAPText.text = $"AP: {_currentHero.CurrentActionPoints}/{_currentHero.MaxActionPoints}";
        }

        // Update facing direction
        if (heroFacingText != null)
        {
            heroFacingText.text = $"Facing: {_currentHero.Facing}";
        }
    }

    /// <summary>
    /// Hides the hero UI panel.
    /// </summary>
    public void HideHeroUI()
    {
        if (heroStatsPanel != null)
        {
            heroStatsPanel.SetActive(false);
        }

        ClearAbilityButtons();

        // Hide the ability button container
        if (abilityButtonContainer != null)
        {
            abilityButtonContainer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Generates buttons for all non-contextual abilities (UI abilities).
    /// These are abilities that can be triggered from UI buttons (like Turn Left/Right).
    /// </summary>
    private void GenerateAbilityButtons()
    {
        if (abilityButtonContainer == null)
        {
            Debug.LogWarning("InGameUIManager: Ability button container not assigned!");
            return;
        }

        // Ensure the container is active
        if (!abilityButtonContainer.gameObject.activeSelf)
        {
            abilityButtonContainer.gameObject.SetActive(true);
        }

        // Clear existing buttons
        ClearAbilityButtons();

        if (_currentHero == null) return;

        // Get all non-contextual abilities (UI abilities)
        List<UnitAbility> uiAbilities = _currentHero.unitAbilities.FindAll(a => !a.isContextual);

        if (uiAbilities.Count == 0) return;

        // Create a button for each UI ability
        foreach (UnitAbility ability in uiAbilities)
        {
            if (ability == null) continue;

            Button button = CreateAbilityButton(ability);
            if (button != null)
            {
                _abilityButtons.Add(button);
            }
        }
    }

    /// <summary>
    /// Creates a single ability button.
    /// </summary>
    private Button CreateAbilityButton(UnitAbility ability)
    {
        GameObject buttonObj;

        // Use prefab if provided, otherwise create programmatically
        if (abilityButtonPrefab != null)
        {
            buttonObj = Instantiate(abilityButtonPrefab, abilityButtonContainer);
        }
        else
        {
            buttonObj = CreateAbilityButtonProgrammatically(ability.abilityName);
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"InGameUIManager: Button component missing on ability button for {ability.abilityName}");
            Destroy(buttonObj);
            return null;
        }

        // Set button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"{ability.abilityName}\n({ability.actionPointCost} AP)";
        }

        // Add click listener
        button.onClick.AddListener(() => OnAbilityButtonClicked(ability));

        // Update button interactability based on AP cost
        UpdateAbilityButtonState(button, ability);

        return button;
    }

    /// <summary>
    /// Creates an ability button programmatically (no prefab needed).
    /// </summary>
    private GameObject CreateAbilityButtonProgrammatically(string abilityName)
    {
        // Create button GameObject
        GameObject buttonObj = new GameObject($"AbilityButton_{abilityName}", typeof(RectTransform));
        buttonObj.transform.SetParent(abilityButtonContainer, false);

        // Configure RectTransform
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(120, 60);

        // Add Button component
        Button button = buttonObj.AddComponent<Button>();

        // Add Image component for visual
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.15f, 0.35f, 0.55f, 1f); // Blue-ish

        // Create text child
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);

        // Add TextMeshProUGUI
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 14;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableWordWrapping = true;

        // Setup button colors
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.35f, 0.55f, 1f);
        colors.highlightedColor = new Color(0.2f, 0.45f, 0.65f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.5f, 1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        button.colors = colors;

        return buttonObj;
    }

    /// <summary>
    /// Called when an ability button is clicked.
    /// If hero doesn't have enough AP, Command Points will be used instead.
    /// </summary>
    private void OnAbilityButtonClicked(UnitAbility ability)
    {
        if (_currentHero == null || ability == null) return;

        int apNeeded = ability.actionPointCost;
        int heroAP = _currentHero.CurrentActionPoints;

        // Check if hero has enough AP
        if (heroAP < apNeeded)
        {
            // Try to use Command Points instead
            int cpNeeded = apNeeded - heroAP; // Use remaining AP + CP to cover the cost
            if (!GameManager.Instance.CanAffordCommandPoints(cpNeeded))
            {
                Debug.Log($"Not enough AP or CP to use {ability.abilityName}. Need {apNeeded} AP, have {heroAP} AP and {GameManager.Instance.CurrentCommandPoints} CP");
                return;
            }

            // Spend Command Points for the deficit
            GameManager.Instance.SpendCommandPoints(cpNeeded);
            Debug.Log($"Used {cpNeeded} Command Points to supplement AP for {ability.abilityName}");
        }

        // Execute the ability (untargeted abilities ignore the null target)
        if (ability is UntargetedAbility untargetedAbility)
        {
            untargetedAbility.Execute(_currentHero);
        }
        else
        {
            // For targeted abilities, we'd need to handle target selection differently
            Debug.LogWarning($"Ability {ability.abilityName} requires a target but was triggered from UI button!");
        }

        // Update UI to reflect new stats
        UpdateHeroStats();

        // Update all ability button states
        UpdateAllAbilityButtons();
    }

    /// <summary>
    /// Updates the interactability of an ability button based on AP and Command Points.
    /// </summary>
    private void UpdateAbilityButtonState(Button button, UnitAbility ability)
    {
        if (_currentHero == null || button == null) return;

        int apNeeded = ability.actionPointCost;
        int heroAP = _currentHero.CurrentActionPoints;
        int availableCP = GameManager.Instance != null ? GameManager.Instance.CurrentCommandPoints : 0;

        // Can afford if: hero AP covers it, OR hero AP + Command Points covers it
        bool canAfford = heroAP >= apNeeded || (heroAP + availableCP) >= apNeeded;
        button.interactable = canAfford;
    }

    /// <summary>
    /// Updates all ability buttons' interactability.
    /// Call this after AP changes.
    /// </summary>
    public void UpdateAllAbilityButtons()
    {
        if (_currentHero == null) return;

        List<UnitAbility> uiAbilities = _currentHero.unitAbilities.FindAll(a => !a.isContextual);

        for (int i = 0; i < _abilityButtons.Count && i < uiAbilities.Count; i++)
        {
            UpdateAbilityButtonState(_abilityButtons[i], uiAbilities[i]);
        }
    }

    /// <summary>
    /// Clears all ability buttons.
    /// </summary>
    private void ClearAbilityButtons()
    {
        foreach (Button button in _abilityButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        _abilityButtons.Clear();
    }

    /// <summary>
    /// Sets up the ability button container if not assigned.
    /// </summary>
    private void SetupAbilityButtonContainer()
    {
        if (abilityButtonContainer != null)
            return;

        // Find canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            // Create a canvas if none exists
            GameObject canvasObj = new GameObject("InGameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("InGameUIManager: Created new Canvas for UI elements.");
        }

        // Create ability button container in bottom-left
        GameObject containerObj = new GameObject("AbilityButtonContainer", typeof(RectTransform));
        containerObj.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = containerObj.GetComponent<RectTransform>();
        // Anchor to bottom-left
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(20, 20); // 20px from bottom-left
        containerRect.sizeDelta = new Vector2(400, 80);

        // Add horizontal layout group
        HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        abilityButtonContainer = containerObj.transform;
        Debug.Log("InGameUIManager: Created ability button container.");
    }

    /// <summary>
    /// Sets up the turn controls panel (Command Points + End Turn button).
    /// </summary>
    private void SetupEndTurnButton()
    {
        // Use assigned panel if provided, otherwise create programmatically
        if (turnControlsPanel != null)
        {
            _turnControlsPanel = turnControlsPanel;
        }
        else
        {
            CreateTurnControlsPanelProgrammatically();
        }

        // Use assigned button if provided
        if (endTurnButton != null)
        {
            _endTurnButton = endTurnButton;
            _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }

        // Use assigned text if provided
        if (commandPointsText != null)
        {
            _commandPointsText = commandPointsText;
        }

        // Update command points display
        UpdateCommandPointsDisplay();
    }

    /// <summary>
    /// Creates the turn controls panel programmatically in the bottom-right corner.
    /// </summary>
    private void CreateTurnControlsPanelProgrammatically()
    {
        // Find or create a canvas to attach to
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            Debug.LogWarning("InGameUIManager: No Canvas found. Cannot create turn controls panel.");
            return;
        }

        // Create panel container
        GameObject panelObj = new GameObject("TurnControlsPanel", typeof(RectTransform));
        panelObj.transform.SetParent(canvas.transform, false);
        _turnControlsPanel = panelObj;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        // Anchor to bottom-right
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20); // 20px from bottom-right
        panelRect.sizeDelta = new Vector2(150, 100);

        // Add background image
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Add vertical layout group
        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Create Command Points text
        GameObject cpTextObj = new GameObject("CommandPointsText", typeof(RectTransform));
        cpTextObj.transform.SetParent(panelObj.transform, false);

        RectTransform cpTextRect = cpTextObj.GetComponent<RectTransform>();
        cpTextRect.sizeDelta = new Vector2(130, 30);

        _commandPointsText = cpTextObj.AddComponent<TextMeshProUGUI>();
        _commandPointsText.fontSize = 16;
        _commandPointsText.color = Color.white;
        _commandPointsText.alignment = TextAlignmentOptions.Center;

        // Create End Turn button
        GameObject buttonObj = new GameObject("EndTurnButton", typeof(RectTransform));
        buttonObj.transform.SetParent(panelObj.transform, false);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(130, 40);

        _endTurnButton = buttonObj.AddComponent<Button>();

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);

        // Create button text
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "End Turn";
        tmpText.fontSize = 16;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;

        // Setup button colors
        ColorBlock colors = _endTurnButton.colors;
        colors.normalColor = new Color(0.6f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.7f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.5f, 0.15f, 0.15f, 1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        _endTurnButton.colors = colors;

        _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
    }

    /// <summary>
    /// Updates the Command Points display text.
    /// </summary>
    public void UpdateCommandPointsDisplay()
    {
        if (_commandPointsText != null && GameManager.Instance != null)
        {
            _commandPointsText.text = $"CP: {GameManager.Instance.CurrentCommandPoints}/{GameManager.Instance.MaxCommandPoints}";
        }
    }

    /// <summary>
    /// Called when the End Turn button is clicked.
    /// </summary>
    public void OnEndTurnButtonClicked()
    {
        GameManager.Instance.EndHeroTurn();
    }

    #region Unit Tooltip

    /// <summary>
    /// Sets up the unit tooltip panel.
    /// </summary>
    private void SetupTooltip()
    {
        if (tooltipPanel != null)
        {
            _tooltipPanel = tooltipPanel;
            _tooltipNameText = tooltipNameText;
            _tooltipHPText = tooltipHPText;
            _tooltipAPText = tooltipAPText;
            _tooltipPanel.SetActive(false);
        }
        else
        {
            CreateTooltipProgrammatically();
        }
    }

    /// <summary>
    /// Creates the tooltip panel programmatically.
    /// </summary>
    private void CreateTooltipProgrammatically()
    {
        // Find canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            Debug.LogWarning("InGameUIManager: No Canvas found. Cannot create tooltip.");
            return;
        }

        // Create tooltip panel
        GameObject panelObj = new GameObject("UnitTooltip", typeof(RectTransform));
        panelObj.transform.SetParent(canvas.transform, false);
        _tooltipPanel = panelObj;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(150, 80);
        // Pivot at bottom-left so tooltip appears above and to the right of cursor
        panelRect.pivot = new Vector2(0, 0);

        // Add background
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Add vertical layout group
        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 2;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Create name text
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform));
        nameObj.transform.SetParent(panelObj.transform, false);
        nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
        _tooltipNameText = nameObj.AddComponent<TextMeshProUGUI>();
        _tooltipNameText.fontSize = 16;
        _tooltipNameText.fontStyle = FontStyles.Bold;
        _tooltipNameText.color = Color.white;

        // Create HP text
        GameObject hpObj = new GameObject("HPText", typeof(RectTransform));
        hpObj.transform.SetParent(panelObj.transform, false);
        hpObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 18);
        _tooltipHPText = hpObj.AddComponent<TextMeshProUGUI>();
        _tooltipHPText.fontSize = 14;
        _tooltipHPText.color = new Color(0.9f, 0.3f, 0.3f, 1f); // Red-ish for HP

        // Create AP text
        GameObject apObj = new GameObject("APText", typeof(RectTransform));
        apObj.transform.SetParent(panelObj.transform, false);
        apObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 18);
        _tooltipAPText = apObj.AddComponent<TextMeshProUGUI>();
        _tooltipAPText.fontSize = 14;
        _tooltipAPText.color = new Color(0.3f, 0.7f, 0.9f, 1f); // Blue-ish for AP

        _tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the tooltip for a unit at the mouse position.
    /// </summary>
    public void ShowUnitTooltip(BaseUnit unit)
    {
        if (_tooltipPanel == null || unit == null)
            return;

        _tooltipTarget = unit;

        // Update tooltip content
        if (_tooltipNameText != null)
            _tooltipNameText.text = unit.UnitName;

        if (_tooltipHPText != null)
            _tooltipHPText.text = $"HP: {unit.CurrentHP}/{unit.maxHP}";

        if (_tooltipAPText != null)
            _tooltipAPText.text = $"AP: {unit.CurrentActionPoints}/{unit.MaxActionPoints}";

        _tooltipPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the unit tooltip.
    /// </summary>
    public void HideUnitTooltip()
    {
        if (_tooltipPanel != null)
        {
            _tooltipPanel.SetActive(false);
        }
        _tooltipTarget = null;
    }

    /// <summary>
    /// Clears all references to a unit that is being destroyed.
    /// Call this when a unit dies to prevent stale references.
    /// </summary>
    public void OnUnitDestroyed(BaseUnit unit)
    {
        if (_tooltipTarget == unit)
        {
            HideUnitTooltip();
        }
    }

    /// <summary>
    /// Refreshes the tooltip if it's currently showing for the given unit.
    /// Call this when unit stats change (e.g., after taking damage).
    /// </summary>
    public void RefreshTooltipIfShowing(BaseUnit unit)
    {
        if (_tooltipTarget == unit && _tooltipPanel != null && _tooltipPanel.activeSelf)
        {
            ShowUnitTooltip(unit);
        }
    }

    /// <summary>
    /// Refreshes all unit info displays (selected hero stats and hovered tooltip).
    /// Call this when any unit's stats change.
    /// </summary>
    public void RefreshUnitInfo(BaseUnit unit)
    {
        // Skip if unit is null or destroyed
        if (unit == null)
            return;

        // Refresh selected hero stats if this unit is the selected hero
        if (_currentHero != null && _currentHero == unit)
        {
            UpdateHeroStats();
            UpdateAllAbilityButtons();
        }

        // Refresh tooltip if showing this unit
        RefreshTooltipIfShowing(unit);
    }

    #endregion

    #region Victory/Defeat Screens

    /// <summary>
    /// Shows the victory screen with Restart and Exit buttons.
    /// </summary>
    public void ShowVictoryScreen()
    {
        HideGameplayUI();

        if (victoryPanel != null)
        {
            _victoryPanel = victoryPanel;
            WireUpPanelButtons(_victoryPanel);
            _victoryPanel.SetActive(true);
        }
        else
        {
            _victoryPanel = CreateGameEndPanel("VICTORY!", new Color(0.1f, 0.4f, 0.1f, 0.95f));
        }
    }

    /// <summary>
    /// Shows the defeat screen with Restart and Exit buttons.
    /// </summary>
    public void ShowDefeatScreen()
    {
        HideGameplayUI();

        if (defeatPanel != null)
        {
            _defeatPanel = defeatPanel;
            WireUpPanelButtons(_defeatPanel);
            _defeatPanel.SetActive(true);
        }
        else
        {
            _defeatPanel = CreateGameEndPanel("DEFEAT", new Color(0.5f, 0.1f, 0.1f, 0.95f));
        }
    }

    /// <summary>
    /// Finds and wires up Restart and Exit buttons in a custom panel.
    /// Searches for buttons by common naming conventions.
    /// </summary>
    private void WireUpPanelButtons(GameObject panel)
    {
        // Find all buttons in the panel
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            string buttonName = button.gameObject.name.ToLower();

            // Wire up Restart button
            if (buttonName.Contains("restart") || buttonName.Contains("retry") || buttonName.Contains("again"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnRestartButtonClicked);
            }
            // Wire up Exit button
            else if (buttonName.Contains("exit") || buttonName.Contains("quit") || buttonName.Contains("menu") || buttonName.Contains("leave"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnExitButtonClicked);
            }
        }
    }

    /// <summary>
    /// Hides all gameplay UI elements (stats, abilities, turn controls).
    /// </summary>
    private void HideGameplayUI()
    {
        HideHeroUI();

        if (_turnControlsPanel != null)
        {
            _turnControlsPanel.SetActive(false);
        }
        if (turnControlsPanel != null)
        {
            turnControlsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Creates a game end panel (victory or defeat) programmatically.
    /// </summary>
    private GameObject CreateGameEndPanel(string title, Color backgroundColor)
    {
        // Find canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            Debug.LogError("InGameUIManager: No Canvas found. Cannot create game end panel.");
            return null;
        }

        // Create full-screen overlay panel
        GameObject panelObj = new GameObject($"{title}Panel", typeof(RectTransform));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // Add semi-transparent background
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = backgroundColor;

        // Create center container for content
        GameObject contentContainer = new GameObject("Content", typeof(RectTransform));
        contentContainer.transform.SetParent(panelObj.transform, false);

        RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(400, 300);

        // Add vertical layout group
        VerticalLayoutGroup layout = contentContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Create title text
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(contentContainer.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(400, 80);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 64;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        // Create button container
        GameObject buttonContainer = new GameObject("Buttons", typeof(RectTransform));
        buttonContainer.transform.SetParent(contentContainer.transform, false);

        RectTransform buttonContainerRect = buttonContainer.GetComponent<RectTransform>();
        buttonContainerRect.sizeDelta = new Vector2(400, 60);

        HorizontalLayoutGroup buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 20;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = false;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        // Create Restart button
        CreateGameEndButton(buttonContainer.transform, "Restart", new Color(0.2f, 0.4f, 0.6f, 1f), OnRestartButtonClicked);

        // Create Exit button
        CreateGameEndButton(buttonContainer.transform, "Exit", new Color(0.5f, 0.3f, 0.3f, 1f), OnExitButtonClicked);

        return panelObj;
    }

    /// <summary>
    /// Creates a button for the game end panel.
    /// </summary>
    private void CreateGameEndButton(Transform parent, string buttonText, Color buttonColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject($"{buttonText}Button", typeof(RectTransform));
        buttonObj.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(150, 50);

        Button button = buttonObj.AddComponent<Button>();

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = buttonColor;

        // Create button text
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = buttonText;
        tmpText.fontSize = 24;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;

        // Setup button colors
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor * 1.2f;
        colors.pressedColor = buttonColor * 0.8f;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        button.colors = colors;

        button.onClick.AddListener(onClick);
    }

    /// <summary>
    /// Called when the Restart button is clicked. Reloads the current scene.
    /// </summary>
    private void OnRestartButtonClicked()
    {
        // Store the current level so it persists through scene reload
        if (GameManager.Instance != null && GameManager.Instance.CurrentLevel != null)
        {
            LevelLoader.SelectedLevel = GameManager.Instance.CurrentLevel;
        }

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Called when the Exit button is clicked. Returns to the main menu.
    /// </summary>
    private void OnExitButtonClicked()
    {
        // Clear selected level
        LevelLoader.SelectedLevel = null;

        // Try to load main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // Fallback: quit application or load scene index 0
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }

    #endregion
}
