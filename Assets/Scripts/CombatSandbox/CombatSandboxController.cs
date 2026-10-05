using System.Collections.Generic;
using SER494.Gameplay.Combat;
using UnityEngine;

namespace SER494.Gameplay.CombatSandbox
{
    public sealed class CombatSandboxController : MonoBehaviour
    {
        private readonly List<string> _combatLog = new List<string>();

        private CombatantState _player;
        private CombatantState _enemy;
        private CombatAction _quickStrike;
        private CombatAction _fireBolt;
        private CombatAction _enemyClaw;
        private BattleOutcome _outcome;

        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _logStyle;
        private Vector2 _logScrollPosition;
        private string _playerStatus;
        private string _enemyStatus;
        private string _outcomeStatus;
        private string _combatLogText;

        private void Awake()
        {
            ResetBattle();
        }

        private void OnGUI()
        {
            if (_player == null || _enemy == null)
            {
                ResetBattle();
            }

            InitializeStyles();

            float width = Mathf.Clamp(Screen.width - 40f, 320f, 760f);
            float height = Mathf.Clamp(Screen.height - 40f, 420f, 720f);
            float left = Mathf.Max(0f, (Screen.width - width) * 0.5f);
            float top = Mathf.Max(0f, (Screen.height - height) * 0.5f);

            GUILayout.BeginArea(new Rect(left, top, width, height), GUI.skin.box);
            GUILayout.Label("Combat Sandbox", _titleStyle);
            GUILayout.Label("Prototype debug UI — this is not the final combat presentation.");
            GUILayout.Space(8f);

            DrawCombatantStatus();
            GUILayout.Space(10f);
            DrawPlayerActions();
            GUILayout.Space(10f);
            DrawCombatLog();
            GUILayout.FlexibleSpace();

            GUILayout.Label(_outcomeStatus, _sectionStyle);
            if (GUILayout.Button("Reset Battle", GUILayout.Height(34f)))
            {
                ResetBattle();
            }

            GUILayout.EndArea();
        }

        private void DrawCombatantStatus()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label("Player", _sectionStyle);
            GUILayout.Label(_playerStatus);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label("Enemy", _sectionStyle);
            GUILayout.Label(_enemyStatus);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawPlayerActions()
        {
            GUILayout.Label("Choose a Player Action", _sectionStyle);

            bool previousEnabledState = GUI.enabled;
            GUI.enabled = CanPlayerUse(_quickStrike);
            if (GUILayout.Button(ActionButtonText(_quickStrike), GUILayout.Height(42f)))
            {
                ExecutePlayerAction(_quickStrike);
            }

            GUI.enabled = CanPlayerUse(_fireBolt);
            if (GUILayout.Button(ActionButtonText(_fireBolt), GUILayout.Height(42f)))
            {
                ExecutePlayerAction(_fireBolt);
            }

            GUI.enabled = previousEnabledState;
        }

        private void DrawCombatLog()
        {
            GUILayout.Label("Combat Log", _sectionStyle);
            _logScrollPosition = GUILayout.BeginScrollView(
                _logScrollPosition,
                GUI.skin.box,
                GUILayout.MinHeight(130f),
                GUILayout.ExpandHeight(true));
            GUILayout.Label(_combatLogText, _logStyle);
            GUILayout.EndScrollView();
        }

        private void ExecutePlayerAction(CombatAction action)
        {
            CombatActionResult result = CombatResolver.Execute(_player, _enemy, action);
            AppendActionResult(_player, _enemy, action, result);

            if (!result.Succeeded)
            {
                RefreshPresentation();
                return;
            }

            EvaluateBattle();
            if (_outcome == BattleOutcome.Ongoing)
            {
                ExecuteEnemyTurn();
                EvaluateBattle();
            }

            RefreshPresentation();
        }

        private void ExecuteEnemyTurn()
        {
            CombatActionResult result = CombatResolver.Execute(_enemy, _player, _enemyClaw);
            AppendActionResult(_enemy, _player, _enemyClaw, result);
        }

