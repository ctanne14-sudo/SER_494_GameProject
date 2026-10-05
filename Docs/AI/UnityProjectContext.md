# Unity Project Context

## Project Summary

- Project: SER_494_GameProject
- Analyzed: 2026-10-02
- Git branch: `docs/minimum-viable-product`
- Git commit: `5c44d6a`
- Current state: Early 3D third-person prototype with design documents for party and solo-combo combat directions.
- Unity version: `6000.3.24f1`
- Render pipeline: Universal Render Pipeline (URP) `17.3.0`

## Confirmed Environment

| Area | Current state |
| --- | --- |
| Unity | Unity 6.3 LTS (`6000.3.24f1`) |
| Rendering | URP with separate PC and Mobile pipeline assets |
| Input | Input System package is installed, but the current movement and camera scripts use the legacy `UnityEngine.Input` API. Both input backends are enabled. |
| Navigation | AI Navigation `2.0.14` is installed. |
| Camera | Cinemachine `3.1.7` is installed, although the current scene uses a custom camera script. |
| UI | uGUI `2.0.0` is installed. |
| Testing | Unity Test Framework `1.6.0` is installed. The shared combat foundation includes an Edit Mode test assembly and initial rule tests. |
| Target platform | Not yet confirmed. PC and Mobile URP assets exist, but this does not confirm the intended release platform. |

## Important Project Structure

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | Only enabled Build Settings scene and the current likely startup scene. |
| `Assets/Scripts/PlayerMovement.cs` | CharacterController-based walking, jumping, gravity, and respawn prototype. |
| `Assets/Scripts/CameraMovement.cs` | Third-person orbit camera prototype. |
| `Assets/Scripts/Combat/` | Engine-independent shared combat rules for stats, resources, actions, damage, and battle outcomes. |
| `Assets/Scripts/CombatSandbox/` | Temporary runtime debug interface that makes the shared combat rules playable without committing to final combat UI or turn rules. |
| `Assets/Tests/EditMode/` | Edit Mode tests for the shared combat rules. |
| `Assets/InputSystem_Actions.inputactions` | Input System action asset with Player and UI action maps. |
| `Assets/CharacterModels/Armature.fbx` | Current player character model. |
| `Assets/Settings/` | URP assets, renderers, global settings, and volume profiles. |
| `Docs/MVP/` | Current MVP design and combat-direction documents. |

## Scene and Runtime Entry Point

`Assets/Scenes/SampleScene.unity` is currently the only enabled scene in Build Settings. It contains:

- A ground plane
- A directional light
- A main camera
- An Armature character instance
- `CharacterController` and `PlayerMovement` on the player
- `CameraMovement` on the main camera

This scene is suitable as a movement experiment, but the first combat prototype should use a separate `CombatSandbox` scene so exploration work and combat experiments remain isolated.

## Current Architecture

- The exploration prototype uses a simple MonoBehaviour-based structure.
- The existing movement and camera scripts compile into Unity's default `Assembly-CSharp` assembly.
- Shared combat rules compile into the dedicated `SER494.Combat` assembly and do not depend on UnityEngine.
- There is no established combat, item, event, save, or data architecture yet.
- There is no established dependency-injection, service, or state-machine framework.

This is a good stage to introduce a small shared combat foundation without committing the project to either party combat or solo combo combat.

## Coding Conventions Observed

- Scripts currently use the global namespace.
- Serialized-field usage is not yet consistent.
- Public fields are used for some tunable movement values.
- No project-wide naming, namespace, documentation, or folder convention has been established.

Before the script count grows, the team should agree on a lightweight convention for folders, namespaces, serialized fields, and data assets.

## Recommended First Implementation Boundary

Build only the parts shared by both combat directions:

1. A separate `CombatSandbox` scene.
2. A small combatant model containing health, mana, stamina, attack, defense, and speed.
3. A generic action definition with resource costs and one or more effects.
4. A deterministic damage/effect resolver.
5. A minimal battle state and victory/defeat result.
6. Simple debug UI for selecting an action and reading the combat log.

After that foundation works, create two intentionally small experiments:

- Party experiment: two player characters versus enemies with side-based or speed-based turns.
- Solo experiment: one player character selects a prepared Combo composed of Opening, Main, optional Closing, and optional Linked Action.

The shared foundation should support comparison, not attempt to become a complete universal combat engine.

## Important Constraints and Risks

- The final combat direction is intentionally undecided.
- The current scripts use legacy input while the project already includes the newer Input System. New common work should preferably standardize on the Input System rather than deepen the mixed-input setup.
- Combo editing, inventory, equipment, Inherent Skills, advanced environmental interactions, full animation, and final visual polish should remain outside the first combat sandbox milestone.
- Scene and prefab changes need validation in the Unity Editor; text-level file inspection alone cannot confirm final runtime behavior.

## Testing and Validation

- Initial Edit Mode tests cover damage, resource costs and recovery, health boundaries, and victory/defeat conditions.
- Future tests should be added as turn order, Combo validation, status effects, and environmental reactions become concrete.
- Scene behavior should also be checked in Play Mode after every small milestone.
- The project has not been compiled or run from this Codex session, so its current Console status is not yet confirmed.

## Tooling Status

- The repository can be inspected and edited directly from Codex.
- No Unity-specific MCP/editor connection is currently available or verified in this session.
- Without that connection, Codex can still implement scripts and data files and provide precise Unity Editor steps; scene-object manipulation and Play Mode verification may require the user to perform Editor actions and report the result.
- No Unity MCP package should be installed merely for onboarding.

## Open Questions

- Which combat direction will become the final MVP direction?
- Is PC the intended first target platform?
- Should the existing movement and camera prototype be migrated immediately to the Input System?
- What coding conventions does the team want to adopt?
- Does the project currently open with a clean Unity Console?

## Sources Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Scripts/PlayerMovement.cs`
- `Assets/Scripts/CameraMovement.cs`
- `Assets/InputSystem_Actions.inputactions`
- Current `Assets`, `Docs`, and repository structure
