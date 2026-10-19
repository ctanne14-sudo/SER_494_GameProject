# Simultaneous Timeline Combat Rules

## Document Status

- Version: discussion draft, October 10, 2026
- Direction: solo-character Combo combat with simultaneous timeline resolution
- Purpose: record the rules currently agreed upon by the team discussion
- Scope: combat rules and the retained pre-combat Combo rules; this is not yet a final balance document or implementation specification

Rules explicitly marked **provisional**, **reserved**, or **open** are not final. Numerical values should remain configurable unless the document states otherwise.

## Core Combat Direction

The player controls one character and prepares a limited set of Combos before battle. In the MVP, the player prepares four Combos. During each Round, the enemy commits an Action Sequence and reveals its intent, the player selects one available Combo, and both submissions resolve on a shared timeline.

The timeline is initially generated from the player's Combo and the enemy's Action Sequence, but it is dynamic. Charge, Action Disruption, restarts, and cancellations may move or remove events while the Round is resolving.

### Intended Combat Asymmetry

Combat deliberately gives the two sides different advantages:

- the player has the information advantage because Enemy Intent is revealed before committing a Combo;
- enemies have the raw-combat advantage and may be substantially stronger in direct statistics, damage, durability, or action pressure;
- the player is expected to overcome stronger opponents through preparation, interpretation of Intent, timing interactions, and resource management;
- the player should have room to accept an occasional enemy action instead of answering every threat perfectly;
- taking one or two uncountered attacks should usually create recoverable attrition rather than immediate defeat, although especially dangerous attacks may still demand a response;
- healing and other recovery resources help convert imperfect defense into a longer-term tactical cost.

This is a direction for encounter design and balance, not a fixed numerical ratio. The intended challenge is informed tactical planning against a stronger opponent, rather than requiring flawless reactions to a sequence of individually lethal attacks.

## Terminology

| Term | Meaning |
| --- | --- |
| Round | One complete cycle in which both sides submit a Combo and resolve it on the shared timeline. |
| Combo | A prepared sequence containing a required Main Action and optional supporting Actions. |
| Opening Action | An optional Action before the Main Action. |
| Main Action | The required central Action of a Combo. |
| Closing Action | An optional Action after the Main Action. |
| Linked Action | One optional Action attached after the Opening, Main, or Closing Action. |
| Timeline Cost | The positive number of Time Steps occupied by an Action. |
| Local Time | Progress measured inside one Action. |
| Global Time | The current Time Step on the Round timeline. |
| Resolution Point | One Time Step at which an instantaneous effect resolves. |
| Resolution Window | A range of candidate Time Steps from which one Resolution Point is selected. |
| Active Window | A duration during which an effect remains active. |
| Enemy Intent | The enemy Action Sequence information revealed during the Planning Phase. |
| Action Disruption | A motion-control effect that changes how an Action proceeds. |
| Super Armor | A temporary level of resistance that negates or downgrades Action Disruption. |

## Combo Construction

A valid Combo follows these rules:

- Main is required.
- Opening and Closing are optional.
- A Combo may contain no more than one Linked Action.
- Linked may be placed after Opening, Main, or Closing.
- Linked has its own Timeline Cost, Mana, Stamina, and Cooldown contribution.
- Linked contributes to the Combo Timeline Cost limit.
- Every Action has a Timeline Cost of at least 1.
- The current Base Combo Timeline Cost Limit is 30 and must remain configurable.
- Total Mana Cost cannot exceed the character's Max Mana during construction.
- Total Stamina Cost cannot exceed the character's Max Stamina during construction.
- Current Mana and Stamina are checked only when the Combo is selected in battle.
- One owned Action can be assigned to only one prepared Combo at a time.
- The MVP uses four prepared Combos. A future version may use four to six.
- Default names remain simple, such as `combo1` and `combo2`, until the player renames them.

The normal execution order is:

```text
Opening -> Main -> Closing
```

Linked is inserted immediately after its host:

```text
Opening -> Linked -> Main -> Closing
Opening -> Main -> Linked -> Closing
Opening -> Main -> Closing -> Linked
```

Linked becomes eligible only when its host Action successfully reaches the end of its Local Timeline. Pause, Resume, and Restart delay Linked. Cancel or timeline timeout cancels Linked. Once Linked begins, it is an independent Action.

