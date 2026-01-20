using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the pause menu during gameplay.
/// Handles ESC key to pause/unpause and menu navigation.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance;

    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject pauseButton; // Optional top-left pause button

    [Header("Scene Management")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool _isPaused = false;

    public bool IsPaused => _isPaused;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Ensure pause menu is hidden at start
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Ensure game is unpaused
        Time.timeScale = 1f;
        _isPaused = false;
    }

    void Update()
    {
        // Toggle pause menu with ESC key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    /// <summary>
    /// Pauses the game and shows the pause menu.
    /// </summary>
    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f; // Freeze game time

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        // Hide pause button when menu is open
        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }
    }

    /// <summary>
    /// Resumes the game and hides the pause menu.
    /// </summary>
    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f; // Unfreeze game time

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Show pause button when menu is closed
        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }
    }

    /// <summary>
    /// Called by Resume button in pause menu.
    /// </summary>
    public void OnResumeButtonClicked()
    {
        ResumeGame();
    }

    /// <summary>
    /// Restarts the current level.
    /// </summary>
    public void OnRestartButtonClicked()
    {
        Time.timeScale = 1f; // Ensure time is unpaused
        _isPaused = false;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Returns to main menu.
    /// </summary>
    public void OnMainMenuButtonClicked()
    {
        Time.timeScale = 1f; // Ensure time is unpaused
        _isPaused = false;

        // Clear level selection
        LevelLoader.SelectedLevel = null;

        // Load main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Quits the game (or exits play mode in editor).
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Time.timeScale = 1f; // Ensure time is unpaused

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
