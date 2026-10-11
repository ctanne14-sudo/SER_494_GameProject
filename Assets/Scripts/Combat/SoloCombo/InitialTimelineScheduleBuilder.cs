using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public static class InitialTimelineScheduleBuilder
    {
        public static InitialTimelineSchedule Build(
            ComboConfiguration playerCombo,
            EnemyActionSequence enemySequence)
        {
            if (playerCombo == null)
            {
                throw new ArgumentNullException(nameof(playerCombo));
            }

            if (playerCombo.MainAction == null)
            {
                throw new ArgumentException(
                    "The player Combo must contain a Main Action.",
                    nameof(playerCombo));
            }

            if (enemySequence == null)
            {
                throw new ArgumentNullException(nameof(enemySequence));
            }

            var actions = new List<ScheduledTimelineAction>(
                playerCombo.GetAssignedActions().Count + enemySequence.Actions.Count);

            int playerCompletionTime = SchedulePlayerActions(
                playerCombo.BuildExecutionOrder(),
                actions);
            int enemyCompletionTime = ScheduleEnemyActions(enemySequence.Actions, actions);

            return CreateSchedule(
                actions,
                Math.Max(playerCompletionTime, enemyCompletionTime));
        }

        public static InitialTimelineSchedule BuildEnemyOnly(
            EnemyActionSequence enemySequence)
        {
            if (enemySequence == null)
            {
                throw new ArgumentNullException(nameof(enemySequence));
            }

            var actions = new List<ScheduledTimelineAction>(enemySequence.Actions.Count);
            int completionTime = ScheduleEnemyActions(enemySequence.Actions, actions);
            return CreateSchedule(actions, completionTime);
        }

        private static InitialTimelineSchedule CreateSchedule(
            List<ScheduledTimelineAction> actions,
            int completionTime)
        {
            ValidateMutuallyExclusiveResolutionEffects(actions);
            var actionsByTime = new SortedDictionary<int, List<ScheduledTimelineAction>>();
            for (int index = 0; index < actions.Count; index++)
            {
                ScheduledTimelineAction action = actions[index];
                if (!actionsByTime.TryGetValue(
                        action.ResolutionTime,
                        out List<ScheduledTimelineAction> sameTimeActions))
                {
                    sameTimeActions = new List<ScheduledTimelineAction>();
                    actionsByTime.Add(action.ResolutionTime, sameTimeActions);
                }

                sameTimeActions.Add(action);
            }

            var steps = new List<TimelineResolutionStep>(actionsByTime.Count);
            foreach (KeyValuePair<int, List<ScheduledTimelineAction>> pair in actionsByTime)
            {
                steps.Add(new TimelineResolutionStep(
                    pair.Key,
                    Array.AsReadOnly(pair.Value.ToArray())));
            }

            return new InitialTimelineSchedule(
                Array.AsReadOnly(actions.ToArray()),
                Array.AsReadOnly(steps.ToArray()),
                completionTime);
        }

        private static void ValidateMutuallyExclusiveResolutionEffects(
            IReadOnlyList<ScheduledTimelineAction> actions)
        {
            var playerTimes = new HashSet<int>();
            var enemyTimes = new HashSet<int>();

            for (int index = 0; index < actions.Count; index++)
            {
                ScheduledTimelineAction action = actions[index];
                if (action.ResolutionEffect != ActionResolutionEffect.Damage
                    && action.ResolutionEffect != ActionResolutionEffect.Guard
                    && action.ResolutionEffect != ActionResolutionEffect.Evade)
                {
                    continue;
                }

                ISet<int> occupiedTimes = action.Side == TimelineActorSide.Player
                    ? playerTimes
                    : enemyTimes;
                if (!occupiedTimes.Add(action.ResolutionTime))
                {
                    throw new InvalidOperationException(
                        "Damage, Guard, and Evade Resolution Points cannot overlap for the same character.");
                }
            }
        }

        private static int SchedulePlayerActions(
            IReadOnlyList<ComboActionDefinition> definitions,
            ICollection<ScheduledTimelineAction> destination)
        {
            int currentTime = 0;
            for (int index = 0; index < definitions.Count; index++)
            {
                ComboActionDefinition definition = definitions[index];
                int endTime = checked(currentTime + definition.LoadCost);
                destination.Add(new ScheduledTimelineAction(
                    TimelineActorSide.Player,
                    index,
                    definition.ActionId,
                    definition.DisplayName,
                    currentTime,
                    endTime,
                    checked(currentTime + definition.ResolutionOffset),
                    definition.DamageEffect,
                    definition.ResolutionEffect,
                    definition.DamageTags,
                    definition.StatusEffect,
                    definition.StatusTarget));
                currentTime = endTime;
            }

            return currentTime;
        }

        private static int ScheduleEnemyActions(
            IReadOnlyList<EnemyActionDefinition> definitions,
            ICollection<ScheduledTimelineAction> destination)
        {
            int currentTime = 0;
            for (int index = 0; index < definitions.Count; index++)
            {
                EnemyActionDefinition definition = definitions[index];
                int endTime = checked(currentTime + definition.LoadCost);
                destination.Add(new ScheduledTimelineAction(
                    TimelineActorSide.Enemy,
                    index,
                    definition.ActionId,
                    definition.DisplayName,
                    currentTime,
                    endTime,
                    checked(currentTime + definition.ResolutionOffset),
                    definition.DamageEffect,
                    definition.ResolutionEffect,
                    definition.DamageTags,
                    definition.StatusEffect,
                    definition.StatusTarget));
                currentTime = endTime;
            }

            return currentTime;
        }
    }
}
