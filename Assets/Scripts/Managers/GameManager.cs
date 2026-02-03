using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState GameState;
    public LevelData CurrentLevel { get; private set; }

    [Header("Command Points")]
    [SerializeField] private int maxCommandPoints = 6;
    public int MaxCommandPoints => maxCommandPoints;
    public int CurrentCommandPoints { get; private set; }

    void Awake()
    {
        Instance = this;
        CurrentCommandPoints = maxCommandPoints;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check if a level was selected from the main menu
        if (LevelLoader.SelectedLevel != null)
        {
            CurrentLevel = LevelLoader.SelectedLevel;
            GridManager.Instance.SetLevel(CurrentLevel);
            LevelLoader.SelectedLevel = null; // Clear after loading
        }

        ChangeState(GameState.GenerateGrid);
    }

    public void ChangeState(GameState newState)
    {
        GameState = newState;
        switch (newState)
        {
            case GameState.GenerateGrid:
                GridManager.Instance.GenerateGrid();
                break;
            case GameState.SpawnHeroes:
                UnitManager.Instance.SpawnHeroes();
                break;
            case GameState.SpawnEnemies:
                UnitManager.Instance.SpawnEnemies();
                break;
            case GameState.HeroesTurn:
                StartHeroTurn();
                break;
            case GameState.EnemiesTurn:
                StartEnemyTurn();
                break;
            case GameState.Victory:
                OnVictory();
                break;
            case GameState.Defeat:
                OnDefeat();
                break;
        }
    }

    /// <summary>
    /// Called when the hero turn begins. Refreshes AP for all heroes and resets CP.
    /// </summary>
    private void StartHeroTurn()
    {
        // Reset command points
        CurrentCommandPoints = maxCommandPoints;

        // Refresh AP for all heroes
        foreach (var hero in UnitManager.Instance.SpawnedHeroes)
        {
            hero.StartNewTurn();
        }

        Debug.Log($"Hero turn started - CP reset to {CurrentCommandPoints}, AP refreshed for all heroes.");

        // Check if any blips are visible at the start of the turn
        // This catches blips that might be in LOS at game start or after enemy movement
        Debug.Log("[Game Start] Checking blips for reveal at turn start...");
        UnitManager.Instance.CheckAllBlipsForReveal();

        // Start the turn timer
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.StartTurnTimer();
        }
    }

    /// <summary>
    /// Called when the enemy turn begins.
    /// Delegates to EnemyAIManager to process all enemy actions.
    /// </summary>
    private void StartEnemyTurn()
    {
        Debug.Log("Enemy turn started.");

        // Delegate to AI manager for enemy turn processing
        if (EnemyAIManager.Instance != null)
        {
            EnemyAIManager.Instance.ExecuteEnemyTurn();
        }
        else
        {
            // Fallback if no AI manager exists
            Debug.LogWarning("EnemyAIManager not found! Skipping enemy turn.");
            OnEnemyTurnComplete();
        }
    }

    /// <summary>
    /// Called by EnemyAIManager when the enemy turn is complete.
    /// Checks victory/defeat conditions and transitions to hero turn.
    /// </summary>
    public void OnEnemyTurnComplete()
    {
        Debug.Log("Enemy turn complete.");

        // Check defeat condition first (all heroes dead)
        CheckDefeatCondition();
        if (GameState == GameState.Defeat)
            return;

        // Check victory condition (hero on target tile survived)
        CheckVictoryCondition();
        if (GameState == GameState.Victory)
            return;

        // Continue to next hero turn
        ChangeState(GameState.HeroesTurn);
    }

    /// <summary>
    /// Ends the current hero turn and switches to enemy turn.
    /// </summary>
    public void EndHeroTurn()
    {
        if (GameState != GameState.HeroesTurn)
            return;

        // Stop the turn timer
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.StopTurnTimer();
        }

        // Deselect hero and clear highlights
        UnitManager.Instance.SetSelectedHero(null);
        GridManager.Instance.ClearMoveHighlights();
        GridManager.Instance.ClearLineOfSight();

        Debug.Log("Hero turn ended.");
        ChangeState(GameState.EnemiesTurn);
    }

    /// <summary>
    /// Called when a hero reaches a target tile (with or without artifact).
    /// The win is pending - hero must survive the following enemy turn.
    /// </summary>
    public void OnHeroReachedTarget(BaseUnit hero)
    {
        bool requiresArtifact = CurrentLevel != null && CurrentLevel.requiresArtifact;

        if (requiresArtifact && hero.HasArtifact)
        {
            Debug.Log($"{hero.UnitName} has reached the target with the artifact! Survive the enemy turn to win!");
        }
        else if (!requiresArtifact)
        {
            Debug.Log($"{hero.UnitName} has reached the target! Survive the enemy turn to win!");
        }
    }

    /// <summary>
    /// Called at the end of the enemy turn to check if the win condition is met.
    /// If level requires artifact: hero with artifact must be alive on target tile.
    /// If level doesn't require artifact: any hero on target tile wins.
    /// </summary>
    public void CheckVictoryCondition()
    {
        bool requiresArtifact = CurrentLevel != null && CurrentLevel.requiresArtifact;

        foreach (var hero in UnitManager.Instance.SpawnedHeroes)
        {
            if (!hero.IsAlive || !(hero.OccupiedTile is TargetTile))
                continue;

            // Check if victory conditions are met
            if (requiresArtifact)
            {
                if (hero.HasArtifact)
                {
                    ChangeState(GameState.Victory);
                    return;
                }
            }
            else
            {
                // No artifact required - just reaching target is enough
                ChangeState(GameState.Victory);
                return;
            }
        }
    }

    /// <summary>
    /// Called when a hero dies. Triggers defeat if all heroes are dead.
    /// </summary>
    public void CheckDefeatCondition()
    {
        // Check if any heroes are still alive
        if (UnitManager.Instance.SpawnedHeroes.Count == 0)
        {
            ChangeState(GameState.Defeat);
        }
    }

    private void OnVictory()
    {
        Debug.Log("VICTORY! The artifact has been secured!");

        // Show victory UI
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowVictoryScreen();
        }
    }

    private void OnDefeat()
    {
        Debug.Log("DEFEAT! All heroes have fallen.");

        // Show defeat UI
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowDefeatScreen();
        }
    }

    /// <summary>
    /// Spends command points. Returns true if successful.
    /// </summary>
    public bool SpendCommandPoints(int amount)
    {
        if (amount > CurrentCommandPoints)
            return false;

        CurrentCommandPoints -= amount;
        Debug.Log($"Spent {amount} Command Points. Remaining: {CurrentCommandPoints}/{maxCommandPoints}");
        return true;
    }

    /// <summary>
    /// Checks if the player can afford the given command point cost.
    /// </summary>
    public bool CanAffordCommandPoints(int amount)
    {
        return CurrentCommandPoints >= amount;
    }
}

public enum GameState
{
    GenerateGrid = 0,
    SpawnHeroes = 1,
    SpawnEnemies = 2,
    HeroesTurn = 3,
    EnemiesTurn = 4,
    Victory = 5,
    Defeat = 6,
}