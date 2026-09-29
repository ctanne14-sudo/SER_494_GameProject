# Unity Technical Architecture

**Project:** SER594 Dungeon Vertical Slice

**Architecture version:** 2.0 proposal

**Unity version:** 6000.3.24f1

**Render pipeline:** Universal Render Pipeline 17.3.0

## 1. Current Repository Baseline

The repository currently contains one enabled scene, `Assets/Scenes/SampleScene.unity`, a humanoid model, and two prototype MonoBehaviour scripts:

- `PlayerMovement.cs`: WASD movement, gravity, jumping, and fall reset.
- `CameraMovement.cs`: mouse-controlled third-person camera.

These scripts are a useful exploration prototype. They are not yet party, room-flow, interaction, or combat systems. The repository also includes URP, the Input System, Cinemachine, AI Navigation, uGUI, and the Unity Test Framework.

## 2. Architecture Goals

- Keep combat and progression rules separate from scene presentation.
- Keep combat in the active room so future room-specific interactions can be added without moving to a separate battle scene.
- Build rooms from authored definitions and modular content sockets.
- Store the run and party in one authoritative session model.
- Make skills, damage types, Inherent Skills, rewards, and optional environment interactions data-driven.
- Support four party members in data structures while shipping two in the slice.
- Leave D&D and follow-up systems inactive without designing the core around them.

## 3. Proposed Scene Structure

| Scene | Responsibility |
| --- | --- |
| `Bootstrap` | Creates persistent services and opens the main menu. |
| `MainMenu` | Starts a new run and exits the application. |
| `DungeonRoom` | Hosts reusable room layout, modules, exploration, encounter anchors, and in-room combat. |
| `RunComplete` | Displays the final result and returns to the main menu. |

The preferred MVP uses one reusable `DungeonRoom` scene populated from `RoomDefinition`. A boss may use a separate room scene only when unique presentation makes modular assembly impractical. Combat should not load a disconnected generic battle scene.

The existing `SampleScene` should remain untouched until the team chooses whether to convert it into `DungeonRoom` or keep it as a prototype.

## 4. Runtime Layers

### Presentation

Scenes, MonoBehaviours, character models, camera rigs, animation, audio, decals, particles, interaction prompts, and UI views.

### Application

Coordinates starting a run, entering a door, assembling a room, beginning an encounter, submitting combat commands, resolving room interactions, awarding items or Inherent Skills, retrying, and ending a run.

### Domain

Plain C# rules and mutable state for party members, equipment, skills, turn order, damage, resistances, Inherent Skills, rewards, optional environment interactions, and room progress.

### Data

ScriptableObject definitions for rooms, modules, characters, weapons, armor, accessories, skills, enemies, events, Inherent Skills, rewards, and any approved environment interactions. Mutable run state remains separate from definitions.

## 5. Core Runtime Services

| Service | Responsibility |
| --- | --- |
| `GameSession` | Owns the current `RunState` and survives scene changes. |
| `SceneFlowService` | Controls valid transitions between Bootstrap, menu, rooms, and completion. |
| `RoomFlowController` | Tracks the current room, door connections, visit state, and exit rules. |
| `RoomAssembler` | Instantiates or enables compatible room modules at authored sockets. |
| `InteractionController` | Detects the focused interaction and submits typed interaction commands. |
| `ExplorationPartyPresenter` | Displays the selected leader and swaps the exploration model when the leader changes. |
| `EncounterController` | Suspends exploration, places combatants, captures retry state, and starts combat. |
| `CameraModeController` | Blends between exploration and fixed battle cameras. |
| `CombatController` | Owns the combat state machine and validates commands. |
| `TurnOrderService` | Builds deterministic Speed-based turn order with stable tie breaking. |
| `SkillResolver` | Validates weapon requirements, targets, costs, damage, and status effects. |
| `EnvironmentInteractionService` | Optional extension point for environment rules approved by the team. |
| `InherentSkillAcquisitionService` | Evaluates event awards and post-combat learning records for character-bound passive skills. |
| `RewardService` | Applies equipment, consumables, resources, Inherent Skills, merchant, chest, event, encounter, and boss results. |

## 6. Suggested Data Types

