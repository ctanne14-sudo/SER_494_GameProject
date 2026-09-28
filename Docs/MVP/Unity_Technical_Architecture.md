# Unity Technical Architecture

**Project:** SER594 Dungeon MVP  
**Architecture status:** Proposed for team review  
**Unity version:** 6000.3.24f1  
**Render pipeline:** Universal Render Pipeline 17.3.0

## 1. Current Repository Baseline

The repository currently contains one enabled scene, `Assets/Scenes/SampleScene.unity`, a humanoid model, and two prototype MonoBehaviour scripts:

- `PlayerMovement.cs`: free WASD movement, gravity, jumping, and fall reset.
- `CameraMovement.cs`: mouse-controlled third-person camera.

This is a movement prototype, not the target grid-combat implementation. The model may be reused, but free movement and click-based hex movement should remain separate until the team decides whether exploration movement belongs in a later release.

The project includes URP, the Input System, Cinemachine, AI Navigation, uGUI, and the Unity Test Framework. No Unity MCP bridge, first-party assembly definition, or automated test is currently present.

## 2. Architecture Goals

- Keep gameplay rules independent from scene objects where practical.
- Store one run in a single authoritative session model.
- Make route generation, dice rolls, hex math, and combat resolution testable without loading a scene.
- Keep UI responsible for presentation and user commands, not game-rule decisions.
- Use ScriptableObjects for static definitions and plain serializable C# objects for mutable run state.
- Avoid direct dependencies between route UI and combat scene objects.

## 3. Proposed Scene Structure

| Scene | Responsibility |
| --- | --- |
| `Bootstrap` | Creates persistent services and opens the main menu. |
| `MainMenu` | Starts a new run and exits the application. |
| `RouteMap` | Displays stage choices and resolves Shop, Reward, and Event panels. |
| `Combat` | Builds the hex board and runs normal or boss combat. |
| `RunComplete` | Displays the final result and returns to the main menu. |

The existing `SampleScene` should remain unchanged until the team decides whether to keep it as a prototype or replace it with the planned scenes.

## 4. Runtime Layers

### Presentation

Unity scenes, MonoBehaviours, UI views, animations, audio, camera control, selection highlights, and visual effects.

### Application

Coordinates use cases such as starting a run, choosing a stage, entering combat, applying rewards, retrying a battle, and ending a run.

### Domain

Contains rules and state transitions for dice, statistics, stage generation, hex coordinates, pathfinding, attacks, items, events, and leveling. Domain logic should not depend on Unity scene objects.

### Data

ScriptableObject definitions for enemies, items, events, node types, and combat configuration. Mutable player and run state are stored separately in the active session.

## 5. Core Runtime Services

| Service | Responsibility |
| --- | --- |
| `GameSession` | Owns the current `RunState` and survives scene changes. |
| `SceneFlowService` | Loads scenes and controls valid transitions. |
| `StageGenerator` | Produces legal route choices for each stage. |
| `DiceService` | Generates D4, D10, and D20 results through an injectable random source. |
| `CombatController` | Owns the combat state machine and processes commands. |
| `HexGridService` | Creates the radius-3 board and resolves coordinates, distance, neighbors, and reachable cells. |
| `PathfindingService` | Finds the shortest legal path for enemy movement. |
| `RewardService` | Applies combat, reward-node, event, and shop results. |

`GameSession` should be the only persistent mutable game object. Other services may be plain C# objects owned by the bootstrap composition root.

## 6. Suggested Data Types

### Mutable Runtime State

- `RunState`
- `PlayerRunState`
- `StageState`
- `CombatSnapshot`
- `CombatState`
- `UnitCombatState`

### Static Definitions

- `EnemyDefinition`
- `WeaponDefinition`
- `ItemDefinition`
- `EventDefinition`
- `CombatConfiguration`
- `NodeDefinition`

Static definitions should use ScriptableObjects only when Inspector editing provides a clear benefit. Simple fixed MVP constants may remain in strongly typed configuration classes.

## 7. Combat State Machine

`CombatController` should be the single authority for combat phase transitions:

