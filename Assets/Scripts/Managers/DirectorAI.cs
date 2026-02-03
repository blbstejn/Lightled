using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The director's current assessment of player pressure.
/// Determines what the director "thinks" about the game state.
/// </summary>
public enum DirectorState
{
    /// <summary>Tension below 0.2 — players are in control, no immediate threats.</summary>
    Calm,

    /// <summary>Tension 0.2-0.5 — threats are forming, pressure is rising.</summary>
    Building,

    /// <summary>Tension 0.5-0.75 — significant danger, enemies closing in.</summary>
    Escalating,

    /// <summary>Tension above 0.75 — overwhelming pressure, heroes in serious trouble.</summary>
    Critical
}

/// <summary>
/// AI Director that monitors player stress and tension.
/// Computes a continuous tension score (0-1) from multiple game state factors.
/// Currently measurement-only — does not influence gameplay.
/// </summary>
public class DirectorAI : MonoBehaviour
{
    public static DirectorAI Instance;

    // ─── Tension weights ───
    // Each weight controls how much a factor contributes to the final tension score.
    // All weights should sum to 1.0 for a normalized output.

    [Header("Tension Weights")]

    [Tooltip("Weight for closest enemy-to-hero distance. This is the most immediate pressure signal.")]
    [SerializeField] private float proximityWeight = 0.30f;

    [Tooltip("Weight for enemy:hero ratio. Being outnumbered raises tension.")]
    [SerializeField] private float outnumberedWeight = 0.20f;

    [Tooltip("Weight for unrevealed blips on the board. Unknown threats create dread.")]
    [SerializeField] private float hiddenThreatWeight = 0.15f;

    [Tooltip("Weight for fraction of heroes lost. Losing squadmates is devastating.")]
    [SerializeField] private float heroLossWeight = 0.15f;

    [Tooltip("Weight for low command points. Fewer CP means less tactical flexibility.")]
    [SerializeField] private float resourceWeight = 0.10f;

    [Tooltip("Weight for recent combat events. Fighting just happened = elevated tension.")]
    [SerializeField] private float recentCombatWeight = 0.10f;

    // ─── Tuning parameters ───

    [Header("Tuning")]

    [Tooltip("How fast the tension score lerps toward the raw calculated value. Higher = snappier response.")]
    [SerializeField] private float smoothingSpeed = 3f;

    [Tooltip("How fast the combat spike decays per second. Lower = combat tension lingers longer.")]
    [SerializeField] private float combatDecayRate = 0.2f;

    [Tooltip("Maximum distance (Manhattan) at which enemies still register as a proximity threat. Beyond this = 0 tension.")]
    [SerializeField] private int maxThreatDistance = 8;

    [Tooltip("Number of active blips that maxes out the hidden threat score.")]
    [SerializeField] private int maxBlipThreatCount = 3;

    // ─── Tension output ───
    // These are the final computed values read by the debug UI and future systems.

    // Smoothed final tension value (0-1), updated each frame
    private float _tensionScore;

    // Unsmoothed calculated tension, useful for debug comparison
    private float _rawTension;

    // Current director state derived from tension thresholds
    private DirectorState _currentState;

    // ─── Combat spike tracking ───
    // Combat events cause an instant spike that decays over time,
    // representing the lingering stress of a recent fight.

    // Current combat spike intensity (0-1), decays each frame
    private float _recentCombatIntensity;

    // ─── Baseline tracking ───

    // Number of heroes at level start, used to calculate loss ratio
    private int _initialHeroCount;

    // Whether we've captured the initial hero count yet
    private bool _initialized;

    // ─── Per-factor scores ───
    // Stored separately so the debug UI can display each factor's contribution.

    // How close the nearest enemy is to any hero (1.0 = adjacent, 0.0 = far/none)
    private float _proximityScore;

    // How outnumbered the heroes are (0.0 = even or advantage, 1.0 = heavily outnumbered)
    private float _outnumberedScore;

    // How many unrevealed blips are active on the board (0.0 = none, 1.0 = maxBlipThreatCount+)
    private float _hiddenThreatScore;

