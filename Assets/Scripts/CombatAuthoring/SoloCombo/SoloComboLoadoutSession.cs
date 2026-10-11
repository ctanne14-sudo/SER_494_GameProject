using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.SoloCombo
{
    [CreateAssetMenu(
        fileName = "SoloComboLoadoutSession",
        menuName = "SER494/Solo Combo/Runtime Loadout Session")]
    public sealed class SoloComboLoadoutSession : ScriptableObject
    {
        [NonSerialized]
        private ComboConfiguration[] _preparedCombos;

        public bool HasPreparedLoadout => _preparedCombos != null && _preparedCombos.Length > 0;

        public void Prepare(IReadOnlyList<ComboConfiguration> combos)
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
        }

        public bool TryGetPreparedLoadout(out IReadOnlyList<ComboConfiguration> combos)
        {
            combos = _preparedCombos;
            return HasPreparedLoadout;
        }

        public void Clear()
        {
            _preparedCombos = null;
        }
    }
}
