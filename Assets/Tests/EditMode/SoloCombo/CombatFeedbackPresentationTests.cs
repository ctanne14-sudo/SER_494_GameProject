using System.Linq;
using NUnit.Framework;
using SER494.Gameplay.CombatPresentation;
using SER494.Gameplay.Combat.SoloCombo;

namespace SER494.Gameplay.Combat.Tests.SoloCombo
{
    [TestFixture]
    [Category("CoreCombat")]
    public sealed class CombatFeedbackPresentationTests
    {
        [Test]
        public void DamageAndCancelDisruption_TargetDefenderAndExposeActualResults()
        {
            var resolution = CreateResolution(
                side: TimelineActorSide.Player,
                damageDealt: 23,
                appliedDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    4),
                disruptionResponse: ActionDisruptionResponse.CancelAction,
                disruptedSide: TimelineActorSide.Enemy);

            var cues = CombatFeedbackPresentation.Build(resolution);

            Assert.That(cues.Select(cue => cue.PrimaryText), Is.EqualTo(
                new[] { "-23", "STAGGER", "CANCELED" }));
            Assert.That(cues.All(cue => cue.TargetSide == TimelineActorSide.Enemy),
                Is.True);
        }

        [Test]
        public void EvadedDamage_ShowsEvadeWithoutDamageNumber()
        {
            var resolution = CreateResolution(
                side: TimelineActorSide.Enemy,
                damageDealt: 0,
                damageAvoidance: DamageAvoidanceResult.Evaded);

            var cues = CombatFeedbackPresentation.Build(resolution);

            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Tone, Is.EqualTo(CombatFeedbackTone.Evade));
            Assert.That(cues[0].TargetSide, Is.EqualTo(TimelineActorSide.Player));
        }

        [Test]
        public void PartiallyGuardedDamage_ShowsGuardAndActualDamage()
        {
            var resolution = CreateResolution(
                side: TimelineActorSide.Enemy,
                damageDealt: 7,
                damageAvoidance: DamageAvoidanceResult.Guarded);

            var cues = CombatFeedbackPresentation.Build(resolution);

            Assert.That(cues.Select(cue => cue.PrimaryText), Is.EqualTo(
                new[] { "GUARD", "-7" }));
            Assert.That(cues.All(cue => cue.TargetSide == TimelineActorSide.Player),
                Is.True);
        }

        [Test]
        public void AppliedOpponentDebuff_TargetsOpponentAndNamesStatus()
        {
            var resolution = new SoloComboActionResolution(
                TimelineActorSide.Player,
                "test.debuff",
                "Test Debuff",
                timeStep: 3,
                damageDealt: 0,
                ActionResolutionEffect.ApplyDebuff,
                DamageAvoidanceResult.None,
                DamageResolutionTag.None,
                statusDisplayName: "Off-Balance",
                statusTarget: ActionStatusTarget.Opponent,
                statusApplied: true);

            var cues = CombatFeedbackPresentation.Build(resolution);

            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Tone, Is.EqualTo(CombatFeedbackTone.Debuff));
            Assert.That(cues[0].DetailText, Is.EqualTo("Off-Balance"));
            Assert.That(cues[0].TargetSide, Is.EqualTo(TimelineActorSide.Enemy));
        }

        private static SoloComboActionResolution CreateResolution(
            TimelineActorSide side,
            int damageDealt,
            DamageAvoidanceResult damageAvoidance = DamageAvoidanceResult.None,
            ActionDisruptionDefinition appliedDisruption = null,
            ActionDisruptionResponse disruptionResponse =
                ActionDisruptionResponse.Continue,
            TimelineActorSide disruptedSide = TimelineActorSide.Player)
        {
            return new SoloComboActionResolution(
                side,
                "test.damage",
                "Test Damage",
                timeStep: 5,
                damageDealt,
                ActionResolutionEffect.Damage,
                damageAvoidance,
                DamageResolutionTag.None,
                statusDisplayName: null,
                statusTarget: ActionStatusTarget.Self,
                statusApplied: false,
                appliedDisruption: appliedDisruption,
                disruptionResponse: disruptionResponse,
                disruptedSide: disruptedSide);
        }
    }
}
