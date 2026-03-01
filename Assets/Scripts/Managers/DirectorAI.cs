using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pressure level thresholds for Director intervention intensity.
/// Maps to the concept's four-tier intervention model (Abschnitt 8.4.1).
/// </summary>
public enum PressureLevel
{
    /// <summary>Pressure below 0.25 — players are in control. Escalation permitted.</summary>
    Low,

    /// <summary>Pressure 0.25-0.50 — manageable challenge. No intervention.</summary>
    Moderate,

    /// <summary>Pressure 0.50-0.70 — significant strain. Soft stabilization active.</summary>
    Elevated,

    /// <summary>Pressure above 0.70 — critical overload. Strong stabilization active.</summary>
    Critical
}

/// <summary>
/// Director AI implementing a pressure-based adaptive system for turn-based tactical gameplay.
///
/// Architecture follows the Transparenz-Schichten-Modell (Abschnitt 8.3):
///   Layer 1 (Protected): Grid, combat math, LOS, movement costs — never touched.
///   Layer 2 (Modulatable): AI aggression, spawn rate/positioning — adjusted via intervention API.
///   Layer 3 (Analytics): Pressure score computed from weighted game-state factors — read-only measurement.
///
/// The Director measures player pressure continuously (Layer 3), then provides
/// intervention parameters (Layer 2) that other systems query during their execution.
/// It never directly manipulates game state — it only influences decisions made by
/// EnemyAIManager (spawn count/positioning) and BlipBehavior (aggression scaling).
/// </summary>
public class DirectorAI : MonoBehaviour
{
    public static DirectorAI Instance;

    // ═══════════════════════════════════════════════════════════════
    //  LAYER 3: Pressure Measurement Configuration
    // ═══════════════════════════════════════════════════════════════

    [Header("Pressure Weights (sum to 1.0)")]

    [Tooltip("Weight for closest enemy-to-hero distance. Immediate spatial threat.")]
    [SerializeField] private float proximityWeight = 0.30f;

    [Tooltip("Weight for enemy:hero ratio. Being outnumbered raises pressure.")]
    [SerializeField] private float outnumberedWeight = 0.15f;

    [Tooltip("Weight for unrevealed blips on the board. Unknown threats create dread.")]
    [SerializeField] private float hiddenThreatWeight = 0.10f;

    [Tooltip("Weight for fraction of heroes lost. Losing squad members is devastating.")]
    [SerializeField] private float heroLossWeight = 0.25f;

    [Tooltip("Weight for recent combat events. Fighting just happened = elevated pressure.")]
    [SerializeField] private float combatWeight = 0.10f;

    [Tooltip("Weight for average enemy density around heroes. Surrounded = exposed.")]
    [SerializeField] private float exposureWeight = 0.10f;

    [Header("Pressure Thresholds")]

    [Tooltip("Below this: Low pressure — escalation permitted.")]
    [SerializeField] private float moderateThreshold = 0.25f;

    [Tooltip("Above this: Elevated pressure — soft stabilization begins.")]
    [SerializeField] private float elevatedThreshold = 0.50f;

    [Tooltip("Above this: Critical pressure — strong stabilization active.")]
    [SerializeField] private float criticalThreshold = 0.70f;

    [Header("Measurement Tuning")]

    [Tooltip("How fast the pressure score lerps toward the raw value. Higher = snappier.")]
    [SerializeField] private float smoothingSpeed = 3f;

    [Tooltip("How fast combat intensity decays per second.")]
    [SerializeField] private float combatDecayRate = 0.15f;

    [Tooltip("Maximum distance (Manhattan) for proximity threat measurement.")]
    [SerializeField] private int maxThreatDistance = 8;

    [Tooltip("Active blips that max out the hidden threat score.")]
    [SerializeField] private int maxBlipThreatCount = 3;

    [Tooltip("Average enemies within threat range per hero that maxes out exposure.")]
    [SerializeField] private int maxExposurePerHero = 4;

    // ═══════════════════════════════════════════════════════════════
    //  LAYER 2: Intervention Configuration
    // ═══════════════════════════════════════════════════════════════

    [Header("Layer 2: AI Behavior Modulation")]

    [Tooltip("Aggression multiplier at critical pressure (< 1.0 = more defensive AI).")]
    [SerializeField] private float aggressionAtCritical = 0.4f;

