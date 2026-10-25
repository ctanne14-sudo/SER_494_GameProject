using System;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Damage;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    public readonly struct PlayerEquipmentDerivedStats
    {
        public PlayerEquipmentDerivedStats(
            int maximumHealthBonus,
            double allDamageDealtModifier)
        {
            MaximumHealthBonus = maximumHealthBonus;
            AllDamageDealtModifier = allDamageDealtModifier;
        }

        public int MaximumHealthBonus { get; }

        public double AllDamageDealtModifier { get; }

        public static bool TryResolve(
            PersistentPlayerState playerState,
            PlayerEquipmentCatalogAsset catalog,
            out PlayerEquipmentDerivedStats stats,
            out string validationError)
        {
            stats = default;
            if (playerState == null)
            {
                validationError = "Persistent Player State is unavailable.";
                return false;
            }

            if (catalog == null)
            {
                validationError = "Player Equipment Catalog is unavailable.";
                return false;
            }

            int maximumHealthBonus = 0;
            double allDamageDealtModifier = 0d;
            foreach (string accessoryId in playerState.Equipment.EquippedAccessoryBySlot.Values)
            {
                if (!catalog.TryGetAccessory(
                        accessoryId,
                        out AccessoryDefinitionAsset accessory,
                        out validationError))
                {
                    return false;
                }

                switch (accessory.EffectType)
                {
                    case AccessoryEffectType.MaximumHealthFlat:
                        maximumHealthBonus = checked(
                            maximumHealthBonus + (int)Math.Round(
                                accessory.EffectAmount,
                                MidpointRounding.AwayFromZero));
                        break;
                    case AccessoryEffectType.AllDamageDealtMultiplier:
                        allDamageDealtModifier += accessory.EffectAmount;
                        break;
                    default:
                        validationError =
                            $"Accessory '{accessory.AccessoryId}' has an unsupported effect.";
                        return false;
                }
            }

            stats = new PlayerEquipmentDerivedStats(
                maximumHealthBonus,
                allDamageDealtModifier);
            validationError = null;
            return true;
        }

        public void ApplyPersistentMaximumHealth(PersistentPlayerState playerState)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            playerState.SetEquipmentMaximumHealthBonus(MaximumHealthBonus);
        }

        public void ApplyCombatModifiers(CombatantState combatant)
        {
            if (combatant == null)
            {
                throw new ArgumentNullException(nameof(combatant));
            }

            if (AllDamageDealtModifier != 0d)
            {
                combatant.AddDamageDealtModifier(
                    DamageModifier.All(AllDamageDealtModifier));
            }
        }
    }
}
