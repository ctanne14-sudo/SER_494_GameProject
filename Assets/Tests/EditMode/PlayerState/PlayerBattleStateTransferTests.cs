using System;
using System.Collections.Generic;
using NUnit.Framework;
using SER494.Gameplay.Combat;
using SER494.Gameplay.CombatIntegration;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    [Category("CombatIntegration")]
    public sealed class PlayerBattleStateTransferTests
    {
        [Test]
        public void CreateCombatant_CarriesAllPersistentResourcesIncludingStamina()
        {
            PlayerState state = CreateState();
            state.TakeDamage(35);
            Assert.That(state.TrySpendMana(8), Is.True);
            state.SetCurrentCombatStamina(11);

            CombatantState combatant = PlayerBattleStateTransfer.CreateCombatant(
                state,
                "Player",
                attack: 70,
                defense: 55);

            Assert.That(combatant.CurrentHealth, Is.EqualTo(65));
            Assert.That(combatant.CurrentMana, Is.EqualTo(22));
            Assert.That(combatant.CurrentStamina, Is.EqualTo(11));
            Assert.That(combatant.Stats.Attack, Is.EqualTo(70));
            Assert.That(combatant.Stats.Defense, Is.EqualTo(55));
        }

        [Test]
        public void CaptureInventory_ReturnsIndependentReadOnlySnapshot()
        {
            PlayerState state = CreateState();
            state.Inventory.Add("item.potion", 3);

            IReadOnlyDictionary<string, int> snapshot =
                PlayerBattleStateTransfer.CaptureInventory(state);
            state.Inventory.SetCount("item.potion", 1);

            Assert.That(snapshot["item.potion"], Is.EqualTo(3));
            var dictionaryView = (IDictionary<string, int>)snapshot;
            Assert.Throws<NotSupportedException>(
                () => dictionaryView["item.potion"] = 99);
        }

        [Test]
        public void ApplyBattleResult_WritesBackHealthManaAndRemainingItems()
        {
            PlayerState state = CreateState();
            state.Inventory.Add("item.potion", 3);
            state.Inventory.Add("item.quest-key", 1);
            var combatant = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 30, 50, 50),
                currentHealth: 42,
                currentMana: 9,
                currentStamina: 0);
            var remainingItems = new Dictionary<string, int>
            {
                ["item.potion"] = 1,
                ["item.quest-key"] = 1
            };

            PlayerBattleStateTransfer.ApplyBattleResult(
                state,
                combatant,
                remainingItems);

            Assert.That(state.CurrentHealth, Is.EqualTo(42));
            Assert.That(state.CurrentMana, Is.EqualTo(9));
            Assert.That(state.CurrentCombatStamina, Is.Zero);
            Assert.That(state.Inventory.GetCount("item.potion"), Is.EqualTo(1));
            Assert.That(state.Inventory.GetCount("item.quest-key"), Is.EqualTo(1));
        }

        [Test]
        public void ApplyBattleResult_RemovesItemsWhoseRemainingCountIsZero()
        {
            PlayerState state = CreateState();
            state.Inventory.Add("item.potion", 1);
            var combatant = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 30, 50, 50));

            PlayerBattleStateTransfer.ApplyBattleResult(
                state,
                combatant,
                new Dictionary<string, int> { ["item.potion"] = 0 });

            Assert.That(state.Inventory.GetCount("item.potion"), Is.Zero);
            Assert.That(state.Inventory.ItemCounts.ContainsKey("item.potion"), Is.False);
        }

        [Test]
        public void ApplyBattleResult_RejectsChangedPersistentMaximumsBeforeMutation()
        {
            PlayerState state = CreateState();
            var combatant = new CombatantState(
                "Player",
                new CombatantStats(120, 30, 30, 50, 50),
                currentHealth: 25,
                currentMana: 10,
                currentStamina: 30);

            Assert.Throws<InvalidOperationException>(() =>
                PlayerBattleStateTransfer.ApplyBattleResult(
                    state,
                    combatant,
                    new Dictionary<string, int>()));
            Assert.That(state.CurrentHealth, Is.EqualTo(100));
            Assert.That(state.CurrentMana, Is.EqualTo(30));
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
