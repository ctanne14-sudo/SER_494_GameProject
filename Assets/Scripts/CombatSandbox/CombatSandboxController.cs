using System.Collections;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Damage;
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
        private const float WideLayoutThreshold = 1050f;

        [Header("Prepared Combo Handoff")]
        [SerializeField]
        private SoloComboLoadoutSession loadoutSession = null;

        [Header("Timeline Playback")]
        [SerializeField]
        [Min(0.01f)]
        private float secondsPerTimeStep = 0.2f;

        [SerializeField]
        [Min(0.1f)]
        private float timingInputLimit = 3f;

        [SerializeField]
        [Min(0.05f)]
        private float timingCandidateSeconds = 0.35f;

        [SerializeField]
        [Min(0.1f)]
        private float chargeInputLimit = 3f;

        [SerializeField]
        [Min(0.1f)]
        private float chargeHoldLimit = 3f;

        [SerializeField]
        [Min(0.1f)]
        private float chargeCycleSeconds = 1.5f;

        private readonly List<string> _combatLog = new List<string>();

        private SoloComboBattleState _battle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _logStyle;
        private GUIStyle _timelineStyle;
        private Vector2 _contentScrollPosition;
        private Vector2 _logScrollPosition;
        private string _playerStatus = string.Empty;
        private string _enemyStatus = string.Empty;
        private string _outcomeStatus = string.Empty;
        private string _combatLogText = string.Empty;
        private string _setupError;
        private Coroutine _timelinePlayback;
        private bool _isTimelinePlaying;
        private int _playbackTimeStep;
        private int _playbackEndTimeStep;
        private SoloComboTimingRequest _timingRequest;
        private float _timingElapsed;
        private bool _timingSelectionRequested;
        private int _timingSelectedTimeStep;
        private SoloComboChargeRequest _chargeRequest;
        private float _chargeInputElapsed;
        private float _chargeHoldElapsed;
        private bool _chargeEngaged;
        private bool _chargeSelectionRequested;
        private bool _chargeForcedLevelZero;
        private int _chargeSelectedLevel;

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

            float width = Mathf.Clamp(Screen.width * 0.92f, 360f, 1600f);
            float height = Mathf.Clamp(Screen.height * 0.92f, 460f, 1100f);
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

            if (width >= WideLayoutThreshold)
            {
                DrawWideBattleLayout(width);
            }
            else
            {
                DrawNarrowBattleLayout();
            }

            GUILayout.Space(6f);
            DrawFooterControls();
            GUILayout.EndArea();
        }

        private void DrawWideBattleLayout(float availableWidth)
        {
            DrawCombatantStatus();
            GUILayout.Space(8f);
            DrawEnemyIntent();
            GUILayout.Space(10f);

            float logWidth = Mathf.Clamp(availableWidth * 0.36f, 380f, 560f);
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.MinWidth(520f),
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            _contentScrollPosition = GUILayout.BeginScrollView(
                _contentScrollPosition,
                GUILayout.ExpandHeight(true));
            DrawTimelinePlayback();
            GUILayout.Space(12f);
            DrawComboActions();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.Width(logWidth),
                GUILayout.ExpandHeight(true));
            DrawCombatLog(expandHeight: true);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawNarrowBattleLayout()
        {
            _contentScrollPosition = GUILayout.BeginScrollView(
                _contentScrollPosition,
                GUILayout.ExpandHeight(true));
            DrawCombatantStatus();
            GUILayout.Space(8f);
            DrawEnemyIntent();
            GUILayout.Space(8f);
            DrawTimelinePlayback();
            GUILayout.Space(10f);
            DrawComboActions();
            GUILayout.Space(10f);
            DrawCombatLog(expandHeight: false);
            GUILayout.EndScrollView();
        }

        private void DrawFooterControls()
        {

            GUILayout.Label(_outcomeStatus, _sectionStyle);
            GUILayout.BeginHorizontal();
            bool previousEnabledState = GUI.enabled;
            GUI.enabled = !_isTimelinePlaying;
            if (GUILayout.Button("Reset Battle", GUILayout.Height(34f)))
            {
                ResetBattle();
            }

            if (GUILayout.Button("Return to Combo Builder", GUILayout.Height(34f)))
            {
                SceneManager.LoadScene(BuilderSceneName);
            }

            GUI.enabled = previousEnabledState;
            GUILayout.EndHorizontal();
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
                GUI.enabled = !_isTimelinePlaying
                    && failure == SoloComboExecutionFailure.None;
                if (GUILayout.Button(ComboButtonText(index, failure), GUILayout.Height(46f)))
                {
                    ExecuteCombo(index);
                }
            }

            if (_battle.CanWait)
            {
                GUI.enabled = !_isTimelinePlaying;
                if (GUILayout.Button(
                        "Wait Turn  |  advance cooldowns and recover resources",
                        GUILayout.Height(42f)))
                {
                    WaitTurn();
                }
            }

            GUI.enabled = previousEnabledState;
        }

        private void DrawTimelinePlayback()
        {
            GUILayout.Label("Timeline Resolution", _sectionStyle);

            int displayEnd = Mathf.Max(1, _playbackEndTimeStep);
            float progress = _isTimelinePlaying
                ? Mathf.Clamp01(_playbackTimeStep / (float)displayEnd)
                : 0f;
            Rect progressRect = GUILayoutUtility.GetRect(
                100f,
                24f,
                GUILayout.ExpandWidth(true));
            GUI.Box(progressRect, GUIContent.none);

            if (_isTimelinePlaying)
            {
                Color previousColor = GUI.color;
                GUI.color = new Color(0.25f, 0.65f, 0.95f, 0.9f);
                GUI.DrawTexture(
                    new Rect(
                        progressRect.x + 2f,
                        progressRect.y + 2f,
                        (progressRect.width - 4f) * progress,
                        progressRect.height - 4f),
                    Texture2D.whiteTexture);
                GUI.color = previousColor;
            }

            string label = _isTimelinePlaying
                ? $"Resolving T{_playbackTimeStep} / T{_playbackEndTimeStep}"
                : "Ready — choose a Combo after reviewing Enemy Intent";
            GUI.Label(progressRect, label, _timelineStyle);

            if (_timingRequest != null)
            {
                DrawResolutionTimingQte();
            }
            else if (_chargeRequest != null)
            {
                DrawChargeQte();
            }
        }

        private void DrawResolutionTimingQte()
        {
            GUILayout.Space(6f);
            GUILayout.Label(
                $"Resolution Timing QTE — {_timingRequest.DisplayName}",
                _sectionStyle);
            int candidate = CurrentTimingCandidate();
            int candidateCount = _timingRequest.MaximumTimeStep
                - _timingRequest.MinimumTimeStep + 1;
            Rect selectorRect = GUILayoutUtility.GetRect(
                100f,
                34f,
                GUILayout.ExpandWidth(true));
            float segmentWidth = selectorRect.width / candidateCount;
            Color previousColor = GUI.color;
            for (int index = 0; index < candidateCount; index++)
            {
                int timeStep = _timingRequest.MinimumTimeStep + index;
                GUI.color = timeStep == candidate
                    ? new Color(0.95f, 0.65f, 0.20f, 1f)
                    : new Color(0.25f, 0.30f, 0.40f, 1f);
                Rect segment = new Rect(
                    selectorRect.x + segmentWidth * index,
                    selectorRect.y,
                    segmentWidth - 2f,
                    selectorRect.height);
                GUI.Box(segment, $"T{timeStep}");
            }

            GUI.color = previousColor;
            float remaining = Mathf.Max(0f, timingInputLimit - _timingElapsed);
            if (GUILayout.Button(
                    $"Lock T{candidate}  |  Space  |  {remaining:0.0}s",
                    GUILayout.Height(34f)))
            {
                RequestTimingSelection(candidate);
            }

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown
                && currentEvent.keyCode == KeyCode.Space)
            {
                RequestTimingSelection(candidate);
                currentEvent.Use();
            }
        }

        private void DrawChargeQte()
        {
            GUILayout.Space(6f);
            GUILayout.Label($"Charge QTE — {_chargeRequest.DisplayName}", _sectionStyle);

            int candidate = _chargeEngaged ? CurrentChargeLevel() : 0;
            int segmentCount = _chargeRequest.MaximumChargeLevel + 1;
            Rect chargeRect = GUILayoutUtility.GetRect(
                100f,
                34f,
                GUILayout.ExpandWidth(true));
            float segmentWidth = chargeRect.width / segmentCount;
            Color previousColor = GUI.color;
            for (int level = 0; level < segmentCount; level++)
            {
                GUI.color = level == candidate
                    ? new Color(0.95f, 0.65f, 0.20f, 1f)
                    : new Color(0.25f, 0.30f, 0.40f, 1f);
                GUI.Box(
                    new Rect(
                        chargeRect.x + segmentWidth * level,
                        chargeRect.y,
                        segmentWidth - 2f,
                        chargeRect.height),
                    $"Level {level}");
            }

            GUI.color = previousColor;
            Event currentEvent = Event.current;
            if (!_chargeEngaged)
            {
                float remaining = Mathf.Max(0f, chargeInputLimit - _chargeInputElapsed);
                if (GUILayout.Button(
                        $"Begin Charging  |  Hold Space  |  {remaining:0.0}s",
                        GUILayout.Height(34f)))
                {
                    BeginCharging();
                }

                if (currentEvent.type == EventType.KeyDown
                    && currentEvent.keyCode == KeyCode.Space)
                {
                    BeginCharging();
                    currentEvent.Use();
                }
            }
            else
            {
                float remaining = Mathf.Max(0f, chargeHoldLimit - _chargeHoldElapsed);
                if (GUILayout.Button(
                        $"Release at Level {candidate}  |  Release Space  |  {remaining:0.0}s",
                        GUILayout.Height(34f)))
                {
                    RequestChargeRelease();
                }

                if (currentEvent.type == EventType.KeyUp
                    && currentEvent.keyCode == KeyCode.Space)
                {
                    RequestChargeRelease();
                    currentEvent.Use();
                }
            }
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
            for (int index = 0; index < _battle.EnemyIntent.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _battle.EnemyIntent.Actions[index];
                if (action.ResolutionEffect == ActionResolutionEffect.None)
                {
                    continue;
                }

                GUI.color = IntentColor(action.ResolutionEffect);
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
                string effect = action.ResolutionEffect.ToString();
                if (action.StatusEffect != null)
                {
                    effect += $" ({action.StatusEffect.DisplayName})";
                }

                GUILayout.Label($"T{action.ResolutionTime}: {action.DisplayName} — {effect}");
            }
        }

        private static Color IntentColor(ActionResolutionEffect effect)
        {
            switch (effect)
            {
                case ActionResolutionEffect.Damage:
                    return new Color(0.92f, 0.25f, 0.22f, 0.9f);
                case ActionResolutionEffect.Guard:
                case ActionResolutionEffect.Evade:
                    return new Color(0.25f, 0.65f, 0.95f, 0.9f);
                case ActionResolutionEffect.ApplyBuff:
                    return new Color(0.25f, 0.80f, 0.35f, 0.9f);
                case ActionResolutionEffect.ApplyDebuff:
                    return new Color(0.70f, 0.30f, 0.85f, 0.9f);
                default:
                    return Color.white;
            }
        }

        private void DrawCombatLog(bool expandHeight)
        {
            GUILayout.Label("Combat Log", _sectionStyle);
            GUILayoutOption heightOption = expandHeight
                ? GUILayout.ExpandHeight(true)
                : GUILayout.MinHeight(240f);
            _logScrollPosition = GUILayout.BeginScrollView(
                _logScrollPosition,
                GUI.skin.box,
                GUILayout.MinHeight(expandHeight ? 300f : 240f),
                heightOption);
            GUILayout.Label(_combatLogText, _logStyle);
            GUILayout.EndScrollView();
        }

        private void ExecuteCombo(int comboIndex)
        {
            ComboConfiguration combo = _battle.Combos[comboIndex];
            SoloComboExecutionFailure failure = _battle.BeginCombo(comboIndex);

            if (failure != SoloComboExecutionFailure.None)
            {
                AppendLog($"Could not execute {combo.Name}: {FailureText(failure)}.");
                RefreshPresentation();
                return;
            }

            AppendLog($"Player committed {combo.Name}. Timeline resolution started.");
            BeginTimelinePlayback();
        }

        private void WaitTurn()
        {
            SoloComboExecutionFailure failure = _battle.BeginWaitTurn();
            if (failure != SoloComboExecutionFailure.None)
            {
                AppendLog($"Could not wait: {FailureText(failure)}.");
                return;
            }

            AppendLog("Player waited. Timeline resolution started.");
            BeginTimelinePlayback();
        }

        private void ResetBattle()
        {
            if (_timelinePlayback != null)
            {
                StopCoroutine(_timelinePlayback);
                _timelinePlayback = null;
            }

            _isTimelinePlaying = false;
            _playbackTimeStep = 0;
            _playbackEndTimeStep = 0;
            _timingRequest = null;
            _timingElapsed = 0f;
            _timingSelectionRequested = false;
            _chargeRequest = null;
            _chargeInputElapsed = 0f;
            _chargeHoldElapsed = 0f;
            _chargeEngaged = false;
            _chargeSelectionRequested = false;
            _chargeForcedLevelZero = false;
            _chargeSelectedLevel = 0;
            _battle = null;
            _setupError = null;
            _combatLog.Clear();
            _combatLogText = string.Empty;
            _contentScrollPosition = Vector2.zero;
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
                    maxMana: 30,
                    maxStamina: 30,
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

            // Temporary sandbox traits make type-specific modifier behavior visible
            // before equipment, Inherent Skills, and timed statuses own these effects.
            player.AddDamageDealtModifier(
                DamageModifier.ForType(DamageType.Slashing, 0.20d));
            enemy.AddDamageTakenModifier(
                DamageModifier.ForType(DamageType.Fire, -0.20d));

            var exposed = new CombatStatusDefinition(
                "training.exposed",
                "Exposed",
                CombatStatusDurationType.Rounds,
                duration: 2,
                CombatStatusModifierDirection.DamageTaken,
                DamageModifier.All(0.20d));

            var exposeClaw = new EnemyActionSequence(
                "training.claw-pattern",
                "Claw Pattern",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.expose",
                        "Expose",
                        loadCost: 2,
                        resolutionOffset: 2,
                        damageMultiplier: 0d,
                        resolutionEffect: ActionResolutionEffect.ApplyDebuff,
                        statusEffect: exposed,
                        statusTarget: ActionStatusTarget.Opponent),
                    new EnemyActionDefinition(
                        "training.claw",
                        "Claw",
                        loadCost: 3,
                        resolutionOffset: 2,
                        damageMultiplier: 0.40d)
                });

            var heavySwing = new EnemyActionSequence(
                "training.heavy-swing-pattern",
                "Heavy Swing",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.heavy-swing",
                        "Heavy Swing",
                        loadCost: 5,
                        resolutionOffset: 5,
                        damageMultiplier: 0.65d,
                        damageTags: DamageResolutionTag.Overcommit)
                });

            var fallbackJab = new EnemyActionSequence(
                "training.fallback-jab",
                "Fallback Jab",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.jab",
                        "Jab",
                        loadCost: 3,
                        resolutionOffset: 3,
                        damageMultiplier: 0.30d)
                });

            var enemyBehavior = new EnemyBehaviorTable(
                new[]
                {
                    new EnemyBehaviorEntry(
                        exposeClaw,
                        selectionWeight: 2,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        heavySwing,
                        selectionWeight: 1,
                        preventImmediateRepeat: true)
                },
                fallbackJab);

            _battle = new SoloComboBattleState(
                player,
                enemy,
                combos,
                enemyBehavior,
                ManaRecoveryPerTurn,
                StaminaRecoveryPerTurn,
                enemyRandom: new System.Random(594));
            AppendLog("Battle started. Choose a prepared Combo.");
            RefreshPresentation();
        }

        private void BeginTimelinePlayback()
        {
            _isTimelinePlaying = true;
            _playbackTimeStep = 0;
            _playbackEndTimeStep = _battle.TimelineCompletionTime;
            RefreshPresentation();
            _timelinePlayback = StartCoroutine(PlayTimeline());
        }

        private IEnumerator PlayTimeline()
        {
            var wait = new WaitForSecondsRealtime(Mathf.Max(0.01f, secondsPerTimeStep));

            while (_battle.IsTimelineResolving)
            {
                yield return wait;
                SoloComboTimelineAdvanceResult advance = _battle.AdvanceTimeline();
                if (advance.RequiresTimingInput)
                {
                    yield return RunResolutionTimingQte(advance.TimingRequest);
                    continue;
                }


                if (advance.RequiresChargeInput)
                {
                    yield return RunChargeQte(advance.ChargeRequest);
                    continue;
                }

                _playbackTimeStep = advance.TimeStep;

                for (int index = 0; index < advance.ActionResolutions.Count; index++)
                {
                    AppendTimelineResolution(advance.ActionResolutions[index]);
                }

                RefreshPresentation();
                if (advance.RoundCompleted)
                {
                    CompleteTimelinePlayback(advance.CompletedTurn);
                }
            }
        }

        private IEnumerator RunResolutionTimingQte(SoloComboTimingRequest request)
        {
            _timingRequest = request;
            _timingElapsed = 0f;
            _timingSelectionRequested = false;
            _timingSelectedTimeStep = request.MinimumTimeStep;

            while (!_timingSelectionRequested && _timingElapsed < timingInputLimit)
            {
                _timingElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            int selectedTimeStep = _timingSelectionRequested
                ? _timingSelectedTimeStep
                : request.MinimumTimeStep;
            _battle.SelectResolutionPoint(selectedTimeStep);
            AppendLog(
                _timingSelectionRequested
                    ? $"  Timing QTE: {request.DisplayName} locked to T{selectedTimeStep}."
                    : $"  Timing QTE timed out: {request.DisplayName} defaulted to "
                        + $"T{selectedTimeStep}.");
            _timingRequest = null;
            _timingElapsed = 0f;
            _timingSelectionRequested = false;
        }

        private int CurrentTimingCandidate()
        {
            int count = _timingRequest.MaximumTimeStep
                - _timingRequest.MinimumTimeStep + 1;
            int index = Mathf.FloorToInt(
                _timingElapsed / Mathf.Max(0.05f, timingCandidateSeconds)) % count;
            return _timingRequest.MinimumTimeStep + index;
        }

        private void RequestTimingSelection(int timeStep)
        {
            if (_timingRequest == null || _timingSelectionRequested)
            {
                return;
            }

            _timingSelectedTimeStep = timeStep;
            _timingSelectionRequested = true;
        }

        private IEnumerator RunChargeQte(SoloComboChargeRequest request)
        {
            _chargeRequest = request;
            _chargeInputElapsed = 0f;
            _chargeHoldElapsed = 0f;
            _chargeEngaged = false;
            _chargeSelectionRequested = false;
            _chargeForcedLevelZero = false;
            _chargeSelectedLevel = 0;

            while (!_chargeSelectionRequested)
            {
                if (_chargeEngaged)
                {
                    _chargeHoldElapsed += Time.unscaledDeltaTime;
                    if (_chargeHoldElapsed >= chargeHoldLimit)
                    {
                        _chargeSelectedLevel = 0;
                        _chargeForcedLevelZero = true;
                        _chargeSelectionRequested = true;
                    }
                }
                else
                {
                    _chargeInputElapsed += Time.unscaledDeltaTime;
                    if (_chargeInputElapsed >= chargeInputLimit)
                    {
                        _chargeSelectedLevel = 0;
                        _chargeSelectionRequested = true;
                    }
                }

                yield return null;
            }

            _battle.SelectChargeLevel(_chargeSelectedLevel);
            _playbackEndTimeStep = _battle.TimelineCompletionTime;
            string reason = _chargeForcedLevelZero
                ? "hold limit reached; forced to Level 0"
                : !_chargeEngaged
                    ? "no input; defaulted to Level 0"
                    : $"released at Level {_chargeSelectedLevel}";
            AppendLog($"  Charge QTE: {request.DisplayName} {reason}.");

            _chargeRequest = null;
            _chargeInputElapsed = 0f;
            _chargeHoldElapsed = 0f;
            _chargeEngaged = false;
            _chargeSelectionRequested = false;
            _chargeForcedLevelZero = false;
            _chargeSelectedLevel = 0;
        }

        private int CurrentChargeLevel()
        {
            int segmentCount = _chargeRequest.MaximumChargeLevel + 1;
            float cycle = Mathf.Max(0.1f, chargeCycleSeconds);
            float normalized = Mathf.Repeat(_chargeHoldElapsed, cycle) / cycle;
            return Mathf.Clamp(
                Mathf.FloorToInt(normalized * segmentCount),
                0,
                segmentCount - 1);
        }

        private void BeginCharging()
        {
            if (_chargeRequest == null
                || _chargeEngaged
                || _chargeSelectionRequested)
            {
                return;
            }

            _chargeEngaged = true;
            _chargeHoldElapsed = 0f;
        }

        private void RequestChargeRelease()
        {
            if (_chargeRequest == null
                || !_chargeEngaged
                || _chargeSelectionRequested)
            {
                return;
            }

            _chargeSelectedLevel = CurrentChargeLevel();
            _chargeSelectionRequested = true;
        }

        private void CompleteTimelinePlayback(SoloComboTurnResult result)
        {
            if (result.ManaRecovered > 0 || result.StaminaRecovered > 0)
            {
                AppendLog(
                    $"Round End: Player recovered {result.ManaRecovered} Mana and "
                    + $"{result.StaminaRecovered} Stamina.");
            }

            if (result.Outcome != BattleOutcome.Ongoing)
            {
                AppendLog(OutcomeText(result.Outcome));
            }
            else
            {
                AppendLog($"Next Enemy Intent locked: {_battle.EnemySequence.DisplayName}.");
            }

            _isTimelinePlaying = false;
            _timelinePlayback = null;
            RefreshPresentation();
        }

        private void AppendLog(string message)
        {
            _combatLog.Add(message);
            _combatLogText = string.Join("\n", _combatLog);
            _logScrollPosition.y = float.MaxValue;
        }

        private void AppendTimelineResolution(SoloComboActionResolution resolution)
        {
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
                string tags = resolution.DamageTags == DamageResolutionTag.None
                    ? string.Empty
                    : $" [{resolution.DamageTags}]";
                AppendLog(
                    $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} dealt "
                    + $"{resolution.DamageDealt} damage{tags}.");
            }
            else if (resolution.StatusDisplayName != null)
            {
                string recipient = resolution.StatusTarget == ActionStatusTarget.Self
                    ? actor
                    : resolution.Side == TimelineActorSide.Player
                        ? "Training Enemy"
                        : "Player";
                AppendLog(
                    resolution.StatusApplied
                        ? $"  T{resolution.TimeStep}: {actor} applied "
                            + $"{resolution.StatusDisplayName} to {recipient}."
                        : $"  T{resolution.TimeStep}: {actor} could not apply "
                            + $"{resolution.StatusDisplayName}.");
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

            _timelineStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
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
                + $"Weapon ATK: {combatant.Stats.Attack}   Defense: {combatant.Stats.Defense}\n"
                + $"Damage Dealt: {ModifierText(combatant.DamageDealtModifiers)}\n"
                + $"Damage Taken: {ModifierText(combatant.DamageTakenModifiers)}\n"
                + $"Statuses: {StatusText(combatant.ActiveStatuses)}";
        }

        private static string StatusText(IReadOnlyList<CombatStatusInstance> statuses)
        {
            if (statuses.Count == 0)
            {
                return "none";
            }

            var labels = new string[statuses.Count];
            for (int index = 0; index < statuses.Count; index++)
            {
                CombatStatusInstance status = statuses[index];
                string unit = status.Definition.DurationType == CombatStatusDurationType.Rounds
                    ? "round"
                    : "Time Steps";
                labels[index] = $"{status.Definition.DisplayName} "
                    + $"({status.RemainingDuration} {unit})";
            }

            return string.Join(", ", labels);
        }

        private static string ModifierText(IReadOnlyList<DamageModifier> modifiers)
        {
            if (modifiers.Count == 0)
            {
                return "none";
            }

            var labels = new string[modifiers.Count];
            for (int index = 0; index < modifiers.Count; index++)
            {
                DamageModifier modifier = modifiers[index];
                string scope;
                switch (modifier.Scope)
                {
                    case DamageModifierScope.Family:
                        scope = modifier.Family.ToString();
                        break;
                    case DamageModifierScope.Type:
                        scope = modifier.Type.ToString();
                        break;
                    default:
                        scope = "All";
                        break;
                }

                labels[index] = $"{scope} {modifier.Amount:+0%;-0%;0%}";
            }

            return string.Join(", ", labels);
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
                case SoloComboExecutionFailure.TimelineResolving:
                    return "timeline is already resolving";
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
