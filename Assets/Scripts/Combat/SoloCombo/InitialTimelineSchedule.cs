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
            int? resolutionWindowEndTime = null)
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
