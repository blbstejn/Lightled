using System.Collections.Generic;
using UnityEngine;

public class FlamethrowerAbility : TargetedAbility
{
    public override string abilityName => "Flamethrower";
    public override int actionPointCost => 2;

    [SerializeField] private int maxCharges = 6;
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

        if (remainingCharges <= 0)
        {
            Debug.Log("[Flamethrower] Out of charges!");
            return false;
        }

        if (unit is not BaseHero hero)
            return false;

        // Can target any tile in line of sight (including unoccupied tiles)
        var losTiles = hero.GetLineOfSightTiles();
        if (!losTiles.Contains(target))
        {
            Debug.Log($"[Flamethrower] Target tile ({target.X},{target.Y}) not in line of sight");
            return false;
        }

        return true;
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        if (!CanExecute(unit, target))
            return;

        if (unit is not BaseHero hero)
            return;

        unit.SpendActionPoints(actionPointCost);
        remainingCharges--;

        // Get target tile + all 8 neighbors (9 tiles total)
        HashSet<Tile> affectedTiles = GetAffectedTiles(target);

        Debug.Log($"[Flamethrower] {hero.UnitName} fires at ({target.X},{target.Y})! Affecting {affectedTiles.Count} tiles. Charges: {remainingCharges}/{maxCharges}");

        // Roll to kill each unit on affected tiles
        int killed = 0;
        foreach (Tile tile in affectedTiles)
        {
            if (tile.OccupiedUnit != null && tile.OccupiedUnit.IsAlive)
            {
                BaseUnit targetUnit = tile.OccupiedUnit;

                // Roll 2+ on d6 to kill
                int roll = Random.Range(1, 7);
                int threshold = 2;
                bool isKilled = roll >= threshold;

                Debug.Log($"[Flamethrower] Roll for {targetUnit.UnitName} at ({tile.X},{tile.Y}): {roll} (need {threshold}+) - {(isKilled ? "KILLED" : "SURVIVED")}");

                CombatDebugLog.LogFireDamage(targetUnit.UnitName, tile.X, tile.Y, roll, threshold, isKilled);

                if (isKilled)
                {
                    killed++;
                    CombatDebugLog.LogKill(targetUnit.UnitName, $"{hero.UnitName} (Flamethrower)");

                    // Blips are killed without spawning ghouls
                    if (targetUnit is Enemy2 blip)
                    {
                        blip.KillByFire();
                    }
                    else
                    {
                        targetUnit.Kill(hero);
                    }
                }
            }
        }

        // Create permanent fire on center tile only (blocks LOS and movement)
        if (FireEffectManager.Instance != null)
        {
            FireEffectManager.Instance.CreateFireEffect(target, rollDamageImmediately: false);

            // Create brief fire flash on all neighbor tiles to show damage area
            var neighbors = GridManager.Instance.GetNeighbours(target);
            foreach (Tile neighbor in neighbors)
            {
                FireEffectManager.Instance.CreateFireFlash(neighbor);
            }
        }

        CombatDebugLog.LogFlamethrower(hero.UnitName, affectedTiles.Count, remainingCharges, maxCharges);
        Debug.Log($"[Flamethrower] Attack complete. Killed {killed} units. Permanent fire on center, flash on {affectedTiles.Count - 1} neighbors.");
    }

    private HashSet<Tile> GetAffectedTiles(Tile center)
    {
        HashSet<Tile> result = new HashSet<Tile> { center };

        // Add all 8 neighbors
        var neighbors = GridManager.Instance.GetNeighbours(center);
        foreach (Tile neighbor in neighbors)
        {
            result.Add(neighbor);
        }

        return result;
    }
}
