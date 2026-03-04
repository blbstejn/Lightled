using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the main menu UI navigation and level loading.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject lorePanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Level Selection")]
    [SerializeField] private LevelData[] availableLevels;
    [SerializeField] private Transform levelButtonContainer; // Parent for dynamically created buttons

    [Header("Level Details Panel (Optional - assign for two-step selection)")]
    [Tooltip("Text component to display level name when selected")]
    [SerializeField] private TextMeshProUGUI levelNameText;
    [Tooltip("Text component to display level description when selected")]
    [SerializeField] private TextMeshProUGUI levelDescriptionText;
    [Tooltip("Button to confirm and start the selected level")]
    [SerializeField] private Button confirmLevelButton;
    [Tooltip("Parent panel for level details (hidden until a level is selected)")]
    [SerializeField] private GameObject levelDetailsPanel;

    [Header("Unit Roster (Lore Panel)")]
    [Tooltip("Assign the LoreData asset to populate the unit lists.")]
    [SerializeField] private LoreData loreData;
    [Tooltip("Text component in the lore panel that displays the overall story.")]
    [SerializeField] private TMPro.TextMeshProUGUI loreStoryText;
    [Tooltip("Parent transform for the generated unit list buttons (heroes and enemies combined).")]
    [SerializeField] private Transform unitButtonContainer;
    [Tooltip("Panel to show/hide when a unit is selected. Hide it by default in the canvas.")]
    [SerializeField] private GameObject unitDetailPanel;
    [Tooltip("Image component to display the selected unit's sprite.")]
    [SerializeField] private UnityEngine.UI.Image unitPortraitImage;
    [Tooltip("Text component for the selected unit's name.")]
    [SerializeField] private TMPro.TextMeshProUGUI unitNameText;
    [Tooltip("Text component for the selected unit's title (role/rank).")]
    [SerializeField] private TMPro.TextMeshProUGUI unitTitleText;
    [Tooltip("Text component for the selected unit's lore description.")]
    [SerializeField] private TMPro.TextMeshProUGUI unitDescriptionText;

    [Header("Scene Management")]
    [SerializeField] private string gameplaySceneName = "TestScene";

    private LevelData _selectedLevel;
    private Button _currentlySelectedButton;
    private Button _currentlySelectedUnitButton;

    void Start()
    {
        // Ensure all panels start in correct state
        HideAllPanels();
        ShowMainMenu();
        GenerateLevelButtons();
        SetupLevelDetails();
        GenerateUnitButtons();
        if (unitDetailPanel != null) unitDetailPanel.SetActive(false);
        if (loreStoryText != null && loreData != null) loreStoryText.text = loreData.overallStory;
    }

    /// <summary>
    /// Sets up the level details panel and confirm button.
    /// </summary>
    private void SetupLevelDetails()
    {
        // Hide level details panel until a level is selected
        if (levelDetailsPanel != null)
        {
            levelDetailsPanel.SetActive(false);
        }

        // Wire up confirm button
        if (confirmLevelButton != null)
        {
            confirmLevelButton.onClick.AddListener(OnConfirmLevelClicked);
            confirmLevelButton.interactable = false; // Disabled until level selected
        }
    }

    /// <summary>
    /// Hides all menu panels.
    /// </summary>
    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (lorePanel != null) lorePanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    #region Panel Navigation

    /// <summary>
    /// Shows the main menu panel and hides all others.
    /// </summary>
    public void ShowMainMenu()
    {
        SetActivePanel(mainMenuPanel);
    }

    /// <summary>
    /// Shows the level selection panel.
    /// </summary>
    public void ShowLevelSelect()
    {
        SetActivePanel(levelSelectPanel);
    }

    /// <summary>
    /// Shows the info panel (hub for Lore and How To Play).
    /// </summary>
    public void ShowInfo()
    {
        SetActivePanel(infoPanel);
    }

    /// <summary>
    /// Shows the lore panel.
    /// </summary>
    public void ShowLore()
    {
        SetActivePanel(lorePanel);
    }

    /// <summary>
    /// Shows the how to play panel.
    /// </summary>
    public void ShowHowToPlay()
    {
        SetActivePanel(howToPlayPanel);
    }

    /// <summary>
    /// Shows the credits panel.
    /// </summary>
    public void ShowCredits()
    {
        SetActivePanel(creditsPanel);
    }

    private void SetActivePanel(GameObject panelToShow)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panelToShow == mainMenuPanel);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(panelToShow == levelSelectPanel);
        if (infoPanel != null) infoPanel.SetActive(panelToShow == infoPanel);
        if (lorePanel != null) lorePanel.SetActive(panelToShow == lorePanel);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(panelToShow == howToPlayPanel);
        if (creditsPanel != null) creditsPanel.SetActive(panelToShow == creditsPanel);
    }

    #endregion

    #region Level Selection

    /// <summary>
    /// Dynamically creates buttons for each level using Unity's built-in UI system.
    /// Creates TextMeshPro buttons programmatically - no prefab needed!
    /// </summary>
    private void GenerateLevelButtons()
    {
        if (levelButtonContainer == null)
        {
            Debug.LogWarning("MainMenuManager: Level button container not assigned!");
            return;
        }

        if (availableLevels == null || availableLevels.Length == 0)
        {
            Debug.LogWarning("MainMenuManager: No levels assigned!");
            return;
        }

        // Clear any existing dynamically created level buttons (preserve manually placed UI like Back button)
        for (int i = levelButtonContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = levelButtonContainer.GetChild(i);
            if (child.name.StartsWith("LevelButton_"))
            {
                Destroy(child.gameObject);
            }
        }

        float buttonWidth = 200f;
        float buttonHeight = 45f;
        float buttonSpacing = 4f;

        for (int i = 0; i < availableLevels.Length; i++)
        {
            LevelData level = availableLevels[i];
            if (level == null) continue;

            GameObject buttonObj = new GameObject($"LevelButton_{i}_{level.levelName}", typeof(RectTransform));
            buttonObj.transform.SetParent(levelButtonContainer, false);

            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(10, -i * (buttonHeight + buttonSpacing));

            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            StyleButton(button, buttonImage);

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = level.levelName;
            tmpText.fontSize = 20;
            tmpText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            tmpText.alignment = TextAlignmentOptions.Left;

            LevelData capturedLevel = level;
            Button capturedButton = button;
            button.onClick.AddListener(() => OnLevelSelected(capturedLevel, capturedButton));
        }
    }

    /// <summary>
    /// Called when a level button is clicked.
    /// Shows level details if panel is assigned, otherwise loads directly.
    /// </summary>
    private void OnLevelSelected(LevelData level, Button clickedButton = null)
    {
        _selectedLevel = level;

        // Update visual selection state
        if (_currentlySelectedButton != null)
        {
            // Reset previous button to normal white
            var prevColors = _currentlySelectedButton.colors;
            prevColors.normalColor = Color.white;
            _currentlySelectedButton.colors = prevColors;
        }

        _currentlySelectedButton = clickedButton;
        if (_currentlySelectedButton != null)
        {
            // Highlight selected button with light blue tint
            var colors = _currentlySelectedButton.colors;
            colors.normalColor = new Color(0.85f, 0.92f, 1f, 1f); // Light blue tint
            _currentlySelectedButton.colors = colors;
        }

        // If level details panel exists, show details and wait for confirm
        if (levelDetailsPanel != null)
        {
            ShowLevelDetails(level);
        }
        else
        {
            // No details panel - load directly (old behavior)
            LoadGameplayScene();
        }
    }

    /// <summary>
    /// Shows the level details in the details panel.
    /// </summary>
    private void ShowLevelDetails(LevelData level)
    {
        Debug.Log($"[MainMenu] ShowLevelDetails called for: {level.levelName}");
        Debug.Log($"[MainMenu] levelDetailsPanel assigned: {levelDetailsPanel != null}");
        Debug.Log($"[MainMenu] levelNameText assigned: {levelNameText != null}");
        Debug.Log($"[MainMenu] levelDescriptionText assigned: {levelDescriptionText != null}");

        if (levelDetailsPanel != null)
        {
            levelDetailsPanel.SetActive(true);
            Debug.Log($"[MainMenu] Panel activated");
        }
        else
        {
            Debug.LogWarning("[MainMenu] levelDetailsPanel is NOT assigned in Inspector!");
        }

        if (levelNameText != null)
        {
            levelNameText.text = level.levelName;
            Debug.Log($"[MainMenu] Set name to: {level.levelName}");
        }
        else
        {
            Debug.LogWarning("[MainMenu] levelNameText is NOT assigned in Inspector!");
        }

        if (levelDescriptionText != null)
        {
            levelDescriptionText.text = level.description;
            Debug.Log($"[MainMenu] Set description to: {level.description}");
        }
        else
        {
            Debug.LogWarning("[MainMenu] levelDescriptionText is NOT assigned in Inspector!");
        }

        if (confirmLevelButton != null)
        {
            confirmLevelButton.interactable = true;
        }
    }

    /// <summary>
    /// Called when the confirm button is clicked to start the selected level.
    /// </summary>
    public void OnConfirmLevelClicked()
    {
        if (_selectedLevel != null)
        {
            LoadGameplayScene();
        }
    }

    /// <summary>
    /// Loads the gameplay scene with the selected level.
    /// </summary>
    private void LoadGameplayScene()
    {
        if (_selectedLevel == null)
        {
            Debug.LogError("MainMenuManager: No level selected!");
            return;
        }

        // Store selected level for GameManager to use
        LevelLoader.SelectedLevel = _selectedLevel;

        // Load the gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }

    #endregion

    /// <summary>
    /// Applies the standard grey button appearance (Unity UISprite + colour block).
    /// </summary>
    private void StyleButton(Button button, Image buttonImage)
    {
        Sprite uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        buttonImage.sprite = uiSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = new Color(0.784f, 0.784f, 0.784f, 1f); // Standard Unity grey

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = new Color(0.88f, 0.92f, 1f, 1f);
        button.colors = colors;
    }

    #region Unit Roster

    private void GenerateUnitButtons()
    {
        if (unitButtonContainer == null)
        {
            Debug.LogWarning("MainMenuManager: unitButtonContainer not assigned!");
            return;
        }

        if (loreData == null)
        {
            Debug.LogWarning("MainMenuManager: LoreData not assigned!");
            return;
        }

        for (int i = unitButtonContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = unitButtonContainer.GetChild(i);
            if (child.name.StartsWith("UnitButton_"))
                Destroy(child.gameObject);
        }

        float buttonWidth = 200f;
        float buttonHeight = 45f;
        float buttonSpacing = 4f;

        if (loreData.units == null || loreData.units.Length == 0)
        {
            Debug.LogWarning("MainMenuManager: No units assigned in LoreData!");
            return;
        }

        for (int i = 0; i < loreData.units.Length; i++)
        {
            if (loreData.units[i] == null) continue;
            CreateUnitButton(loreData.units[i], i, buttonWidth, buttonHeight, buttonSpacing);
        }
    }

    private void CreateUnitButton(BaseUnit unit, int index, float buttonWidth, float buttonHeight, float buttonSpacing)
    {
        GameObject buttonObj = new GameObject($"UnitButton_{index}_{unit.UnitName}", typeof(RectTransform));
        buttonObj.transform.SetParent(unitButtonContainer, false);

        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(10, -index * (buttonHeight + buttonSpacing));

        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        StyleButton(button, buttonImage);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = unit.UnitName;
        tmpText.fontSize = 20;
        tmpText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        tmpText.alignment = TextAlignmentOptions.Left;

        BaseUnit capturedUnit = unit;
        Button capturedButton = button;
        button.onClick.AddListener(() => OnUnitSelected(capturedUnit, capturedButton));
    }

    /// <summary>
    /// Called when a unit button is clicked.
    /// Reads portrait, name, title, and description directly from the unit prefab.
    /// </summary>
    private void OnUnitSelected(BaseUnit unit, Button clickedButton)
    {
        // Update button highlight
        if (_currentlySelectedUnitButton != null)
        {
            var prevColors = _currentlySelectedUnitButton.colors;
            prevColors.normalColor = Color.white;
            _currentlySelectedUnitButton.colors = prevColors;
        }

        _currentlySelectedUnitButton = clickedButton;
        if (_currentlySelectedUnitButton != null)
        {
            var colors = _currentlySelectedUnitButton.colors;
            colors.normalColor = new Color(0.85f, 0.92f, 1f, 1f);
            _currentlySelectedUnitButton.colors = colors;
        }

        // Show detail panel
        if (unitDetailPanel != null) unitDetailPanel.SetActive(true);

        // Portrait — read sprite directly from the unit's SpriteRenderer
        if (unitPortraitImage != null)
        {
            Sprite unitSprite = unit.GetComponentInChildren<SpriteRenderer>()?.sprite;
            unitPortraitImage.sprite = unitSprite;
            unitPortraitImage.gameObject.SetActive(unitSprite != null);
        }

        if (unitNameText != null) unitNameText.text = unit.UnitName;
        if (unitTitleText != null) unitTitleText.text = unit.title;
        if (unitDescriptionText != null) unitDescriptionText.text = unit.description;
    }

    #endregion

    #region Button Callbacks

    /// <summary>
    /// Called by the Play button - goes to level select.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        ShowLevelSelect();
    }

    /// <summary>
    /// Called by the Info button (game rules, design, lore).
    /// </summary>
    public void OnInfoButtonClicked()
    {
        ShowInfo();
    }

    /// <summary>
    /// Called by the Lore button inside the info panel.
    /// </summary>
    public void OnLoreButtonClicked()
    {
        ShowLore();
    }

    /// <summary>
    /// Called by the How To Play button inside the info panel.
    /// </summary>
    public void OnHowToPlayButtonClicked()
    {
        ShowHowToPlay();
    }

    /// <summary>
    /// Called by Back buttons inside Lore or How To Play panels — returns to the info hub.
    /// </summary>
    public void OnInfoSubPanelBackButtonClicked()
    {
        ShowInfo();
    }

    /// <summary>
    /// Called by the Credits button.
    /// </summary>
    public void OnCreditsButtonClicked()
    {
        ShowCredits();
    }

    /// <summary>
    /// Called by the Exit/Quit button.
    /// </summary>
    public void OnExitButtonClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Called by Back buttons to return to main menu.
    /// </summary>
    public void OnBackButtonClicked()
    {
        ShowMainMenu();
    }

    #endregion
}

/// <summary>
/// Static helper class to pass selected level data between scenes.
/// </summary>
public static class LevelLoader
{
    public static LevelData SelectedLevel { get; set; }
}
