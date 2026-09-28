# SER594 Dungeon Exploration Game

## MVP Game Design and Requirements Specification

**Version:** 1.0  
**Status:** Approved MVP baseline  
**Unity version:** 6000.3.24f1  
**Target platform:** Windows desktop  
**Game mode:** Single-player  
**Presentation:** 3D, fixed angled camera, placeholder or low-poly visuals

## 1. Product Summary

The MVP is a short dungeon run that combines a branching route-selection interface with D&D-inspired, turn-based combat on a 3D hexagonal board. A complete run contains five stages. The player manages health, gold, experience, potions, and one weapon upgrade while choosing between combat, event, reward, and shop nodes.

The MVP uses selected tabletop concepts rather than the complete D&D ruleset. It includes initiative, armor class, attack rolls, damage dice, movement, and simple ability checks.

## 2. Player Experience Goal

The player should be able to complete one full run in a short session, make meaningful route and resource decisions, and understand every combat result through visible dice rolls and a combat log.

## 3. Core Game Loop

1. Start a new run from the main menu.
2. Enter the route-selection screen.
3. Choose one available node for the current stage.
4. Resolve combat or a UI-based node.
5. Apply rewards, damage, purchases, and progression to the current run.
6. Continue until the Stage 1-5 boss battle.
7. Win the run or return to the main menu after defeat.

## 4. Run Structure

### Stage 1-1

- Always a normal combat node.
- Only one route option is displayed.

### Stages 1-2 through 1-4

- Display two different node types selected from Combat, Event, Reward, and Shop.
- If the completed node was Event, Reward, or Shop, the next stage cannot offer that same type.
- Combat is the exception and may appear in consecutive stages.
- The route is generated one stage at a time. The MVP does not require a complete Slay-the-Spire-style map.

### Stage 1-5

- Always a boss combat node.
- Only one route option is displayed.

## 5. Player Statistics

| Statistic | Initial value |
| --- | ---: |
| Maximum HP | 100 |
| Current HP | 100 |
| Armor Class | 10 |
| Gold | 10 |
| Movement | 3 hexes per turn |
| Attack range | 1 hex |
| Attack bonus | +2 |
| Base damage | 1D10 |
| Strength check bonus | +2 |
| Constitution check bonus | +2 |

## 6. Experience and Leveling

- A normal combat victory awards 5 EXP.
- The player gains one level for every 10 EXP earned.
- Leveling increases maximum HP by 5.
- Leveling does not restore current HP.
- After leveling, 10 EXP is removed and remaining EXP is retained.
- Boss EXP does not affect the current MVP run because the run ends after the boss victory.

## 7. Combat Board

- The board is rendered in 3D.
- It uses pointy-topped hexagons.
- The board has radius 3 and contains 37 hexes.
- The MVP has no terrain height, obstacles, hazards, or environmental effects.
- Each unit occupies one hex.
- The player and enemy begin on opposite sides of the board.
- Normal combat contains one enemy.
- Boss combat contains one boss.

## 8. Turn Structure

1. The player and enemy each roll 1D20 for initiative.
2. The higher result acts first.
3. Tied initiative rolls are rerolled.
4. Each turn provides one movement phase and one action.
5. A unit may move first, then attack or use a potion.
6. A unit may skip movement and use its action immediately.
7. Using an action ends the unit's turn.
8. The MVP does not support movement after an action.

## 9. Attack Resolution

An attack hits when:

`1D20 + attack bonus >= target Armor Class`

On a hit, roll the attacker's damage dice and subtract the result from the target's current HP. On a miss, no damage is dealt.

The MVP does not include critical hits, automatic failure on a natural 1, opportunity attacks, reactions, advantage, disadvantage, saving throws, or damage resistance.

## 10. Enemy Statistics

### Normal Enemy

| Statistic | Value |
| --- | ---: |
| HP | 50 |
| Armor Class | 7 |
| Attack bonus | +1 |
| Damage | 1D4 |
| Attack range | 1 hex |
| Movement | 3 hexes per turn |

### Boss

| Statistic | Value |
| --- | ---: |
| HP | 80 |
| Armor Class | 7 |
| Attack bonus | +2 |
| Damage | 2D4 |
| Attack range | 1 hex |
| Movement | 3 hexes per turn |

## 11. Enemy AI

1. Attack if the player is already within attack range.
2. Otherwise, follow the shortest legal path toward the player.
3. Move up to three hexes.
4. Attack if the player is within range after movement.
5. If the ideal destination is blocked, choose the reachable hex with the shortest distance to the player.

## 12. Potions

