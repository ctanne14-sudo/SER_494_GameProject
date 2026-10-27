using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    public sealed class PlayerStateServiceTests
    {
        private static readonly MethodInfo AwakeMethod = typeof(PlayerStateService).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Awake_CreatesDefaultStateAndBecomesAuthority()
        {
            PlayerStateService service = CreateService("Primary Player State");

            Assert.That(service.IsAuthority, Is.True);
            Assert.That(PlayerStateService.Active, Is.SameAs(service));
            Assert.That(service.State.CurrentHealth, Is.EqualTo(100));
            Assert.That(service.State.MaximumMana, Is.EqualTo(30));
            Assert.That(service.State.MaximumCombatStamina, Is.EqualTo(100));
            Assert.That(service.State.BaseStaminaRecoveryPerRound, Is.EqualTo(10));
            Assert.That(service.State.ComboLoadouts.UnlockedSlotCount, Is.EqualTo(1));
            Assert.That(
                service.State.Equipment.EquippedWeaponId,
                Is.EqualTo(PlayerStateInitialValues.InitialWeaponId));
            Assert.That(
                service.State.Inventory.GetCount(PlayerStateInitialValues.HealingPotionId),
                Is.EqualTo(3));
        }

        [Test]
        public void StartNewGame_ReplacesStateUsingExplicitInitialValues()
        {
            PlayerStateService service = CreateService("Player State");
            int replacementCount = 0;
            service.StateReplaced += _ => replacementCount++;

            service.StartNewGame(new PlayerStateInitialValues(
                maximumHealth: 150,
                maximumMana: 40,
                maximumCombatStamina: 35,
                initiallyUnlockedLoadoutSlots: 2));

            Assert.That(service.State.MaximumHealth, Is.EqualTo(150));
            Assert.That(service.State.MaximumMana, Is.EqualTo(40));
            Assert.That(service.State.MaximumCombatStamina, Is.EqualTo(35));
            Assert.That(service.State.BaseStaminaRecoveryPerRound, Is.EqualTo(10));
            Assert.That(service.State.ComboLoadouts.UnlockedSlotCount, Is.EqualTo(2));
            Assert.That(replacementCount, Is.EqualTo(1));
        }

        [Test]
        public void InitialValues_CreateConfiguredStartingEquipmentActionsItemsAndLoadout()
        {
            PlayerState state = new PlayerStateInitialValues().CreateState();

            Assert.That(state.Equipment.EquippedWeaponId,
                Is.EqualTo(PlayerStateInitialValues.InitialWeaponId));
            Assert.That(
                state.Equipment.EquippedArmorBySlot[PlayerStateInitialValues.HeadArmorSlotId],
                Is.EqualTo(PlayerStateInitialValues.LinenHeadpieceId));
            Assert.That(
                state.Equipment.EquippedArmorBySlot[PlayerStateInitialValues.UpperBodyArmorSlotId],
                Is.EqualTo(PlayerStateInitialValues.LinenShirtId));
            Assert.That(
                state.Equipment.EquippedArmorBySlot[PlayerStateInitialValues.LowerBodyArmorSlotId],
                Is.EqualTo(PlayerStateInitialValues.LinenHoseId));

            Assert.That(state.Inventory.GetCount(PlayerStateInitialValues.HealingPotionId),
                Is.EqualTo(3));
            Assert.That(state.Inventory.GetCount(PlayerStateInitialValues.GreaterHealingPotionId),
                Is.EqualTo(1));
            Assert.That(state.Inventory.GetCount(PlayerStateInitialValues.VitalityTonicId),
                Is.EqualTo(3));

            Assert.That(state.ActionLibrary.OwnedActionIds, Has.Count.EqualTo(21));
            Assert.That(state.ActionLibrary.Owns("greatsword.main.charged-slash"), Is.True);
            Assert.That(state.ActionLibrary.Owns("common.opening.prepare"), Is.True);
            Assert.That(state.ActionLibrary.Owns("common.main.focused-guard-1"), Is.True);
            Assert.That(state.ActionLibrary.Owns("common.closing.breather-1"), Is.True);
            Assert.That(state.ActionLibrary.Owns("common.linked.use-healing-potion"), Is.True);
            Assert.That(state.ActionLibrary.Owns("common.linked.use-vitality-tonic"), Is.True);

            PlayerComboLoadoutState loadout = state.ComboLoadouts.ActiveLoadout;
            Assert.That(loadout.Name, Is.EqualTo("Greatsword Loadout"));
            Assert.That(loadout.Combos, Has.Count.EqualTo(4));

            var assignedActions = new HashSet<string>(StringComparer.Ordinal);
            for (int comboIndex = 0; comboIndex < loadout.Combos.Count; comboIndex++)
            {
                IReadOnlyList<string> comboActions =
                    loadout.Combos[comboIndex].GetAssignedActionIds();
                Assert.That(comboActions, Has.Count.EqualTo(4));
                for (int actionIndex = 0; actionIndex < comboActions.Count; actionIndex++)
                {
                    Assert.That(
                        assignedActions.Add(comboActions[actionIndex]),
                        Is.True,
                        $"Starting Action '{comboActions[actionIndex]}' is assigned to more than one Combo.");
                    Assert.That(state.ActionLibrary.Owns(comboActions[actionIndex]), Is.True);
                }
            }
        }

        [Test]
        public void RestoreState_UsesProvidedStateAsAuthoritativeInstance()
        {
            PlayerStateService service = CreateService("Player State");
            var restored = new PlayerState(90, 20, 25);
            restored.TakeDamage(15);

            service.RestoreState(restored);

            Assert.That(service.State, Is.SameAs(restored));
            Assert.That(service.State.CurrentHealth, Is.EqualTo(75));
        }

        [Test]
        public void DuplicateService_DoesNotReplaceExistingAuthority()
        {
            PlayerStateService first = CreateService("First Player State");
            PlayerStateService second = CreateService("Duplicate Player State");

            Assert.That(PlayerStateService.Active, Is.SameAs(first));
            Assert.That(first.IsAuthority, Is.True);
            Assert.That(second.IsAuthority, Is.False);
            Assert.That(second.enabled, Is.False);
            Assert.Throws<InvalidOperationException>(() => second.RestoreState(
                new PlayerState(100, 30, 30)));
        }

        [Test]
        public void DestroyingAuthority_ReleasesStaticReference()
        {
            PlayerStateService service = CreateService("Player State");

            UnityEngine.Object.DestroyImmediate(service.gameObject);

            Assert.That(PlayerStateService.TryGetActive(out _), Is.False);
        }

        private PlayerStateService CreateService(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            PlayerStateService service = gameObject.AddComponent<PlayerStateService>();

            Assert.That(AwakeMethod, Is.Not.Null,
                "The test must invoke PlayerStateService.Awake because Edit Mode does not run it automatically.");
            AwakeMethod.Invoke(service, null);

            return service;
        }
    }
}
