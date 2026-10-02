# Solo Combat Resolution Options

## Option A: Sequential Resolution

The player chooses one prepared Combo and resolves all of its Actions in order. After the complete Combo finishes, the enemy takes its turn.

This version is easier to understand, implement, present, and balance. Its main risk is that combat may become repetitive if the same efficient Combo remains the best answer in many situations. Enemy states, resources, cooldowns, environmental conditions, and limited reconfiguration would need to create reasons to choose different Combos.

## Option B: Simultaneous Timeline Resolution

The enemy announces an intended Action sequence and the player chooses one prepared Combo. Both sides commit for the same turn. Each Action has a Load Cost, and cumulative Load determines when it resolves on the shared timeline.

For example, if the player's Action completion times are 8, 18, and 28 while the enemy's are 9, 21, and 27, the resolution order is player, enemy, player, enemy, enemy, player. A specially designed interrupt Action may cancel or weaken an unresolved enemy Action that would complete later.

This model makes Load Cost useful both during Combo construction and during battle. It also creates more interaction, prediction, and counterplay. Its main risks are timeline readability, interrupt balance, enemy-intent design, animation presentation, and higher implementation cost.

The combat rules and presentation do not need to be equally simultaneous. The system can calculate a shared timeline first and then play the resulting events in a readable order, allowing the team to test the gameplay before attempting complex overlapping animations.

## Environment Interaction Direction

Actions should not contain hard-coded references to individual room objects. Instead, Actions can produce general effects such as Fire, Lightning, Slashing, Blunt, or Projectile, while environmental objects expose traits such as Flammable, Conductive, Cuttable, or Fragile. A reaction system then determines the result.

Combat Zones can represent proximity even though the battle has no movement command. An enemy and nearby objects share a Zone, allowing an Action aimed at that enemy or Zone to interact with the surrounding environment.

Current provisional surface examples are Oil + Fire and Water + Lightning. Possible object examples include cutting a suspended object, breaking an unstable structure, igniting a flammable feature, or overloading a conductive mechanism. Exact rules and MVP inclusion remain open for team discussion.

## D&D-Inspired Rules

D&D-inspired damage dice, saving throws, ability checks, advantage or disadvantage, and event checks could be layered onto either resolution option. Traditional D&D initiative is not required: sequential combat already defines alternating turns, while simultaneous combat uses Load to determine timing.

## Suggested Prototype Scope

An early prototype can use one player, one enemy, four predefined Combos, a small Action set, visible resource and cooldown information, and simple placeholder animations. The sequential model can test the core Combo idea with less risk. The simultaneous model can additionally test enemy intent, one interrupt Action, and a basic timeline before the full Combo Editor or advanced environment system is built.

