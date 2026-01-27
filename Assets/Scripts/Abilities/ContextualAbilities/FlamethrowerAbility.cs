using System.Collections.Generic;
using UnityEngine;

public class FlamethrowerAbility : TargetedAbility
{
    public override string abilityName => "Flamethrower";
    public override int actionPointCost => 2;

    [SerializeField] private int range = 12; // How many tiles forward
    [SerializeField] private int maxCharges = 12;
    private int remainingCharges;

    public int RemainingCharges => remainingCharges;

    public void Initialize()
    {
        remainingCharges = maxCharges;
    }

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (unit == null || target == null)
            return false;

        if (!unit.CanPay(actionPointCost))
            return false;

        // Check if flamethrower has charges left
        if (remainingCharges <= 0)
        {
            Debug.Log("Flamethrower out of charges!");
            return false;
        }

        if (unit is not BaseHero hero)
            return false;

        // Check if target is in the 3x3 flame area in front of the hero
        HashSet<Tile> flameTiles = GetFlameTiles(hero);

        return flameTiles.Contains(target);
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        if (!CanExecute(unit, target))
            return;

        if (unit is not BaseHero hero)
            return;

        unit.SpendActionPoints(actionPointCost);

        // Consume one charge
        remainingCharges--;

        // Get all tiles in the 3x3 area
        HashSet<Tile> flameTiles = GetFlameTiles(hero);

        // Set fire to all tiles and immediately roll to kill aliens
        foreach (Tile tile in flameTiles)
        {
            FireEffectManager.Instance.CreateFireEffect(tile);
        }

        Debug.Log($"{hero.UnitName} used Flamethrower! {flameTiles.Count} tiles set on fire. Charges remaining: {remainingCharges}/{maxCharges}");

        // Log to combat debug
        CombatDebugLog.LogFlamethrower(hero.UnitName, flameTiles.Count, remainingCharges, maxCharges);
    }

    private HashSet<Tile> GetFlameTiles(BaseHero hero)
    {
        HashSet<Tile> result = new HashSet<Tile>();

        if (hero.OccupiedTile == null)
            return result;

        Vector2Int origin = new Vector2Int(hero.OccupiedTile.X, hero.OccupiedTile.Y);
        Vector2Int forward = hero.Forward;
        Vector2Int perpendicular = hero.Perpendicular;

        // Create 3x3 area at specified range
        for (int depth = 1; depth <= range; depth++)
        {
            for (int lateral = -1; lateral <= 1; lateral++)
            {
                Vector2Int pos = origin + forward * depth + perpendicular * lateral;
                Tile tile = GridManager.Instance.GetTileAtPosition(pos);

                if (tile != null)
                {
                    result.Add(tile);
                }
            }
        }

        return result;
    }
}
