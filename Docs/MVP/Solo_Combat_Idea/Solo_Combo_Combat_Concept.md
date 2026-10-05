# Solo Combo Combat Concept

## Core Idea

The player controls one character and learns Actions through exploration, training, events, items such as manuals or scrolls, and possible future combat-observation systems. Before entering battle, the player arranges owned Actions into a limited number of custom Combos.

During battle, one player turn always uses one complete Combo. A longer Combo does not consume additional turns, but its costs and internal Action order may affect whether and how it can be used.

## Terminology

| Term | Meaning |
| --- | --- |
| Combo | A player-created sequence used as one combat command. |
| Opening Action | An optional Action resolved before the Main Action. |
| Main Action | The required central Action of every Combo. |
| Closing Action | An optional Action resolved after the Main Action. |
| Linked Action | One optional Action attached to an Opening, Main, or Closing Action and resolved immediately after its host. |
| Load Cost | The amount of Combo capacity occupied by an Action. It may also determine Action timing in the simultaneous timeline model. |
| Mana Cost | Magical resource required by the Combo. |
| Stamina Cost | Physical resource required by the Combo. |

## Combo Construction

A valid Combo has one required Main Action, optional Opening and Closing Actions, and no more than one Linked Action. Opening, Main, Closing, and Linked Actions resolve in a meaningful order, so earlier effects may change the outcome of later Actions.

The Combo Editor validates the following rules:

- Total Load cannot exceed the character's Combo Load Limit.
- Total Mana Cost cannot exceed Max Mana.
- Total Stamina Cost cannot exceed Max Stamina.
- One owned Action can be assigned to only one Combo at a time.
- A Linked Action must be compatible with its selected host.

Current Mana and Stamina are checked only when the player attempts to execute a Combo. They are not used to reject a Combo during construction because resources may recover before the player uses it.

New Combo slots use simple names such as `combo1` and `combo2`. Players may rename them, and the custom name can appear as stylized text when the Combo is performed.

## Resources and Cooldown

Mana and Stamina can decrease and recover during combat. Exact recovery rules remain undecided.

The current prototype rule is:

> Combo Cooldown equals the sum of the cooldown values contributed by all Actions in that Combo.

This formula is provisional and may change after playtesting. Cooldown is stored on the Combo configuration rather than as a separate timer for every Action.

## Combat Reconfiguration

The player may have a limited number of Reconfiguration Charges per battle. One Charge allows the player to replace one Action in one Combo. The replacement must create a valid Combo and must be a real Action change; opening the editor or restoring the same configuration does not reset cooldown.

A successfully modified Combo becomes available even if it was previously on cooldown. The player may intentionally preserve a strong Main Action while replacing an Opening or Closing Action to refresh the Combo. Equipment, accessories, or Inherent Skills may increase Reconfiguration Charges and create a choice between direct combat power and tactical flexibility.

At the end of combat, the game asks whether the player wants to keep the combat-time changes or restore the pre-battle Combo setup.

## Action Acquisition

Actions represent learned techniques rather than ordinary physical loot. Possible acquisition sources include manuals, scrolls, trainers, character events, observation of enemy techniques, and personal breakthroughs during practice.

Random rewards should normally remove already-known Actions from their candidate pool. If a fixed reward duplicates a known Action, it can be converted into currency, Technique Insight, upgrade material, or another appropriate reward.

