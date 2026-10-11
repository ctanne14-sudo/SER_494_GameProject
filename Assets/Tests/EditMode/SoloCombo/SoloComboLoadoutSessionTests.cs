using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using UnityEngine;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
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
        }

        private static ComboConfiguration MainOnlyCombo()
        {
            var main = new ComboActionDefinition(
                "main",
                "Main",
                ComboActionCategory.Main,
                loadCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 1,
                damageMultiplier: 0.10d);
            return new ComboConfiguration("combo1", null, main, null, null, null);
        }
    }
}
