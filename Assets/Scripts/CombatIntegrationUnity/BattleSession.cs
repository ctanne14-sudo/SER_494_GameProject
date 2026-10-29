using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using UnityEngine;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    public sealed class BattleSession
    {
        internal BattleSession(
            SoloComboEncounterDefinitionAsset encounter,
            SoloComboLoadoutSession loadoutSession,
            string encounterInstanceId,
            string sourceSceneName,
            string battleSceneName,
            Vector3 returnPosition,
            Quaternion returnRotation)
        {
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            LoadoutSession = loadoutSession
                ?? throw new ArgumentNullException(nameof(loadoutSession));
            EncounterInstanceId = RequireText(encounterInstanceId, nameof(encounterInstanceId));
            SourceSceneName = RequireText(sourceSceneName, nameof(sourceSceneName));
            BattleSceneName = RequireText(battleSceneName, nameof(battleSceneName));
            ReturnPosition = returnPosition;
            ReturnRotation = returnRotation;
            Outcome = BattleOutcome.Ongoing;
        }

        public SoloComboEncounterDefinitionAsset Encounter { get; }

        public SoloComboLoadoutSession LoadoutSession { get; }

        public string EncounterInstanceId { get; }

        public string SourceSceneName { get; }

        public string BattleSceneName { get; }

        public Vector3 ReturnPosition { get; }

        public Quaternion ReturnRotation { get; }

        public BattleOutcome Outcome { get; private set; }

        public BattleRewardSummary GrantedRewards { get; private set; } =
            BattleRewardSummary.None;

        public bool IsComplete => Outcome != BattleOutcome.Ongoing;

        internal void Complete(
            BattleOutcome outcome,
            BattleRewardSummary grantedRewards = null)
        {
            if (outcome == BattleOutcome.Ongoing)
            {
                throw new ArgumentException(
                    "A completed Battle Session requires a final outcome.",
                    nameof(outcome));
            }

            if (IsComplete)
            {
                throw new InvalidOperationException("The active Battle Session is already complete.");
            }

            Outcome = outcome;
            GrantedRewards = grantedRewards ?? BattleRewardSummary.None;
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A value is required.", parameterName);
            }

            return value.Trim();
        }
    }

    public sealed class BattleRewardSummary
    {
        public const string WhiteCopperMaterialId = "material.white-copper";
        public const string BronzeMaterialId = "material.bronze";
        public const string BrassMaterialId = "material.brass";

        public static readonly BattleRewardSummary None =
            new BattleRewardSummary(0, null);

        private readonly IReadOnlyDictionary<string, int> _materials;

        public BattleRewardSummary(
            int gold,
            IReadOnlyDictionary<string, int> materials)
        {
            if (gold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gold));
            }

            var copy = new Dictionary<string, int>(StringComparer.Ordinal);
            if (materials != null)
            {
                foreach (KeyValuePair<string, int> material in materials)
                {
                    if (string.IsNullOrWhiteSpace(material.Key) || material.Value <= 0)
                    {
                        throw new ArgumentException(
                            "Reward materials require a stable ID and positive quantity.",
                            nameof(materials));
                    }

                    copy.Add(material.Key.Trim(), material.Value);
                }
            }

            Gold = gold;
            _materials = new ReadOnlyDictionary<string, int>(copy);
        }

        public int Gold { get; }

        public IReadOnlyDictionary<string, int> Materials => _materials;

        public int TotalMaterialCount
        {
            get
            {
                int total = 0;
                foreach (int count in _materials.Values)
                {
                    total += count;
                }

                return total;
            }
        }
    }
}
