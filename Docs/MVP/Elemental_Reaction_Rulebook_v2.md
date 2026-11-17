# Elemental Reaction Rulebook v2

## Document Status

- Version: 2.0
- Date: October 17, 2026
- Scope: Solo Combo Combat MVP
- Status: Approved design specification; implementation is intentionally deferred until the physical weapon combat MVP is complete
- Related document: [Simultaneous Timeline Combat Rules](Solo_Combat_Idea/Simultaneous_Timeline_Combat_Rules.md)

This document is the authoritative English specification for the current elemental damage, tag, status, environment, and reaction rules. Older elemental design documents are historical references only. If an older document conflicts with this rulebook, this rulebook takes precedence.

## 1. Design Vocabulary

The system continues to use **Tag** as the shared player-facing term because Damage Tags, Status Tags, and Environment Tags are intended to interact with one another.

| Category | Purpose | Examples |
|---|---|---|
| Damage Tag | Describes the nature of a Damage Effect. | Slashing, Blunt, Piercing, Fire, Ice, Water, Lightning |
| Status Tag | Describes a state currently affecting a combatant. | Frigid, Hypothermia, Soaked, Off-Balance |
| Environment Tag | Describes the current battlefield environment. | Damp, Dry, Cold, Hot, Miasma |
| Reaction Property | Supplies a reactive quality to a combatant, object, or environment. | Flammable, Metallic, Explosive, Toxin |

Damage Tags describe what a hit is. Reaction Properties describe what can react. A Status or Environment may provide one or more Reaction Properties without becoming the same data type as those properties.

## 2. Levels, Instances, and Duration

### 2.1 Levels

- Leveled Tags and Properties use Levels I through V.
- Runtime data stores these Levels as integers from 1 through 5.
- Player-facing UI uses Roman numerals.
- Every Reaction Property listed in this rulebook has a Level unless a future rule explicitly declares it level-less.
- A Damage Tag normally receives its Level from its Action.
- A temporary enchantment may add a Damage Tag or replace its Level with a higher Level. A single Damage Effect never contains two copies of the same named Damage Tag.

### 2.2 Status instances

- Statuses are **Multiple** by default. Multiple instances coexist and all applicable passive modifiers stack additively.
- A Status explicitly marked **Single** may have only one active instance.
- Applying an existing Single Status does not replace or refresh it.
- If a Reaction would create a Single Status that already exists, the Reaction fails and consumes none of its inputs.
- Each successfully created Status, Environment, or Reaction Property instance receives an immutable `Applied Sequence` representing when it entered the battle state.
- UI sorting never changes `Applied Sequence` and therefore never changes combat results.

### 2.3 Duration

- A Time Step duration continues across Round boundaries until all of its Time Steps are consumed.
- A Round duration is removed at the end of its final Round. A one-Round Status is removed at the end of the Round in which it is applied.
- When Round and Time Step durations must be compared for input selection, one Round is treated as 30 Time Steps.
- Effects do not extend a Round merely to finish their remaining duration. Unspent Time Step duration carries into the next Round.

### 2.4 Default input selection

When a Reaction must select one instance from multiple eligible inputs:

1. Select the highest Level.
2. If Levels are equal, select the instance with the shortest remaining duration.
3. If both are equal, select the instance with the earlier `Applied Sequence`.

An individual Reaction may override this selection policy.

### 2.5 Reaction result Level

Unless a Reaction defines another rule, its Result Level is:

```text
floor(sum of all participating Levels / number of participating Levels)
```

Only participants that actually take part in the Reaction are included. Immediate-only Reactions that do not create a persistent result do not need to store a Result Level.

## 3. Damage Rules

### 3.1 Elemental Attack values

Each combatant separately owns:

- Fire Attack
- Ice Attack
- Water Attack
- Lightning Attack

### 3.2 Elemental Damage formula

Elemental Damage uses the matching Elemental Attack value and does not use the normal Defense multiplier:

```text
Elemental Damage
= Elemental Attack
× Damage Multiplier
× max(0, 1 + sum of all applicable elemental damage modifiers)
```

All intermediate calculations retain full precision. Debug displays may show three decimal places. Final Damage is rounded to the nearest integer only when it is applied.

### 3.3 Multiple Damage Tags

A Damage Effect may contain multiple Damage Tags. Each damage component applies only the modifiers that match its own Tags, and the resulting components are added together.

### 3.4 Reaction Damage