    [Tooltip("Aggression multiplier at low pressure (> 1.0 = more aggressive AI).")]
    [SerializeField] private float aggressionAtLow = 1.3f;

    [Header("Layer 2: Spawn Modulation")]

    [Tooltip("Probability of skipping an individual spawn at elevated pressure.")]
    [SerializeField, Range(0f, 1f)] private float spawnSkipChanceElevated = 0.3f;

    [Tooltip("Probability of skipping an individual spawn at critical pressure.")]
    [SerializeField, Range(0f, 1f)] private float spawnSkipChanceCritical = 0.6f;

    [Header("Layer 2: Spawn Positioning")]

    [Tooltip("Score bias for spawn tile selection. Positive = favor far tiles at high pressure.")]
    [SerializeField] private float spawnDistanceBias = 30f;

    [Header("Layer 2: Combat Dice Modulation")]

    [Tooltip("Enemy melee dice adjustment at critical pressure. Negative = fewer dice = less lethal to heroes.")]
    [SerializeField] private int meleeDiceAtCritical = -1;

    [Tooltip("Enemy melee dice adjustment at low pressure. Positive = more dice = more lethal to heroes.")]
    [SerializeField] private int meleeDiceAtLow = 1;

    [Tooltip("Ranged hit threshold adjustment at critical pressure. Negative = easier for heroes to hit.")]
    [SerializeField] private int hitThresholdAtCritical = -1;

    // ═══════════════════════════════════════════════════════════════
    //  Internal State
    // ═══════════════════════════════════════════════════════════════

    private float _pressureScore;
    private float _rawPressure;
    private PressureLevel _pressureLevel;

    // Per-component scores (0-1 each)
    private float _proximityScore;
    private float _outnumberedScore;
    private float _hiddenThreatScore;
    private float _heroLossScore;
    private float _combatScore;
    private float _exposureScore;

    // Combat spike tracking — decays over time
    private float _combatIntensity;

    // Baseline
    private int _initialHeroCount;
    private bool _initialized;

    // Timestamp for elapsed-time-based decay and smoothing
    private float _lastRecalcTime;

    // ═══════════════════════════════════════════════════════════════
    //  Public API — Layer 3 (Read-Only Measurement)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Smoothed aggregate pressure score (0-1).</summary>
    public float PressureScore => _pressureScore;

    /// <summary>Unsmoothed raw pressure for debug comparison.</summary>
    public float RawPressure => _rawPressure;

    /// <summary>Current pressure level derived from thresholds.</summary>
    public PressureLevel CurrentPressureLevel => _pressureLevel;

    // Per-factor getters for debug UI
    public float ProximityScore => _proximityScore;
    public float OutnumberedScore => _outnumberedScore;
    public float HiddenThreatScore => _hiddenThreatScore;
    public float HeroLossScore => _heroLossScore;
    public float CombatScore => _combatScore;
    public float ExposureScore => _exposureScore;

