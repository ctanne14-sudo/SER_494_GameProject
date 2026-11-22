using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;

namespace SER494.Gameplay.BattleResult.Unity
{
    public sealed class BattleResultMaterialLine
    {
        public BattleResultMaterialLine(string materialId, string displayName, int quantity)
        {
            MaterialId = materialId ?? throw new ArgumentNullException(nameof(materialId));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Quantity = quantity;
        }

        public string MaterialId { get; }

        public string DisplayName { get; }

        public int Quantity { get; }
    }

    public sealed class BattleResultPresentation
    {
        internal BattleResultPresentation(
            BattleOutcome outcome,
            string outcomeTitle,
            string enemyName,
            string rewardHeading,
            int gold,
            IReadOnlyList<BattleResultMaterialLine> materials,
            string learnedActionName,
            string consequenceHeading,
            string consequenceBody,
            string continueLabel,
            string outcomeStyleClass)
        {
            Outcome = outcome;
            OutcomeTitle = outcomeTitle;
            EnemyName = enemyName;
            RewardHeading = rewardHeading;
            Gold = gold;
            Materials = materials;
            LearnedActionName = learnedActionName;
            ConsequenceHeading = consequenceHeading;
            ConsequenceBody = consequenceBody;
            ContinueLabel = continueLabel;
            OutcomeStyleClass = outcomeStyleClass;
        }

        public BattleOutcome Outcome { get; }

        public string OutcomeTitle { get; }

        public string EnemyName { get; }

        public string RewardHeading { get; }

        public int Gold { get; }

        public IReadOnlyList<BattleResultMaterialLine> Materials { get; }

        public string LearnedActionName { get; }

        public bool HasLearnedAction => !string.IsNullOrWhiteSpace(LearnedActionName);

        public string ConsequenceHeading { get; }

        public string ConsequenceBody { get; }

        public string ContinueLabel { get; }

        public string OutcomeStyleClass { get; }

        public bool HasRewards => Gold > 0 || Materials.Count > 0 || HasLearnedAction;
    }