- Reaction Damage is produced by a Reaction rather than by a new Action Resolution Point.
- Unless a Reaction states otherwise, Reaction Damage uses the corresponding Elemental Attack and Elemental Damage formula.
- Reaction Damage does not receive a second Guard, Evade, or Clash check.
- Reaction Damage has no On Hit package.
- Reaction Damage cannot trigger another Reaction unless a future Reaction explicitly overrides this rule.
- Toxin Activation is True Damage and follows its own formula.

## 4. Reaction Resolution

The MVP uses **Single-Pass Reaction Resolution**.

1. Resolve Guard, Evade, and other hit validation for the original effect.
2. Capture a Snapshot of all valid Tags, Statuses, Environment Tags, and Reaction Properties at that Resolution Point.
3. Build every candidate Reaction from the Snapshot.
4. Remove candidates prevented by Reaction Immunity before reserving or consuming inputs.
5. Reserve consumable input instances so the same instance cannot be spent twice.
6. Resolve all non-conflicting Immediate Outputs at the same Time Step.
7. Apply all Timed Status and Environment Outputs.
8. Outputs created during this pass cannot generate a second Reaction during the same pass.

### 4.1 Multiple Reactions from one elemental hit

An elemental Damage Tag is not consumed when it participates in a Reaction. One elemental hit may therefore trigger multiple Reactions when they use different materials.

For example, one successful Fire hit against a target that has Flammable, Frigid, and Explosive may trigger Burning, Thermal Shock, and Explosion during the same Resolution Point.

Only consumable input instances are exclusive. If multiple candidate Reactions compete for consumable inputs, candidates associated with the earlier-applied input are processed first according to `Applied Sequence`. An input reserved or consumed by an earlier candidate is unavailable to later candidates in the same Snapshot.

The current v2 content does not define two different Reactions that can consume the exact same single input instance under otherwise identical conditions. Any future content that creates such a case must define an additional tie-break rule.

## 5. Reaction Immunity

Reaction Immunity is data-driven and targets a specific Reaction ID.

### 5.1 Full Reaction Immunity

Full immunity causes the entire Reaction to fail:

- No inputs are consumed.
- No Immediate Outputs occur.
- No Timed Outputs are applied.
- The original attack and unrelated On Hit effects still resolve normally.

Example: an accessory may grant `Reaction Immunity: Toxin Activation` without preventing Water Damage or other Toxin-related Reactions.

### 5.2 Reaction Disruption Immunity

Reaction Disruption Immunity prevents only the Action-control output of a specified Reaction:

- The Reaction still succeeds.
- Inputs are consumed normally.
- Damage and persistent Status outputs are applied normally.
- The Reaction's Forced Action Cancel is ignored.

This immunity is suitable for individual Actions that should not be interrupted by Frozen or Paralysis.

## 6. Direct Elemental Status Tags

These Statuses are applied only when an Action explicitly says it applies them. Each lasts 30 Time Steps, is Multiple, and may coexist with other instances of the same Status.

| Element | Status | Level I–V effect |
|---|---|---|
| Fire | Parched | Damage dealt with Water and Ice is reduced by 1/2/3/4/5%. |
| Ice | Frigid | Damage dealt with Fire and Lightning is reduced by 1/2/3/4/5%. Frigid is less severe than Hypothermia and Frozen. |
| Water | Soaked | Damage taken from Ice and Lightning is increased by 1/2/3/4/5%. Soaked does not provide Water-Bearing. |
| Lightning | Residual Charge | Damage taken from Fire and Water is increased by 1/2/3/4/5%. |

## 7. General Combat Status Tags

| Status | Instances | Level I–V effect |
|---|---|---|
| Oil-Coated | Multiple | Increases incoming Flinch duration by +1/+1/+2/+2/+3 Time Steps and may provide Flammable. |
| Softened | Multiple | Increases Slashing and Piercing Damage taken by 3/5/7/10/15%; reduces Blunt Damage taken by the same amount. |
| Brittle | Multiple | Increases Piercing and Blunt Damage taken by 3/5/7/10/15%; reduces Water Damage taken by the same amount. |
| Elemental Vulnerability | Multiple | Increases all Elemental Damage taken by 1/2/3/4/5%. |
| Physical Vulnerability | Multiple | Increases Slashing, Blunt, and Piercing Damage taken by 1/2/3/4/5%. |
| Elemental Fatigue | Multiple | Reduces all Elemental Damage dealt by 1/2/3/4/5%. May be consumed by Hypothermia. |
| Physical Fatigue | Multiple | Reduces Slashing, Blunt, and Piercing Damage dealt by 1/2/3/4/5%. May be consumed by Hypothermia. |
| Off-Balance | Single | Modifies incoming Stagger: I adds 1 Time Step; II adds 2; III converts it to Knockdown; IV converts it and adds 1 Time Step; V converts it and adds 2. Existing Off-Balance is not replaced or refreshed. |

