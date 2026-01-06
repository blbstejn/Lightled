using UnityEngine;

public abstract class TargetedAbility : UnitAbility
    {
        public Tile TargetTile { get; protected set; }

        public virtual void SetTarget(Tile tile)
        {
            TargetTile = tile;
        }
    }