# Complex Systems Overview

## 1. Enemy AI Pipeline — Turn Orchestration and Blip Behavior

**Files:** [EnemyAIManager.cs](Assets/Scripts/Managers/EnemyAIManager.cs), [BlipBehavior.cs](Assets/Scripts/AI/BlipBehavior.cs), [PathfindingUtils.cs](Assets/Scripts/AI/PathfindingUtils.cs)

The enemy AI is not a single script but a layered pipeline that runs each enemy turn through three distinct phases — spawn, refresh, and action — with the Blip behavior sitting at the center as the most decision-heavy piece.

### Turn Orchestration (EnemyAIManager)

`EnemyAIManager` drives the entire enemy turn as a coroutine, which allows it to pace out actions visually and wait on movement animations before moving to the next enemy.

**Phase 1 — Spawn:** The number of blips to spawn ramps up each turn (`initialBlipsPerTurn + (turn - 1) × blipRampPerTurn`, capped at `maxBlipsPerTurn`). Before each individual spawn, the Director AI can probabilistically veto it based on current player pressure, giving the system granular control over threat escalation.

**Phase 2 — AP Refresh:** All living enemies get their action points reset via `StartNewTurn()`.

**Phase 3 — Action:** Enemies are sorted before acting — closest to any hero first, with Blips (Enemy2) before Ghouls as a tiebreaker. This ordering ensures ambush units set up position before melee units commit to attacks. Each enemy then loops through `DecideNextAction()` until it runs out of AP or explicitly returns a Wait action. Pressure is recalculated after every individual action, so later enemies in the queue use up-to-date game state.

### Blip Behavior — Six-Priority Decision Tree

`BlipBehavior.DecideAction()` is where the real complexity lives. Blips are hidden ambush units that must balance staying concealed, closing distance, and triggering at the right moment. Their decision-making runs through six ordered priorities:

1. **Ambush** — If adjacent to any hero, reveal immediately (spawn a Ghoul).
2. **Fight or Flight (while visible)** — If the blip is currently in hero LOS, it checks its aggression level. Above the charge threshold it rushes forward anyway; below it, it tries to escape to a safe (out-of-LOS) tile; if no escape exists, it charges anyway.
3. **Charge (while hidden, close range)** — If within effective charge range (base ambush distance + `aggressionFactor × 4` bonus tiles), the blip closes in. Whether it ignores the hero's facing direction depends on the `ignoreFacingThreshold`.
4. **Advance / Sneak** — Aggressive blips take the direct BFS path; cautious ones use only LOS-safe tiles, and only if the safe path actually makes forward progress.
5. **Risky move** — If still stuck, and the hero isn't facing toward it (or aggression exceeds the risk threshold), it pushes through LOS anyway.
6. **Last resort** — Any walkable step toward the nearest hero.

**Aggression** is the key variable threading through all these decisions. It starts at a base value and scales up with the number of active blips on the field (`baseAggression + (1 - base) × activeBlips / fullAggressionBlipCount`). More blips on the board means each individual blip behaves more boldly — an emergent pack dynamic. The Director AI then applies a final multiplier on top, pulling aggression down when the player is already under heavy pressure.

Pathfinding throughout is BFS-based (not Manhattan) so blips correctly navigate around walls rather than phasing through them.

---

## 2. Line of Sight — Angular Shadow Casting and Overwatch

**Files:** [BaseHero.cs](Assets/Scripts/Units/Heroes/BaseHero.cs), [OverwatchAbility.cs](Assets/Scripts/Abilities/UIAbilities/OverwatchAbility.cs), [OverwatchManager.cs](Assets/Scripts/Managers/OverwatchManager.cs)

Line of sight determines what heroes can see and shoot. Overwatch then builds a reactive interrupt system on top of it. Together they are both the most algorithmically precise and the most tactically central feature in the game.

### LOS — Angular Shadow Casting

`GetLineOfSightTiles()` in `BaseHero` implements a **shadow-angle cone expansion** algorithm rather than a simple raycasting or Manhattan flood-fill. The reason: a flood-fill would let heroes see around corners and through thin diagonal gaps, while pure raycasting per-tile is expensive and misses partial occlusion.

The algorithm works like this:

1. Start from the hero's tile and expand outward depth-by-depth in the hero's forward direction. The lateral width at each depth is `shootConeWidth × depth`.
2. For every candidate tile at the current depth, compute its **angular center** (`offset / depth`) and its **angular footprint** (`(offset ± 0.5) / depth`).
3. Check the tile's center against a list of accumulated **shadow intervals** — angle ranges already blocked by opaque tiles at shallower depths. If the center falls inside any shadow, the tile is skipped.
4. If the tile is not shadowed and it blocks LOS (void tile, or a tile that `FireEffectManager` reports as on fire), add its angular footprint as a new shadow interval. If it's open and walkable, add it to the visible result set.
5. Stop when an entire depth layer produces no tiles at all — the cone has left the grid.

