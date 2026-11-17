# Party Turn System Options

## Option A: Side-Based Turns

Every living party member acts during the player-side phase. After the party finishes, all surviving enemies take their actions.

This version is familiar, readable, and relatively easy to implement. It gives the player freedom to coordinate the party within one phase. Its main risk is that the acting side may focus attacks and defeat important targets before they have a chance to respond.

The fun would mainly come from party composition, action order within the player phase, target selection, support effects, and combining the roles of multiple characters.

## Option B: Speed-Based Initiative

Player and enemy units share one action order based on Speed. This creates more interaction between both sides and makes Speed an important part of character building.

Two versions are possible:

- In the round-based version, every living unit acts once before the next round begins. Faster units act earlier but do not gain additional turns.
- In the continuous-timeline version, faster units may act more often and overtake slower units.

The round-based version is easier to read and balance. The continuous version gives Speed more strategic value but increases the risk of snowballing, complicated status durations, and difficult turn-order UI.

## D&D-Inspired Rules

D&D-inspired damage dice, saving throws, ability checks, advantage or disadvantage, and event checks could be layered onto either turn system. Traditional D&D initiative is optional and would replace, rather than simply supplement, the selected side-based or Speed-based action order.

## Possible Future Mechanics

Both turn systems could later support follow-up attacks, Speed buffs and debuffs, action delay, turn advancement, weapon proficiency, additional party members, and more specialized enemy roles. The continuous timeline could also support characters who gain extra turns through high Speed.

## Suggested Prototype Scope

An early prototype can use two player characters, one or two enemies, a small set of active skills, simple equipment, and placeholder animations. Side-based turns are the lowest-risk starting point. Speed-sorted rounds are a stronger option if the team wants initiative order to be part of the MVP. Lapping, advanced follow-ups, and full D&D-inspired rules can remain future extensions until the basic party battle is enjoyable.

