using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SER494.Gameplay.CombatSandbox
{
    public sealed class CombatSandboxController : MonoBehaviour
    {
        private const string BuilderSceneName = "ComboBuilderSandbox";
        private const int ManaRecoveryPerTurn = 1;
        private const int StaminaRecoveryPerTurn = 2;
        private const int IntentTimelineLength = 30;

        [Header("Prepared Combo Handoff")]
        [SerializeField]
        private SoloComboLoadoutSession loadoutSession = null;

        private readonly List<string> _combatLog = new List<string>();

        private SoloComboBattleState _battle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _logStyle;
        private Vector2 _logScrollPosition;
        private string _playerStatus = string.Empty;
        private string _enemyStatus = string.Empty;
        private string _outcomeStatus = string.Empty;
        private string _combatLogText = string.Empty;
        private string _setupError;

        public SoloComboLoadoutSession LoadoutSession
        {
            get => loadoutSession;
            set => loadoutSession = value;
        }

        private void Awake()
        {
            ResetBattle();
        }

        private void OnGUI()
        {
            InitializeStyles();

            float width = Mathf.Clamp(Screen.width - 40f, 360f, 820f);
            float height = Mathf.Clamp(Screen.height - 40f, 460f, 780f);
            float left = Mathf.Max(0f, (Screen.width - width) * 0.5f);
            float top = Mathf.Max(0f, (Screen.height - height) * 0.5f);

            GUILayout.BeginArea(new Rect(left, top, width, height), GUI.skin.box);
            GUILayout.Label("Solo Combo Combat Sandbox", _titleStyle);
            GUILayout.Label("Prototype debug UI — preview intent, then resolve both sides on one timeline.");
            GUILayout.Space(8f);

            if (_battle == null)
            {
                DrawMissingSetup();
                GUILayout.EndArea();
                return;
            }

            DrawCombatantStatus();
            GUILayout.Space(8f);
            DrawEnemyIntent();
            GUILayout.Space(10f);
            DrawComboActions();
            GUILayout.Space(10f);
            DrawCombatLog();
            GUILayout.FlexibleSpace();

            GUILayout.Label(_outcomeStatus, _sectionStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Battle", GUILayout.Height(34f)))
            {
                ResetBattle();
            }

            if (GUILayout.Button("Return to Combo Builder", GUILayout.Height(34f)))
            {
                SceneManager.LoadScene(BuilderSceneName);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawMissingSetup()
        {
            GUILayout.Space(18f);
            GUILayout.Label("No Prepared Combo Loadout", _sectionStyle);
            GUILayout.Label(_setupError ?? "Open the Combo Builder and prepare a valid loadout first.");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Combo Builder", GUILayout.Height(38f)))
            {
                SceneManager.LoadScene(BuilderSceneName);
            }
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

        private void DrawComboActions()
        {
            GUILayout.Label("Choose a Prepared Combo", _sectionStyle);

            bool previousEnabledState = GUI.enabled;
            for (int index = 0; index < _battle.Combos.Count; index++)
            {
                SoloComboExecutionFailure failure = _battle.GetExecutionFailure(index);
                GUI.enabled = failure == SoloComboExecutionFailure.None;
                if (GUILayout.Button(ComboButtonText(index, failure), GUILayout.Height(46f)))
                {
                    ExecuteCombo(index);
                }
            }

            if (_battle.CanWait)
            {
                GUI.enabled = true;
                if (GUILayout.Button(
                        "Wait Turn  |  advance cooldowns and recover resources",
                        GUILayout.Height(42f)))
                {
                    WaitTurn();
                }
            }

            GUI.enabled = previousEnabledState;
        }

        private void DrawEnemyIntent()
        {
            GUILayout.Label($"Enemy Intent: {_battle.EnemySequence.DisplayName}", _sectionStyle);

            Rect timelineRect = GUILayoutUtility.GetRect(
                100f,
                28f,
                GUILayout.ExpandWidth(true));
            GUI.Box(timelineRect, GUIContent.none);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.92f, 0.25f, 0.22f, 0.9f);
            for (int index = 0; index < _battle.EnemyIntent.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _battle.EnemyIntent.Actions[index];
                if (!action.HasDamage)
                {
                    continue;
                }

                float startRatio = Mathf.Clamp01(
                    (action.ResolutionTime - 1f) / IntentTimelineLength);
                float endRatio = Mathf.Clamp01(
                    action.ResolutionTime / (float)IntentTimelineLength);
                Rect marker = new Rect(
                    timelineRect.x + timelineRect.width * startRatio,
                    timelineRect.y + 2f,
                    Mathf.Max(3f, timelineRect.width * (endRatio - startRatio)),
                    timelineRect.height - 4f);
                GUI.DrawTexture(marker, Texture2D.whiteTexture);
            }

            GUI.color = previousColor;
            for (int time = 0; time <= IntentTimelineLength; time += 5)
            {
                float ratio = time / (float)IntentTimelineLength;
                float x = timelineRect.x + timelineRect.width * ratio;
                GUI.DrawTexture(
                    new Rect(x, timelineRect.y, 1f, timelineRect.height),
                    Texture2D.whiteTexture);
                GUI.Label(
                    new Rect(x - 10f, timelineRect.y + 5f, 24f, 20f),
                    time.ToString());
            }

            for (int index = 0; index < _battle.EnemyIntent.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _battle.EnemyIntent.Actions[index];
                string effect = action.HasDamage ? "Damage" : "Action";
                GUILayout.Label($"T{action.ResolutionTime}: {action.DisplayName} — {effect}");
            }
        }

        private void DrawCombatLog()
        {
            GUILayout.Label("Combat Log", _sectionStyle);
            _logScrollPosition = GUILayout.BeginScrollView(
                _logScrollPosition,
                GUI.skin.box,
                GUILayout.MinHeight(150f),
                GUILayout.ExpandHeight(true));
            GUILayout.Label(_combatLogText, _logStyle);
            GUILayout.EndScrollView();
        }

        private void ExecuteCombo(int comboIndex)
        {
            ComboConfiguration combo = _battle.Combos[comboIndex];
            SoloComboTurnResult result = _battle.ExecuteCombo(comboIndex);

            if (!result.Succeeded)
            {
                AppendLog($"Could not execute {combo.Name}: {FailureText(result.Failure)}.");
                RefreshPresentation();
                return;
            }

            AppendLog($"Player used {combo.Name}.");
            AppendTimelineResolutions(result);

            if (result.ManaRecovered > 0 || result.StaminaRecovered > 0)
            {
                AppendLog(
                    $"Player recovered {result.ManaRecovered} Mana and "
                    + $"{result.StaminaRecovered} Stamina.");
            }

            if (result.Outcome != BattleOutcome.Ongoing)
            {
                AppendLog(OutcomeText(result.Outcome));
            }

            RefreshPresentation();
        }

        private void WaitTurn()
        {
            SoloComboTurnResult result = _battle.WaitTurn();
            if (!result.Succeeded)
            {
                AppendLog($"Could not wait: {FailureText(result.Failure)}.");
                return;
            }

            AppendLog("Player waited. All active cooldowns advanced by one turn.");
            AppendTimelineResolutions(result);

            if (result.ManaRecovered > 0 || result.StaminaRecovered > 0)
            {
                AppendLog(
                    $"Player recovered {result.ManaRecovered} Mana and "
                    + $"{result.StaminaRecovered} Stamina.");
            }

            if (result.Outcome != BattleOutcome.Ongoing)
            {
                AppendLog(OutcomeText(result.Outcome));
            }

            RefreshPresentation();
        }

        private void ResetBattle()
        {
            _battle = null;
            _setupError = null;
            _combatLog.Clear();
            _combatLogText = string.Empty;
            _logScrollPosition = Vector2.zero;

            if (loadoutSession == null)
            {
                _setupError = "The Combat Sandbox has no runtime loadout session assigned.";
                return;
            }

            if (!loadoutSession.TryGetPreparedLoadout(out IReadOnlyList<ComboConfiguration> combos))
            {
                _setupError = "No loadout was transferred from the Combo Builder.";
                return;
            }

            var player = new CombatantState(
                "Player",
                new CombatantStats(
                    maxHealth: 100,
                    maxMana: 12,
                    maxStamina: 14,
                    attack: 50,
                    defense: 50));

            var enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(
                    maxHealth: 50,
                    maxMana: 0,
                    maxStamina: 0,
                    attack: 60,
                    defense: 60));

            var enemySequence = new EnemyActionSequence(
                "training.claw-pattern",
                "Claw Pattern",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.claw-windup",
                        "Claw Windup",
                        loadCost: 2,
                        resolutionOffset: 2,
                        damageMultiplier: 0d),
                    new EnemyActionDefinition(
                        "training.claw",
                        "Claw",
                        loadCost: 3,
                        resolutionOffset: 2,
                        damageMultiplier: 0.40d)
                });

            _battle = new SoloComboBattleState(
                player,
                enemy,
                combos,
                enemySequence,
                ManaRecoveryPerTurn,
                StaminaRecoveryPerTurn);
            AppendLog("Battle started. Choose a prepared Combo.");
            RefreshPresentation();
        }

        private void AppendLog(string message)
        {
            _combatLog.Add(message);
            _combatLogText = string.Join("\n", _combatLog);
            _logScrollPosition.y = float.MaxValue;
        }

        private void AppendTimelineResolutions(SoloComboTurnResult result)
        {
            for (int index = 0; index < result.ActionResolutions.Count; index++)
            {
                SoloComboActionResolution resolution = result.ActionResolutions[index];
                string actor = resolution.Side == TimelineActorSide.Player
                    ? "Player"
                    : "Training Enemy";
                if (resolution.DamageAvoidance == DamageAvoidanceResult.Guarded)
                {
                    AppendLog(
                        $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} was Guarded; "
                        + "its Damage and dependent On Hit effects failed.");
                }
                else if (resolution.DamageAvoidance == DamageAvoidanceResult.Evaded)
                {
                    AppendLog(
                        $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} was Evaded; "
                        + "its Damage and dependent On Hit effects failed.");
                }
                else if (resolution.ResolutionEffect == ActionResolutionEffect.Damage)
                {
                    AppendLog(
                        $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} dealt "
                        + $"{resolution.DamageDealt} damage.");
                }
                else
                {
                    string effect = resolution.ResolutionEffect == ActionResolutionEffect.None
                        ? "with no prototype combat effect"
                        : $"{resolution.ResolutionEffect}";
                    AppendLog(
                        $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} resolved "
                        + $"{effect}.");
                }
            }
        }

        private void RefreshPresentation()
        {
            _playerStatus = StatusText(_battle.Player);
            _enemyStatus = StatusText(_battle.Enemy);
            _outcomeStatus = OutcomeText(_battle.Outcome);
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

        private string ComboButtonText(int comboIndex, SoloComboExecutionFailure failure)
        {
            ComboConfiguration combo = _battle.Combos[comboIndex];
            string text = $"{combo.Name}  |  Mana {combo.TotalManaCost}  |  "
                + $"Stamina {combo.TotalStaminaCost}  |  CD {_battle.GetCooldownRemaining(comboIndex)}";
            return failure == SoloComboExecutionFailure.None
                ? text
                : $"{text}  —  {FailureText(failure)}";
        }

        private static string StatusText(CombatantState combatant)
        {
            return $"HP: {combatant.CurrentHealth}/{combatant.Stats.MaxHealth}\n"
                + $"Mana: {combatant.CurrentMana}/{combatant.Stats.MaxMana}\n"
                + $"Stamina: {combatant.CurrentStamina}/{combatant.Stats.MaxStamina}\n"
                + $"Weapon ATK: {combatant.Stats.Attack}   Defense: {combatant.Stats.Defense}";
        }

        private static string FailureText(SoloComboExecutionFailure failure)
        {
            switch (failure)
            {
                case SoloComboExecutionFailure.BattleEnded:
                    return "battle ended";
                case SoloComboExecutionFailure.ComboIndexOutOfRange:
                    return "invalid Combo";
                case SoloComboExecutionFailure.ComboOnCooldown:
                    return "on cooldown";
                case SoloComboExecutionFailure.InsufficientMana:
                    return "not enough Mana";
                case SoloComboExecutionFailure.InsufficientStamina:
                    return "not enough Stamina";
                case SoloComboExecutionFailure.WaitNotAvailable:
                    return "at least one Combo is available";
                default:
                    return "available";
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
                    return "Mutual Destruction";
                default:
                    return "Battle in Progress";
            }
        }
    }
}
