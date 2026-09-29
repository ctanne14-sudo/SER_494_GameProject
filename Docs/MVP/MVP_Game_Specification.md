# SER594 Dungeon Exploration Game

## MVP Vertical Slice Game Design and Requirements Specification

**Version:** 2.0

**Status:** Working baseline for team review

**Unity version:** 6000.3.24f1

**Target platform:** Windows desktop

**Game mode:** Single-player

**Presentation:** 3D third-person room exploration with a fixed battle camera

## 1. Product Summary

The vertical slice is a short dungeon expedition through connected 3D rooms. The player explores as a party leader, enters doors to reach new rooms, interacts with mixed room content, and controls a party in classic turn-based battles. Combat uses no tactical grid and provides no movement command.

Rooms are compositions rather than exclusive types. One room may contain enemies, a merchant corner, an event, a chest, resources, and possible environment content at the same time. Combat remains in the room where the encounter began so approved room-specific interactions can be added later without requiring a separate battle scene.

## 2. Experience Goals

- Make entering a new room feel uncertain and worth observing.
- Let party composition, weapon loadouts, active skills, and acquired Inherent Skills create different character roles.
- Keep combat and progression systems flexible enough to support future mechanics that the team has not finalized.
- Deliver one short, complete run that demonstrates exploration, combat, rewards, progression, and a boss conclusion.

## 3. Vertical Slice Scope

The first playable slice contains:

- Three connected 3D rooms, including a final boss room.
- Two playable party members; runtime data structures support up to four.
- Two normal enemy definitions and one boss definition.
- Two to three doors per non-final room.
- At least one merchant interaction, one chest, and one event across the run.
- Two active skills and one initial passive or identity feature per playable character.
- Two weapon slots, one armor slot, and one accessory slot per character.
- Slashing, Blunt, Piercing, Fire, Ice, and Lightning damage types.
- Oil + Fire and Water + Lightning as provisional environment-interaction examples, subject to team revision.
- At least one event-granted Inherent Skill and one combat-learning Inherent Skill.
- Exploration-to-combat and combat-to-exploration camera transitions.

## 4. Core Game Loop

1. Start a new run and initialize the party.
2. Enter a 3D room through a door.
3. Explore, inspect, and interact with available room content.
4. Trigger and resolve any encounter in the current room.
5. Collect treasure, trade, resolve events, and apply progression.
6. Choose an available exit door.
7. Continue until the boss room is cleared or the party is defeated.
8. Display the run result and return to the main menu.

## 5. Room Model

Each visited node is represented as a playable 3D room. A room is assembled from a base layout plus optional modules rather than assigned one exclusive gameplay type.

### Required Room Elements

- Entry point.
- Two or more door sockets when the room is not final.
- Exploration bounds and camera bounds.
- Encounter trigger area.
- Party and enemy battle anchors.
- Optional sockets for future room-specific interactions.

### Optional Room Modules

- Enemy group.
- Merchant corner.
- Event interaction.
- Chest or resource pickup.
- Environmental feature.
- Rest or recovery point.
- Lore or non-reward interaction.

A `RoomDefinition` determines which compatible modules are enabled, their placement sockets, door connections, encounter data, and environmental configuration. A boss room may use a dedicated scene if its presentation cannot fit the reusable room scene.

## 6. Door and Run Progression

- Doors are physical navigation choices, not UI-only route nodes.
- A door displays a readable interaction prompt when the leader is in range.
- Locked or unavailable doors clearly show why they cannot be used.
- Entering a valid door records the current room as cleared or visited and loads the connected room.
- The vertical slice may use authored room connections. Full procedural dungeon generation is not required.
- Returning through previous doors is optional and may be disabled for the first slice.

## 7. Party and Exploration Presentation

### MVP Presentation

- Only the selected party leader appears and moves during room exploration.
- The party roster remains visible in the HUD.
- Entering combat places all active party members at battle anchors.
- Changing the leader outside combat changes the exploration model and interaction character.

This avoids follower pathfinding, door congestion, animation synchronization, and teleport correction during the first slice.

### Retained Presentation Option

Showing the other party members following the leader, similar to a visible RPG party, remains a future presentation option. It is not required by v2.0.

