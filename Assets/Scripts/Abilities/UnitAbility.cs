using UnityEngine;

public abstract class UnitAbility : ScriptableObject
    {
        public abstract string Name { get; }
        public abstract int APCost { get; }
        public abstract bool isContextual { get; } // contextual would be tile clicks like shooting a melee attack
        
        public abstract bool CanExecute(BaseUnit unit, Tile target);
        public abstract void Execute(BaseUnit unit, Tile target);
    }