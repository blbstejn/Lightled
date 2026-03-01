using System.Collections;
using System.Linq;
using UnityEngine;

public class BaseEnemy : BaseUnit
{
    [Header("AI")]
    [SerializeField] protected EnemyBehavior aiBehavior;
    public EnemyBehavior AIBehavior => aiBehavior;

    private Tile _previousTile;
    public Tile PreviousTile => _previousTile;

    public override void Awake()
    {
        MaxActionPoints = 6; // Enemies have 6 AP
        base.Awake();
    }

    /// <summary>
    /// Turns to face a new direction without spending AP (enemies can turn freely).
    /// </summary>
    public void TurnFree(FacingDirection newFacing)
    {
        if (facing == newFacing)
            return;

        SetFacing(newFacing);
        OnFacingChanged();
    }

    private void OnFacingChanged()
    {
        // hook for anim
    }

    /// <summary>
    /// Called by EnemyAIManager to get this enemy's next action.
    /// Delegates to the assigned AI behavior.
    /// </summary>
    public AIAction DecideNextAction()
    {
        if (aiBehavior == null)
        {
            Debug.LogWarning($"[AI] {UnitName} has no AI behavior assigned!");
            return AIAction.Wait();
        }

        return aiBehavior.DecideAction(this);
    }

    /// <summary>
    /// Executes a movement to the target tile. Spends AP based on direction.
    /// </summary>
    public void ExecuteMove(Tile target)
    {
        if (target == null || !target.Walkable)
        {
            Debug.LogWarning($"[AI] {UnitName}: Cannot move to invalid tile");
            return;
        }

        int cost = GetMoveCost(target);
        if (cost > CurrentActionPoints)
        {
            Debug.LogWarning($"[AI] {UnitName}: Not enough AP to move (need {cost}, have {CurrentActionPoints})");
            return;
        }

        // Track previous tile for anti-oscillation
        _previousTile = OccupiedTile;

        // Use existing movement system
        MoveOneTile(target);
    }

    /// <summary>
    /// Override movement coroutine to trigger overwatch checks when entering a tile.
    /// </summary>
    protected override IEnumerator MoveCoroutine(Tile target)
    {
        IsMoving = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = target.transform.position;

        float t = 0f;
        float moveSpeed = 5f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // Set the unit on the tile - this is when the enemy "enters" the tile
        target.SetUnit(this);

        // Trigger overwatch checks - enemy has entered a new tile
        if (OverwatchManager.Instance != null)
        {
            OverwatchManager.Instance.OnEnemyEnteredTile(this, target);

            // Wait for overwatch processing to complete (if any shots were fired)
            // This ensures the enemy waits for reaction shots before continuing
            yield return new WaitForSeconds(0.1f);
        }

        IsMoving = false;
        OnMoveEnded();
    }

    /// <summary>
    /// Executes a melee attack on the forward tile.
    /// </summary>
    public void ExecuteAttack()
    {
        var meleeAbility = unitAbilities.OfType<MeleeAttackAbility>().FirstOrDefault();
        if (meleeAbility == null)
        {
            Debug.LogWarning($"[AI] {UnitName}: No melee ability found");
            return;
        }

        Tile forwardTile = GetForwardTile();
        if (forwardTile == null)
        {
            Debug.LogWarning($"[AI] {UnitName}: No forward tile for attack");
            return;
        }

        if (!meleeAbility.CanExecute(this, forwardTile))
        {
            Debug.LogWarning($"[AI] {UnitName}: Cannot execute melee attack");
            return;
        }

        Debug.Log($"[AI] {UnitName}: Executing melee attack!");
        meleeAbility.Execute(this, forwardTile);
    }

    protected override void OnDeath(BaseUnit killer)
    {
        base.OnDeath(killer);

        // Clear UI references before destruction
        InGameUIManager.Instance?.OnUnitDestroyed(this);

        UnitManager.Instance?.OnEnemyDestroyed(this);
    }
}
