using System.Collections.Generic;
using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Weapons;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    [Category("CoreCombat")]
    [Category("Authoring")]
    public sealed class PlayerEquipmentCatalogAssetTests
    {
        [Test]
        public void DefaultCatalog_ResolvesStartingWeaponAndArmorStats()
        {
            PlayerEquipmentCatalogAsset catalog = PlayerEquipmentCatalogAsset.LoadDefault();

            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.TryGetWeapon(
                    "weapon.greatsword.rusted-iron-sword",
                    out WeaponDefinition weapon,
                    out string weaponError),
                Is.True,
                weaponError);
            Assert.That(weapon.DisplayName, Is.EqualTo("鏽蝕鐵劍"));
            Assert.That(weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword));
            Assert.That(weapon.WeaponAttack, Is.EqualTo(50));

            var equippedArmor = new Dictionary<string, string>
            {
                ["armor.head"] = "armor.linen-headpiece",
                ["armor.upper-body"] = "armor.linen-shirt",
                ["armor.lower-body"] = "armor.linen-hose"
            };
            Assert.That(
                catalog.TryGetTotalDefense(
                    equippedArmor,
                    out int totalDefense,
                    out string armorError),
                Is.True,
                armorError);
            Assert.That(totalDefense, Is.EqualTo(10));
        }

        [Test]
        public void DefaultCatalog_RejectsArmorEquippedInWrongSlot()
        {
            PlayerEquipmentCatalogAsset catalog = PlayerEquipmentCatalogAsset.LoadDefault();
            var equippedArmor = new Dictionary<string, string>
            {
                ["armor.head"] = "armor.linen-shirt"
            };

            bool succeeded = catalog.TryGetTotalDefense(
                equippedArmor,
                out _,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(validationError, Does.Contain("not 'armor.head'"));
        }

        [Test]
        public void DefaultCatalog_ContainsMenuTestEquipment()
        {
            PlayerEquipmentCatalogAsset catalog = PlayerEquipmentCatalogAsset.LoadDefault();

            Assert.That(
                catalog.TryGetWeapon(
                    "weapon.greatsword.cast-iron-greatsword",
                    out WeaponDefinition weapon,
                    out string weaponError),
                Is.True,
                weaponError);
            Assert.That(weapon.WeaponType, Is.EqualTo(WeaponType.Greatsword));
            Assert.That(weapon.WeaponAttack, Is.EqualTo(60));

            Assert.That(
                catalog.TryGetAccessory(
                    "accessory.rust-ring",
                    out AccessoryDefinitionAsset rustRing,
                    out string rustError),
                Is.True,
                rustError);
            Assert.That(rustRing.EffectType, Is.EqualTo(AccessoryEffectType.MaximumHealthFlat));
            Assert.That(rustRing.EffectAmount, Is.EqualTo(20f));

            Assert.That(
                catalog.TryGetAccessory(
                    "accessory.ruby-ring",
                    out AccessoryDefinitionAsset rubyRing,
                    out string rubyError),
                Is.True,
                rubyError);
            Assert.That(
                rubyRing.EffectType,
                Is.EqualTo(AccessoryEffectType.AllDamageDealtMultiplier));
            Assert.That(rubyRing.EffectAmount, Is.EqualTo(0.05f).Within(0.0001f));
        }
    }
}
