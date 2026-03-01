using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the main menu UI navigation and level loading.
/// Handles transitions between menu panels (Main, Level Select, Options, Info).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject optionsPanel;
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
    [Tooltip("Parent transform for the generated hero list buttons.")]
    [SerializeField] private Transform heroButtonContainer;
    [Tooltip("Parent transform for the generated enemy list buttons.")]
    [SerializeField] private Transform enemyButtonContainer;
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
        if (optionsPanel != null) optionsPanel.SetActive(false);
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
    /// Shows the options panel.
    /// </summary>
    public void ShowOptions()
    {
        SetActivePanel(optionsPanel);
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
        if (optionsPanel != null) optionsPanel.SetActive(panelToShow == optionsPanel);
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

        // Button layout settings
        float buttonWidth = 200f;
        float buttonHeight = 40f;
        float buttonSpacing = 5f;

        // Create a button for each level
        for (int i = 0; i < availableLevels.Length; i++)
        {
            LevelData level = availableLevels[i];
            if (level == null) continue;

            // Create button GameObject
            GameObject buttonObj = new GameObject($"LevelButton_{i}_{level.levelName}", typeof(RectTransform));
            buttonObj.transform.SetParent(levelButtonContainer, false);

            // Configure RectTransform - anchor to top-left
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchorMin = new Vector2(0f, 1f); // Anchor to top-left
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f); // Pivot at top-left
            rectTransform.anchoredPosition = new Vector2(10, -i * (buttonHeight + buttonSpacing)); // Stack vertically with left padding

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();

            // Add Image component for button visual (Unity default button style)
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = Color.white; // Standard Unity button base color

            // Create TextMeshPro text child
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero; // Fill parent
            textRect.offsetMin = new Vector2(10, 5); // Padding
            textRect.offsetMax = new Vector2(-10, -5);

            // Add TextMeshProUGUI component
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = level.levelName;
            tmpText.fontSize = 20;
            tmpText.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark text on light button
            tmpText.alignment = TextAlignmentOptions.Left;

            // Setup button colors (Unity default style)
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);        // White
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f); // Slightly darker on hover
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);     // Darker when pressed
            colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            button.colors = colors;

            // Assign click listener
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

    #region Unit Roster

    /// <summary>
    /// Generates hero buttons and enemy buttons into their respective containers.
    /// Data (name, title, portrait, description) is read directly from each unit prefab.
    /// </summary>
    private void GenerateUnitButtons()
    {
        if (loreData == null) return;

        if (loreData.heroes != null)
            PopulateUnitButtonContainer(heroButtonContainer, loreData.heroes);

        if (loreData.enemies != null)
            PopulateUnitButtonContainer(enemyButtonContainer, loreData.enemies);
    }

    private void PopulateUnitButtonContainer(Transform container, BaseUnit[] units)
    {
        if (container == null) return;

        // Clear previously generated buttons
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child.name.StartsWith("UnitButton_"))
                Destroy(child.gameObject);
        }

        for (int i = 0; i < units.Length; i++)
        {
            BaseUnit unit = units[i];
            if (unit == null) continue;

            // Button root — Vertical Layout Group handles all positioning
            GameObject buttonObj = new GameObject($"UnitButton_{i}_{unit.UnitName}", typeof(RectTransform));
            buttonObj.transform.SetParent(container, false);

            // Fixed height; width is controlled by the VLG's child width setting
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 40f);

            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = Color.white;

            // Label
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 4);
            textRect.offsetMax = new Vector2(-10, -4);

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = unit.UnitName;
            tmpText.fontSize = 18;
            tmpText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            tmpText.alignment = TextAlignmentOptions.Left;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            button.colors = colors;

            BaseUnit capturedUnit = unit;
            Button capturedButton = button;
            button.onClick.AddListener(() => OnUnitSelected(capturedUnit, capturedButton));
        }
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
    /// Called by the Options button.
    /// </summary>
    public void OnOptionsButtonClicked()
    {
        ShowOptions();
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
