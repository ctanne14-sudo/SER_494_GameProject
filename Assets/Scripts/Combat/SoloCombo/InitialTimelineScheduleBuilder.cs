using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public static class InitialTimelineScheduleBuilder
    {
        public const int AbsoluteTimelineLimit = 100;

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

        public static InitialTimelineSchedule SelectResolutionTime(
            InitialTimelineSchedule schedule,
            TimelineActorSide side,
            int sequenceIndex,
            int selectedTime)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            var actions = new List<ScheduledTimelineAction>(schedule.Actions.Count);
            bool replaced = false;
            for (int index = 0; index < schedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = schedule.Actions[index];
                if (action.Side == side && action.SequenceIndex == sequenceIndex)
                {
                    if (!action.HasResolutionWindow
                        || selectedTime < action.ResolutionWindowStartTime.Value
                        || selectedTime > action.ResolutionWindowEndTime.Value)
                    {
                        throw new ArgumentOutOfRangeException(nameof(selectedTime));
                    }

                    action = new ScheduledTimelineAction(
                        action.Side,
                        action.SequenceIndex,
                        action.ActionId,
                        action.DisplayName,
                        action.StartTime,
                        action.EndTime,
                        selectedTime,
                        action.DamageEffect,
                        action.ResolutionEffect,
                        action.DamageTags,
                        action.StatusEffect,
                        action.StatusTarget,
                        resolutionWindowStartTime: null,
                        resolutionWindowEndTime: null,
                        chargeLevels: action.ChargeLevels,
                        activeWindows: action.ActiveWindows,
                        onHitDisruption: action.OnHitDisruption,
                        disruptionResponseSegments: action.DisruptionResponseSegments,
                        accumulatedPauseDuration: action.AccumulatedPauseDuration,
                        disruptionPauses: action.DisruptionPauses);
                    replaced = true;
                }

                actions.Add(action);
            }

            if (!replaced)
            {
                throw new ArgumentException("The requested Resolution Window was not found.");
            }

            return CreateSchedule(actions, schedule.CompletionTime);
        }

        public static InitialTimelineSchedule ApplyChargeLevel(
            InitialTimelineSchedule schedule,
            TimelineActorSide side,
            int sequenceIndex,
            int chargeLevel)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            ScheduledTimelineAction selectedAction = null;
            for (int index = 0; index < schedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = schedule.Actions[index];
                if (action.Side == side && action.SequenceIndex == sequenceIndex)
                {
                    selectedAction = action;
                    break;
                }
            }

            if (selectedAction == null || !selectedAction.IsChargeable)
            {
                throw new ArgumentException("The requested chargeable Action was not found.");
            }

            if (chargeLevel < 0 || chargeLevel > selectedAction.ChargeLevels.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(chargeLevel));
            }

            int additionalLoad = chargeLevel == 0
                ? 0
                : selectedAction.ChargeLevels[chargeLevel - 1].AdditionalLoad;
            double damageScale = chargeLevel == 0
                ? 1d
                : selectedAction.ChargeLevels[chargeLevel - 1].DamageMultiplierScale;
            var actions = new List<ScheduledTimelineAction>(schedule.Actions.Count);
            int completionTime = 0;

            for (int index = 0; index < schedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = schedule.Actions[index];
                int shift = action.Side == side && action.SequenceIndex > sequenceIndex
                    ? additionalLoad
                    : 0;

                if (action == selectedAction)
                {
                    action = CopyScheduledAction(
                        action,
                        startShift: 0,
                        endShift: additionalLoad,
                        resolutionShift: additionalLoad,
                        damageScale,
                        keepChargeLevels: false);
                }
                else if (shift > 0)
                {
                    action = CopyScheduledAction(
                        action,
                        startShift: shift,
                        endShift: shift,
                        resolutionShift: shift,
                        damageScale: 1d,
                        keepChargeLevels: true);
                }

                actions.Add(action);
                completionTime = Math.Max(completionTime, action.EndTime);
            }

            if (completionTime > AbsoluteTimelineLimit)
            {
                throw new InvalidOperationException(
                    $"Charge Level {chargeLevel} would extend the Timeline beyond "
                    + $"the absolute limit of {AbsoluteTimelineLimit}.");
            }

            return CreateSchedule(actions, completionTime);
        }

        public static InitialTimelineSchedule ApplyPauseAndResume(
            InitialTimelineSchedule schedule,
            TimelineActorSide side,
            int sequenceIndex,
            int disruptionTime,
            int duration,
            ActionDisruptionType disruptionType)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            if (duration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            var actions = new List<ScheduledTimelineAction>(schedule.Actions.Count);
            bool found = false;
            int completionTime = 0;
            for (int index = 0; index < schedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = schedule.Actions[index];
                if (action.Side == side && action.SequenceIndex == sequenceIndex)
                {
                    found = true;
                    action = PauseScheduledAction(
                        action,
                        disruptionTime,
                        duration,
                        disruptionType);
                }
                else if (action.Side == side && action.SequenceIndex > sequenceIndex)
                {
                    action = CopyScheduledAction(
                        action,
                        startShift: duration,
                        endShift: duration,
                        resolutionShift: duration,
                        damageScale: 1d,
                        keepChargeLevels: true);
                }

                actions.Add(action);
                completionTime = Math.Max(completionTime, action.EndTime);
            }

            if (!found)
            {
                throw new ArgumentException("The Action to pause was not found.");
            }

            if (completionTime > AbsoluteTimelineLimit)
            {
                throw new InvalidOperationException(
                    $"Action Disruption would extend the Timeline beyond the absolute limit of {AbsoluteTimelineLimit}.");
            }

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
                    checked(currentTime + (definition.ResolutionWindowStart
                        ?? definition.ResolutionOffset)),
                    definition.DamageEffect,
                    definition.ResolutionEffect,
                    definition.DamageTags,
                    definition.StatusEffect,
                    definition.StatusTarget,
                    definition.ResolutionWindowStart.HasValue
                        ? (int?)checked(currentTime + definition.ResolutionWindowStart.Value)
                        : null,
                    definition.ResolutionWindowEnd.HasValue
                        ? (int?)checked(currentTime + definition.ResolutionWindowEnd.Value)
                        : null,
                    definition.ChargeLevels,
                    ScheduleActiveWindows(definition.ActiveWindows, currentTime),
                    definition.OnHitDisruption,
                    definition.DisruptionResponseSegments));
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
                    definition.StatusTarget,
                    activeWindows: ScheduleActiveWindows(
                        definition.ActiveWindows,
                        currentTime),
                    onHitDisruption: definition.OnHitDisruption,
                    disruptionResponseSegments: definition.DisruptionResponseSegments));
                currentTime = endTime;
            }

            return currentTime;
        }

        private static ScheduledTimelineAction CopyScheduledAction(
            ScheduledTimelineAction action,
            int startShift,
            int endShift,
            int resolutionShift,
            double damageScale,
            bool keepChargeLevels)
        {
            return new ScheduledTimelineAction(
                action.Side,
                action.SequenceIndex,
                action.ActionId,
                action.DisplayName,
                checked(action.StartTime + startShift),
                checked(action.EndTime + endShift),
                checked(action.ResolutionTime + resolutionShift),
                ScaleDamageEffect(action.DamageEffect, damageScale),
                action.ResolutionEffect,
                action.DamageTags,
                action.StatusEffect,
                action.StatusTarget,
                action.ResolutionWindowStartTime.HasValue
                    ? (int?)checked(action.ResolutionWindowStartTime.Value + resolutionShift)
                    : null,
                action.ResolutionWindowEndTime.HasValue
                    ? (int?)checked(action.ResolutionWindowEndTime.Value + resolutionShift)
                    : null,
                keepChargeLevels
                    ? action.ChargeLevels
                    : Array.Empty<ComboActionChargeLevelDefinition>(),
                ShiftActiveWindows(action.ActiveWindows, resolutionShift),
                action.OnHitDisruption,
                action.DisruptionResponseSegments,
                action.AccumulatedPauseDuration,
                ShiftDisruptionPauses(action.DisruptionPauses, startShift));
        }

        private static ScheduledTimelineAction PauseScheduledAction(
            ScheduledTimelineAction action,
            int disruptionTime,
            int duration,
            ActionDisruptionType disruptionType)
        {
            int resolutionShift = action.ResolutionTime > disruptionTime ? duration : 0;
            int? windowStart = action.ResolutionWindowStartTime;
            int? windowEnd = action.ResolutionWindowEndTime;
            if (windowStart.HasValue && windowStart.Value > disruptionTime)
            {
                windowStart = checked(windowStart.Value + duration);
                windowEnd = checked(windowEnd.Value + duration);
            }

            var pauses = new ScheduledDisruptionPause[action.DisruptionPauses.Count + 1];
            for (int index = 0; index < action.DisruptionPauses.Count; index++)
            {
                pauses[index] = action.DisruptionPauses[index];
            }

            pauses[pauses.Length - 1] = new ScheduledDisruptionPause(
                disruptionType,
                disruptionTime,
                duration);

            return new ScheduledTimelineAction(
                action.Side,
                action.SequenceIndex,
                action.ActionId,
                action.DisplayName,
                action.StartTime,
                checked(action.EndTime + duration),
                checked(action.ResolutionTime + resolutionShift),
                action.DamageEffect,
                action.ResolutionEffect,
                action.DamageTags,
                action.StatusEffect,
                action.StatusTarget,
                windowStart,
                windowEnd,
                action.ChargeLevels,
                ShiftFutureActiveWindows(action.ActiveWindows, disruptionTime, duration),
                action.OnHitDisruption,
                action.DisruptionResponseSegments,
                checked(action.AccumulatedPauseDuration + duration),
                Array.AsReadOnly(pauses));
        }

        private static DamageEffectDefinition ScaleDamageEffect(
            DamageEffectDefinition damageEffect,
            double scale)
        {
            if (damageEffect == null || Math.Abs(scale - 1d) < double.Epsilon)
            {
                return damageEffect;
            }

            var components = new DamageComponentDefinition[damageEffect.Components.Count];
            for (int index = 0; index < components.Length; index++)
            {
                DamageComponentDefinition component = damageEffect.Components[index];
                components[index] = new DamageComponentDefinition(
                    component.Type,
                    checked(component.ActionMultiplier * scale));
            }

            return new DamageEffectDefinition(components);
        }

        private static IReadOnlyList<ScheduledActiveWindow> ScheduleActiveWindows(
            IReadOnlyList<ActionActiveWindowDefinition> definitions,
            int actionStartTime)
        {
            if (definitions.Count == 0)
            {
                return Array.Empty<ScheduledActiveWindow>();
            }

            var windows = new ScheduledActiveWindow[definitions.Count];
            for (int index = 0; index < definitions.Count; index++)
            {
                ActionActiveWindowDefinition definition = definitions[index];
                windows[index] = new ScheduledActiveWindow(
                    definition.WindowId,
                    definition.DisplayName,
                    checked(actionStartTime + definition.StartOffset),
                    checked(actionStartTime + definition.EndOffset),
                    definition.Effect,
                    definition.DamageReduction,
                    definition.DisruptionPolicy);
            }

            return Array.AsReadOnly(windows);
        }

        private static IReadOnlyList<ScheduledActiveWindow> ShiftActiveWindows(
            IReadOnlyList<ScheduledActiveWindow> windows,
            int shift)
        {
            if (windows.Count == 0 || shift == 0)
            {
                return windows;
            }

            var shifted = new ScheduledActiveWindow[windows.Count];
            for (int index = 0; index < windows.Count; index++)
            {
                ScheduledActiveWindow window = windows[index];
                shifted[index] = new ScheduledActiveWindow(
                    window.WindowId,
                    window.DisplayName,
                    checked(window.StartTime + shift),
                    checked(window.EndTime + shift),
                    window.Effect,
                    window.DamageReduction,
                    window.DisruptionPolicy);
            }

            return Array.AsReadOnly(shifted);
        }

        private static IReadOnlyList<ScheduledActiveWindow> ShiftFutureActiveWindows(
            IReadOnlyList<ScheduledActiveWindow> windows,
            int disruptionTime,
            int shift)
        {
            if (windows.Count == 0)
            {
                return windows;
            }

            var shifted = new ScheduledActiveWindow[windows.Count];
            for (int index = 0; index < windows.Count; index++)
            {
                ScheduledActiveWindow window = windows[index];
                int windowShift = window.StartTime > disruptionTime ? shift : 0;
                shifted[index] = new ScheduledActiveWindow(
                    window.WindowId,
                    window.DisplayName,
                    checked(window.StartTime + windowShift),
                    checked(window.EndTime + windowShift),
                    window.Effect,
                    window.DamageReduction,
                    window.DisruptionPolicy);
            }

            return Array.AsReadOnly(shifted);
        }

        private static IReadOnlyList<ScheduledDisruptionPause> ShiftDisruptionPauses(
            IReadOnlyList<ScheduledDisruptionPause> pauses,
            int shift)
        {
            if (pauses.Count == 0 || shift == 0)
            {
                return pauses;
            }

            var shifted = new ScheduledDisruptionPause[pauses.Count];
            for (int index = 0; index < pauses.Count; index++)
            {
                ScheduledDisruptionPause pause = pauses[index];
                shifted[index] = new ScheduledDisruptionPause(
                    pause.Type,
                    checked(pause.StartTime + shift),
                    pause.Duration);
            }

            return Array.AsReadOnly(shifted);
        }
    }
}
