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
            double allDamageDealtModifier,
            double healingPotionMultiplierBonus = 0d,
            double guardCounterPiercingMultiplier = 0d,
            double clashDamageDealtMultiplierBonus = 0d,
            double victoryGoldMultiplierBonus = 0d,
            int extraMaterialRewardRolls = 0,
            double slashingDamageTakenModifier = 0d,
            double piercingDamageTakenModifier = 0d,
            double bluntDamageTakenModifier = 0d)
        {
            MaximumHealthBonus = maximumHealthBonus;
            AllDamageDealtModifier = allDamageDealtModifier;
            HealingPotionMultiplierBonus = healingPotionMultiplierBonus;
            GuardCounterPiercingMultiplier = guardCounterPiercingMultiplier;
            ClashDamageDealtMultiplierBonus = clashDamageDealtMultiplierBonus;
            VictoryGoldMultiplierBonus = victoryGoldMultiplierBonus;
            ExtraMaterialRewardRolls = extraMaterialRewardRolls;
            SlashingDamageTakenModifier = slashingDamageTakenModifier;
            PiercingDamageTakenModifier = piercingDamageTakenModifier;
            BluntDamageTakenModifier = bluntDamageTakenModifier;
        }

        public int MaximumHealthBonus { get; }

        public double AllDamageDealtModifier { get; }

        public double HealingPotionMultiplierBonus { get; }

        public double GuardCounterPiercingMultiplier { get; }

        public double ClashDamageDealtMultiplierBonus { get; }

        public double VictoryGoldMultiplierBonus { get; }

        public int ExtraMaterialRewardRolls { get; }

        public double SlashingDamageTakenModifier { get; }

        public double PiercingDamageTakenModifier { get; }

        public double BluntDamageTakenModifier { get; }

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
            double maximumHealthMultiplier = 0d;
            double allDamageDealtModifier = 0d;
            double healingPotionMultiplierBonus = 0d;
            double guardCounterPiercingMultiplier = 0d;
            double slashingDamageTakenModifier = 0d;
            double piercingDamageTakenModifier = 0d;
            double bluntDamageTakenModifier = 0d;
            var equippedSeriesCounts = new System.Collections.Generic.Dictionary<string, int>(
                StringComparer.Ordinal);

            foreach (string armorId in playerState.Equipment.EquippedArmorBySlot.Values)
            {
                if (!catalog.TryGetArmor(armorId, out ArmorDefinitionAsset armor, out validationError))
                {
                    return false;
                }

                switch (armor.EffectType)
                {
                    case ArmorEffectType.None:
                        break;
                    case ArmorEffectType.DamageTakenMultiplier:
                        switch (armor.EffectDamageType)
                        {
                            case DamageType.Slashing:
                                slashingDamageTakenModifier -= armor.EffectAmount;
                                break;
                            case DamageType.Piercing:
                                piercingDamageTakenModifier -= armor.EffectAmount;
                                break;
                            case DamageType.Blunt:
                                bluntDamageTakenModifier -= armor.EffectAmount;
                                break;
                            default:
                                validationError = $"Armor '{armor.ArmorId}' has an unsupported damage-type effect.";
                                return false;
                        }
                        break;
                    case ArmorEffectType.MaximumHealthMultiplier:
                        maximumHealthMultiplier += armor.EffectAmount;
                        break;
                    case ArmorEffectType.HealingPotionMultiplier:
                        healingPotionMultiplierBonus += armor.EffectAmount;
                        break;
                    case ArmorEffectType.GuardCounterPiercingMultiplier:
                        guardCounterPiercingMultiplier += armor.EffectAmount;
                        break;
                    case ArmorEffectType.AllDamageDealtMultiplier:
                        allDamageDealtModifier += armor.EffectAmount;
                        break;
                    default:
                        validationError = $"Armor '{armor.ArmorId}' has an unsupported effect.";
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(armor.SeriesId))
                {
                    equippedSeriesCounts.TryGetValue(armor.SeriesId, out int count);
                    equippedSeriesCounts[armor.SeriesId] = count + 1;
                }
            }
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
                    case AccessoryEffectType.Custom:
                        // Dedicated accessory rules are applied by their owning combat,
                        // reward, or inventory system. They do not alter derived stats here.
                        break;
                    default:
                        validationError =
                            $"Accessory '{accessory.AccessoryId}' has an unsupported effect.";
                        return false;
                }
            }

            maximumHealthBonus = checked(maximumHealthBonus + (int)Math.Round(
                (playerState.BaseMaximumHealth + maximumHealthBonus)
                    * maximumHealthMultiplier,
                MidpointRounding.AwayFromZero));

            double clashDamageDealtMultiplierBonus = 0d;
            double victoryGoldMultiplierBonus = 0d;
            int extraMaterialRewardRolls = 0;
            foreach (System.Collections.Generic.KeyValuePair<string, int> series in equippedSeriesCounts)
            {
                if (series.Value < 3)
                {
                    continue;
                }

                switch (series.Key)
                {
                    case "armor-series.yellow-silk":
                        victoryGoldMultiplierBonus += 0.20d;
                        break;
                    case "armor-series.red-silk":
                        extraMaterialRewardRolls += 1;
                        break;
                    case "armor-series.crystal":
                        allDamageDealtModifier += 0.10d;
                        break;
                    case "armor-series.pure-gold":
                        clashDamageDealtMultiplierBonus += 0.50d;
                        break;
                }
            }

            stats = new PlayerEquipmentDerivedStats(
                maximumHealthBonus,
                allDamageDealtModifier,
                healingPotionMultiplierBonus,
                guardCounterPiercingMultiplier,
                clashDamageDealtMultiplierBonus,
                victoryGoldMultiplierBonus,
                extraMaterialRewardRolls,
                slashingDamageTakenModifier,
                piercingDamageTakenModifier,
                bluntDamageTakenModifier);
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

            if (SlashingDamageTakenModifier != 0d)
            {
                combatant.AddDamageTakenModifier(
                    DamageModifier.ForType(DamageType.Slashing, SlashingDamageTakenModifier));
            }

            if (PiercingDamageTakenModifier != 0d)
            {
                combatant.AddDamageTakenModifier(
                    DamageModifier.ForType(DamageType.Piercing, PiercingDamageTakenModifier));
            }

            if (BluntDamageTakenModifier != 0d)
            {
                combatant.AddDamageTakenModifier(
                    DamageModifier.ForType(DamageType.Blunt, BluntDamageTakenModifier));
            }

            combatant.AddEquipmentBattleBonuses(
                HealingPotionMultiplierBonus,
                GuardCounterPiercingMultiplier,
                ClashDamageDealtMultiplierBonus);
        }

    }
}
