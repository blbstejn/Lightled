using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Melee Attack")]
public class MeleeAttackAbility : TargetedAbility
{
    public override string abilityName => "Melee Attack";
    public override int actionPointCost => 1;

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (unit == null || target == null)
        {
            Debug.Log($"[MeleeAttack] CanExecute: unit or target is null");
            return false;
        }

        int heroAP = unit.CurrentActionPoints;
        int availableCP = GameManager.Instance != null ? GameManager.Instance.CurrentCommandPoints : 0;

        // Check if AP or AP + CP can cover the cost
        if (heroAP < actionPointCost && (heroAP + availableCP) < actionPointCost)
        {
            Debug.Log($"[MeleeAttack] CanExecute: {unit.UnitName} cannot afford {actionPointCost} AP (has {heroAP} AP, {availableCP} CP)");
            return false;
        }

        Tile forwardTile = unit.GetForwardTile();
        if (forwardTile == null)
        {
            Debug.Log($"[MeleeAttack] CanExecute: {unit.UnitName} has no forward tile");
            return false;
        }

        // IMPORTANT: Check that the clicked tile IS the forward tile
        if (target != forwardTile)
        {
            Debug.Log($"[MeleeAttack] CanExecute: Clicked tile ({target.X},{target.Y}) is not the forward tile ({forwardTile.X},{forwardTile.Y})");
            return false;
        }

        if (forwardTile.OccupiedUnit == null)
        {
            Debug.Log($"[MeleeAttack] CanExecute: Forward tile ({forwardTile.X},{forwardTile.Y}) has no unit");
            return false;
        }

        if (!forwardTile.OccupiedUnit.IsTargetable)
        {
            Debug.Log($"[MeleeAttack] CanExecute: {forwardTile.OccupiedUnit.UnitName} is not targetable (lurking on spawn tile)");
            return false;
        }

        if (forwardTile.OccupiedUnit.Faction == unit.Faction)
        {
            Debug.Log($"[MeleeAttack] CanExecute: Target {forwardTile.OccupiedUnit.UnitName} is same faction");
            return false;
        }

        Debug.Log($"[MeleeAttack] CanExecute: {unit.UnitName} CAN attack {forwardTile.OccupiedUnit.UnitName} at ({forwardTile.X},{forwardTile.Y})");
        return true;
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        Debug.Log($"[MeleeAttack] Execute: {unit.UnitName} attacking tile ({target.X},{target.Y})");

        Tile forwardTile = unit.GetForwardTile();
        if (forwardTile == null || forwardTile.OccupiedUnit == null)
        {
            Debug.LogWarning($"[MeleeAttack] Execute: No valid target in forward tile!");
            return;
        }

        BaseUnit attacker = unit;
        BaseUnit defender = forwardTile.OccupiedUnit;

        // Spend AP
        int apToSpend = Mathf.Min(unit.CurrentActionPoints, actionPointCost);
        if (apToSpend > 0)
        {
            unit.SpendActionPoints(apToSpend);
            Debug.Log($"[MeleeAttack] {unit.UnitName} spent {apToSpend} AP (remaining: {unit.CurrentActionPoints})");
        }

        // Melee attack breaks sustained fire
        unit.BreakSustainedFire();

        // Melee attack breaks overwatch
        if (unit is BaseHero heroUnit)
        {
            heroUnit.BreakOverwatch();
        }

        // Flash the defender to show they're being attacked
        defender.FlashHit();

        // Determine dice count: heroes get 1 die, ghouls/enemies get 3 dice
        int attackerDice = GetDiceCount(attacker, true);
        int defenderDice = GetDiceCount(defender, false);

        // Roll dice and get highest for each side
        int attackerBest = RollDiceGetHighest(attackerDice);
        int defenderBest = RollDiceGetHighest(defenderDice);

        Debug.Log($"[MeleeAttack] {attacker.UnitName} ({attackerDice}d6) rolled best: {attackerBest} vs {defender.UnitName} ({defenderDice}d6) rolled best: {defenderBest}");

