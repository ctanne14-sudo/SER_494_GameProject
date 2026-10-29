# Unity Project Structure and Migration Rules

This document defines the target Unity project layout and the safety rules used while migrating the existing project. The migration is organizational only: gameplay, save data, scene behavior, public IDs, and serialized references must remain unchanged.

## Target layout

```text
Assets/
  _Project/
    Art/
    Content/
      Actions/
      Encounters/
      Equipment/
      UI/
    Editor/
    Prefabs/
    Scenes/
      Application/
      Exploration/
      Prototype/
    Scripts/
      Combat/
      CombatAuthoring/
      CombatIntegration/
      Exploration/
      PlayerState/
      UI/
  Tests/
    EditMode/
    PlayMode/
  ThirdParty/
  TextMesh Pro/
```

This is a destination model. Existing folders are migrated in small batches rather than moved all at once.

## Ownership rules

### First-party content

Assets created or maintained by the project team belong under `Assets/_Project/`.

Examples:

- production and prototype scenes;
- game-specific scripts and editor tools;
- authored Action, Encounter, Equipment, and UI data;
- team-created prefabs, materials, models, animations, and textures.

### Tests

Automated tests remain under `Assets/Tests/`. Test assemblies must not be included in runtime assemblies.

### Third-party content

Imported vendor packages belong under `Assets/ThirdParty/<VendorOrPackage>/` and should retain their internal folder structure.

- Move a complete vendor root rather than separating its scripts, materials, and prefabs.
- Keep the license or attribution file with the vendor root.
- Do not edit vendor source unless a project-specific patch is necessary and documented.
- `Assets/TextMesh Pro/` is a Unity-managed essential-resources exception and may remain at its conventional path.
- Assets with uncertain authorship are not moved until their origin and license are identified.

## Migration safety rules

1. **Preserve every `.meta` file.** An asset and its existing `.meta` move together. Existing GUID values must never be regenerated or copied to another asset.
2. **Use Unity-aware moves.** Perform asset moves through the Unity Project window or `AssetDatabase.MoveAsset`. Do not move Unity assets externally while the Editor is open.
3. **One concern per migration commit.** Do not combine scene moves, third-party moves, and code refactors in one commit.
4. **Do not refactor while moving.** Namespace, type, field, GameObject, asset, and scene names remain unchanged during an organizational move.
5. **Update path-based consumers in the same batch.** Build Settings, editor tools, tests, documentation, and any string-based asset paths must point to the destination path before the batch is complete.
6. **Preserve stable gameplay identifiers.** Action IDs, equipment IDs, encounter instance IDs, world-object IDs, save keys, scene names, and loadout IDs do not change because their files moved.
7. **Scene file names remain stable.** Runtime code currently loads several scenes by name. Directory changes must not rename those scenes.
8. **No incidental scene saves.** Only moved scenes should change path. Their serialized scene contents should remain byte-for-byte unchanged unless Unity must perform a documented migration.
9. **Review the complete Git diff.** Every moved asset must show its `.meta` moving with it. Unexpected scene, prefab, Project Settings, or package changes stop the batch.
10. **Revert the whole batch on broken references.** Do not patch around missing scripts or lost references until the move itself has been ruled out.

## Migration batches

### Batch 1: Scene consolidation

Status: completed on 2026-10-28. All eight scene files and their `.meta` files retained their pre-move SHA-256 hashes. Unity compilation succeeded, 258 Edit Mode tests passed, and 3 Play Mode smoke tests passed.

| Current path | Destination path |
|---|---|
| `Assets/Scenes/TitleMenu.unity` | `Assets/_Project/Scenes/Application/TitleMenu.unity` |
| `Assets/_Project/Scenes/Floor1_Cell_A.unity` | `Assets/_Project/Scenes/Exploration/Floor1_Cell_A.unity` |
| `Assets/_Project/Scenes/Floor2_Lobby_A.unity` | `Assets/_Project/Scenes/Exploration/Floor2_Lobby_A.unity` |
| `Assets/_Project/Scenes/Floor3_Boss_A.unity` | `Assets/_Project/Scenes/Exploration/Floor3_Boss_A.unity` |
| `Assets/Scenes/TestScene.unity` | `Assets/_Project/Scenes/Prototype/TestScene.unity` |
| `Assets/Scenes/ComboBuilderSandbox.unity` | `Assets/_Project/Scenes/Prototype/ComboBuilderSandbox.unity` |
| `Assets/Scenes/CombatSandbox.unity` | `Assets/_Project/Scenes/Prototype/CombatSandbox.unity` |
| `Assets/Scenes/FloorTest.unity` | `Assets/_Project/Scenes/Prototype/FloorTest.unity` |

