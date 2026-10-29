using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.SoloCombo
{
    [Serializable]
    public sealed class EncounterItemAuthoringData
    {
        [SerializeField]
        private string itemId = string.Empty;

        [SerializeField]
        [Min(1)]
        private int quantity = 1;

        public EncounterItemAuthoringData(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }

        public string ItemId => itemId;

        public int Quantity => quantity;
    }

    [Serializable]
    public sealed class EncounterRewardDropAuthoringData
    {
        [SerializeField]
        private string itemId = string.Empty;

        [SerializeField]
        [Min(1)]
        private int weight = 1;

        public EncounterRewardDropAuthoringData(string itemId, int weight)
        {
            this.itemId = itemId;
            this.weight = weight;
        }

        public string ItemId => itemId;

        public int Weight => weight;
    }

    [Serializable]
    public sealed class EnemyActionAuthoringData
    {
        [SerializeField]
        private string actionId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField]
        [Min(1)]
        private int timelineCost = 1;

        [SerializeField]
        private ActionResolutionEffect resolutionEffect = ActionResolutionEffect.Damage;

        [SerializeField]
        [Min(0f)]
        private float damageMultiplier;

        [SerializeField]
        private DamageType damageType = DamageType.Slashing;

        [SerializeField]
        private DamageResolutionTag damageTags = DamageResolutionTag.None;

        [SerializeField]
        private bool appliesOnHitDisruption;

        [SerializeField]
        private ActionDisruptionType onHitDisruptionType = ActionDisruptionType.Stagger;

        [SerializeField]
        [Min(1)]
        private int onHitDisruptionDuration = 1;

        [SerializeField]
        private bool appliesOnEvadedDisruption;

        [SerializeField]
        private ActionDisruptionType onEvadedDisruptionType = ActionDisruptionType.Stagger;

        [SerializeField]
        [Min(1)]
        private int onEvadedDisruptionDuration = 1;

        [SerializeField]
        private ActionDisruptionResponse flinchResponse = ActionDisruptionResponse.Continue;

        [SerializeField]
        private ActionDisruptionResponse staggerResponse = ActionDisruptionResponse.PauseAndResume;

        [SerializeField]
        private ActionDisruptionResponse knockdownResponse = ActionDisruptionResponse.PauseAndResume;

        public EnemyActionAuthoringData(
            string actionId,
            string displayName,
            int timelineCost,
            ActionResolutionEffect resolutionEffect,
            float damageMultiplier = 0f,
            DamageType damageType = DamageType.Slashing,
            DamageResolutionTag damageTags = DamageResolutionTag.None,
            ActionDisruptionDefinition onHitDisruption = null,
            ActionDisruptionDefinition onEvadedDisruption = null)
        {
            this.actionId = actionId;
            this.displayName = displayName;
            this.timelineCost = timelineCost;
            this.resolutionEffect = resolutionEffect;
            this.damageMultiplier = damageMultiplier;
            this.damageType = damageType;
            this.damageTags = damageTags;
            appliesOnHitDisruption = onHitDisruption != null;
            onHitDisruptionType = onHitDisruption?.Type ?? ActionDisruptionType.Stagger;
            onHitDisruptionDuration = onHitDisruption?.Duration ?? 1;
            appliesOnEvadedDisruption = onEvadedDisruption != null;
            onEvadedDisruptionType = onEvadedDisruption?.Type ?? ActionDisruptionType.Stagger;
            onEvadedDisruptionDuration = onEvadedDisruption?.Duration ?? 1;
        }

        public bool TryCreateDefinition(
            out EnemyActionDefinition definition,
            out string validationError)
        {
            definition = null;
            try
            {
                definition = new EnemyActionDefinition(
                    actionId,
                    displayName,
                    timelineCost,
                    resolutionOffset: timelineCost,
                    damageMultiplier: damageMultiplier,
                    damageType: damageType,
                    resolutionEffect: resolutionEffect,
                    damageTags: damageTags,
                    onHitDisruption: appliesOnHitDisruption
                        ? new ActionDisruptionDefinition(
                            onHitDisruptionType,
                            onHitDisruptionDuration)
                        : null,
                    disruptionResponseSegments: new[]
                    {
                        new DisruptionResponseSegmentDefinition(
                            1,
                            timelineCost,
                            flinchResponse,
                            staggerResponse,
                            knockdownResponse)
                    },
                    onEvadedDisruption: appliesOnEvadedDisruption
                        ? new ActionDisruptionDefinition(
                            onEvadedDisruptionType,
                            onEvadedDisruptionDuration)
                        : null);
                validationError = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is InvalidOperationException)
            {
                validationError = exception.Message;
                return false;
            }
        }
    }

    [Serializable]
    public sealed class EnemySequenceAuthoringData
    {
        [SerializeField]
        private string sequenceId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField]
        private EnemyActionAuthoringData[] actions = Array.Empty<EnemyActionAuthoringData>();

        public EnemySequenceAuthoringData(
            string sequenceId,
            string displayName,
            params EnemyActionAuthoringData[] actions)
        {
            this.sequenceId = sequenceId;
            this.displayName = displayName;
            this.actions = actions ?? Array.Empty<EnemyActionAuthoringData>();
        }

        public bool TryCreateDefinition(
            out EnemyActionSequence definition,
            out string validationError)
        {
            definition = null;
            if (actions == null || actions.Length == 0)
            {
                validationError = $"Sequence '{displayName}' has no Actions.";
                return false;
            }

            var runtimeActions = new EnemyActionDefinition[actions.Length];
            for (int index = 0; index < actions.Length; index++)
            {
                if (actions[index] == null)
                {
                    validationError = $"Sequence '{displayName}', Action {index} is null.";
                    return false;
                }

                if (!actions[index].TryCreateDefinition(
                        out runtimeActions[index],
                        out string actionError))
                {
                    validationError = $"Sequence '{displayName}', Action {index}: {actionError}";
                    return false;
                }
            }

            try
            {
                definition = new EnemyActionSequence(sequenceId, displayName, runtimeActions);
                validationError = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                validationError = exception.Message;
                return false;
            }
        }
    }

    [CreateAssetMenu(
        fileName = "NewSoloComboEncounter",
        menuName = "SER494/Solo Combo/Encounter")]
    public sealed class SoloComboEncounterDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string encounterId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField]
        [TextArea(2, 4)]
        private string description = string.Empty;

        [Header("Enemy")]
        [SerializeField]
        private string enemyDisplayName = "Training Enemy";

        [SerializeField]
        [Min(1)]
        private int enemyMaximumHealth = 100;

        [SerializeField]
        [Min(0)]
        private int enemyAttack = 50;

        [SerializeField]
        [Min(0)]
        private int enemyDefense = 50;

        [Header("Round Rules")]
        [SerializeField]
        [Min(0)]
        private int manaRecoveryPerTurn = 1;

        [SerializeField]
        [Min(0)]
        private int staminaRecoveryPerTurn = 30;

        [SerializeField]
        private EncounterItemAuthoringData[] startingItems =
            Array.Empty<EncounterItemAuthoringData>();

        [Header("Rewards")]
        [SerializeField]
        [Min(0)]
        private int rewardGold;

        [SerializeField]
        [Min(0)]
        private int rewardMaterialRollCount;

        [SerializeField]
        private EncounterRewardDropAuthoringData[] rewardMaterialDrops =
            Array.Empty<EncounterRewardDropAuthoringData>();

        [Header("Scripted Enemy Rounds")]
        [SerializeField]
        private EnemySequenceAuthoringData[] scriptedSequences =
            Array.Empty<EnemySequenceAuthoringData>();

        public string EncounterId => encounterId;

        public string DisplayName => displayName;

        public string Description => description;

        public void Configure(
            string newEncounterId,
            string newDisplayName,
            string newDescription,
            string newEnemyDisplayName,
            int newEnemyMaximumHealth,
            int newEnemyAttack,
            int newEnemyDefense,
            int newManaRecoveryPerTurn,
            int newStaminaRecoveryPerTurn,
            EncounterItemAuthoringData[] newStartingItems,
            EnemySequenceAuthoringData[] newScriptedSequences,
            int newRewardGold = 0,
            int newRewardMaterialRollCount = 0,
            EncounterRewardDropAuthoringData[] newRewardMaterialDrops = null)
        {
            encounterId = newEncounterId;
            displayName = newDisplayName;
            description = newDescription;
            enemyDisplayName = newEnemyDisplayName;
            enemyMaximumHealth = newEnemyMaximumHealth;
            enemyAttack = newEnemyAttack;
            enemyDefense = newEnemyDefense;
            manaRecoveryPerTurn = newManaRecoveryPerTurn;
            staminaRecoveryPerTurn = newStaminaRecoveryPerTurn;
            startingItems = newStartingItems ?? Array.Empty<EncounterItemAuthoringData>();
            scriptedSequences = newScriptedSequences ?? Array.Empty<EnemySequenceAuthoringData>();
            rewardGold = newRewardGold;
            rewardMaterialRollCount = newRewardMaterialRollCount;
            rewardMaterialDrops = newRewardMaterialDrops
                ?? Array.Empty<EncounterRewardDropAuthoringData>();
        }

        public bool TryCreateDefinition(
            out SoloComboEncounterDefinition definition,
            out string validationError)
        {
            definition = null;
            if (scriptedSequences == null || scriptedSequences.Length == 0)
            {
                validationError = $"{name} has no scripted enemy Sequences.";
                return false;
            }

            var sequences = new EnemyActionSequence[scriptedSequences.Length];
            for (int index = 0; index < scriptedSequences.Length; index++)
            {
                if (scriptedSequences[index] == null)
                {
                    validationError = $"{name}, Sequence {index} is null.";
                    return false;
                }

                if (!scriptedSequences[index].TryCreateDefinition(
                        out sequences[index],
                        out string sequenceError))
                {
                    validationError = $"{name}, Sequence {index}: {sequenceError}";
                    return false;
                }
            }

            if (!TryCreateStartingItems(out Dictionary<string, int> items, out validationError))
            {
                return false;
            }

            if (!TryCreateRewards(out EncounterRewardDefinition rewards, out validationError))
            {
                return false;
            }

            try
            {
                definition = new SoloComboEncounterDefinition(
                    encounterId,
                    displayName,
                    description,
                    enemyDisplayName,
                    new CombatantStats(
                        maxHealth: enemyMaximumHealth,
                        maxMana: 0,
                        maxStamina: 0,
                        attack: enemyAttack,
                        defense: enemyDefense),
                    EnemyBehaviorTable.Scripted(sequences),
                    manaRecoveryPerTurn,
                    staminaRecoveryPerTurn,
                    items,
                    rewards);
                validationError = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is InvalidOperationException)
            {
                validationError = exception.Message;
                return false;
            }
        }

        private bool TryCreateRewards(
            out EncounterRewardDefinition rewards,
            out string validationError)
        {
            rewards = null;
            EncounterRewardDropAuthoringData[] authoredDrops = rewardMaterialDrops
                ?? Array.Empty<EncounterRewardDropAuthoringData>();
            var drops = new WeightedMaterialReward[authoredDrops.Length];
            try
            {
                for (int index = 0; index < authoredDrops.Length; index++)
                {
                    EncounterRewardDropAuthoringData drop = authoredDrops[index];
                    if (drop == null)
                    {
                        validationError = $"{name} contains a null reward at index {index}.";
                        return false;
                    }

                    drops[index] = new WeightedMaterialReward(drop.ItemId, drop.Weight);
                }

                rewards = new EncounterRewardDefinition(
                    rewardGold,
                    rewardMaterialRollCount,
                    drops);
                validationError = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is InvalidOperationException)
            {
                validationError = exception.Message;
                return false;
            }
        }

        private bool TryCreateStartingItems(
            out Dictionary<string, int> items,
            out string validationError)
        {
            items = new Dictionary<string, int>(StringComparer.Ordinal);
            if (startingItems == null)
            {
                validationError = null;
                return true;
            }

            for (int index = 0; index < startingItems.Length; index++)
            {
                EncounterItemAuthoringData item = startingItems[index];
                if (item == null
                    || string.IsNullOrWhiteSpace(item.ItemId)
                    || item.Quantity <= 0
                    || items.ContainsKey(item.ItemId.Trim()))
                {
                    validationError = $"{name} contains an invalid or duplicate starting Item at index {index}.";
                    return false;
                }

                items.Add(item.ItemId.Trim(), item.Quantity);
            }

            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            enemyMaximumHealth = Mathf.Max(1, enemyMaximumHealth);
            enemyAttack = Mathf.Max(0, enemyAttack);
            enemyDefense = Mathf.Max(0, enemyDefense);
            manaRecoveryPerTurn = Mathf.Max(0, manaRecoveryPerTurn);
            staminaRecoveryPerTurn = Mathf.Max(0, staminaRecoveryPerTurn);
            rewardGold = Mathf.Max(0, rewardGold);
            rewardMaterialRollCount = Mathf.Max(0, rewardMaterialRollCount);
        }
    }
}
