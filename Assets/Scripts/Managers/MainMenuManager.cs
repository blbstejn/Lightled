using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the main menu UI navigation and level loading.
/// Handles transitions between menu panels (Main, Level Select, Options, Lore).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject lorePanel;

    [Header("Level Selection")]
    [SerializeField] private LevelData[] availableLevels;
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private GameObject levelButtonPrefab;

    [Header("Scene Management")]
    [SerializeField] private string gameplaySceneName = "TestScene";

    private LevelData _selectedLevel;

    void Start()
    {
        ShowMainMenu();
        GenerateLevelButtons();
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
    /// Shows the lore panel.
    /// </summary>
    public void ShowLore()
    {
        SetActivePanel(lorePanel);
    }

    private void SetActivePanel(GameObject panelToShow)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panelToShow == mainMenuPanel);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(panelToShow == levelSelectPanel);
        if (optionsPanel != null) optionsPanel.SetActive(panelToShow == optionsPanel);
        if (lorePanel != null) lorePanel.SetActive(panelToShow == lorePanel);
    }

    #endregion

    #region Level Selection

    /// <summary>
    /// Generates a button for each available level in the level select panel.
    /// </summary>
    private void GenerateLevelButtons()
    {
        if (levelButtonContainer == null || levelButtonPrefab == null)
        {
            Debug.LogWarning("MainMenuManager: Level button container or prefab not assigned!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Create button for each level
        for (int i = 0; i < availableLevels.Length; i++)
        {
            LevelData level = availableLevels[i];
            if (level == null) continue;

            GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = level.levelName;
            }

            // Capture level in closure for button callback
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
    /// Called by the Lore button.
    /// </summary>
    public void OnLoreButtonClicked()
    {
        ShowLore();
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
