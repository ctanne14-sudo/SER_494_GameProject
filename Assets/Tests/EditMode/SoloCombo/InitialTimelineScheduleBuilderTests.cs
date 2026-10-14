using System;
using System.Linq;
using NUnit.Framework;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    public sealed class InitialTimelineScheduleBuilderTests
    {
        [Test]
        public void Build_AccumulatesEachSideIndependentlyAndUsesLocalResolutionOffsets()
        {
            ComboConfiguration playerCombo = new ComboConfiguration(
                "combo1",
                PlayerAction("opening", ComboActionCategory.Opening, load: 2, resolution: 1),
                PlayerAction("main", ComboActionCategory.Main, load: 4, resolution: 3),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            EnemyActionSequence enemySequence = EnemySequence(
                EnemyAction("enemy-first", load: 3, resolution: 2),
                EnemyAction("enemy-second", load: 2, resolution: 2));

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.Build(
                playerCombo,
                enemySequence);

            ScheduledTimelineAction[] player = schedule.Actions
                .Where(action => action.Side == TimelineActorSide.Player)
                .ToArray();
            ScheduledTimelineAction[] enemy = schedule.Actions
                .Where(action => action.Side == TimelineActorSide.Enemy)
                .ToArray();

            Assert.That(
                player.Select(action => (action.StartTime, action.EndTime, action.ResolutionTime)),
                Is.EqualTo(new[] { (0, 2, 1), (2, 6, 5) }));
            Assert.That(
                enemy.Select(action => (action.StartTime, action.EndTime, action.ResolutionTime)),
                Is.EqualTo(new[] { (0, 3, 2), (3, 5, 5) }));
            Assert.That(schedule.CompletionTime, Is.EqualTo(6));
        }

        [Test]
        public void Build_GroupsBothSidesAtSameTimeStepWithoutResolvingEitherFirst()
        {
            ComboConfiguration playerCombo = MainOnlyCombo(
                PlayerAction("player-strike", ComboActionCategory.Main, load: 3, resolution: 2));
            EnemyActionSequence enemySequence = EnemySequence(
                EnemyAction("enemy-strike", load: 4, resolution: 2));

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.Build(
                playerCombo,
                enemySequence);

            TimelineResolutionStep sharedStep = schedule.ResolutionSteps.Single();
            Assert.That(sharedStep.TimeStep, Is.EqualTo(2));
            Assert.That(sharedStep.Actions, Has.Count.EqualTo(2));
            Assert.That(
                sharedStep.Actions.Select(action => action.Side),
                Is.EquivalentTo(new[] { TimelineActorSide.Player, TimelineActorSide.Enemy }));
        }

        [Test]
        public void ComboAction_DefaultResolutionOffset_UsesEndOfAction()
        {
            var action = new ComboActionDefinition(
                "main",
                "Main",
                ComboActionCategory.Main,
                loadCost: 5,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0);

            Assert.That(action.ResolutionOffset, Is.EqualTo(5));
        }

        [Test]
        public void ComboAction_GuardResolution_IsCarriedIntoTimeline()
        {
            var guard = new ComboActionDefinition(
                "guard",
                "Guard",
                ComboActionCategory.Opening,
                loadCost: 3,
                manaCost: 0,
                staminaCost: 1,
                cooldown: 0,
                damageMultiplier: 0d,
                resolutionOffset: 2,
                resolutionEffect: ActionResolutionEffect.Guard);
            ComboConfiguration combo = new ComboConfiguration(
                "combo1",
                guard,
                PlayerAction("main", ComboActionCategory.Main, load: 2, resolution: 2),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.Build(
                combo,
                EnemySequence(EnemyAction("enemy", load: 2, resolution: 2)));

            ScheduledTimelineAction scheduledGuard = schedule.Actions.Single(
                action => action.Side == TimelineActorSide.Player
                    && action.ActionId == "guard");
            Assert.That(scheduledGuard.ResolutionTime, Is.EqualTo(2));
            Assert.That(scheduledGuard.ResolutionEffect, Is.EqualTo(ActionResolutionEffect.Guard));
            Assert.That(scheduledGuard.HasDamage, Is.False);
        }

        [Test]
        public void ComboAction_DamageResolutionWithoutDamage_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ComboActionDefinition(
                "invalid",
                "Invalid",
                ComboActionCategory.Main,
                loadCost: 2,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0d,
                resolutionOffset: 1,
                resolutionEffect: ActionResolutionEffect.Damage));
        }

        [Test]
        public void Build_SameCharacterDamageAndGuardAtSameTime_IsRejected()
        {
            var damage = new ComboActionDefinition(
                "opening-damage",
                "Opening Damage",
                ComboActionCategory.Opening,
                loadCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.20d,
                resolutionOffset: 1);
            var guard = new ComboActionDefinition(
                "main-guard",
                "Main Guard",
                ComboActionCategory.Main,
                loadCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0d,
                resolutionOffset: 0,
                resolutionEffect: ActionResolutionEffect.Guard);
            var combo = new ComboConfiguration(
                "invalid-overlap",
                damage,
                guard,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);

            Assert.Throws<InvalidOperationException>(() => InitialTimelineScheduleBuilder.Build(
                combo,
                EnemySequence(EnemyAction("enemy", load: 2, resolution: 2))));
        }

        [Test]
        public void ActionDefinitions_RejectInvalidLoadAndResolutionOffsets()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ComboActionDefinition(
                "main",
                "Main",
                ComboActionCategory.Main,
                loadCost: 0,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyAction(
                "enemy",
                load: 3,
                resolution: 4));
        }

        [Test]
        public void EnemySequence_RequiresAtLeastOneAction()
        {
            Assert.Throws<ArgumentException>(() => new EnemyActionSequence(
                "empty",
                "Empty",
                Array.Empty<EnemyActionDefinition>()));
        }

        [Test]
        public void BuildEnemyOnly_CreatesIntentScheduleWithoutPlayerActions()
        {
            EnemyActionSequence enemySequence = EnemySequence(
                EnemyAction("enemy-first", load: 3, resolution: 2),
                EnemyAction("enemy-second", load: 2, resolution: 1));

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.BuildEnemyOnly(
                enemySequence);

            Assert.That(schedule.Actions, Has.Count.EqualTo(2));
            Assert.That(
                schedule.Actions.All(action => action.Side == TimelineActorSide.Enemy),
                Is.True);
            Assert.That(
                schedule.Actions.Select(action => action.ResolutionTime),
                Is.EqualTo(new[] { 2, 4 }));
            Assert.That(schedule.CompletionTime, Is.EqualTo(5));
        }

        [Test]
        public void Build_ConvertsActiveWindowFromActionLocalTimeToTimelineTime()
        {
            var activeWindow = new ActionActiveWindowDefinition(
                "focus.defense",
                "Focused Defense",
                startOffset: 1,
                endOffset: 3,
                ActiveWindowEffect.DefensiveState,
                damageReduction: 0.30d,
                ActiveWindowDisruptionPolicy.PersistThroughDisruption);
            var opening = new ComboActionDefinition(
                "focus",
                "Focus",
                ComboActionCategory.Opening,
                loadCost: 3,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                activeWindows: new[] { activeWindow });
            ComboConfiguration combo = new ComboConfiguration(
                "combo1",
                opening,
                PlayerAction("main", ComboActionCategory.Main, load: 2, resolution: 2),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.Build(
                combo,
                EnemySequence(EnemyAction("enemy", load: 2, resolution: 2)));

            ScheduledActiveWindow scheduledWindow = schedule.Actions
                .Single(action => action.ActionId == "focus")
                .ActiveWindows.Single();
            Assert.That(scheduledWindow.StartTime, Is.EqualTo(1));
            Assert.That(scheduledWindow.EndTime, Is.EqualTo(3));
            Assert.That(scheduledWindow.IsActiveAt(1), Is.True);
            Assert.That(scheduledWindow.IsActiveAt(3), Is.True);
            Assert.That(scheduledWindow.IsActiveAt(4), Is.False);
            Assert.That(scheduledWindow.DamageReduction, Is.EqualTo(0.30d));
        }

        [Test]
        public void ComboAction_ActiveWindowOutsideActionLoad_IsRejected()
        {
            var activeWindow = new ActionActiveWindowDefinition(
                "invalid.window",
                "Invalid Window",
                startOffset: 1,
                endOffset: 4,
                ActiveWindowEffect.DefensiveState,
                damageReduction: 0.30d,
                ActiveWindowDisruptionPolicy.PersistThroughDisruption);

            Assert.Throws<ArgumentException>(() => new ComboActionDefinition(
                "main",
                "Main",
                ComboActionCategory.Main,
                loadCost: 3,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                activeWindows: new[] { activeWindow }));
        }

        [Test]
        public void ApplyPauseAndResume_ShiftsUnresolvedActionAndLaterActions()
        {
            ComboConfiguration combo = MainOnlyCombo(
                PlayerAction("player", ComboActionCategory.Main, load: 4, resolution: 2));
            var pauseResponse = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.PauseAndResume,
                    ActionDisruptionResponse.PauseAndResume)
            };
            EnemyActionSequence enemy = EnemySequence(
                new EnemyActionDefinition(
                    "heavy",
                    "heavy",
                    loadCost: 5,
                    resolutionOffset: 5,
                    damageMultiplier: 0.50d,
                    disruptionResponseSegments: pauseResponse),
                EnemyAction("follow-up", load: 2, resolution: 2));
            InitialTimelineSchedule original = InitialTimelineScheduleBuilder.Build(combo, enemy);

            InitialTimelineSchedule delayed = InitialTimelineScheduleBuilder.ApplyPauseAndResume(
                original,
                TimelineActorSide.Enemy,
                sequenceIndex: 0,
                disruptionTime: 2,
                duration: 2,
                disruptionType: ActionDisruptionType.Stagger);

            ScheduledTimelineAction heavy = delayed.Actions.Single(
                action => action.ActionId == "heavy");
            ScheduledTimelineAction followUp = delayed.Actions.Single(
                action => action.ActionId == "follow-up");
            Assert.That(heavy.StartTime, Is.Zero);
            Assert.That(heavy.EndTime, Is.EqualTo(7));
            Assert.That(heavy.ResolutionTime, Is.EqualTo(7));
            Assert.That(heavy.AccumulatedPauseDuration, Is.EqualTo(2));
            Assert.That(heavy.DisruptionPauses, Has.Count.EqualTo(1));
            Assert.That(heavy.DisruptionPauses[0].Type,
                Is.EqualTo(ActionDisruptionType.Stagger));
            Assert.That(heavy.DisruptionPauses[0].StartTime, Is.EqualTo(2));
            Assert.That(heavy.DisruptionPauses[0].EndTime, Is.EqualTo(4));
            Assert.That(followUp.StartTime, Is.EqualTo(7));
            Assert.That(followUp.EndTime, Is.EqualTo(9));
            Assert.That(delayed.CompletionTime, Is.EqualTo(9));
        }

        [Test]
        public void ApplyRestartAction_RestartsWholeActionAndShiftsLaterActions()
        {
            ComboConfiguration combo = MainOnlyCombo(
                PlayerAction("player", ComboActionCategory.Main, load: 4, resolution: 2));
            var restartResponse = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.RestartAction,
                    ActionDisruptionResponse.RestartAction)
            };
            EnemyActionSequence enemy = EnemySequence(
                new EnemyActionDefinition(
                    "heavy",
                    "heavy",
                    loadCost: 5,
                    resolutionOffset: 1,
                    damageMultiplier: 0.50d,
                    disruptionResponseSegments: restartResponse),
                EnemyAction("follow-up", load: 2, resolution: 2));
            InitialTimelineSchedule original = InitialTimelineScheduleBuilder.Build(combo, enemy);

            InitialTimelineSchedule restarted = InitialTimelineScheduleBuilder.ApplyRestartAction(
                original,
                TimelineActorSide.Enemy,
                sequenceIndex: 0,
                disruptionTime: 2,
                duration: 2,
                disruptionType: ActionDisruptionType.Stagger);

            ScheduledTimelineAction heavy = restarted.Actions.Single(
                action => action.ActionId == "heavy");
            ScheduledTimelineAction followUp = restarted.Actions.Single(
                action => action.ActionId == "follow-up");
            Assert.That(heavy.StartTime, Is.EqualTo(4));
            Assert.That(heavy.EndTime, Is.EqualTo(9));
            Assert.That(heavy.ResolutionTime, Is.EqualTo(5));
            Assert.That(heavy.AccumulatedPauseDuration, Is.Zero);
            Assert.That(heavy.Restarts, Has.Count.EqualTo(1));
            Assert.That(heavy.Restarts[0].PreviousStartTime, Is.Zero);
            Assert.That(heavy.Restarts[0].DisruptionTime, Is.EqualTo(2));
            Assert.That(heavy.Restarts[0].RestartTime, Is.EqualTo(4));
            Assert.That(followUp.StartTime, Is.EqualTo(9));
            Assert.That(followUp.EndTime, Is.EqualTo(11));
            Assert.That(restarted.CompletionTime, Is.EqualTo(11));
        }

        [Test]
        public void ApplyCancelAction_CancelsDependentLinkedAndAdvancesClosing()
        {
            var main = PlayerAction("main", ComboActionCategory.Main, load: 5, resolution: 5);
            var linked = PlayerAction(
                "linked",
                ComboActionCategory.Linked,
                load: 2,
                resolution: 2);
            var closing = PlayerAction(
                "closing",
                ComboActionCategory.Closing,
                load: 2,
                resolution: 2);
            var combo = new ComboConfiguration(
                "linked-combo",
                openingAction: null,
                main,
                closing,
                linked,
                LinkedActionPlacement.AfterMain);
            InitialTimelineSchedule original = InitialTimelineScheduleBuilder.Build(
                combo,
                EnemySequence(EnemyAction("observe", load: 1, resolution: 1)));

            InitialTimelineSchedule canceled = InitialTimelineScheduleBuilder.ApplyCancelAction(
                original,
                TimelineActorSide.Player,
                sequenceIndex: 0,
                disruptionTime: 2,
                duration: 2,
                disruptionType: ActionDisruptionType.Knockdown);

            ScheduledTimelineAction canceledMain = canceled.Actions.Single(
                action => action.ActionId == "main");
            ScheduledTimelineAction canceledLinked = canceled.Actions.Single(
                action => action.ActionId == "linked");
            ScheduledTimelineAction advancedClosing = canceled.Actions.Single(
                action => action.ActionId == "closing");
            Assert.That(canceledMain.IsCanceled, Is.True);
            Assert.That(canceledMain.Cancellation.RecoveryEndTime, Is.EqualTo(4));
            Assert.That(canceledLinked.RequiredCompletedSequenceIndex, Is.EqualTo(0));
            Assert.That(canceledLinked.IsCanceled, Is.True);
            Assert.That(canceledLinked.Cancellation.DependencyFailed, Is.True);
            Assert.That(advancedClosing.StartTime, Is.EqualTo(4));
            Assert.That(advancedClosing.EndTime, Is.EqualTo(6));
            string[] resolvedActionIds = canceled.ResolutionSteps
                .SelectMany(step => step.Actions)
                .Select(action => action.ActionId)
                .ToArray();
            Assert.That(resolvedActionIds, Does.Not.Contain("main"));
            Assert.That(resolvedActionIds, Does.Not.Contain("linked"));
            Assert.That(canceled.CompletionTime, Is.EqualTo(6));
        }

        [Test]
        public void ApplyPauseAndResume_PersistWindowContinuesDuringDisruption()
        {
            InitialTimelineSchedule delayed = ApplyPauseToDefensiveWindow(
                ActiveWindowDisruptionPolicy.PersistThroughDisruption);
            ScheduledActiveWindow window = delayed.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy).ActiveWindows.Single();

            Assert.That(window.IsActiveAt(2), Is.True);
            Assert.That(window.IsActiveAt(3), Is.True);
            Assert.That(window.IsActiveAt(4), Is.True);
            Assert.That(window.IsActiveAt(5), Is.True);
            Assert.That(window.IsActiveAt(6), Is.False);
        }

        [Test]
        public void ApplyPauseAndResume_BreakWindowEndsAfterDisruptionStep()
        {
            InitialTimelineSchedule delayed = ApplyPauseToDefensiveWindow(
                ActiveWindowDisruptionPolicy.BreakOnDisruption);
            ScheduledActiveWindow window = delayed.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy).ActiveWindows.Single();

            Assert.That(window.BreakTime, Is.EqualTo(2));
            Assert.That(window.IsActiveAt(2), Is.True);
            Assert.That(window.IsActiveAt(3), Is.False);
            Assert.That(window.EffectiveEndTime, Is.EqualTo(2));
        }

        [Test]
        public void ApplyPauseAndResume_SuppressWindowReactivatesWhenTimeRemains()
        {
            InitialTimelineSchedule delayed = ApplyPauseToDefensiveWindow(
                ActiveWindowDisruptionPolicy.SuppressDuringDisruption);
            ScheduledActiveWindow window = delayed.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy).ActiveWindows.Single();

            Assert.That(window.Suppressions, Has.Count.EqualTo(1));
            Assert.That(window.IsActiveAt(2), Is.True);
            Assert.That(window.IsActiveAt(3), Is.False);
            Assert.That(window.IsActiveAt(4), Is.False);
            Assert.That(window.IsActiveAt(5), Is.True);
        }

        [Test]
        public void ApplyRestartAction_BreaksCurrentWindowAndReplaysFreshWindow()
        {
            InitialTimelineSchedule original = BuildDefensiveWindowSchedule(
                ActiveWindowDisruptionPolicy.BreakOnDisruption,
                ActionDisruptionResponse.RestartAction);

            InitialTimelineSchedule restarted = InitialTimelineScheduleBuilder.ApplyRestartAction(
                original,
                TimelineActorSide.Enemy,
                sequenceIndex: 0,
                disruptionTime: 2,
                duration: 2,
                disruptionType: ActionDisruptionType.Stagger);

            ScheduledActiveWindow[] windows = restarted.Actions.Single(
                    action => action.Side == TimelineActorSide.Enemy)
                .ActiveWindows.ToArray();
            Assert.That(windows, Has.Length.EqualTo(2));
            Assert.That(windows[0].BreakTime, Is.EqualTo(2));
            Assert.That(windows[0].IsActiveAt(3), Is.False);
            Assert.That(windows[1].StartTime, Is.EqualTo(5));
            Assert.That(windows[1].EndTime, Is.EqualTo(9));
            Assert.That(windows[1].IsActiveAt(5), Is.True);
        }

        [Test]
        public void ActionDefinition_ResponseSegmentsWithGap_AreRejected()
        {
            var segments = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    2,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.PauseAndResume,
                    ActionDisruptionResponse.PauseAndResume),
                new DisruptionResponseSegmentDefinition(
                    4,
                    5,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.Continue,
                    ActionDisruptionResponse.PauseAndResume)
            };

            Assert.Throws<ArgumentException>(() => new EnemyActionDefinition(
                "invalid",
                "Invalid",
                loadCost: 5,
                resolutionOffset: 5,
                damageMultiplier: 0.20d,
                disruptionResponseSegments: segments));
        }

        [Test]
        public void Build_SustainedAttack_CreatesIndependentResolutionStepsInsideWindow()
        {
            var sustained = new EnemyActionDefinition(
                "sweep",
                "Sweep",
                loadCost: 6,
                resolutionOffset: 1,
                damageMultiplier: 0.30d,
                sustainedAttack: new SustainedAttackDefinition(
                    windowStartOffset: 2,
                    windowEndOffset: 6,
                    firstHitOffset: 2,
                    hitInterval: 2,
                    maximumHitCount: 3));

            InitialTimelineSchedule schedule = InitialTimelineScheduleBuilder.Build(
                MainOnlyCombo(
                    PlayerAction("player", ComboActionCategory.Main, load: 6, resolution: 6)),
                EnemySequence(sustained));

            ScheduledTimelineAction enemy = schedule.Actions.Single(
                action => action.Side == TimelineActorSide.Enemy);
            Assert.That(enemy.ResolutionTimes, Is.EqualTo(new[] { 2, 4, 6 }));
            Assert.That(
                schedule.ResolutionSteps
                    .Where(step => step.Actions.Contains(enemy))
                    .Select(step => step.TimeStep),
                Is.EqualTo(new[] { 2, 4, 6 }));
        }

        [Test]
        public void Build_SustainedAttackOverlappingOwnGuard_IsRejected()
        {
            var sustained = new ComboActionDefinition(
                "sustained",
                "Sustained",
                ComboActionCategory.Opening,
                loadCost: 3,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.20d,
                resolutionOffset: 1,
                sustainedAttack: new SustainedAttackDefinition(1, 3, 1, 2, 2));
            var guard = new ComboActionDefinition(
                "guard",
                "Guard",
                ComboActionCategory.Main,
                loadCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionOffset: 0,
                resolutionEffect: ActionResolutionEffect.Guard);

            Assert.Throws<InvalidOperationException>(() =>
                InitialTimelineScheduleBuilder.Build(
                    new ComboConfiguration(
                        "combo1",
                        sustained,
                        guard,
                        closingAction: null,
                        linkedAction: null,
                        linkedActionPlacement: null),
                    EnemySequence(EnemyAction("enemy", load: 4, resolution: 4))));
        }

        private static InitialTimelineSchedule ApplyPauseToDefensiveWindow(
            ActiveWindowDisruptionPolicy policy)
        {
            InitialTimelineSchedule original = BuildDefensiveWindowSchedule(
                policy,
                ActionDisruptionResponse.PauseAndResume);
            return InitialTimelineScheduleBuilder.ApplyPauseAndResume(
                original,
                TimelineActorSide.Enemy,
                sequenceIndex: 0,
                disruptionTime: 2,
                duration: 2,
                disruptionType: ActionDisruptionType.Stagger);
        }

        private static InitialTimelineSchedule BuildDefensiveWindowSchedule(
            ActiveWindowDisruptionPolicy policy,
            ActionDisruptionResponse response)
        {
            var window = new ActionActiveWindowDefinition(
                "enemy.guard",
                "Enemy Guard",
                startOffset: 1,
                endOffset: 5,
                ActiveWindowEffect.DefensiveState,
                damageReduction: 0.30d,
                policy);
            var responses = new[]
            {
                new DisruptionResponseSegmentDefinition(
                    1,
                    5,
                    ActionDisruptionResponse.Continue,
                    response,
                    response)
            };
            return InitialTimelineScheduleBuilder.Build(
                MainOnlyCombo(
                    PlayerAction("player", ComboActionCategory.Main, load: 4, resolution: 2)),
                EnemySequence(
                    new EnemyActionDefinition(
                        "guarding-enemy",
                        "Guarding Enemy",
                        loadCost: 5,
                        resolutionOffset: 5,
                        damageMultiplier: 0.50d,
                        activeWindows: new[] { window },
                        disruptionResponseSegments: responses)));
        }

        private static ComboConfiguration MainOnlyCombo(ComboActionDefinition main)
        {
            return new ComboConfiguration(
                "combo1",
                openingAction: null,
                main,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
        }

        private static ComboActionDefinition PlayerAction(
            string id,
            ComboActionCategory category,
            int load,
            int resolution)
        {
            return new ComboActionDefinition(
                id,
                id,
                category,
                load,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.10d,
                resolutionOffset: resolution);
        }

        private static EnemyActionDefinition EnemyAction(
            string id,
            int load,
            int resolution)
        {
            return new EnemyActionDefinition(
                id,
                id,
                load,
                resolution,
                damageMultiplier: 0.10d);
        }

        private static EnemyActionSequence EnemySequence(params EnemyActionDefinition[] actions)
        {
            return new EnemyActionSequence("enemy-sequence", "Enemy Sequence", actions);
        }
    }
}
