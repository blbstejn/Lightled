using System.Collections.Generic;
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
        // Ensure OverwatchManager exists
        EnsureOverwatchManager();

        // Check if a level was selected from the main menu
        if (LevelLoader.SelectedLevel != null)
        {
            CurrentLevel = LevelLoader.SelectedLevel;
            GridManager.Instance.SetLevel(CurrentLevel);
            LevelLoader.SelectedLevel = null; // Clear after loading
        }

        // Reset escape tracking for fresh start
        UnitManager.Instance.ClearEscapedHeroes();

        ChangeState(GameState.GenerateGrid);
    }

    /// <summary>
    /// Creates OverwatchManager if it doesn't exist in the scene.
    /// </summary>
    private void EnsureOverwatchManager()
    {
        if (OverwatchManager.Instance == null)
        {
            var overwatchManagerObj = new GameObject("OverwatchManager");
            overwatchManagerObj.AddComponent<OverwatchManager>();
            Debug.Log("[GameManager] Created OverwatchManager");
        }
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

        // Recalculate Director pressure at turn start (picks up all hero-turn changes)
        DirectorAI.Instance?.RecalculatePressure();

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

        // Clear fire effects at end of enemy turn
        if (FireEffectManager.Instance != null)
        {
            FireEffectManager.Instance.ClearAllFire();
        }

        // Check victory first (for Escape mode, this also processes escapes)
        CheckVictoryCondition();
        if (GameState == GameState.Victory)
            return;

        // Check defeat condition (all heroes dead/gone without victory)
        CheckDefeatCondition();
        if (GameState == GameState.Defeat)
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
        if (CurrentLevel == null) return;

        switch (CurrentLevel.victoryConditionType)
        {
            case VictoryConditionType.Escape:
                Debug.Log($"{hero.UnitName} is on the escape zone! They will escape at the end of the enemy turn.");
                break;

            case VictoryConditionType.FlamethrowerTarget:
                break;
        }
    }

    /// <summary>
    /// Called at the end of the enemy turn to check if the win condition is met.
    /// Dispatches to the appropriate check based on LevelData.victoryConditionType.
    /// </summary>
    public void CheckVictoryCondition()
    {
        if (CurrentLevel == null)
        {
            CheckVictoryCondition_Escape();
            return;
        }

        switch (CurrentLevel.victoryConditionType)
        {
            case VictoryConditionType.Escape:
                CheckVictoryCondition_Escape();
                break;
            case VictoryConditionType.FlamethrowerTarget:
                // Checked at ability execution time via CheckFlamethrowerVictory()
                break;
        }
    }

    /// <summary>
    /// Escape mode: heroes on target tiles at end of enemy turn are removed from play.
    /// Victory when required escapee count is reached (or artifact carrier escapes if required).
    /// </summary>
    private void CheckVictoryCondition_Escape()
    {
        int requiredEscapees = CurrentLevel != null ? CurrentLevel.requiredEscapees : 1;

        // Find heroes currently on target tiles and escape them
        var heroesToEscape = new List<BaseHero>();
        foreach (var hero in UnitManager.Instance.SpawnedHeroes)
        {
            if (hero.IsAlive && hero.OccupiedTile is TargetTile)
                heroesToEscape.Add(hero);
        }

        // Process escapes (iterate copy since EscapeHero modifies SpawnedHeroes)
        foreach (var hero in heroesToEscape)
        {
            UnitManager.Instance.EscapeHero(hero);
        }

        // Update escape progress UI
        if (heroesToEscape.Count > 0 && InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.UpdateEscapeProgress(
                UnitManager.Instance.EscapedHeroCount, requiredEscapees);
        }

        // Check victory
        if (CurrentLevel != null && CurrentLevel.requiresArtifact)
        {
            // Artifact mode: victory only when artifact carrier has escaped
            if (UnitManager.Instance.ArtifactCarrierEscaped)
            {
                Debug.Log($"[Victory] Artifact carrier has escaped!");
                ChangeState(GameState.Victory);
            }
        }
        else
        {
            // Standard escape: need minimum number of escapees
            if (UnitManager.Instance.EscapedHeroCount >= requiredEscapees)
            {
                Debug.Log($"[Victory] {UnitManager.Instance.EscapedHeroCount}/{requiredEscapees} heroes escaped!");
                ChangeState(GameState.Victory);
            }
        }
    }

    /// <summary>
    /// Called by FlamethrowerAbility when it fires. If the level uses FlamethrowerTarget
    /// victory type and any affected tile is a TargetTile, trigger instant victory.
    /// </summary>
    public void CheckFlamethrowerVictory(HashSet<Tile> affectedTiles)
    {
        if (CurrentLevel == null || CurrentLevel.victoryConditionType != VictoryConditionType.FlamethrowerTarget)
            return;

        foreach (var tile in affectedTiles)
        {
            if (tile is TargetTile)
            {
                Debug.Log($"[Victory] Flamethrower hit target tile at ({tile.X},{tile.Y})! Instant victory!");
                ChangeState(GameState.Victory);
                return;
            }
        }
    }

    /// <summary>
    /// Called when a hero dies. Triggers defeat if all on-board heroes are gone.
    /// In Escape mode: checks if escape conditions were already met before declaring defeat.
    /// </summary>
    public void CheckDefeatCondition()
    {
        if (UnitManager.Instance.SpawnedHeroes.Count > 0)
            return;

        // All on-board heroes are gone. Check if Escape mode victory was achieved.
        if (CurrentLevel != null && CurrentLevel.victoryConditionType == VictoryConditionType.Escape)
        {
            if (CurrentLevel.requiresArtifact)
            {
                if (UnitManager.Instance.ArtifactCarrierEscaped)
                {
                    Debug.Log("[Victory] All on-board heroes gone, but artifact carrier escaped!");
                    ChangeState(GameState.Victory);
                    return;
                }
            }
            else
            {
                if (UnitManager.Instance.EscapedHeroCount >= CurrentLevel.requiredEscapees)
                {
                    Debug.Log("[Victory] All on-board heroes gone, but enough heroes escaped!");
                    ChangeState(GameState.Victory);
                    return;
                }
            }
        }

        ChangeState(GameState.Defeat);
    }

    private void OnVictory()
    {
        string message = "VICTORY!";
        if (CurrentLevel != null)
        {
            switch (CurrentLevel.victoryConditionType)
            {
                case VictoryConditionType.Escape:
                    message = $"VICTORY! {UnitManager.Instance.EscapedHeroCount} heroes escaped!";
                    break;
                case VictoryConditionType.FlamethrowerTarget:
                    message = "VICTORY! Target destroyed by flamethrower!";
                    break;
            }
        }

        Debug.Log(message);

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