## 8. Super Armor and Tenacity

Super Armor reduces or negates Action Disruption by severity according to its existing I–V rules.

Each Tenacity source has a Level and an authored integer `Duration Reduction`.

| Tenacity Level | Flinch | Stagger | Knockdown |
|---|---|---|---|
| I | Full reduction | No reduction | No reduction |
| II | Full reduction | Half reduction | No reduction |
| III | Full reduction | Full reduction | No reduction |
| IV | Full reduction | Full reduction | Half reduction |
| V | Full reduction | Full reduction | Full reduction |

Half of an odd reduction value is rounded down. A sufficient reduction may reduce a disruption duration to zero and thereby negate it.

## 9. Environment Tags

The elemental MVP initially supports one shared battlefield environment. Local player-side and enemy-side environments remain a future extension.

| Environment Tag | Level I–V effect | Coexistence rule |
|---|---|---|
| Standing Water | Provides Water-Bearing at the same Level. | Same-name applications retain the higher Level. |
| Loose Sand | Provides Earthen at the same Level. | Same-name applications retain the higher Level. |
| Damp | Reduces newly created Burning duration by 1/2/3/4/5 Time Steps. | Replaces Dry. Same-name applications retain the higher Level. |
| Dry | Increases newly created Burning duration by 1/2/3/4/5 Time Steps. | Replaces Damp. Same-name applications retain the higher Level. |
| Cold | Increases Ice Damage and reduces Fire Damage by 3/5/7/10/15%. | Replaces Hot. Same-name applications retain the higher Level. |
| Hot | Reduces Ice Damage and increases Fire Damage by 3/5/7/10/15%. | Replaces Cold. Same-name applications retain the higher Level. |
| Miasma | Provides Explosive and Toxin at the same Level as Miasma. Toxin also copies an authored Toxin Potency value from the Miasma content data. | Uses the generic Explosion and Toxin Activation rules rather than unique Miasma-only Reactions. |

After Damp or Dry modifies a Burning duration, a duration of zero or less creates no Burning instance. This is the MVP safety rule for non-positive durations.

## 10. Reaction Properties

| Reaction Property | Common source | Reaction use | Consumption rule |
|---|---|---|---|
| Flammable | Wood, oil, Oil-Coated, or authored content | Fire → Burning | Consume one using the default input selection policy. |
| Water-Bearing | Water-bearing objects or environments such as Standing Water | Required by Electric Arc | Not consumed by Electric Arc. Soaked does not provide this Property. |
| Earthen | Loose Sand or authored content | Water → Mired | Consume one using the default input selection policy. |
| Metallic | Metal armor, objects, or authored content | Fire → Metal Softening | Not consumed. |
| Explosive | Explosive objects, Miasma, or authored content | Fire → Explosion | Explosion atomically consumes all instances on the target. |
| Toxin | Miasma or authored poison sources | Water → Toxin Activation | Stores both Level and Toxin Potency. Consume one using the default policy. |
| Soluble | Authored Actions, objects, enemies, or environments | Water → Dissolution | Not consumed. |
| Magnetic | Authored Actions, objects, enemies, or environments | Lightning → Overload | Consume one using the default policy. |

Conductive, Insulated, Absorbent, Grounded, and Volatile are not part of v2.

## 11. Fire Reactions

| Reaction | Requirements | Consumption and instances | Immediate output | Timed output |
|---|---|---|---|---|
| Burning | Fire + Flammable | Consume one Flammable. Multiple. Result Level is the average of Fire and Flammable. | None. | For 3/4/5/6/7 Time Steps based on Flammable Level, deal Fire Damage each Time Step at 2/4/6/8/10% based on Fire Level. Damp and Dry modify the duration. |
| Metal Softening | Fire + Metallic | Metallic is not consumed. Creates the Single Status Heat-Softened Metal; if it already exists, the Reaction fails. | None. | Heat-Softened Metal lasts 10 Time Steps and has the same modifiers as Softened. Result Level averages Fire and Metallic. |
| Thermal Shock | Fire + Frigid | Consume one Frigid using the default policy. Multiple. | Deal Fire Damage at 10/20/30/40/50% based on Fire Level. | For 30 Time Steps, increase Fire Damage taken by 3/5/7/10/15% based on the consumed Frigid Level. Result Level averages Fire and Frigid. |
| Explosion | A successful Fire hit + one or more Explosive instances | Once per target per Resolution Point, atomically consume all Explosive instances. Immediate-only. | Each consumed Explosive independently creates one simultaneous Fire Reaction Damage packet at 10/20/30/40/50% based on that Explosive's Level. | None. |