    // ═══════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Recalculates all pressure components and the aggregate score.
    /// Called by game systems after state-changing events (actions, spawns, deaths, turn transitions)
    /// instead of every frame — the game state only changes at discrete action points.
    /// </summary>
    public void RecalculatePressure()
    {
        // Lazy initialization — wait until heroes have spawned
        if (!_initialized)
        {
            if (UnitManager.Instance != null && UnitManager.Instance.SpawnedHeroes.Count > 0)
            {
                _initialHeroCount = UnitManager.Instance.SpawnedHeroes.Count;
                _initialized = true;
                _lastRecalcTime = Time.time;
            }
            else return;
        }

        float elapsed = Time.time - _lastRecalcTime;
        _lastRecalcTime = Time.time;

        // Decay combat intensity based on elapsed real time
        _combatIntensity = Mathf.Max(0f, _combatIntensity - combatDecayRate * elapsed);

        // Compute BFS distance maps per hero once — shared by Proximity and Exposure
        var heroDistMaps = new List<Dictionary<Tile, int>>();
        if (UnitManager.Instance != null)
        {
            foreach (var hero in UnitManager.Instance.SpawnedHeroes)
            {
                if (hero != null && hero.IsAlive && hero.OccupiedTile != null)
                    heroDistMaps.Add(PathfindingUtils.ComputeDistanceMap(hero.OccupiedTile));
            }
        }

        // Calculate each pressure component
        _proximityScore = CalcProximity(heroDistMaps);
        _outnumberedScore = CalcOutnumbered();
        _hiddenThreatScore = CalcHiddenThreats();
        _heroLossScore = CalcHeroLoss();
        _combatScore = _combatIntensity;
        _exposureScore = CalcExposure(heroDistMaps);

        // Weighted aggregate
        _rawPressure = Mathf.Clamp01(
            _proximityScore * proximityWeight +
            _outnumberedScore * outnumberedWeight +
            _hiddenThreatScore * hiddenThreatWeight +
            _heroLossScore * heroLossWeight +
            _combatScore * combatWeight +
            _exposureScore * exposureWeight
        );

        // Smooth based on elapsed time — long pauses snap, rapid actions smooth
        _pressureScore = Mathf.Lerp(_pressureScore, _rawPressure, Mathf.Clamp01(smoothingSpeed * elapsed));

        // Classify pressure level
        if (_pressureScore >= criticalThreshold)
            _pressureLevel = PressureLevel.Critical;
        else if (_pressureScore >= elevatedThreshold)
            _pressureLevel = PressureLevel.Elevated;
        else if (_pressureScore >= moderateThreshold)
            _pressureLevel = PressureLevel.Moderate;
        else
            _pressureLevel = PressureLevel.Low;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Public API — Layer 2 (Intervention Queries)
    //
    //  These methods are called by other systems to get Director guidance.
    //  They return modifiers, not commands — the calling system decides
    //  how to apply them, preserving its own decision autonomy.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns an aggression multiplier for enemy AI behaviors.
    /// Below 1.0 at high pressure (more defensive), above 1.0 at low pressure (more aggressive).
    /// Applied to BlipBehavior's aggression factor.
    /// </summary>
    public float GetAggressionModifier()
    {
        if (!_initialized) return 1f;
        // Linearly interpolate: low pressure → aggressionAtLow, high pressure → aggressionAtCritical
        return Mathf.Lerp(aggressionAtLow, aggressionAtCritical, _pressureScore);
    }

    /// <summary>
    /// Returns whether an individual spawn should be skipped (probabilistic).
    /// Used by EnemyAIManager per-spawn for granular control.
    /// </summary>
    public bool ShouldSkipSpawn()
    {
        float chance = _pressureLevel switch
        {
            PressureLevel.Critical => spawnSkipChanceCritical,
            PressureLevel.Elevated => spawnSkipChanceElevated,
            _ => 0f
        };
        return Random.value < chance;
    }

    /// <summary>
    /// Returns a scoring bias for spawn tile selection.
    /// Positive at high pressure: favors tiles far from heroes (less threatening spawns).
    /// Negative at low pressure: favors tiles close to heroes (escalation).
    /// Applied by UnitManager.ScoreSpawnTile().
    /// </summary>
    public float GetSpawnPositionBias()
    {
        return _pressureLevel switch
        {
            PressureLevel.Critical => spawnDistanceBias,
            PressureLevel.Elevated => spawnDistanceBias * 0.5f,
            PressureLevel.Low => -spawnDistanceBias * 0.3f,
            _ => 0f
        };
    }

    /// <summary>
    /// Returns an additive modifier to enemy melee dice count.
    /// Negative at critical pressure (fewer enemy dice = less lethal to heroes).
    /// Positive at low pressure (more enemy dice = harder for heroes).
    /// Applied by MeleeAttackAbility.GetDiceCount() for enemy units only.
    /// </summary>
    public int GetMeleeDiceModifier()
    {
        if (!_initialized) return 0;
        return _pressureLevel switch
        {
            PressureLevel.Critical => meleeDiceAtCritical,
            PressureLevel.Low => meleeDiceAtLow,
            _ => 0
        };
    }

    /// <summary>
    /// Returns an additive modifier to the ranged hit threshold.
    /// Negative at critical pressure (lower threshold = easier for heroes to hit).
    /// Applied by RangedAttackAbility before rolling dice.
    /// </summary>
    public int GetHitThresholdModifier()
    {
        if (!_initialized) return 0;
        return _pressureLevel switch
        {
            PressureLevel.Critical => hitThresholdAtCritical,
            _ => 0
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Event Registration
    //
    //  Called by combat systems to notify the Director of significant events.
    //  These spike the combat intensity component, which then decays over time.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Called after any combat resolution (melee, ranged, flamethrower).
    /// Spikes combat intensity — multiple rapid events stack up to 1.0.
    /// </summary>
    public void RegisterCombatEvent()
    {
        _combatIntensity = Mathf.Min(1f, _combatIntensity + 0.4f);
        RecalculatePressure();
    }

    /// <summary>
    /// Called when a hero dies. Larger spike than regular combat.
    /// </summary>
    public void RegisterHeroDeath()
    {
        _combatIntensity = Mathf.Min(1f, _combatIntensity + 0.7f);
        RecalculatePressure();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Layer 3: Pressure Component Calculations
    //
    //  Each method returns a 0-1 score for one aspect of player pressure.
    //  These are pure measurements with no side effects.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Closest targetable enemy distance to any hero.
    /// Adjacent (1 tile) = 1.0, beyond maxThreatDistance = 0.0.
    /// </summary>
    private float CalcProximity(List<Dictionary<Tile, int>> heroDistMaps)
    {
        if (UnitManager.Instance == null || heroDistMaps.Count == 0) return 0f;

        var enemies = UnitManager.Instance.SpawnedEnemies;
        if (enemies.Count == 0) return 0f;

        int closest = int.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive || enemy.OccupiedTile == null || !enemy.IsTargetable)
                continue;

            foreach (var distMap in heroDistMaps)
            {
                int dist = PathfindingUtils.GetPathDistance(enemy.OccupiedTile, distMap);
                if (dist < closest) closest = dist;
            }
        }

        if (closest == int.MaxValue) return 0f;
        return Mathf.Clamp01(1f - (float)(closest - 1) / (maxThreatDistance - 1));
    }

    /// <summary>
    /// Ratio of targetable enemies to living heroes.
    /// Even numbers = 0.25, 2:1 = 0.5, 4:1+ = 1.0.
    /// </summary>
    private float CalcOutnumbered()
    {
        if (UnitManager.Instance == null) return 0f;

        int heroCount = UnitManager.Instance.SpawnedHeroes.Count(h => h != null && h.IsAlive);
        if (heroCount == 0) return 1f;

        int enemyCount = UnitManager.Instance.SpawnedEnemies
            .Count(e => e != null && e.IsAlive && e.IsTargetable);

        return Mathf.Clamp01((float)enemyCount / heroCount / 4f);
    }

    /// <summary>
    /// Unrevealed blips that have left their spawn tile (active threats).
    /// 0 blips = 0.0, maxBlipThreatCount+ = 1.0.
    /// </summary>
    private float CalcHiddenThreats()
    {
        if (UnitManager.Instance == null) return 0f;

        int blips = 0;
        foreach (var enemy in UnitManager.Instance.SpawnedEnemies)
        {
            if (enemy is Enemy2 blip && !blip.IsRevealed && blip.IsAlive && blip.IsTargetable)
                blips++;
        }

        return Mathf.Clamp01((float)blips / maxBlipThreatCount);
    }

    /// <summary>
    /// Fraction of the starting hero squad that has been lost.
    /// 0 losses = 0.0, all dead = 1.0.
    /// </summary>
    private float CalcHeroLoss()
    {
        if (_initialHeroCount <= 0) return 0f;

        int current = 0;
        if (UnitManager.Instance != null)
            current = UnitManager.Instance.SpawnedHeroes.Count(h => h != null && h.IsAlive);

        int losses = _initialHeroCount - current;
        return Mathf.Clamp01((float)losses / _initialHeroCount);
    }

    /// <summary>
    /// Average number of targetable enemies within threat range per living hero.
    /// Measures how "surrounded" the hero squad is.
    /// 0 threats = 0.0, maxExposurePerHero+ average = 1.0.
    /// </summary>
    private float CalcExposure(List<Dictionary<Tile, int>> heroDistMaps)
    {
        if (UnitManager.Instance == null || heroDistMaps.Count == 0) return 0f;

        float totalThreats = 0f;

        foreach (var distMap in heroDistMaps)
        {
            foreach (var enemy in UnitManager.Instance.SpawnedEnemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.OccupiedTile == null || !enemy.IsTargetable)
                    continue;

                int dist = PathfindingUtils.GetPathDistance(enemy.OccupiedTile, distMap);
                if (dist <= maxThreatDistance)
                    totalThreats++;
            }
        }

        float avgThreats = totalThreats / heroDistMaps.Count;
        return Mathf.Clamp01(avgThreats / maxExposurePerHero);
    }
}
