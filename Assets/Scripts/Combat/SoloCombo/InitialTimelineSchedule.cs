using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public enum TimelineActorSide
    {
        Player,
        Enemy
    }

    public sealed class ScheduledActiveWindowSuppression
    {
        internal ScheduledActiveWindowSuppression(int startTime, int endTime)
        {
            if (endTime <= startTime)
            {
                throw new ArgumentException(
                    "An Active Window suppression must end after it begins.");
            }

            StartTime = startTime;
            EndTime = endTime;
        }

        public int StartTime { get; }

        public int EndTime { get; }

        public bool Suppresses(int timeStep)
        {
            // The disruption is applied after all effects at StartTime resolve.
            return timeStep > StartTime && timeStep <= EndTime;
        }
    }

    public sealed class ScheduledActiveWindow
    {
        internal ScheduledActiveWindow(
            string windowId,
            string displayName,
            int startTime,
            int endTime,
            ActiveWindowEffect effect,
            double damageReduction,
            ActiveWindowDisruptionPolicy disruptionPolicy,
            int sourceActionStartTime,
            int localStartOffset,
            int localEndOffset,
            int? breakTime = null,
            IReadOnlyList<ScheduledActiveWindowSuppression> suppressions = null,
            int superArmorLevel = 0)
        {
            WindowId = windowId;
            DisplayName = displayName;
            StartTime = startTime;
            EndTime = endTime;
            Effect = effect;
            DamageReduction = damageReduction;
            SuperArmorLevel = superArmorLevel;
            DisruptionPolicy = disruptionPolicy;
            SourceActionStartTime = sourceActionStartTime;
            LocalStartOffset = localStartOffset;
            LocalEndOffset = localEndOffset;
            BreakTime = breakTime;
            Suppressions = suppressions
                ?? Array.Empty<ScheduledActiveWindowSuppression>();
        }

        public string WindowId { get; }

        public string DisplayName { get; }

        public int StartTime { get; }

        public int EndTime { get; }

        public ActiveWindowEffect Effect { get; }

        public double DamageReduction { get; }

        public int SuperArmorLevel { get; }

        public ActiveWindowDisruptionPolicy DisruptionPolicy { get; }

        public int SourceActionStartTime { get; }

        public int LocalStartOffset { get; }

        public int LocalEndOffset { get; }

        public int? BreakTime { get; }

        public IReadOnlyList<ScheduledActiveWindowSuppression> Suppressions { get; }

        public int EffectiveEndTime => BreakTime.HasValue
            ? Math.Min(EndTime, BreakTime.Value)
            : EndTime;

        public bool IsActiveAt(int timeStep)
        {
            if (timeStep < StartTime || timeStep > EffectiveEndTime)
            {
                return false;
            }

            for (int index = 0; index < Suppressions.Count; index++)
            {
                if (Suppressions[index].Suppresses(timeStep))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class ScheduledDisruptionPause
    {
        internal ScheduledDisruptionPause(
            ActionDisruptionType type,
            int startTime,
            int duration)
        {
            Type = type;
            StartTime = startTime;
            Duration = duration;
        }

        public ActionDisruptionType Type { get; }

        public int StartTime { get; }

        public int Duration { get; }

        public int EndTime => checked(StartTime + Duration);
    }

    public sealed class ScheduledActionRestart
    {
        internal ScheduledActionRestart(
            ActionDisruptionType type,
            int previousStartTime,
            int disruptionTime,
            int restartTime)
        {
            Type = type;
            PreviousStartTime = previousStartTime;
            DisruptionTime = disruptionTime;
            RestartTime = restartTime;
        }

        public ActionDisruptionType Type { get; }

        public int PreviousStartTime { get; }

        public int DisruptionTime { get; }

        public int RestartTime { get; }
    }

    public sealed class ScheduledActionCancellation
    {
        internal ScheduledActionCancellation(
            ActionDisruptionType type,
            int originalStartTime,
            int originalEndTime,
            int cancellationTime,
            int recoveryEndTime,
            bool dependencyFailed)
        {
            Type = type;
            OriginalStartTime = originalStartTime;
            OriginalEndTime = originalEndTime;
            CancellationTime = cancellationTime;
            RecoveryEndTime = recoveryEndTime;
            DependencyFailed = dependencyFailed;
        }

        public ActionDisruptionType Type { get; }

        public int OriginalStartTime { get; }

        public int OriginalEndTime { get; }

        public int CancellationTime { get; }

        public int RecoveryEndTime { get; }

        public bool DependencyFailed { get; }
    }

    public sealed class ScheduledResolutionEvent
    {
        internal ScheduledResolutionEvent(
            int time,
            DamageEffectDefinition damageEffect,
            ActionResolutionEffect effect,
            DamageResolutionTag damageTags,
            CombatStatusDefinition statusEffect,
            ActionStatusTarget statusTarget,
            ActionDisruptionDefinition onHitDisruption,
            ActionDisruptionDefinition onGuardedDisruption,
            DamageClashDefinition clash,
            ActionDisruptionDefinition onEvadedDisruption,
            int healingAmount = 0,
            ActionItemCostDefinition itemCost = null,
            ActionStatusRequirementDefinition statusRequirement = null)
        {
            Time = time;
            DamageEffect = damageEffect;
            Effect = effect;
            DamageTags = damageTags;
            StatusEffect = statusEffect;
            StatusTarget = statusTarget;
            OnHitDisruption = onHitDisruption;
            OnGuardedDisruption = onGuardedDisruption;
            Clash = clash;
            OnEvadedDisruption = onEvadedDisruption;
            HealingAmount = healingAmount;
            ItemCost = itemCost;
            StatusRequirement = statusRequirement;
        }

        public int Time { get; }
        public DamageEffectDefinition DamageEffect { get; }
        public bool HasDamage => DamageEffect != null;
        public ActionResolutionEffect Effect { get; }
        public DamageResolutionTag DamageTags { get; }
        public CombatStatusDefinition StatusEffect { get; }
        public ActionStatusTarget StatusTarget { get; }
        public ActionDisruptionDefinition OnHitDisruption { get; }
        public ActionDisruptionDefinition OnGuardedDisruption { get; }
        public DamageClashDefinition Clash { get; }
        public ActionDisruptionDefinition OnEvadedDisruption { get; }
        public int HealingAmount { get; }
        public ActionItemCostDefinition ItemCost { get; }
        public ActionStatusRequirementDefinition StatusRequirement { get; }
    }

    public sealed class ScheduledTimelineAction
    {
        internal ScheduledTimelineAction(
            TimelineActorSide side,
            int sequenceIndex,
            string actionId,
            string displayName,
            int startTime,
            int endTime,
            int resolutionTime,
            DamageEffectDefinition damageEffect,
            ActionResolutionEffect resolutionEffect,
            DamageResolutionTag damageTags,
            CombatStatusDefinition statusEffect,
            ActionStatusTarget statusTarget,
            int? resolutionWindowStartTime = null,
            int? resolutionWindowEndTime = null,
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null,
            IReadOnlyList<ScheduledActiveWindow> activeWindows = null,
            ActionDisruptionDefinition onHitDisruption = null,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments = null,
            int accumulatedPauseDuration = 0,
            IReadOnlyList<ScheduledDisruptionPause> disruptionPauses = null,
            IReadOnlyList<ScheduledActionRestart> restarts = null,
            int? requiredCompletedSequenceIndex = null,
            ScheduledActionCancellation cancellation = null,
            ActionDisruptionDefinition onGuardedDisruption = null,
            DamageClashDefinition clash = null,
            ActionDisruptionDefinition onEvadedDisruption = null,
            IReadOnlyList<int> resolutionTimes = null,
            IReadOnlyList<ScheduledResolutionEvent> resolutionEvents = null,
            int healingAmount = 0,
            ActionItemCostDefinition itemCost = null,
            ActionStatusRequirementDefinition statusRequirement = null)
        {
            Side = side;
            SequenceIndex = sequenceIndex;
            ActionId = actionId;
            DisplayName = displayName;
            StartTime = startTime;
            EndTime = endTime;
            ResolutionEvents = resolutionEvents ?? CreateLegacyResolutionEvents(
                resolutionTimes ?? new[] { resolutionTime },
                damageEffect,
                resolutionEffect,
                damageTags,
                statusEffect,
                statusTarget,
                onHitDisruption,
                onGuardedDisruption,
                clash,
                onEvadedDisruption,
                healingAmount,
                itemCost,
                statusRequirement);
            if (ResolutionEvents.Count == 0)
            {
                throw new ArgumentException("A scheduled Action needs a Resolution Event.");
            }

            ScheduledResolutionEvent primary = ResolutionEvents[0];
            ResolutionTime = primary.Time;
            ResolutionTimes = CopyResolutionTimes(ResolutionEvents);
            DamageEffect = primary.DamageEffect;
            ResolutionEffect = primary.Effect;
            DamageTags = primary.DamageTags;
            StatusEffect = primary.StatusEffect;
            StatusTarget = primary.StatusTarget;
            ResolutionWindowStartTime = resolutionWindowStartTime;
            ResolutionWindowEndTime = resolutionWindowEndTime;
            ChargeLevels = chargeLevels
                ?? Array.Empty<ComboActionChargeLevelDefinition>();
            ActiveWindows = activeWindows ?? Array.Empty<ScheduledActiveWindow>();
            OnHitDisruption = primary.OnHitDisruption;
            DisruptionResponseSegments = disruptionResponseSegments
                ?? Array.Empty<DisruptionResponseSegmentDefinition>();
            AccumulatedPauseDuration = accumulatedPauseDuration;
            DisruptionPauses = disruptionPauses
                ?? Array.Empty<ScheduledDisruptionPause>();
            Restarts = restarts ?? Array.Empty<ScheduledActionRestart>();
            RequiredCompletedSequenceIndex = requiredCompletedSequenceIndex;
            Cancellation = cancellation;
            OnGuardedDisruption = primary.OnGuardedDisruption;
            Clash = primary.Clash;
            OnEvadedDisruption = primary.OnEvadedDisruption;
            HealingAmount = primary.HealingAmount;
            ItemCost = primary.ItemCost;
            StatusRequirement = primary.StatusRequirement;
        }

        public TimelineActorSide Side { get; }

        public int SequenceIndex { get; }

        public string ActionId { get; }

        public string DisplayName { get; }

        public int StartTime { get; }

        public int EndTime { get; }

        public int ResolutionTime { get; }

        public IReadOnlyList<int> ResolutionTimes { get; }

        public IReadOnlyList<ScheduledResolutionEvent> ResolutionEvents { get; }

        public DamageEffectDefinition DamageEffect { get; }

        public bool HasDamage => DamageEffect != null;

        public ActionResolutionEffect ResolutionEffect { get; }

        public DamageResolutionTag DamageTags { get; }

        public CombatStatusDefinition StatusEffect { get; }

        public ActionStatusTarget StatusTarget { get; }

        public int? ResolutionWindowStartTime { get; }

        public int? ResolutionWindowEndTime { get; }

        public bool HasResolutionWindow => ResolutionWindowStartTime.HasValue;

        public IReadOnlyList<ComboActionChargeLevelDefinition> ChargeLevels { get; }

        public bool IsChargeable => ChargeLevels.Count > 0;

        public IReadOnlyList<ScheduledActiveWindow> ActiveWindows { get; }

        public ActionDisruptionDefinition OnHitDisruption { get; }

        public ActionDisruptionDefinition OnGuardedDisruption { get; }

        public DamageClashDefinition Clash { get; }

        public ActionDisruptionDefinition OnEvadedDisruption { get; }

        public int HealingAmount { get; }

        public ActionItemCostDefinition ItemCost { get; }

        public ActionStatusRequirementDefinition StatusRequirement { get; }

        public IReadOnlyList<DisruptionResponseSegmentDefinition> DisruptionResponseSegments { get; }

        public int AccumulatedPauseDuration { get; }

        public IReadOnlyList<ScheduledDisruptionPause> DisruptionPauses { get; }

        public IReadOnlyList<ScheduledActionRestart> Restarts { get; }

        public int? RequiredCompletedSequenceIndex { get; }

        public ScheduledActionCancellation Cancellation { get; }

        public bool IsCanceled => Cancellation != null;

        public int GetLocalTimeAt(int timeStep)
        {
            int localTime = timeStep - StartTime;
            for (int index = 0; index < DisruptionPauses.Count; index++)
            {
                ScheduledDisruptionPause pause = DisruptionPauses[index];
                if (timeStep <= pause.StartTime)
                {
                    continue;
                }

                localTime -= Math.Min(pause.Duration, timeStep - pause.StartTime);
            }

            return localTime;
        }

        public ActionDisruptionResponse GetDisruptionResponse(
            ActionDisruptionType type,
            int timeStep)
        {
            int localTime = GetLocalTimeAt(timeStep);
            for (int index = 0; index < DisruptionResponseSegments.Count; index++)
            {
                DisruptionResponseSegmentDefinition segment =
                    DisruptionResponseSegments[index];
                if (localTime >= segment.StartOffset && localTime <= segment.EndOffset)
                {
                    return segment.GetResponse(type);
                }
            }

            return ActionDisruptionResponse.Continue;
        }

        internal ScheduledTimelineAction CreateResolutionView(
            ScheduledResolutionEvent resolutionEvent)
        {
            return new ScheduledTimelineAction(
                Side,
                SequenceIndex,
                ActionId,
                DisplayName,
                StartTime,
                EndTime,
                resolutionEvent.Time,
                resolutionEvent.DamageEffect,
                resolutionEvent.Effect,
                resolutionEvent.DamageTags,
                resolutionEvent.StatusEffect,
                resolutionEvent.StatusTarget,
                ResolutionWindowStartTime,
                ResolutionWindowEndTime,
                ChargeLevels,
                ActiveWindows,
                resolutionEvent.OnHitDisruption,
                DisruptionResponseSegments,
                AccumulatedPauseDuration,
                DisruptionPauses,
                Restarts,
                RequiredCompletedSequenceIndex,
                Cancellation,
                resolutionEvent.OnGuardedDisruption,
                resolutionEvent.Clash,
                resolutionEvent.OnEvadedDisruption,
                resolutionEvents: new[] { resolutionEvent },
                healingAmount: resolutionEvent.HealingAmount,
                itemCost: resolutionEvent.ItemCost,
                statusRequirement: resolutionEvent.StatusRequirement);
        }

        internal bool UsesPrimaryResolutionData(ScheduledResolutionEvent resolutionEvent)
        {
            return resolutionEvent != null
                && ReferenceEquals(resolutionEvent.DamageEffect, DamageEffect)
                && resolutionEvent.Effect == ResolutionEffect
                && resolutionEvent.DamageTags == DamageTags
                && ReferenceEquals(resolutionEvent.StatusEffect, StatusEffect)
                && resolutionEvent.StatusTarget == StatusTarget
                && ReferenceEquals(resolutionEvent.OnHitDisruption, OnHitDisruption)
                && ReferenceEquals(resolutionEvent.OnGuardedDisruption, OnGuardedDisruption)
                && ReferenceEquals(resolutionEvent.Clash, Clash)
                && ReferenceEquals(resolutionEvent.OnEvadedDisruption, OnEvadedDisruption)
                && resolutionEvent.HealingAmount == HealingAmount
                && ReferenceEquals(resolutionEvent.ItemCost, ItemCost)
                && ReferenceEquals(resolutionEvent.StatusRequirement, StatusRequirement);
        }

        private static IReadOnlyList<ScheduledResolutionEvent> CreateLegacyResolutionEvents(
            IReadOnlyList<int> times,
            DamageEffectDefinition damageEffect,
            ActionResolutionEffect effect,
            DamageResolutionTag damageTags,
            CombatStatusDefinition statusEffect,
            ActionStatusTarget statusTarget,
            ActionDisruptionDefinition onHitDisruption,
            ActionDisruptionDefinition onGuardedDisruption,
            DamageClashDefinition clash,
            ActionDisruptionDefinition onEvadedDisruption,
            int healingAmount,
            ActionItemCostDefinition itemCost,
            ActionStatusRequirementDefinition statusRequirement)
        {
            var events = new ScheduledResolutionEvent[times.Count];
            for (int index = 0; index < times.Count; index++)
            {
                events[index] = new ScheduledResolutionEvent(
                    times[index],
                    damageEffect,
                    effect,
                    damageTags,
                    statusEffect,
                    statusTarget,
                    onHitDisruption,
                    onGuardedDisruption,
                    clash,
                    onEvadedDisruption,
                    healingAmount,
                    itemCost,
                    statusRequirement);
            }

            return Array.AsReadOnly(events);
        }

        private static IReadOnlyList<int> CopyResolutionTimes(
            IReadOnlyList<ScheduledResolutionEvent> events)
        {
            var times = new int[events.Count];
            for (int index = 0; index < events.Count; index++)
            {
                times[index] = events[index].Time;
            }

            return Array.AsReadOnly(times);
        }
    }

    public sealed class TimelineResolutionStep
    {
        internal TimelineResolutionStep(
            int timeStep,
            IReadOnlyList<ScheduledTimelineAction> actions)
        {
            TimeStep = timeStep;
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        public int TimeStep { get; }

        // All entries at this step must be collected before a resolver applies effects.
        public IReadOnlyList<ScheduledTimelineAction> Actions { get; }
    }

    public sealed class InitialTimelineSchedule
    {
        internal InitialTimelineSchedule(
            IReadOnlyList<ScheduledTimelineAction> actions,
            IReadOnlyList<TimelineResolutionStep> resolutionSteps,
            int completionTime)
        {
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            ResolutionSteps = resolutionSteps
                ?? throw new ArgumentNullException(nameof(resolutionSteps));
            CompletionTime = completionTime;
        }

        public IReadOnlyList<ScheduledTimelineAction> Actions { get; }

        public IReadOnlyList<TimelineResolutionStep> ResolutionSteps { get; }

        public int CompletionTime { get; }
    }
}
