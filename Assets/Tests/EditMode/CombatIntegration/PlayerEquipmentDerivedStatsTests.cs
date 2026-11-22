using NUnit.Framework;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Damage;
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
        }

        [Test]
        public void YellowAndRedSilkSets_ProvideTheirRewardBonuses()
        {
            PlayerEquipmentDerivedStats yellow = Resolve(CreateStateWithArmorSet("yellow-silk"));
            PlayerEquipmentDerivedStats red = Resolve(CreateStateWithArmorSet("red-silk"));

            Assert.That(yellow.VictoryGoldMultiplierBonus, Is.EqualTo(0.20d).Within(0.0001d));
            Assert.That(red.ExtraMaterialRewardRolls, Is.EqualTo(1));
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
