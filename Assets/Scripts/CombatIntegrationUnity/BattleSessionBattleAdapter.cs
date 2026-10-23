using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    public static class BattleSessionBattleAdapter
    {
        public const int MvpPlayerDefense = 50;
        public const int MvpFallbackWeaponAttack = 50;

        public static bool TryCreateBattle(
            BattleSession session,
            PersistentPlayerState playerState,
            out SoloComboBattleState battle,
            out string validationError)
        {
            battle = null;
            if (session == null)
            {
                validationError = "An active Battle Session is required.";
                return false;
            }

            if (session.IsComplete)
            {
                validationError = "The active Battle Session is already complete.";
                return false;
            }

            if (playerState == null)
            {
                validationError = "Persistent Player State is unavailable.";
                return false;
            }

            if (!session.LoadoutSession.TryGetPreparedLoadout(
                    out IReadOnlyList<ComboConfiguration> combos))
            {
                validationError = "The Battle Session no longer has a prepared Combo Loadout.";
                return false;
            }

            if (!session.Encounter.TryCreateDefinition(
                    out SoloComboEncounterDefinition encounter,
                    out validationError))
            {
                return false;
            }

            int attack = session.LoadoutSession.EquippedWeapon?.WeaponAttack
                ?? MvpFallbackWeaponAttack;
            try
            {
                battle = new SoloComboBattleState(
                    PlayerBattleStateTransfer.CreateCombatant(
                        playerState,
                        "Player",
                        attack,
                        MvpPlayerDefense),
                    new CombatantState(
                        encounter.EnemyDisplayName,
                        encounter.EnemyStats),
                    combos,
                    encounter.EnemyBehavior,
                    encounter.ManaRecoveryPerTurn,
                    encounter.StaminaRecoveryPerTurn,
                    enemyRandom: new Random(594),
                    startingItems: PlayerBattleStateTransfer.CaptureInventory(playerState));
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is InvalidOperationException)
            {
                validationError = exception.Message;
                battle = null;
                return false;
            }

            validationError = null;
            return true;
        }

        public static bool TryCommitBattleResult(
            BattleSessionService sessionService,
            BattleSession expectedSession,
            PersistentPlayerState playerState,
            SoloComboBattleState battle,
            out string validationError)
        {
            if (sessionService == null || !sessionService.IsAuthority)
            {
                validationError = "The active BattleSessionService is unavailable.";
                return false;
            }

            if (expectedSession == null
                || !ReferenceEquals(sessionService.CurrentSession, expectedSession))
            {
                validationError = "The battle does not match the active Battle Session.";
                return false;
            }

            if (playerState == null || battle == null)
            {
                validationError = "Player State and Battle State are required.";
                return false;
            }

            if (battle.Outcome == BattleOutcome.Ongoing)
            {
                validationError = "An ongoing battle cannot be committed.";
                return false;
            }

            if (expectedSession.IsComplete)
            {
                validationError = "The active Battle Session is already complete.";
                return false;
            }

            try
            {
                PlayerBattleStateTransfer.ApplyBattleResult(
                    playerState,
                    battle.Player,
                    battle.ItemCounts);
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is InvalidOperationException)
            {
                validationError = exception.Message;
                return false;
            }

            return sessionService.TryCompleteActiveBattle(
                battle.Outcome,
                out validationError);
        }
    }
}
