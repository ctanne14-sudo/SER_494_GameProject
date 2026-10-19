# Game Flow and Menus

Author: Anshul Kumar Sharma

## Overview

Game Flow controls how the player moves through the game: the main menu, the three floors of the tower, pausing, saving progress, and the victory screen. It connects the other systems (rooms, combat, merchant, abilities) into one playable loop.

## Game Loop

1. Main Menu: New Game or Continue
2. Floor 1: Prisoners
3. Rest stage, then stairs to the next floor
4. Floor 2: Guards
5. Floor 3: Boss
6. Victory screen, then back to the Main Menu

Progress is saved every time the player reaches a new floor.

## Scripts

Located in `Assets/Scripts/GameFlowScripts`.

| Script | Purpose |
|---|---|
| `GameManager.cs` | Tracks the current floor, enemy type per floor, gold, and saving. Stays alive between scenes. |
| `FloorExit.cs` | Stairs trigger. Sends the player to the next floor when walked into. Can be locked until the floor is cleared. |
| `FloorUI.cs` | Shows the current floor and enemy type at the top of the screen, and short messages. |
| `PauseMenu.cs` | Esc opens the pause menu: Resume, Restart Floor, Main Menu, Quit. |
| `VictoryScreen.cs` | Shown after the last floor is cleared. |
| `MainMenu.cs` | New Game, Continue (only shown when a save exists), Quit. |

## Prefabs and Scenes

| Asset | Description |
|---|---|
| `Assets/PreFabs/GameFlow/GameSystems.prefab` | GameManager, floor display, pause menu, and victory screen. Drop one into every floor scene. |
| `Assets/PreFabs/GameFlow/Stairs.prefab` | The stairs to the next floor. Place it in the rest stage of each floor. |
| `Assets/Scenes/MainMenu.unity` | Start screen of the game. First scene in Build Settings. |
| `Assets/Scenes/FloorTest.unity` | Test floor (copy of TestScene) with GameSystems and Stairs added. |

## Controls

- `Esc` pauses and resumes the game
- Walking into the stairs moves to the next floor

## Methods Other Systems Can Use

| Method | Used by | Description |
|---|---|---|
| `GameManager.Instance.NextFloor()` | Rooms, rest stage | Moves to the next floor, or shows the victory screen after floor 3. |
| `GameManager.Instance.currentFloor` | Rooms, enemies, story | Which floor the player is on (1 to 3). |
| `GameManager.Instance.CurrentEnemyName()` | AI, combat | Enemy type for this floor: Prisoners, Guards, Boss. |
| `GameManager.Instance.AddGold(amount)` | Combat, encounters | Adds gold earned after a fight. |
| `GameManager.Instance.ShowMessage(text)` | Any | Shows a short message on screen. |
| `GameManager.Instance.LoadFloor()` | Any | Reloads the current floor. |
| `GameManager.Instance.SaveGame()` | Any | Saves floor and gold. |
| `FloorExit.Open()` | Rooms, encounters | Unlocks the stairs when the floor is cleared. |

## Setting Up a Floor Scene

1. Drag `GameSystems.prefab` into the scene.
2. Drag `Stairs.prefab` into the rest stage area.
3. To lock the stairs until the floor is cleared, untick `Is Open` on the stairs and call `Open()` when the last encounter is won.
4. In the GameSystems object, set `Floor Scenes` to the scene names for floors 1, 2 and 3.
5. Add the scene to File > Build Settings.

The floor scene needs an EventSystem for the pause menu buttons. Logan's PlayerHud prefab already includes one.

## Next Steps

- Replace FloorTest with the real floor scenes from room creation
- Lock the stairs until all encounters on the floor are cleared
- Load the merchant at the rest stage before the stairs
- Unlock an ability and a memory cutscene after each floor
- Save unlocked abilities and memories along with the floor and gold