## 8. Character Identity Model

Character identity combines a fixed core with a flexible loadout.

### Fixed or Character-Bound Elements

- Base statistics and growth tendencies.
- Weapon proficiency values and growth caps.
- One identity passive or unique feature.
- Acquired Inherent Skills, which cannot be transferred to another character.

### Player-Configurable Elements

- Active skills owned by the party.
- Two equipped weapons.
- Armor and accessory.
- Consumable assignment.

Most basic weapons should remain equippable even at low proficiency. Proficiency modifies effectiveness and may unlock advanced weapons or additional skill effects. This preserves experimentation while keeping characters meaningfully different.

## 9. Weapons and Active Skills

- Each character has an Active Weapon slot and a Secondary Weapon slot.
- Every weapon has one or more tags, such as Sword, Mace, Bow, Staff, Melee, Ranged, Heavy, or Magic Focus.
- An active skill declares the weapon tags it requires.
- A skill is unavailable when neither equipped weapon satisfies its requirement.
- The character may switch the active weapon once at the beginning of their turn without spending the action.
- Only the active weapon needs to be visibly held in the MVP. Full body-mounted display of every carried weapon is retained for later presentation work.

## 10. Inherent Skills

An **Inherent Skill** is a character-bound passive skill. It cannot be equipped by or transferred to another character and becomes part of that character's run history.

### Event Acquisition

1. An event may ask the player to select one or more responding characters.
2. The event resolves using those characters and the selected choice.
3. Its result may award equipment, consumable items, resources, an Inherent Skill, another benefit, or a negative consequence.
4. When the result is an Inherent Skill, it binds to the responding character who receives it.

### Combat Learning

1. Combat records meaningful actions by character, including damage type, weapon tag, finishing blows, reactions caused, and support actions.
2. The result phase checks Inherent Skill learning conditions.
3. Eligible characters may receive an Inherent Skill from the matching pool.
4. The reward screen explains the behavior that led to the skill.

### MVP Limits

- Each character has one Inherent Skill slot in the vertical slice.
- The slice demonstrates at least one event acquisition and one combat-learning acquisition.
- Acquisition chances and thresholds are data-driven.
- The test configuration may guarantee the first eligible acquisition so the feature can be demonstrated reliably.

### Example

**Execution Insight: Slashing Lv1**

Possible condition: the character repeatedly delivers finishing blows with Slashing attacks.

Effect: deal 10% more Slashing damage to targets at or below 25% HP.

## 11. Combat Presentation

- An encounter begins in the current room.
- Exploration input is suspended.
- The camera blends from free third-person view to an authored fixed battle view.
- Party members and enemies use predefined battle anchors.
- The room geometry, visible features, and current environment states remain present.
- After victory, the camera returns to exploration view and cleared enemies remain inactive.

## 12. Turn Structure

1. Build a deterministic turn order from each combatant's Speed and a stable tie-break rule.
2. Start the next living combatant's turn.
3. Apply start-of-turn statuses and any approved encounter effects.
4. Allow an optional active-weapon switch.
5. Select one valid action: active skill, item, defend, or available environment action.
6. Select valid targets or currently supported interactables.
7. Preview costs, targets, damage type, and any currently defined interaction.
8. Resolve the action and any currently supported environment interaction.
9. Apply end-of-turn effects.
10. Check victory or defeat, then continue the queue.

There is no movement phase, movement command, hex grid, position pathfinding, attack roll, or Armor Class check in v2.0 combat.

## 13. Damage and Resistance Model

### Damage Types

| Family | Types |
| --- | --- |
| Physical | Slashing, Blunt, Piercing |
| Elemental | Fire, Ice, Lightning |

### Delivery and Weapon Tags

Damage type and delivery method are separate. A Piercing attack may be Melee or Ranged. A Fire skill may require a Staff or may be delivered through a Fire-tagged weapon.

Prototype damage uses a stable formula:

`Final Damage = round(Base Power × Skill Multiplier × Type Multiplier × Situational Modifiers)`

Resistance data determines the Type Multiplier. Equipment, proficiency, Inherent Skills, statuses, and other future mechanics may contribute situational modifiers. Exact balance values remain editable data and should not be hardcoded in presentation scripts.

