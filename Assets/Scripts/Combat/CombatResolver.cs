using System;

namespace SER494.Gameplay.Combat
{
    public static class CombatResolver
    {
        public static CombatActionResult Execute(
            CombatantState actor,
            CombatantState target,
            CombatAction action)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            CombatActionFailure failure = GetFailure(actor, target, action);
            if (failure != CombatActionFailure.None)
            {
                return new CombatActionResult(failure, 0);
            }

            actor.Pay(action.ManaCost, action.StaminaCost);

            int damage = Math.Max(1, actor.Stats.Attack + action.Power - target.Stats.Defense);
            target.ReceiveDamage(damage);

            return new CombatActionResult(CombatActionFailure.None, damage);
        }

        private static CombatActionFailure GetFailure(
            CombatantState actor,
            CombatantState target,
            CombatAction action)
        {
            if (actor.IsDefeated)
            {
                return CombatActionFailure.ActorDefeated;
            }

            if (target.IsDefeated)
            {
                return CombatActionFailure.TargetDefeated;
            }

            if (actor.CurrentMana < action.ManaCost)
            {
                return CombatActionFailure.InsufficientMana;
            }

            if (actor.CurrentStamina < action.StaminaCost)
            {
                return CombatActionFailure.InsufficientStamina;
            }

            return CombatActionFailure.None;
        }
    }
}
