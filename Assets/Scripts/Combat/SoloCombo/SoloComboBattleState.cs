using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public enum SoloComboExecutionFailure
    {
        None,
        BattleEnded,
        ComboIndexOutOfRange,
        ComboOnCooldown,
        InsufficientMana,
        InsufficientStamina,
        WaitNotAvailable
    }

    public enum DamageAvoidanceResult
    {
        None,
        Guarded,
        Evaded
    }

    public readonly struct SoloComboActionResolution
    {
        public SoloComboActionResolution(
            TimelineActorSide side,
            string actionId,
            string displayName,
            int timeStep,
            int damageDealt,
            ActionResolutionEffect resolutionEffect,
            DamageAvoidanceResult damageAvoidance,
            DamageResolutionTag damageTags,
            string statusDisplayName,
            ActionStatusTarget statusTarget,
            bool statusApplied)
        {
            Side = side;
            ActionId = actionId ?? throw new ArgumentNullException(nameof(actionId));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            TimeStep = timeStep;
            DamageDealt = damageDealt;
            ResolutionEffect = resolutionEffect;
            DamageAvoidance = damageAvoidance;
            DamageTags = damageTags;
            StatusDisplayName = statusDisplayName;
            StatusTarget = statusTarget;
            StatusApplied = statusApplied;
        }

        public TimelineActorSide Side { get; }

        public string ActionId { get; }

        public string DisplayName { get; }

        public int TimeStep { get; }

        public int DamageDealt { get; }

        public ActionResolutionEffect ResolutionEffect { get; }

        public DamageAvoidanceResult DamageAvoidance { get; }

        public DamageResolutionTag DamageTags { get; }

        public string StatusDisplayName { get; }

        public ActionStatusTarget StatusTarget { get; }

        public bool StatusApplied { get; }
    }

    public sealed class SoloComboTurnResult
    {
        private static readonly IReadOnlyList<SoloComboActionResolution> NoActions =
            Array.Empty<SoloComboActionResolution>();

        private SoloComboTurnResult(
            SoloComboExecutionFailure failure,
            IReadOnlyList<SoloComboActionResolution> actionResolutions,
            InitialTimelineSchedule schedule,
            int manaRecovered,
            int staminaRecovered,
            BattleOutcome outcome)
        {
            Failure = failure;
            ActionResolutions = actionResolutions;
            Schedule = schedule;
            ManaRecovered = manaRecovered;
            StaminaRecovered = staminaRecovered;
            Outcome = outcome;
        }

        public SoloComboExecutionFailure Failure { get; }

        public IReadOnlyList<SoloComboActionResolution> ActionResolutions { get; }

        public InitialTimelineSchedule Schedule { get; }

        public int ManaRecovered { get; }

        public int StaminaRecovered { get; }

        public BattleOutcome Outcome { get; }

        public bool Succeeded => Failure == SoloComboExecutionFailure.None;

        public int TotalDamageDealt => SumDamage(TimelineActorSide.Player);

        public int EnemyDamageDealt => SumDamage(TimelineActorSide.Enemy);

        internal static SoloComboTurnResult Failed(
            SoloComboExecutionFailure failure,
            BattleOutcome outcome)
        {
            return new SoloComboTurnResult(failure, NoActions, null, 0, 0, outcome);
        }

        internal static SoloComboTurnResult Completed(
            IReadOnlyList<SoloComboActionResolution> actionResolutions,
            InitialTimelineSchedule schedule,
            int manaRecovered,
            int staminaRecovered,
            BattleOutcome outcome)
        {
            return new SoloComboTurnResult(
                SoloComboExecutionFailure.None,
                actionResolutions,
                schedule,
                manaRecovered,
                staminaRecovered,
                outcome);
        }

        private int SumDamage(TimelineActorSide side)
        {
            int total = 0;
            for (int index = 0; index < ActionResolutions.Count; index++)
            {
                SoloComboActionResolution resolution = ActionResolutions[index];
                if (resolution.Side == side)
                {
                    total += resolution.DamageDealt;
                }
            }

            return total;
        }
    }

    public sealed class SoloComboBattleState
    {
        private readonly ComboConfiguration[] _combos;
        private readonly int[] _cooldowns;
        private readonly EnemyActionSequence _enemySequence;
        private readonly BattleDamageConfiguration _damageConfiguration;
        private readonly int _manaRecoveryPerTurn;
        private readonly int _staminaRecoveryPerTurn;

        public SoloComboBattleState(
            CombatantState player,
            CombatantState enemy,
            IReadOnlyList<ComboConfiguration> combos,
            EnemyActionSequence enemySequence,
            int manaRecoveryPerTurn,
            int staminaRecoveryPerTurn,
            BattleDamageConfiguration damageConfiguration = null)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            _enemySequence = enemySequence
                ?? throw new ArgumentNullException(nameof(enemySequence));
            _damageConfiguration = damageConfiguration
                ?? BattleDamageConfiguration.CreateMvpDefault();
            _manaRecoveryPerTurn = RequireNonNegative(
                manaRecoveryPerTurn,
                nameof(manaRecoveryPerTurn));
            _staminaRecoveryPerTurn = RequireNonNegative(
                staminaRecoveryPerTurn,
                nameof(staminaRecoveryPerTurn));
            _combos = CopyAndValidateCombos(combos);
            _cooldowns = new int[_combos.Length];
            EnemyIntent = InitialTimelineScheduleBuilder.BuildEnemyOnly(_enemySequence);
            Outcome = EvaluateOutcome();
        }

        public CombatantState Player { get; }

        public CombatantState Enemy { get; }

        public IReadOnlyList<ComboConfiguration> Combos => _combos;

        public EnemyActionSequence EnemySequence => _enemySequence;

        public InitialTimelineSchedule EnemyIntent { get; }

        public BattleOutcome Outcome { get; private set; }

        public bool CanWait
        {
            get
            {
                if (Outcome != BattleOutcome.Ongoing)
                {
                    return false;
                }

                for (int index = 0; index < _combos.Length; index++)
                {
                    if (GetExecutionFailure(index) == SoloComboExecutionFailure.None)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public int GetCooldownRemaining(int comboIndex)
        {
            ValidateComboIndex(comboIndex);
            return _cooldowns[comboIndex];
        }

        public SoloComboExecutionFailure GetExecutionFailure(int comboIndex)
        {
            if (comboIndex < 0 || comboIndex >= _combos.Length)
            {
                return SoloComboExecutionFailure.ComboIndexOutOfRange;
            }

            if (Outcome != BattleOutcome.Ongoing)
            {
                return SoloComboExecutionFailure.BattleEnded;
            }

            if (_cooldowns[comboIndex] > 0)
            {
                return SoloComboExecutionFailure.ComboOnCooldown;
            }

            ComboConfiguration combo = _combos[comboIndex];
            if (Player.CurrentMana < combo.TotalManaCost)
            {
                return SoloComboExecutionFailure.InsufficientMana;
            }

            return Player.CurrentStamina < combo.TotalStaminaCost
                ? SoloComboExecutionFailure.InsufficientStamina
                : SoloComboExecutionFailure.None;
        }

        public SoloComboTurnResult ExecuteCombo(int comboIndex)
        {
            SoloComboExecutionFailure failure = GetExecutionFailure(comboIndex);
            if (failure != SoloComboExecutionFailure.None)
            {
                return SoloComboTurnResult.Failed(failure, Outcome);
            }

            ComboConfiguration combo = _combos[comboIndex];
            Player.Pay(combo.TotalManaCost, combo.TotalStaminaCost);
            AdvanceExistingCooldowns();
            _cooldowns[comboIndex] = combo.TotalCooldown;

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.Build(
                combo,
                _enemySequence);
            IReadOnlyList<SoloComboActionResolution> resolutions = ResolveTimeline(schedule);

            Outcome = EvaluateOutcome();
            RecoverAfterRound(out int manaRecovered, out int staminaRecovered);
            AdvanceRoundStatuses();

            return SoloComboTurnResult.Completed(
                resolutions,
                schedule,
                manaRecovered,
                staminaRecovered,
                Outcome);
        }

        public SoloComboTurnResult WaitTurn()
        {
            if (Outcome != BattleOutcome.Ongoing)
            {
                return SoloComboTurnResult.Failed(
                    SoloComboExecutionFailure.BattleEnded,
                    Outcome);
            }

            if (!CanWait)
            {
                return SoloComboTurnResult.Failed(
                    SoloComboExecutionFailure.WaitNotAvailable,
                    Outcome);
            }

            AdvanceExistingCooldowns();
            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.BuildEnemyOnly(
                _enemySequence);
            IReadOnlyList<SoloComboActionResolution> resolutions = ResolveTimeline(schedule);

            Outcome = EvaluateOutcome();
            RecoverAfterRound(out int manaRecovered, out int staminaRecovered);
            AdvanceRoundStatuses();

            return SoloComboTurnResult.Completed(
                resolutions,
                schedule,
                manaRecovered,
                staminaRecovered,
                Outcome);
        }

        private IReadOnlyList<SoloComboActionResolution> ResolveTimeline(
            InitialTimelineSchedule schedule)
        {
            var resolutions = new List<SoloComboActionResolution>(schedule.Actions.Count);

            for (int stepIndex = 0; stepIndex < schedule.ResolutionSteps.Count; stepIndex++)
            {
                TimelineResolutionStep step = schedule.ResolutionSteps[stepIndex];
                int pendingPlayerDamage = 0;
                int pendingEnemyDamage = 0;
                var pendingStatuses = new List<ScheduledTimelineAction>();
                ActionResolutionEffect playerDefense = FindDefensiveResolution(
                    step,
                    TimelineActorSide.Player);
                ActionResolutionEffect enemyDefense = FindDefensiveResolution(
                    step,
                    TimelineActorSide.Enemy);

                for (int actionIndex = 0; actionIndex < step.Actions.Count; actionIndex++)
                {
                    ScheduledTimelineAction action = step.Actions[actionIndex];
                    CombatantState actor = action.Side == TimelineActorSide.Player
                        ? Player
                        : Enemy;
                    CombatantState target = action.Side == TimelineActorSide.Player
                        ? Enemy
                        : Player;

                    // Defeat at an earlier time step prevents later effects. Effects collected
                    // at this same step still resolve together.
                    if (actor.IsDefeated || target.IsDefeated)
                    {
                        continue;
                    }

                    if (action.StatusEffect != null)
                    {
                        pendingStatuses.Add(action);
                        continue;
                    }

                    ActionResolutionEffect targetDefense = action.Side == TimelineActorSide.Player
                        ? enemyDefense
                        : playerDefense;
                    DamageAvoidanceResult avoidance = action.HasDamage
                        ? ResolveDamageAvoidance(targetDefense, action.DamageTags)
                        : DamageAvoidanceResult.None;
                    int damage = action.HasDamage
                        && avoidance == DamageAvoidanceResult.None
                            ? CalculateDamage(actor, target, action.DamageEffect)
                            : 0;
                    resolutions.Add(new SoloComboActionResolution(
                        action.Side,
                        action.ActionId,
                        action.DisplayName,
                        step.TimeStep,
                        damage,
                        action.ResolutionEffect,
                        avoidance,
                        action.DamageTags,
                        statusDisplayName: null,
                        statusTarget: ActionStatusTarget.Self,
                        statusApplied: false));

                    if (action.Side == TimelineActorSide.Player)
                    {
                        pendingEnemyDamage = checked(pendingEnemyDamage + damage);
                    }
                    else
                    {
                        pendingPlayerDamage = checked(pendingPlayerDamage + damage);
                    }
                }

                Enemy.ReceiveDamage(pendingEnemyDamage);
                Player.ReceiveDamage(pendingPlayerDamage);

                for (int statusIndex = 0; statusIndex < pendingStatuses.Count; statusIndex++)
                {
                    ScheduledTimelineAction action = pendingStatuses[statusIndex];
                    CombatantState actor = action.Side == TimelineActorSide.Player
                        ? Player
                        : Enemy;
                    CombatantState opponent = action.Side == TimelineActorSide.Player
                        ? Enemy
                        : Player;
                    CombatantState recipient = action.StatusTarget == ActionStatusTarget.Self
                        ? actor
                        : opponent;
                    bool applied = !actor.IsDefeated && !recipient.IsDefeated;
                    if (applied)
                    {
                        recipient.ApplyStatus(action.StatusEffect);
                    }

                    resolutions.Add(new SoloComboActionResolution(
                        action.Side,
                        action.ActionId,
                        action.DisplayName,
                        step.TimeStep,
                        damageDealt: 0,
                        action.ResolutionEffect,
                        DamageAvoidanceResult.None,
                        DamageResolutionTag.None,
                        action.StatusEffect.DisplayName,
                        action.StatusTarget,
                        applied));
                }

                if (EvaluateOutcome() != BattleOutcome.Ongoing)
                {
                    break;
                }
            }

            return Array.AsReadOnly(resolutions.ToArray());
        }

        private static ActionResolutionEffect FindDefensiveResolution(
            TimelineResolutionStep step,
            TimelineActorSide side)
        {
            ActionResolutionEffect defense = ActionResolutionEffect.None;

            for (int index = 0; index < step.Actions.Count; index++)
            {
                ScheduledTimelineAction action = step.Actions[index];
                if (action.Side != side
                    || (action.ResolutionEffect != ActionResolutionEffect.Guard
                        && action.ResolutionEffect != ActionResolutionEffect.Evade))
                {
                    continue;
                }

                if (defense != ActionResolutionEffect.None)
                {
                    throw new InvalidOperationException(
                        "A character cannot resolve more than one Guard or Evade point at the same Time Step.");
                }

                defense = action.ResolutionEffect;
            }

            return defense;
        }

        private static DamageAvoidanceResult ResolveDamageAvoidance(
            ActionResolutionEffect defensiveEffect,
            DamageResolutionTag damageTags)
        {
            switch (defensiveEffect)
            {
                case ActionResolutionEffect.Guard:
                    return (damageTags & (DamageResolutionTag.Unguardable
                            | DamageResolutionTag.Overcommit)) == 0
                        ? DamageAvoidanceResult.Guarded
                        : DamageAvoidanceResult.None;
                case ActionResolutionEffect.Evade:
                    return (damageTags & DamageResolutionTag.Area) == 0
                        ? DamageAvoidanceResult.Evaded
                        : DamageAvoidanceResult.None;
                default:
                    return DamageAvoidanceResult.None;
            }
        }

        private int CalculateDamage(
            CombatantState attacker,
            CombatantState defender,
            DamageEffectDefinition damageEffect)
        {
            return DamageCalculator.CalculateEffect(
                damageEffect,
                weaponAttack: attacker.Stats.Attack,
                attackModifier: 0d,
                baseDefense: defender.Stats.Defense,
                defenseModifier: 0d,
                damageDealtModifiers: attacker.DamageDealtModifiers,
                damageTakenModifiers: defender.DamageTakenModifiers,
                clashDamageReduction: 0d,
                configuration: _damageConfiguration).FinalDamage;
        }

        private void AdvanceExistingCooldowns()
        {
            for (int index = 0; index < _cooldowns.Length; index++)
            {
                if (_cooldowns[index] > 0)
                {
                    _cooldowns[index]--;
                }
            }
        }

        private void RecoverAfterRound(out int manaRecovered, out int staminaRecovered)
        {
            manaRecovered = 0;
            staminaRecovered = 0;

            if (Outcome != BattleOutcome.Ongoing)
            {
                return;
            }

            int manaBeforeRecovery = Player.CurrentMana;
            int staminaBeforeRecovery = Player.CurrentStamina;
            Player.RestoreMana(_manaRecoveryPerTurn);
            Player.RestoreStamina(_staminaRecoveryPerTurn);
            manaRecovered = Player.CurrentMana - manaBeforeRecovery;
            staminaRecovered = Player.CurrentStamina - staminaBeforeRecovery;
        }

        private void AdvanceRoundStatuses()
        {
            Player.AdvanceRoundStatuses();
            Enemy.AdvanceRoundStatuses();
        }

        private BattleOutcome EvaluateOutcome()
        {
            return BattleEvaluator.Evaluate(new[] { Player }, new[] { Enemy });
        }

        private void ValidateComboIndex(int comboIndex)
        {
            if (comboIndex < 0 || comboIndex >= _combos.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(comboIndex));
            }
        }

        private static ComboConfiguration[] CopyAndValidateCombos(
            IReadOnlyList<ComboConfiguration> combos)
        {
            if (combos == null)
            {
                throw new ArgumentNullException(nameof(combos));
            }

            if (combos.Count == 0)
            {
                throw new ArgumentException("A Solo Combo battle needs at least one Combo.", nameof(combos));
            }

            var copy = new ComboConfiguration[combos.Count];
            for (int index = 0; index < combos.Count; index++)
            {
                ComboConfiguration combo = combos[index];
                if (combo == null || combo.MainAction == null)
                {
                    throw new ArgumentException(
                        "Every battle Combo must exist and contain a Main Action.",
                        nameof(combos));
                }

                copy[index] = combo;
            }

            return copy;
        }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }
    }
}