## 14. Provisional Environment Interaction Examples

Environment interaction remains an open design area. The current discussion provides only two provisional examples:

- Oil + Fire.
- Water + Lightning.

Oil or Water may be present in a room or may be applied through a skill. The exact effects, targeting rules, duration, presentation, and final inclusion in the MVP have not been decided. The design and technical structure should remain open to changing, replacing, expanding, or removing these examples after further team discussion.

## 15. Room Activities

### Merchant

- Appears as an interactable corner or character inside a room.
- Opens a trade panel without changing to a dedicated shop node.
- The MVP supports a small authored stock list.

### Chest and Resources

- A chest or pickup is opened in the room.
- Rewards are applied to the run state and shown in a result panel.
- A room may contain treasure even when it also contains combat or an event.

### Events

- Events occur through room interactions and dialogue choices.
- Some choices require selecting responding characters.
- Outcomes may award equipment, consumable items, resources, an Inherent Skill, another benefit, or a negative consequence. Inherent Skills are one possible event result, not the default or exclusive reward.

## 16. Victory, Defeat, and Retry

- A normal victory returns control to room exploration after the result panel.
- Boss victory opens the run-complete result.
- Party defeat offers Retry Encounter or Return to Main Menu.
- Retry restores the snapshot captured immediately before combat, including party state, loadouts, consumables, Inherent Skills, encounter state, and room interaction state.

## 17. Runtime State

The active run retains:

- Current room and door connection history.
- Room module and interaction states.
- Party roster and selected leader.
- HP, statistics, proficiencies, equipment, active skills, and Inherent Skills for every character.
- Inventory and run resources.
- Cleared encounters, opened chests, completed events, and merchant state.
- Any environment-interaction state required by the final approved design.
- Pre-combat snapshot.

Persistent save and load across application restarts are outside the vertical slice.

## 18. Required Screens and UI States

1. Main Menu.
2. Exploration HUD and interaction prompts.
3. Party and loadout screen.
4. Combat HUD and action selection.
5. Merchant panel.
6. Chest or reward panel.
7. Event dialogue and responder selection.
8. Combat result and Inherent Skill learning result.
9. Defeat and retry panel.
10. Run-complete screen.

## 19. MVP Acceptance Criteria

The v2.0 vertical slice is complete when a player can:

1. Start a run with two party members.
2. Explore three connected 3D rooms and choose physical doors.
3. Encounter mixed content without every room being assigned one exclusive type.
4. Enter battle through a camera transition and return to exploration after victory.
5. Control both party members in deterministic turn-based combat with no movement command.
6. Equip two weapons and use only active skills supported by equipped weapon tags.
7. Resolve all six damage types and data-driven resistances.
8. Resolve events that can produce different result categories, including equipment or consumable items.
9. Acquire a non-transferable Inherent Skill through an event or combat-learning condition.
10. Use a merchant, open a chest, and complete an event.
11. Retry a failed encounter from the correct pre-combat state.
12. Defeat the boss and reach the run-complete screen.

## 20. Retained but Inactive Design Systems

The following ideas are retained for later evaluation but do not exist as active v2.0 systems:

- D&D-derived combat rules, including D20 attack rolls, Armor Class, damage dice, saving throws, and tabletop initiative rolls.
- Follow-up skills that trigger outside the owner's normal turn.
- Follow-up chains, trigger priority, per-round limits, and reaction queues.
- A visible follower party during room exploration.
- Four simultaneously playable party members in shipped vertical-slice content.
- Full weapon display on the character's back and waist.
- Additional or alternative environment interactions. The final environment system remains open to future team decisions.

Code may leave clean extension points for these ideas, but the backlog must not include their implementation as required work.

## 21. Out of Scope

- Multiplayer or online services.
- Persistent save files.
- Procedural generation of complete room geometry.
- Combat grids, movement commands, pathfinding, or free positioning during battle.
- Full D&D rules.
- Follow-up attacks.
- More than three playable rooms in the first slice.
- Advanced follower AI.
- Final art, voice acting, cinematic production, or large content libraries.
- Mobile, console, or web builds.
