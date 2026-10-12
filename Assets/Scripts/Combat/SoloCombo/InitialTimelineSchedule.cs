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

    public sealed class ScheduledActiveWindow
    {
        internal ScheduledActiveWindow(
            string windowId,
            string displayName,
            int startTime,
            int endTime,
            ActiveWindowEffect effect,
            double damageReduction,
            ActiveWindowDisruptionPolicy disruptionPolicy)
        {
            WindowId = windowId;
            DisplayName = displayName;
            StartTime = startTime;
            EndTime = endTime;
            Effect = effect;
            DamageReduction = damageReduction;
            DisruptionPolicy = disruptionPolicy;
        }

        public string WindowId { get; }

        public string DisplayName { get; }

        public int StartTime { get; }

        public int EndTime { get; }

        public ActiveWindowEffect Effect { get; }

        public double DamageReduction { get; }

        public ActiveWindowDisruptionPolicy DisruptionPolicy { get; }

        public bool IsActiveAt(int timeStep)
        {
            return timeStep >= StartTime && timeStep <= EndTime;
        }
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
            int accumulatedPauseDuration = 0)
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

        public IReadOnlyList<DisruptionResponseSegmentDefinition> DisruptionResponseSegments { get; }

        public int AccumulatedPauseDuration { get; }

        public int GetLocalTimeAt(int timeStep)
        {
            return timeStep - StartTime - AccumulatedPauseDuration;
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
