using System;
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

        public bool IsComplete => Outcome != BattleOutcome.Ongoing;

        internal void Complete(BattleOutcome outcome)
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
}
