using System.Collections.Generic;
using NUnit.Framework;
using SER494.Gameplay.BattleResult.Unity;
using SER494.Gameplay.Combat;
using SER494.Gameplay.CombatIntegration.Unity;

namespace SER494.Gameplay.CombatIntegration.Tests
{
    [Category("CombatIntegration")]
    public sealed class BattleResultPresentationTests
    {
        private const string HorizontalSlashActionId =
            "sword-and-shield.opening.horizontal-slash";

        [Test]
        public void Victory_PresentsCommittedGoldMaterialsLearnedActionAndExplorationReturn()
        {
            var rewards = new BattleRewardSummary(
                200,
                new Dictionary<string, int>
                {
                    [BattleRewardSummary.BrassMaterialId] = 2,
                    [BattleRewardSummary.BronzeMaterialId] = 1
                },
                HorizontalSlashActionId);

            BattleResultPresentation result = BattleResultPresentationFactory.Create(
                BattleOutcome.PlayerVictory,
                "Dungeon Guard",
                rewards);

            Assert.That(result.OutcomeTitle, Is.EqualTo("VICTORY"));
            Assert.That(result.EnemyName, Is.EqualTo("Dungeon Guard"));
            Assert.That(result.Gold, Is.EqualTo(200));
            Assert.That(result.Materials.Count, Is.EqualTo(2));
            Assert.That(result.Materials[0].DisplayName, Is.EqualTo("Brass"));
            Assert.That(result.Materials[0].Quantity, Is.EqualTo(2));
            Assert.That(result.Materials[1].DisplayName, Is.EqualTo("Bronze"));
            Assert.That(result.HasLearnedAction, Is.True);
            Assert.That(result.LearnedActionName, Is.EqualTo("Horizontal Slash [O]"));
            Assert.That(result.ContinueLabel, Is.EqualTo("Continue Exploring"));
            Assert.That(result.HasRewards, Is.True);
        }

        [Test]
        public void Defeat_NeverPresentsConfiguredRewardsAndReturnsToLastSave()
        {
            var rewards = new BattleRewardSummary(
                999,
                new Dictionary<string, int>
                {
                    [BattleRewardSummary.WhiteCopperMaterialId] = 3
                },
                HorizontalSlashActionId);

            BattleResultPresentation result = BattleResultPresentationFactory.Create(
                BattleOutcome.PlayerDefeat,
                "Ape Beast",
                rewards);

            Assert.That(result.OutcomeTitle, Is.EqualTo("DEFEAT"));
            Assert.That(result.EnemyName, Is.EqualTo("Ape Beast"));
            Assert.That(result.Gold, Is.Zero);
            Assert.That(result.Materials.Count, Is.Zero);
            Assert.That(result.HasLearnedAction, Is.False);
            Assert.That(result.LearnedActionName, Is.Null);
            Assert.That(result.HasRewards, Is.False);
            Assert.That(result.ContinueLabel, Is.EqualTo("Return to Last Save"));
        }

        [Test]
        public void MutualDestruction_PreservesRewardsAndExplainsOneResourceReturn()
        {
            var rewards = new BattleRewardSummary(
                150,
                null,
                HorizontalSlashActionId);

            BattleResultPresentation result = BattleResultPresentationFactory.Create(
                BattleOutcome.Draw,
                "Armored Guard",
                rewards);

            Assert.That(result.OutcomeTitle, Is.EqualTo("MUTUAL DESTRUCTION"));
            Assert.That(result.Gold, Is.EqualTo(150));
            Assert.That(result.HasLearnedAction, Is.True);
            Assert.That(result.LearnedActionName, Is.EqualTo("Horizontal Slash [O]"));
            Assert.That(result.ConsequenceBody, Does.Contain("1 HP"));
            Assert.That(result.ConsequenceBody, Does.Contain("1 Stamina"));
            Assert.That(result.ContinueLabel, Is.EqualTo("Return to Save Point"));
            Assert.That(result.HasRewards, Is.True);
        }

        [Test]
        public void OngoingOutcome_IsRejected()
        {
            Assert.That(
                () => BattleResultPresentationFactory.Create(
                    BattleOutcome.Ongoing,
                    "Dungeon Guard",
                    BattleRewardSummary.None),
                Throws.ArgumentException);
        }
    }
}
