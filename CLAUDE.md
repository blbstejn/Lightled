# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Lightled is a Unity 2D turn-based tactical game with a grid-based combat system. The game features heroes and enemies on a procedurally generated grid with direction-based movement and line-of-sight mechanics.

## Unity Project Structure

This is a Unity project using:
- Unity 2D with Universal Render Pipeline (URP)
- Input System package for controls
- ScriptableObject-based data architecture

### Key Directories

- `Assets/Scripts/Managers/` - Core game systems (GameManager, GridManager, UnitManager, MenuManager)
- `Assets/Scripts/Units/` - Unit base classes and implementations (BaseUnit, BaseHero, BaseEnemy)
- `Assets/Scripts/Tiles/` - Tile system (Tile, WalkableTile, VoidTile)
- `Assets/Scripts/Abilities/` - Ability system with contextual and UI abilities
- `Assets/Scripts/Utils/` - Utility classes (PriorityQueue, DirectionUtils)
- `Assets/Resources/Units/` - ScriptableUnit assets loaded at runtime
- `Assets/Prefabs/` - Prefab assets for units and tiles
- `Assets/Scenes/` - Unity scene files

## Core Architecture

### Game State Flow

The game operates through a state machine managed by [GameManager.cs](Assets/Scripts/Managers/GameManager.cs):

1. **GenerateGrid** - GridManager creates the tile grid
2. **SpawnHeroes** - UnitManager spawns hero units on the left side
3. **SpawnEnemies** - UnitManager spawns enemy units on the right side
4. **HeroesTurn** - Player controls hero units
5. **EnemiesTurn** - Enemy AI takes actions

### Manager Pattern

All managers use the singleton pattern with `public static Instance`:
- **GameManager** - Controls game state transitions
- **GridManager** - Manages the tile grid, movement highlights, and line-of-sight visualization
- **UnitManager** - Spawns units, manages unit selection, loads ScriptableUnit assets from Resources
- **MenuManager** - Handles UI display for tiles and units

### Unit System

Units inherit from [BaseUnit.cs](Assets/Scripts/Units/BaseUnit.cs):
- **BaseUnit** - Core unit logic with HP, action points (AP), abilities, movement, and facing direction
- **BaseHero** (extends BaseUnit) - Player-controlled units with cone-based line-of-sight calculation
- **BaseEnemy** (extends BaseUnit) - AI-controlled units

Units are instantiated from **ScriptableUnit** assets which define the faction and unit prefab.

### Movement System

Movement is direction-aware and AP-based:
- Units have a `FacingDirection` (Up, Down, Left, Right) that affects rotation and abilities
- `MovementProfile` defines costs: forward/sideways movement vs backward movement
- Movement validation checks AP cost via `GetMoveCost()` which returns different costs based on move direction relative to facing
- Movement is smooth via coroutine-based lerping

### Ability System

Abilities inherit from [UnitAbility.cs](Assets/Scripts/Abilities/UnitAbility.cs) ScriptableObject:
- **Contextual abilities** (`isContextual = true`) - Triggered by clicking tiles (e.g., MoveAbility, MeleeAttackAbility)
- **UI abilities** (`isContextual = false`) - Triggered via UI buttons (e.g., TurnLeftAbility, TurnRightAbility)
- Each ability has an AP cost and implements `CanExecute()` and `Execute()` methods
- Abilities are assigned to units as a list in the inspector

### Grid and Tile System

[GridManager.cs](Assets/Scripts/Managers/GridManager.cs) manages a Dictionary<Vector2, Tile>:
- Tiles store their grid position (X, Y) and occupied unit
- `Walkable` property checks if tile is walkable type AND unoccupied
- Tiles handle mouse input for selection and ability execution
- Visual feedback: highlighted tiles for movement range, color overlay for line-of-sight

### Line of Sight

Heroes calculate cone-shaped line of sight in [BaseHero.cs](Assets/Scripts/Units/Heroes/BaseHero.cs):
- Expands forward from unit's facing direction
- Width increases with depth using `shootConeWidth` parameter
- Stops expanding when no tiles found at current depth
- Used for ranged attack targeting

## Development Workflow

### Opening the Project

1. Open Unity Hub
2. Open the project at `Lightled/` directory
3. Unity will process and import assets (may take several minutes on first open)

### Running the Game

- In Unity Editor: Open a scene from `Assets/Scenes/` and click Play button
- No build commands needed for testing in editor

### Making Code Changes

1. Edit C# scripts in `Assets/Scripts/` directory
2. Unity will auto-recompile when you return to the editor
3. Check Unity Console window for compilation errors
4. Scripts reference Unity components (MonoBehaviour, ScriptableObject) so they must be tested in Unity

### Creating New Units

1. Create a new C# script inheriting from BaseHero or BaseEnemy
2. Create a prefab with the script component attached
3. Create a ScriptableUnit asset (right-click → Create → Scriptable Unit)
4. Assign the prefab to the ScriptableUnit
5. Place the ScriptableUnit in `Assets/Resources/Units/` so UnitManager can load it

### Creating New Abilities

1. Create a new C# script inheriting from UnitAbility
2. Add `[CreateAssetMenu]` attribute
3. Implement abstract properties and methods
4. Create ability asset via right-click menu
5. Assign to unit prefabs in inspector

## Code Conventions

- Managers use singleton pattern accessed via `Instance`
- Private serialized fields use `_camelCase` with underscore prefix
- Public properties use PascalCase
- Methods use PascalCase
- MonoBehaviour lifecycle methods (Awake, Start, Update) are used throughout
- Coroutines handle time-based operations (movement, animations)
- `NotImplementedException` indicates placeholder hooks for future features (OnMoveEnded, OnFacingChanged, etc.)
