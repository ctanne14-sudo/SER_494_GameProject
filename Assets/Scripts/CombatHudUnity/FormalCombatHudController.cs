using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatSandbox;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SER494.Gameplay.CombatHud.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FormalCombatHudController : MonoBehaviour
    {
        private const int TimelineCellCount = 30;
        private const int VisibleStatusCount = 4;

        private readonly List<Button> _comboButtons = new List<Button>();
        private readonly List<VisualElement> _playerTimelineCells =
            new List<VisualElement>();
        private readonly List<VisualElement> _enemyTimelineCells =
            new List<VisualElement>();

        private VisualElement _root;
        private Label _roundLabel;
        private Label _playerName;
        private ProgressBar _playerHealth;
        private ProgressBar _playerStamina;
        private ProgressBar _playerMana;
        private Label _enemyName;
        private ProgressBar _enemyHealth;
        private VisualElement _playerStatuses;
        private VisualElement _enemyStatuses;
        private Label _healingPotionCount;
        private Label _greaterHealingPotionCount;
        private Label _vitalityTonicCount;
        private Label _timelinePageLabel;
        private Label _timelineStateLabel;
        private Button _previousTimelinePageButton;
        private Button _nextTimelinePageButton;
        private VisualElement _comboList;
        private Button _clearPreviewButton;
        private CombatSandboxController _sandbox;
        private CombatSandboxController _builtForSandbox;
        private bool _isVisible;
        private int _lastTimelineMarker = -1;
        private int _timelinePageStart = 1;
        private int _renderedTimelinePageStart = -1;
        private InitialTimelineSchedule _renderedTimelineSchedule;
        private string _lastPlayerStatusSignature;
        private string _lastEnemyStatusSignature;

        private void Awake()
        {
            VisualElement documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _root = documentRoot.Q<VisualElement>("combatHudRoot");
            _roundLabel = documentRoot.Q<Label>("roundLabel");
            _playerName = documentRoot.Q<Label>("playerName");
            _playerHealth = documentRoot.Q<ProgressBar>("playerHealth");
            _playerStamina = documentRoot.Q<ProgressBar>("playerStamina");
            _playerMana = documentRoot.Q<ProgressBar>("playerMana");
            _enemyName = documentRoot.Q<Label>("enemyName");
            _enemyHealth = documentRoot.Q<ProgressBar>("enemyHealth");
            _playerStatuses = documentRoot.Q<VisualElement>("playerStatuses");
            _enemyStatuses = documentRoot.Q<VisualElement>("enemyStatuses");
            _healingPotionCount = documentRoot.Q<Label>("healingPotionCount");
            _greaterHealingPotionCount =
                documentRoot.Q<Label>("greaterHealingPotionCount");
            _vitalityTonicCount = documentRoot.Q<Label>("vitalityTonicCount");
            _timelinePageLabel = documentRoot.Q<Label>("timelinePageLabel");
            _timelineStateLabel = documentRoot.Q<Label>("timelineStateLabel");
            _previousTimelinePageButton =
                documentRoot.Q<Button>("previousTimelinePageButton");
            _nextTimelinePageButton =
                documentRoot.Q<Button>("nextTimelinePageButton");
            _comboList = documentRoot.Q<VisualElement>("comboList");
            _clearPreviewButton = documentRoot.Q<Button>("clearPreviewButton");
            _clearPreviewButton.clicked += ClearPreview;
            _previousTimelinePageButton.clicked += ShowPreviousTimelinePage;
            _nextTimelinePageButton.clicked += ShowNextTimelinePage;

            CreateTimelineCells(
                documentRoot.Q<VisualElement>("playerTimelineCells"),
                _playerTimelineCells);
            CreateTimelineCells(
                documentRoot.Q<VisualElement>("enemyTimelineCells"),
                _enemyTimelineCells);
            _root.style.display = DisplayStyle.None;
            _isVisible = false;
        }

        private void OnDestroy()
        {
            if (_clearPreviewButton != null)
            {
                _clearPreviewButton.clicked -= ClearPreview;
            }

            if (_previousTimelinePageButton != null)
            {
                _previousTimelinePageButton.clicked -= ShowPreviousTimelinePage;
            }

            if (_nextTimelinePageButton != null)
            {
                _nextTimelinePageButton.clicked -= ShowNextTimelinePage;
            }
        }

        private void Update()
        {
            if (_sandbox == null)
            {
                _sandbox = Object.FindAnyObjectByType<CombatSandboxController>();
            }

            bool shouldShow = _sandbox != null && _sandbox.ShouldShowFormalBattleHud;
            SetVisible(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            if (!ReferenceEquals(_sandbox, _builtForSandbox))
            {
                BuildComboCards();
                _builtForSandbox = _sandbox;
            }

            HandleComboShortcuts();
            RefreshPresentation();
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void BuildComboCards()
        {
            _comboList.Clear();
            _comboButtons.Clear();
            SoloComboBattleState battle = _sandbox.CurrentBattle;
            if (battle == null)
            {
                return;
            }

            for (int index = 0; index < battle.Combos.Count; index++)
            {
                int comboIndex = index;
                var button = new Button(() => SelectCombo(comboIndex));
                button.AddToClassList("combat-hud-combo-card");
                button.tooltip =
                    "Select once to preview. Select the same Combo again to confirm.";
                _comboButtons.Add(button);
                _comboList.Add(button);
            }
        }

        private void RefreshPresentation()
        {
            SoloComboBattleState battle = _sandbox.CurrentBattle;
            if (battle == null)
            {
                return;
            }

            _roundLabel.text = $"ROUND {_sandbox.CurrentRoundNumber}";
            SetCombatantPresentation(
                battle.Player,
                _playerName,
                _playerHealth,
                _playerStatuses,
                ref _lastPlayerStatusSignature);
            SetResourceBar(
                _playerStamina,
                "Stamina",
                battle.Player.CurrentStamina,
                battle.Player.Stats.MaxStamina);
            SetResourceBar(
                _playerMana,
                "Mana",
                battle.Player.CurrentMana,
                battle.Player.Stats.MaxMana);
            _playerMana.style.display = battle.Player.Stats.MaxMana > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            SetCombatantPresentation(
                battle.Enemy,
                _enemyName,
                _enemyHealth,
                _enemyStatuses,
                ref _lastEnemyStatusSignature);

            _healingPotionCount.text = battle.GetItemCount(
                PlayerStateInitialValues.HealingPotionId).ToString();
            _greaterHealingPotionCount.text = battle.GetItemCount(
                PlayerStateInitialValues.GreaterHealingPotionId).ToString();
            _vitalityTonicCount.text = battle.GetItemCount(
                PlayerStateInitialValues.VitalityTonicId).ToString();

            RefreshComboCards(battle);
            RefreshTimelineState(battle);
        }

        private void RefreshComboCards(SoloComboBattleState battle)
        {
            for (int index = 0; index < _comboButtons.Count; index++)
            {
                Button button = _comboButtons[index];
                ComboConfiguration combo = battle.Combos[index];
                int cooldown = battle.GetCooldownRemaining(index);
                bool previewed = index == _sandbox.PreviewedComboIndex;
                SoloComboExecutionFailure failure = battle.GetExecutionFailure(index);
                bool available = failure == SoloComboExecutionFailure.None;

                button.text =
                    $"{index + 1}  {combo.Name}\n"
                    + $"STA {combo.TotalStaminaCost}  MANA {combo.TotalManaCost}  "
                    + $"CD {cooldown}"
                    + (previewed ? "\nCONFIRM" : string.Empty);
                button.EnableInClassList("combat-hud-combo-previewed", previewed);
                button.SetEnabled(!_sandbox.IsTimelinePlaying && available);
                button.tooltip = available
                    ? "Select once to preview. Select again to confirm."
                    : _sandbox.GetFormalComboAvailabilityText(index);
            }

            _clearPreviewButton.style.display = _sandbox.PreviewedComboIndex >= 0
                && !_sandbox.IsTimelinePlaying
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void RefreshTimelineState(SoloComboBattleState battle)
        {
            InitialTimelineSchedule schedule = _sandbox.FormalTimelineSchedule;
            bool scheduleChanged = !ReferenceEquals(
                schedule,
                _renderedTimelineSchedule);
            int maximumTimeStep = Mathf.Max(
                TimelineCellCount,
                schedule?.CompletionTime ?? TimelineCellCount);

            if (_sandbox.IsTimelinePlaying)
            {
                int currentTimeStep = Mathf.Max(1, _sandbox.PlaybackTimeStep);
                _timelinePageStart = PageStartFor(currentTimeStep);
            }
            else if (scheduleChanged)
            {
                _timelinePageStart = 1;
            }

            int finalPageStart = PageStartFor(maximumTimeStep);
            _timelinePageStart = Mathf.Clamp(
                _timelinePageStart,
                1,
                finalPageStart);

            if (scheduleChanged
                || _timelinePageStart != _renderedTimelinePageStart)
            {
                RenderTimeline(
                    schedule,
                    TimelineActorSide.Player,
                    _playerTimelineCells,
                    _timelinePageStart);
                RenderTimeline(
                    schedule,
                    TimelineActorSide.Enemy,
                    _enemyTimelineCells,
                    _timelinePageStart);
                _renderedTimelineSchedule = schedule;
                _renderedTimelinePageStart = _timelinePageStart;
                _lastTimelineMarker = -1;
            }

            int marker = _sandbox.IsTimelinePlaying
                && _sandbox.PlaybackTimeStep >= _timelinePageStart
                && _sandbox.PlaybackTimeStep < _timelinePageStart + TimelineCellCount
                ? _sandbox.PlaybackTimeStep - _timelinePageStart + 1
                : -1;
            if (marker != _lastTimelineMarker)
            {
                for (int index = 0; index < TimelineCellCount; index++)
                {
                    bool current = index + 1 == marker;
                    _playerTimelineCells[index].EnableInClassList(
                        "combat-hud-timeline-current", current);
                    _enemyTimelineCells[index].EnableInClassList(
                        "combat-hud-timeline-current", current);
                }

                _lastTimelineMarker = marker;
            }

            int pageEnd = _timelinePageStart + TimelineCellCount - 1;
            _timelinePageLabel.text = $"T{_timelinePageStart}–T{pageEnd}";
            _previousTimelinePageButton.SetEnabled(
                !_sandbox.IsTimelinePlaying && _timelinePageStart > 1);
            _nextTimelinePageButton.SetEnabled(
                !_sandbox.IsTimelinePlaying
                && _timelinePageStart < finalPageStart);
            if (_sandbox.IsTimelinePlaying)
            {
                _timelineStateLabel.text =
                    $"Resolving T{_sandbox.PlaybackTimeStep} / "
                    + $"{_sandbox.PlaybackEndTimeStep}";
            }
            else if (_sandbox.PreviewedComboIndex >= 0)
            {
                _timelineStateLabel.text =
                    $"Preview: {battle.Combos[_sandbox.PreviewedComboIndex].Name}";
            }
            else
            {
                _timelineStateLabel.text = "Select a Combo to preview this Round";
            }
        }

        private void SelectCombo(int comboIndex)
        {
            if (_sandbox == null || !_sandbox.ShouldShowFormalBattleHud)
            {
                return;
            }

            if (_sandbox.PreviewedComboIndex == comboIndex)
            {
                _sandbox.TryConfirmFormalCombo(comboIndex);
            }
            else
            {
                _sandbox.TryPreviewFormalCombo(comboIndex);
            }
        }

        private void ClearPreview()
        {
            _sandbox?.ClearFormalComboPreview();
        }

        private void ShowPreviousTimelinePage()
        {
            if (_sandbox == null || _sandbox.IsTimelinePlaying)
            {
                return;
            }

            _timelinePageStart = Mathf.Max(1, _timelinePageStart - TimelineCellCount);
        }

        private void ShowNextTimelinePage()
        {
            if (_sandbox == null || _sandbox.IsTimelinePlaying)
            {
                return;
            }

            int maximumTimeStep = Mathf.Max(
                TimelineCellCount,
                _sandbox.FormalTimelineSchedule?.CompletionTime
                    ?? TimelineCellCount);
            int finalPageStart = PageStartFor(maximumTimeStep);
            _timelinePageStart = Mathf.Min(
                finalPageStart,
                _timelinePageStart + TimelineCellCount);
        }

        private void HandleComboShortcuts()
        {
            if (_sandbox.IsTimelinePlaying)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SelectCombo(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) SelectCombo(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) SelectCombo(2);
                else if (keyboard.digit4Key.wasPressedThisFrame) SelectCombo(3);
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return;
            }

            if (gamepad.dpad.up.wasPressedThisFrame) SelectCombo(0);
            else if (gamepad.dpad.right.wasPressedThisFrame) SelectCombo(1);
            else if (gamepad.dpad.down.wasPressedThisFrame) SelectCombo(2);
            else if (gamepad.dpad.left.wasPressedThisFrame) SelectCombo(3);
        }

        private static void SetCombatantPresentation(
            CombatantState combatant,
            Label nameLabel,
            ProgressBar healthBar,
            VisualElement statusRow,
            ref string previousStatusSignature)
        {
            nameLabel.text = combatant.DisplayName;
            SetResourceBar(
                healthBar,
                "HP",
                combatant.CurrentHealth,
                combatant.Stats.MaxHealth);

            string signature = BuildStatusSignature(combatant.ActiveStatuses);
            if (signature == previousStatusSignature)
            {
                return;
            }

            previousStatusSignature = signature;
            statusRow.Clear();
            int visibleCount = Mathf.Min(
                VisibleStatusCount,
                combatant.ActiveStatuses.Count);
            for (int index = 0; index < visibleCount; index++)
            {
                CombatStatusInstance status = combatant.ActiveStatuses[index];
                var chip = new Label(status.Definition.DisplayName);
                chip.AddToClassList("combat-hud-status-chip");
                chip.tooltip = status.Definition.DisplayName;
                statusRow.Add(chip);
            }

            int hiddenCount = combatant.ActiveStatuses.Count - visibleCount;
            if (hiddenCount > 0)
            {
                var overflow = new Label($"+{hiddenCount}");
                overflow.AddToClassList("combat-hud-status-chip");
                overflow.AddToClassList("combat-hud-status-overflow");
                statusRow.Add(overflow);
            }
        }

        private static void SetResourceBar(
            ProgressBar bar,
            string resourceName,
            int current,
            int maximum)
        {
            bar.lowValue = 0f;
            bar.highValue = Mathf.Max(1, maximum);
            bar.value = current;
            bar.title = $"{resourceName}  {current} / {maximum}";
        }

        private static string BuildStatusSignature(
            IReadOnlyList<CombatStatusInstance> statuses)
        {
            if (statuses.Count == 0)
            {
                return string.Empty;
            }

            var parts = new string[statuses.Count];
            for (int index = 0; index < statuses.Count; index++)
            {
                CombatStatusInstance status = statuses[index];
                parts[index] =
                    $"{status.Definition.StatusId}:{status.RemainingDuration}";
            }

            return string.Join("|", parts);
        }

        private static void CreateTimelineCells(
            VisualElement container,
            ICollection<VisualElement> cells)
        {
            container.Clear();
            cells.Clear();
            for (int index = 0; index < TimelineCellCount; index++)
            {
                var cell = new FormalTimelineCell();
                if ((index + 1) % 5 == 0)
                {
                    cell.AddToClassList("combat-hud-timeline-major-cell");
                    cell.tooltip = $"T{index + 1}";
                }

                container.Add(cell);
                cells.Add(cell);
            }
        }

        private static int PageStartFor(int timeStep)
        {
            return ((Mathf.Max(1, timeStep) - 1) / TimelineCellCount)
                * TimelineCellCount + 1;
        }

        private static void RenderTimeline(
            InitialTimelineSchedule schedule,
            TimelineActorSide side,
            IReadOnlyList<VisualElement> cells,
            int pageStart)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                var cell = (FormalTimelineCell)cells[index];
                cell.SetTimeStep(pageStart + index);
                cell.ResetPresentation();
            }

            if (schedule == null)
            {
                return;
            }

            int pageEnd = pageStart + TimelineCellCount - 1;
            for (int actionIndex = 0;
                actionIndex < schedule.Actions.Count;
                actionIndex++)
            {
                ScheduledTimelineAction action = schedule.Actions[actionIndex];
                if (action.Side != side)
                {
                    continue;
                }

                int occupiedStart = Mathf.Max(pageStart, action.StartTime + 1);
                int occupiedEnd = Mathf.Min(pageEnd, action.EndTime);
                for (int timeStep = occupiedStart;
                    timeStep <= occupiedEnd;
                    timeStep++)
                {
                    GetTimelineCell(cells, pageStart, timeStep).SetActionOccupied();
                }

                for (int eventIndex = 0;
                    eventIndex < action.ResolutionEvents.Count;
                    eventIndex++)
                {
                    ScheduledResolutionEvent resolution =
                        action.ResolutionEvents[eventIndex];
                    int? windowStart = resolution.ResolutionWindowStartTime;
                    int? windowEnd = resolution.ResolutionWindowEndTime;
                    if (!windowStart.HasValue
                        && eventIndex == 0
                        && action.HasResolutionWindow)
                    {
                        windowStart = action.ResolutionWindowStartTime;
                        windowEnd = action.ResolutionWindowEndTime;
                    }

                    if (windowStart.HasValue && windowEnd.HasValue)
                    {
                        int visibleStart = Mathf.Max(pageStart, windowStart.Value);
                        int visibleEnd = Mathf.Min(pageEnd, windowEnd.Value);
                        for (int timeStep = visibleStart;
                            timeStep <= visibleEnd;
                            timeStep++)
                        {
                            GetTimelineCell(cells, pageStart, timeStep)
                                .SetResolutionWindow(resolution.Effect);
                        }
                    }
                    else if (resolution.Time >= pageStart
                        && resolution.Time <= pageEnd)
                    {
                        GetTimelineCell(cells, pageStart, resolution.Time)
                            .SetResolutionPoint(resolution.Effect);
                    }
                }
            }
        }

        private static FormalTimelineCell GetTimelineCell(
            IReadOnlyList<VisualElement> cells,
            int pageStart,
            int timeStep)
        {
            return (FormalTimelineCell)cells[timeStep - pageStart];
        }

        private sealed class FormalTimelineCell : VisualElement
        {
            private readonly Label _marker;
            private Color _stripeColor;
            private bool _hasResolutionWindow;

            public FormalTimelineCell()
            {
                AddToClassList("combat-hud-timeline-cell");
                _marker = new Label();
                _marker.AddToClassList("combat-hud-timeline-marker");
                Add(_marker);
                generateVisualContent += DrawResolutionWindow;
            }

            public void SetTimeStep(int timeStep)
            {
                tooltip = timeStep % 5 == 0 ? $"T{timeStep}" : string.Empty;
            }

            public void ResetPresentation()
            {
                EnableInClassList("combat-hud-timeline-action", false);
                EnableInClassList("combat-hud-timeline-damage", false);
                EnableInClassList("combat-hud-timeline-guard", false);
                EnableInClassList("combat-hud-timeline-evade", false);
                EnableInClassList("combat-hud-timeline-status", false);
                EnableInClassList("combat-hud-timeline-heal", false);
                EnableInClassList("combat-hud-timeline-stamina", false);
                EnableInClassList("combat-hud-timeline-current", false);
                _marker.text = string.Empty;
                _hasResolutionWindow = false;
                MarkDirtyRepaint();
            }

            public void SetActionOccupied()
            {
                EnableInClassList("combat-hud-timeline-action", true);
            }

            public void SetResolutionPoint(ActionResolutionEffect effect)
            {
                AddEffectClass(effect);
                AddMarker(effect);
            }

            public void SetResolutionWindow(ActionResolutionEffect effect)
            {
                EnableInClassList("combat-hud-timeline-action", true);
                _stripeColor = EffectColor(effect);
                _hasResolutionWindow = true;
                AddMarker(effect);
                MarkDirtyRepaint();
            }

            private void AddEffectClass(ActionResolutionEffect effect)
            {
                string className = EffectClass(effect);
                if (!string.IsNullOrEmpty(className))
                {
                    EnableInClassList(className, true);
                }
            }

            private void AddMarker(ActionResolutionEffect effect)
            {
                string marker = EffectMarker(effect);
                if (string.IsNullOrEmpty(marker)
                    || _marker.text.Contains(marker))
                {
                    return;
                }

                _marker.text += marker;
            }

            private void DrawResolutionWindow(MeshGenerationContext context)
            {
                if (!_hasResolutionWindow
                    || contentRect.width < 1f
                    || contentRect.height < 1f)
                {
                    return;
                }

                Painter2D painter = context.painter2D;
                painter.strokeColor = _stripeColor;
                painter.lineWidth = 2f;
                float height = contentRect.height;
                float width = contentRect.width;
                for (float bottomX = -height; bottomX < width; bottomX += 8f)
                {
                    Vector2 start = bottomX < 0f
                        ? new Vector2(0f, height + bottomX)
                        : new Vector2(bottomX, height);
                    float topX = bottomX + height;
                    Vector2 end = topX > width
                        ? new Vector2(width, height - width + bottomX)
                        : new Vector2(topX, 0f);
                    painter.BeginPath();
                    painter.MoveTo(start);
                    painter.LineTo(end);
                    painter.Stroke();
                }
            }

            private static string EffectClass(ActionResolutionEffect effect)
            {
                switch (effect)
                {
                    case ActionResolutionEffect.Damage:
                        return "combat-hud-timeline-damage";
                    case ActionResolutionEffect.Guard:
                        return "combat-hud-timeline-guard";
                    case ActionResolutionEffect.Evade:
                        return "combat-hud-timeline-evade";
                    case ActionResolutionEffect.ApplyBuff:
                    case ActionResolutionEffect.ApplyDebuff:
                        return "combat-hud-timeline-status";
                    case ActionResolutionEffect.Heal:
                        return "combat-hud-timeline-heal";
                    case ActionResolutionEffect.RestoreStamina:
                        return "combat-hud-timeline-stamina";
                    default:
                        return string.Empty;
                }
            }

            private static string EffectMarker(ActionResolutionEffect effect)
            {
                switch (effect)
                {
                    case ActionResolutionEffect.Damage: return "D";
                    case ActionResolutionEffect.Guard: return "G";
                    case ActionResolutionEffect.Evade: return "E";
                    case ActionResolutionEffect.ApplyBuff:
                    case ActionResolutionEffect.ApplyDebuff: return "B";
                    case ActionResolutionEffect.Heal: return "H";
                    case ActionResolutionEffect.RestoreStamina: return "S";
                    default: return string.Empty;
                }
            }

            private static Color EffectColor(ActionResolutionEffect effect)
            {
                switch (effect)
                {
                    case ActionResolutionEffect.Damage:
                        return new Color(0.90f, 0.30f, 0.25f, 1f);
                    case ActionResolutionEffect.Guard:
                        return new Color(0.32f, 0.58f, 0.95f, 1f);
                    case ActionResolutionEffect.Evade:
                        return new Color(0.30f, 0.80f, 0.88f, 1f);
                    case ActionResolutionEffect.ApplyBuff:
                    case ActionResolutionEffect.ApplyDebuff:
                        return new Color(0.70f, 0.42f, 0.88f, 1f);
                    case ActionResolutionEffect.Heal:
                        return new Color(0.34f, 0.82f, 0.45f, 1f);
                    case ActionResolutionEffect.RestoreStamina:
                        return new Color(0.95f, 0.74f, 0.22f, 1f);
                    default:
                        return new Color(0.75f, 0.72f, 0.64f, 1f);
                }
            }
        }
    }
}