    // Fraction of starting heroes that have died (0.0 = no losses, 1.0 = all dead)
    private float _heroLossScore;

    // How depleted command points are (0.0 = full CP, 1.0 = no CP)
    private float _resourceScore;

    // Recent combat spike value after decay (0.0 = calm, 1.0 = just fought)
    private float _combatScore;

    // ─── Public API ───

    /// <summary>The smoothed tension score (0-1). Main output of the director.</summary>
    public float TensionScore => _tensionScore;

    /// <summary>The raw unsmoothed tension score (0-1). Useful for debug comparison.</summary>
    public float RawTension => _rawTension;

    /// <summary>The director's current state assessment.</summary>
    public DirectorState CurrentState => _currentState;

    // Per-factor getters for the debug UI
    public float ProximityScore => _proximityScore;
    public float OutnumberedScore => _outnumberedScore;
    public float HiddenThreatScore => _hiddenThreatScore;
    public float HeroLossScore => _heroLossScore;
    public float ResourceScore => _resourceScore;
    public float CombatScore => _combatScore;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Wait until heroes have actually spawned before measuring anything
        if (!_initialized)
        {
            if (UnitManager.Instance != null && UnitManager.Instance.SpawnedHeroes.Count > 0)
            {
                _initialHeroCount = UnitManager.Instance.SpawnedHeroes.Count;
                _initialized = true;
                Debug.Log($"[Director] Initialized with {_initialHeroCount} heroes");
            }
            else
            {
                return;
            }
        }

        // Decay the combat spike over time
        _recentCombatIntensity = Mathf.Max(0f, _recentCombatIntensity - combatDecayRate * Time.deltaTime);

        // Calculate each factor
        _proximityScore = CalcProximityScore();
        _outnumberedScore = CalcOutnumberedScore();
        _hiddenThreatScore = CalcHiddenThreatScore();
        _heroLossScore = CalcHeroLossScore();
        _resourceScore = CalcResourceScore();
        _combatScore = _recentCombatIntensity;

        // Combine weighted factors into raw tension
        _rawTension = Mathf.Clamp01(
            _proximityScore * proximityWeight +
            _outnumberedScore * outnumberedWeight +
            _hiddenThreatScore * hiddenThreatWeight +
            _heroLossScore * heroLossWeight +
            _resourceScore * resourceWeight +
            _combatScore * recentCombatWeight
        );

        // Smooth the tension score to prevent jitter
        _tensionScore = Mathf.Lerp(_tensionScore, _rawTension, smoothingSpeed * Time.deltaTime);

