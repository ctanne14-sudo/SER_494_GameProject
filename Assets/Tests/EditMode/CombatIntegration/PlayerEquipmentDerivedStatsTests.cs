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
    }
}