This correctly handles partial occlusion: a wall at depth 2 only shadows the narrow angle directly behind it, not the full row at depth 3. Fire tiles integrate naturally — the `FireEffectManager` check runs inline so a burning tile dynamically shrinks hero vision mid-combat without any special wiring.

The width-grows-with-depth geometry also means the cone physically spreads: a hero looking down a corridor sees a narrow beam, while an open room floods with visible tiles quickly.

### Overwatch — Cached LOS Interrupt

Overwatch converts the cone calculation into a stateful reaction mechanic:

1. When a hero activates Overwatch, `EnterOverwatch()` calls `GetLineOfSightTiles()` once and **freezes** the result in `OverwatchLOSTiles`.
2. During the enemy turn, `OverwatchManager` checks every enemy movement: if the destination tile is inside any hero's cached LOS set, that hero fires a reaction shot before the move completes.
3. Overwatch breaks if the hero takes any other action, or automatically at the start of their next turn via `StartNewTurn()`.

The caching decision is deliberate and creates the core tactical tension: heroes commit to a fixed field of view when entering Overwatch. If the enemy approaches from a different angle than expected, the hero won't react. Players must think carefully about facing direction before activating — turning to re-angle the cone would cost AP and break Overwatch. The system rewards spatial prediction over reflexes.

The interaction with fire is an interesting edge case: if a tile inside the cached LOS set catches fire after Overwatch is activated, the enemy can move through it without triggering a reaction (the live LOS would have excluded that tile, but the cache still includes it). This is a minor known discrepancy between cached and live state.

---

## 3. ASCII Level Data — One Scene, Infinite Levels

**Files:** [LevelData.cs](Assets/Scripts/LevelData.cs), [GridManager.cs](Assets/Scripts/Managers/GridManager.cs)

The level system is what makes the game practical to build with a small team: a single gameplay scene handles every level in the game, and adding or modifying a level requires zero Unity scene editing. A designer works entirely inside a ScriptableObject asset using plain text.

### The Encoding

Each `LevelData` asset contains a `gridRows` string array — one string per row of the grid, read top-to-bottom. Every character in those strings maps directly to a tile type and, in some cases, to game objects placed on that tile:

| Character | Result |
|---|---|
| `.` | Walkable tile |
| `#` | Wall (blocks LOS and movement) |
| ` ` | Void (empty space, not part of the playfield) |
| `1`–`5` | Hero spawn point (numbered, one per hero slot) |
| `B` | Blip spawn point (enemy reinforcement entry) |
| `A` | Artifact pickup (walkable tile + artifact object) |
| `T` | Target area (victory condition zone) |

A complete level layout looks like this in the Inspector:

```
##1.....B###
#....A......#
#.....T..B..#
##2.........#
#############
```

That single text block defines spawn positions, the artifact location, the escape zone, enemy entry points, and the full wall geometry simultaneously.

### How GridManager Consumes It

`GenerateGrid()` iterates the string array and calls `LevelData.GetTileType(x, y)` for each cell. One detail worth noting: Y=0 in the string array is the top of the map visually, but Unity's world space has Y increasing upward. The generator flips the Y coordinate during instantiation (`worldY = height - 1 - y`) so the layout reads correctly in the Inspector without designers having to think in inverted coordinates.

After the tile pass, a second phase spawns `ArtifactPickup` objects on any tiles flagged as `ArtifactSpawn`. This keeps tile and object spawning decoupled — the tile is a plain walkable surface; the artifact is a separate game object parented above it.

The `GetHeroSpawnTile(int heroIndex)` and `GetBlipSpawnTile()` accessors then serve as the single source of truth for where units appear, so `UnitManager` never needs to know the level geometry directly.

### Victory Conditions Encoded in the Asset

Each `LevelData` also declares its own win condition via the `victoryConditionType` field:

- **Escape** — Heroes must reach `T` tiles. `requiredEscapees` sets how many must make it out, and `requiresArtifact` gates the win on carrying the `A` pickup.
- **FlamethrowerTarget** — Any flamethrower shot that hits a `T` tile triggers an instant win.

This means two levels can have completely different objectives — one an extraction mission, one a demolition run — without any code changes or scene variants. The `Validate()` method, called automatically by Unity whenever the asset is edited in the Inspector (`OnValidate`), checks for mismatches like a FlamethrowerTarget level with no `T` tiles, so misconfigured levels fail loudly at design time rather than silently at runtime.

### Why This Matters for the Team

The whole system means the game only needs **two scenes** — the main menu and the gameplay scene. Every level is a `.asset` file that a designer can duplicate, rename, and modify in minutes. The Inspector tooltip on `gridRows` is a built-in cheat sheet listing every character and what it does, so no external documentation is required to author a level. Validation catches layout errors immediately on save. And because `LevelData` is a `ScriptableObject`, levels can be referenced, inspected, and passed between systems like any other Unity asset without serialization overhead.
