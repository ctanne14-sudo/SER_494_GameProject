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
        private enum CombatLogTone
        {
            Normal,
            System,
            Player,
            Enemy,
            Avoided,
            Disruption,
            Error,
            Success
        }

        private readonly struct CombatLogEntry
        {
            public CombatLogEntry(string message, CombatLogTone tone)
            {
                Message = message;
                Tone = tone;
            }

            public string Message { get; }

            public CombatLogTone Tone { get; }
        }

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

        private readonly List<CombatLogEntry> _combatLog = new List<CombatLogEntry>();

        private SoloComboBattleState _battle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _logStyle;
        private GUIStyle _timelineStyle;
        private GUIStyle _timelineActionStyle;
        private GUIStyle _timelineTickStyle;
        private Vector2 _contentScrollPosition;
        private Vector2 _logScrollPosition;
        private string _playerStatus = string.Empty;
        private string _enemyStatus = string.Empty;
        private string _outcomeStatus = string.Empty;
        private string _setupError;
        private Coroutine _timelinePlayback;
        private bool _isTimelinePlaying;
        private int _playbackTimeStep;
        private int _playbackEndTimeStep;
        private InitialTimelineSchedule _lastCompletedTimeline;
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
        private int _previewedComboIndex = -1;
        private InitialTimelineSchedule _previewSchedule;

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

            float width = Mathf.Clamp(Screen.width * 0.95f, 360f, 1900f);
            float height = Mathf.Clamp(Screen.height * 0.95f, 460f, 1200f);
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
            GUILayout.Space(8f);
            DrawTimelinePlayback();
            GUILayout.Space(10f);

            float comboWidth = Mathf.Clamp(availableWidth * 0.40f, 440f, 660f);
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.Width(comboWidth),
                GUILayout.ExpandHeight(true));
            _contentScrollPosition = GUILayout.BeginScrollView(
                _contentScrollPosition,
                GUILayout.ExpandHeight(true));
            DrawComboActions();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.ExpandWidth(true),
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
            GUILayout.Label("Choose and Preview a Prepared Combo", _sectionStyle);
            GUILayout.Label(
                "First select a Combo to compare both timelines. Confirm it separately to resolve the Round.");

            bool previousEnabledState = GUI.enabled;
            for (int index = 0; index < _battle.Combos.Count; index++)
            {
                SoloComboExecutionFailure failure = _battle.GetExecutionFailure(index);
                GUI.enabled = !_isTimelinePlaying
                    && failure == SoloComboExecutionFailure.None;
                string prefix = index == _previewedComboIndex
                    ? "PREVIEWING  |  "
                    : "Preview  |  ";
                if (GUILayout.Button(
                        prefix + ComboButtonText(index, failure),
                        GUILayout.Height(50f)))
                {
                    PreviewCombo(index);
                }
            }

            if (_previewedComboIndex >= 0)
            {
                ComboConfiguration previewed = _battle.Combos[_previewedComboIndex];
                GUILayout.Space(8f);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"Selected: {previewed.Name}", _sectionStyle);
                GUILayout.Label(
                    $"Timeline Cost {previewed.TotalTimelineCost}  |  Mana {previewed.TotalManaCost}  |  "
                    + $"Stamina {previewed.TotalStaminaCost}  |  CD {previewed.TotalCooldown}");
                GUILayout.BeginHorizontal();
                GUI.enabled = !_isTimelinePlaying
                    && _battle.GetExecutionFailure(_previewedComboIndex)
                        == SoloComboExecutionFailure.None;
                if (GUILayout.Button("Confirm and Resolve", GUILayout.Height(42f)))
                {
                    ExecuteCombo(_previewedComboIndex);
                }

                GUI.enabled = !_isTimelinePlaying;
                if (GUILayout.Button("Clear Preview", GUILayout.Height(42f)))
                {
                    ClearComboPreview();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
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

            InitialTimelineSchedule displayedSchedule = _battle.ActiveTimeline
                ?? _lastCompletedTimeline;
            if (displayedSchedule != null)
            {
                DrawActiveTimelineSchedule(
                    displayedSchedule,
                    _battle.ActiveTimeline != null);
            }

            if (_timingRequest != null)
            {
                DrawResolutionTimingQte();
            }
            else if (_chargeRequest != null)
            {
                DrawChargeQte();
            }
        }

        private void DrawActiveTimelineSchedule(
            InitialTimelineSchedule schedule,
            bool isActive)
        {
            int displayEnd = Mathf.Max(IntentTimelineLength, schedule.CompletionTime);

            GUILayout.Space(8f);
            GUILayout.Label(
                isActive
                    ? $"Live Action Schedule — T0 to T{displayEnd}"
                    : $"Previous Round Schedule — T0 to T{displayEnd}",
                _sectionStyle);
            DrawTimelineTrack(
                "Player",
                TimelineActorSide.Player,
                schedule,
                displayEnd,
                new Color(0.20f, 0.55f, 0.90f, 0.82f));
            DrawTimelineTrack(
                "Enemy",
                TimelineActorSide.Enemy,
                schedule,
                displayEnd,
                new Color(0.88f, 0.28f, 0.24f, 0.82f));
            DrawTimelineRuler(displayEnd);
            GUILayout.Label(
                "Green: Defensive Window   Teal: Super Armor   Yellow: Resolution Point   "
                + "Orange: Resolution Window   Purple: Disruption Pause   "
                + "Magenta: Restart   Red: Cancel / Broken Window   "
                + "Gray: Suppressed Window   Cyan: Current Time",
                _timelineTickStyle);
        }

        private void DrawTimelineTrack(
            string label,
            TimelineActorSide side,
            InitialTimelineSchedule schedule,
            int displayEnd,
            Color actionColor,
            bool drawCurrentTime = true)
        {
            const float labelWidth = 70f;
            Rect rowRect = GUILayoutUtility.GetRect(
                100f,
                42f,
                GUILayout.ExpandWidth(true));
            Rect trackRect = new Rect(
                rowRect.x + labelWidth,
                rowRect.y,
                Mathf.Max(1f, rowRect.width - labelWidth),
                rowRect.height);
            GUI.Label(
                new Rect(rowRect.x, rowRect.y, labelWidth - 4f, rowRect.height),
                label,
                _timelineStyle);
            GUI.Box(trackRect, GUIContent.none);
            DrawTimelineGrid(trackRect, displayEnd);

            for (int index = 0; index < schedule.Actions.Count; index++)
            {
                ScheduledTimelineAction action = schedule.Actions[index];
                if (action.Side != side)
                {
                    continue;
                }

                float startRatio = Mathf.Clamp01(action.StartTime / (float)displayEnd);
                float endRatio = Mathf.Clamp01(action.EndTime / (float)displayEnd);
                Rect actionRect = new Rect(
                    trackRect.x + trackRect.width * startRatio + 1f,
                    trackRect.y + 3f,
                    Mathf.Max(3f, trackRect.width * (endRatio - startRatio) - 2f),
                    trackRect.height - 6f);

                Color previousColor = GUI.color;
                GUI.color = action.IsCanceled
                    ? new Color(0.38f, 0.40f, 0.44f, 0.62f)
                    : actionColor;
                GUI.DrawTexture(actionRect, Texture2D.whiteTexture);
                GUI.color = previousColor;

                string actionLabel = action.IsChargeable
                    ? $"{action.DisplayName} [Charge]"
                    : action.DisplayName;
                if (action.AccumulatedPauseDuration > 0)
                {
                    actionLabel += $" [Paused +{action.AccumulatedPauseDuration}]";
                }

                if (action.Restarts.Count > 0)
                {
                    actionLabel += $" [Restarted x{action.Restarts.Count}]";
                }

                if (action.IsCanceled)
                {
                    actionLabel += action.Cancellation.DependencyFailed
                        ? " [Canceled: Dependency]"
                        : " [Canceled]";
                }

                GUI.Label(actionRect, actionLabel, _timelineActionStyle);

                if (action.IsCanceled)
                {
                    ScheduledActionCancellation cancellation = action.Cancellation;
                    float cancelPoint = cancellation.CancellationTime / (float)displayEnd;
                    GUI.color = new Color(1f, 0.24f, 0.24f, 1f);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * cancelPoint - 2f,
                            actionRect.y,
                            4f,
                            actionRect.height),
                        Texture2D.whiteTexture);
                    if (!cancellation.DependencyFailed)
                    {
                        float recoveryEnd = cancellation.RecoveryEndTime
                            / (float)displayEnd;
                        GUI.DrawTexture(
                            new Rect(
                                trackRect.x + trackRect.width * cancelPoint,
                                actionRect.yMax - 5f,
                                Mathf.Max(
                                    3f,
                                    trackRect.width * (recoveryEnd - cancelPoint)),
                                5f),
                            Texture2D.whiteTexture);
                    }

                    GUI.color = previousColor;
                }

                for (int restartIndex = 0;
                    restartIndex < action.Restarts.Count;
                    restartIndex++)
                {
                    ScheduledActionRestart restart = action.Restarts[restartIndex];
                    float attemptStart = restart.PreviousStartTime / (float)displayEnd;
                    float disruptionPoint = restart.DisruptionTime / (float)displayEnd;
                    float restartPoint = restart.RestartTime / (float)displayEnd;
                    GUI.color = new Color(0.76f, 0.24f, 0.76f, 0.40f);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * attemptStart,
                            actionRect.y + 6f,
                            Mathf.Max(
                                3f,
                                trackRect.width * (disruptionPoint - attemptStart)),
                            actionRect.height - 12f),
                        Texture2D.whiteTexture);
                    GUI.color = new Color(1f, 0.22f, 0.72f, 1f);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * disruptionPoint - 1f,
                            actionRect.y,
                            2f,
                            actionRect.height),
                        Texture2D.whiteTexture);
                    GUI.color = new Color(0.72f, 0.36f, 0.94f, 1f);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * disruptionPoint,
                            actionRect.yMax - 5f,
                            Mathf.Max(
                                3f,
                                trackRect.width * (restartPoint - disruptionPoint)),
                            5f),
                        Texture2D.whiteTexture);
                    GUI.color = previousColor;
                }

                for (int windowIndex = 0;
                    windowIndex < action.ActiveWindows.Count;
                    windowIndex++)
                {
                    ScheduledActiveWindow window = action.ActiveWindows[windowIndex];
                    float activeStart = window.StartTime / (float)displayEnd;
                    float activeEnd = window.EffectiveEndTime / (float)displayEnd;
                    GUI.color = window.Effect == ActiveWindowEffect.SuperArmor
                        ? new Color(0.18f, 0.92f, 0.88f, 1f)
                        : new Color(0.25f, 0.92f, 0.48f, 1f);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * activeStart,
                            actionRect.y,
                            Mathf.Max(3f, trackRect.width * (activeEnd - activeStart)),
                            5f),
                        Texture2D.whiteTexture);

                    if (window.BreakTime.HasValue)
                    {
                        float breakPoint = window.BreakTime.Value / (float)displayEnd;
                        float originalEnd = window.EndTime / (float)displayEnd;
                        GUI.color = new Color(1f, 0.24f, 0.24f, 1f);
                        GUI.DrawTexture(
                            new Rect(
                                trackRect.x + trackRect.width * breakPoint - 1f,
                                actionRect.y,
                                3f,
                                5f),
                            Texture2D.whiteTexture);
                        GUI.color = new Color(0.30f, 0.34f, 0.38f, 0.82f);
                        GUI.DrawTexture(
                            new Rect(
                                trackRect.x + trackRect.width * breakPoint,
                                actionRect.y,
                                Mathf.Max(
                                    2f,
                                    trackRect.width * (originalEnd - breakPoint)),
                                5f),
                            Texture2D.whiteTexture);
                    }

                    for (int suppressionIndex = 0;
                        suppressionIndex < window.Suppressions.Count;
                        suppressionIndex++)
                    {
                        ScheduledActiveWindowSuppression suppression =
                            window.Suppressions[suppressionIndex];
                        float suppressionStart = Mathf.Max(
                            window.StartTime,
                            suppression.StartTime) / (float)displayEnd;
                        float suppressionEnd = Mathf.Min(
                            window.EndTime,
                            suppression.EndTime) / (float)displayEnd;
                        if (suppressionEnd <= suppressionStart)
                        {
                            continue;
                        }

                        GUI.color = new Color(0.42f, 0.46f, 0.52f, 1f);
                        GUI.DrawTexture(
                            new Rect(
                                trackRect.x + trackRect.width * suppressionStart,
                                actionRect.y,
                                Mathf.Max(
                                    2f,
                                    trackRect.width * (suppressionEnd - suppressionStart)),
                                5f),
                            Texture2D.whiteTexture);
                    }

                    GUI.color = previousColor;
                }

                for (int pauseIndex = 0;
                    pauseIndex < action.DisruptionPauses.Count;
                    pauseIndex++)
                {
                    ScheduledDisruptionPause pause = action.DisruptionPauses[pauseIndex];
                    float pauseStart = pause.StartTime / (float)displayEnd;
                    float pauseEnd = pause.EndTime / (float)displayEnd;
                    GUI.color = new Color(0.72f, 0.36f, 0.94f, 1f);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * pauseStart,
                            actionRect.yMax - 5f,
                            Mathf.Max(3f, trackRect.width * (pauseEnd - pauseStart)),
                            5f),
                        Texture2D.whiteTexture);
                    GUI.color = previousColor;
                }

                if (action.IsCanceled)
                {
                    continue;
                }

                if (action.HasResolutionWindow)
                {
                    float windowStart = action.ResolutionWindowStartTime.Value
                        / (float)displayEnd;
                    float windowEnd = action.ResolutionWindowEndTime.Value
                        / (float)displayEnd;
                    GUI.color = new Color(0.96f, 0.62f, 0.16f, 1f);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * windowStart,
                            actionRect.yMax - 5f,
                            Mathf.Max(3f, trackRect.width * (windowEnd - windowStart)),
                            5f),
                        Texture2D.whiteTexture);
                    GUI.color = previousColor;
                }

                for (int resolutionIndex = 0;
                    resolutionIndex < action.ResolutionEvents.Count;
                    resolutionIndex++)
                {
                    ScheduledResolutionEvent resolution =
                        action.ResolutionEvents[resolutionIndex];
                    float resolutionRatio = Mathf.Clamp01(
                        resolution.Time / (float)displayEnd);
                    GUI.color = IntentColor(resolution.Effect);
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x + trackRect.width * resolutionRatio - 1f,
                            actionRect.y,
                            2f,
                            actionRect.height),
                        Texture2D.whiteTexture);
                }
                GUI.color = previousColor;
            }

            if (drawCurrentTime)
            {
                float currentRatio = Mathf.Clamp01(
                    _playbackTimeStep / (float)displayEnd);
                Color originalColor = GUI.color;
                GUI.color = new Color(0.35f, 1f, 1f, 1f);
                GUI.DrawTexture(
                    new Rect(
                        trackRect.x + trackRect.width * currentRatio - 1f,
                        trackRect.y,
                        2f,
                        trackRect.height),
                    Texture2D.whiteTexture);
                GUI.color = originalColor;
            }
        }

        private static void DrawTimelineGrid(Rect trackRect, int displayEnd)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            for (int time = 0; time <= displayEnd; time += 5)
            {
                float ratio = time / (float)displayEnd;
                float x = trackRect.x + trackRect.width * ratio;
                GUI.DrawTexture(
                    new Rect(x, trackRect.y, 1f, trackRect.height),
                    Texture2D.whiteTexture);
            }

            GUI.color = previousColor;
        }

        private void DrawTimelineRuler(int displayEnd)
        {
            const float labelWidth = 70f;
            Rect rulerRect = GUILayoutUtility.GetRect(
                100f,
                18f,
                GUILayout.ExpandWidth(true));
            Rect trackRect = new Rect(
                rulerRect.x + labelWidth,
                rulerRect.y,
                Mathf.Max(1f, rulerRect.width - labelWidth),
                rulerRect.height);
            for (int time = 0; time <= displayEnd; time += 5)
            {
                float ratio = time / (float)displayEnd;
                float x = trackRect.x + trackRect.width * ratio;
                GUI.Label(
                    new Rect(x - 14f, trackRect.y, 28f, trackRect.height),
                    time.ToString(),
                    _timelineTickStyle);
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

            if (_previewSchedule != null && _previewedComboIndex >= 0)
            {
                int displayEnd = Mathf.Max(
                    IntentTimelineLength,
                    _previewSchedule.CompletionTime);
                GUILayout.Label(
                    $"Round Preview: {_battle.Combos[_previewedComboIndex].Name} vs. "
                    + $"{_battle.EnemySequence.DisplayName}");
                DrawTimelineTrack(
                    "Player Preview",
                    TimelineActorSide.Player,
                    _previewSchedule,
                    displayEnd,
                    new Color(0.20f, 0.55f, 0.90f, 0.82f),
                    drawCurrentTime: false);
                DrawTimelineTrack(
                    "Enemy Intent",
                    TimelineActorSide.Enemy,
                    _previewSchedule,
                    displayEnd,
                    new Color(0.88f, 0.28f, 0.24f, 0.82f),
                    drawCurrentTime: false);
                DrawTimelineRuler(displayEnd);
                GUILayout.Label(
                    "Preview uses default Resolution Points and Charge Level 0. "
                    + "Disruptions may dynamically change the live schedule.",
                    _timelineTickStyle);
                DrawEnemyIntentDetails();
                return;
            }

            Rect timelineRect = GUILayoutUtility.GetRect(
                100f,
                28f,
                GUILayout.ExpandWidth(true));
            GUI.Box(timelineRect, GUIContent.none);

            Color previousColor = GUI.color;
            for (int index = 0; index < _battle.EnemyIntent.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _battle.EnemyIntent.Actions[index];
                for (int resolutionIndex = 0;
                    resolutionIndex < action.ResolutionEvents.Count;
                    resolutionIndex++)
                {
                    ScheduledResolutionEvent resolution =
                        action.ResolutionEvents[resolutionIndex];
                    if (resolution.Effect == ActionResolutionEffect.None)
                    {
                        continue;
                    }

                    GUI.color = IntentColor(resolution.Effect);
                    int resolutionTime = resolution.Time;
                    float startRatio = Mathf.Clamp01(
                        (resolutionTime - 1f) / IntentTimelineLength);
                    float endRatio = Mathf.Clamp01(
                        resolutionTime / (float)IntentTimelineLength);
                    Rect marker = new Rect(
                        timelineRect.x + timelineRect.width * startRatio,
                        timelineRect.y + 2f,
                        Mathf.Max(3f, timelineRect.width * (endRatio - startRatio)),
                        timelineRect.height - 4f);
                    GUI.DrawTexture(marker, Texture2D.whiteTexture);
                }
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

            DrawEnemyIntentDetails();
            GUILayout.Label(
                "Select a prepared Combo below to add a time-aligned Player Preview.",
                _timelineTickStyle);
        }

        private void DrawEnemyIntentDetails()
        {
            for (int index = 0; index < _battle.EnemyIntent.Actions.Count; index++)
            {
                ScheduledTimelineAction action = _battle.EnemyIntent.Actions[index];
                for (int resolutionIndex = 0;
                    resolutionIndex < action.ResolutionEvents.Count;
                    resolutionIndex++)
                {
                    ScheduledResolutionEvent resolution =
                        action.ResolutionEvents[resolutionIndex];
                    string effect = resolution.Effect.ToString();
                    if (resolution.StatusEffect != null)
                    {
                        effect += $" ({resolution.StatusEffect.DisplayName})";
                    }

                    if (resolution.DamageTags != DamageResolutionTag.None)
                    {
                        effect += $" [{resolution.DamageTags}]";
                    }

                    if (resolution.OnEvadedDisruption != null)
                    {
                        effect += $" | On Evaded: self-{resolution.OnEvadedDisruption.Type} "
                            + $"for {resolution.OnEvadedDisruption.Duration} Time Steps";
                    }

                    GUILayout.Label(
                        $"T{resolution.Time}: {action.DisplayName} — {effect}");
                }
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
            GUILayout.Label(
                "Blue: system/QTE   Green: player result   Red: enemy result   "
                + "Magenta: disruption   Yellow: avoided",
                _timelineTickStyle);
            GUILayoutOption heightOption = expandHeight
                ? GUILayout.ExpandHeight(true)
                : GUILayout.MinHeight(240f);
            _logScrollPosition = GUILayout.BeginScrollView(
                _logScrollPosition,
                GUI.skin.box,
                GUILayout.MinHeight(expandHeight ? 300f : 240f),
                heightOption);
            Color previousContentColor = GUI.contentColor;
            for (int index = 0; index < _combatLog.Count; index++)
            {
                CombatLogEntry entry = _combatLog[index];
                GUI.contentColor = CombatLogColor(entry.Tone);
                GUILayout.Label(entry.Message, _logStyle);
            }

            GUI.contentColor = previousContentColor;
            GUILayout.EndScrollView();
        }

        private void PreviewCombo(int comboIndex)
        {
            SoloComboExecutionFailure failure = _battle.GetExecutionFailure(comboIndex);
            if (failure != SoloComboExecutionFailure.None)
            {
                AppendLog(
                    $"Could not preview {_battle.Combos[comboIndex].Name}: "
                    + $"{FailureText(failure)}.",
                    CombatLogTone.Error);
                return;
            }

            _previewedComboIndex = comboIndex;
            _previewSchedule = _battle.PreviewCombo(comboIndex);
        }

        private void ClearComboPreview()
        {
            _previewedComboIndex = -1;
            _previewSchedule = null;
        }

        private void ExecuteCombo(int comboIndex)
        {
            ComboConfiguration combo = _battle.Combos[comboIndex];
            SoloComboExecutionFailure failure = _battle.BeginCombo(comboIndex);

            if (failure != SoloComboExecutionFailure.None)
            {
                AppendLog(
                    $"Could not execute {combo.Name}: {FailureText(failure)}.",
                    CombatLogTone.Error);
                RefreshPresentation();
                return;
            }

            ClearComboPreview();
            AppendLog(
                $"Player committed {combo.Name}. Timeline resolution started.",
                CombatLogTone.System);
            BeginTimelinePlayback();
        }

        private void WaitTurn()
        {
            SoloComboExecutionFailure failure = _battle.BeginWaitTurn();
            if (failure != SoloComboExecutionFailure.None)
            {
                AppendLog(
                    $"Could not wait: {FailureText(failure)}.",
                    CombatLogTone.Error);
                return;
            }

            ClearComboPreview();
            AppendLog(
                "Player waited. Timeline resolution started.",
                CombatLogTone.System);
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
            _lastCompletedTimeline = null;
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
            ClearComboPreview();
            _battle = null;
            _setupError = null;
            _combatLog.Clear();
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
                    attack: loadoutSession.EquippedWeapon?.WeaponAttack ?? 50,
                    defense: 50));

            var enemy = new CombatantState(
                "Training Enemy",
                new CombatantStats(
                    maxHealth: 100,
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

            // The seeded first intent is an interactive Sustained Attack drill.
            // combo3 evades the T1 hit but not the T3 hit, making the independent
            // per-hit avoidance checks immediately visible in Play Mode.
            var sustainedBarrage = new EnemyActionSequence(
                "training.sustained-barrage-pattern",
                "Sustained Barrage",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.twin-sweep",
                        "Twin Sweep",
                        timelineCost: 4,
                        resolutionOffset: 1,
                        damageMultiplier: 0.30d,
                        disruptionResponseSegments: PauseOnStagger(timelineCost: 4),
                        sustainedAttack: new SustainedAttackDefinition(
                            windowStartOffset: 1,
                            windowEndOffset: 3,
                            firstHitOffset: 1,
                            hitInterval: 2,
                            maximumHitCount: 2))
                });

            var overcommitFeint = new EnemyActionSequence(
                "training.overcommit-feint-pattern",
                "Overcommit Feint",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.committed-lunge",
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
                        "training.feint-follow-up",
                        "Feint Follow-up",
                        timelineCost: 3,
                        resolutionOffset: 3,
                        damageMultiplier: 0.35d,
                        disruptionResponseSegments: PauseOnStagger(timelineCost: 3))
                });

            var clashDrill = new EnemyActionSequence(
                "training.clash-drill-pattern",
                "Clash Drill",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.clash-strike",
                        "Clash Strike",
                        timelineCost: 10,
                        resolutionOffset: 9,
                        damageMultiplier: 0.50d,
                        onHitDisruption: new ActionDisruptionDefinition(
                            ActionDisruptionType.Stagger,
                            duration: 2))
                });

            var exposeClaw = new EnemyActionSequence(
                "training.claw-pattern",
                "Claw Pattern",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.expose",
                        "Expose",
                        timelineCost: 2,
                        resolutionOffset: 2,
                        damageMultiplier: 0d,
                        resolutionEffect: ActionResolutionEffect.ApplyDebuff,
                        statusEffect: exposed,
                        statusTarget: ActionStatusTarget.Opponent),
                    new EnemyActionDefinition(
                        "training.claw",
                        "Claw",
                        timelineCost: 3,
                        resolutionOffset: 2,
                        damageMultiplier: 0.40d,
                        disruptionResponseSegments: PauseOnStagger(timelineCost: 3))
                });

            var recklessAssault = new EnemyActionSequence(
                "training.reckless-assault-pattern",
                "Reckless Assault",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.reckless-lunge",
                        "Reckless Lunge",
                        timelineCost: 4,
                        resolutionOffset: 1,
                        damageMultiplier: 0.55d,
                        disruptionResponseSegments: new[]
                        {
                            new DisruptionResponseSegmentDefinition(
                                startOffset: 1,
                                endOffset: 4,
                                flinchResponse: ActionDisruptionResponse.PauseAndResume,
                                staggerResponse: ActionDisruptionResponse.Continue,
                                knockdownResponse: ActionDisruptionResponse.CancelAction)
                        },
                        onGuardedDisruption: new ActionDisruptionDefinition(
                            ActionDisruptionType.Flinch,
                            duration: 2)),
                    new EnemyActionDefinition(
                        "training.reckless-follow-up",
                        "Follow-up Slash",
                        timelineCost: 3,
                        resolutionOffset: 3,
                        damageMultiplier: 0.35d,
                        disruptionResponseSegments: PauseOnStagger(timelineCost: 3))
                });

            var armoredAdvance = new EnemyActionSequence(
                "training.armored-advance-pattern",
                "Armored Advance",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.armored-thrust",
                        "Armored Thrust",
                        timelineCost: 5,
                        resolutionOffset: 5,
                        damageMultiplier: 0.45d,
                        activeWindows: new[]
                        {
                            new ActionActiveWindowDefinition(
                                "training.armored-thrust.super-armor",
                                "Super Armor Lv2",
                                startOffset: 1,
                                endOffset: 5,
                                ActiveWindowEffect.SuperArmor,
                                damageReduction: 0d,
                                ActiveWindowDisruptionPolicy.PersistThroughDisruption,
                                superArmorLevel: 2)
                        },
                        disruptionResponseSegments: new[]
                        {
                            new DisruptionResponseSegmentDefinition(
                                startOffset: 1,
                                endOffset: 5,
                                flinchResponse: ActionDisruptionResponse.PauseAndResume,
                                staggerResponse: ActionDisruptionResponse.CancelAction,
                                knockdownResponse: ActionDisruptionResponse.CancelAction)
                        })
                });

            var heavySwing = new EnemyActionSequence(
                "training.heavy-swing-pattern",
                "Heavy Swing",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.heavy-swing",
                        "Heavy Swing",
                        timelineCost: 5,
                        resolutionOffset: 5,
                        damageMultiplier: 0.65d,
                        damageTags: DamageResolutionTag.Overcommit,
                        activeWindows: new[]
                        {
                            new ActionActiveWindowDefinition(
                                "training.heavy-guard",
                                "Heavy Guard",
                                startOffset: 1,
                                endOffset: 5,
                                ActiveWindowEffect.DefensiveState,
                                damageReduction: 0.30d,
                                ActiveWindowDisruptionPolicy.BreakOnDisruption)
                        },
                        disruptionResponseSegments: RestartOnStagger(timelineCost: 5))
                });

            var jab = new EnemyActionSequence(
                "training.fallback-jab",
                "Jab",
                new[]
                {
                    new EnemyActionDefinition(
                        "training.jab",
                        "Jab",
                        timelineCost: 3,
                        resolutionOffset: 3,
                        damageMultiplier: 0.30d,
                        disruptionResponseSegments: CancelOnStagger(timelineCost: 3))
                });

            var enemyBehavior = new EnemyBehaviorTable(
                new[]
                {
                    new EnemyBehaviorEntry(
                        sustainedBarrage,
                        selectionWeight: 1,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        overcommitFeint,
                        selectionWeight: 1,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        clashDrill,
                        selectionWeight: 1,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        recklessAssault,
                        selectionWeight: 1,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        armoredAdvance,
                        selectionWeight: 1,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        exposeClaw,
                        selectionWeight: 2,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        heavySwing,
                        selectionWeight: 1,
                        preventImmediateRepeat: true),
                    new EnemyBehaviorEntry(
                        jab,
                        selectionWeight: 2,
                        preventImmediateRepeat: true)
                },
                jab);

            _battle = new SoloComboBattleState(
                player,
                enemy,
                combos,
                enemyBehavior,
                ManaRecoveryPerTurn,
                StaminaRecoveryPerTurn,
                enemyRandom: new System.Random(594));
            AppendLog(
                "Battle started. Choose a prepared Combo to preview.",
                CombatLogTone.System);
            RefreshPresentation();
        }

        private static IReadOnlyList<DisruptionResponseSegmentDefinition> PauseOnStagger(
            int timelineCost)
        {
            return new[]
            {
                new DisruptionResponseSegmentDefinition(
                    startOffset: 1,
                    endOffset: timelineCost,
                    flinchResponse: ActionDisruptionResponse.Continue,
                    staggerResponse: ActionDisruptionResponse.PauseAndResume,
                    knockdownResponse: ActionDisruptionResponse.PauseAndResume)
            };
        }

        private static IReadOnlyList<DisruptionResponseSegmentDefinition> RestartOnStagger(
            int timelineCost)
        {
            return new[]
            {
                new DisruptionResponseSegmentDefinition(
                    startOffset: 1,
                    endOffset: timelineCost,
                    flinchResponse: ActionDisruptionResponse.Continue,
                    staggerResponse: ActionDisruptionResponse.RestartAction,
                    knockdownResponse: ActionDisruptionResponse.RestartAction)
            };
        }

        private static IReadOnlyList<DisruptionResponseSegmentDefinition> CancelOnStagger(
            int timelineCost)
        {
            return new[]
            {
                new DisruptionResponseSegmentDefinition(
                    startOffset: 1,
                    endOffset: timelineCost,
                    flinchResponse: ActionDisruptionResponse.Continue,
                    staggerResponse: ActionDisruptionResponse.CancelAction,
                    knockdownResponse: ActionDisruptionResponse.CancelAction)
            };
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
                _playbackEndTimeStep = _battle.TimelineCompletionTime;

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
                        + $"T{selectedTimeStep}.",
                CombatLogTone.System);
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
            AppendLog(
                $"  Charge QTE: {request.DisplayName} {reason}.",
                CombatLogTone.System);

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
            _lastCompletedTimeline = result.Schedule;
            if (result.ManaRecovered > 0 || result.StaminaRecovered > 0)
            {
                AppendLog(
                    $"Round End: Player recovered {result.ManaRecovered} Mana and "
                    + $"{result.StaminaRecovered} Stamina.",
                    CombatLogTone.Success);
            }

            if (result.Outcome != BattleOutcome.Ongoing)
            {
                AppendLog(
                    OutcomeText(result.Outcome),
                    result.Outcome == BattleOutcome.PlayerVictory
                        ? CombatLogTone.Success
                        : CombatLogTone.Error);
            }
            else
            {
                AppendLog(
                    $"Next Enemy Intent locked: {_battle.EnemySequence.DisplayName}.",
                    CombatLogTone.System);
            }

            ClearComboPreview();
            _isTimelinePlaying = false;
            _timelinePlayback = null;
            RefreshPresentation();
        }

        private void AppendLog(
            string message,
            CombatLogTone tone = CombatLogTone.Normal)
        {
            _combatLog.Add(new CombatLogEntry(message, tone));
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
                    + "its Damage and dependent On Hit effects failed.",
                    CombatLogTone.Avoided);
            }
            else if (resolution.DamageAvoidance == DamageAvoidanceResult.Evaded)
            {
                AppendLog(
                    $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} was Evaded; "
                    + "its Damage and dependent On Hit effects failed.",
                    CombatLogTone.Avoided);
            }
            else if (resolution.ResolutionEffect == ActionResolutionEffect.Damage)
            {
                string tags = resolution.DamageTags == DamageResolutionTag.None
                    ? string.Empty
                    : $" [{resolution.DamageTags}]";
                string defense = resolution.DefensiveDamageReduction > 0d
                    ? $" [Defensive State -{resolution.DefensiveDamageReduction * 100d:0}%]"
                    : string.Empty;
                string clash = resolution.ClashDamageReduction > 0d
                    ? $" [Clash Defense -{resolution.ClashDamageReduction * 100d:0}%]"
                    : string.Empty;
                AppendLog(
                    $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} dealt "
                    + $"{resolution.DamageDealt} damage{tags}{defense}{clash}.",
                    resolution.Side == TimelineActorSide.Player
                        ? CombatLogTone.Player
                        : CombatLogTone.Enemy);
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
                            + $"{resolution.StatusDisplayName}.",
                    resolution.Side == TimelineActorSide.Player
                        ? CombatLogTone.Player
                        : CombatLogTone.Enemy);
            }
            else
            {
                string effect = resolution.ResolutionEffect == ActionResolutionEffect.None
                    ? "with no prototype combat effect"
                    : $"{resolution.ResolutionEffect}";
                AppendLog(
                    $"  T{resolution.TimeStep}: {actor} {resolution.DisplayName} resolved "
                    + $"{effect}.",
                    resolution.Side == TimelineActorSide.Player
                        ? CombatLogTone.Player
                        : CombatLogTone.Enemy);
            }

            if (resolution.ResolutionEffect == ActionResolutionEffect.Damage)
            {
                AppendDisruptionResolution(resolution);
            }
        }

        private void AppendDisruptionResolution(SoloComboActionResolution resolution)
        {
            string trigger;
            switch (resolution.DisruptionTrigger)
            {
                case ActionDisruptionTrigger.OnGuarded:
                    trigger = "On Guarded response: ";
                    break;
                case ActionDisruptionTrigger.OnEvaded:
                    trigger = "On Evaded Overcommit penalty: ";
                    break;
                default:
                    trigger = string.Empty;
                    break;
            }
            if (resolution.AppliedDisruption != null)
            {
                string target = resolution.DisruptedSide == TimelineActorSide.Player
                    ? "Player"
                    : "Training Enemy";
                string result;
                switch (resolution.DisruptionResponse)
                {
                    case ActionDisruptionResponse.PauseAndResume:
                        result = $"paused the current Action for "
                            + $"{resolution.AppliedDisruption.Duration} Time Steps";
                        break;
                    case ActionDisruptionResponse.RestartAction:
                        result = $"restarted the current Action after "
                            + $"{resolution.AppliedDisruption.Duration} Time Steps";
                        break;
                    case ActionDisruptionResponse.CancelAction:
                        result = $"canceled the current Action; the next Action begins after "
                            + $"{resolution.AppliedDisruption.Duration} Time Steps";
                        break;
                    default:
                        result = "did not alter the current Action schedule";
                        break;
                }

                string armorResult = resolution.AttemptedDisruption != null
                    && resolution.AttemptedDisruption.Type
                        != resolution.AppliedDisruption.Type
                        ? $"Super Armor Lv{resolution.SuperArmorLevel} reduced "
                            + $"{resolution.AttemptedDisruption.Type} to "
                            + $"{resolution.AppliedDisruption.Type}. "
                        : string.Empty;
                AppendLog(
                    $"    {trigger}{armorResult}{resolution.AppliedDisruption.Type} on "
                    + $"{target}: {result}.",
                    CombatLogTone.Disruption);
            }
            else if (resolution.AttemptedDisruption != null
                && resolution.SuperArmorLevel > 0)
            {
                AppendLog(
                    $"    {trigger}Super Armor Lv{resolution.SuperArmorLevel} negated "
                    + $"{resolution.AttemptedDisruption.Type} on the target.",
                    CombatLogTone.Avoided);
            }
        }

        private static Color CombatLogColor(CombatLogTone tone)
        {
            switch (tone)
            {
                case CombatLogTone.System:
                    return new Color(0.45f, 0.82f, 1f, 1f);
                case CombatLogTone.Player:
                    return new Color(0.45f, 1f, 0.58f, 1f);
                case CombatLogTone.Enemy:
                    return new Color(1f, 0.52f, 0.48f, 1f);
                case CombatLogTone.Avoided:
                    return new Color(1f, 0.86f, 0.30f, 1f);
                case CombatLogTone.Disruption:
                    return new Color(1f, 0.48f, 0.92f, 1f);
                case CombatLogTone.Error:
                    return new Color(1f, 0.38f, 0.34f, 1f);
                case CombatLogTone.Success:
                    return new Color(0.48f, 1f, 0.66f, 1f);
                default:
                    return Color.white;
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
                wordWrap = true,
                fontSize = 14,
                padding = new RectOffset(4, 4, 2, 2)
            };

            _timelineStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            _timelineActionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _timelineTickStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                clipping = TextClipping.Clip,
                fontSize = 10
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
