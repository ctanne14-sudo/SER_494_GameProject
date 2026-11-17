using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Unity.Tests
{
    [Category("CombatIntegration")]
    public sealed class PlayerStateLoadoutSessionBuilderTests
    {
        private SoloComboLoadoutSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_session);
        }

        [Test]
        public void TryPrepare_ConvertsActivePlayerLoadoutAndEquippedWeapon()
        {
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();

            bool succeeded = PlayerStateLoadoutSessionBuilder.TryPrepare(
                state,
                _session,
                SoloComboTestScenario.GreatswordTrial,
                out string validationError);

            Assert.That(succeeded, Is.True, validationError);
            Assert.That(_session.EquippedWeapon.WeaponId,
                Is.EqualTo(PlayerStateInitialValues.InitialWeaponId));
            Assert.That(_session.EquippedWeapon.WeaponType,
                Is.EqualTo(WeaponType.Greatsword));
            Assert.That(
                _session.TryGetPreparedLoadout(out var configurations),
                Is.True);
            Assert.That(configurations.Count, Is.EqualTo(4));
            Assert.That(configurations[0].OpeningAction.ActionId,
                Is.EqualTo("greatsword.opening.quick-draw-slash"));
            Assert.That(configurations[2].LinkedAction.ActionId,
                Is.EqualTo("common.linked.use-healing-potion"));
        }

        [Test]
        public void TryPrepare_ActionOutsideOwnedActionSet_ReturnsError()
        {
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();
            PreparedComboState original = state.ComboLoadouts.ActiveLoadout.Combos[0];
            state.ComboLoadouts.ActiveLoadout.SetCombo(
                0,
                new PreparedComboState(
                    original.Name,
                    "common.opening.roll",
                    original.MainActionId,
                    original.ClosingActionId,
                    original.LinkedActionId,
                    original.LinkedPlacement));

            bool succeeded = PlayerStateLoadoutSessionBuilder.TryPrepare(
                state,
                _session,
                SoloComboTestScenario.GreatswordTrial,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(validationError, Does.Contain("not in the player's Action Set"));
            Assert.That(_session.HasPreparedLoadout, Is.False);
        }

        [Test]
        public void TryPrepare_IncompatibleOwnedAction_DoesNotBlockBattlePreparation()
        {
            const string incompatibleOpeningId =
                "sword-and-shield.opening.horizontal-slash";
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();
            state.ActionLibrary.Learn(incompatibleOpeningId);
            PreparedComboState original = state.ComboLoadouts.ActiveLoadout.Combos[0];
            state.ComboLoadouts.ActiveLoadout.SetCombo(
                0,
                new PreparedComboState(
                    original.Name,
                    incompatibleOpeningId,
                    original.MainActionId,
                    original.ClosingActionId,
                    original.LinkedActionId,
                    original.LinkedPlacement));

            bool succeeded = PlayerStateLoadoutSessionBuilder.TryPrepare(
                state,
                _session,
                SoloComboTestScenario.GreatswordTrial,
                out string validationError);

            Assert.That(succeeded, Is.True, validationError);
            Assert.That(
                _session.TryGetPreparedLoadout(out var configurations),
                Is.True);
            Assert.That(configurations[0].OpeningAction.ActionId,
                Is.EqualTo(incompatibleOpeningId));
        }

        [Test]
        public void TryBuildLoadout_UnlockedNonActiveLoadout_UsesRequestedPage()
        {
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();
            PlayerComboLoadoutState source = state.ComboLoadouts.ActiveLoadout;
            PlayerComboLoadoutState second = state.ComboLoadouts.UnlockNext();
            second.Rename("Second Greatsword Page");
            for (int index = 0; index < PlayerComboLoadoutState.ComboCount; index++)
            {
                second.SetCombo(index, source.Combos[index]);
            }

            bool succeeded = PlayerStateComboConfigurationBuilder.TryBuildLoadout(
                second,
                state,
                ComboActionCatalogAsset.LoadDefault(),
                out var configurations,
                out string validationError);

            Assert.That(succeeded, Is.True, validationError);
            Assert.That(configurations.Count, Is.EqualTo(PlayerComboLoadoutState.ComboCount));
            Assert.That(configurations[0].Name, Is.EqualTo("Charge Timing"));
            Assert.That(state.ComboLoadouts.ActiveLoadoutId, Is.Not.EqualTo(second.LoadoutId));
        }

        [Test]
        public void TryBuildCombo_ActionOutsideOwnedSet_ReturnsErrorWithoutChangingState()
        {
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();
            PreparedComboState original = state.ComboLoadouts.ActiveLoadout.Combos[0];
            var draft = new PreparedComboState(
                original.Name,
                "common.opening.roll",
                original.MainActionId,
                original.ClosingActionId,
                original.LinkedActionId,
                original.LinkedPlacement);

            bool succeeded = PlayerStateComboConfigurationBuilder.TryBuildCombo(
                draft,
                state,
                ComboActionCatalogAsset.LoadDefault(),
                out _,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(validationError, Does.Contain("not in the player's Action Set"));
            Assert.That(
                state.ComboLoadouts.ActiveLoadout.Combos[0].OpeningActionId,
                Is.EqualTo(original.OpeningActionId));
        }
    }
}