## Enemy Action Sequences

Enemy and player Actions use the same underlying timeline language. Enemy Actions may define:

- Timeline Cost and Local Time;
- fixed Resolution Points;
- Active Windows;
- Damage, Guard, Evade, Buff, Debuff, and other effects;
- Disruption Response Segments and other timeline interactions.

Enemy Actions do not use Resolution Windows and never require a Resolution Timing QTE. Their initial effect timing is authored as fixed Resolution Points.

Enemy sequences differ from player Combos in the following ways:

- an enemy sequence contains one or more ordered Actions but has no Opening, Main, Closing, or Linked slot categories;
- enemy Actions do not use the player's Action Cooldown values;
- enemies do not pay Mana or Stamina to submit a sequence;
- the MVP therefore does not restrict enemy selection through the player's Cooldown or resource rules;
- an enemy-specific pacing, availability, or resource system may be designed later, but it must remain separate from the player's system.

For the MVP, each enemy owns several pre-authored Action Sequences and a conditional behavior table. The table determines which sequences are eligible by evaluating information such as:

- the enemy's current HP or phase;
- the player's current HP, status, or other relevant state;
- the sequence used in the previous Round;
- encounter-specific conditions.

Restrictions based on the previous sequence are behavior conditions, not the player's Cooldown system. They may either make a sequence ineligible for the next Round or reduce its selection weight.

After filtering, the enemy uses weighted random selection among the eligible sequences. Each sequence owns a configurable selection weight. The selection occurs before Intent is revealed and does not change afterward. Automated tests may provide a fixed random seed so the result is reproducible.

Every enemy must also define one unconditional Fallback Sequence. If no ordinary sequence is eligible, the enemy uses this Fallback. It may contain multiple Actions and does not have to be a basic attack; its requirement is that it is always legal and prevents the enemy from having no valid submission.

At Round Start, the enemy selects and locks its complete sequence. It cannot replace that choice after the Intent is revealed. Later effects may delay, restart, cancel, or otherwise modify the locked sequence through normal dynamic-timeline rules, but the enemy does not reselect a different sequence.

## Round Structure

Each Round uses the following phases:

1. **Round Start**
   - Resolve Round Start effects.
   - The enemy selects and locks its complete Action sequence.
2. **Planning Phase**
   - The HUD shows a simplified Enemy Intent as a horizontal preview of the enemy's initial planned timeline.
   - The preview uses guide marks every 5 Time Steps.
   - Different semantic colors mark intervals ending at planned Resolution Points for Damage, Guard or Evade, self-Buffs, and player-targeting Debuffs. For example, Damage resolving at Time Step 5 marks the interval from 4 to 5 in the Damage color.
   - A Debuff that belongs to a Damage effect's On Hit package does not create a second marker in the simplified HUD. The HUD shows only the Damage marker; the detailed Intent shows the attached Debuff.
   - These colors are presentation defaults and may be changed with the UI theme as long as the four categories remain distinguishable.
   - The player may inspect the enemy for detailed Intent and combat information. The detailed Intent lists the sequence's Action names and order, exact planned Resolution Points, effect categories, Tags, and targets. An enemy sequence contains one or more Actions but does not use the player's Opening, Main, Closing, and Linked slot categories.
   - Detailed Intent does not reveal the full Damage formula or the enemy AI's internal eligibility rules, priorities, or weights.
   - The player may inspect their own status.
   - The player chooses one available Combo.
3. **Combo Commit**
   - The selection becomes final and cannot be changed.
   - The player pays required resources. The enemy does not use the player's resource-cost system.
   - Initial timelines are generated for both sides.
4. **Timeline Resolution**
   - Both sides advance on the same timeline.
   - Resolution Points, Active Windows, QTEs, Charge, and Action Disruption are processed.
   - The schedule may change dynamically.
5. **Round End**
   - Player Combo Cooldowns advance.
   - Player Mana and Stamina recover according to the current battle configuration.
   - Round-based status durations advance.
   - If combat continues, the next Round begins.

The player cannot voluntarily Wait while any Combo is executable. If no Combo can be executed because of Cooldown or insufficient resources, the player must Wait. Waiting advances the Round, permits the enemy to act, advances Cooldowns, and applies normal resource recovery.

