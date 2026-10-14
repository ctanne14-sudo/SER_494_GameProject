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
            ActionDisruptionDefinition onEvadedDisruption = null)
        {
            Side = side;
            SequenceIndex = sequenceIndex;
            ActionId = actionId;
            DisplayName = displayName;
            StartTime = startTime;
            EndTime = endTime;
            ResolutionTime = resolutionTime;
            DamageEffect = damageEffect;
            ResolutionEffect = resolutionEffect;
            DamageTags = damageTags;
            StatusEffect = statusEffect;
            StatusTarget = statusTarget;
            ResolutionWindowStartTime = resolutionWindowStartTime;
            ResolutionWindowEndTime = resolutionWindowEndTime;
            ChargeLevels = chargeLevels
                ?? Array.Empty<ComboActionChargeLevelDefinition>();
            ActiveWindows = activeWindows ?? Array.Empty<ScheduledActiveWindow>();
            OnHitDisruption = onHitDisruption;
            DisruptionResponseSegments = disruptionResponseSegments
                ?? Array.Empty<DisruptionResponseSegmentDefinition>();
            AccumulatedPauseDuration = accumulatedPauseDuration;
            DisruptionPauses = disruptionPauses
                ?? Array.Empty<ScheduledDisruptionPause>();
            Restarts = restarts ?? Array.Empty<ScheduledActionRestart>();
            RequiredCompletedSequenceIndex = requiredCompletedSequenceIndex;
            Cancellation = cancellation;
            OnGuardedDisruption = onGuardedDisruption;
            Clash = clash;
            OnEvadedDisruption = onEvadedDisruption;
        }

        public TimelineActorSide Side { get; }

        public int SequenceIndex { get; }

        public string ActionId { get; }

        public string DisplayName { get; }

        public int StartTime { get; }

        public int EndTime { get; }

        public int ResolutionTime { get; }

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
