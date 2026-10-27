using System;
using NUnit.Framework;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    [Category("Persistence")]
    public sealed class PlayerStateSnapshotTests
    {
        [Test]
        public void CaptureAndRestore_PreservesCompletePlayerState()
        {
            PlayerState original = CreatePopulatedState();

            PlayerState restored = PlayerStateSnapshotMapper.Restore(
                PlayerStateSnapshotMapper.Capture(original));

            Assert.That(restored.CurrentHealth, Is.EqualTo(83));
            Assert.That(restored.BaseMaximumHealth, Is.EqualTo(100));
            Assert.That(restored.EquipmentMaximumHealthBonus, Is.EqualTo(20));
            Assert.That(restored.MaximumHealth, Is.EqualTo(120));
            Assert.That(restored.CurrentMana, Is.EqualTo(17));
            Assert.That(restored.MaximumMana, Is.EqualTo(30));
            Assert.That(restored.MaximumCombatStamina, Is.EqualTo(100));
            Assert.That(restored.BaseStaminaRecoveryPerRound, Is.EqualTo(10));
            Assert.That(restored.Inventory.GetCount("item.healing-potion"), Is.EqualTo(3));
            Assert.That(restored.Inventory.GetCount("material.iron"), Is.EqualTo(7));
            Assert.That(restored.Currencies.Gold, Is.EqualTo(250));
            Assert.That(restored.Equipment.EquippedWeaponId,
                Is.EqualTo("weapon.greatsword.cast-iron"));
            Assert.That(restored.Equipment.EquippedArmorBySlot["armor.head"],
                Is.EqualTo("armor.leather-headpiece"));
            Assert.That(restored.Equipment.EquippedAccessoryBySlot["accessory.primary"],
                Is.EqualTo("accessory.rust-ring"));
            Assert.That(restored.ActionLibrary.Owns("greatsword.opening.draw"), Is.True);
            Assert.That(restored.ActionLibrary.Owns("greatsword.main.slash"), Is.True);
            Assert.That(restored.ComboLoadouts.UnlockedSlotCount, Is.EqualTo(2));
            Assert.That(restored.ComboLoadouts.ActiveLoadoutId, Is.EqualTo("loadout.2"));

            PlayerComboLoadoutState first = restored.ComboLoadouts.Loadouts[0];
            Assert.That(first.Name, Is.EqualTo("Greatsword Loadout"));
            Assert.That(first.Combos[0].Name, Is.EqualTo("Opening Test"));
            Assert.That(first.Combos[0].LinkedPlacement,
                Is.EqualTo(PreparedLinkedActionPlacement.AfterMain));
            Assert.That(first.Combos[0].LinkedActionId,
                Is.EqualTo("greatsword.linked.tackle"));
            Assert.That(restored.ComboLoadouts.Loadouts[1].Name,
                Is.EqualTo("Second Loadout"));
        }

        [Test]
        public void Capture_UsesStableOrdinalOrderingForIdCollections()
        {
            PlayerState state = CreatePopulatedState();

            PlayerStateSnapshot snapshot = PlayerStateSnapshotMapper.Capture(state);

            Assert.That(snapshot.Inventory[0].Id, Is.EqualTo("item.healing-potion"));
            Assert.That(snapshot.Inventory[1].Id, Is.EqualTo("material.iron"));
            Assert.That(snapshot.OwnedActionIds[0], Is.EqualTo("greatsword.main.slash"));
            Assert.That(snapshot.OwnedActionIds[1], Is.EqualTo("greatsword.opening.draw"));
            Assert.That(snapshot.Equipment.OwnedWeaponIds[0],
                Is.EqualTo("weapon.greatsword.cast-iron"));
            Assert.That(snapshot.Equipment.OwnedWeaponIds[1],
                Is.EqualTo("weapon.greatsword.rusted-iron"));
        }

        [Test]
        public void Restore_RejectsUnsupportedSnapshotVersion()
        {
            PlayerStateSnapshot snapshot = PlayerStateSnapshotMapper.Capture(
                CreatePopulatedState());
            snapshot.Version++;

            Assert.Throws<NotSupportedException>(
                () => PlayerStateSnapshotMapper.Restore(snapshot));
        }

        [Test]
        public void Restore_RejectsSnapshotWithoutLoadout()
        {
            var snapshot = new PlayerStateSnapshot
            {
                BaseMaximumHealth = 100,
                CurrentHealth = 100,
                MaximumMana = 30,
                CurrentMana = 30,
                MaximumCombatStamina = 100,
                BaseStaminaRecoveryPerRound = 10
            };

            Assert.Throws<ArgumentException>(
                () => PlayerStateSnapshotMapper.Restore(snapshot));
        }

        [Test]
        public void Restore_TreatsMissingOptionalCollectionsAsEmpty()
        {
            PlayerStateSnapshot snapshot = PlayerStateSnapshotMapper.Capture(
                new PlayerState(100, 30, 100));
            snapshot.Inventory = null;
            snapshot.Currencies = null;
            snapshot.Equipment = null;
            snapshot.OwnedActionIds = null;

            PlayerState restored = PlayerStateSnapshotMapper.Restore(snapshot);

            Assert.That(restored.Inventory.ItemCounts, Is.Empty);
            Assert.That(restored.Currencies.Amounts, Is.Empty);
            Assert.That(restored.Equipment.OwnedWeaponIds, Is.Empty);
            Assert.That(restored.ActionLibrary.OwnedActionIds, Is.Empty);
            Assert.That(restored.ComboLoadouts.UnlockedSlotCount, Is.EqualTo(1));
        }

        private static PlayerState CreatePopulatedState()
        {
            var state = new PlayerState(
                maximumHealth: 100,
                maximumMana: 30,
                maximumCombatStamina: 100,
                initiallyUnlockedLoadoutSlots: 2,
                baseStaminaRecoveryPerRound: 10);
            state.SetEquipmentMaximumHealthBonus(20);
            state.SetCurrentHealth(83);
            state.SetCurrentMana(17);

            state.Inventory.Add("material.iron", 7);
            state.Inventory.Add("item.healing-potion", 3);
            state.Currencies.Add(PlayerCurrencyState.GoldCurrencyId, 250);

            state.Equipment.Grant(
                EquipmentCategory.Weapon,
                "weapon.greatsword.rusted-iron");
            state.Equipment.Grant(
                EquipmentCategory.Weapon,
                "weapon.greatsword.cast-iron");
            state.Equipment.EquipWeapon("weapon.greatsword.cast-iron");
            state.Equipment.Grant(
                EquipmentCategory.Armor,
                "armor.leather-headpiece");
            state.Equipment.EquipArmor("armor.head", "armor.leather-headpiece");
            state.Equipment.Grant(
                EquipmentCategory.Accessory,
                "accessory.rust-ring");
            state.Equipment.EquipAccessory(
                "accessory.primary",
                "accessory.rust-ring");

            state.ActionLibrary.Learn("greatsword.opening.draw");
            state.ActionLibrary.Learn("greatsword.main.slash");

            PlayerComboLoadoutState first = state.ComboLoadouts.Loadouts[0];
            first.Rename("Greatsword Loadout");
            first.SetCombo(0, new PreparedComboState(
                "Opening Test",
                "greatsword.opening.draw",
                "greatsword.main.slash",
                closingActionId: null,
                linkedActionId: "greatsword.linked.tackle",
                linkedPlacement: PreparedLinkedActionPlacement.AfterMain));
            state.ComboLoadouts.Loadouts[1].Rename("Second Loadout");
            state.ComboLoadouts.SetActive("loadout.2");
            return state;
        }
    }
}