## Cooldown

The current provisional formula is:

```text
Combo Cooldown = sum of all Action Cooldown values in the Combo
```

Cooldown semantics are:

- Cooldown 0: the Combo is available again in the next Round.
- Cooldown 1: the Combo is unavailable in the next Round and becomes available after that Round ends.
- Cooldown 2: the Combo is unavailable for the next two Rounds.
- Higher values follow the same rule.

Cooldown is applied when a Combo is committed. Interrupted or cancelled Actions do not refund resources or remove the Combo Cooldown.

## Timeline Limits and Completion

- The Base Combo Timeline Cost Limit is currently 30.
- The Base Combo Timeline Cost Limit may be expanded later.
- A Resolution Point at Time Step 30 is valid.
- The timeline ends early when both sides have no remaining Actions, events, or relevant Active Windows.
- Charge and Action Disruption may extend the actual Round beyond the original Combo Timeline Cost.
- The Absolute Timeline Limit is 100.
- Resolution Points moved beyond 100 do not resolve.
- Active Windows are truncated at 100.
- An unfinished Action at 100 is cancelled as `Timed Out`.
- Later Actions belonging to that character are cancelled.
- Paid resources and Cooldown are not refunded.

A normal Time Step may eventually correspond to approximately 0.2 seconds of presentation time. This value is provisional and does not affect deterministic rule calculations.

## Action Timeline Model

An Action is not required to use fixed Startup, Execution, and Recovery phases. Instead, every Action owns a freely authored Local Timeline from 1 through its Timeline Cost.

An Action may contain:

- one or more Resolution Points;
- a Resolution Window;
- one or more Active Windows;
- Charge definitions;
- Damage Effects and Damage Components;
- Buff, Debuff, Guard, Evade, or other effects;
- Disruption Response Segments covering its full Local Timeline.

Global scheduling is calculated from cumulative Timeline Cost. Linked Action time is also cumulative from all preceding Actions.

When an Action is delayed, its Local Time does not change meaning. A marker authored at Local Time 6 remains Local Time 6 even if its Global Time moves from 15 to 18.

## Resolution Points, Windows, and Mutual Exclusion

A Resolution Point represents an instantaneous effect, including:

- Damage;
- Guard;
- Evade;
- Apply Buff;
- Apply Debuff.

A Resolution Window is a set of possible Resolution Points. A QTE selects one point from the window. Changing the selected point does not change the Action end time or the schedule of later Actions.

For the same character, Damage, Guard, and Evade are mutually exclusive and may never overlap at the same Time Step. This is a hard rule.

Validation uses the conservative interpretation:

- every candidate Time Step in a Resolution Window must be legal;
- every tick of a Sustained Attack must be legal;
- a Damage Window that includes a Guard or Evade point is invalid even if the player might select a different point;
- an Active Defensive State may overlap Damage because it is not an instantaneous Guard or Evade point.

## QTE Rules

QTE is the presentation and input layer for a `Timing Interaction`; it is not the underlying combat rule.

- The Global Timeline pauses during a QTE.
- Character Actions and combat events pause with it.
- The QTE UI continues using real, unscaled time.
- Active Window duration does not decrease while the Global Timeline is paused.
- Every QTE has an Input Time Limit.
- QTEs do not necessarily have success or failure; they may simply select different outcomes.
- Some Actions require no QTE.

### Resolution Timing QTE

- The Resolution Window is represented as a divided dial or an equivalent interface.
- Each section corresponds to one possible Resolution Point.
- The player stops the indicator to select the actual point.
- If the player gives no input before timeout, the minimum value of the Resolution Window is selected.

### Charge QTE

- The QTE becomes available before the uncharged Resolution Point.
- Pressing and holding starts a repeating charge gauge.
- The gauge contains `Maximum Charge Count + 1` sections, including Charge Level 0.
- Releasing selects the section currently under the gauge.
- Each Charge QTE has a Maximum Hold Time.
- If the player never participates, Charge Level 0 is selected.
- If Maximum Hold Time forces an automatic release, Charge Level 0 is selected regardless of gauge position.
- Each Charge Level defines its own modifications. Possible changes include extra Timeline Cost, power, tags, effect strength, Resolution Points, and Active Window duration.
- Extra Action time created by Charge inherits the Interruption Response of the Local Time at which charging began.
- Charge may move the current Action and all later Actions, but cannot exceed the Absolute Timeline Limit.

