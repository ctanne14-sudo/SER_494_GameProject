using NUnit.Framework;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.PlayerState;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Tests
{
    [Category("CombatIntegration")]
    public sealed class PlayerEquipmentDerivedStatsTests
    {
        [Test]
        public void RustRing_IncreasesPersistentAndCombatMaximumHealth()
        {
            var state = new PersistentPlayerState(100, 30, 100);
            state.Equipment.Grant(EquipmentCategory.Accessory, "accessory.rust-ring");
            state.Equipment.EquipAccessory("accessory.primary", "accessory.rust-ring");

            bool resolved = PlayerEquipmentDerivedStats.TryResolve(
                state,
                PlayerEquipmentCatalogAsset.LoadDefault(),
                out PlayerEquipmentDerivedStats stats,
                out string error);

            Assert.That(resolved, Is.True, error);
            stats.ApplyPersistentMaximumHealth(state);
            Assert.That(state.BaseMaximumHealth, Is.EqualTo(100));
            Assert.That(state.MaximumHealth, Is.EqualTo(120));
        }

        [Test]
        public void RubyRing_AddsAllDamageModifierToCombatant()
        {
            var state = new PersistentPlayerState(100, 30, 100);
            state.Equipment.Grant(EquipmentCategory.Accessory, "accessory.ruby-ring");
            state.Equipment.EquipAccessory("accessory.primary", "accessory.ruby-ring");
            Assert.That(
                PlayerEquipmentDerivedStats.TryResolve(
                    state,
                    PlayerEquipmentCatalogAsset.LoadDefault(),
                    out PlayerEquipmentDerivedStats stats,
                    out string error),
                Is.True,
                error);
            var combatant = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 100, 50, 10));

            stats.ApplyCombatModifiers(combatant);

            Assert.That(combatant.DamageDealtModifiers, Has.Count.EqualTo(1));
            DamageModifier modifier = combatant.DamageDealtModifiers[0];
            Assert.That(modifier.Scope, Is.EqualTo(DamageModifierScope.All));
            Assert.That(modifier.Amount, Is.EqualTo(0.05d).Within(0.0001d));
        }

        [Test]
        public void ApeBeastSet_IncreasesMaximumHealthByTenPercent()
        {
            PersistentPlayerState state = CreateStateWithArmorSet("ape-beast");

            PlayerEquipmentDerivedStats stats = Resolve(state);
            stats.ApplyPersistentMaximumHealth(state);

            Assert.That(stats.MaximumHealthBonus, Is.EqualTo(10));
            Assert.That(state.MaximumHealth, Is.EqualTo(110));
        }

        [Test]
        public void SlimeSet_StacksHealingPotionBonusAcrossAllPieces()
        {
            PlayerEquipmentDerivedStats stats = Resolve(CreateStateWithArmorSet("slime"));

            Assert.That(stats.HealingPotionMultiplierBonus, Is.EqualTo(0.40d).Within(0.0001d));
        }

        [Test]
        public void ThornSet_StacksGuardCounterPiercingMultiplierAcrossAllPieces()
        {
            PlayerEquipmentDerivedStats stats = Resolve(CreateStateWithArmorSet("thorn"));

            Assert.That(stats.GuardCounterPiercingMultiplier, Is.EqualTo(0.50d).Within(0.0001d));
        }

        [Test]
        public void CrystalSet_AppliesLatestPieceAndFullSetBonuses()
        {
            PlayerEquipmentDerivedStats stats = Resolve(CreateStateWithArmorSet("crystal"));

            Assert.That(stats.AllDamageDealtModifier, Is.EqualTo(0.15d).Within(0.0001d));
            Assert.That(stats.BluntDamageTakenModifier, Is.EqualTo(-0.05d).Within(0.0001d));
            Assert.That(stats.StaminaRecoveryPerRoundBonus, Is.EqualTo(3));
            Assert.That(stats.ApplyStaminaRecoveryPerRound(10), Is.EqualTo(13));
        }

        [Test]
        public void YellowAndRedSilkSets_ProvideTheirRewardBonuses()
        {
            PlayerEquipmentDerivedStats yellow = Resolve(CreateStateWithArmorSet("yellow-silk"));
            PlayerEquipmentDerivedStats red = Resolve(CreateStateWithArmorSet("red-silk"));

            Assert.That(yellow.VictoryGoldMultiplierBonus, Is.EqualTo(0.20d).Within(0.0001d));
            Assert.That(red.MaterialRewardRollModifier, Is.EqualTo(1));
        }

        [Test]
        public void BronzeRing_IncreasesTotalDefenseByFifteenPercent()
        {
            PlayerEquipmentDerivedStats stats = Resolve(
                CreateStateWithAccessory("accessory.bronze-ring"));

            Assert.That(stats.DefenseMultiplierBonus, Is.EqualTo(0.15d).Within(0.0001d));
            Assert.That(stats.ApplyDefenseMultiplier(105), Is.EqualTo(121));
        }

        [Test]
        public void CrystalPendant_RaisesOnlyActionLearningChance()
        {
            PlayerEquipmentDerivedStats stats = Resolve(
                CreateStateWithAccessory("accessory.crystal-pendant"));

            Assert.That(stats.ActionLearningChance, Is.EqualTo(0.70d).Within(0.0001d));
            Assert.That(stats.FlatVictoryGoldBonus, Is.Zero);
            Assert.That(stats.MaterialRewardRollModifier, Is.Zero);
        }

        [Test]
        public void GoldRing_AppliesPercentageGoldBeforeFlatBonusAndReducesMaterialRolls()
        {
            PersistentPlayerState state = CreateStateWithArmorSet("yellow-silk");
            EquipAccessory(state, "accessory.gold-ring");

            PlayerEquipmentDerivedStats stats = Resolve(state);

            Assert.That(stats.VictoryGoldMultiplierBonus, Is.EqualTo(0.20d).Within(0.0001d));
            Assert.That(stats.FlatVictoryGoldBonus, Is.EqualTo(300));
            Assert.That(stats.MaterialRewardRollModifier, Is.EqualTo(-1));
            Assert.That(stats.ApplyVictoryGoldModifiers(100), Is.EqualTo(420));
        }

        [TestCase("white-copper", DamageType.Slashing)]
        [TestCase("bronze", DamageType.Piercing)]
        [TestCase("brass", DamageType.Blunt)]
        public void MetalSets_StackTenPercentDamageTypeReduction(
            string seriesId,
            DamageType damageType)
        {
            PlayerEquipmentDerivedStats stats = Resolve(CreateStateWithArmorSet(seriesId));

            double modifier = damageType == DamageType.Slashing
                ? stats.SlashingDamageTakenModifier
                : damageType == DamageType.Piercing
                    ? stats.PiercingDamageTakenModifier
                    : stats.BluntDamageTakenModifier;
            Assert.That(modifier, Is.EqualTo(-0.10d).Within(0.0001d));
        }

        [Test]
        public void PureGoldSet_AppliesLatestPieceAndFullSetBonuses()
        {
            PlayerEquipmentDerivedStats stats = Resolve(CreateStateWithArmorSet("pure-gold"));

            Assert.That(stats.HealingPotionMultiplierBonus, Is.EqualTo(0.10d).Within(0.0001d));
            Assert.That(stats.SlashingDamageTakenModifier, Is.EqualTo(-0.05d).Within(0.0001d));
            Assert.That(stats.ClashDamageDealtMultiplierBonus, Is.EqualTo(0.50d).Within(0.0001d));
            Assert.That(stats.DefenseMultiplierBonus, Is.EqualTo(0.10d).Within(0.0001d));
            Assert.That(stats.ApplyDefenseMultiplier(105), Is.EqualTo(116));
        }

        [Test]
        public void MithrilSet_AddsMaximumStaminaAndReducesAllIncomingDamage()
        {
            PersistentPlayerState state = CreateStateWithArmorSet("mithril");
            state.SetCurrentCombatStamina(90);
            PlayerEquipmentDerivedStats stats = Resolve(state);

            stats.ApplyPersistentMaximumCombatStamina(state);

            Assert.That(stats.MaximumCombatStaminaBonus, Is.EqualTo(10));
            Assert.That(state.BaseMaximumCombatStamina, Is.EqualTo(100));
            Assert.That(state.MaximumCombatStamina, Is.EqualTo(110));
            Assert.That(state.CurrentCombatStamina, Is.EqualTo(90));
            Assert.That(stats.AllDamageTakenModifier, Is.EqualTo(-0.10d).Within(0.0001d));

            var combatant = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 110, 50, 200));
            stats.ApplyCombatModifiers(combatant);
            Assert.That(combatant.DamageTakenModifiers, Has.Count.EqualTo(2));
            Assert.That(combatant.DamageTakenModifiers[0].Scope,
                Is.EqualTo(DamageModifierScope.Type));
            Assert.That(combatant.DamageTakenModifiers[0].Type,
                Is.EqualTo(DamageType.Piercing));
            Assert.That(combatant.DamageTakenModifiers[0].Amount,
                Is.EqualTo(-0.05d).Within(0.0001d));
            Assert.That(combatant.DamageTakenModifiers[1].Scope,
                Is.EqualTo(DamageModifierScope.All));
            Assert.That(combatant.DamageTakenModifiers[1].Amount,
                Is.EqualTo(-0.10d).Within(0.0001d));

            state.SetCurrentCombatStamina(108);
            state.Equipment.UnequipArmor("armor.lower-body");
            Resolve(state).ApplyPersistentMaximumCombatStamina(state);

            Assert.That(state.MaximumCombatStamina, Is.EqualTo(100));
            Assert.That(state.CurrentCombatStamina, Is.EqualTo(100));
        }

        [TestCase("accessory.white-copper-ring")]
        [TestCase("accessory.brass-ring")]
        [TestCase("accessory.ape-beast-fresh-blood-ornament")]
        [TestCase("accessory.slime-ornament")]
        [TestCase("accessory.yellow-handkerchief")]
        [TestCase("accessory.red-handkerchief")]
        [TestCase("accessory.thorn-bracer")]
        public void StatefulAccessory_CreatesItsPerBattleConfiguration(
            string accessoryId)
        {
            PlayerEquipmentDerivedStats stats = Resolve(
                CreateStateWithAccessory(accessoryId));
            SoloComboEquipmentEffectConfiguration configuration =
                stats.CreateBattleEffectConfiguration();

            Assert.That(stats.StatefulAccessoryId, Is.EqualTo(accessoryId));
            Assert.That(
                configuration.DisableEnemyGuardDuringFirstRound
                || configuration.ApplyBrassRingDefenseCurve
                || configuration.HealFromFirstPlayerDamageEachRound
                || configuration.NullifyPeriodicIncomingBluntDamage
                || configuration.PreserveActivatedBattleItems
                || configuration.TrackDamageResolutionsForMaterialRewards
                || configuration.ApplyThornBracerRoundEndHealth,
                Is.True);
        }

        private static PlayerEquipmentDerivedStats Resolve(PersistentPlayerState state)
        {
            Assert.That(
                PlayerEquipmentDerivedStats.TryResolve(
                    state,
                    PlayerEquipmentCatalogAsset.LoadDefault(),
                    out PlayerEquipmentDerivedStats stats,
                    out string error),
                Is.True,
                error);
            return stats;
        }

        private static PersistentPlayerState CreateStateWithArmorSet(string seriesId)
        {
            var state = new PersistentPlayerState(100, 30, 100);
            EquipArmor(state, "armor.head", $"armor.{seriesId}.head");
            EquipArmor(state, "armor.upper-body", $"armor.{seriesId}.upper");
            EquipArmor(state, "armor.lower-body", $"armor.{seriesId}.lower");
            return state;
        }

        private static PersistentPlayerState CreateStateWithAccessory(string accessoryId)
        {
            var state = new PersistentPlayerState(100, 30, 100);
            EquipAccessory(state, accessoryId);
            return state;
        }

        private static void EquipAccessory(
            PersistentPlayerState state,
            string accessoryId)
        {
            state.Equipment.Grant(EquipmentCategory.Accessory, accessoryId);
            state.Equipment.EquipAccessory("accessory.primary", accessoryId);
        }

        private static void EquipArmor(
            PersistentPlayerState state,
            string slotId,
            string armorId)
        {
            state.Equipment.Grant(EquipmentCategory.Armor, armorId);
            state.Equipment.EquipArmor(slotId, armorId);
        }
    }
}