1. Setup
2. Roll Initiative
3. Begin Unit Turn
4. Movement Selection
5. Action Selection
6. Resolve Action
7. Check Victory or Defeat
8. End Turn
9. Result or Retry

UI buttons and hex clicks submit commands to the controller. They must not directly subtract HP, grant rewards, or change turns.

## 8. Hex Grid Model

Use axial coordinates `(q, r)` for domain logic and convert them to world positions for presentation.

Recommended domain operations:

- Generate all coordinates within radius 3.
- Return six neighbors.
- Calculate hex distance.
- Find reachable cells within movement range.
- Reject occupied or invalid destinations.
- Find the shortest path with breadth-first search or A*.

The MVP board has only 37 cells, so clarity and correctness are more important than pathfinding optimization.

## 9. Input Policy

The package `com.unity.inputsystem` is installed, while the existing prototype scripts use the legacy `Input` API. The team should use one documented policy:

- Keep **Both** input backends enabled while the prototype exists.
- Use the Input System for new UI and combat input.
- Do not rewrite the prototype scripts unless the team decides to keep them.
- Remove legacy input only after all retained gameplay uses Input Actions.

This prevents unrelated prototype cleanup from blocking MVP feature work.

## 10. Suggested Asset Layout

```text
Assets/
  _Game/
    Art/
    Audio/
    Data/
      Combat/
      Enemies/
      Events/
      Items/
    Prefabs/
      Combat/
      UI/
    Scenes/
    Scripts/
      Application/
      Domain/
        Combat/
        HexGrid/
        Progression/
        Stages/
      Infrastructure/
      Presentation/
        Combat/
        Menu/
        RouteMap/
        UI/
    Tests/
      EditMode/
      PlayMode/
```

New MVP content should live under `Assets/_Game/`. Existing prototype files should not be moved until the team agrees, because moving assets can create avoidable merge conflicts.

## 11. Assembly Boundaries

Assembly definitions are optional for the first milestone. If the team adds them, use a small structure:

- `SER594.Domain`: Unity-light rules and state.
- `SER594.Runtime`: application services and Unity integration.
- `SER594.Tests.EditMode`: domain tests.
- `SER594.Tests.PlayMode`: scene and interaction tests.

Avoid creating one assembly per folder or feature during the MVP.

## 12. Testing Strategy

### EditMode Tests

- Radius-3 grid contains 37 unique cells.
- Hex distance and neighbor calculations are correct.
- Stage choices follow exclusion and combat-repeat rules.
- Attack results compare roll plus bonus against Armor Class.
- Leveling increases maximum HP without healing current HP.
- Rewards, purchases, events, and weapon replacement apply correctly.
- Retry snapshots restore all required fields.

### PlayMode Tests

- Starting a run opens Stage 1-1.
- Completing a combat returns to route selection.
- A boss victory opens the run-complete screen.
- Returning to the main menu clears the run.

Manual testing remains necessary for selection highlights, camera framing, readable UI, and interaction timing.

## 13. Git Collaboration Rules

- Pull before starting work and before creating a branch.
- Use one feature branch per task or small group of related tasks.
- Avoid having two people edit the same `.unity`, `.prefab`, or ScriptableObject asset at the same time.
- Commit `.meta` files with their matching assets.
- Do not commit `Library`, `Temp`, `Logs`, `obj`, or `UserSettings`.
- Review unexpected `ProjectSettings` changes before committing them.
- Prefer prefab-based work and additive content over large shared-scene edits.

## 14. Recommended Implementation Order

1. Agree on folders, input policy, and scene ownership.
2. Implement domain models, dice, and `GameSession`.
3. Implement stage generation and route selection.
4. Implement the hex grid and selection feedback.
5. Implement player turns, attacks, and combat results.
6. Implement enemy AI and boss values.
7. Implement Shop, Reward, and Event nodes.
8. Connect leveling, retry snapshots, run completion, and reset.
9. Add automated tests and complete acceptance testing.

## 15. Team Decisions Still Required

- Whether `SampleScene` remains a prototype scene or becomes disposable.
- Whether the imported armature model is final for the MVP.
- Final owner of each scene and shared prefab.
- Whether the team wants assembly definitions in the first milestone.
- Whether the team wants a Unity MCP bridge for live editor automation.
