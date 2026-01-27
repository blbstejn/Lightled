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

    [Header("Level Selection")]
    [SerializeField] private LevelData[] availableLevels;
    [SerializeField] private Transform levelButtonContainer; // Parent for dynamically created buttons (e.g., Vertical Layout Group)

    [Header("Scene Management")]
    [SerializeField] private string gameplaySceneName = "TestScene";

    private LevelData _selectedLevel;

    void Start()
    {
        // Ensure all panels start in correct state
        HideAllPanels();
        ShowMainMenu();
        GenerateLevelButtons();
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
    /// Shows the info panel (game rules, design, lore).
    /// </summary>
    public void ShowInfo()
    {
        SetActivePanel(infoPanel);
    }

    private void SetActivePanel(GameObject panelToShow)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panelToShow == mainMenuPanel);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(panelToShow == levelSelectPanel);
        if (optionsPanel != null) optionsPanel.SetActive(panelToShow == optionsPanel);
        if (infoPanel != null) infoPanel.SetActive(panelToShow == infoPanel);
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
        float buttonWidth = 300f;
        float buttonHeight = 50f;
        float buttonSpacing = 10f;

        // Create a button for each level
        for (int i = 0; i < availableLevels.Length; i++)
        {
            LevelData level = availableLevels[i];
            if (level == null) continue;

            // Create button GameObject
            GameObject buttonObj = new GameObject($"LevelButton_{i}_{level.levelName}", typeof(RectTransform));
            buttonObj.transform.SetParent(levelButtonContainer, false);

            // Configure RectTransform - position each button manually
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rectTransform.anchorMin = new Vector2(0.5f, 1f); // Anchor to top-center
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f); // Pivot at top-center
            rectTransform.anchoredPosition = new Vector2(0, -i * (buttonHeight + buttonSpacing)); // Stack vertically

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();

            // Add Image component for button visual
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray background

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
            tmpText.fontSize = 24;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;

            // Setup button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            button.colors = colors;

            // Assign click listener
            LevelData capturedLevel = level;
            button.onClick.AddListener(() => OnLevelSelected(capturedLevel));
        }
    }

    /// <summary>
    /// Called when a level button is clicked.
    /// </summary>
    private void OnLevelSelected(LevelData level)
    {
        _selectedLevel = level;
        LoadGameplayScene();
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