        // Determine director state from tension thresholds
        _currentState = _tensionScore switch
        {
            < 0.2f => DirectorState.Calm,
            < 0.5f => DirectorState.Building,
            < 0.75f => DirectorState.Escalating,
            _ => DirectorState.Critical
        };
    }

    /// <summary>
    /// Call this after any melee combat resolution to spike combat tension.
    /// The spike decays over time via combatDecayRate.
    /// </summary>
    public void RegisterCombatEvent()
    {
        // Each combat event pushes the spike toward 1.0
        // Multiple rapid events stack up to the cap
        _recentCombatIntensity = Mathf.Min(1f, _recentCombatIntensity + 0.5f);
        Debug.Log($"[Director] Combat event registered. Combat intensity: {_recentCombatIntensity:F2}");
    }

    /// <summary>
    /// Call this when a hero dies. Spikes combat tension and updates loss tracking.
    /// </summary>
    public void RegisterHeroDeath()
    {
        // Hero death is a major combat event
        _recentCombatIntensity = Mathf.Min(1f, _recentCombatIntensity + 0.8f);
        Debug.Log($"[Director] Hero death registered. Combat intensity: {_recentCombatIntensity:F2}");
    }

    // ─── Factor calculations ───

    /// <summary>
    /// Measures how close the nearest enemy is to any hero.
    /// Adjacent (1 tile) = 1.0, beyond maxThreatDistance = 0.0.
    /// Only counts enemies that are on the board (not on spawn tiles).
    /// </summary>
    private float CalcProximityScore()
    {
        if (UnitManager.Instance == null)
            return 0f;

        var heroes = UnitManager.Instance.SpawnedHeroes;
        var enemies = UnitManager.Instance.SpawnedEnemies;

        if (heroes.Count == 0 || enemies.Count == 0)
            return 0f;

        int closestDistance = int.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive || enemy.OccupiedTile == null)
                continue;

            // Skip enemies still on spawn tiles (not yet a real threat)
            if (!enemy.IsTargetable)
                continue;

            foreach (var hero in heroes)
            {
                if (hero == null || !hero.IsAlive || hero.OccupiedTile == null)
                    continue;

                int dist = PathfindingUtils.ManhattanDistance(enemy.OccupiedTile, hero.OccupiedTile);
                if (dist < closestDistance)
                    closestDistance = dist;
            }
        }

        if (closestDistance == int.MaxValue)
            return 0f;

        // Invert and normalize: distance 1 = 1.0, distance maxThreatDistance+ = 0.0
        return Mathf.Clamp01(1f - (float)(closestDistance - 1) / (maxThreatDistance - 1));
    }

    /// <summary>
    /// Measures how outnumbered the heroes are.
    /// Ratio of targetable enemies to living heroes.
    /// 1:1 = 0.25, 2:1 = 0.5, 3:1+ = 1.0.
    /// </summary>
    private float CalcOutnumberedScore()
    {
        if (UnitManager.Instance == null)
            return 0f;

        int heroCount = UnitManager.Instance.SpawnedHeroes.Count(h => h != null && h.IsAlive);
        if (heroCount == 0)
            return 1f;

        // Count targetable enemies only (excludes blips on spawn tiles)
        int enemyCount = UnitManager.Instance.SpawnedEnemies.Count(e => e != null && e.IsAlive && e.IsTargetable);

        // Ratio: 0 enemies = 0, 1:1 = 0.25, 2:1 = 0.5, 3:1 = 0.75, 4:1+ = 1.0
        float ratio = (float)enemyCount / heroCount;
        return Mathf.Clamp01(ratio / 4f);
    }

    /// <summary>
    /// Measures how many unrevealed blips are active on the board (not on spawn tiles).
    /// These represent unknown threats the player can't fully account for.
    /// 0 blips = 0.0, maxBlipThreatCount+ = 1.0.
    /// </summary>
    private float CalcHiddenThreatScore()
    {
        if (UnitManager.Instance == null)
            return 0f;

        int activeBlips = 0;

        foreach (var enemy in UnitManager.Instance.SpawnedEnemies)
        {
            if (enemy is Enemy2 blip && !blip.IsRevealed && blip.IsAlive)
            {
                // Only count blips that have left their spawn tile (active threats)
                if (blip.IsTargetable)
                    activeBlips++;
            }
        }

        return Mathf.Clamp01((float)activeBlips / maxBlipThreatCount);
    }

    /// <summary>
    /// Measures what fraction of the starting hero squad has been lost.
    /// 0 losses = 0.0, all dead = 1.0.
    /// </summary>
    private float CalcHeroLossScore()
    {
        if (_initialHeroCount <= 0)
            return 0f;

        int currentHeroes = 0;
        if (UnitManager.Instance != null)
            currentHeroes = UnitManager.Instance.SpawnedHeroes.Count(h => h != null && h.IsAlive);

        int losses = _initialHeroCount - currentHeroes;
        return Mathf.Clamp01((float)losses / _initialHeroCount);
    }

    /// <summary>
    /// Measures how depleted the player's command points are.
    /// Full CP = 0.0, no CP = 1.0.
    /// </summary>
    private float CalcResourceScore()
    {
        if (GameManager.Instance == null || GameManager.Instance.MaxCommandPoints <= 0)
            return 0f;

        float cpRatio = (float)GameManager.Instance.CurrentCommandPoints / GameManager.Instance.MaxCommandPoints;
        return Mathf.Clamp01(1f - cpRatio);
    }
}