### Mutable Runtime State

- `RunState`
- `RoomRuntimeState`
- `DoorRuntimeState`
- `PartyState`
- `CharacterRunState`
- `EquipmentLoadoutState`
- `CombatState`
- `CombatantState`
- `EnvironmentRuntimeState`
- Optional environment-interaction state required by the approved design.
- `CombatSnapshot`
- `CombatLearningRecord`

### Static Definitions

- `RoomDefinition`
- `RoomModuleDefinition`
- `DoorConnectionDefinition`
- `CharacterDefinition`
- `WeaponDefinition`
- `EquipmentDefinition`
- `SkillDefinition`
- `InherentSkillDefinition`
- `EnemyDefinition`
- `EncounterDefinition`
- `EnvironmentInteractionDefinition` when the team approves an interaction.
- `EventDefinition`
- `MerchantInventoryDefinition`

## 7. Room Assembly

`DungeonRoom` contains authored sockets rather than arbitrary procedural geometry. A `RoomDefinition` selects compatible modules for those sockets.

Recommended socket categories:

- Door.
- Encounter.
- Merchant.
- Event.
- Treasure.
- Optional Environment Content.
- Party Battle Anchor.
- Enemy Battle Anchor.

The assembler validates required sockets before the room becomes interactive. Module state is recorded in `RoomRuntimeState` so an opened chest or resolved event does not reset after combat.

## 8. Exploration and Party Presentation

The MVP instantiates only the selected leader as the controllable exploration character. Other party members exist in `PartyState` and UI, then spawn at battle anchors when combat begins.

`ExplorationPartyPresenter` should isolate this decision. A later follower implementation can replace the presentation strategy without changing party data, combat participation, or door flow.

## 9. Combat State Machine

`CombatController` is the only authority for combat phase changes:

1. Encounter Setup.
2. Camera Transition In.
3. Build Turn Order.
4. Start Turn.
5. Apply Start Effects.
6. Optional Weapon Switch.
7. Select Action and Targets.
8. Preview the action and any currently supported interaction.
9. Resolve Action.
10. Resolve an optional approved environment interaction.
11. Apply End Effects.
12. Check Outcome.
13. Advance Turn, Victory, or Defeat.
14. Camera Transition Out or Retry.

UI code submits commands. It must not directly change HP, equipment, Inherent Skills, environment state, or turn order.

## 10. Damage and Skill Resolution

Use enums or stable identifiers for six damage types and separate tag collections for weapon and interaction properties.

Resolution order:

1. Validate actor, phase, equipped weapon, skill requirements, cost, and target.
2. Calculate base damage and resistance multiplier.
3. Apply Inherent Skill and status modifiers.
4. Apply direct damage or healing.
5. Ask `EnvironmentInteractionService` to evaluate an interaction only when the team has defined one.
6. Emit ordered combat events for animation, UI, audio, and the learning record.

Presentation listens to result events. It should not reproduce the rule calculation.

## 11. Environment Interaction Extension Point

Environment interaction remains an open design area. The architecture should provide a small optional extension point rather than committing to a detailed zone, surface, spreading, or feature-state model.

The only current provisional examples are Oil + Fire and Water + Lightning. Their exact effects and final inclusion are undecided. Future environment interactions may use different data or runtime structures, so no additional rules should be treated as required until the team approves them.

## 12. Inherent Skill Architecture

`CombatLearningRecord` logs relevant facts per character rather than storing presentation text:

- Damage and weapon tags used.
- Finishing blows.
- Any approved environment interaction caused.
- Healing, guarding, and support actions.
- Event participation and outcome tags.

`InherentSkillAcquisitionService` evaluates this record against data-driven conditions at event or combat completion. Awarded Inherent Skill IDs are stored on `CharacterRunState`, never in a shared equipment inventory.

An event may instead award equipment, consumable items, resources, another benefit, or a negative consequence. The event system should use a general result model; an Inherent Skill is only one possible result type.

The MVP supports one slot per character. The collection should still be represented in a way that can later expand beyond one.

## 13. Input Policy

