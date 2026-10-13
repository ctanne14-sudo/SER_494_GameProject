using System;
using NUnit.Framework;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.Tests
{
    public sealed class DamageCalculatorTests
    {
        [Test]
        public void DamageComponent_RejectsUnknownDamageType()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DamageComponentDefinition((DamageType)999, 0.5d));
        }

        [Test]
        public void DamageEffect_RejectsUninitializedComponent()
        {
            Assert.Throws<ArgumentException>(
                () => new DamageEffectDefinition(
                    new[] { default(DamageComponentDefinition) }));
        }

        private static readonly BattleDamageConfiguration Configuration =
            BattleDamageConfiguration.CreateMvpDefault();

        [Test]
        public void CalculateOrdinaryComponent_MvpBaseline_ProducesExpectedDamageRange()
        {
            DamageFormulaResult lowerResult = DamageCalculator.CalculateOrdinaryComponent(
                Input(actionMultiplier: 0.30d),
                Configuration);
            DamageFormulaResult upperResult = DamageCalculator.CalculateOrdinaryComponent(
                Input(actionMultiplier: 0.70d),
                Configuration);

            Assert.That(lowerResult.FinalDamage, Is.EqualTo(10));
            Assert.That(upperResult.FinalDamage, Is.EqualTo(23));
        }

        [Test]
        public void CalculateOrdinaryComponent_UsesAllFormulaBuckets()
        {
            var input = new DamageFormulaInput(
                actionMultiplier: 0.50d,
                weaponAttack: 50,
                attackModifier: 0.20d,
                baseDefense: 50,
                defenseModifier: -0.20d,
                applicableDamageModifier: 0.10d,
                clashDamageReduction: 0.25d);

            DamageFormulaResult result = DamageCalculator.CalculateOrdinaryComponent(
                input,
                Configuration);

            Assert.That(result.UnroundedDamage, Is.EqualTo(17.678571d).Within(0.000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(18));
        }

        [Test]
        public void CalculateOrdinaryComponent_NegativeMultiplierBuckets_ClampToZero()
        {
            var input = new DamageFormulaInput(
                actionMultiplier: 0.70d,
                weaponAttack: 50,
                attackModifier: -2d,
                baseDefense: 50,
                defenseModifier: -2d,
                applicableDamageModifier: 0d,
                clashDamageReduction: 0d);

            DamageFormulaResult result = DamageCalculator.CalculateOrdinaryComponent(
                input,
                Configuration);

            Assert.That(result.UnroundedDamage, Is.Zero);
            Assert.That(result.FinalDamage, Is.Zero);
        }

        [Test]
        public void CalculateOrdinaryComponent_ClashMultiplier_ClampsBetweenZeroAndOne()
        {
            DamageFormulaInput baseline = Input(actionMultiplier: 0.70d);
            var completeReduction = new DamageFormulaInput(
                baseline.ActionMultiplier,
                baseline.WeaponAttack,
                baseline.AttackModifier,
                baseline.BaseDefense,
                baseline.DefenseModifier,
                baseline.ApplicableDamageModifier,
                clashDamageReduction: 1.5d);
            var negativeReduction = new DamageFormulaInput(
                baseline.ActionMultiplier,
                baseline.WeaponAttack,
                baseline.AttackModifier,
                baseline.BaseDefense,
                baseline.DefenseModifier,
                baseline.ApplicableDamageModifier,
                clashDamageReduction: -0.5d);

            DamageFormulaResult completeResult = DamageCalculator.CalculateOrdinaryComponent(
                completeReduction,
                Configuration);
            DamageFormulaResult negativeResult = DamageCalculator.CalculateOrdinaryComponent(
                negativeReduction,
                Configuration);

            Assert.That(completeResult.FinalDamage, Is.Zero);
            Assert.That(negativeResult.FinalDamage, Is.EqualTo(23));
        }

        [Test]
        public void CalculateOrdinaryComponent_ClashAtMathematicalHalf_RoundsAwayFromZero()
        {
            var input = new DamageFormulaInput(
                actionMultiplier: 0.50d,
                weaponAttack: 50,
                attackModifier: 0d,
                baseDefense: 50,
                defenseModifier: 0d,
                applicableDamageModifier: 0d,
                clashDamageReduction: 0.25d);

            DamageFormulaResult result = DamageCalculator.CalculateOrdinaryComponent(
                input,
                Configuration);

            Assert.That(result.UnroundedDamage, Is.EqualTo(12.5d).Within(0.0000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(13));
        }

        [Test]
        public void CalculateOrdinaryComponent_RoundsHalfAwayFromZeroOnlyAtEnd()
        {
            var input = new DamageFormulaInput(
                actionMultiplier: 0.015d,
                weaponAttack: 50,
                attackModifier: 0d,
                baseDefense: 50,
                defenseModifier: 0d,
                applicableDamageModifier: 0d,
                clashDamageReduction: 0d);

            DamageFormulaResult result = DamageCalculator.CalculateOrdinaryComponent(
                input,
                Configuration);

            Assert.That(result.UnroundedDamage, Is.EqualTo(0.5d).Within(0.0000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(1));
        }

        [Test]
        public void Configuration_RejectsInvalidDefenseConstant()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleDamageConfiguration(0d));
        }

        [Test]
        public void CalculateEffect_AppliesOnlyMatchingModifiersToEachComponent()
        {
            var effect = new DamageEffectDefinition(new[]
            {
                new DamageComponentDefinition(DamageType.Slashing, 0.30d),
                new DamageComponentDefinition(DamageType.Fire, 0.20d)
            });
            var dealtModifiers = new[]
            {
                DamageModifier.All(0.10d),
                DamageModifier.ForFamily(DamageFamily.Physical, 0.20d),
                DamageModifier.ForType(DamageType.Fire, 0.30d)
            };
            var takenModifiers = new[]
            {
                DamageModifier.ForType(DamageType.Slashing, -0.10d)
            };

            DamageEffectResult result = DamageCalculator.CalculateEffect(
                effect,
                weaponAttack: 50,
                attackModifier: 0d,
                baseDefense: 50,
                defenseModifier: 0d,
                damageDealtModifiers: dealtModifiers,
                damageTakenModifiers: takenModifiers,
                clashDamageReduction: 0d,
                configuration: Configuration);

            Assert.That(result.Components[0].ApplicableDamageModifier,
                Is.EqualTo(0.20d).Within(0.000001d));
            Assert.That(result.Components[1].ApplicableDamageModifier,
                Is.EqualTo(0.40d).Within(0.000001d));
            Assert.That(result.UnroundedDamage, Is.EqualTo(21.333333d).Within(0.000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(21));
        }

        [Test]
        public void CalculateEffect_RoundsOnlyAfterAddingAllComponents()
        {
            var effect = new DamageEffectDefinition(new[]
            {
                new DamageComponentDefinition(DamageType.Slashing, 0.01d),
                new DamageComponentDefinition(DamageType.Fire, 0.01d)
            });

            DamageEffectResult result = DamageCalculator.CalculateEffect(
                effect,
                weaponAttack: 50,
                attackModifier: 0d,
                baseDefense: 50,
                defenseModifier: 0d,
                damageDealtModifiers: null,
                damageTakenModifiers: null,
                clashDamageReduction: 0d,
                configuration: Configuration);

            Assert.That(result.Components[0].UnroundedDamage,
                Is.EqualTo(0.333333d).Within(0.000001d));
            Assert.That(result.Components[1].UnroundedDamage,
                Is.EqualTo(0.333333d).Within(0.000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(1));
        }

        private static DamageFormulaInput Input(double actionMultiplier)
        {
            return new DamageFormulaInput(
                actionMultiplier,
                weaponAttack: 50,
                attackModifier: 0d,
                baseDefense: 50,
                defenseModifier: 0d,
                applicableDamageModifier: 0d,
                clashDamageReduction: 0d);
        }
    }
}
