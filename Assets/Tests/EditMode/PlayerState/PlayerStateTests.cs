using System;
using NUnit.Framework;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    public sealed class PlayerStateTests
    {
        [Test]
        public void Constructor_StartsPersistentResourcesFull()
        {
            var state = CreateState();

            Assert.That(state.CurrentHealth, Is.EqualTo(100));
            Assert.That(state.CurrentMana, Is.EqualTo(30));
            Assert.That(state.MaximumCombatStamina, Is.EqualTo(30));
            Assert.That(state.CurrentCombatStamina, Is.EqualTo(30));
            Assert.That(state.BaseStaminaRecoveryPerRound, Is.EqualTo(10));
        }

        [Test]
        public void CombatStaminaMutations_ClampAndRestoreToMaximum()
        {
            var state = CreateState();

            state.SetCurrentCombatStamina(7);
            state.RestoreCombatStamina(5);
            Assert.That(state.CurrentCombatStamina, Is.EqualTo(12));

            state.RestoreCombatStaminaToMaximum();
            Assert.That(state.CurrentCombatStamina, Is.EqualTo(30));
        }

        [Test]
        public void HealthAndManaMutations_ClampAndRejectInsufficientSpend()
        {
            var state = CreateState();

            state.TakeDamage(120);
            state.RestoreHealth(25);
            Assert.That(state.CurrentHealth, Is.EqualTo(25));

            Assert.That(state.TrySpendMana(31), Is.False);
            Assert.That(state.TrySpendMana(12), Is.True);
            state.RestoreMana(50);
            Assert.That(state.CurrentMana, Is.EqualTo(30));
        }

        [Test]
        public void IncreaseMaximumHealth_PreservesCurrentHealth()
        {
            var state = CreateState();
            state.TakeDamage(25);

            state.IncreaseMaximumHealth(50);

            Assert.That(state.MaximumHealth, Is.EqualTo(150));
            Assert.That(state.CurrentHealth, Is.EqualTo(75));
        }

        [Test]
        public void Inventory_AddsConsumesAndRemovesZeroCountEntries()
        {
            var inventory = CreateState().Inventory;

            inventory.Add("item.potion", 2);
            Assert.That(inventory.TryConsume("item.potion", 1), Is.True);
            Assert.That(inventory.TryConsume("item.potion", 2), Is.False);
            Assert.That(inventory.TryConsume("item.potion", 1), Is.True);

            Assert.That(inventory.GetCount("item.potion"), Is.Zero);
            Assert.That(inventory.ItemCounts.ContainsKey("item.potion"), Is.False);
        }

        [Test]
        public void Currencies_TrackGoldSeparatelyFromItems()
        {
            PlayerCurrencyState currencies = CreateState().Currencies;

            currencies.Add(PlayerCurrencyState.GoldCurrencyId, 125);
            Assert.That(currencies.TrySpend(PlayerCurrencyState.GoldCurrencyId, 30), Is.True);
            Assert.That(currencies.TrySpend(PlayerCurrencyState.GoldCurrencyId, 100), Is.False);

            Assert.That(currencies.Gold, Is.EqualTo(95));
        }

        [Test]
        public void Equipment_UsesUniqueOwnershipAndRequiresOwnershipBeforeEquip()
        {
            var equipment = CreateState().Equipment;

            Assert.That(equipment.Grant(EquipmentCategory.Weapon, "weapon.greatsword.iron"), Is.True);
            Assert.That(equipment.Grant(EquipmentCategory.Weapon, "weapon.greatsword.iron"), Is.False);
            equipment.EquipWeapon("weapon.greatsword.iron");

            Assert.That(equipment.EquippedWeaponId, Is.EqualTo("weapon.greatsword.iron"));
            Assert.Throws<InvalidOperationException>(
                () => equipment.EquipWeapon("weapon.sword-and-shield.iron"));
        }

        [Test]
        public void Equipment_MovesSameUniqueItemBetweenSlots()
        {
            var equipment = CreateState().Equipment;
            equipment.Grant(EquipmentCategory.Accessory, "accessory.test-charm");

            equipment.EquipAccessory("accessory.1", "accessory.test-charm");
            equipment.EquipAccessory("accessory.2", "accessory.test-charm");

            Assert.That(equipment.EquippedAccessoryBySlot.ContainsKey("accessory.1"), Is.False);
            Assert.That(
                equipment.EquippedAccessoryBySlot["accessory.2"],
                Is.EqualTo("accessory.test-charm"));
        }

        [Test]
        public void ActionLibrary_TreatsActionOwnershipAsUniqueUnlocks()
        {
            var library = CreateState().ActionLibrary;

            Assert.That(library.Learn("action.greatsword.draw-slash.o"), Is.True);
            Assert.That(library.Learn("action.greatsword.draw-slash.o"), Is.False);
            Assert.That(library.Owns("action.greatsword.draw-slash.o"), Is.True);
        }

        [Test]
        public void ComboLoadouts_StartWithOneUnlockedPageAndFourEmptyCombos()
        {
            var loadouts = CreateState().ComboLoadouts;

            Assert.That(loadouts.UnlockedSlotCount, Is.EqualTo(1));
            Assert.That(loadouts.ActiveLoadout.LoadoutId, Is.EqualTo("loadout.1"));
            Assert.That(loadouts.ActiveLoadout.Combos, Has.Count.EqualTo(4));
            Assert.That(loadouts.ActiveLoadout.Combos[0].Name, Is.EqualTo("combo1"));
        }

        [Test]
        public void ComboLoadouts_CanUnlockAndManuallyActivateAnotherPage()
        {
            var loadouts = CreateState().ComboLoadouts;
            PlayerComboLoadoutState second = loadouts.UnlockNext();

            loadouts.SetActive(second.LoadoutId);

            Assert.That(loadouts.UnlockedSlotCount, Is.EqualTo(2));
            Assert.That(loadouts.ActiveLoadoutId, Is.EqualTo("loadout.2"));
        }

        [Test]
        public void PreparedCombo_StoresStableActionIdsWithoutWeaponBinding()
        {
            var combo = new PreparedComboState(
                "Guard Counter",
                "action.common.guard.o",
                "action.glaive.counter.m",
                closingActionId: null,
                "action.common.potion.l",
                PreparedLinkedActionPlacement.AfterMain);

            Assert.That(combo.MainActionId, Is.EqualTo("action.glaive.counter.m"));
            Assert.That(combo.LinkedPlacement, Is.EqualTo(PreparedLinkedActionPlacement.AfterMain));
            Assert.That(combo.GetAssignedActionIds(), Has.Count.EqualTo(3));
        }

        [Test]
        public void PreparedCombo_RejectsLinkedPlacementWithoutAssignedParent()
        {
            Assert.Throws<ArgumentException>(() => new PreparedComboState(
                "Invalid",
                openingActionId: null,
                "action.greatsword.draw-slash.m",
                closingActionId: null,
                "action.common.potion.l",
                PreparedLinkedActionPlacement.AfterOpening));
        }

        private static PlayerState CreateState()
        {
            return new PlayerState(
                maximumHealth: 100,
                maximumMana: 30,
                maximumCombatStamina: 30);
        }
    }
}
