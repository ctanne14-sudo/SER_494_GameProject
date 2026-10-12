using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Damage;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.SoloCombo
{
    [CreateAssetMenu(
        fileName = "NewComboAction",
        menuName = "SER494/Solo Combo/Action")]
    public sealed class ComboActionAsset : ScriptableObject
    {
        [Serializable]
        private sealed class ChargeLevelAuthoringData
        {
            [Min(1)]
            public int additionalLoad = 3;

            [Min(0.01f)]
            public float damageMultiplierScale = 1f;
        }

        [Serializable]
        private sealed class ActiveWindowAuthoringData
        {
            [Tooltip("Stable ID used to prevent the same named effect from stacking.")]
            public string windowId = string.Empty;

            public string displayName = string.Empty;

            [Min(1)]
            public int startOffset = 1;

            [Min(1)]
            public int endOffset = 1;

            public ActiveWindowEffect effect = ActiveWindowEffect.DefensiveState;

            [Range(0.01f, 0.99f)]
            public float damageReduction = 0.30f;

            public ActiveWindowDisruptionPolicy disruptionPolicy =
                ActiveWindowDisruptionPolicy.PersistThroughDisruption;
        }

        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable internal ID. It must be unique across all owned Actions.")]
        private string actionId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [Header("Combo Role")]
        [SerializeField]
        private ComboActionCategory category = ComboActionCategory.Opening;

        [Header("Costs")]
        [SerializeField]
        [Min(1)]
        private int loadCost = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("Fixed Resolution Point in Action Local Time. Zero resolves when the Action starts.")]
        private int resolutionOffset = 1;

        [SerializeField]
        [Tooltip("When enabled, the player selects a Resolution Point from a local-time window during Timeline Resolution.")]
        private bool useResolutionWindow = false;

        [SerializeField]
        [Min(0)]
        private int resolutionWindowStart = 1;

        [SerializeField]
        [Min(0)]
        private int resolutionWindowEnd = 1;

        [SerializeField]
        [Tooltip("Optional Charge Levels. Level 0 always uses the base Action values.")]
        private ChargeLevelAuthoringData[] chargeLevels =
            Array.Empty<ChargeLevelAuthoringData>();

        [SerializeField]
        [Tooltip("Continuous effects active over inclusive Action Local Time ranges.")]
        private ActiveWindowAuthoringData[] activeWindows =
            Array.Empty<ActiveWindowAuthoringData>();

        [SerializeField]
        [Min(0)]
        private int manaCost;

        [SerializeField]
        [Min(0)]
        private int staminaCost;

        [SerializeField]
        [Min(0)]
        private int cooldown;

        [Header("Prototype Effect")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Ordinary damage multiplier. For example, 0.50 means 50% of relevant Weapon Attack before the remaining damage buckets.")]
        private float damageMultiplier;

        [SerializeField]
        private DamageType damageType = DamageType.Slashing;

        [SerializeField]
        [Tooltip("Prototype effect resolved at the fixed Resolution Point. Damage requires a positive Damage Multiplier.")]
        private ActionResolutionEffect resolutionEffect = ActionResolutionEffect.None;

        [SerializeField]
        [Tooltip("Tags that alter how a Damage Resolution Point interacts with Guard and Evade.")]
        private DamageResolutionTag damageTags = DamageResolutionTag.None;

        [SerializeField]
        [Tooltip("Apply an Action Disruption when this Damage effect successfully hits.")]
        private bool appliesOnHitDisruption = false;

        [SerializeField]
        private ActionDisruptionType onHitDisruptionType = ActionDisruptionType.Stagger;

        [SerializeField]
        [Min(1)]
        private int onHitDisruptionDuration = 1;

        public string ActionId => actionId;

        public string DisplayName => displayName;

        public ComboActionCategory Category => category;

        public int LoadCost => loadCost;

        public int ManaCost => manaCost;

        public int ResolutionOffset => resolutionOffset;

        public bool UsesResolutionWindow => useResolutionWindow;

        public int ResolutionWindowStart => resolutionWindowStart;

        public int ResolutionWindowEnd => resolutionWindowEnd;

        public int ChargeLevelCount => chargeLevels?.Length ?? 0;

        public int ActiveWindowCount => activeWindows?.Length ?? 0;

        public string GetActiveWindowDisplayName(int index)
        {
            return GetActiveWindow(index).displayName;
        }

        public int GetActiveWindowStartOffset(int index)
        {
            return GetActiveWindow(index).startOffset;
        }

        public int GetActiveWindowEndOffset(int index)
        {
            return GetActiveWindow(index).endOffset;
        }

        public float GetActiveWindowDamageReduction(int index)
        {
            return GetActiveWindow(index).damageReduction;
        }

        public int GetChargeAdditionalLoad(int chargeLevel)
        {
            return GetChargeLevel(chargeLevel).additionalLoad;
        }

        public float GetChargeDamageMultiplierScale(int chargeLevel)
        {
            return GetChargeLevel(chargeLevel).damageMultiplierScale;
        }

        public int StaminaCost => staminaCost;

        public int Cooldown => cooldown;

        public float DamageMultiplier => damageMultiplier;

        public DamageType DamageType => damageType;

        public ActionResolutionEffect ResolutionEffect => resolutionEffect;

        public DamageResolutionTag DamageTags => damageTags;

        public bool AppliesOnHitDisruption => appliesOnHitDisruption;

        public ActionDisruptionType OnHitDisruptionType => onHitDisruptionType;

        public int OnHitDisruptionDuration => onHitDisruptionDuration;

        public bool TryCreateDefinition(
            out ComboActionDefinition definition,
            out string validationError)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(actionId))
            {
                validationError = $"{name} has an empty Action ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                validationError = $"{name} has an empty Display Name.";
                return false;
            }

            if (loadCost <= 0)
            {
                validationError = $"{name} must have a Load Cost greater than zero.";
                return false;
            }

            if (resolutionOffset < 0 || resolutionOffset > loadCost)
            {
                validationError = $"{name} has a Resolution Offset outside its Action Load.";
                return false;
            }

            if (useResolutionWindow
                && (resolutionWindowStart < 0
                    || resolutionWindowEnd > loadCost
                    || resolutionWindowStart >= resolutionWindowEnd))
            {
                validationError = $"{name} must have an ordered Resolution Window with "
                    + "at least two Time Steps inside its Action Load.";
                return false;
            }

            if (!TryCreateChargeLevels(
                    out IReadOnlyList<ComboActionChargeLevelDefinition> chargeDefinitions,
                    out validationError))
            {
                return false;
            }

            if (useResolutionWindow && chargeDefinitions.Count > 0)
            {
                validationError = $"{name} cannot use both a Resolution Window and Charge Levels in the MVP.";
                return false;
            }

            if (!TryCreateActiveWindows(
                    out IReadOnlyList<ActionActiveWindowDefinition> activeWindowDefinitions,
                    out validationError))
            {
                return false;
            }

            if (chargeDefinitions.Count > 0 && activeWindowDefinitions.Count > 0)
            {
                validationError = $"{name} cannot use both Charge Levels and Active Windows "
                    + "until charge timing for Active Windows is defined.";
                return false;
            }

            if (manaCost < 0 || staminaCost < 0 || cooldown < 0 || damageMultiplier < 0f)
            {
                validationError = $"{name} contains a negative cost or Damage Multiplier value.";
                return false;
            }

            if (damageMultiplier > 0f
                && resolutionEffect != ActionResolutionEffect.Damage)
            {
                validationError = $"{name} has damage but its Resolution Effect is not Damage.";
                return false;
            }

            if (damageMultiplier <= 0f
                && resolutionEffect == ActionResolutionEffect.Damage)
            {
                validationError = $"{name} uses a Damage Resolution Effect without damage.";
                return false;
            }

            if (damageMultiplier <= 0f && damageTags != DamageResolutionTag.None)
            {
                validationError = $"{name} uses Damage Tags without damage.";
                return false;
            }

            if (appliesOnHitDisruption && damageMultiplier <= 0f)
            {
                validationError = $"{name} applies an On Hit Disruption without damage.";
                return false;
            }

            if (appliesOnHitDisruption
                && (!Enum.IsDefined(typeof(ActionDisruptionType), onHitDisruptionType)
                    || onHitDisruptionDuration <= 0))
            {
                validationError = $"{name} contains an invalid On Hit Disruption.";
                return false;
            }

            if (resolutionEffect == ActionResolutionEffect.ApplyBuff
                || resolutionEffect == ActionResolutionEffect.ApplyDebuff)
            {
                validationError = $"{name} requires Status authoring, which is not yet "
                    + "available on player Combo Action assets.";
                return false;
            }

            definition = new ComboActionDefinition(
                actionId.Trim(),
                displayName.Trim(),
                category,
                loadCost,
                manaCost,
                staminaCost,
                cooldown,
                damageMultiplier,
                resolutionOffset,
                damageType,
                resolutionEffect,
                damageTags,
                resolutionWindowStart: useResolutionWindow
                    ? (int?)resolutionWindowStart
                    : null,
                resolutionWindowEnd: useResolutionWindow
                    ? (int?)resolutionWindowEnd
                    : null,
                chargeLevels: chargeDefinitions,
                activeWindows: activeWindowDefinitions,
                onHitDisruption: appliesOnHitDisruption
                    ? new ActionDisruptionDefinition(
                        onHitDisruptionType,
                        onHitDisruptionDuration)
                    : null);

            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            loadCost = Mathf.Max(1, loadCost);
            resolutionOffset = Mathf.Clamp(resolutionOffset, 0, loadCost);
            resolutionWindowStart = Mathf.Clamp(resolutionWindowStart, 0, loadCost);
            resolutionWindowEnd = Mathf.Clamp(resolutionWindowEnd, 0, loadCost);
            manaCost = Mathf.Max(0, manaCost);
            staminaCost = Mathf.Max(0, staminaCost);
            cooldown = Mathf.Max(0, cooldown);
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
            onHitDisruptionDuration = Mathf.Max(1, onHitDisruptionDuration);
            if (chargeLevels == null)
            {
                chargeLevels = Array.Empty<ChargeLevelAuthoringData>();
            }

            for (int index = 0; index < chargeLevels.Length; index++)
            {
                ChargeLevelAuthoringData level = chargeLevels[index];
                if (level == null)
                {
                    continue;
                }

                level.additionalLoad = Mathf.Max(1, level.additionalLoad);
                level.damageMultiplierScale = Mathf.Max(0.01f, level.damageMultiplierScale);
            }

            if (activeWindows == null)
            {
                activeWindows = Array.Empty<ActiveWindowAuthoringData>();
            }

            for (int index = 0; index < activeWindows.Length; index++)
            {
                ActiveWindowAuthoringData window = activeWindows[index];
                if (window == null)
                {
                    continue;
                }

                window.startOffset = Mathf.Clamp(window.startOffset, 1, loadCost);
                window.endOffset = Mathf.Clamp(window.endOffset, window.startOffset, loadCost);
                window.damageReduction = Mathf.Clamp(window.damageReduction, 0.01f, 0.99f);
            }
        }

        private bool TryCreateChargeLevels(
            out IReadOnlyList<ComboActionChargeLevelDefinition> definitions,
            out string validationError)
        {
            if (chargeLevels == null || chargeLevels.Length == 0)
            {
                definitions = Array.Empty<ComboActionChargeLevelDefinition>();
                validationError = null;
                return true;
            }

            var created = new ComboActionChargeLevelDefinition[chargeLevels.Length];
            int previousAdditionalLoad = 0;
            for (int index = 0; index < chargeLevels.Length; index++)
            {
                ChargeLevelAuthoringData level = chargeLevels[index];
                if (level == null
                    || level.additionalLoad <= previousAdditionalLoad
                    || level.damageMultiplierScale <= 0f)
                {
                    definitions = null;
                    validationError = $"{name} Charge Levels must be non-null, use a positive "
                        + "damage scale, and add progressively more Load.";
                    return false;
                }

                created[index] = new ComboActionChargeLevelDefinition(
                    level.additionalLoad,
                    level.damageMultiplierScale);
                previousAdditionalLoad = level.additionalLoad;
            }

            definitions = Array.AsReadOnly(created);
            validationError = null;
            return true;
        }

        private ChargeLevelAuthoringData GetChargeLevel(int chargeLevel)
        {
            if (chargeLevels == null
                || chargeLevel <= 0
                || chargeLevel > chargeLevels.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(chargeLevel));
            }

            return chargeLevels[chargeLevel - 1];
        }

        private bool TryCreateActiveWindows(
            out IReadOnlyList<ActionActiveWindowDefinition> definitions,
            out string validationError)
        {
            if (activeWindows == null || activeWindows.Length == 0)
            {
                definitions = Array.Empty<ActionActiveWindowDefinition>();
                validationError = null;
                return true;
            }

            var created = new ActionActiveWindowDefinition[activeWindows.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < activeWindows.Length; index++)
            {
                ActiveWindowAuthoringData window = activeWindows[index];
                if (window == null
                    || string.IsNullOrWhiteSpace(window.windowId)
                    || string.IsNullOrWhiteSpace(window.displayName)
                    || window.startOffset < 1
                    || window.endOffset < window.startOffset
                    || window.endOffset > loadCost
                    || window.damageReduction <= 0f
                    || window.damageReduction >= 1f)
                {
                    definitions = null;
                    validationError = $"{name} contains an invalid Active Window.";
                    return false;
                }

                string trimmedId = window.windowId.Trim();
                if (!ids.Add(trimmedId))
                {
                    definitions = null;
                    validationError = $"{name} contains duplicate Active Window IDs.";
                    return false;
                }

                created[index] = new ActionActiveWindowDefinition(
                    trimmedId,
                    window.displayName.Trim(),
                    window.startOffset,
                    window.endOffset,
                    window.effect,
                    window.damageReduction,
                    window.disruptionPolicy);
            }

            definitions = Array.AsReadOnly(created);
            validationError = null;
            return true;
        }

        private ActiveWindowAuthoringData GetActiveWindow(int index)
        {
            if (activeWindows == null || index < 0 || index >= activeWindows.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return activeWindows[index];
        }
    }
}