## Same-Time-Step Resolution

Same-time events should be uncommon, but the engine resolves them in this order:

1. Collect every effect at the current Time Step.
2. Resolve immediate interactions such as Guard, Evade, and Clash.
3. Calculate every remaining Damage Effect.
4. Apply calculated Damage simultaneously.
5. Apply Action Disruption attached to successful hits.
6. Apply Buffs, Debuffs, and other status changes.
7. Reschedule all future events affected by the result.

An ordinary Buff applied at the current Time Step does not retroactively alter Damage at that same Time Step. Guard, Evade, and Clash are explicit same-time interaction mechanics and are exceptions.

Action Disruption applied at a Time Step cannot cancel effects that already resolved at that same Time Step. It affects only later events.

## Hit Result and Effect Dependencies

Effects are classified as:

- `On Hit Effect`: occurs only if the associated hit succeeds;
- `On Damage Dealt`: occurs only if the hit deals positive Final Damage;
- `Independent Effect`: does not depend on the associated hit.

Guard and Evade make the entire associated On Hit Effect package fail, not only its Damage.

If ordinary calculation reduces Final Damage to 0, the result is still a Hit. On Hit Effects may still occur, while On Damage Dealt Effects do not.

## Guard

- Guard exists at a Resolution Point.
- Every Damage Resolution Point is guardable by default.
- `Unguardable` and `Overcommit` each bypass Guard. Overcommit does not also need the Unguardable tag.
- Guard blocks every otherwise guardable Damage Effect against that character at the same Time Step.
- Guard can block melee and ranged Damage.
- Guard does not inherently require a Melee tag.
- Ignores Evade has no direct interaction with Guard. Ignores Evade Damage may be either guardable or unguardable according to the normal rules above.
- Every blocked Damage Effect may define an optional `On Guarded Response` for its attacker.
- Melee attacks commonly define an On Guarded Response such as self-Flinch or self-Stagger.
- Ranged attacks commonly have no On Guarded Response even though their Damage is blocked.
- An Overcommit attack bypasses Guard.
- If multiple blocked Damage Effects create multiple Disruptions, normal same-time Disruption rules select the most severe result.

## Evade

- Evade exists at a Resolution Point.
- It avoids attacks resolving against the character at exactly the same Time Step.
- A successful Evade cancels the entire On Hit Effect package.
- Independent Effects remain valid.
- Damage tagged Ignores Evade ignores Evade.
- An Overcommit attack that is Evaded may apply its authored self-penalty.

## Defensive State

A Defensive State is normally an Active Window. It reduces specified kinds of Damage during its active duration rather than granting complete immunity.

Damage modifiers may be scoped to:

- all Damage;
- a Damage family such as Physical or Magical;
- a specific Damage type such as Slashing, Blunt, Piercing, Fire, Ice, or Lightning.

Scope determines whether a modifier is applicable. All applicable attacker Damage Dealt modifiers and defender Damage Taken modifiers enter the same additive multiplier bucket.

## Clash

Clash represents the idea that an attack can also be a defense.

A Damage Effect tagged Clash may define:

- Clash Damage Reduction percentage;
- an optional Clash Super Armor Level.

When opposing Damage Effects resolve at the same Time Step:

- each side's outgoing Damage still resolves normally;
- a side with Clash reduces the incoming Damage from that simultaneous attack by its authored percentage;
- that side receives its authored Super Armor at that Time Step;
- if both sides have Clash, both receive their own reduction and Super Armor.

Clash does not subtract one attack's power from the other. Guard and Evade do not overlap a Damage Point for the same character, so a character cannot simultaneously attack and actively Guard or Evade at that Time Step.

## Sustained Attack

A Sustained Attack uses an Active Window plus authored hit timing:

- First Hit Offset;
- Hit Interval;
- Maximum Hit Count.

Example:

```text
Active Window: 8-16
First Hit: 8
Hit Interval: 2
Maximum Hit Count: 5
Generated Damage Points: 8, 10, 12, 14, 16
```