- Keep **Both** input backends enabled while the existing prototype uses the legacy `Input` API.
- Use the Input System for new exploration actions, interaction, menus, and combat commands.
- Do not rewrite prototype scripts until the team decides to retain them.
- Remove legacy input only after all retained gameplay uses Input Actions.

## 14. Suggested Asset Layout

```text
Assets/
  _Game/
    Art/
    Audio/
    Data/
      Characters/
      Combat/
      Enemies/
      Environment/
      Events/
      Items/
      Rooms/
      Skills/
      InherentSkills/
    Prefabs/
      Characters/
      Environment/
      Rooms/
      UI/
    Scenes/
    Scripts/
      Application/
      Domain/
        Combat/
        Environment/
        Party/
        Progression/
        Rooms/
      Infrastructure/
      Presentation/
        Combat/
        Exploration/
        Rooms/
        UI/
    Tests/
      EditMode/
      PlayMode/
```

New content should live under `Assets/_Game/`. Do not move existing prototype assets until the team agrees, because moves can create avoidable merge conflicts.

## 15. Assembly Boundaries

Assembly definitions remain optional for the first milestone. If used:

- `SER594.Domain`: Unity-light rules and state.
- `SER594.Runtime`: application services and Unity integration.
- `SER594.Tests.EditMode`: domain tests.
- `SER594.Tests.PlayMode`: scene and interaction tests.

Avoid one assembly per feature folder.

## 16. Testing Strategy

### EditMode Tests

- Room definitions select only compatible sockets and modules.
- Door transitions update the current room exactly once.
- Turn order is deterministic for Speed ties.
- Skills enforce equipped weapon tags.
- All six damage types use the correct resistance entry.
- Any retained Oil + Fire or Water + Lightning prototype follows its approved definition.
- Event results can award equipment, consumables, resources, or an Inherent Skill.
- Event Inherent Skills bind only to selected responders.
- Combat-learning Inherent Skills use the correct character record.
- Retry restores party, room, event rewards, Inherent Skills, and any approved environment-interaction state.

### PlayMode Tests

- Starting a run loads the first room and controllable leader.
- A door loads the connected room.
- An encounter blends to the battle camera and back.
- Merchant, chest, and event interactions coexist in a room.
- Boss victory opens the run-complete screen.
- Returning to the main menu clears the run.

Manual testing remains necessary for camera framing, animation timing, interaction prompts, reward presentation, and any environment interaction retained by the team.

## 17. Git Collaboration Rules

- Pull before starting work and before creating a branch.
- Use one feature branch per task or small related group.
- Avoid simultaneous edits to the same scene, prefab, or ScriptableObject.
- Commit `.meta` files with their assets.
- Do not commit `Library`, `Temp`, `Logs`, `obj`, or `UserSettings`.
- Review unexpected `ProjectSettings` changes before committing them.
- Prefer prefab modules and additive scene content over large shared-scene edits.

## 18. Recommended Implementation Order

1. Confirm folders, input policy, room ownership, and scene ownership.
2. Implement session, party state, room state, and data definitions.
3. Build one reusable room, door transition, leader exploration, and interaction framework.
4. Implement encounter transition, fixed battle camera, turn order, and basic skills.
5. Add equipment, weapon requirements, damage types, and resistance resolution.
6. Add merchant, chest, event responders, general event rewards, and Inherent Skill acquisition.
7. If the team retains them, prototype the provisional Oil + Fire and Water + Lightning examples through the optional environment extension point.
8. Connect retry, boss completion, automated tests, and acceptance testing.

## 19. Explicitly Inactive Systems

- D20 attacks, Armor Class, damage dice, saving throws, and other D&D combat resolution.
- Follow-up skill triggers and reaction chains between characters.
- Combat movement, hex grids, and pathfinding.
- Visible exploration followers.

Interfaces should remain clean enough to extend later, but no v2.0 task should implement these systems.

## 20. Team Decisions Still Required

- Whether `SampleScene` becomes the reusable `DungeonRoom` scene.
- Final two playable character concepts and proficiency profiles.
- Final room layouts and content combinations.
- Final balance values for skills, resistances, rewards, and Inherent Skill acquisition.
- Whether Oil + Fire, Water + Lightning, or different environment interactions belong in the final MVP.
- Whether the boss room requires its own scene.
