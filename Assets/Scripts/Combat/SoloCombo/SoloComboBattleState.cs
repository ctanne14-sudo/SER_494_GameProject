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
        WaitNotAvailable,
        TimelineResolving
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
            bool statusApplied,
            double defensiveDamageReduction = 0d,
            ActionDisruptionDefinition appliedDisruption = null,
            ActionDisruptionResponse disruptionResponse = ActionDisruptionResponse.Continue,
            TimelineActorSide disruptedSide = TimelineActorSide.Player,
            int disruptedSequenceIndex = -1)
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
            DefensiveDamageReduction = defensiveDamageReduction;
            AppliedDisruption = appliedDisruption;
            DisruptionResponse = disruptionResponse;
            DisruptedSide = disruptedSide;
            DisruptedSequenceIndex = disruptedSequenceIndex;
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

        public double DefensiveDamageReduction { get; }

        public ActionDisruptionDefinition AppliedDisruption { get; }

        public ActionDisruptionResponse DisruptionResponse { get; }

        public TimelineActorSide DisruptedSide { get; }

        public int DisruptedSequenceIndex { get; }
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

        public int LastResolvedTimeStep
        {
            get
            {
                int completionTime = 0;
                for (int index = 0; index < ActionResolutions.Count; index++)
                {
                    completionTime = Math.Max(
                        completionTime,
                        ActionResolutions[index].TimeStep);
                }

                return completionTime;
            }
        }

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

    public sealed class SoloComboTimelineAdvanceResult
    {
        internal SoloComboTimelineAdvanceResult(
            int timeStep,
            IReadOnlyList<SoloComboActionResolution> actionResolutions,
            SoloComboTurnResult completedTurn,
            SoloComboTimingRequest timingRequest = null,
            SoloComboChargeRequest chargeRequest = null)
        {
            TimeStep = timeStep;
            ActionResolutions = actionResolutions
                ?? throw new ArgumentNullException(nameof(actionResolutions));
            CompletedTurn = completedTurn;
            TimingRequest = timingRequest;
            ChargeRequest = chargeRequest;
        }

        public int TimeStep { get; }

        public IReadOnlyList<SoloComboActionResolution> ActionResolutions { get; }

        public SoloComboTurnResult CompletedTurn { get; }

        public SoloComboTimingRequest TimingRequest { get; }

        public bool RoundCompleted => CompletedTurn != null;

        public bool RequiresTimingInput => TimingRequest != null;

        public SoloComboChargeRequest ChargeRequest { get; }

        public bool RequiresChargeInput => ChargeRequest != null;
    }

    public sealed class SoloComboChargeRequest
    {
        internal SoloComboChargeRequest(ScheduledTimelineAction action)
        {
            Side = action.Side;
            SequenceIndex = action.SequenceIndex;
            ActionId = action.ActionId;
            DisplayName = action.DisplayName;
            ChargeLevels = action.ChargeLevels;
        }

        public TimelineActorSide Side { get; }

        public int SequenceIndex { get; }

        public string ActionId { get; }

        public string DisplayName { get; }

        public IReadOnlyList<ComboActionChargeLevelDefinition> ChargeLevels { get; }

        public int MaximumChargeLevel => ChargeLevels.Count;
    }

    public sealed class SoloComboTimingRequest
    {
        internal SoloComboTimingRequest(ScheduledTimelineAction action)
        {
            Side = action.Side;
            SequenceIndex = action.SequenceIndex;
            ActionId = action.ActionId;
            DisplayName = action.DisplayName;
            MinimumTimeStep = action.ResolutionWindowStartTime.Value;
            MaximumTimeStep = action.ResolutionWindowEndTime.Value;
        }

        public TimelineActorSide Side { get; }

        public int SequenceIndex { get; }

        public string ActionId { get; }

        public string DisplayName { get; }

        public int MinimumTimeStep { get; }

        public int MaximumTimeStep { get; }
    }

    public sealed class SoloComboBattleState
    {
        private static readonly IReadOnlyList<SoloComboActionResolution> NoResolutions =
            Array.Empty<SoloComboActionResolution>();

        private readonly ComboConfiguration[] _combos;
        private readonly int[] _cooldowns;
        private readonly EnemyBehaviorTable _enemyBehavior;
        private readonly Random _enemyRandom;
        private readonly BattleDamageConfiguration _damageConfiguration;
        private readonly int _manaRecoveryPerTurn;
        private readonly int _staminaRecoveryPerTurn;
        private readonly List<SoloComboActionResolution> _activeResolutions =
            new List<SoloComboActionResolution>();

        private InitialTimelineSchedule _activeSchedule;
        private int _activeResolutionStepIndex;
        private int _currentTimelineTimeStep;
        private EnemyActionSequence _currentEnemySequence;
        private string _previousEnemySequenceId;
        private SoloComboTimingRequest _pendingTimingRequest;
        private SoloComboChargeRequest _pendingChargeRequest;

        public SoloComboBattleState(
            CombatantState player,
            CombatantState enemy,
            IReadOnlyList<ComboConfiguration> combos,
            EnemyActionSequence enemySequence,
            int manaRecoveryPerTurn,
            int staminaRecoveryPerTurn,
            BattleDamageConfiguration damageConfiguration = null)
            : this(
                player,
                enemy,
                combos,
                EnemyBehaviorTable.Always(enemySequence),
                manaRecoveryPerTurn,
                staminaRecoveryPerTurn,
                damageConfiguration,
                enemyRandom: null)
        {
        }

        public SoloComboBattleState(
            CombatantState player,
            CombatantState enemy,
            IReadOnlyList<ComboConfiguration> combos,
            EnemyBehaviorTable enemyBehavior,
            int manaRecoveryPerTurn,
            int staminaRecoveryPerTurn,
            BattleDamageConfiguration damageConfiguration = null,
            Random enemyRandom = null)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            _enemyBehavior = enemyBehavior
                ?? throw new ArgumentNullException(nameof(enemyBehavior));
            _enemyRandom = enemyRandom ?? new Random();
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
            Outcome = EvaluateOutcome();
            if (Outcome == BattleOutcome.Ongoing)
            {
                SelectAndLockEnemyIntent();
            }
        }

        public CombatantState Player { get; }

        public CombatantState Enemy { get; }

        public IReadOnlyList<ComboConfiguration> Combos => _combos;

        public EnemyActionSequence EnemySequence => _currentEnemySequence;

        public InitialTimelineSchedule EnemyIntent { get; private set; }

        public BattleOutcome Outcome { get; private set; }

        public bool IsTimelineResolving => _activeSchedule != null;

        public int CurrentTimelineTimeStep => _currentTimelineTimeStep;

        public int TimelineCompletionTime => _activeSchedule?.CompletionTime ?? 0;

        public InitialTimelineSchedule ActiveTimeline => _activeSchedule;

        public bool CanWait
        {
            get
            {
                if (Outcome != BattleOutcome.Ongoing || IsTimelineResolving)
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

            if (IsTimelineResolving)
            {
                return SoloComboExecutionFailure.TimelineResolving;
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

        public InitialTimelineSchedule PreviewCombo(int comboIndex)
        {
            SoloComboExecutionFailure failure = GetExecutionFailure(comboIndex);
            if (failure != SoloComboExecutionFailure.None)
            {
                throw new InvalidOperationException(
                    $"Combo {comboIndex} cannot be previewed: {failure}.");
            }

            return InitialTimelineScheduleBuilder.Build(
                _combos[comboIndex],
                _currentEnemySequence);
        }

        public SoloComboTurnResult ExecuteCombo(int comboIndex)
        {
            SoloComboExecutionFailure failure = BeginCombo(comboIndex);
            if (failure != SoloComboExecutionFailure.None)
            {
                return SoloComboTurnResult.Failed(failure, Outcome);
            }

            return ResolveActiveTimelineToCompletion();
        }

        public SoloComboExecutionFailure BeginCombo(int comboIndex)
        {
            SoloComboExecutionFailure failure = GetExecutionFailure(comboIndex);
            if (failure != SoloComboExecutionFailure.None)
            {
                return failure;
            }

            ComboConfiguration combo = _combos[comboIndex];
            Player.Pay(combo.TotalManaCost, combo.TotalStaminaCost);
            AdvanceExistingCooldowns();
            _cooldowns[comboIndex] = combo.TotalCooldown;

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.Build(
                combo,
                _currentEnemySequence);
            BeginTimeline(schedule);
            return SoloComboExecutionFailure.None;
        }

        public SoloComboTurnResult WaitTurn()
        {
            SoloComboExecutionFailure failure = BeginWaitTurn();
            if (failure != SoloComboExecutionFailure.None)
            {
                return SoloComboTurnResult.Failed(failure, Outcome);
            }

            return ResolveActiveTimelineToCompletion();
        }

        public SoloComboExecutionFailure BeginWaitTurn()
        {
            if (Outcome != BattleOutcome.Ongoing)
            {
                return SoloComboExecutionFailure.BattleEnded;
            }

            if (IsTimelineResolving)
            {
                return SoloComboExecutionFailure.TimelineResolving;
            }

            if (!CanWait)
            {
                return SoloComboExecutionFailure.WaitNotAvailable;
            }

            AdvanceExistingCooldowns();
            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.BuildEnemyOnly(
                _currentEnemySequence);
            BeginTimeline(schedule);
            return SoloComboExecutionFailure.None;
        }

        public SoloComboTimelineAdvanceResult AdvanceTimeline()
        {
            if (!IsTimelineResolving)
            {
                throw new InvalidOperationException("No Solo Combo timeline is currently resolving.");
            }

            if (_pendingTimingRequest != null)
            {
                return new SoloComboTimelineAdvanceResult(
                    _currentTimelineTimeStep,
                    NoResolutions,
                    completedTurn: null,
                    timingRequest: _pendingTimingRequest);
            }

            if (_pendingChargeRequest != null)
            {
                return new SoloComboTimelineAdvanceResult(
                    _currentTimelineTimeStep,
                    NoResolutions,
                    completedTurn: null,
                    chargeRequest: _pendingChargeRequest);
            }

            ScheduledTimelineAction pendingCharge = FindChargeStartingAt(
                _currentTimelineTimeStep + 1);
            if (pendingCharge != null)
            {
                _pendingChargeRequest = new SoloComboChargeRequest(pendingCharge);
                return new SoloComboTimelineAdvanceResult(
                    _currentTimelineTimeStep,
                    NoResolutions,
                    completedTurn: null,
                    chargeRequest: _pendingChargeRequest);
            }

            ScheduledTimelineAction pendingWindow = FindWindowStartingAt(
                _currentTimelineTimeStep + 1);
            if (pendingWindow != null)
            {
                _pendingTimingRequest = new SoloComboTimingRequest(pendingWindow);
                return new SoloComboTimelineAdvanceResult(
                    _currentTimelineTimeStep,
                    NoResolutions,
                    completedTurn: null,
                    timingRequest: _pendingTimingRequest);
            }

            _currentTimelineTimeStep++;
            IReadOnlyList<SoloComboActionResolution> stepResolutions = NoResolutions;

            if (_activeResolutionStepIndex < _activeSchedule.ResolutionSteps.Count)
            {
                TimelineResolutionStep nextStep =
                    _activeSchedule.ResolutionSteps[_activeResolutionStepIndex];
                if (nextStep.TimeStep == _currentTimelineTimeStep)
                {
                    stepResolutions = ResolveTimelineStep(nextStep);
                    _activeResolutions.AddRange(stepResolutions);
                    RecalculateActiveResolutionStepIndex();
                }
            }

            Outcome = EvaluateOutcome();

            SoloComboTurnResult completedTurn = null;
            if (Outcome != BattleOutcome.Ongoing
                || _currentTimelineTimeStep >= _activeSchedule.CompletionTime)
            {
                completedTurn = CompleteActiveRound();
            }

            return new SoloComboTimelineAdvanceResult(
                _currentTimelineTimeStep,
                stepResolutions,
                completedTurn);
        }

        public void SelectResolutionPoint(int selectedTimeStep)
        {
            if (_pendingTimingRequest == null)
            {
                throw new InvalidOperationException("No Resolution Timing input is pending.");
            }

            if (selectedTimeStep < _pendingTimingRequest.MinimumTimeStep
                || selectedTimeStep > _pendingTimingRequest.MaximumTimeStep)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedTimeStep));
            }

            _activeSchedule = InitialTimelineScheduleBuilder.SelectResolutionTime(
                _activeSchedule,
                _pendingTimingRequest.Side,
                _pendingTimingRequest.SequenceIndex,
                selectedTimeStep);
            _activeResolutionStepIndex = 0;
            while (_activeResolutionStepIndex < _activeSchedule.ResolutionSteps.Count
                && _activeSchedule.ResolutionSteps[_activeResolutionStepIndex].TimeStep
                    <= _currentTimelineTimeStep)
            {
                _activeResolutionStepIndex++;
            }

            _pendingTimingRequest = null;
        }

        public void SelectChargeLevel(int chargeLevel)
        {
            if (_pendingChargeRequest == null)
            {
                throw new InvalidOperationException("No Charge input is pending.");
            }

            if (chargeLevel < 0 || chargeLevel > _pendingChargeRequest.MaximumChargeLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(chargeLevel));
            }

            _activeSchedule = InitialTimelineScheduleBuilder.ApplyChargeLevel(
                _activeSchedule,
                _pendingChargeRequest.Side,
                _pendingChargeRequest.SequenceIndex,
                chargeLevel);
            RecalculateActiveResolutionStepIndex();
            _pendingChargeRequest = null;
        }

        private IReadOnlyList<SoloComboActionResolution> ResolveTimelineStep(
            TimelineResolutionStep step)
        {
            var resolutions = new List<SoloComboActionResolution>(step.Actions.Count);
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
                TimelineActorSide targetSide = action.Side == TimelineActorSide.Player
                    ? TimelineActorSide.Enemy
                    : TimelineActorSide.Player;
                double defensiveDamageReduction = action.HasDamage
                    && avoidance == DamageAvoidanceResult.None
                        ? GetActiveDefensiveDamageReduction(targetSide, step.TimeStep)
                        : 0d;
                int damage = action.HasDamage
                    && avoidance == DamageAvoidanceResult.None
                        ? CalculateDamage(
                            actor,
                            target,
                            action.DamageEffect,
                            defensiveDamageReduction)
                        : 0;
                ScheduledTimelineAction disruptedAction = action.HasDamage
                    && avoidance == DamageAvoidanceResult.None
                    && action.OnHitDisruption != null
                        ? FindOngoingAction(targetSide, step.TimeStep)
                        : null;
                ActionDisruptionResponse disruptionResponse = disruptedAction != null
                    ? disruptedAction.GetDisruptionResponse(
                        action.OnHitDisruption.Type,
                        step.TimeStep)
                    : ActionDisruptionResponse.Continue;
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
                    statusApplied: false,
                    defensiveDamageReduction,
                    appliedDisruption: disruptedAction != null
                        ? action.OnHitDisruption
                        : null,
                    disruptionResponse,
                    disruptedSide: targetSide,
                    disruptedSequenceIndex: disruptedAction?.SequenceIndex ?? -1));

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

            for (int resolutionIndex = 0; resolutionIndex < resolutions.Count; resolutionIndex++)
            {
                SoloComboActionResolution resolution = resolutions[resolutionIndex];
                if (resolution.AppliedDisruption == null)
                {
                    continue;
                }

                switch (resolution.DisruptionResponse)
                {
                    case ActionDisruptionResponse.PauseAndResume:
                        _activeSchedule = InitialTimelineScheduleBuilder.ApplyPauseAndResume(
                            _activeSchedule,
                            resolution.DisruptedSide,
                            resolution.DisruptedSequenceIndex,
                            step.TimeStep,
                            resolution.AppliedDisruption.Duration,
                            resolution.AppliedDisruption.Type);
                        break;
                    case ActionDisruptionResponse.RestartAction:
                        _activeSchedule = InitialTimelineScheduleBuilder.ApplyRestartAction(
                            _activeSchedule,
                            resolution.DisruptedSide,
                            resolution.DisruptedSequenceIndex,
                            step.TimeStep,
                            resolution.AppliedDisruption.Duration,
                            resolution.AppliedDisruption.Type);
                        break;
                    case ActionDisruptionResponse.CancelAction:
                        _activeSchedule = InitialTimelineScheduleBuilder.ApplyCancelAction(
                            _activeSchedule,
                            resolution.DisruptedSide,
                            resolution.DisruptedSequenceIndex,
                            step.TimeStep,
                            resolution.AppliedDisruption.Duration,
                            resolution.AppliedDisruption.Type);
                        break;
                }
            }

            return Array.AsReadOnly(resolutions.ToArray());
        }

        private ScheduledTimelineAction FindOngoingAction(
            TimelineActorSide side,
            int timeStep)
        {
            for (int index = 0; index < _activeSchedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _activeSchedule.Actions[index];
                if (!action.IsCanceled
                    && action.Side == side
                    && action.StartTime < timeStep
                    && action.EndTime > timeStep)
                {
                    return action;
                }
            }

            return null;
        }

        private void BeginTimeline(InitialTimelineSchedule schedule)
        {
            _activeSchedule = schedule
                ?? throw new ArgumentNullException(nameof(schedule));
            _activeResolutionStepIndex = 0;
            _currentTimelineTimeStep = 0;
            _pendingTimingRequest = null;
            _pendingChargeRequest = null;
            _activeResolutions.Clear();
        }

        private SoloComboTurnResult ResolveActiveTimelineToCompletion()
        {
            SoloComboTurnResult completedTurn = null;
            while (IsTimelineResolving)
            {
                SoloComboTimelineAdvanceResult advance = AdvanceTimeline();
                if (advance.RequiresTimingInput)
                {
                    SelectResolutionPoint(advance.TimingRequest.MinimumTimeStep);
                }
                else if (advance.RequiresChargeInput)
                {
                    SelectChargeLevel(0);
                }
                else
                {
                    completedTurn = advance.CompletedTurn;
                }
            }

            return completedTurn;
        }

        private SoloComboTurnResult CompleteActiveRound()
        {
            InitialTimelineSchedule completedSchedule = _activeSchedule;
            IReadOnlyList<SoloComboActionResolution> completedResolutions =
                Array.AsReadOnly(_activeResolutions.ToArray());

            RecoverAfterRound(out int manaRecovered, out int staminaRecovered);
            AdvanceRoundStatuses();

            var result = SoloComboTurnResult.Completed(
                completedResolutions,
                completedSchedule,
                manaRecovered,
                staminaRecovered,
                Outcome);

            _activeSchedule = null;
            _activeResolutionStepIndex = 0;
            _pendingTimingRequest = null;
            _pendingChargeRequest = null;
            _activeResolutions.Clear();

            if (Outcome == BattleOutcome.Ongoing)
            {
                SelectAndLockEnemyIntent();
            }

            return result;
        }

        private ScheduledTimelineAction FindWindowStartingAt(int timeStep)
        {
            for (int index = 0; index < _activeSchedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _activeSchedule.Actions[index];
                if (!action.IsCanceled
                    && action.Side == TimelineActorSide.Player
                    && action.ResolutionWindowStartTime == timeStep)
                {
                    return action;
                }
            }

            return null;
        }

        private ScheduledTimelineAction FindChargeStartingAt(int timeStep)
        {
            for (int index = 0; index < _activeSchedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _activeSchedule.Actions[index];
                if (!action.IsCanceled
                    && action.Side == TimelineActorSide.Player
                    && action.IsChargeable
                    && action.ResolutionTime == timeStep)
                {
                    return action;
                }
            }

            return null;
        }

        private void RecalculateActiveResolutionStepIndex()
        {
            _activeResolutionStepIndex = 0;
            while (_activeResolutionStepIndex < _activeSchedule.ResolutionSteps.Count
                && _activeSchedule.ResolutionSteps[_activeResolutionStepIndex].TimeStep
                    <= _currentTimelineTimeStep)
            {
                _activeResolutionStepIndex++;
            }
        }

        private void SelectAndLockEnemyIntent()
        {
            _currentEnemySequence = _enemyBehavior.SelectSequence(
                Player,
                Enemy,
                _previousEnemySequenceId,
                _enemyRandom.NextDouble());
            _previousEnemySequenceId = _currentEnemySequence.SequenceId;
            EnemyIntent = InitialTimelineScheduleBuilder.BuildEnemyOnly(
                _currentEnemySequence);
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
            DamageEffectDefinition damageEffect,
            double defensiveDamageReduction)
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
                configuration: _damageConfiguration,
                additionalApplicableDamageModifier: -defensiveDamageReduction).FinalDamage;
        }

        private double GetActiveDefensiveDamageReduction(
            TimelineActorSide defendedSide,
            int timeStep)
        {
            var appliedWindowIds = new HashSet<string>(StringComparer.Ordinal);
            double totalReduction = 0d;

            for (int actionIndex = 0; actionIndex < _activeSchedule.Actions.Count; actionIndex++)
            {
                ScheduledTimelineAction action = _activeSchedule.Actions[actionIndex];
                if (action.Side != defendedSide)
                {
                    continue;
                }

                for (int windowIndex = 0;
                    windowIndex < action.ActiveWindows.Count;
                    windowIndex++)
                {
                    ScheduledActiveWindow window = action.ActiveWindows[windowIndex];
                    if (window.Effect != ActiveWindowEffect.DefensiveState
                        || !window.IsActiveAt(timeStep)
                        || !appliedWindowIds.Add(window.WindowId))
                    {
                        continue;
                    }

                    totalReduction += window.DamageReduction;
                }
            }

            return Math.Min(1d, totalReduction);
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