Every generated Damage Point is an independent On Hit Effect and is separately checked against Guard, Evade, Clash, and other rules.

## Action Disruption

The MVP supports three standard Action Disruption types:

```text
Flinch < Stagger < Knockdown
```

Launch and Restrain are reserved for future systems involving aerial Actions and restraint rules.

Disruption has a duration measured in Time Steps. If multiple Disruptions arrive at the same Time Step:

- keep only the most severe type;
- if multiple instances share that type, use the longest duration;
- do not add their durations together.

### Super Armor

Super Armor affects Disruption as follows:

| Level | Result |
| --- | --- |
| Lv1 | Negates Flinch. |
| Lv2 | Negates Flinch; downgrades Stagger to Flinch. |
| Lv3 | Negates Flinch and Stagger. |
| Lv4 | Negates Flinch and Stagger; downgrades Knockdown to Stagger. |
| Lv5 | Negates Flinch, Stagger, and Knockdown. |

Downgrading changes only the Disruption type. Duration remains unchanged. Future Buffs may modify duration separately.

### Disruption Response Segments

Every Action divides its complete Local Timeline into contiguous, non-overlapping Disruption Response Segments. No Local Time may be uncovered.

Each segment may define a different response for Flinch, Stagger, and Knockdown. Super Armor is applied first; the Action then looks up the response for the resulting Disruption type at its current Local Time.

The first implementation supports:

- `Continue`: the Action schedule does not change, although other attached effects may remain;
- `Pause and Resume`: pause for the Disruption duration, then continue from the same Local Time;
- `Restart Action`: after the Disruption duration, restart from Local Time 1;
- `Cancel Action`: remove the remaining Action, then begin the next eligible Action after the Disruption duration.

Pause, delay, or restart moves the current Action and all later Actions. Cancelling an Action may allow later Actions to begin earlier than their original schedule, but only after the Disruption duration.

On Restart:

- effects that already resolved are not undone;
- those effects may resolve again when the restarted Action reaches them;
- QTEs occur again;
- previous Charge choices are cleared;
- Mana and Stamina are not paid again;
- Combo Cooldown is not applied again;
- the Absolute Timeline Limit prevents an infinite restart loop.

## Active Window Runtime Rules

When an Active Window begins, it becomes a runtime effect with a Global Time duration. Its remaining duration normally continues while a character is being disrupted. QTE pauses the entire Global Timeline and therefore also pauses this duration.

Each Active Window selects one Disruption Policy:

| Policy | Behavior |
| --- | --- |
| Persist Through Disruption | Remains active and continues losing duration during Disruption. |
| Break on Disruption | Ends immediately when any effective Interrupt occurs. |
| Suppress During Disruption | Becomes inactive during Disruption while duration continues; reactivates if time remains after all Disruptions end. |

Pause and Resume, Restart Action, and Cancel Action count as effective Interrupts. Continue does not. A Disruption fully negated by Super Armor does not. A downgraded Disruption is evaluated using its downgraded type and the current Response Segment.

If multiple interruptions overlap, a Suppressed effect remains suppressed until every effective interruption has ended. If it expires first, it does not reactivate.

The current MVP stacking rule for receiving the same named Active Window again is `Refresh Duration`. The duration resets; separate instances do not stack.

## Buff and Debuff Duration

A Buff or Debuff uses one of two duration models:

1. **Time Step Duration**
   - Remaining duration decreases as the Global Timeline advances.
   - If duration remains when a Round ends, the effect carries into the next Round with that remaining duration.
   - It resumes counting down when the next Timeline Resolution advances.
   - Planning, Combo Commit, and a QTE pause do not consume Time Step duration because the Global Timeline is not advancing.
2. **Round Duration**
   - Remaining duration decreases at Round End.
   - The effect is removed at the end of the Round in which its remaining duration reaches zero.
   - Therefore, an effect applied with a duration of one Round remains active for the rest of the current Round and is removed at that Round End.

An individual effect must explicitly select its duration model. These rules describe duration only; the effect's trigger and Disruption Policy remain separately authored.

## Damage Components and Formula

One Damage Effect may contain multiple Damage Components. For example, a burning sword attack may contain separate Slashing and Fire components. Each component applies the modifiers matching its own family and type, and the resulting component values are added.

