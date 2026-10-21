using System.Collections.Generic;
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
        public void ExecuteCombo_MultiEventAction_ResolvesDamageGuardAndDamageInOrder()
        {
            var main = new ComboActionDefinition(
                "multi-main",
                "Multi Main [M]",
                ComboActionCategory.Main,
                timelineCost: 6,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        2,
                        ActionResolutionEffect.Damage,
                        DamageEffectDefinition.Single(DamageType.Slashing, 0.30d)),
                    new ActionResolutionDefinition(4, ActionResolutionEffect.Guard),
                    new ActionResolutionDefinition(
                        5,
                        ActionResolutionEffect.Damage,
                        DamageEffectDefinition.Single(DamageType.Blunt, 0.50d))
                });
            var combo = new ComboConfiguration(
                "combo1",
                openingAction: null,
                mainAction: main,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            SoloComboBattleState battle = CreateBattle(new[] { combo });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.ActionResolutions
                    .Where(resolution => resolution.Side == TimelineActorSide.Player)
                    .Select(resolution => (resolution.TimeStep, resolution.ResolutionEffect)),
                Is.EqualTo(new[]
                {
                    (2, ActionResolutionEffect.Damage),
                    (4, ActionResolutionEffect.Guard),
                    (5, ActionResolutionEffect.Damage)
                }));
            Assert.That(
                result.ActionResolutions
                    .Where(resolution => resolution.Side == TimelineActorSide.Player)
                    .Sum(resolution => resolution.DamageDealt),
                Is.GreaterThan(0));
        }

        [Test]
        public void ExecuteCombo_MultiHitAtSameTime_ResolvesEveryDamageEvent()
        {
            var main = new ComboActionDefinition(
                "multi-hit-main",
                "Multi-Hit Main [M]",
                ComboActionCategory.Main,
                timelineCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        2,
                        ActionResolutionEffect.Damage,
                        DamageEffectDefinition.Single(DamageType.Blunt, 0.60d)),
                    new ActionResolutionDefinition(
                        2,
                        ActionResolutionEffect.Damage,
                        DamageEffectDefinition.Single(DamageType.Blunt, 0.80d))
                });
            var combo = new ComboConfiguration(
                "combo1",
                openingAction: null,
                mainAction: main,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            SoloComboBattleState battle = CreateBattle(new[] { combo });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            SoloComboActionResolution[] hits = result.ActionResolutions
                .Where(resolution => resolution.Side == TimelineActorSide.Player)
                .ToArray();
            Assert.That(result.Succeeded, Is.True);
            Assert.That(hits, Has.Length.EqualTo(2));
            Assert.That(hits.Select(hit => hit.TimeStep), Is.All.EqualTo(2));
            Assert.That(hits.Select(hit => hit.DamageDealt), Is.All.GreaterThan(0));
            Assert.That(
                result.TotalDamageDealt,
                Is.EqualTo(hits.Sum(hit => hit.DamageDealt)));
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

        [TestCase(1, ActionDisruptionType.Flinch, 0)]
        [TestCase(2, ActionDisruptionType.Stagger, (int)ActionDisruptionType.Flinch)]
        [TestCase(3, ActionDisruptionType.Stagger, 0)]
        [TestCase(4, ActionDisruptionType.Knockdown, (int)ActionDisruptionType.Stagger)]
        [TestCase(5, ActionDisruptionType.Knockdown, 0)]
        public void SuperArmorRules_ResolveUsingConfiguredLevel(
            int level,
            ActionDisruptionType incomingType,
            int expectedType)
        {
            var incoming = new ActionDisruptionDefinition(incomingType, duration: 3);

            ActionDisruptionDefinition resolved = SuperArmorRules.Resolve(incoming, level);

            if (expectedType == 0)
            {
                Assert.That(resolved, Is.Null);
                return;
            }

            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.Type, Is.EqualTo((ActionDisruptionType)expectedType));
            Assert.That(resolved.Duration, Is.EqualTo(3));
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
        public void PreviewCombo_BuildsScheduleWithoutChangingBattleState()
        {
            SoloComboBattleState battle = CreateBattle(
                new[] { CompleteCombo("combo1", LinkedActionPlacement.AfterMain, cooldown: 3) });
            int manaBefore = battle.Player.CurrentMana;
            int staminaBefore = battle.Player.CurrentStamina;

            InitialTimelineSchedule preview = battle.PreviewCombo(0);

            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.Actions.Any(
                action => action.Side == TimelineActorSide.Player), Is.True);
            Assert.That(preview.Actions.Any(
                action => action.Side == TimelineActorSide.Enemy), Is.True);
            Assert.That(battle.Player.CurrentMana, Is.EqualTo(manaBefore));
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(staminaBefore));
            Assert.That(battle.GetCooldownRemaining(0), Is.Zero);
            Assert.That(battle.IsTimelineResolving, Is.False);
            Assert.That(battle.ActiveTimeline, Is.Null);
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
        public void AdvanceTimeline_TwoEventWindows_RequestTwoIndependentInputs()
        {
            var focusedGuard = new ComboActionDefinition(
                "focused-guard",
                "Focused Guard I [M]",
                ComboActionCategory.Main,
                timelineCost: 9,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        1,
                        ActionResolutionEffect.Guard,
                        resolutionWindowStart: 1,
                        resolutionWindowEnd: 3),
                    new ActionResolutionDefinition(
                        6,
                        ActionResolutionEffect.Guard,
                        resolutionWindowStart: 6,
                        resolutionWindowEnd: 8)
                });
            SoloComboBattleState battle = CreatePassiveBattle(
                new[] { MainOnlyConfiguration("focused", focusedGuard) },
                playerStamina: 14,
                maximumStamina: 14,
                staminaRecoveryPerTurn: 0);

            Assert.That(battle.BeginCombo(0), Is.EqualTo(SoloComboExecutionFailure.None));
            SoloComboTimelineAdvanceResult firstRequest = battle.AdvanceTimeline();
            Assert.That(firstRequest.RequiresTimingInput, Is.True);
            Assert.That(firstRequest.TimingRequest.ResolutionEventIndex, Is.Zero);
            battle.SelectResolutionPoint(3);
            SoloComboTimelineAdvanceResult secondRequest = null;
            while (battle.IsTimelineResolving && secondRequest == null)
            {
                SoloComboTimelineAdvanceResult advance = battle.AdvanceTimeline();
                if (advance.RequiresTimingInput)
                {
                    secondRequest = advance;
                }
            }

            Assert.That(secondRequest, Is.Not.Null);
            Assert.That(secondRequest.TimingRequest.ResolutionEventIndex, Is.EqualTo(1));
            Assert.That(secondRequest.TimingRequest.MinimumTimeStep, Is.EqualTo(6));
            Assert.That(secondRequest.TimingRequest.MaximumTimeStep, Is.EqualTo(8));
        }

        [Test]
        public void ExecuteCombo_RestoreStaminaEventsResolveAtAuthoredTimesAndCapAtMaximum()
        {
            var breather = new ComboActionDefinition(
                "breather",
                "Breather I [C]",
                ComboActionCategory.Main,
                timelineCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        2,
                        ActionResolutionEffect.RestoreStamina,
                        staminaRecovery: 5),
                    new ActionResolutionDefinition(
                        4,
                        ActionResolutionEffect.RestoreStamina,
                        staminaRecovery: 3)
                });
            SoloComboBattleState battle = CreatePassiveBattle(
                new[] { MainOnlyConfiguration("breather", breather) },
                playerStamina: 4,
                maximumStamina: 10,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.ActionResolutions
                    .Where(item => item.ResolutionEffect == ActionResolutionEffect.RestoreStamina)
                    .Select(item => (item.TimeStep, item.StaminaRestored)),
                Is.EqualTo(new[] { (2, 5), (4, 1) }));
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(10));
        }

        [Test]
        public void VitalityTonicBuff_AffectsCurrentAndNextRoundThenExpires()
        {
            var vitality = new CombatStatusDefinition(
                "vitality",
                "Vitality Tonic",
                CombatStatusDurationType.Rounds,
                duration: 2,
                CombatStatusModifierDirection.DamageDealt,
                DamageModifier.All(0d),
                CombatStatusStackingPolicy.IndependentInstances,
                staminaRecoveryMultiplierBonus: 0.50d);
            var tonic = new ComboActionDefinition(
                "tonic",
                "Use Vitality Tonic [M]",
                ComboActionCategory.Main,
                timelineCost: 2,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        2,
                        ActionResolutionEffect.ApplyBuff,
                        statusEffect: vitality)
                });
            var wait = new ComboActionDefinition(
                "plain",
                "Plain [M]",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(1, ActionResolutionEffect.None)
                });
            SoloComboBattleState battle = CreatePassiveBattle(
                new[]
                {
                    MainOnlyConfiguration("tonic", tonic),
                    MainOnlyConfiguration("plain", wait)
                },
                playerStamina: 0,
                maximumStamina: 30,
                staminaRecoveryPerTurn: 3);

            battle.ExecuteCombo(0);
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(5));
            battle.ExecuteCombo(1);
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(10));
            battle.ExecuteCombo(1);
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(13));
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
        public void ExecuteCombo_IgnoresEvadeDamage_BypassesEvade()
        {
            SoloComboBattleState battle = CreateBattleAgainstTaggedDamage(
                ActionResolutionEffect.Evade,
                DamageResolutionTag.IgnoresEvade);

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
                DamageResolutionTag.IgnoresEvade);
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
                        timelineCost: 1,
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
                timelineCost: 4,
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
                        timelineCost: 5,
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
                timelineCost: 4,
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
                timelineCost: 1,
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
                            timelineCost: 5,
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
        public void OnHitStagger_SuperArmorLevelTwoDowngradesBeforeResponseLookup()
        {
            var breaker = new ComboActionDefinition(
                "breaker",
                "Breaker",
                ComboActionCategory.Main,
                timelineCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 2,
                onHitDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    duration: 2));
            var combo = new ComboConfiguration(
                "armor-test",
                openingAction: null,
                mainAction: breaker,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var superArmor = new ActionActiveWindowDefinition(
                "enemy.super-armor",
                "Super Armor Lv2",
                startOffset: 1,
                endOffset: 5,
                ActiveWindowEffect.SuperArmor,
                damageReduction: 0d,
                ActiveWindowDisruptionPolicy.PersistThroughDisruption,
                superArmorLevel: 2);
            var responses = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.PauseAndResume,
                    ActionDisruptionResponse.CancelAction,
                    ActionDisruptionResponse.CancelAction)
            };
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                new EnemyActionSequence(
                    "armored-advance",
                    "Armored Advance",
                    new[]
                    {
                        new EnemyActionDefinition(
                            "armored-thrust",
                            "Armored Thrust",
                            timelineCost: 5,
                            resolutionOffset: 5,
                            damageMultiplier: 0.40d,
                            activeWindows: new[] { superArmor },
                            disruptionResponseSegments: responses)
                    }),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution playerHit = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Player);
            ScheduledTimelineAction enemyAction = result.Schedule.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy);

            Assert.That(playerHit.AttemptedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Stagger));
            Assert.That(playerHit.AppliedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Flinch));
            Assert.That(playerHit.AppliedDisruption.Duration, Is.EqualTo(2));
            Assert.That(playerHit.SuperArmorLevel, Is.EqualTo(2));
            Assert.That(playerHit.DisruptionResponse,
                Is.EqualTo(ActionDisruptionResponse.PauseAndResume));
            Assert.That(enemyAction.IsCanceled, Is.False);
            Assert.That(enemyAction.AccumulatedPauseDuration, Is.EqualTo(2));
            Assert.That(enemyAction.ResolutionTime, Is.EqualTo(7));
        }

        [Test]
        public void OnHitStagger_RestartActionRepeatsPreviouslyResolvedEffect()
        {
            var breaker = new ComboActionDefinition(
                "breaker",
                "Breaker",
                ComboActionCategory.Main,
                timelineCost: 4,
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
                            timelineCost: 5,
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
                timelineCost: 4,
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
                            timelineCost: 3,
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
                timelineCost: 4,
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
                            timelineCost: 5,
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
        public void SimultaneousClash_ReducesIncomingDamageAndProvidesSuperArmor()
        {
            var clashAction = new ComboActionDefinition(
                "clash",
                "Clash",
                ComboActionCategory.Main,
                timelineCost: 5,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 4,
                damageTags: DamageResolutionTag.Clash,
                clash: new DamageClashDefinition(
                    damageReduction: 0.50d,
                    superArmorLevel: 2));
            var enemySequence = new EnemyActionSequence(
                "clash-test",
                "Clash Test",
                new[]
                {
                    new EnemyActionDefinition(
                        "enemy-strike",
                        "Enemy Strike",
                        timelineCost: 5,
                        resolutionOffset: 4,
                        damageMultiplier: 0.60d,
                        onHitDisruption: new ActionDisruptionDefinition(
                            ActionDisruptionType.Stagger,
                            duration: 2))
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 50)),
                new[]
                {
                    new ComboConfiguration(
                        "clash-combo",
                        openingAction: null,
                        mainAction: clashAction,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null)
                },
                enemySequence,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution playerClash = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Player);
            SoloComboActionResolution enemyStrike = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy);

            Assert.That(playerClash.DamageDealt, Is.EqualTo(17));
            Assert.That(enemyStrike.DamageDealt, Is.EqualTo(12));
            Assert.That(enemyStrike.ClashDamageReduction, Is.EqualTo(0.50d));
            Assert.That(enemyStrike.SuperArmorLevel, Is.EqualTo(2));
            Assert.That(enemyStrike.AttemptedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Stagger));
            Assert.That(enemyStrike.AppliedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Flinch));
            Assert.That(enemyStrike.AppliedDisruption.Duration, Is.EqualTo(2));
        }

        [Test]
        public void SimultaneousClash_WhenBothSidesHaveTag_BothReceiveProtection()
        {
            var playerClash = new ComboActionDefinition(
                "player-clash",
                "Player Clash",
                ComboActionCategory.Main,
                timelineCost: 5,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 4,
                damageTags: DamageResolutionTag.Clash,
                clash: new DamageClashDefinition(0.50d));
            var enemySequence = new EnemyActionSequence(
                "enemy-clash",
                "Enemy Clash",
                new[]
                {
                    new EnemyActionDefinition(
                        "enemy-clash",
                        "Enemy Clash",
                        timelineCost: 5,
                        resolutionOffset: 4,
                        damageMultiplier: 0.60d,
                        damageTags: DamageResolutionTag.Clash,
                        clash: new DamageClashDefinition(0.25d))
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 50)),
                new[]
                {
                    new ComboConfiguration(
                        "player-clash-combo",
                        openingAction: null,
                        mainAction: playerClash,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null)
                },
                enemySequence,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution playerResolution = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Player);
            SoloComboActionResolution enemyResolution = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy);

            Assert.That(playerResolution.DamageDealt, Is.EqualTo(13));
            Assert.That(playerResolution.ClashDamageReduction, Is.EqualTo(0.25d));
            Assert.That(enemyResolution.DamageDealt, Is.EqualTo(12));
            Assert.That(enemyResolution.ClashDamageReduction, Is.EqualTo(0.50d));
        }

        [Test]
        public void NonSimultaneousClash_DoesNotReduceLaterIncomingDamage()
        {
            var clashAction = new ComboActionDefinition(
                "early-clash",
                "Early Clash",
                ComboActionCategory.Main,
                timelineCost: 5,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.50d,
                resolutionOffset: 3,
                damageTags: DamageResolutionTag.Clash,
                clash: new DamageClashDefinition(0.50d, superArmorLevel: 2));
            var enemySequence = new EnemyActionSequence(
                "late-strike",
                "Late Strike",
                new[]
                {
                    new EnemyActionDefinition(
                        "late-strike",
                        "Late Strike",
                        timelineCost: 5,
                        resolutionOffset: 4,
                        damageMultiplier: 0.60d)
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 50)),
                new[]
                {
                    new ComboConfiguration(
                        "early-clash-combo",
                        openingAction: null,
                        mainAction: clashAction,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null)
                },
                enemySequence,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution enemyStrike = result.ActionResolutions.Single(
                resolution => resolution.Side == TimelineActorSide.Enemy);

            Assert.That(enemyStrike.DamageDealt, Is.EqualTo(24));
            Assert.That(enemyStrike.ClashDamageReduction, Is.Zero);
        }

        [Test]
        public void OnHitStagger_GuardedDamageDoesNotDisrupt()
        {
            var guard = new ComboActionDefinition(
                "guard",
                "Guard",
                ComboActionCategory.Opening,
                timelineCost: 3,
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
                    timelineCost: 1,
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
                        timelineCost: 2,
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

        [Test]
        public void OnGuardedFlinch_DisruptsAttackerAndDelaysFollowingAction()
        {
            var quickGuard = new ComboActionDefinition(
                "quick-guard",
                "Quick Guard",
                ComboActionCategory.Opening,
                timelineCost: 2,
                manaCost: 0,
                staminaCost: 1,
                cooldown: 0,
                resolutionOffset: 1,
                resolutionEffect: ActionResolutionEffect.Guard);
            var main = new ComboActionDefinition(
                "counter",
                "Counter",
                ComboActionCategory.Main,
                timelineCost: 2,
                manaCost: 0,
                staminaCost: 1,
                cooldown: 0,
                damageMultiplier: 0.30d,
                resolutionOffset: 2);
            var combo = new ComboConfiguration(
                "guard-counter",
                quickGuard,
                main,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var responseSegments = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    startOffset: 1,
                    endOffset: 4,
                    flinchResponse: ActionDisruptionResponse.PauseAndResume,
                    staggerResponse: ActionDisruptionResponse.Continue,
                    knockdownResponse: ActionDisruptionResponse.CancelAction)
            };
            var enemySequence = new EnemyActionSequence(
                "reckless-assault",
                "Reckless Assault",
                new[]
                {
                    new EnemyActionDefinition(
                        "reckless-lunge",
                        "Reckless Lunge",
                        timelineCost: 4,
                        resolutionOffset: 1,
                        damageMultiplier: 0.50d,
                        disruptionResponseSegments: responseSegments,
                        onGuardedDisruption: new ActionDisruptionDefinition(
                            ActionDisruptionType.Flinch,
                            duration: 2)),
                    new EnemyActionDefinition(
                        "follow-up",
                        "Follow-up",
                        timelineCost: 3,
                        resolutionOffset: 3,
                        damageMultiplier: 0.30d)
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                enemySequence,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution guardedLunge = result.ActionResolutions.Single(
                resolution => resolution.ActionId == "reckless-lunge");
            ScheduledTimelineAction delayedLunge = result.Schedule.Actions.Single(
                action => action.ActionId == "reckless-lunge");
            ScheduledTimelineAction delayedFollowUp = result.Schedule.Actions.Single(
                action => action.ActionId == "follow-up");

            Assert.That(guardedLunge.DamageAvoidance,
                Is.EqualTo(DamageAvoidanceResult.Guarded));
            Assert.That(guardedLunge.DisruptionTrigger,
                Is.EqualTo(ActionDisruptionTrigger.OnGuarded));
            Assert.That(guardedLunge.AppliedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Flinch));
            Assert.That(guardedLunge.DisruptedSide, Is.EqualTo(TimelineActorSide.Enemy));
            Assert.That(guardedLunge.DisruptedSequenceIndex, Is.Zero);
            Assert.That(guardedLunge.DisruptionResponse,
                Is.EqualTo(ActionDisruptionResponse.PauseAndResume));
            Assert.That(delayedLunge.EndTime, Is.EqualTo(6));
            Assert.That(delayedLunge.AccumulatedPauseDuration, Is.EqualTo(2));
            Assert.That(delayedFollowUp.StartTime, Is.EqualTo(6));
            Assert.That(delayedFollowUp.ResolutionTime, Is.EqualTo(9));
            Assert.That(result.Schedule.CompletionTime, Is.EqualTo(9));
        }

        [Test]
        public void OvercommitOnEvadedStagger_CancelsAttackerAndAdvancesFollowingAction()
        {
            var sidestep = new ComboActionDefinition(
                "sidestep",
                "Sidestep",
                ComboActionCategory.Opening,
                timelineCost: 2,
                manaCost: 0,
                staminaCost: 1,
                cooldown: 0,
                resolutionOffset: 1,
                resolutionEffect: ActionResolutionEffect.Evade);
            var combo = new ComboConfiguration(
                "evade-counter",
                sidestep,
                Action(
                    "counter",
                    ComboActionCategory.Main,
                    timelineCost: 1,
                    mana: 0,
                    stamina: 0,
                    cooldown: 0,
                    damageMultiplier: 0.30d),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var enemySequence = new EnemyActionSequence(
                "overcommit-feint",
                "Overcommit Feint",
                new[]
                {
                    new EnemyActionDefinition(
                        "committed-lunge",
                        "Committed Lunge",
                        timelineCost: 4,
                        resolutionOffset: 1,
                        damageMultiplier: 0.60d,
                        damageTags: DamageResolutionTag.Overcommit,
                        disruptionResponseSegments: new[]
                        {
                            new DisruptionResponseSegmentDefinition(
                                startOffset: 1,
                                endOffset: 4,
                                flinchResponse: ActionDisruptionResponse.PauseAndResume,
                                staggerResponse: ActionDisruptionResponse.CancelAction,
                                knockdownResponse: ActionDisruptionResponse.CancelAction)
                        },
                        onEvadedDisruption: new ActionDisruptionDefinition(
                            ActionDisruptionType.Stagger,
                            duration: 2)),
                    new EnemyActionDefinition(
                        "follow-up",
                        "Follow-up",
                        timelineCost: 3,
                        resolutionOffset: 3,
                        damageMultiplier: 0.35d)
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                enemySequence,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution evadedLunge = result.ActionResolutions.Single(
                resolution => resolution.ActionId == "committed-lunge");
            ScheduledTimelineAction canceledLunge = result.Schedule.Actions.Single(
                action => action.ActionId == "committed-lunge");
            ScheduledTimelineAction advancedFollowUp = result.Schedule.Actions.Single(
                action => action.ActionId == "follow-up");

            Assert.That(evadedLunge.DamageAvoidance,
                Is.EqualTo(DamageAvoidanceResult.Evaded));
            Assert.That(evadedLunge.DisruptionTrigger,
                Is.EqualTo(ActionDisruptionTrigger.OnEvaded));
            Assert.That(evadedLunge.AppliedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Stagger));
            Assert.That(evadedLunge.DisruptedSide, Is.EqualTo(TimelineActorSide.Enemy));
            Assert.That(evadedLunge.DisruptionResponse,
                Is.EqualTo(ActionDisruptionResponse.CancelAction));
            Assert.That(canceledLunge.IsCanceled, Is.True);
            Assert.That(canceledLunge.Cancellation.RecoveryEndTime, Is.EqualTo(3));
            Assert.That(advancedFollowUp.StartTime, Is.EqualTo(3));
            Assert.That(advancedFollowUp.ResolutionTime, Is.EqualTo(6));
            Assert.That(result.Schedule.CompletionTime, Is.EqualTo(6));
        }

        [Test]
        public void SustainedAttack_EachHitChecksEvadeIndependently()
        {
            var evade = new ComboActionDefinition(
                "evade",
                "Evade",
                ComboActionCategory.Opening,
                timelineCost: 2,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionOffset: 1,
                resolutionEffect: ActionResolutionEffect.Evade);
            ComboConfiguration combo = new ComboConfiguration(
                "combo1",
                evade,
                new ComboActionDefinition(
                    "main",
                    "Main",
                    ComboActionCategory.Main,
                    timelineCost: 2,
                    manaCost: 0,
                    staminaCost: 0,
                    cooldown: 0),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            EnemyActionSequence enemy = new EnemyActionSequence(
                "sustained",
                "Sustained",
                new[]
                {
                    new EnemyActionDefinition(
                        "sweep",
                        "Sweep",
                        timelineCost: 4,
                        resolutionOffset: 1,
                        damageMultiplier: 0.30d,
                        sustainedAttack: new SustainedAttackDefinition(1, 3, 1, 2, 2))
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[] { combo },
                enemy,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution[] hits = result.ActionResolutions
                .Where(item => item.ActionId == "sweep")
                .ToArray();

            Assert.That(hits, Has.Length.EqualTo(2));
            Assert.That(hits[0].TimeStep, Is.EqualTo(1));
            Assert.That(hits[0].DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.Evaded));
            Assert.That(hits[0].DamageDealt, Is.Zero);
            Assert.That(hits[1].TimeStep, Is.EqualTo(3));
            Assert.That(hits[1].DamageAvoidance, Is.EqualTo(DamageAvoidanceResult.None));
            Assert.That(hits[1].DamageDealt, Is.GreaterThan(0));
        }

        [Test]
        public void SustainedAttack_CancelActionPreventsLaterHits()
        {
            var breaker = new ComboActionDefinition(
                "breaker",
                "Breaker",
                ComboActionCategory.Main,
                timelineCost: 4,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.30d,
                resolutionOffset: 1,
                onHitDisruption: new ActionDisruptionDefinition(
                    ActionDisruptionType.Stagger,
                    duration: 1));
            var enemy = new EnemyActionSequence(
                "sustained",
                "Sustained",
                new[]
                {
                    new EnemyActionDefinition(
                        "sweep",
                        "Sweep",
                        timelineCost: 4,
                        resolutionOffset: 1,
                        damageMultiplier: 0.30d,
                        disruptionResponseSegments: new[]
                        {
                            new DisruptionResponseSegmentDefinition(
                                1,
                                4,
                                ActionDisruptionResponse.Continue,
                                ActionDisruptionResponse.CancelAction,
                                ActionDisruptionResponse.CancelAction)
                        },
                        sustainedAttack: new SustainedAttackDefinition(1, 3, 1, 2, 2))
                });
            var battle = new SoloComboBattleState(
                new CombatantState("Player", new CombatantStats(100, 30, 30, 50, 50)),
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 60, 60)),
                new[]
                {
                    new ComboConfiguration(
                        "combo1",
                        openingAction: null,
                        breaker,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null)
                },
                enemy,
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(
                result.ActionResolutions.Count(item => item.ActionId == "sweep"),
                Is.EqualTo(1));
            Assert.That(
                result.Schedule.Actions.Single(item => item.ActionId == "sweep").IsCanceled,
                Is.True);
        }

        [Test]
        public void HealingItem_RequiresInventoryAndConsumesAtResolution()
        {
            var potion = new ActionItemCostDefinition(
                "healing-potion",
                "Healing Potion",
                amount: 1);
            var drinkPotion = new ComboActionDefinition(
                "use-healing-potion-main",
                "Use Healing Potion / M",
                ComboActionCategory.Main,
                timelineCost: 6,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        offset: 5,
                        effect: ActionResolutionEffect.Heal,
                        healingAmount: 70,
                        itemCost: potion)
                });
            var combo = new ComboConfiguration(
                "potion-combo",
                openingAction: null,
                mainAction: drinkPotion,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var player = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 30, 50, 50));
            player.ReceiveDamage(80);
            var battle = new SoloComboBattleState(
                player,
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 50, 50)),
                new[] { combo },
                PassiveEnemySequence(),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0,
                startingItems: new Dictionary<string, int>
                {
                    { "healing-potion", 1 }
                });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(player.CurrentHealth, Is.EqualTo(90));
            Assert.That(battle.GetItemCount("healing-potion"), Is.Zero);
            Assert.That(
                result.ActionResolutions.Single(item =>
                    item.ResolutionEffect == ActionResolutionEffect.Heal).HealingDone,
                Is.EqualTo(70));
            Assert.That(
                battle.GetExecutionFailure(0),
                Is.EqualTo(SoloComboExecutionFailure.InsufficientItems));
        }

        [Test]
        public void PercentageHealing_UsesMaximumHealthAndRoundsAwayFromZero()
        {
            var heal = new ComboActionDefinition(
                "percentage-heal",
                "Percentage Heal",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        offset: 1,
                        effect: ActionResolutionEffect.Heal,
                        healing: ActionHealingDefinition.MaximumHealthPercentage(0.25d))
                });
            var combo = new ComboConfiguration(
                "healing-combo",
                openingAction: null,
                mainAction: heal,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var player = new CombatantState(
                "Player",
                new CombatantStats(102, 30, 30, 50, 50));
            player.ReceiveDamage(50);
            var battle = new SoloComboBattleState(
                player,
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 50, 50)),
                new[] { combo },
                PassiveEnemySequence(),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            SoloComboActionResolution resolution = result.ActionResolutions.Single(item =>
                item.ResolutionEffect == ActionResolutionEffect.Heal);

            Assert.That(resolution.HealingDone, Is.EqualTo(26));
            Assert.That(player.CurrentHealth, Is.EqualTo(78));
        }

        [Test]
        public void MultiStepHealing_ConsumesPotionOnlyAtFirstHeal()
        {
            var potion = new ActionItemCostDefinition(
                "healing-potion",
                "Healing Potion",
                amount: 1);
            var drinkPotion = new ComboActionDefinition(
                "multi-step-potion",
                "Multi-step Potion",
                ComboActionCategory.Main,
                timelineCost: 6,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        offset: 3,
                        effect: ActionResolutionEffect.Heal,
                        healing: ActionHealingDefinition.MaximumHealthPercentage(0.25d),
                        itemCost: potion),
                    new ActionResolutionDefinition(
                        offset: 6,
                        effect: ActionResolutionEffect.Heal,
                        healing: ActionHealingDefinition.MaximumHealthPercentage(0.10d))
                });
            var combo = new ComboConfiguration(
                "potion-combo",
                openingAction: null,
                mainAction: drinkPotion,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var player = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 30, 50, 50));
            player.ReceiveDamage(80);
            var battle = new SoloComboBattleState(
                player,
                new CombatantState("Enemy", new CombatantStats(100, 0, 0, 50, 50)),
                new[] { combo },
                PassiveEnemySequence(),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: 0,
                startingItems: new Dictionary<string, int>
                {
                    { "healing-potion", 1 }
                });

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(player.CurrentHealth, Is.EqualTo(55));
            Assert.That(battle.GetItemCount("healing-potion"), Is.Zero);
            Assert.That(
                result.ActionResolutions.Count(item =>
                    item.ResolutionEffect == ActionResolutionEffect.Heal),
                Is.EqualTo(2));
        }

        private static EnemyActionSequence PassiveEnemySequence()
        {
            return new EnemyActionSequence(
                "observe",
                "Observe",
                new[]
                {
                    new EnemyActionDefinition(
                        "observe-action",
                        "Observe",
                        timelineCost: 1,
                        resolutionOffset: 1,
                        damageMultiplier: 0d)
                });
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
                timelineCost: 5,
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
                        timelineCost: 2,
                        resolutionOffset: 2,
                        damageMultiplier: 0.40d),
                    new EnemyActionDefinition(
                        "second-strike",
                        "Second Strike",
                        timelineCost: 3,
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
                timelineCost: 3,
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
                        timelineCost: 1,
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
                timelineCost: 2,
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
                timelineCost: 2,
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
                        timelineCost: 1,
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
                        timelineCost: 2,
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
                        timelineCost: 1,
                        resolutionOffset: 1,
                        resolutionEffect: ActionResolutionEffect.ApplyDebuff,
                        statusEffect: exposed,
                        statusTarget: ActionStatusTarget.Opponent),
                    new EnemyActionDefinition(
                        "claw",
                        "Claw",
                        timelineCost: 1,
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
                timelineCost: 2,
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
                    timelineCost: 1,
                    mana: manaCost,
                    stamina: staminaCost,
                    cooldown,
                    damageMultiplier),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
        }

        private static ComboConfiguration MainOnlyConfiguration(
            string name,
            ComboActionDefinition main)
        {
            return new ComboConfiguration(
                name,
                openingAction: null,
                mainAction: main,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
        }

        private static SoloComboBattleState CreatePassiveBattle(
            ComboConfiguration[] combos,
            int playerStamina,
            int maximumStamina,
            int staminaRecoveryPerTurn)
        {
            var player = new CombatantState(
                "Player",
                new CombatantStats(100, 30, maximumStamina, 50, 50));
            player.Pay(manaCost: 0, staminaCost: maximumStamina - playerStamina);
            return new SoloComboBattleState(
                player,
                new CombatantState(
                    "Training Enemy",
                    new CombatantStats(100, 0, 0, 60, 60)),
                combos,
                PassiveEnemySequence(),
                manaRecoveryPerTurn: 0,
                staminaRecoveryPerTurn: staminaRecoveryPerTurn);
        }

        private static ComboActionDefinition Action(
            string id,
            ComboActionCategory category,
            int timelineCost,
            int mana,
            int stamina,
            int cooldown,
            double damageMultiplier)
        {
            return new ComboActionDefinition(
                id,
                id,
                category,
                timelineCost,
                mana,
                stamina,
                cooldown,
                damageMultiplier);
        }
    }
}
