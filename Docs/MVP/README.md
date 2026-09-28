# SER594 Dungeon MVP Team Handoff Package

This folder contains the shared design, technical planning, diagrams, and implementation backlog for the first playable MVP.

## Documents

- [MVP Game Specification](MVP_Game_Specification.md): approved gameplay scope, rules, values, screens, and acceptance criteria.
- [Unity Technical Architecture](Unity_Technical_Architecture.md): proposed scene structure, runtime services, data boundaries, folder layout, and testing approach.
- `MVP_Development_Backlog.xlsx`: prioritized implementation tasks with dependencies, ownership fields, status tracking, and acceptance criteria.

## Diagrams

- [Core Game Loop source](Core_Game_Loop.mmd) and `Core_Game_Loop.png`
- [Combat State Machine source](Combat_State_Machine.mmd) and `Combat_State_Machine.png`
- [Stage Generation Flow source](Stage_Generation_Flow.mmd) and `Stage_Generation_Flow.png`
- [Unity Technical Architecture source](Unity_Technical_Architecture.mmd) and `Unity_Technical_Architecture.png`

The Mermaid files are the editable sources. Update them when the design changes, then regenerate the PNG files for presentations and team discussions.

## Project Baseline

- Unity Editor: 6000.3.24f1
- Render pipeline: Universal Render Pipeline 17.3.0
- Target platform: Windows desktop
- Current repository scene: `Assets/Scenes/SampleScene.unity`
- Current prototype scripts: `PlayerMovement.cs` and `CameraMovement.cs`

## Document Status

The gameplay specification is the MVP baseline approved by the team representative. The technical architecture and backlog are implementation proposals and should be reviewed by the team before tasks are assigned.