For each ordinary Damage Component:

```text
Normal Component Damage =
    Action Multiplier
    * Relevant Weapon Attack
    * max(0, 1 + sum of Attack modifiers)
    * C / (C + max(0, Base Defense * (1 + sum of Defense modifiers)))
    * max(0, 1 + sum of applicable Damage Dealt modifiers
                + sum of applicable Damage Taken modifiers)
    * clamp(1 - Clash Damage Reduction, 0, 1)
```

The MVP rules are:

- ordinary Defense affects every non-Reaction Damage Component;
- classifications such as All, Physical, Magical, Slashing, and Fire determine which modifiers apply;
- all applicable Damage Dealt and Damage Taken modifiers are added in the same multiplier bucket;
- positive and negative modifiers are symmetric;
- a multiplier cannot become negative;
- Clash is an independent reduction multiplier;
- no Critical Hit system is included in the MVP;
- the Defense constant `C` belongs to Battle Configuration and is 100 for the MVP.

The damage precision rule is:

- keep full internal precision through the calculation;
- display diagnostic values to three decimal places when useful;
- round only the final result to an integer;
- use ordinary half-away-from-zero rounding;
- Final Damage cannot be negative.

### Initial MVP Sample Values

The first balance pass uses the following configurable player baseline:

- Weapon Attack: 50;
- Base Defense: 50;
- ordinary Action Multiplier range: 30% to 70% (`0.30` to `0.70`).

With `C = 100` and no other modifiers, these values produce approximately 10 to 23 ordinary Damage before final rounding. These are prototype starting values rather than permanent balance limits.

## Elemental Reaction Damage

Elemental reactions are completely excluded from the first MVP implementation. The first version has no environment-reaction surfaces, triggers, or Reaction Damage, and the ordinary damage resolver does not reserve an active reaction calculation step.

Elemental Reaction Damage remains a future design space for environment interactions. Oil + Fire and Water + Lightning remain provisional examples only. Trigger classifications, formulas, resistance, and their relationship to ordinary Damage will be designed when this feature enters scope.

## Tags

Current combat tags include:

| Tag | Current meaning |
| --- | --- |
| Overcommit | Bypasses Guard. If Evaded, it may apply an authored Action Disruption to its user. |
| Super Armor | Grants the authored Disruption resistance level during its active time. |
| Ignores Evade | Its Damage ignores Evade. |
| Unguardable | Makes a Damage Resolution Point bypass Guard. It is unnecessary when that point already has Overcommit. |
| Clash | Grants authored simultaneous-attack Damage reduction and optional Super Armor. |

The tag system is expected to expand. Exact compatibility and validation rules should remain data-driven.

## Battle Outcome

- The player wins when all enemies are defeated.
- The player loses when the player character is defeated.
- If the player and the last enemy are defeated by the same Time Step, the result is **Mutual Destruction**. The result screen shows neither Victory nor Defeat.
- Mutual Destruction still grants the rewards earned for defeating the enemy.
- The defeated enemy is also committed to persistent world state as dead. If the player's death returns them to an earlier save point, returning to the encounter location does not respawn that enemy.
- The player's normal death penalty and return behavior still apply.

## Retained Future Rules Outside the Timeline MVP

The following concepts remain part of the larger Solo Combo design but are not required for the first timeline implementation:

- limited in-battle replacement of one Action in one Combo;
- a real replacement may reset the modified Combo's Cooldown, while a no-op change may not;
- a post-battle prompt may keep or discard combat-time Combo changes;
- equipment or Inherent Skills may increase reconfiguration charges;
- Action acquisition through manuals, scrolls, trainers, events, observation, or breakthroughs;
- duplicate known Actions converting to another reward;
- Launch, Restrain, aerial states, and restraint systems;
- detailed weapon proficiency, equipment, and Inherent Skill integration;
- D&D-inspired checks or damage rules;
- Pursuit mechanics;
- advanced environment-object interaction;
- final animation and presentation polish.

## Remaining Open Decisions

Important unresolved areas include:

- any future enemy-specific pacing or availability system beyond the MVP conditional weighted-selection table;
- final QTE time limits and visual implementations;
- other advanced timeline-preview presentation beyond the first simplified HUD;
- final MVP feature cut and implementation milestones.