    public static class BattleResultPresentationFactory
    {
        private static readonly IReadOnlyDictionary<string, string> MaterialNames =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [BattleRewardSummary.WhiteCopperMaterialId] = "White Copper",
                    [BattleRewardSummary.BronzeMaterialId] = "Bronze",
                    [BattleRewardSummary.BrassMaterialId] = "Brass",
                    [BattleRewardSummary.ApeBeastPeltMaterialId] = "Ape Beast Pelt",
                    [BattleRewardSummary.ApeBeastArmguardMaterialId] = "Ape Beast Armguard",
                    [BattleRewardSummary.ApeBeastBlackFurMaterialId] = "Ape Beast Black Fur",
                    [BattleRewardSummary.ApeBeastBloodMaterialId] = "Ape Beast Blood",
                    [BattleRewardSummary.SlimeChunkMaterialId] = "Slime Chunk",
                    [BattleRewardSummary.ThornMaterialId] = "Thorn",
                    [BattleRewardSummary.CrystalMaterialId] = "Crystal",
                    [BattleRewardSummary.PureGoldMaterialId] = "Pure Gold",
                    [BattleRewardSummary.MithrilMaterialId] = "Mithril",
                    [BattleRewardSummary.YellowSilkMaterialId] = "Yellow Silk",
                    [BattleRewardSummary.RedSilkMaterialId] = "Red Silk"
                });

        public static BattleResultPresentation Create(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!session.IsComplete)
            {
                throw new InvalidOperationException(
                    "A Battle Result presentation requires a completed Battle Session.");
            }

            string enemyName = session.Encounter.DisplayName;
            if (session.Encounter.TryCreateDefinition(
                    out SoloComboEncounterDefinition definition,
                    out _))
            {
                enemyName = definition.EnemyDisplayName;
            }

            return Create(session.Outcome, enemyName, session.GrantedRewards);
        }

        public static BattleResultPresentation Create(
            BattleOutcome outcome,
            string enemyName,
            BattleRewardSummary rewards)
        {
            if (outcome == BattleOutcome.Ongoing)
            {
                throw new ArgumentException(
                    "A Battle Result presentation requires a final outcome.",
                    nameof(outcome));
            }

            if (string.IsNullOrWhiteSpace(enemyName))
            {
                throw new ArgumentException("An enemy name is required.", nameof(enemyName));
            }

            rewards = rewards ?? BattleRewardSummary.None;
            IReadOnlyList<BattleResultMaterialLine> materials = CreateMaterialLines(rewards);
            string learnedActionName = CreateLearnedActionName(rewards);

            switch (outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return new BattleResultPresentation(
                        outcome,
                        "VICTORY",
                        enemyName.Trim(),
                        "Battle Rewards",
                        rewards.Gold,
                        materials,
                        learnedActionName,
                        "Enemy Defeated",
                        "The defeated enemy will not reappear. Continue exploring from the battle location.",
                        "Continue Exploring",
                        "battle-result-victory");

                case BattleOutcome.PlayerDefeat:
                    return new BattleResultPresentation(
                        outcome,
                        "DEFEAT",
                        enemyName.Trim(),
                        "No Rewards",
                        0,
                        Array.Empty<BattleResultMaterialLine>(),
                        null,
                        "Return to Last Save",
                        "Progress made after the last save will be discarded.",
                        "Return to Last Save",
                        "battle-result-defeat");

                case BattleOutcome.Draw:
                    return new BattleResultPresentation(
                        outcome,
                        "MUTUAL DESTRUCTION",
                        enemyName.Trim(),
                        "Rewards Secured",
                        rewards.Gold,
                        materials,
                        learnedActionName,
                        "Enemy Defeated",
                        "Rewards are kept. Return to the last Save Point with 1 HP and 1 Stamina.",
                        "Return to Save Point",
                        "battle-result-mutual");

                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome));
            }
        }

        private static IReadOnlyList<BattleResultMaterialLine> CreateMaterialLines(
            BattleRewardSummary rewards)
        {
            var lines = new List<BattleResultMaterialLine>();
            foreach (KeyValuePair<string, int> material in rewards.Materials)
            {
                string displayName = MaterialNames.TryGetValue(material.Key, out string knownName)
                    ? knownName
                    : FormatUnknownMaterialName(material.Key);
                lines.Add(new BattleResultMaterialLine(
                    material.Key,
                    displayName,
                    material.Value));
            }

            lines.Sort((left, right) => string.Compare(
                left.DisplayName,
                right.DisplayName,
                StringComparison.Ordinal));
            return new ReadOnlyCollection<BattleResultMaterialLine>(lines);
        }

        private static string CreateLearnedActionName(BattleRewardSummary rewards)
        {
            if (!rewards.HasLearnedAction)
            {
                return null;
            }

            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            if (catalog != null
                && catalog.TryCreateDefinition(
                    rewards.LearnedActionId,
                    out ComboActionDefinition definition,
                    out _))
            {
                return definition.DisplayName;
            }

            return FormatUnknownActionName(rewards.LearnedActionId);
        }

        private static string FormatUnknownActionName(string actionId)
        {
            string[] words = actionId.Split(
                new[] { '-', '_', '.' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < words.Length; index++)
            {
                string word = words[index];
                words[index] = word.Length == 0
                    ? word
                    : char.ToUpperInvariant(word[0]) + word.Substring(1);
            }

            return words.Length == 0 ? actionId : string.Join(" ", words);
        }

        private static string FormatUnknownMaterialName(string materialId)
        {
            const string prefix = "material.";
            string value = materialId.StartsWith(prefix, StringComparison.Ordinal)
                ? materialId.Substring(prefix.Length)
                : materialId;
            string[] words = value.Split(new[] { '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < words.Length; index++)
            {
                string word = words[index];
                words[index] = word.Length == 0
                    ? word
                    : char.ToUpperInvariant(word[0]) + word.Substring(1);
            }

            return words.Length == 0 ? materialId : string.Join(" ", words);
        }
    }
}
