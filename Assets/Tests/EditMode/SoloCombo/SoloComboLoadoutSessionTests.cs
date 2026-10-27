using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using UnityEngine;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    [Category("CoreCombat")]
    public sealed class SoloComboLoadoutSessionTests
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
        public void Prepare_CopiesLoadoutForNextScene()
        {
            ComboConfiguration combo = MainOnlyCombo();

            _session.Prepare(new[] { combo });
            bool found = _session.TryGetPreparedLoadout(out var prepared);

            Assert.That(found, Is.True);
            Assert.That(prepared.Count, Is.EqualTo(1));
            Assert.That(prepared[0], Is.SameAs(combo));
        }

        [Test]
        public void Clear_RemovesPreparedLoadout()
        {
            _session.Prepare(new[] { MainOnlyCombo() });

            _session.Clear();

            Assert.That(_session.HasPreparedLoadout, Is.False);
            Assert.That(_session.TryGetPreparedLoadout(out _), Is.False);
            Assert.That(_session.EquippedWeapon, Is.Null);
        }

        [Test]
        public void Prepare_WithWeapon_PreservesRuntimeWeaponDefinition()
        {
            var weapon = new WeaponDefinition(
                "weapon.bow",
                "Bow",
                WeaponType.Bow,
                weaponAttack: 50);

            _session.Prepare(new[] { MainOnlyCombo() }, weapon);

            Assert.That(_session.EquippedWeapon, Is.SameAs(weapon));
        }

        [Test]
        public void Prepare_WithScenario_PreservesSelectedTestScenario()
        {
            _session.Prepare(
                new[] { MainOnlyCombo() },
                equippedWeapon: null,
                SoloComboTestScenario.GlaiveAndShieldTrial);

            Assert.That(
                _session.SelectedTestScenario,
                Is.EqualTo(SoloComboTestScenario.GlaiveAndShieldTrial));
        }

        [Test]
        public void Clear_RestoresDefaultTestScenario()
        {
            _session.Prepare(
                new[] { MainOnlyCombo() },
                equippedWeapon: null,
                SoloComboTestScenario.SwordAndShieldTrial);

            _session.Clear();

            Assert.That(
                _session.SelectedTestScenario,
                Is.EqualTo(SoloComboTestScenario.GreatswordTrial));
        }

        [Test]
        public void EncounterId_MapsEveryTrialToStableEncounterData()
        {
            Assert.That(
                SoloComboTestScenarioInfo.EncounterId(
                    SoloComboTestScenario.GreatswordTrial),
                Is.EqualTo("training.greatsword"));
            Assert.That(
                SoloComboTestScenarioInfo.EncounterId(
                    SoloComboTestScenario.SwordAndShieldTrial),
                Is.EqualTo("training.sword-and-shield"));
            Assert.That(
                SoloComboTestScenarioInfo.EncounterId(
                    SoloComboTestScenario.GlaiveAndShieldTrial),
                Is.EqualTo("training.glaive-and-shield"));
        }

        private static ComboConfiguration MainOnlyCombo()
        {
            var main = new ComboActionDefinition(
                "main",
                "Main",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 1,
                damageMultiplier: 0.10d);
            return new ComboConfiguration("combo1", null, main, null, null, null);
        }
    }
}
