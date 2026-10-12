using System.Linq;
using NUnit.Framework;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    public sealed class SoloComboBattleStateTests
    {
        [Test]
        public void ExecuteCombo_UsesConfiguredTimelineOrderAndResolvesBothSides()
        {
            ComboConfiguration combo = CompleteCombo(
                "combo1",
                LinkedActionPlacement.AfterMain,
                cooldown: 3);
            SoloComboBattleState battle = CreateBattle(new[] { combo });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.ActionResolutions
                    .Where(item => item.Side == TimelineActorSide.Player)
                    .Select(item => item.ActionId),
                Is.EqualTo(new[] { "opening", "main", "linked", "closing" }));
            Assert.That(
                result.ActionResolutions
                    .Where(item => item.Side == TimelineActorSide.Player)
                    .Select(item => item.DamageDealt),
                Is.EqualTo(new[] { 0, 13, 6, 0 }));
            Assert.That(result.TotalDamageDealt, Is.EqualTo(19));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(81));
            Assert.That(result.LastResolvedTimeStep, Is.EqualTo(7));
        }

        [Test]
        public void FailedTurn_HasNoLastResolvedTimeStep()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { CompleteCombo("combo1", LinkedActionPlacement.AfterMain, cooldown: 3) });

            SoloComboTurnResult result = battle.ExecuteCombo(-1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.LastResolvedTimeStep, Is.Zero);
        }

        [Test]
        public void BeginCombo_PaysResourcesButDefersTimelineEffects()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { CompleteCombo("combo1", LinkedActionPlacement.AfterMain, cooldown: 3) });

            SoloComboExecutionFailure failure = battle.BeginCombo(0);

            Assert.That(failure, Is.EqualTo(SoloComboExecutionFailure.None));
            Assert.That(battle.IsTimelineResolving, Is.True);
            Assert.That(battle.CurrentTimelineTimeStep, Is.Zero);
            Assert.That(battle.TimelineCompletionTime, Is.EqualTo(7));
            Assert.That(battle.Player.CurrentMana, Is.EqualTo(8));
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(10));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(60));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));
            Assert.That(battle.ActiveTimeline, Is.Not.Null);
            Assert.That(battle.ActiveTimeline.CompletionTime, Is.EqualTo(7));
        }

        [Test]
        public void AdvanceTimeline_AppliesEffectsOnlyWhenTheirTimeStepArrives()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { CompleteCombo("combo1", LinkedActionPlacement.AfterMain, cooldown: 3) });
            battle.BeginCombo(0);

            SoloComboTimelineAdvanceResult firstStep = battle.AdvanceTimeline();
            Assert.That(firstStep.TimeStep, Is.EqualTo(1));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(60));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));

            SoloComboTimelineAdvanceResult secondStep = battle.AdvanceTimeline();
            Assert.That(secondStep.TimeStep, Is.EqualTo(2));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(44));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));

            battle.AdvanceTimeline();
            SoloComboTimelineAdvanceResult fourthStep = battle.AdvanceTimeline();
            Assert.That(fourthStep.TimeStep, Is.EqualTo(4));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(87));
        }

        [Test]
        public void ActiveTimeline_BlocksAnotherCommitAndCompletesAtActionEnd()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { CompleteCombo("combo1", LinkedActionPlacement.AfterMain, cooldown: 3) });
            battle.BeginCombo(0);

            Assert.That(
                battle.BeginCombo(0),
                Is.EqualTo(SoloComboExecutionFailure.TimelineResolving));
            Assert.That(battle.CanWait, Is.False);

            SoloComboTimelineAdvanceResult advance = null;
            while (battle.IsTimelineResolving)
            {
                advance = battle.AdvanceTimeline();
            }

            Assert.That(advance, Is.Not.Null);
            Assert.That(advance.RoundCompleted, Is.True);
            Assert.That(advance.CompletedTurn, Is.Not.Null);
            Assert.That(advance.TimeStep, Is.EqualTo(7));
            Assert.That(battle.IsTimelineResolving, Is.False);
            Assert.That(battle.Player.CurrentMana, Is.EqualTo(9));
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(12));
        }

        [Test]
        public void AdvanceTimeline_ResolutionWindowPausesBeforeItsFirstCandidate()
        {
            SoloComboBattleState battle = CreateBattleWithTimingWindow();
            battle.BeginCombo(0);

            SoloComboTimelineAdvanceResult advance = battle.AdvanceTimeline();

            Assert.That(advance.RequiresTimingInput, Is.True);
            Assert.That(advance.TimeStep, Is.Zero);
            Assert.That(advance.TimingRequest.DisplayName, Is.EqualTo("Timed Strike"));
            Assert.That(advance.TimingRequest.MinimumTimeStep, Is.EqualTo(1));
            Assert.That(advance.TimingRequest.MaximumTimeStep, Is.EqualTo(3));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void SelectResolutionPoint_MovesEffectWithoutMovingActionCompletion()
        {
            SoloComboBattleState battle = CreateBattleWithTimingWindow();
            battle.BeginCombo(0);
            battle.AdvanceTimeline();
            battle.SelectResolutionPoint(3);

            Assert.That(
                battle.AdvanceTimeline().ActionResolutions.Any(
                    resolution => resolution.Side == TimelineActorSide.Player),
                Is.False);
            Assert.That(battle.AdvanceTimeline().ActionResolutions, Is.Empty);
            SoloComboTimelineAdvanceResult thirdStep = battle.AdvanceTimeline();

            Assert.That(thirdStep.ActionResolutions, Has.Count.EqualTo(1));
            Assert.That(thirdStep.ActionResolutions[0].TimeStep, Is.EqualTo(3));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(87));
            Assert.That(thirdStep.RoundCompleted, Is.True);
        }

        [Test]
        public void ExecuteCombo_ResolutionWindowWithoutInputDefaultsToMinimumTime()
        {
            SoloComboBattleState battle = CreateBattleWithTimingWindow();

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ActionResolutions[0].TimeStep, Is.EqualTo(1));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(87));
        }

        [Test]
        public void AdvanceTimeline_ChargeableActionPausesBeforeBaseResolutionPoint()
        {
            SoloComboBattleState battle = CreateBattleWithChargeAction();
            battle.BeginCombo(0);

            SoloComboTimelineAdvanceResult advance = battle.AdvanceTimeline();

            Assert.That(advance.RequiresChargeInput, Is.True);
            Assert.That(advance.TimeStep, Is.Zero);
            Assert.That(advance.ChargeRequest.DisplayName, Is.EqualTo("Charged Strike"));
            Assert.That(advance.ChargeRequest.MaximumChargeLevel, Is.EqualTo(2));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void SelectChargeLevel_ExtendsActionAndLaterActionsAndScalesDamage()
        {
            SoloComboBattleState battle = CreateBattleWithChargeAction();
            battle.BeginCombo(0);
            battle.AdvanceTimeline();

            battle.SelectChargeLevel(2);

            Assert.That(battle.TimelineCompletionTime, Is.EqualTo(10));
            SoloComboTurnResult result = null;
            while (battle.IsTimelineResolving)
            {
                SoloComboTimelineAdvanceResult advance = battle.AdvanceTimeline();
                if (advance.RoundCompleted)
                {
                    result = advance.CompletedTurn;
                }
            }

            Assert.That(result, Is.Not.Null);
            ScheduledTimelineAction charged = result.Schedule.Actions.Single(
                action => action.ActionId == "charged-strike");
            ScheduledTimelineAction closing = result.Schedule.Actions.Single(
                action => action.ActionId == "charged-closing");
            Assert.That(
                (charged.StartTime, charged.EndTime, charged.ResolutionTime),
                Is.EqualTo((0, 8, 7)));
            Assert.That(
                (closing.StartTime, closing.EndTime, closing.ResolutionTime),
                Is.EqualTo((8, 10, 10)));
            Assert.That(
                result.ActionResolutions.Single(
                    resolution => resolution.ActionId == "charged-strike").DamageDealt,
                Is.EqualTo(21));
        }

        [Test]
        public void ExecuteCombo_ChargeWithoutInputDefaultsToLevelZero()
        {
            SoloComboBattleState battle = CreateBattleWithChargeAction();

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            ScheduledTimelineAction charged = result.Schedule.Actions.Single(
                action => action.ActionId == "charged-strike");
            Assert.That(result.Succeeded, Is.True);
            Assert.That(charged.EndTime, Is.EqualTo(2));
            Assert.That(charged.ResolutionTime, Is.EqualTo(1));
            Assert.That(
                result.ActionResolutions.Single(
                    resolution => resolution.ActionId == "charged-strike").DamageDealt,
                Is.EqualTo(13));
        }

        [Test]
        public void ExecuteCombo_PaysWholeComboThenRecoversResourcesAfterEnemyTurn()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { CompleteCombo("combo1", LinkedActionPlacement.AfterMain, cooldown: 3) });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(battle.Player.CurrentMana, Is.EqualTo(9));
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(12));
            Assert.That(result.ManaRecovered, Is.EqualTo(1));
            Assert.That(result.StaminaRecovered, Is.EqualTo(2));
            Assert.That(result.EnemyDamageDealt, Is.EqualTo(16));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(44));
        }

        [Test]
        public void ExecuteCombo_GuardAtEnemyDamageTime_BlocksHitPackage()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { DefensiveCombo(ActionResolutionEffect.Guard, resolutionOffset: 2) });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution enemyDamage = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy
                    && resolution.ResolutionEffect == ActionResolutionEffect.Damage);
            Assert.That(enemyDamage.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.Guarded));
            Assert.That(enemyDamage.DamageDealt, Is.Zero);
            Assert.That(result.EnemyDamageDealt, Is.Zero);
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(60));
        }

        [Test]
        public void ExecuteCombo_EvadeAtEnemyDamageTime_BlocksHitPackage()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { DefensiveCombo(ActionResolutionEffect.Evade, resolutionOffset: 2) });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution enemyDamage = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy
                    && resolution.ResolutionEffect == ActionResolutionEffect.Damage);
            Assert.That(enemyDamage.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.Evaded));
            Assert.That(enemyDamage.DamageDealt, Is.Zero);
            Assert.That(result.EnemyDamageDealt, Is.Zero);
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(60));
        }

        [Test]
        public void ExecuteCombo_UnguardableDamage_BypassesGuard()
        {
            SoloComboBattleState battle = CreateBattleAgainstTaggedDamage(
                ActionResolutionEffect.Guard,
                DamageResolutionTag.Unguardable);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution enemyDamage = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy
                    && resolution.ResolutionEffect == ActionResolutionEffect.Damage);
            Assert.That(enemyDamage.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.None));
            Assert.That(enemyDamage.DamageTags, Is.EqualTo(DamageResolutionTag.Unguardable));
            Assert.That(enemyDamage.DamageDealt, Is.EqualTo(16));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(44));
        }

        [Test]
        public void ExecuteCombo_OvercommitDamage_BypassesGuard()
        {
            SoloComboBattleState battle = CreateBattleAgainstTaggedDamage(
                ActionResolutionEffect.Guard,
                DamageResolutionTag.Overcommit);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution enemyDamage = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy
                    && resolution.ResolutionEffect == ActionResolutionEffect.Damage);
            Assert.That(enemyDamage.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.None));
            Assert.That(enemyDamage.DamageDealt, Is.EqualTo(16));
        }

        [Test]
        public void ExecuteCombo_AreaDamage_BypassesEvade()
        {
            SoloComboBattleState battle = CreateBattleAgainstTaggedDamage(
                ActionResolutionEffect.Evade,
                DamageResolutionTag.Area);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution enemyDamage = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy
                    && resolution.ResolutionEffect == ActionResolutionEffect.Damage);
            Assert.That(enemyDamage.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.None));
            Assert.That(enemyDamage.DamageDealt, Is.EqualTo(16));
        }

        [Test]
        public void ExecuteCombo_DamageTags_DoNotBypassUnrelatedDefense()
        {
            SoloComboBattleState guardBattle = CreateBattleAgainstTaggedDamage(
                ActionResolutionEffect.Guard,
                DamageResolutionTag.Area);
            SoloComboBattleState evadeBattle = CreateBattleAgainstTaggedDamage(
                ActionResolutionEffect.Evade,
                DamageResolutionTag.Unguardable);

            SoloComboTurnResult guardResult = guardBattle.ExecuteCombo(0);
            SoloComboTurnResult evadeResult = evadeBattle.ExecuteCombo(0);

            Assert.That(
                guardResult.ActionResolutions.Single(
                    resolution => resolution.Side == TimelineActorSide.Enemy
                        && resolution.ResolutionEffect == ActionResolutionEffect.Damage)
                    .DamageAvoidance,
                Is.EqualTo(DamageAvoidanceResult.Guarded));
            Assert.That(
                evadeResult.ActionResolutions.Single(
                    resolution => resolution.Side == TimelineActorSide.Enemy
                        && resolution.ResolutionEffect == ActionResolutionEffect.Damage)
                    .DamageAvoidance,
                Is.EqualTo(DamageAvoidanceResult.Evaded));
        }

        [Test]
        public void ExecuteCombo_UsesAttackersDamageDealtModifiers()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { MainOnlyCombo("modified", 0, 0, 0, 0.40d) });
            battle.Player.AddDamageDealtModifier(
                DamageModifier.ForType(DamageType.Slashing, 0.20d));

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.TotalDamageDealt, Is.EqualTo(15));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(85));
        }

        [Test]
        public void ExecuteCombo_UsesDefendersDamageTakenModifiers()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { MainOnlyCombo("resisted", 0, 0, 0, 0.40d) });
            battle.Enemy.AddDamageTakenModifier(
                DamageModifier.ForType(DamageType.Slashing, -0.20d));

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.TotalDamageDealt, Is.EqualTo(10));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(90));
        }

        [Test]
        public void ExecuteCombo_DebuffBeforeDamage_ModifiesLaterResolutionPoint()
        {
            SoloComboBattleState battle = CreateBattleWithExposedSequence(
                clawResolutionOffset: 1);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.EnemyDamageDealt, Is.EqualTo(19));
            Assert.That(battle.Player.ActiveStatuses, Has.Count.EqualTo(1));
            Assert.That(battle.Player.ActiveStatuses[0].Definition.DisplayName, Is.EqualTo("Exposed"));
            Assert.That(battle.Player.ActiveStatuses[0].RemainingDuration, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteCombo_DebuffAtSameTime_DoesNotRetroactivelyModifyDamage()
        {
            SoloComboBattleState battle = CreateBattleWithExposedSequence(
                clawResolutionOffset: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.EnemyDamageDealt, Is.EqualTo(16));
            Assert.That(
                result.ActionResolutions.Single(
                    resolution => resolution.ResolutionEffect
                        == ActionResolutionEffect.ApplyDebuff).StatusApplied,
                Is.True);
        }

        [Test]
        public void ExecuteCombo_GuardAtDifferentTime_DoesNotBlockDamage()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { DefensiveCombo(ActionResolutionEffect.Guard, resolutionOffset: 1) });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution enemyDamage = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy
                    && resolution.ResolutionEffect == ActionResolutionEffect.Damage);
            Assert.That(enemyDamage.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.None));
            Assert.That(enemyDamage.DamageDealt, Is.EqualTo(16));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(44));
        }

        [Test]
        public void ExecuteCombo_EnemyGuardAtPlayerDamageTime_BlocksHitPackage()
        {
            var player = new CombatantState(
                "Player",
                new CombatantStats(60, 12, 14, 50, 50));
            var enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(100, 0, 0, 60, 60));
            ComboConfiguration combo = MainOnlyCombo(
                "player-strike",
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.40d);
            var enemyGuard = new EnemyActionSequence(
                "enemy-guard",
                "Enemy Guard",
                new[]
                {
                    new EnemyActionDefinition(
                        "guard",
                        "Guard",
                        loadCost: 1,
                        resolutionOffset: 1,
                        damageMultiplier: 0d,
                        resolutionEffect: ActionResolutionEffect.Guard)
                });
            var battle = new SoloComboBattleState(
                player,
                enemy,
                new[] { combo },
                enemyGuard,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution playerDamage = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Player
                    && resolution.ResolutionEffect == ActionResolutionEffect.Damage);
            Assert.That(playerDamage.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.Guarded));
            Assert.That(playerDamage.DamageDealt, Is.Zero);
            Assert.That(result.TotalDamageDealt, Is.Zero);
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void ExecuteCombo_WhenManaIsInsufficient_DoesNotAdvanceBattle()
        {
            ComboConfiguration expensiveCombo = MainOnlyCombo(
                "expensive",
                manaCost: 13,
                staminaCost: 0,
                cooldown: 2,
                damageMultiplier: 0.40d);
            SoloComboBattleState battle = CreateBattle(new[] { expensiveCombo });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Failure, Is.EqualTo(SoloComboExecutionFailure.InsufficientMana));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(60));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));
            Assert.That(battle.GetCooldownRemaining(0), Is.Zero);
        }

        [Test]
        public void ExecuteCombo_SetsCooldownAndOtherComboAdvancesIt()
        {
            ComboConfiguration first = MainOnlyCombo(
                "first",
                manaCost: 0,
                staminaCost: 0,
                cooldown: 2,
                damageMultiplier: 0.10d);
            ComboConfiguration second = MainOnlyCombo(
                "second",
                manaCost: 0,
                staminaCost: 0,
                cooldown: 1,
                damageMultiplier: 0.10d);
            SoloComboBattleState battle = CreateBattle(new[] { first, second });

            battle.ExecuteCombo(0);
            SoloComboTurnResult blocked = battle.ExecuteCombo(0);
            battle.ExecuteCombo(1);

            Assert.That(blocked.Failure, Is.EqualTo(SoloComboExecutionFailure.ComboOnCooldown));
            Assert.That(battle.GetCooldownRemaining(0), Is.EqualTo(1));
            Assert.That(battle.GetCooldownRemaining(1), Is.EqualTo(1));
        }

        [Test]
        public void ExecuteCombo_WithZeroCooldown_IsAvailableOnNextTurn()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { MainOnlyCombo("repeatable", 0, 0, 0, 0.10d) });

            battle.ExecuteCombo(0);

            Assert.That(battle.GetCooldownRemaining(0), Is.Zero);
            Assert.That(
                battle.GetExecutionFailure(0),
                Is.EqualTo(SoloComboExecutionFailure.None));
        }

        [Test]
        public void WaitTurn_WhenAllCombosAreUnavailable_AdvancesCooldownAndRound()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { MainOnlyCombo("cooling-down", 0, 0, 1, 0.10d) });

            battle.ExecuteCombo(0);
            SoloComboTurnResult waitResult = battle.WaitTurn();

            Assert.That(waitResult.Succeeded, Is.True);
            Assert.That(
                waitResult.ActionResolutions.All(
                    resolution => resolution.Side == TimelineActorSide.Enemy),
                Is.True);
            Assert.That(waitResult.EnemyDamageDealt, Is.EqualTo(16));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(28));
            Assert.That(battle.GetCooldownRemaining(0), Is.Zero);
            Assert.That(
                battle.GetExecutionFailure(0),
                Is.EqualTo(SoloComboExecutionFailure.None));
        }

        [Test]
        public void WaitTurn_WhenAComboIsAvailable_DoesNotAdvanceBattle()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { MainOnlyCombo("available", 0, 0, 0, 0.10d) });

            SoloComboTurnResult result = battle.WaitTurn();

            Assert.That(result.Failure, Is.EqualTo(SoloComboExecutionFailure.WaitNotAvailable));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(60));
            Assert.That(battle.Enemy.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void ExecuteCombo_WhenEnemyIsDefeated_SkipsEnemyTurnAndRecovery()
        {
            ComboConfiguration finisher = MainOnlyCombo(
                "finisher",
                manaCost: 2,
                staminaCost: 2,
                cooldown: 4,
                damageMultiplier: 4d);
            SoloComboBattleState battle = CreateBattle(new[] { finisher });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(result.EnemyDamageDealt, Is.Zero);
            Assert.That(result.ManaRecovered, Is.Zero);
            Assert.That(result.StaminaRecovered, Is.Zero);
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(60));
            Assert.That(battle.Player.CurrentMana, Is.EqualTo(10));
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(12));
        }

        [Test]
        public void ExecuteCombo_WhenBothDamageEffectsShareTimeStep_AllowsMutualDestruction()
        {
            var player = new CombatantState(
                "Player",
                new CombatantStats(60, 12, 14, 50, 50));
            player.ReceiveDamage(50);
            var enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(10, 0, 0, 50, 50));
            ComboConfiguration finisher = MainOnlyCombo(
                "shared-step",
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.30d);
            var enemySequence = new EnemyActionSequence(
                "enemy-shared-step",
                "Enemy Shared Step",
                new[]
                {
                    new EnemyActionDefinition(
                        "claw",
                        "Claw",
                        1,
                        1,
                        damageMultiplier: 0.30d)
                });
            var battle = new SoloComboBattleState(
                player,
                enemy,
                new[] { finisher },
                enemySequence,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(battle.Player.IsDefeated, Is.True);
            Assert.That(battle.Enemy.IsDefeated, Is.True);
            Assert.That(result.ActionResolutions, Has.Count.EqualTo(2));
            Assert.That(result.ManaRecovered, Is.Zero);
            Assert.That(result.StaminaRecovered, Is.Zero);
        }

        [Test]
        public void Constructor_ExposesLockedEnemyIntentSchedule()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { MainOnlyCombo("combo1", 0, 0, 0, 0.10d) });

            Assert.That(battle.EnemySequence.DisplayName, Is.EqualTo("Claw Pattern"));
            Assert.That(battle.EnemyIntent.Actions, Has.Count.EqualTo(1));
            Assert.That(battle.EnemyIntent.Actions[0].ResolutionTime, Is.EqualTo(2));
            Assert.That(battle.EnemyIntent.Actions[0].Side, Is.EqualTo(TimelineActorSide.Enemy));
        }

        [Test]
        public void RoundEnd_SelectsAndLocksANewEligibleEnemyIntent()
        {
            var first = new EnemyActionSequence(
                "first",
                "First",
                new[] { new EnemyActionDefinition("first-action", "First", 1, 1) });
            var second = new EnemyActionSequence(
                "second",
                "Second",
                new[] { new EnemyActionDefinition("second-action", "Second", 1, 1) });
            var behavior = new EnemyBehaviorTable(
                new[]
                {
                    new EnemyBehaviorEntry(first, 1, preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(second, 1, preventImmediateRepeat: true)
                },
                first);
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(60, 12, 14, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { MainOnlyCombo("safe", 0, 0, 0, 0d) },
                behavior,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2,
                enemyRandom: new System.Random(7));
            string firstIntentId = battle.EnemySequence.SequenceId;

            battle.ExecuteCombo(0);

            Assert.That(battle.EnemySequence.SequenceId, Is.Not.EqualTo(firstIntentId));
            Assert.That(
                battle.EnemyIntent.Actions[0].ActionId,
                Is.EqualTo($"{battle.EnemySequence.SequenceId}-action"));
        }

        [Test]
        public void GetExecutionFailure_WithInvalidIndex_ReturnsIndexFailure()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { MainOnlyCombo("combo1", 0, 0, 1, 0.10d) });

            Assert.That(
                battle.GetExecutionFailure(1),
                Is.EqualTo(SoloComboExecutionFailure.ComboIndexOutOfRange));
        }

        [Test]
        public void DefensiveActiveWindow_ReducesOnlyDamageInsideInclusiveWindow()
        {
            SoloComboBattleState battle = CreateBattleWithDefensiveActiveWindow();

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution[] enemyDamage = result.ActionResolutions
                .Where(resolution => resolution.Side == TimelineActorSide.Enemy)
                .ToArray();
            Assert.That(enemyDamage, Has.Length.EqualTo(2));
            Assert.That(enemyDamage[0].TimeStep, Is.EqualTo(2));
            Assert.That(enemyDamage[0].DamageDealt, Is.EqualTo(11));
            Assert.That(enemyDamage[0].DefensiveDamageReduction, Is.EqualTo(0.30d));
            Assert.That(enemyDamage[1].TimeStep, Is.EqualTo(5));
            Assert.That(enemyDamage[1].DamageDealt, Is.EqualTo(16));
            Assert.That(enemyDamage[1].DefensiveDamageReduction, Is.Zero);
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(73));
        }

        [Test]
        public void OnHitStagger_PauseAndResumeDelaysUnresolvedEnemyAction()
        {
            var breaker = new ComboActionDefinition(
                "breaker",
                "Breaker",
                ComboActionCategory.Main,
                loadCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 2,
                onHitDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    duration: 2));
            var combo = new ComboConfiguration(
                "breaker-combo",
                openingAction: null,
                mainAction: breaker,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var response = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.PauseAndResume,
                    ActionDisruptionResponse.PauseAndResume)
            };
            var enemySequence = new EnemyActionSequence(
                "heavy",
                "Heavy",
                new[]
                {
                    new EnemyActionDefinition(
                        "heavy-swing",
                        "Heavy Swing",
                        loadCost: 5,
                        resolutionOffset: 5,
                        damageMultiplier: 0.65d,
                        disruptionResponseSegments: response)
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(50, 0, 0, 60, 60)),
                new[] { combo },
                enemySequence,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            Assert.That(battle.BeginCombo(0), Is.EqualTo(SoloComboExecutionFailure.None));
            battle.AdvanceTimeline();
            SoloComboTimelineAdvanceResult staggerStep = battle.AdvanceTimeline();

            Assert.That(staggerStep.TimeStep, Is.EqualTo(2));
            Assert.That(staggerStep.ActionResolutions[0].AppliedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Stagger));
            Assert.That(staggerStep.ActionResolutions[0].DisruptionResponse,
                Is.EqualTo(ActionDisruptionResponse.PauseAndResume));
            Assert.That(battle.TimelineCompletionTime, Is.EqualTo(7));
            ScheduledTimelineAction delayedEnemyAction = battle.ActiveTimeline.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy);
            Assert.That(delayedEnemyAction.DisruptionPauses, Has.Count.EqualTo(1));
            Assert.That(delayedEnemyAction.DisruptionPauses[0].StartTime, Is.EqualTo(2));
            Assert.That(delayedEnemyAction.DisruptionPauses[0].EndTime, Is.EqualTo(4));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(100));

            while (battle.IsTimelineResolving)
            {
                battle.AdvanceTimeline();
            }

            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(74));
        }

        [Test]
        public void OnHitStagger_BreakWindowStopsReducingLaterDamage()
        {
            var breaker = new ComboActionDefinition(
                "breaker",
                "Breaker",
                ComboActionCategory.Main,
                loadCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 2,
                onHitDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    duration: 2));
            var followUp = new ComboActionDefinition(
                "follow-up",
                "Follow Up",
                ComboActionCategory.Linked,
                loadCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 1);
            var combo = new ComboConfiguration(
                "window-breaker",
                openingAction: null,
                mainAction: breaker,
                closingAction: null,
                linkedAction: followUp,
                LinkedActionPlacement.AfterMain);
            var defensiveWindow = new ActionActiveWindowDefinition(
                "enemy.guard",
                "Enemy Guard",
                startOffset: 1,
                endOffset: 5,
                ActiveWindowEffect.DefensiveState,
                damageReduction: 0.30d,
                ActiveWindowDisruptionPolicy.BreakOnDisruption);
            var response = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.PauseAndResume,
                    ActionDisruptionResponse.PauseAndResume)
            };
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                new EnemyActionSequence(
                    "guarding-enemy",
                    "Guarding Enemy",
                    new[]
                    {
                        new EnemyActionDefinition(
                            "guarding-enemy",
                            "Guarding Enemy",
                            loadCost: 5,
                            resolutionOffset: 5,
                            damageMultiplier: 0.20d,
                            activeWindows: new[] { defensiveWindow },
                            disruptionResponseSegments: response)
                    }),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution[] playerHits = result.ActionResolutions
                .Where(resolution => resolution.Side == TimelineActorSide.Player)
                .ToArray();

            Assert.That(playerHits, Has.Length.EqualTo(2));
            Assert.That(playerHits[0].TimeStep, Is.EqualTo(2));
            Assert.That(playerHits[0].DefensiveDamageReduction, Is.EqualTo(0.30d));
            Assert.That(playerHits[1].TimeStep, Is.EqualTo(5));
            Assert.That(playerHits[1].DefensiveDamageReduction, Is.Zero);
        }

        [Test]
        public void OnHitStagger_RestartActionRepeatsPreviouslyResolvedEffect()
        {
            var breaker = new ComboActionDefinition(
                "breaker",
                "Breaker",
                ComboActionCategory.Main,
                loadCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 2,
                onHitDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    duration: 2));
            var response = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.RestartAction,
                    ActionDisruptionResponse.RestartAction)
            };
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[]
                {
                    new ComboConfiguration(
                        "breaker-combo",
                        openingAction: null,
                        mainAction: breaker,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null)
                },
                new EnemyActionSequence(
                    "early-heavy",
                    "Early Heavy",
                    new[]
                    {
                        new EnemyActionDefinition(
                            "early-heavy",
                            "Early Heavy",
                            loadCost: 5,
                            resolutionOffset: 1,
                            damageMultiplier: 0.40d,
                            disruptionResponseSegments: response)
                    }),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            Assert.That(battle.BeginCombo(0), Is.EqualTo(SoloComboExecutionFailure.None));
            SoloComboTimelineAdvanceResult firstHit = battle.AdvanceTimeline();
            Assert.That(firstHit.TimeStep, Is.EqualTo(1));
            Assert.That(firstHit.ActionResolutions.Single().DamageDealt, Is.EqualTo(16));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(84));

            SoloComboTimelineAdvanceResult restartStep = battle.AdvanceTimeline();
            Assert.That(restartStep.ActionResolutions.Single().DisruptionResponse,
                Is.EqualTo(ActionDisruptionResponse.RestartAction));
            Assert.That(battle.ActiveTimeline.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy).Restarts,
                Has.Count.EqualTo(1));

            SoloComboTurnResult completed = null;
            while (battle.IsTimelineResolving)
            {
                completed = battle.AdvanceTimeline().CompletedTurn ?? completed;
            }

            SoloComboActionResolution[] enemyHits = completed.ActionResolutions
                .Where(resolution => resolution.Side == TimelineActorSide.Enemy)
                .ToArray();
            Assert.That(enemyHits, Has.Length.EqualTo(2));
            Assert.That(enemyHits[0].TimeStep, Is.EqualTo(1));
            Assert.That(enemyHits[1].TimeStep, Is.EqualTo(5));
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(68));
            Assert.That(completed.Schedule.CompletionTime, Is.EqualTo(9));
        }

        [Test]
        public void OnHitStagger_CancelActionPreventsUnresolvedEnemyEffect()
        {
            var breaker = new ComboActionDefinition(
                "breaker",
                "Breaker",
                ComboActionCategory.Main,
                loadCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 2,
                onHitDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    duration: 2));
            var response = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    3,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.CancelAction,
                    ActionDisruptionResponse.CancelAction)
            };
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[]
                {
                    new ComboConfiguration(
                        "breaker-combo",
                        openingAction: null,
                        mainAction: breaker,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null)
                },
                new EnemyActionSequence(
                    "cancelable-jab",
                    "Cancelable Jab",
                    new[]
                    {
                        new EnemyActionDefinition(
                            "cancelable-jab",
                            "Cancelable Jab",
                            loadCost: 3,
                            resolutionOffset: 3,
                            damageMultiplier: 0.40d,
                            disruptionResponseSegments: response)
                    }),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            Assert.That(battle.BeginCombo(0), Is.EqualTo(SoloComboExecutionFailure.None));
            battle.AdvanceTimeline();
            SoloComboTimelineAdvanceResult cancelStep = battle.AdvanceTimeline();

            Assert.That(cancelStep.ActionResolutions.Single().DisruptionResponse,
                Is.EqualTo(ActionDisruptionResponse.CancelAction));
            ScheduledTimelineAction canceledEnemyAction = battle.ActiveTimeline.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy);
            Assert.That(canceledEnemyAction.IsCanceled, Is.True);
            Assert.That(canceledEnemyAction.Cancellation.RecoveryEndTime, Is.EqualTo(4));

            SoloComboTurnResult completed = null;
            while (battle.IsTimelineResolving)
            {
                completed = battle.AdvanceTimeline().CompletedTurn ?? completed;
            }

            Assert.That(completed.ActionResolutions.Any(
                resolution => resolution.Side == TimelineActorSide.Enemy),
                Is.False);
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(100));
            Assert.That(completed.Schedule.CompletionTime, Is.EqualTo(4));
        }

        [Test]
        public void OnHitStagger_ZeroRoundedDamageStillDisrupts()
        {
            var breaker = new ComboActionDefinition(
                "light-breaker",
                "Light Breaker",
                ComboActionCategory.Main,
                loadCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.01d,
                resolutionOffset: 2,
                onHitDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    duration: 2));
            var response = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.PauseAndResume,
                    ActionDisruptionResponse.PauseAndResume)
            };
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 1, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 100)),
                new[]
                {
                    new ComboConfiguration(
                        "light-breaker-combo",
                        openingAction: null,
                        mainAction: breaker,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null)
                },
                new EnemyActionSequence(
                    "heavy",
                    "Heavy",
                    new[]
                    {
                        new EnemyActionDefinition(
                            "heavy-swing",
                            "Heavy Swing",
                            loadCost: 5,
                            resolutionOffset: 5,
                            damageMultiplier: 0.65d,
                            disruptionResponseSegments: response)
                    }),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            Assert.That(battle.BeginCombo(0), Is.EqualTo(SoloComboExecutionFailure.None));
            battle.AdvanceTimeline();
            SoloComboTimelineAdvanceResult hitStep = battle.AdvanceTimeline();

            Assert.That(hitStep.ActionResolutions[0].DamageDealt, Is.Zero);
            Assert.That(hitStep.ActionResolutions[0].DamageAvoidance,
                Is.EqualTo(DamageAvoidanceResult.None));
            Assert.That(hitStep.ActionResolutions[0].AppliedDisruption, Is.Not.Null);
            Assert.That(battle.TimelineCompletionTime, Is.EqualTo(7));
        }

        [Test]
        public void OnHitStagger_GuardedDamageDoesNotDisrupt()
        {
            var guard = new ComboActionDefinition(
                "guard",
                "Guard",
                ComboActionCategory.Opening,
                loadCost: 3,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionOffset: 2,
                resolutionEffect: ActionResolutionEffect.Guard);
            var combo = new ComboConfiguration(
                "guard-combo",
                openingAction: guard,
                mainAction: Action(
                    "main",
                    ComboActionCategory.Main,
                    load: 1,
                    mana: 0,
                    stamina: 0,
                    cooldown: 0,
                    damageMultiplier: 0d),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var enemySequence = new EnemyActionSequence(
                "stagger-strike",
                "Stagger Strike",
                new[]
                {
                    new EnemyActionDefinition(
                        "stagger-strike",
                        "Stagger Strike",
                        loadCost: 2,
                        resolutionOffset: 2,
                        damageMultiplier: 0.40d,
                        onHitDisruption: new ActionDisruptionDefinition(
                            ActionDisruptionType.Stagger,
                            duration: 2))
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                enemySequence,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution enemyHit = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy);

            Assert.That(enemyHit.DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.Guarded));
            Assert.That(enemyHit.AppliedDisruption, Is.Null);
            Assert.That(result.Schedule.CompletionTime, Is.EqualTo(4));
        }

        private static SoloComboBattleState CreateBattle(ComboConfiguration[] combos)
        {
            var player = new CombatantState(
                "Player",
                new CombatantStats(60, 12, 14, 50, 50));
            var enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(100, 0, 0, 60, 60));
            var enemySequence = new EnemyActionSequence(
                "training.claw-pattern",
                "Claw Pattern",
                new[]
                {
                    new EnemyActionDefinition(
                        "claw",
                        "Claw",
                        2,
                        2,
                        damageMultiplier: 0.40d)
                });
            return new SoloComboBattleState(
                player,
                enemy,
                combos,
                enemySequence,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);
        }

        private static SoloComboBattleState CreateBattleWithDefensiveActiveWindow()
        {
            var activeWindow = new ActionActiveWindowDefinition(
                "focus.defense",
                "Focused Defense",
                startOffset: 1,
                endOffset: 3,
                ActiveWindowEffect.DefensiveState,
                damageReduction: 0.30d,
                ActiveWindowDisruptionPolicy.PersistThroughDisruption);
            var focus = new ComboActionDefinition(
                "focus",
                "Focus",
                ComboActionCategory.Main,
                loadCost: 5,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                activeWindows: new[] { activeWindow });
            var combo = new ComboConfiguration(
                "defensive-window",
                openingAction: null,
                mainAction: focus,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var enemySequence = new EnemyActionSequence(
                "double-strike",
                "Double Strike",
                new[]
                {
                    new EnemyActionDefinition(
                        "first-strike",
                        "First Strike",
                        loadCost: 2,
                        resolutionOffset: 2,
                        damageMultiplier: 0.40d),
                    new EnemyActionDefinition(
                        "second-strike",
                        "Second Strike",
                        loadCost: 3,
                        resolutionOffset: 3,
                        damageMultiplier: 0.40d)
                });

            return new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(50, 0, 0, 60, 60)),
                new[] { combo },
                enemySequence,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);
        }

        private static SoloComboBattleState CreateBattleWithTimingWindow()
        {
            var timedStrike = new ComboActionDefinition(
                "timed-strike",
                "Timed Strike",
                ComboActionCategory.Main,
                loadCost: 3,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.40d,
                resolutionOffset: 3,
                resolutionWindowStart: 1,
                resolutionWindowEnd: 3);
            var combo = new ComboConfiguration(
                "timed-combo",
                openingAction: null,
                mainAction: timedStrike,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var enemySequence = new EnemyActionSequence(
                "observe",
                "Observe",
                new[]
                {
                    new EnemyActionDefinition(
                        "observe-action",
                        "Observe",
                        loadCost: 1,
                        resolutionOffset: 1,
                        damageMultiplier: 0d)
                });

            return new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(60, 12, 14, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                enemySequence,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);
        }

        private static SoloComboBattleState CreateBattleWithChargeAction()
        {
            var chargedStrike = new ComboActionDefinition(
                "charged-strike",
                "Charged Strike",
                ComboActionCategory.Main,
                loadCost: 2,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.40d,
                resolutionOffset: 1,
                chargeLevels: new[]
                {
                    new ComboActionChargeLevelDefinition(3, 1.35d),
                    new ComboActionChargeLevelDefinition(6, 1.70d)
                });
            var closing = new ComboActionDefinition(
                "charged-closing",
                "Charged Closing",
                ComboActionCategory.Closing,
                loadCost: 2,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.20d,
                resolutionOffset: 2);
            var combo = new ComboConfiguration(
                "charged-combo",
                openingAction: null,
                mainAction: chargedStrike,
                closingAction: closing,
                linkedAction: null,
                linkedActionPlacement: null);
            var enemySequence = new EnemyActionSequence(
                "observe",
                "Observe",
                new[]
                {
                    new EnemyActionDefinition(
                        "observe-action",
                        "Observe",
                        loadCost: 1,
                        resolutionOffset: 1,
                        damageMultiplier: 0d)
                });

            return new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(60, 12, 14, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                enemySequence,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);
        }

        private static SoloComboBattleState CreateBattleAgainstTaggedDamage(
            ActionResolutionEffect defensiveEffect,
            DamageResolutionTag damageTags)
        {
            var player = new CombatantState(
                "Player",
                new CombatantStats(60, 12, 14, 50, 50));
            var enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(100, 0, 0, 60, 60));
            var enemySequence = new EnemyActionSequence(
                "tagged-attack",
                "Tagged Attack",
                new[]
                {
                    new EnemyActionDefinition(
                        "tagged-strike",
                        "Tagged Strike",
                        loadCost: 2,
                        resolutionOffset: 2,
                        damageMultiplier: 0.40d,
                        damageTags: damageTags)
                });
            return new SoloComboBattleState(
                player,
                enemy,
                new[] { DefensiveCombo(defensiveEffect, resolutionOffset: 2) },
                enemySequence,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);
        }

        private static SoloComboBattleState CreateBattleWithExposedSequence(
            int clawResolutionOffset)
        {
            var player = new CombatantState(
                "Player",
                new CombatantStats(60, 12, 14, 50, 50));
            var enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(100, 0, 0, 60, 60));
            var exposed = new CombatStatusDefinition(
                "exposed",
                "Exposed",
                CombatStatusDurationType.Rounds,
                duration: 2,
                CombatStatusModifierDirection.DamageTaken,
                DamageModifier.All(0.20d));
            var enemySequence = new EnemyActionSequence(
                "expose-claw",
                "Expose Claw",
                new[]
                {
                    new EnemyActionDefinition(
                        "expose",
                        "Expose",
                        loadCost: 1,
                        resolutionOffset: 1,
                        resolutionEffect: ActionResolutionEffect.ApplyDebuff,
                        statusEffect: exposed,
                        statusTarget: ActionStatusTarget.Opponent),
                    new EnemyActionDefinition(
                        "claw",
                        "Claw",
                        loadCost: 1,
                        resolutionOffset: clawResolutionOffset,
                        damageMultiplier: 0.40d)
                });
            return new SoloComboBattleState(
                player,
                enemy,
                new[] { MainOnlyCombo("status-test", 0, 0, 0, 0.10d) },
                enemySequence,
                manaRecoveryPerTurn: 1,
                staminaRecoveryPerTurn: 2);
        }

        private static ComboConfiguration CompleteCombo(
            string name,
            LinkedActionPlacement placement,
            int cooldown)
        {
            return new ComboConfiguration(
                name,
                Action("opening", ComboActionCategory.Opening, 1, 1, 1, 0, 0d),
                Action("main", ComboActionCategory.Main, 3, 2, 3, cooldown, 0.40d),
                Action("closing", ComboActionCategory.Closing, 1, 0, 0, 0, 0d),
                Action("linked", ComboActionCategory.Linked, 2, 1, 0, 0, 0.20d),
                placement);
        }

        private static ComboConfiguration DefensiveCombo(
            ActionResolutionEffect defensiveEffect,
            int resolutionOffset)
        {
            var defense = new ComboActionDefinition(
                $"{defensiveEffect.ToString().ToLowerInvariant()}-opening",
                defensiveEffect.ToString(),
                ComboActionCategory.Opening,
                loadCost: 2,
                manaCost: 0,
                staminaCost: 1,
                cooldown: 0,
                damageMultiplier: 0d,
                resolutionOffset: resolutionOffset,
                resolutionEffect: defensiveEffect);

            return new ComboConfiguration(
                $"{defensiveEffect.ToString().ToLowerInvariant()}-combo",
                defense,
                Action("main", ComboActionCategory.Main, 1, 0, 0, 0, 0d),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
        }

        private static ComboConfiguration MainOnlyCombo(
            string name,
            int manaCost,
            int staminaCost,
            int cooldown,
            double damageMultiplier)
        {
            return new ComboConfiguration(
                name,
                openingAction: null,
                mainAction: Action(
                    $"{name}-main",
                    ComboActionCategory.Main,
                    load: 1,
                    mana: manaCost,
                    stamina: staminaCost,
                    cooldown,
                    damageMultiplier),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
        }

        private static ComboActionDefinition Action(
            string id,
            ComboActionCategory category,
            int load,
            int mana,
            int stamina,
            int cooldown,
            double damageMultiplier)
        {
            return new ComboActionDefinition(
                id,
                id,
                category,
                load,
                mana,
                stamina,
                cooldown,
                damageMultiplier);
        }
    }
}