        // Log the contested melee
        CombatDebugLog.LogMeleeContest(attacker.UnitName, defender.UnitName, attackerDice, attackerBest, defenderDice, defenderBest);

        if (attackerBest > defenderBest)
        {
            // Attacker wins - defender dies
            Debug.Log($"[MeleeAttack] {attacker.UnitName} WINS! {defender.UnitName} is killed!");
            CombatDebugLog.LogKill(defender.UnitName, attacker.UnitName);
            defender.Kill(attacker);
        }
        else if (defenderBest > attackerBest)
        {
            // Defender wins - check if defender is facing attacker
            bool defenderFacingAttacker = IsUnitFacingTarget(defender, attacker);

            if (defenderFacingAttacker)
            {
                // Defender is facing attacker - attacker dies
                Debug.Log($"[MeleeAttack] {defender.UnitName} WINS and is facing attacker! {attacker.UnitName} is killed!");
                CombatDebugLog.LogKill(attacker.UnitName, defender.UnitName);
                attacker.Kill(defender);
            }
            else
            {
                // Defender not facing attacker - defender turns to face, no death
                Debug.Log($"[MeleeAttack] {defender.UnitName} WINS but was not facing. Turning to face {attacker.UnitName}.");
                TurnUnitTowardTarget(defender, attacker);
            }
        }
        else
        {
            // Tie - nothing happens
            Debug.Log($"[MeleeAttack] TIE! Both rolled {attackerBest}. No effect.");
        }

        DirectorAI.Instance?.RegisterCombatEvent();
    }

    private int GetDiceCount(BaseUnit unit, bool bIsAttacking)
    {
        if (unit.Faction == Faction.Hero)
            return bIsAttacking ? 2 : 1;

        int modifier = DirectorAI.Instance?.GetMeleeDiceModifier() ?? 0;
        int dice = Mathf.Max(1, 3 + modifier);
        if (modifier != 0)
            Debug.Log($"[MeleeAttack] Director adjusts enemy dice by {modifier:+#;-#}: {dice}d6");
        return dice;
    }

    private int RollDiceGetHighest(int diceCount)
    {
        int highest = 0;
        for (int i = 0; i < diceCount; i++)
        {
            int roll = Random.Range(1, 7);
            if (roll > highest)
                highest = roll;
        }
        return highest;
    }

    private bool IsUnitFacingTarget(BaseUnit unit, BaseUnit target)
    {
        if (unit.OccupiedTile == null || target.OccupiedTile == null)
            return false;

        // Get direction from unit to target
        int dx = target.OccupiedTile.X - unit.OccupiedTile.X;
        int dy = target.OccupiedTile.Y - unit.OccupiedTile.Y;

        // Normalize to get cardinal direction
        Vector2Int directionToTarget = new Vector2Int(
            dx == 0 ? 0 : (dx > 0 ? 1 : -1),
            dy == 0 ? 0 : (dy > 0 ? 1 : -1)
        );

        // Check if unit's forward matches direction to target
        return unit.Forward == directionToTarget;
    }

    private void TurnUnitTowardTarget(BaseUnit unit, BaseUnit target)
    {
        if (unit.OccupiedTile == null || target.OccupiedTile == null)
            return;

        // Get direction from unit to target
        int dx = target.OccupiedTile.X - unit.OccupiedTile.X;
        int dy = target.OccupiedTile.Y - unit.OccupiedTile.Y;

        // Determine the primary direction (cardinal only)
        FacingDirection newFacing;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            newFacing = dx > 0 ? FacingDirection.Right : FacingDirection.Left;
        }
        else
        {
            newFacing = dy > 0 ? FacingDirection.Up : FacingDirection.Down;
        }

        unit.SetFacing(newFacing);
        Debug.Log($"[MeleeAttack] {unit.UnitName} turned to face {newFacing}");
    }
}