### 11.1 Explosion details

- Explosion does not trigger if Guard or Evade prevents the original Fire hit.
- Every packet uses the triggering combatant's Fire Attack and applicable Fire modifiers.
- Every packet retains full precision and is rounded independently when applied.
- All packets resolve even if an earlier simultaneous packet would already reduce the target to zero Health.
- Explosion packets cannot be Guarded, Evaded, Clashed, or used to trigger another Reaction.
- Explosion packets do not carry On Hit effects.
- In the MVP, Explosion damages only the target carrying the consumed Explosive instances. Spatial propagation is a future extension.

## 12. Ice Reactions

| Reaction | Requirements | Consumption and instances | Immediate output | Timed output |
|---|---|---|---|---|
| Hypothermia | Ice + Physical Fatigue or Elemental Fatigue | Consume the highest-Level eligible Fatigue; equal Levels use the shortest remaining duration. Multiple. | None. | Reduce all Damage dealt by 3/5/7/10/15% based on Ice Level. Duration is 20/25/30/40/50 Time Steps based on the consumed Fatigue Level. Result Level averages Ice and the consumed Fatigue. |
| Frozen | Ice + Hypothermia | Consume one Hypothermia using the default policy. Single; if Frozen already exists, the Reaction fails. | Forced Action Cancel. | Reduce all Damage dealt by 3/5/7/10/15% and increase Ice Damage taken by 3/5/7/10/15%, both based on Ice Level. Duration is 30/40/50/65/80 Time Steps based on consumed Hypothermia Level. Result Level averages Ice and Hypothermia. |
| Quenching | Ice + Softened or Heat-Softened Metal | Consume the highest Level. At equal Levels, consume ordinary Softened first. Multiple. | None. | For 30/40/50/65/80 Time Steps based on the consumed Status Level, normal Guard hit prevention fails and an independent multiplier instead reduces incoming Damage by 80/60/40/20/0% based on Ice Level. The attack counts as a Hit; On Hit effects apply and On Guarded Response does not. |
| Flexoelectric | Ice + Residual Charge | Consume one Residual Charge using the default policy. Multiple. | Deal Lightning Damage at 10/20/30/40/50% based on Residual Charge Level. | For 30 Time Steps, reduce Slashing, Blunt, and Piercing Damage dealt by 3/5/7/10/15% based on Ice Level. Result Level averages Ice and Residual Charge. |

## 13. Water Reactions

| Reaction | Requirements | Consumption and instances | Immediate output | Timed output |
|---|---|---|---|---|
| Mired | Water + Earthen | Consume one Earthen using the default policy. Single; if Mired exists, the Reaction fails and consumes nothing. | None. | Evade may succeed only 2/2/1/1/0 times per Round based on Water Level. Every Evade Resolution Point inserts two blank Time Steps immediately after that point within the current Action, delaying the remainder of that Action and all following Actions, whether or not the Evade successfully avoids Damage. Duration is 1/2/2/3/3 Rounds based on consumed Earthen Level. Result Level averages Water and Earthen. |
| Evaporation | Water + Parched | Consume one Parched using the default policy. Multiple. | None. | For 30 Time Steps, reduce Healing Received by 20/40/60/80/100% based on Water Level and reduce Fire Damage dealt by 3/5/7/10/15% based on consumed Parched Level. Result Level averages Water and Parched. |
| Toxin Activation | Water + Toxin | Consume one Toxin using the default policy. Multiple. | None. | For 20/25/30/40/50 Time Steps based on Water Level, deal True Damage each Time Step equal to the consumed Toxin Potency multiplied by 2/4/6/8/10% based on Toxin Level. The created instance preserves the consumed Toxin Potency. Result Level averages Water and Toxin. |
| Dissolution | Water + Soluble | Soluble is not consumed. Multiple. | At the actual Reaction Time Step, uniformly select Slashing, Blunt, Piercing, Fire, Ice, Water, or Lightning. Enemy Intent does not reveal the selected result before resolution. | Increase Damage taken from the selected type by 1/2/3/4/5% based on Water Level. Duration is 20/25/30/40/50 Time Steps based on Soluble Level. Result Level averages Water and Soluble. Existing results do not leave the random pool; repeated types create independent stacking instances. |