- A potion restores 20 HP without exceeding maximum HP.
- A potion may be used from the route-selection screen or another non-combat node.
- Using a potion during combat consumes the player's action.
- A shop may sell up to three potions per visit.
- Potions gained from rewards and events do not count against the shop limit.
- The MVP does not impose an inventory capacity.

## 13. Weapon Upgrades

The player may have one weapon upgrade at a time. A new weapon replaces the existing weapon effect.

### Accuracy Weapon

- Adds +2 to the player's attack bonus.
- The total attack bonus becomes +4.

### Damage Weapon

- Changes the player's attack damage from 1D10 to 2D10.

Weapon effects do not stack.

## 14. Shop Node

The shop is a UI-only node.

| Item | Cost | Purchase limit | Effect |
| --- | ---: | ---: | --- |
| Potion | 5 gold | 3 per shop visit | Restore 20 HP when used |
| Accuracy Weapon | 10 gold | 1 weapon purchase per visit | Attack bonus +2 |
| Damage Weapon | 10 gold | 1 weapon purchase per visit | Damage becomes 2D10 |

The player chooses which weapon to purchase. Purchase controls are disabled when the player lacks enough gold.

## 15. Reward Node

- 50% chance to receive one potion.
- 50% chance to receive one randomly selected weapon upgrade.
- A newly awarded weapon replaces the current weapon effect.

## 16. Event Nodes

Ability checks succeed when:

`1D20 + ability bonus >= DC 12`

### Event 1

| Choice | Result |
| --- | --- |
| Constitution check | Success: gain one potion. Failure: lose 10 HP. |
| Pay 5 gold | Gain one potion. |
| Leave | No effect. |

### Event 2

| Choice | Result |
| --- | --- |
| Strength check | Success: gain a random weapon. Failure: lose 10 HP. |
| Sacrifice 15 HP | Increase maximum HP by 5. Current HP is not restored. |
| Take gold | Gain 5 gold with no additional cost. |

If event damage reduces current HP to zero, the run ends and returns to the main menu.

## 17. Combat Rewards

A normal combat victory awards:

- 5 EXP
- 5 gold
- 25% chance to receive one potion

A result panel displays the rewards before the player returns to route selection. A boss victory opens the run-complete screen.

## 18. Defeat and Retry

When combat reduces the player's HP to zero, display two options:

- **Retry Battle:** restore the exact run state captured immediately before entering that combat node.
- **Return to Main Menu:** abandon the current run and clear all run progress.

The retry snapshot includes HP, maximum HP, EXP, gold, potion count, and weapon effect.

## 19. Required Screens

1. Main Menu
2. Route Selection
3. Combat HUD
4. Shop
5. Reward
6. Event
7. Normal Combat Result
8. Defeat
9. Run Complete

The combat HUD displays player HP, enemy HP, Armor Class, EXP, gold, potion count, current weapon, active turn, remaining movement, and a readable combat log.

## 20. Controls and Feedback

- Use mouse input for selecting route nodes, UI choices, hexes, enemies, and actions.
- Highlight legal movement destinations.
- Highlight enemies that are currently within attack range.
- Reject illegal selections without consuming movement or an action.
- Display initiative, attack rolls, hit or miss results, damage rolls, healing, and rewards in readable UI feedback.

## 21. Runtime State

The current run must retain:

- Stage number
- Previously completed node type
- Current and maximum HP
- Armor Class
- EXP and level
- Gold
- Potion count
- Active weapon effect
- Shop purchase counts for the current visit
- Pre-combat retry snapshot

Leaving the application or returning to the main menu clears the run. Persistent save and load are outside the MVP.

## 22. MVP Acceptance Criteria

The MVP is complete when a player can:

1. Start a new run from the main menu.
2. Progress from Stage 1-1 through Stage 1-5.
3. Receive valid route choices that follow the generation rules.
4. Move on a 37-hex board using legal hex movement.
5. Complete player and enemy turns without invalid overlapping actions.
6. Resolve initiative, attack rolls, Armor Class, damage, healing, victory, and defeat correctly.
7. Complete Shop, Reward, and both Event nodes.
8. Retain run statistics between scenes and nodes.
9. Retry a failed battle from the correct pre-combat state.
10. Defeat the boss and reach the run-complete screen.
11. Return to the main menu and begin a clean new run.

## 23. Out of Scope

- Multiplayer
- Persistent save files
- Character creation or classes
- Full D&D rules
- Spells, equipment inventory, or multiple weapons
- Multiple enemies in one battle
- Terrain height, obstacles, hazards, or line of sight
- Procedural 3D dungeon exploration
- Advanced animation, cinematic sequences, voice acting, or final art
- Mobile, console, or web builds
- Monetization or online services
