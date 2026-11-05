# Lightled

A turn-based 2D tactics game made in Unity. A small squad of heroes pushes through tight corridors while enemies keep arriving as hidden contacts. There are no hit points: one successful attack kills any unit. Each move, turn and shot has to be planned.

The game also serves as a demonstrator for a **Director AI**. This system measures how much pressure the player is under and adjusts enemy behaviour, reinforcements and dice odds to keep each mission tense without making it hopeless.

> **Status:** in active development. This is a student project: expect rough edges and placeholder content.

<!-- Add a screenshot or GIF here, e.g. ![Lightled gameplay](Docs/screenshot.png) -->

---

## Features

- **Facing-based movement.** Every unit faces a direction. Moving forward or sideways is cheaper than moving backward, and turning costs action points (AP).
- **Cone line of sight.** Heroes see in a cone that widens with distance. Walls cast real angular shadows and burning tiles block vision.
- **Overwatch.** A hero can lock in their current view cone and automatically shoot at any enemy that moves into it during the enemy turn.
- **Blips and Ghouls.** Enemies enter as *Blips*, unrevealed tokens that each hide 1–3 *Ghouls*. Blips sneak around the heroes' sight lines and reveal themselves when they get close.
- **Dice combat.** Melee, ranged and flamethrower attacks are resolved with d6 rolls.
- **Command Points.** A shared pool (6 per turn) that any hero can spend to cover missing AP.
- **Several objectives.** Escape through the target zone, carry an artifact out, or burn a target with the flamethrower.
- **Director AI.** A pressure model that adjusts the game while you play (see [below](#director-ai)).
- **ASCII level system.** Levels are drawn as plain text inside ScriptableObjects, so one gameplay scene runs every level.

## Getting started

### Requirements

- **Unity 6000.2.11f1** (Unity 6). Other Unity 6 versions will probably work but haven't been tested.
- Packages are resolved automatically from `Packages/manifest.json`: Universal Render Pipeline, Input System, uGUI and TextMesh Pro.

### Running the game

1. Clone the repository:
   ```bash
   git clone https://github.com/blbstejn/Lightled.git
   ```
2. In Unity Hub, choose **Add → Add project from disk** and select the cloned `Lightled` folder.
3. Open the project. The first import can take a few minutes.
4. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
5. Choose **Play** and pick a level.

To test a single level directly, open `Assets/Scenes/TestScene.unity`, assign a level asset to the GridManager's `Current Level` field and press Play.

## How to play

Each round has a **hero turn** followed by an **enemy turn**. At the start of every hero turn, all heroes get their AP back (4 each) and the shared Command Point pool refills.

| Input | Action |
|---|---|
| Left click on a hero | Select the hero |
| Left click on a tile | Move or attack, depending on the target |
| Ability buttons | Turn left, turn right, Overwatch |
| WASD / arrow keys | Pan the camera |
| Middle or right mouse drag | Pan the camera |
| Mouse wheel | Zoom |
| Esc | Pause menu |
| F1 / F2 / F3 | Debug overlays: combat log, enemy AI log, Director AI |

**Combat**

- **Melee:** both sides roll d6s and keep their highest die. Heroes roll 2 when attacking and 1 when defending, Ghouls roll 3. The higher result wins.
- **Ranged:** roll 2d6. A 6 on either die hits (5+ with sustained fire).
- **Flamethrower:** roll 1d6 for every unit in the area. Each unit dies on a 2+. The area keeps burning and blocks line of sight.

**Winning and losing:** each level sets its own objective (escape, artifact extraction or flamethrower target). You lose when no heroes are left on the board and the objective hasn't been met.

### Levels

| # | Level | Objective |
|---|---|---|
| 1 | Suicide Mission | Hit the target with the flamethrower |
| 2 | Decoy | Escape |
| 3 | The Artifact | Pick up the artifact and escape with it |
| 4 | Pitfall | Escape |

Prototype and test layouts are in `Assets/Resources/Levels/Prototype/`.

## Director AI

The Director continuously measures a **pressure score** from 0 to 1, built from six weighted signals:

| Signal | Weight | What it measures |
|---|---|---|
| Proximity | 0.30 | Shortest walkable path from any enemy to any hero |
| Hero loss | 0.25 | Share of the squad that has died |
| Outnumbered | 0.15 | Number of active enemies per living hero |
| Hidden threats | 0.10 | Unrevealed Blips moving on the board |
| Combat intensity | 0.10 | Recent fighting; decays over time |
| Exposure | 0.10 | Average number of enemies within reach of each hero |

The score is grouped into four levels: **Low**, **Moderate**, **Elevated** and **Critical**. Other systems ask the Director for recommendations; it never changes their state directly. It can:

- make Blip AI bolder or more cautious (aggression multiplier ×1.3 to ×0.4)
- skip some reinforcement spawns (30% chance at Elevated, 60% at Critical)
- move spawn positions closer to or further from the heroes
- adjust Ghoul melee dice (2–4) and the heroes' ranged hit threshold

Grid rules, movement costs and line of sight are never touched. Press **F3** in play to see all values live.

More detail:
- [SystemDescriptions.md](SystemDescriptions.md): enemy AI pipeline, line of sight and Overwatch, level encoding (English)
- [Stellungnahme_DirectorAI.md](Stellungnahme_DirectorAI.md): full write-up of the Director AI implementation and design decisions (German)

## Creating levels

Levels are `LevelData` ScriptableObjects in `Assets/Resources/Levels/`. Create one via **Create → Game → Level Data** and draw the map in the `Grid Rows` field:

```
##1.....B###
#....A......#
#.....T..B..#
##2.........#
#############
```

| Char | Meaning |
|---|---|
| `.` | Floor |
| `#` | Wall (blocks movement and line of sight) |
| ` ` | Void (outside the map) |
| `1`–`5` | Hero spawn for that hero slot |
| `B` | Blip spawn / enemy entry point |
| `A` | Artifact pickup |
| `T` | Target zone (escape exit or flamethrower target) |

Then set the objective (`Victory Condition Type`, `Required Escapees`, `Requires Artifact`) and add the asset to the `Available Levels` list on the `MainMenuManager` in the MainMenu scene. The asset checks itself in the Inspector and logs a warning if something is misconfigured.

## Project structure

```
Assets/
├── Scenes/            MainMenu, TestScene (the gameplay scene)
├── Scripts/
│   ├── AI/            Enemy behaviours (Blip, Ghoul) and BFS pathfinding
│   ├── Abilities/     Move, melee, ranged, flamethrower, turn, Overwatch
│   ├── Managers/      GameManager, GridManager, UnitManager, EnemyAIManager, DirectorAI, UI
│   ├── Units/         BaseUnit → BaseHero / BaseEnemy and concrete units
│   ├── Tiles/         Floor, wall, void, spawn, target and fire tiles
│   ├── Debug/         F1–F3 debug overlays
│   └── Utils/         Direction helpers, tooltips, scaling
├── Resources/         Levels, units, abilities and lore loaded at runtime
├── Prefabs/
└── Sprites/
```

Architecture in brief: managers are singletons (`GameManager.Instance` and so on). `GameManager` runs a state machine (grid generation → spawning → hero turn → enemy turn → victory or defeat). Units, abilities and levels are data-driven ScriptableObjects.

## Credits

<!-- List the team, art/audio sources and any third-party assets here. -->
Made by Leon.

## License

<!-- Choose a license before publishing, e.g. MIT for code. If the art, lore or audio may not be reused, say so separately. -->
No license has been chosen yet. Until one is added, all rights are reserved.
