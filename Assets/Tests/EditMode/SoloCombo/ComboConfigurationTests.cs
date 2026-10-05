using System.Linq;
using NUnit.Framework;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    public sealed class ComboConfigurationTests
    {
        [Test]
        public void CreateEmpty_UsesPlainDefaultName()
        {
            ComboConfiguration combo = ComboConfiguration.CreateEmpty(2);

            Assert.That(combo.Name, Is.EqualTo("combo2"));
            Assert.That(combo.MainAction, Is.Null);
        }

        [Test]
        public void Rename_UsesPlayerProvidedName()
        {
            ComboConfiguration combo = ComboConfiguration.CreateEmpty(1);

            combo.Rename("Ashen Reversal");

            Assert.That(combo.Name, Is.EqualTo("Ashen Reversal"));
        }

        [Test]
        public void Totals_IncludeEveryAssignedAction()
        {
            ComboConfiguration combo = CreateCompleteCombo(LinkedActionPlacement.AfterMain);

            Assert.That(combo.TotalLoadCost, Is.EqualTo(10));
            Assert.That(combo.TotalManaCost, Is.EqualTo(6));
            Assert.That(combo.TotalStaminaCost, Is.EqualTo(8));
            Assert.That(combo.TotalCooldown, Is.EqualTo(10));
        }

        [TestCase(LinkedActionPlacement.AfterOpening, "opening,linked,main,closing")]
        [TestCase(LinkedActionPlacement.AfterMain, "opening,main,linked,closing")]
        [TestCase(LinkedActionPlacement.AfterClosing, "opening,main,closing,linked")]
        public void BuildExecutionOrder_InsertsLinkedActionAfterSelectedAction(
            LinkedActionPlacement placement,
            string expectedOrder)
        {
            ComboConfiguration combo = CreateCompleteCombo(placement);

            string actualOrder = string.Join(
                ",",
                combo.BuildExecutionOrder().Select(action => action.ActionId));

            Assert.That(actualOrder, Is.EqualTo(expectedOrder));
        }

        private static ComboConfiguration CreateCompleteCombo(LinkedActionPlacement placement)
        {
            return new ComboConfiguration(
                "combo1",
                Action("opening", ComboActionCategory.Opening, 1, 2, 1, 1),
                Action("main", ComboActionCategory.Main, 4, 3, 4, 4),
                Action("closing", ComboActionCategory.Closing, 3, 1, 2, 3),
                Action("linked", ComboActionCategory.Linked, 2, 0, 1, 2),
                placement);
        }

        private static ComboActionDefinition Action(
            string id,
            ComboActionCategory category,
            int load,
            int mana,
            int stamina,
            int cooldown)
        {
            return new ComboActionDefinition(id, id, category, load, mana, stamina, cooldown);
        }
    }
}
