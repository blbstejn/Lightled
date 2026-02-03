using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FacingDirection
{
    Up,
    Down,
    Left,
    Right
}

[Serializable]
public class MovementProfile
{
    public bool allowSideways;
    // also applies to sideways
    public int forwardCost = 1;
    public int backwardCost = 2;
}

public class BaseUnit : MonoBehaviour
{
    public string UnitName;
    public Tile OccupiedTile;
    public Faction Faction;

    [Header("Health")]
    private bool _isAlive = true;
    public bool IsAlive => _isAlive;

    /// <summary>
    /// Whether this unit can be targeted by attacks.
    /// Override in subclasses for special cases (e.g., blips on spawn tiles).
    /// </summary>
    public virtual bool IsTargetable => true;
    
    [Header("ActionPoints")]
    public int MaxActionPoints { get; protected set; } = 4;
    public int CurrentActionPoints { get; protected set; }

    [Header("Abilities")]
    public List<UnitAbility> unitAbilities = new List<UnitAbility>();
    public IReadOnlyList<UnitAbility> Abilities => unitAbilities;
    
    [Header("Movement")]
    [SerializeField] protected MovementProfile movementProfile;
    [SerializeField] protected float moveSpeed = 5f;
    public bool IsMoving { get; private set; }
    public bool HasMovedThisTurn { get; private set; }

    [Header("Facing")]
    [SerializeField] protected FacingDirection facing;
    public FacingDirection Facing => facing;

    [Header("Combat")]
    public BaseUnit SustainedFireTarget { get; private set; }
    public bool HasSustainedFire => SustainedFireTarget != null;

    [Header("Artifact")]
    public bool HasArtifact { get; private set; }

    public virtual void Awake()
    {
        _isAlive = true;
        CurrentActionPoints = MaxActionPoints;

        // Disable collider so raycasts pass through to tiles
        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;
    }

    public void StartNewTurn()
    {
        CurrentActionPoints = MaxActionPoints;
        HasMovedThisTurn = false;
        SustainedFireTarget = null;
    }

    public void SetHasMoved()
    {
        HasMovedThisTurn = true;
    }

    public void SetSustainedFireTarget(BaseUnit target)
    {
        SustainedFireTarget = target;
    }

    public void BreakSustainedFire()
    {
        SustainedFireTarget = null;
    }

    public void PickupArtifact()
    {
        HasArtifact = true;
        Debug.Log($"{UnitName} picked up the artifact!");
    }

    public void DropArtifact()
    {
        HasArtifact = false;
    }

    public Tile GetForwardTile()
    {
        if (OccupiedTile == null)
            return null;
        
        Vector2Int forward = DirectionUtils.ToVector(facing);
        Vector2Int pos = new Vector2Int(OccupiedTile.X + forward.x, OccupiedTile.Y + forward.y);
        
        return GridManager.Instance.GetTileAtPosition(pos);
    }
    
    #region Movement

    public void SetFacing(FacingDirection newFacing)
    {
        facing = newFacing;
        UpdateVisualFacing();
    }

    private void UpdateVisualFacing()
    {
        float angle = facing switch
        {
            FacingDirection.Up    => 0f,
            FacingDirection.Right => -90f,
            FacingDirection.Down  => 180f,
            FacingDirection.Left  => 90f,
            _ => 0f
        };

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public Vector2Int Forward => DirectionUtils.ToVector(facing);
    
    public Vector2Int Perpendicular => new Vector2Int(-Forward.y, Forward.x);
    
    public int GetMoveCost(Tile target)
    {
        if (OccupiedTile == null || !target.Walkable || movementProfile == null)
            return int.MaxValue;

        int dx = target.X - OccupiedTile.X;
        int dy = target.Y - OccupiedTile.Y;

        // Adjacent tiles only
        if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1)
            return int.MaxValue;

        Vector2Int moveDir = new Vector2Int(dx, dy);
        Vector2Int forward = DirectionUtils.ToVector(facing);

        int dot = (int)Vector2.Dot(forward, moveDir);

        // Sideways
        if (dot == 0)
        {
            return movementProfile.allowSideways
                ? movementProfile.forwardCost
                : int.MaxValue;
        }

        // Forward / diagonal forward
        if (dot > 0)
            return movementProfile.forwardCost;

        // Backward / diagonal backward
        return movementProfile.backwardCost;
    }