        private void EvaluateBattle()
        {
            BattleOutcome previousOutcome = _outcome;
            _outcome = BattleEvaluator.Evaluate(
                new[] { _player },
                new[] { _enemy });

            if (previousOutcome == BattleOutcome.Ongoing && _outcome != BattleOutcome.Ongoing)
            {
                AppendLog(OutcomeText(_outcome));
            }
        }

        private bool CanPlayerUse(CombatAction action)
        {
            return _outcome == BattleOutcome.Ongoing
                && !_player.IsDefeated
                && !_enemy.IsDefeated
                && _player.CanPay(action.ManaCost, action.StaminaCost);
        }

        private void ResetBattle()
        {
            _player = new CombatantState(
                "Player",
                new CombatantStats(
                    maxHealth: 60,
                    maxMana: 12,
                    maxStamina: 14,
                    attack: 8,
                    defense: 3,
                    speed: 6));

            _enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(
                    maxHealth: 50,
                    maxMana: 0,
                    maxStamina: 0,
                    attack: 7,
                    defense: 2,
                    speed: 4));

            _quickStrike = new CombatAction(
                "Quick Strike",
                power: 3,
                manaCost: 0,
                staminaCost: 2);

            _fireBolt = new CombatAction(
                "Fire Bolt",
                power: 7,
                manaCost: 4,
                staminaCost: 0);

            _enemyClaw = new CombatAction(
                "Claw",
                power: 2,
                manaCost: 0,
                staminaCost: 0);

            _outcome = BattleOutcome.Ongoing;
            _combatLog.Clear();
            _logScrollPosition = Vector2.zero;
            AppendLog("Battle started. Choose an action.");
            RefreshPresentation();
        }

        private void AppendActionResult(
            CombatantState actor,
            CombatantState target,
            CombatAction action,
            CombatActionResult result)
        {
            if (result.Succeeded)
            {
                AppendLog($"{actor.DisplayName} used {action.DisplayName} and dealt {result.DamageDealt} damage to {target.DisplayName}.");
                return;
            }

            AppendLog($"{actor.DisplayName} could not use {action.DisplayName}: {FailureText(result.Failure)}.");
        }

        private void AppendLog(string message)
        {
            _combatLog.Add(message);
            _combatLogText = string.Join("\n", _combatLog);
            _logScrollPosition.y = float.MaxValue;
        }

        private void RefreshPresentation()
        {
            _playerStatus = StatusText(_player);
            _enemyStatus = StatusText(_enemy);
            _outcomeStatus = OutcomeText(_outcome);
        }

        private void InitializeStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };

            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            _logStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
        }

        private static string StatusText(CombatantState combatant)
        {
            return $"HP: {combatant.CurrentHealth}/{combatant.Stats.MaxHealth}\n"
                + $"Mana: {combatant.CurrentMana}/{combatant.Stats.MaxMana}\n"
                + $"Stamina: {combatant.CurrentStamina}/{combatant.Stats.MaxStamina}\n"
                + $"Attack: {combatant.Stats.Attack}   Defense: {combatant.Stats.Defense}   Speed: {combatant.Stats.Speed}";
        }

        private static string ActionButtonText(CombatAction action)
        {
            return $"{action.DisplayName}  |  Power {action.Power}  |  Mana {action.ManaCost}  |  Stamina {action.StaminaCost}";
        }

        private static string FailureText(CombatActionFailure failure)
        {
            switch (failure)
            {
                case CombatActionFailure.ActorDefeated:
                    return "the actor is defeated";
                case CombatActionFailure.TargetDefeated:
                    return "the target is defeated";
                case CombatActionFailure.InsufficientMana:
                    return "not enough Mana";
                case CombatActionFailure.InsufficientStamina:
                    return "not enough Stamina";
                default:
                    return "unknown reason";
            }
        }

        private static string OutcomeText(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return "Player Victory";
                case BattleOutcome.PlayerDefeat:
                    return "Player Defeat";
                case BattleOutcome.Draw:
                    return "Draw";
                default:
                    return "Battle in Progress";
            }
        }
    }
}
