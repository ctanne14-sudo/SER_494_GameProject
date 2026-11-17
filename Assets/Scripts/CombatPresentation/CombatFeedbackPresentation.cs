using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.SoloCombo;

namespace SER494.Gameplay.CombatPresentation
{
    public enum CombatFeedbackTone
    {
        Damage,
        Guard,
        Evade,
        Clash,
        Heal,
        Buff,
        Debuff,
        Stamina,
        Flinch,
        Stagger,
        Knockdown,
        Canceled
    }

    public readonly struct CombatFeedbackCue
    {
        public CombatFeedbackCue(
            TimelineActorSide targetSide,
            CombatFeedbackTone tone,
            string primaryText,
            string detailText = null)
        {
            TargetSide = targetSide;
            Tone = tone;
            PrimaryText = !string.IsNullOrWhiteSpace(primaryText)
                ? primaryText
                : throw new ArgumentException(
                    "Combat feedback requires visible text.",
                    nameof(primaryText));
            DetailText = detailText ?? string.Empty;
        }

        public TimelineActorSide TargetSide { get; }

        public CombatFeedbackTone Tone { get; }

        public string PrimaryText { get; }

        public string DetailText { get; }
    }

    /// <summary>
    /// Converts completed combat results into presentation-only cues. This layer never
    /// predicts or modifies combat state; it only describes an authoritative resolution.
    /// </summary>
    public static class CombatFeedbackPresentation
    {
        private static readonly IReadOnlyList<CombatFeedbackCue> NoCues =
            Array.Empty<CombatFeedbackCue>();

        public static IReadOnlyList<CombatFeedbackCue> Build(
            SoloComboActionResolution resolution)
        {
            if (!resolution.Succeeded)
            {
                return NoCues;
            }

            var cues = new List<CombatFeedbackCue>(3);
            TimelineActorSide actorSide = resolution.Side;
            TimelineActorSide opponentSide = Opposite(actorSide);

            if (resolution.DamageAvoidance == DamageAvoidanceResult.Guarded)
            {
                cues.Add(new CombatFeedbackCue(
                    opponentSide,
                    CombatFeedbackTone.Guard,
                    "GUARD"));
            }
            else if (resolution.DamageAvoidance == DamageAvoidanceResult.Evaded)
            {
                cues.Add(new CombatFeedbackCue(
                    opponentSide,
                    CombatFeedbackTone.Evade,
                    "EVADE"));
            }

            // A Guard normally prevents the complete hit package today, but the
            // presentation also supports future partial-Guard rules. An Evade is the
            // only avoidance result that always suppresses damage presentation.
            if (resolution.DamageAvoidance != DamageAvoidanceResult.Evaded)
            {
                AddResolvedEffect(cues, resolution, actorSide, opponentSide);
            }

            if (resolution.ClashDamageReduction > 0d)
            {
                cues.Insert(
                    0,
                    new CombatFeedbackCue(
                        opponentSide,
                        CombatFeedbackTone.Clash,
                        "CLASH"));
            }

            AddDisruption(cues, resolution);
            return cues.Count > 0 ? cues.AsReadOnly() : NoCues;
        }

        private static void AddResolvedEffect(
            ICollection<CombatFeedbackCue> cues,
            SoloComboActionResolution resolution,
            TimelineActorSide actorSide,
            TimelineActorSide opponentSide)
        {
            switch (resolution.ResolutionEffect)
            {
                case ActionResolutionEffect.Damage:
                    if (resolution.DamageDealt > 0)
                    {
                        cues.Add(new CombatFeedbackCue(
                            opponentSide,
                            CombatFeedbackTone.Damage,
                            $"-{resolution.DamageDealt}"));
                    }

                    break;
                case ActionResolutionEffect.Guard:
                    cues.Add(new CombatFeedbackCue(
                        actorSide,
                        CombatFeedbackTone.Guard,
                        "GUARD"));
                    break;
                case ActionResolutionEffect.Evade:
                    cues.Add(new CombatFeedbackCue(
                        actorSide,
                        CombatFeedbackTone.Evade,
                        "EVADE"));
                    break;
                case ActionResolutionEffect.Heal:
                    if (resolution.HealingDone > 0)
                    {
                        cues.Add(new CombatFeedbackCue(
                            actorSide,
                            CombatFeedbackTone.Heal,
                            $"+{resolution.HealingDone} HP"));
                    }

                    break;
                case ActionResolutionEffect.RestoreStamina:
                    if (resolution.StaminaRestored > 0)
                    {
                        cues.Add(new CombatFeedbackCue(
                            actorSide,
                            CombatFeedbackTone.Stamina,
                            $"+{resolution.StaminaRestored} STAMINA"));
                    }

                    break;
                case ActionResolutionEffect.ApplyBuff:
                case ActionResolutionEffect.ApplyDebuff:
                    if (resolution.StatusApplied)
                    {
                        TimelineActorSide recipient =
                            resolution.StatusTarget == ActionStatusTarget.Self
                                ? actorSide
                                : opponentSide;
                        bool isBuff = resolution.ResolutionEffect
                            == ActionResolutionEffect.ApplyBuff;
                        cues.Add(new CombatFeedbackCue(
                            recipient,
                            isBuff
                                ? CombatFeedbackTone.Buff
                                : CombatFeedbackTone.Debuff,
                            isBuff ? "BUFF" : "DEBUFF",
                            resolution.StatusDisplayName));
                    }

                    break;
            }
        }

        private static void AddDisruption(
            ICollection<CombatFeedbackCue> cues,
            SoloComboActionResolution resolution)
        {
            if (resolution.AppliedDisruption == null)
            {
                return;
            }

            CombatFeedbackTone tone;
            switch (resolution.AppliedDisruption.Type)
            {
                case ActionDisruptionType.Flinch:
                    tone = CombatFeedbackTone.Flinch;
                    break;
                case ActionDisruptionType.Stagger:
                    tone = CombatFeedbackTone.Stagger;
                    break;
                case ActionDisruptionType.Knockdown:
                    tone = CombatFeedbackTone.Knockdown;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(resolution),
                        resolution.AppliedDisruption.Type,
                        "Unsupported disruption feedback type.");
            }

            cues.Add(new CombatFeedbackCue(
                resolution.DisruptedSide,
                tone,
                resolution.AppliedDisruption.Type.ToString().ToUpperInvariant(),
                $"{resolution.AppliedDisruption.Duration} Time Steps"));

            if (resolution.DisruptionResponse == ActionDisruptionResponse.CancelAction)
            {
                cues.Add(new CombatFeedbackCue(
                    resolution.DisruptedSide,
                    CombatFeedbackTone.Canceled,
                    "CANCELED"));
            }
        }

        private static TimelineActorSide Opposite(TimelineActorSide side)
        {
            return side == TimelineActorSide.Player
                ? TimelineActorSide.Enemy
                : TimelineActorSide.Player;
        }
    }
}
