using System;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.SoloCombo;
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
            double slashingDamageTakenModifier = 0d,
            double piercingDamageTakenModifier = 0d,
            double bluntDamageTakenModifier = 0d,
            double defenseMultiplierBonus = 0d,
            int staminaRecoveryPerRoundBonus = 0,
            int maximumCombatStaminaBonus = 0,
            double allDamageTakenModifier = 0d,
            int flatVictoryGoldBonus = 0,
            int materialRewardRollModifier = 0,
            double actionLearningChance = ActionLearningRewardRoller.LearnChance,
            string statefulAccessoryId = null)
        {
            MaximumHealthBonus = maximumHealthBonus;
            AllDamageDealtModifier = allDamageDealtModifier;
            HealingPotionMultiplierBonus = healingPotionMultiplierBonus;
            GuardCounterPiercingMultiplier = guardCounterPiercingMultiplier;
            ClashDamageDealtMultiplierBonus = clashDamageDealtMultiplierBonus;
            VictoryGoldMultiplierBonus = victoryGoldMultiplierBonus;
            SlashingDamageTakenModifier = slashingDamageTakenModifier;
            PiercingDamageTakenModifier = piercingDamageTakenModifier;
            BluntDamageTakenModifier = bluntDamageTakenModifier;
            DefenseMultiplierBonus = defenseMultiplierBonus;
            StaminaRecoveryPerRoundBonus = staminaRecoveryPerRoundBonus;
            MaximumCombatStaminaBonus = maximumCombatStaminaBonus;
            AllDamageTakenModifier = allDamageTakenModifier;
            FlatVictoryGoldBonus = flatVictoryGoldBonus;
            MaterialRewardRollModifier = materialRewardRollModifier;
            ActionLearningChance = actionLearningChance;
            StatefulAccessoryId = statefulAccessoryId;
        }

        public int MaximumHealthBonus { get; }

        public double AllDamageDealtModifier { get; }

        public double HealingPotionMultiplierBonus { get; }

        public double GuardCounterPiercingMultiplier { get; }

        public double ClashDamageDealtMultiplierBonus { get; }

        public double VictoryGoldMultiplierBonus { get; }

        public int MaterialRewardRollModifier { get; }

        public double SlashingDamageTakenModifier { get; }

        public double PiercingDamageTakenModifier { get; }

        public double BluntDamageTakenModifier { get; }

        public double DefenseMultiplierBonus { get; }

        public int StaminaRecoveryPerRoundBonus { get; }

        public int MaximumCombatStaminaBonus { get; }

        public double AllDamageTakenModifier { get; }

        public int FlatVictoryGoldBonus { get; }

        public double ActionLearningChance { get; }

        public string StatefulAccessoryId { get; }

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
            double defenseMultiplierBonus = 0d;
            int staminaRecoveryPerRoundBonus = 0;
            int maximumCombatStaminaBonus = 0;
            double allDamageTakenModifier = 0d;
            int flatVictoryGoldBonus = 0;
            int materialRewardRollModifier = 0;
            double actionLearningChance = ActionLearningRewardRoller.LearnChance;
            string statefulAccessoryId = null;
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
                    case ArmorEffectType.DefenseMultiplier:
                        defenseMultiplierBonus += armor.EffectAmount;
                        break;
                    case ArmorEffectType.StaminaRecoveryPerRoundFlat:
                        staminaRecoveryPerRoundBonus = checked(
                            staminaRecoveryPerRoundBonus + RoundToInt(armor.EffectAmount));
                        break;
                    case ArmorEffectType.MaximumStaminaFlat:
                        maximumCombatStaminaBonus = checked(
                            maximumCombatStaminaBonus + RoundToInt(armor.EffectAmount));
                        break;
                    case ArmorEffectType.AllDamageTakenMultiplier:
                        allDamageTakenModifier -= armor.EffectAmount;
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
                        switch (accessory.AccessoryId)
                        {
                            case "accessory.bronze-ring":
                                defenseMultiplierBonus += 0.15d;
                                break;
                            case "accessory.crystal-pendant":
                                actionLearningChance = 0.70d;
                                break;
                            case "accessory.gold-ring":
                                materialRewardRollModifier -= 1;
                                flatVictoryGoldBonus = checked(flatVictoryGoldBonus + 300);
                                break;
                            case "accessory.white-copper-ring":
                            case "accessory.brass-ring":
                            case "accessory.ape-beast-fresh-blood-ornament":
                            case "accessory.slime-ornament":
                            case "accessory.yellow-handkerchief":
                            case "accessory.red-handkerchief":
                            case "accessory.thorn-bracer":
                                statefulAccessoryId = accessory.AccessoryId;
                                break;
                        }
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
                        materialRewardRollModifier += 1;
                        break;
                    case "armor-series.crystal":
                        allDamageDealtModifier += 0.10d;
                        break;
                    case "armor-series.pure-gold":
                        clashDamageDealtMultiplierBonus += 0.50d;
                        break;
                    case "armor-series.mithril":
                        allDamageTakenModifier -= 0.10d;
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
                slashingDamageTakenModifier,
                piercingDamageTakenModifier,
                bluntDamageTakenModifier,
                defenseMultiplierBonus,
                staminaRecoveryPerRoundBonus,
                maximumCombatStaminaBonus,
                allDamageTakenModifier,
                flatVictoryGoldBonus,
                materialRewardRollModifier,
                actionLearningChance,
                statefulAccessoryId);
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

        public void ApplyPersistentMaximumCombatStamina(PersistentPlayerState playerState)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            playerState.SetEquipmentMaximumCombatStaminaBonus(
                MaximumCombatStaminaBonus);
        }

        public int ApplyDefenseMultiplier(int baseDefense)
        {
            if (baseDefense < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDefense));
            }

            return checked((int)Math.Round(
                baseDefense * (1d + DefenseMultiplierBonus),
                MidpointRounding.AwayFromZero));
        }

        public int ApplyStaminaRecoveryPerRound(int baseRecovery)
        {
            if (baseRecovery < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRecovery));
            }

            return checked(baseRecovery + StaminaRecoveryPerRoundBonus);
        }

        public int ApplyVictoryGoldModifiers(int baseGold)
        {
            if (baseGold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseGold));
            }

            int multipliedGold = checked((int)Math.Round(
                baseGold * (1d + VictoryGoldMultiplierBonus),
                MidpointRounding.AwayFromZero));
            return checked(multipliedGold + FlatVictoryGoldBonus);
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

            if (AllDamageTakenModifier != 0d)
            {
                combatant.AddDamageTakenModifier(
                    DamageModifier.All(AllDamageTakenModifier));
            }

            combatant.AddEquipmentBattleBonuses(
                HealingPotionMultiplierBonus,
                GuardCounterPiercingMultiplier,
                ClashDamageDealtMultiplierBonus);
        }

        public SoloComboEquipmentEffectConfiguration CreateBattleEffectConfiguration()
        {
            return new SoloComboEquipmentEffectConfiguration(
                disableEnemyGuardDuringFirstRound: string.Equals(
                    StatefulAccessoryId,
                    "accessory.white-copper-ring",
                    StringComparison.Ordinal),
                applyBrassRingDefenseCurve: string.Equals(
                    StatefulAccessoryId,
                    "accessory.brass-ring",
                    StringComparison.Ordinal),
                healFromFirstPlayerDamageEachRound: string.Equals(
                    StatefulAccessoryId,
                    "accessory.ape-beast-fresh-blood-ornament",
                    StringComparison.Ordinal),
                nullifyPeriodicIncomingBluntDamage: string.Equals(
                    StatefulAccessoryId,
                    "accessory.slime-ornament",
                    StringComparison.Ordinal),
                preserveActivatedBattleItems: string.Equals(
                    StatefulAccessoryId,
                    "accessory.yellow-handkerchief",
                    StringComparison.Ordinal),
                trackDamageResolutionsForMaterialRewards: string.Equals(
                    StatefulAccessoryId,
                    "accessory.red-handkerchief",
                    StringComparison.Ordinal),
                applyThornBracerRoundEndHealth: string.Equals(
                    StatefulAccessoryId,
                    "accessory.thorn-bracer",
                    StringComparison.Ordinal));
        }

        private static int RoundToInt(float value)
        {
            return checked((int)Math.Round(value, MidpointRounding.AwayFromZero));
        }

    }
}
