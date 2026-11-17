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
            ConfigureStarterCombo(state.ComboLoadouts.ActiveLoadout);

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
            Assert.That(configurations.Count, Is.EqualTo(1));
            Assert.That(configurations[0].OpeningAction.ActionId,
                Is.EqualTo("greatsword.opening.sweeping-slash"));
            Assert.That(configurations[0].LinkedAction.ActionId,
                Is.EqualTo("greatsword.linked.shoulder-tackle"));
        }

        [Test]
        public void TryPrepare_EmptyStartingLoadout_ReturnsPlayerFacingError()
        {
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();

            bool succeeded = PlayerStateLoadoutSessionBuilder.TryPrepare(
                state,
                _session,
                SoloComboTestScenario.GreatswordTrial,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(validationError,
                Is.EqualTo(
                    "No Combo is configured. Configure at least one Combo before entering battle."));
            Assert.That(_session.HasPreparedLoadout, Is.False);
        }

        [Test]
        public void TryPrepare_ActionOutsideOwnedActionSet_ReturnsError()
        {
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();
            ConfigureStarterCombo(state.ComboLoadouts.ActiveLoadout);
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
            ConfigureStarterCombo(state.ComboLoadouts.ActiveLoadout);
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
            ConfigureStarterCombo(source);
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
            Assert.That(configurations.Count, Is.EqualTo(1));
            Assert.That(configurations[0].Name, Is.EqualTo("Starter Combo"));
            Assert.That(state.ComboLoadouts.ActiveLoadoutId, Is.Not.EqualTo(second.LoadoutId));
        }

        [Test]
        public void TryBuildCombo_ActionOutsideOwnedSet_ReturnsErrorWithoutChangingState()
        {
            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();
            ConfigureStarterCombo(state.ComboLoadouts.ActiveLoadout);
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

        private static void ConfigureStarterCombo(PlayerComboLoadoutState loadout)
        {
            loadout.SetCombo(
                0,
                new PreparedComboState(
                    "Starter Combo",
                    "greatsword.opening.sweeping-slash",
                    "greatsword.main.charged-slash",
                    "greatsword.closing.empowered-sheathe",
                    "greatsword.linked.shoulder-tackle",
                    PreparedLinkedActionPlacement.AfterMain));
        }
    }
}
