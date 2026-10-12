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
