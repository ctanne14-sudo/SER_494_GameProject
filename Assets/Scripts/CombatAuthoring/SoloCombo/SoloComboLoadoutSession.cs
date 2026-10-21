using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.SoloCombo
{
    public enum SoloComboTestScenario
    {
        GreatswordTrial,
        SwordAndShieldTrial,
        GlaiveAndShieldTrial
    }

    public static class SoloComboTestScenarioInfo
    {
        public static string DisplayName(SoloComboTestScenario scenario)
        {
            switch (scenario)
            {
                case SoloComboTestScenario.SwordAndShieldTrial:
                    return "Sword & Shield Trial";
                case SoloComboTestScenario.GlaiveAndShieldTrial:
                    return "Glaive & Shield Trial";
                default:
                    return "Greatsword Trial";
            }
        }

        public static string Description(SoloComboTestScenario scenario)
        {
            switch (scenario)
            {
                case SoloComboTestScenario.SwordAndShieldTrial:
                    return "Four scripted rounds for Guard, Evade, airborne setup, multi-hit, and split healing. Stamina refills between rounds.";
                case SoloComboTestScenario.GlaiveAndShieldTrial:
                    return "Four scripted rounds for Clash, Guard windows, defense, Buffs, and split recovery. Stamina refills between rounds.";
                default:
                    return "Four scripted rounds for Charge, Clash, Super Armor, Buffs, and split damage. Stamina refills between rounds.";
            }
        }
    }

    [CreateAssetMenu(
        fileName = "SoloComboLoadoutSession",
        menuName = "SER494/Solo Combo/Runtime Loadout Session")]
    public sealed class SoloComboLoadoutSession : ScriptableObject
    {
        [NonSerialized]
        private ComboConfiguration[] _preparedCombos;

        [NonSerialized]
        private WeaponDefinition _equippedWeapon;

        [NonSerialized]
        private SoloComboTestScenario _selectedTestScenario =
            SoloComboTestScenario.GreatswordTrial;

        public bool HasPreparedLoadout => _preparedCombos != null && _preparedCombos.Length > 0;

        public WeaponDefinition EquippedWeapon => _equippedWeapon;

        public SoloComboTestScenario SelectedTestScenario => _selectedTestScenario;

        public void Prepare(IReadOnlyList<ComboConfiguration> combos)
        {
            Prepare(combos, equippedWeapon: null);
        }

        public void Prepare(
            IReadOnlyList<ComboConfiguration> combos,
            WeaponDefinition equippedWeapon)
        {
            Prepare(
                combos,
                equippedWeapon,
                SoloComboTestScenario.GreatswordTrial);
        }

        public void Prepare(
            IReadOnlyList<ComboConfiguration> combos,
            WeaponDefinition equippedWeapon,
            SoloComboTestScenario selectedTestScenario)
        {
            if (combos == null)
            {
                throw new ArgumentNullException(nameof(combos));
            }

            if (combos.Count == 0)
            {
                throw new ArgumentException("A prepared loadout needs at least one Combo.", nameof(combos));
            }

            var copy = new ComboConfiguration[combos.Count];
            for (int index = 0; index < combos.Count; index++)
            {
                copy[index] = combos[index]
                    ?? throw new ArgumentException("A prepared loadout cannot contain a null Combo.", nameof(combos));
            }

            _preparedCombos = copy;
            _equippedWeapon = equippedWeapon;
            _selectedTestScenario = selectedTestScenario;
        }

        public bool TryGetPreparedLoadout(out IReadOnlyList<ComboConfiguration> combos)
        {
            combos = _preparedCombos;
            return HasPreparedLoadout;
        }

        public void Clear()
        {
            _preparedCombos = null;
            _equippedWeapon = null;
            _selectedTestScenario = SoloComboTestScenario.GreatswordTrial;
        }
    }
}
