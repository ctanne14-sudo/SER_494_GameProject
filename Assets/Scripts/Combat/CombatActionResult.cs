namespace SER494.Gameplay.Combat
{
    public enum CombatActionFailure
    {
        None,
        ActorDefeated,
        TargetDefeated,
        InsufficientMana,
        InsufficientStamina
    }

    public readonly struct CombatActionResult
    {
        public CombatActionResult(CombatActionFailure failure, int damageDealt)
        {
            Failure = failure;
            DamageDealt = damageDealt;
        }

        public CombatActionFailure Failure { get; }

        public int DamageDealt { get; }

        public bool Succeeded => Failure == CombatActionFailure.None;
    }
}