Known path-based consumers that must be updated:

- `ProjectSettings/EditorBuildSettings.asset`;
- `Assets/Editor/SoloCombo/ComboBuilderPrototypeSceneCreator.cs`;
- `Assets/Editor/SoloCombo/CombatIntegrationDemoSceneCreator.cs`;
- `Assets/Editor/PersistentHealingPotionPickupSceneCreator.cs`;
- `Assets/_Project/Scripts/Editor/RoomBuilder.cs`;
- `Assets/Tests/EditMode/CombatIntegration/AuthoredContentContractTests.cs`.

`ProjectSettings/ProjectSettings.asset` currently contains a stale `templateDefaultScene` path to `Assets/Scenes/SampleScene.unity`. It should be reviewed separately rather than silently changed as part of the move.

### Batch 2: Confirmed third-party roots

| Current path | Destination path |
|---|---|
| `Assets/BTM_Assets/` | `Assets/ThirdParty/BTM_Assets/` |
| `Assets/TreasureChestMaker/` | `Assets/ThirdParty/TreasureChestMaker/` |

The complete package root and root `.meta` must move together. Package demos remain with their package unless the entire demo is deliberately removed in a separate commit.

Compatibility note: Treasure Chest Maker contains editor-only code with fixed `AssetDatabase` paths to its original package root. After moving the package with its `.meta` files intact, those path strings are updated to `Assets/ThirdParty/TreasureChestMaker/`. This is a documented project compatibility patch; runtime assets and their GUID-based references are unchanged.

Status (2026-10-28): completed. Both package roots and root `.meta` files were moved through Unity's `AssetDatabase`; pre-move file counts and content fingerprints were preserved before applying the compatibility patch above. Unity reimport and compilation succeeded, followed by 258/258 passing Edit Mode tests and 3/3 passing Play Mode smoke tests.

### Batch 3: Template and recovery cleanup

Review and, if unused, remove:

- `Assets/Readme.asset`;
- `Assets/TutorialInfo/`;
- `Assets/_Recovery.meta`.

Also clear the stale `templateDefaultScene` value in `ProjectSettings/ProjectSettings.asset`; it referenced the removed template scene at `Assets/Scenes/SampleScene.unity` and does not control the project's Build Settings or runtime title-scene flow.

Status (2026-10-28): completed after confirming that none of the template asset GUIDs had external references. The URP template welcome asset, tutorial editor files, template layout and artwork, and orphaned recovery metadata were removed. The stale template default-scene value was cleared without changing Build Settings or authored scenes.

### Batch 4 and later: Code organization

Code splits occur only after the asset layout is stable. They preserve namespaces and public behavior and are covered by the existing tests before any API redesign is considered.

## Validation gate for every batch

Before the batch:

- repository working tree is clean;
- Unity Console has no compile errors;
- exact Edit Mode and Play Mode test totals/results are recorded;
- current Title -> Exploration -> Combat -> Exploration flow is known to work.

After the batch:

- Unity opens without Safe Mode;
- Console contains no new errors;
- no scene or prefab reports missing scripts;
- Build Settings contains the intended scenes in the same order;
- all Edit Mode tests pass with the same expected total;
- all Play Mode smoke tests pass with the same expected total;
- New Game and Load Game reach the correct exploration spawn;
- Exploration Menu and Combo Configuration behave unchanged;
- exploration can enter and leave Combat Sandbox correctly;
- checkpoint save, load, defeat rollback, Mutual Destruction, persistent pickup, and save deletion behave unchanged;
- Git diff contains only the planned moves and necessary path updates.

## Rollback rule

Each batch must be committed separately only after validation. If a batch fails validation, restore that whole uncommitted batch through GitHub Desktop rather than attempting additional migrations on top of a broken baseline.
