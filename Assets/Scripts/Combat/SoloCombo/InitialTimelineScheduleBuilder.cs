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

            int playerCompletionTime = SchedulePlayerActions(playerCombo, actions);
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
                        disruptionPauses: action.DisruptionPauses,
                        restarts: action.Restarts,
                        requiredCompletedSequenceIndex: action.RequiredCompletedSequenceIndex,
                        cancellation: action.Cancellation,
                        onGuardedDisruption: action.OnGuardedDisruption,
                        clash: action.Clash,
                        onEvadedDisruption: action.OnEvadedDisruption);
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

        public static InitialTimelineSchedule ApplyRestartAction(
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
            int scheduleShift = 0;
            int completionTime = 0;
            for (int index = 0; index < schedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = schedule.Actions[index];
                if (action.Side == side && action.SequenceIndex == sequenceIndex)
                {
                    found = true;
                    int previousEndTime = action.EndTime;
                    action = RestartScheduledAction(
                        action,
                        disruptionTime,
                        duration,
                        disruptionType);
                    scheduleShift = checked(action.EndTime - previousEndTime);
                }
                else if (action.Side == side && action.SequenceIndex > sequenceIndex)
                {
                    action = CopyScheduledAction(
                        action,
                        startShift: scheduleShift,
                        endShift: scheduleShift,
                        resolutionShift: scheduleShift,
                        damageScale: 1d,
                        keepChargeLevels: true);
                }

                actions.Add(action);
                completionTime = Math.Max(completionTime, action.EndTime);
            }

            if (!found)
            {
                throw new ArgumentException("The Action to restart was not found.");
            }

            if (completionTime > AbsoluteTimelineLimit)
            {
                throw new InvalidOperationException(
                    $"Action Disruption would extend the Timeline beyond the absolute limit of {AbsoluteTimelineLimit}.");
            }

            return CreateSchedule(actions, completionTime);
        }

        public static InitialTimelineSchedule ApplyCancelAction(
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
            int continuationTime = checked(disruptionTime + duration);
            int completionTime = 0;
            for (int index = 0; index < schedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = schedule.Actions[index];
                if (action.Side == side && action.SequenceIndex == sequenceIndex)
                {
                    found = true;
                    action = CancelScheduledAction(
                        action,
                        disruptionTime,
                        continuationTime,
                        disruptionType,
                        dependencyFailed: false);
                }
                else if (action.Side == side && action.SequenceIndex > sequenceIndex)
                {
                    if (action.RequiredCompletedSequenceIndex == sequenceIndex)
                    {
                        action = CancelScheduledAction(
                            action,
                            disruptionTime,
                            continuationTime,
                            disruptionType,
                            dependencyFailed: true);
                    }
                    else
                    {
                        int shift = checked(continuationTime - action.StartTime);
                        action = CopyScheduledAction(
                            action,
                            startShift: shift,
                            endShift: shift,
                            resolutionShift: shift,
                            damageScale: 1d,
                            keepChargeLevels: true);
                        continuationTime = action.EndTime;
                    }
                }

                actions.Add(action);
                if (!action.IsCanceled)
                {
                    completionTime = Math.Max(completionTime, action.EndTime);
                }
                else if (!action.Cancellation.DependencyFailed)
                {
                    completionTime = Math.Max(
                        completionTime,
                        action.Cancellation.RecoveryEndTime);
                }

                for (int windowIndex = 0;
                    windowIndex < action.ActiveWindows.Count;
                    windowIndex++)
                {
                    completionTime = Math.Max(
                        completionTime,
                        action.ActiveWindows[windowIndex].EffectiveEndTime);
                }
            }

            if (!found)
            {
                throw new ArgumentException("The Action to cancel was not found.");
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
                if (action.IsCanceled)
                {
                    continue;
                }

                for (int resolutionIndex = 0;
                    resolutionIndex < action.ResolutionTimes.Count;
                    resolutionIndex++)
                {
                    int resolutionTime = action.ResolutionTimes[resolutionIndex];
                    if (!actionsByTime.TryGetValue(
                            resolutionTime,
                            out List<ScheduledTimelineAction> sameTimeActions))
                    {
                        sameTimeActions = new List<ScheduledTimelineAction>();
                        actionsByTime.Add(resolutionTime, sameTimeActions);
                    }

                    sameTimeActions.Add(action);
                }
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
                if (action.IsCanceled)
                {
                    continue;
                }

                if (action.ResolutionEffect != ActionResolutionEffect.Damage
                    && action.ResolutionEffect != ActionResolutionEffect.Guard
                    && action.ResolutionEffect != ActionResolutionEffect.Evade)
                {
                    continue;
                }

                ISet<int> occupiedTimes = action.Side == TimelineActorSide.Player
                    ? playerTimes
                    : enemyTimes;
                for (int resolutionIndex = 0;
                    resolutionIndex < action.ResolutionTimes.Count;
                    resolutionIndex++)
                {
                    if (!occupiedTimes.Add(action.ResolutionTimes[resolutionIndex]))
                    {
                        throw new InvalidOperationException(
                            "Damage, Guard, and Evade Resolution Points cannot overlap for the same character.");
                    }
                }
            }
        }

        private static int SchedulePlayerActions(
            ComboConfiguration combo,
            ICollection<ScheduledTimelineAction> destination)
        {
            IReadOnlyList<ComboActionDefinition> definitions = combo.BuildExecutionOrder();
            int currentTime = 0;
            for (int index = 0; index < definitions.Count; index++)
            {
                ComboActionDefinition definition = definitions[index];
                int endTime = checked(currentTime + definition.LoadCost);
                int? requiredCompletedSequenceIndex = ReferenceEquals(
                    definition,
                    combo.LinkedAction)
                        ? (int?)(index - 1)
                        : null;
                destination.Add(new ScheduledTimelineAction(
                    TimelineActorSide.Player,
                    index,
                    definition.ActionId,
                    definition.DisplayName,
                    currentTime,
                    endTime,
                    checked(currentTime + (definition.ResolutionWindowStart
                        ?? definition.SustainedAttack?.FirstHitOffset
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
                    definition.DisruptionResponseSegments,
                    requiredCompletedSequenceIndex: requiredCompletedSequenceIndex,
                    onGuardedDisruption: definition.OnGuardedDisruption,
                    clash: definition.Clash,
                    onEvadedDisruption: definition.OnEvadedDisruption,
                    resolutionTimes: ScheduleResolutionTimes(
                        definition.SustainedAttack,
                        currentTime,
                        definition.ResolutionOffset)));
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
                    checked(currentTime + (definition.SustainedAttack?.FirstHitOffset
                        ?? definition.ResolutionOffset)),
                    definition.DamageEffect,
                    definition.ResolutionEffect,
                    definition.DamageTags,
                    definition.StatusEffect,
                    definition.StatusTarget,
                    activeWindows: ScheduleActiveWindows(
                        definition.ActiveWindows,
                        currentTime),
                    onHitDisruption: definition.OnHitDisruption,
                    disruptionResponseSegments: definition.DisruptionResponseSegments,
                    onGuardedDisruption: definition.OnGuardedDisruption,
                    clash: definition.Clash,
                    onEvadedDisruption: definition.OnEvadedDisruption,
                    resolutionTimes: ScheduleResolutionTimes(
                        definition.SustainedAttack,
                        currentTime,
                        definition.ResolutionOffset)));
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
                ShiftDisruptionPauses(action.DisruptionPauses, startShift),
                ShiftActionRestarts(action.Restarts, startShift),
                action.RequiredCompletedSequenceIndex,
                ShiftCancellation(action.Cancellation, startShift),
                action.OnGuardedDisruption,
                action.Clash,
                action.OnEvadedDisruption,
                ShiftResolutionTimes(action.ResolutionTimes, resolutionShift));
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
                ApplyActiveWindowInterruption(
                    action.ActiveWindows,
                    action.StartTime,
                    disruptionTime,
                    checked(disruptionTime + duration),
                    shiftFutureWindowsBy: duration,
                    replayAtTime: null,
                    cancelFutureWindows: false),
                action.OnHitDisruption,
                action.DisruptionResponseSegments,
                checked(action.AccumulatedPauseDuration + duration),
                Array.AsReadOnly(pauses),
                action.Restarts,
                action.RequiredCompletedSequenceIndex,
                action.Cancellation,
                action.OnGuardedDisruption,
                action.Clash,
                action.OnEvadedDisruption,
                PauseResolutionTimes(
                    action.ResolutionTimes,
                    disruptionTime,
                    duration));
        }

        private static ScheduledTimelineAction RestartScheduledAction(
            ScheduledTimelineAction action,
            int disruptionTime,
            int duration,
            ActionDisruptionType disruptionType)
        {
            int restartTime = checked(disruptionTime + duration);
            int actionDuration = action.GetLocalTimeAt(action.EndTime);
            int resolutionOffset = action.GetLocalTimeAt(action.ResolutionTime);
            var restarts = new ScheduledActionRestart[action.Restarts.Count + 1];
            for (int index = 0; index < action.Restarts.Count; index++)
            {
                restarts[index] = action.Restarts[index];
            }

            restarts[restarts.Length - 1] = new ScheduledActionRestart(
                disruptionType,
                action.StartTime,
                disruptionTime,
                restartTime);

            return new ScheduledTimelineAction(
                action.Side,
                action.SequenceIndex,
                action.ActionId,
                action.DisplayName,
                restartTime,
                checked(restartTime + actionDuration),
                checked(restartTime + resolutionOffset),
                action.DamageEffect,
                action.ResolutionEffect,
                action.DamageTags,
                action.StatusEffect,
                action.StatusTarget,
                action.ResolutionWindowStartTime.HasValue
                    ? (int?)checked(restartTime
                        + action.GetLocalTimeAt(action.ResolutionWindowStartTime.Value))
                    : null,
                action.ResolutionWindowEndTime.HasValue
                    ? (int?)checked(restartTime
                        + action.GetLocalTimeAt(action.ResolutionWindowEndTime.Value))
                    : null,
                action.ChargeLevels,
                ApplyActiveWindowInterruption(
                    action.ActiveWindows,
                    action.StartTime,
                    disruptionTime,
                    restartTime,
                    shiftFutureWindowsBy: 0,
                    replayAtTime: restartTime,
                    cancelFutureWindows: true),
                action.OnHitDisruption,
                action.DisruptionResponseSegments,
                accumulatedPauseDuration: 0,
                disruptionPauses: Array.Empty<ScheduledDisruptionPause>(),
                restarts: Array.AsReadOnly(restarts),
                requiredCompletedSequenceIndex: action.RequiredCompletedSequenceIndex,
                onGuardedDisruption: action.OnGuardedDisruption,
                clash: action.Clash,
                onEvadedDisruption: action.OnEvadedDisruption,
                resolutionTimes: RestartResolutionTimes(
                    action,
                    restartTime));
        }

        private static ScheduledTimelineAction CancelScheduledAction(
            ScheduledTimelineAction action,
            int cancellationTime,
            int recoveryEndTime,
            ActionDisruptionType disruptionType,
            bool dependencyFailed)
        {
            return new ScheduledTimelineAction(
                action.Side,
                action.SequenceIndex,
                action.ActionId,
                action.DisplayName,
                action.StartTime,
                dependencyFailed ? action.EndTime : recoveryEndTime,
                action.ResolutionTime,
                action.DamageEffect,
                action.ResolutionEffect,
                action.DamageTags,
                action.StatusEffect,
                action.StatusTarget,
                action.ResolutionWindowStartTime,
                action.ResolutionWindowEndTime,
                action.ChargeLevels,
                ApplyActiveWindowInterruption(
                    action.ActiveWindows,
                    action.StartTime,
                    cancellationTime,
                    recoveryEndTime,
                    shiftFutureWindowsBy: 0,
                    replayAtTime: null,
                    cancelFutureWindows: true),
                action.OnHitDisruption,
                action.DisruptionResponseSegments,
                action.AccumulatedPauseDuration,
                action.DisruptionPauses,
                action.Restarts,
                action.RequiredCompletedSequenceIndex,
                new ScheduledActionCancellation(
                    disruptionType,
                    action.StartTime,
                    action.EndTime,
                    cancellationTime,
                    recoveryEndTime,
                    dependencyFailed),
                action.OnGuardedDisruption,
                action.Clash,
                action.OnEvadedDisruption,
                action.ResolutionTimes);
        }

        private static IReadOnlyList<int> ScheduleResolutionTimes(
            SustainedAttackDefinition sustainedAttack,
            int actionStartTime,
            int fallbackResolutionOffset)
        {
            if (sustainedAttack == null)
            {
                return new[] { checked(actionStartTime + fallbackResolutionOffset) };
            }

            IReadOnlyList<int> offsets = sustainedAttack.BuildHitOffsets();
            var times = new int[offsets.Count];
            for (int index = 0; index < offsets.Count; index++)
            {
                times[index] = checked(actionStartTime + offsets[index]);
            }

            return Array.AsReadOnly(times);
        }

        private static IReadOnlyList<int> ShiftResolutionTimes(
            IReadOnlyList<int> source,
            int shift)
        {
            var shifted = new int[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                shifted[index] = checked(source[index] + shift);
            }

            return Array.AsReadOnly(shifted);
        }

        private static IReadOnlyList<int> PauseResolutionTimes(
            IReadOnlyList<int> source,
            int disruptionTime,
            int duration)
        {
            var shifted = new int[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                shifted[index] = source[index] > disruptionTime
                    ? checked(source[index] + duration)
                    : source[index];
            }

            return Array.AsReadOnly(shifted);
        }

        private static IReadOnlyList<int> RestartResolutionTimes(
            ScheduledTimelineAction action,
            int restartTime)
        {
            var restarted = new int[action.ResolutionTimes.Count];
            for (int index = 0; index < action.ResolutionTimes.Count; index++)
            {
                restarted[index] = checked(
                    restartTime + action.GetLocalTimeAt(action.ResolutionTimes[index]));
            }

            return Array.AsReadOnly(restarted);
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
                    definition.DisruptionPolicy,
                    actionStartTime,
                    definition.StartOffset,
                    definition.EndOffset,
                    superArmorLevel: definition.SuperArmorLevel);
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
                    window.DisruptionPolicy,
                    checked(window.SourceActionStartTime + shift),
                    window.LocalStartOffset,
                    window.LocalEndOffset,
                    window.BreakTime.HasValue
                        ? (int?)checked(window.BreakTime.Value + shift)
                        : null,
                    ShiftSuppressions(window.Suppressions, shift),
                    window.SuperArmorLevel);
            }

            return Array.AsReadOnly(shifted);
        }

        private static IReadOnlyList<ScheduledActiveWindow> ApplyActiveWindowInterruption(
            IReadOnlyList<ScheduledActiveWindow> windows,
            int currentActionStartTime,
            int disruptionTime,
            int disruptionEndTime,
            int shiftFutureWindowsBy,
            int? replayAtTime,
            bool cancelFutureWindows)
        {
            if (windows.Count == 0)
            {
                return windows;
            }

            var updated = new List<ScheduledActiveWindow>(
                windows.Count + (replayAtTime.HasValue ? windows.Count : 0));
            for (int index = 0; index < windows.Count; index++)
            {
                ScheduledActiveWindow window = windows[index];
                if (window.SourceActionStartTime != currentActionStartTime
                    || window.EndTime < disruptionTime)
                {
                    updated.Add(window);
                    continue;
                }

                if (window.StartTime > disruptionTime)
                {
                    if (!cancelFutureWindows)
                    {
                        updated.Add(ShiftSingleActiveWindow(
                            window,
                            shiftFutureWindowsBy,
                            shiftSourceActionStartTime: false));
                    }

                    continue;
                }

                updated.Add(ApplyPolicyToActiveWindow(
                    window,
                    disruptionTime,
                    disruptionEndTime));
            }

            if (replayAtTime.HasValue)
            {
                var replayedIds = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < windows.Count; index++)
                {
                    ScheduledActiveWindow template = windows[index];
                    if (template.SourceActionStartTime != currentActionStartTime
                        || !replayedIds.Add(template.WindowId))
                    {
                        continue;
                    }

                    updated.Add(new ScheduledActiveWindow(
                        template.WindowId,
                        template.DisplayName,
                        checked(replayAtTime.Value + template.LocalStartOffset),
                        checked(replayAtTime.Value + template.LocalEndOffset),
                        template.Effect,
                        template.DamageReduction,
                        template.DisruptionPolicy,
                        replayAtTime.Value,
                        template.LocalStartOffset,
                        template.LocalEndOffset,
                        superArmorLevel: template.SuperArmorLevel));
                }
            }

            return Array.AsReadOnly(updated.ToArray());
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

        private static ScheduledActiveWindow ApplyPolicyToActiveWindow(
            ScheduledActiveWindow window,
            int disruptionTime,
            int disruptionEndTime)
        {
            switch (window.DisruptionPolicy)
            {
                case ActiveWindowDisruptionPolicy.BreakOnDisruption:
                    return new ScheduledActiveWindow(
                        window.WindowId,
                        window.DisplayName,
                        window.StartTime,
                        window.EndTime,
                        window.Effect,
                        window.DamageReduction,
                        window.DisruptionPolicy,
                        window.SourceActionStartTime,
                        window.LocalStartOffset,
                        window.LocalEndOffset,
                        window.BreakTime.HasValue
                            ? (int?)Math.Min(window.BreakTime.Value, disruptionTime)
                            : disruptionTime,
                        window.Suppressions,
                        window.SuperArmorLevel);
                case ActiveWindowDisruptionPolicy.SuppressDuringDisruption:
                    var suppressions = new ScheduledActiveWindowSuppression[
                        window.Suppressions.Count + 1];
                    for (int index = 0; index < window.Suppressions.Count; index++)
                    {
                        suppressions[index] = window.Suppressions[index];
                    }

                    suppressions[suppressions.Length - 1] =
                        new ScheduledActiveWindowSuppression(
                            disruptionTime,
                            disruptionEndTime);
                    return new ScheduledActiveWindow(
                        window.WindowId,
                        window.DisplayName,
                        window.StartTime,
                        window.EndTime,
                        window.Effect,
                        window.DamageReduction,
                        window.DisruptionPolicy,
                        window.SourceActionStartTime,
                        window.LocalStartOffset,
                        window.LocalEndOffset,
                        window.BreakTime,
                        Array.AsReadOnly(suppressions),
                        window.SuperArmorLevel);
                default:
                    return window;
            }
        }

        private static ScheduledActiveWindow ShiftSingleActiveWindow(
            ScheduledActiveWindow window,
            int shift,
            bool shiftSourceActionStartTime)
        {
            if (shift == 0)
            {
                return window;
            }

            return new ScheduledActiveWindow(
                window.WindowId,
                window.DisplayName,
                checked(window.StartTime + shift),
                checked(window.EndTime + shift),
                window.Effect,
                window.DamageReduction,
                window.DisruptionPolicy,
                checked(window.SourceActionStartTime
                    + (shiftSourceActionStartTime ? shift : 0)),
                window.LocalStartOffset,
                window.LocalEndOffset,
                window.BreakTime.HasValue
                    ? (int?)checked(window.BreakTime.Value + shift)
                    : null,
                ShiftSuppressions(window.Suppressions, shift),
                window.SuperArmorLevel);
        }

        private static IReadOnlyList<ScheduledActiveWindowSuppression> ShiftSuppressions(
            IReadOnlyList<ScheduledActiveWindowSuppression> suppressions,
            int shift)
        {
            if (suppressions.Count == 0 || shift == 0)
            {
                return suppressions;
            }

            var shifted = new ScheduledActiveWindowSuppression[suppressions.Count];
            for (int index = 0; index < suppressions.Count; index++)
            {
                ScheduledActiveWindowSuppression suppression = suppressions[index];
                shifted[index] = new ScheduledActiveWindowSuppression(
                    checked(suppression.StartTime + shift),
                    checked(suppression.EndTime + shift));
            }

            return Array.AsReadOnly(shifted);
        }

        private static IReadOnlyList<ScheduledActionRestart> ShiftActionRestarts(
            IReadOnlyList<ScheduledActionRestart> restarts,
            int shift)
        {
            if (restarts.Count == 0 || shift == 0)
            {
                return restarts;
            }

            var shifted = new ScheduledActionRestart[restarts.Count];
            for (int index = 0; index < restarts.Count; index++)
            {
                ScheduledActionRestart restart = restarts[index];
                shifted[index] = new ScheduledActionRestart(
                    restart.Type,
                    checked(restart.PreviousStartTime + shift),
                    checked(restart.DisruptionTime + shift),
                    checked(restart.RestartTime + shift));
            }

            return Array.AsReadOnly(shifted);
        }

        private static ScheduledActionCancellation ShiftCancellation(
            ScheduledActionCancellation cancellation,
            int shift)
        {
            if (cancellation == null || shift == 0)
            {
                return cancellation;
            }

            return new ScheduledActionCancellation(
                cancellation.Type,
                checked(cancellation.OriginalStartTime + shift),
                checked(cancellation.OriginalEndTime + shift),
                checked(cancellation.CancellationTime + shift),
                checked(cancellation.RecoveryEndTime + shift),
                cancellation.DependencyFailed);
        }
    }
}