    public bool CanMoveTo(Tile target)
    {
        return GetMoveCost(target) <= CurrentActionPoints;
    }

    public void MoveOneTile(Tile target)
    {
        int cost = GetMoveCost(target);

        // Note: CanExecute is checked in Tile.OnPointerClick before calling Execute
        // CP is also spent there if needed, so we only spend remaining AP here
        int apToSpend = Mathf.Min(CurrentActionPoints, cost);
        if (apToSpend > 0)
        {
            SpendActionPoints(apToSpend);
        }

        StartCoroutine(MoveCoroutine(target));
    }
    
    protected virtual IEnumerator MoveCoroutine(Tile target)
    {
        IsMoving = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = target.transform.position;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        target.SetUnit(this);
        IsMoving = false;
        OnMoveEnded();
    }

    protected virtual void OnMoveEnded()
    {
        // Check if sustained fire target is still in line of sight
        if (HasSustainedFire && this is BaseHero hero)
        {
            var losTiles = hero.GetLineOfSightTiles();
            if (SustainedFireTarget.OccupiedTile == null || !losTiles.Contains(SustainedFireTarget.OccupiedTile))
            {
                Debug.Log($"{UnitName} lost line of sight to {SustainedFireTarget.UnitName} - sustained fire broken!");
                BreakSustainedFire();
            }
        }

        // Check for artifact pickup
        var artifact = GridManager.Instance.GetArtifactOnTile(OccupiedTile);
        if (artifact != null)
        {
            artifact.Pickup(this);
        }

        // Check if hero reached target tile
        if (this is BaseHero && OccupiedTile is TargetTile)
        {
            GameManager.Instance.OnHeroReachedTarget(this);
        }

        // When a hero moves, check if any blips are now revealed
        if (this is BaseHero)
        {
            Debug.Log($"[Hero Move] {UnitName} finished moving - checking blips for reveal");
            UnitManager.Instance.CheckAllBlipsForReveal();
        }

        // Check for fire damage if entering a tile adjacent to fire
        if (FireEffectManager.Instance != null)
        {
            FireEffectManager.Instance.CheckAdjacentFireDamage(this);
        }
    }

    #endregion
    
    #region AbilitiesAndAP

    public void SetActionPoints(int amount)
    {
        CurrentActionPoints = Mathf.Clamp(amount, 0, MaxActionPoints);
    }

    public void SpendActionPoints(int amount)
    {
        CurrentActionPoints = Mathf.Max(CurrentActionPoints - amount, 0);

        // Refresh UI to show updated stats
        InGameUIManager.Instance?.RefreshUnitInfo(this);
    }

    public bool CanPay(int cost) => CurrentActionPoints >= cost;
    
    #endregion
    
    #region Damage

    /// <summary>
    /// Kills this unit instantly.
    /// </summary>
    public virtual void Kill(BaseUnit killer = null)
    {
        if (!IsAlive)
            return;

        _isAlive = false;
        Die(killer);
    }

    #endregion
    
    #region Death

    protected virtual void Die(BaseUnit killer)
    {
        OnDeath(killer);

        if (OccupiedTile != null)
        {
            OccupiedTile.OccupiedUnit = null;
            OccupiedTile = null;
        }
        
        Destroy(gameObject);
    }

    protected virtual void OnDeath(BaseUnit killer)
    {
        // hook for death anim, particles, XP
    }
    
    #endregion
    
    public void DisplayUnitInfo()
    {
        // show unit info in canva
    }
}