## 14. Lightning Reactions

| Reaction | Requirements | Consumption and instances | Immediate output | Timed output |
|---|---|---|---|---|
| Overload | Lightning + Magnetic | Consume one Magnetic using the default policy. Multiple. | Deal Lightning Damage at 20/40/60/80/100% based on Lightning Level. | For 30 Time Steps, reduce Lightning Damage taken by 10/8/6/4/2% based on consumed Magnetic Level. Result Level averages Lightning and Magnetic. |
| Paralysis | Lightning + one or more Electrified instances | Atomically consume all Electrified instances on the target. Multiple. | Forced Action Cancel. | Increase Lightning Damage taken by 3/5/7/10/15% based on Lightning Level. Duration is `20 + 5 × sum of all consumed Electrified Levels` Time Steps. Result Level averages Lightning and every consumed Electrified Level. |
| Electric Arc | A Lightning hit while the attacker has Soaked and the battlefield has Water-Bearing | Consume the attacker's highest-Level Soaked; Water-Bearing is not consumed. Multiple. | Deal Lightning Damage at 10/20/30/40/50% based on Lightning Level. | For 30 Time Steps, reduce Lightning and Water Damage taken by the attacker by 3/5/7/10/15% based on consumed Soaked Level. Result Level averages Lightning, Soaked, and Water-Bearing. |
| Electrified | Lightning + Soaked | Consume one Soaked using the default policy. Multiple. | Deal Lightning Damage at 10/20/30/40/50% based on Lightning Level. | Create Electrified for 20/25/30/40/50 Time Steps based on consumed Soaked Level. Result Level averages Lightning and Soaked. Electrified is an input for Paralysis. |

## 15. Frozen and Paralysis Control

Frozen and Paralysis each produce two separate outputs:

1. A one-time Forced Action Cancel when the Reaction resolves.
2. A persistent Damage-modifying Status for the listed duration.

Forced Action Cancel:

- Immediately ends the current Action.
- Continues into the next Action using normal post-disruption continuation rules.
- Ignores Super Armor, Tenacity, and the current Action's ordinary Disruption Response.
- Can be prevented by Reaction Disruption Immunity for that Reaction.
- Does not repeatedly prevent Actions during the persistent Status duration.

## 16. Player-Facing Names and Combat Log

A Reaction and its resulting Status may share the same player-facing name to reduce vocabulary burden. Their internal stable IDs and event types remain separate.

Example:

```text
[Reaction] Hypothermia triggered.
[Status Applied] Hypothermia III — 30 Time Steps.
[Status Expired] Hypothermia III.
```

## 17. Deferred Features

The following concepts are retained for possible future development but are outside the current MVP and have no finalized behavior:

- Confusion: proposed to invalidate a future Resolution Point.
- Fear: proposed to invalidate the first Resolution Point of a Round.
- Slowed: proposed to insert blank Time Steps before an Action.
- Local player-side and enemy-side environments.
- Spatial Explosion propagation.
- Conductive, Insulated, Absorbent, Grounded, and Volatile Reaction Properties.

These concepts must not be implemented until their detailed rules are approved.

## 18. Deferred Implementation Order

Elemental Reaction implementation is not part of the current development milestone. The team will first complete and validate the shared physical combat rules, weapon Action structure, and initial physical weapon content. This rulebook is retained so elemental work can resume later without reopening the completed design discussion.

When elemental implementation resumes, the recommended sequence is:

1. Migrate finalized terminology while preserving serialized enum values and stable IDs.
2. Add Water Damage and separate Fire, Ice, Water, and Lightning Attack values.
3. Extend Status instances with Level, stacking policy, duration type, and `Applied Sequence`.
4. Add Environment Tags and Reaction Properties.
5. Implement Single-Pass Reaction Resolution and Reaction Immunity.
6. Implement one end-to-end vertical slice: Fire + Flammable → Burning.
7. Add the remaining Reactions incrementally with Edit Mode tests and an interactive Play Mode scenario for each major interaction.
