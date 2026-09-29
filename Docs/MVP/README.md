# SER594 Dungeon Vertical Slice — v2.0 Team Package

This folder contains the English design baseline, technical proposal, diagrams, and implementation backlog for the revised 3D-room dungeon vertical slice.

## Documents

- [MVP Game Specification](MVP_Game_Specification.md): gameplay scope, party model, combat rules, progression, and acceptance criteria.
- [Unity Technical Architecture](Unity_Technical_Architecture.md): proposed scenes, runtime services, data boundaries, folder layout, and testing approach.
- `MVP_Development_Backlog_v2.xlsx`: prioritized tasks with dependencies, suggested roles, status tracking, and acceptance criteria.

## Diagrams

- [Core Game Loop source](Core_Game_Loop.mmd) and `Core_Game_Loop_v2_current.png`
- [Combat State Machine source](Combat_State_Machine.mmd) and `Combat_State_Machine_v2_current.png`
- [Room Composition Flow source](Room_Composition_Flow.mmd) and `Room_Composition_Flow_v2_current.png`
- [Environment Reaction Flow source](Environment_Reaction_Flow.mmd) and `Environment_Reaction_Flow_v2_current.png`
- [Character Progression Flow source](Character_Progression_Flow.mmd) and `Character_Progression_Flow_v2_current.png`
- [Unity Technical Architecture source](Unity_Technical_Architecture.mmd) and `Unity_Technical_Architecture_v2_current.png`

The Mermaid files are editable sources. Regenerate the PNG files after changing a diagram.

## Project Baseline

- Unity Editor: 6000.3.24f1
- Render pipeline: Universal Render Pipeline 17.3.0
- Target platform: Windows desktop
- Current repository scene: `Assets/Scenes/SampleScene.unity`
- Current prototype scripts: `PlayerMovement.cs` and `CameraMovement.cs`

## Scope Decisions in v2.0

- Exploration takes place in connected 3D rooms. Doors replace the old DAG route-selection UI.
- Rooms can combine enemies, events, a merchant, treasure, resources, and environmental features.
- Combat occurs in the current room with a fixed battle camera, turn-based commands, and no grid or movement command.
- The playable slice begins with two party members. Runtime data should support four.
- Exploration displays only the selected party leader in the MVP. Visible follower characters remain a presentation option for a later version.
- Selectable active skills and character-bound acquired Inherent Skills are in scope.
- Oil + Fire and Water + Lightning are provisional environment-interaction examples. Their final behavior and inclusion remain open to team discussion.
- D&D-derived combat rules and the follow-up attack system are retained design ideas, but they are not active systems in v2.0.

## Document Status

Version 2.0 is a working baseline for team review. Balance values and final content names may change, but implementation should not reintroduce v1 route-map, hex-grid, D&D attack-roll, or follow-up requirements unless the team explicitly changes scope.
