using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Damage;
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
        private readonly List<Label> _qteOptionLabels = new List<Label>();

        private VisualElement _root;
        private VisualElement _playerStatusPanel;
        private VisualElement _enemyStatusPanel;
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
        private Button _enemyIntentDetailsButton;
        private VisualElement _comboList;
        private Button _clearPreviewButton;
        private Label _reconfigurationChargesLabel;
        private Button _openReconfigurationButton;
        private VisualElement _reconfigurationOverlay;
        private Label _reconfigurationSummary;
        private VisualElement _reconfigurationComboList;
        private VisualElement _reconfigurationSlotList;
        private VisualElement _reconfigurationActionList;
        private Label _reconfigurationSelection;
        private Label _reconfigurationError;
        private Button _applyReconfigurationButton;
        private Button _closeReconfigurationButton;
        private VisualElement _statusDetailsOverlay;
        private Label _statusDetailsTitle;
        private Label _statusDetailsSummary;
        private ScrollView _statusDetailsScrollView;
        private VisualElement _statusDetailsList;
        private Label _statusDetailsEmpty;
        private Button _statusDetailsCloseButton;
        private VisualElement _enemyIntentDetailsOverlay;
        private Label _enemyIntentDetailsTitle;
        private Label _enemyIntentDetailsSummary;
        private ScrollView _enemyIntentDetailsScrollView;
        private VisualElement _enemyIntentDetailsList;
        private Label _enemyIntentDetailsEmpty;
        private Button _enemyIntentDetailsCloseButton;
        private VisualElement _qteOverlay;
        private Label _qteType;
        private Label _qteActionName;
        private Label _qtePrompt;
        private VisualElement _qteOptions;
        private ProgressBar _qteTimeRemaining;
        private Button _qteActionButton;
        private Label _qteInputHint;
        private CombatSandboxController _sandbox;
        private CombatSandboxController _builtForSandbox;
        private bool _isVisible;
        private int _lastTimelineMarker = -1;
        private int _timelinePageStart = 1;
        private int _renderedTimelinePageStart = -1;
        private InitialTimelineSchedule _renderedTimelineSchedule;
        private string _lastPlayerStatusSignature;
        private string _lastEnemyStatusSignature;
        private string _statusDetailsSignature;
        private TimelineActorSide _statusDetailsSide;
        private bool _isStatusDetailsOpen;
        private bool _isEnemyIntentDetailsOpen;
        private string _qteLayoutSignature;
        private bool _isQteVisible;
        private bool _isReconfigurationOpen;
        private int _reconfigurationComboIndex;
        private BattleComboActionSlot _reconfigurationSlot = BattleComboActionSlot.Main;
        private string _reconfigurationActionId;

        private void Awake()
        {
            VisualElement documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _root = documentRoot.Q<VisualElement>("combatHudRoot");
            _playerStatusPanel = documentRoot.Q<VisualElement>("playerStatusPanel");
            _enemyStatusPanel = documentRoot.Q<VisualElement>("enemyStatusPanel");
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
            _enemyIntentDetailsButton =
                documentRoot.Q<Button>("enemyIntentDetailsButton");
            _comboList = documentRoot.Q<VisualElement>("comboList");
            _clearPreviewButton = documentRoot.Q<Button>("clearPreviewButton");
            _reconfigurationChargesLabel =
                documentRoot.Q<Label>("reconfigurationChargesLabel");
            _openReconfigurationButton =
                documentRoot.Q<Button>("openReconfigurationButton");
            _reconfigurationOverlay =
                documentRoot.Q<VisualElement>("reconfigurationOverlay");
            _reconfigurationSummary =
                documentRoot.Q<Label>("reconfigurationSummary");
            _reconfigurationComboList =
                documentRoot.Q<VisualElement>("reconfigurationComboList");
            _reconfigurationSlotList =
                documentRoot.Q<VisualElement>("reconfigurationSlotList");
            _reconfigurationActionList =
                documentRoot.Q<VisualElement>("reconfigurationActionList");
            _reconfigurationSelection =
                documentRoot.Q<Label>("reconfigurationSelection");
            _reconfigurationError =
                documentRoot.Q<Label>("reconfigurationError");
            _applyReconfigurationButton =
                documentRoot.Q<Button>("applyReconfigurationButton");
            _closeReconfigurationButton =
                documentRoot.Q<Button>("closeReconfigurationButton");
            _statusDetailsOverlay =
                documentRoot.Q<VisualElement>("statusDetailsOverlay");
            _statusDetailsTitle = documentRoot.Q<Label>("statusDetailsTitle");
            _statusDetailsSummary = documentRoot.Q<Label>("statusDetailsSummary");
            _statusDetailsScrollView =
                documentRoot.Q<ScrollView>("statusDetailsScrollView");
            _statusDetailsList =
                documentRoot.Q<VisualElement>("statusDetailsList");
            _statusDetailsEmpty = documentRoot.Q<Label>("statusDetailsEmpty");
            _statusDetailsCloseButton =
                documentRoot.Q<Button>("statusDetailsCloseButton");
            _enemyIntentDetailsOverlay =
                documentRoot.Q<VisualElement>("enemyIntentDetailsOverlay");
            _enemyIntentDetailsTitle =
                documentRoot.Q<Label>("enemyIntentDetailsTitle");
            _enemyIntentDetailsSummary =
                documentRoot.Q<Label>("enemyIntentDetailsSummary");
            _enemyIntentDetailsScrollView =
                documentRoot.Q<ScrollView>("enemyIntentDetailsScrollView");
            _enemyIntentDetailsList =
                documentRoot.Q<VisualElement>("enemyIntentDetailsList");
            _enemyIntentDetailsEmpty =
                documentRoot.Q<Label>("enemyIntentDetailsEmpty");
            _enemyIntentDetailsCloseButton =
                documentRoot.Q<Button>("enemyIntentDetailsCloseButton");
            _qteOverlay = documentRoot.Q<VisualElement>("qteOverlay");
            _qteType = documentRoot.Q<Label>("qteType");
            _qteActionName = documentRoot.Q<Label>("qteActionName");
            _qtePrompt = documentRoot.Q<Label>("qtePrompt");
            _qteOptions = documentRoot.Q<VisualElement>("qteOptions");
            _qteTimeRemaining = documentRoot.Q<ProgressBar>("qteTimeRemaining");
            _qteActionButton = documentRoot.Q<Button>("qteActionButton");
            _qteInputHint = documentRoot.Q<Label>("qteInputHint");
            _clearPreviewButton.clicked += ClearPreview;
            _openReconfigurationButton.clicked += OpenReconfiguration;
            _applyReconfigurationButton.clicked += ApplyReconfiguration;
            _closeReconfigurationButton.clicked += CloseReconfiguration;
            _previousTimelinePageButton.clicked += ShowPreviousTimelinePage;
            _nextTimelinePageButton.clicked += ShowNextTimelinePage;
            _enemyIntentDetailsButton.clicked += OpenEnemyIntentDetails;
            _statusDetailsCloseButton.clicked += CloseStatusDetails;
            _enemyIntentDetailsCloseButton.clicked += CloseEnemyIntentDetails;
            _qteActionButton.clicked += OnQteActionClicked;
            _qteActionButton.RegisterCallback<PointerDownEvent>(OnQtePointerDown);
            _qteActionButton.RegisterCallback<PointerUpEvent>(OnQtePointerUp);
            _playerStatusPanel.RegisterCallback<ClickEvent>(OnPlayerStatusClicked);
            _enemyStatusPanel.RegisterCallback<ClickEvent>(OnEnemyStatusClicked);
            _playerStatusPanel.focusable = true;
            _enemyStatusPanel.focusable = true;
            _playerStatusPanel.tooltip = "Player status details (Q / LB)";
            _enemyStatusPanel.tooltip = "Enemy status details (E / RB)";

            CreateTimelineCells(
                documentRoot.Q<VisualElement>("playerTimelineCells"),
                _playerTimelineCells);
            CreateTimelineCells(
                documentRoot.Q<VisualElement>("enemyTimelineCells"),
                _enemyTimelineCells);
            _root.style.display = DisplayStyle.None;
            _statusDetailsOverlay.style.display = DisplayStyle.None;
            _enemyIntentDetailsOverlay.style.display = DisplayStyle.None;
            _qteOverlay.style.display = DisplayStyle.None;
            _reconfigurationOverlay.style.display = DisplayStyle.None;
            _isVisible = false;
        }

        private void OnDestroy()
        {
            if (_clearPreviewButton != null)
            {
                _clearPreviewButton.clicked -= ClearPreview;
            }

            if (_openReconfigurationButton != null)
            {
                _openReconfigurationButton.clicked -= OpenReconfiguration;
            }

            if (_applyReconfigurationButton != null)
            {
                _applyReconfigurationButton.clicked -= ApplyReconfiguration;
            }

            if (_closeReconfigurationButton != null)
            {
                _closeReconfigurationButton.clicked -= CloseReconfiguration;
            }

            if (_previousTimelinePageButton != null)
            {
                _previousTimelinePageButton.clicked -= ShowPreviousTimelinePage;
            }

            if (_nextTimelinePageButton != null)
            {
                _nextTimelinePageButton.clicked -= ShowNextTimelinePage;
            }

            if (_enemyIntentDetailsButton != null)
            {
                _enemyIntentDetailsButton.clicked -= OpenEnemyIntentDetails;
            }

            if (_statusDetailsCloseButton != null)
            {
                _statusDetailsCloseButton.clicked -= CloseStatusDetails;
            }

            if (_enemyIntentDetailsCloseButton != null)
            {
                _enemyIntentDetailsCloseButton.clicked -= CloseEnemyIntentDetails;
            }

            if (_qteActionButton != null)
            {
                _qteActionButton.clicked -= OnQteActionClicked;
                _qteActionButton.UnregisterCallback<PointerDownEvent>(OnQtePointerDown);
                _qteActionButton.UnregisterCallback<PointerUpEvent>(OnQtePointerUp);
            }

            _playerStatusPanel?.UnregisterCallback<ClickEvent>(OnPlayerStatusClicked);
            _enemyStatusPanel?.UnregisterCallback<ClickEvent>(OnEnemyStatusClicked);
        }

        private void Update()
        {
            if (_sandbox == null)
            {
                _sandbox = UnityEngine.Object.FindAnyObjectByType<CombatSandboxController>();
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

            RefreshPresentation();
            if (RefreshQtePresentation())
            {
                CloseReconfiguration();
                CloseEnemyIntentDetails();
                HandleQteInput();
                return;
            }

            if (_isReconfigurationOpen)
            {
                if (!_sandbox.CanReconfigureFormalCombo)
                {
                    CloseReconfiguration();
                }
                else
                {
                    HandleReconfigurationInput();
                }

                return;
            }

            if (_isEnemyIntentDetailsOpen)
            {
                HandleEnemyIntentDetailsInput();
                return;
            }

            if (_isStatusDetailsOpen)
            {
                HandleStatusDetailsInput();
                RefreshStatusDetails();
                return;
            }

            HandleStatusDetailsShortcuts();
            HandleEnemyIntentDetailsShortcut();
            HandleComboShortcuts();
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
            {
                CloseStatusDetails();
                CloseEnemyIntentDetails();
                CloseReconfiguration();
                HideQte();
            }
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
            _enemyIntentDetailsButton.SetEnabled(!_sandbox.IsTimelinePlaying);
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

            int reconfigurationCharges =
                battle.ComboReconfigurationChargesRemaining;
            _reconfigurationChargesLabel.text =
                $"CONFIGURE ×{reconfigurationCharges}";
            _openReconfigurationButton.SetEnabled(
                _sandbox.CanReconfigureFormalCombo);

            RefreshComboCards(battle);
            RefreshTimelineState(battle);
        }

        private void OpenReconfiguration()
        {
            if (_sandbox == null || !_sandbox.CanReconfigureFormalCombo)
            {
                return;
            }

            CloseStatusDetails();
            CloseEnemyIntentDetails();
            _sandbox.ClearFormalComboPreview();
            _isReconfigurationOpen = true;
            _reconfigurationComboIndex = 0;
            _reconfigurationSlot = BattleComboActionSlot.Main;
            _reconfigurationActionId = null;
            _reconfigurationError.text = string.Empty;
            _reconfigurationOverlay.style.display = DisplayStyle.Flex;
            RebuildReconfigurationDialog();
        }

        private void CloseReconfiguration()
        {
            if (!_isReconfigurationOpen)
            {
                return;
            }

            _isReconfigurationOpen = false;
            _reconfigurationActionId = null;
            _reconfigurationOverlay.style.display = DisplayStyle.None;
            _openReconfigurationButton?.Focus();
        }

        private void RebuildReconfigurationDialog()
        {
            SoloComboBattleState battle = _sandbox?.CurrentBattle;
            if (battle == null || battle.Combos.Count == 0)
            {
                CloseReconfiguration();
                return;
            }

            _reconfigurationComboIndex = Mathf.Clamp(
                _reconfigurationComboIndex,
                0,
                battle.Combos.Count - 1);
            _reconfigurationSummary.text =
                $"Remaining changes: {battle.ComboReconfigurationChargesRemaining}. "
                + "Only one existing Action is replaced per change.";
            RebuildReconfigurationComboChoices(battle);
            RebuildReconfigurationSlotChoices(battle.Combos[_reconfigurationComboIndex]);
            RebuildReconfigurationActionChoices(battle.Combos[_reconfigurationComboIndex]);
            _applyReconfigurationButton.SetEnabled(
                !string.IsNullOrWhiteSpace(_reconfigurationActionId));
        }

        private void RebuildReconfigurationComboChoices(SoloComboBattleState battle)
        {
            _reconfigurationComboList.Clear();
            for (int index = 0; index < battle.Combos.Count; index++)
            {
                int selectedIndex = index;
                var button = new Button(() =>
                {
                    _reconfigurationComboIndex = selectedIndex;
                    _reconfigurationSlot = BattleComboActionSlot.Main;
                    _reconfigurationActionId = null;
                    _reconfigurationError.text = string.Empty;
                    RebuildReconfigurationDialog();
                })
                {
                    text = $"{index + 1}  {battle.Combos[index].Name}"
                };
                button.AddToClassList("combat-hud-reconfigure-choice");
                button.EnableInClassList(
                    "combat-hud-reconfigure-choice-selected",
                    index == _reconfigurationComboIndex);
                _reconfigurationComboList.Add(button);
            }
        }

        private void RebuildReconfigurationSlotChoices(ComboConfiguration combo)
        {
            _reconfigurationSlotList.Clear();
            BattleComboActionSlot[] slots =
            {
                BattleComboActionSlot.Opening,
                BattleComboActionSlot.Main,
                BattleComboActionSlot.Closing,
                BattleComboActionSlot.Linked
            };
            bool selectedSlotExists = GetComboAction(combo, _reconfigurationSlot) != null;
            if (!selectedSlotExists)
            {
                for (int index = 0; index < slots.Length; index++)
                {
                    if (GetComboAction(combo, slots[index]) != null)
                    {
                        _reconfigurationSlot = slots[index];
                        break;
                    }
                }
            }

            for (int index = 0; index < slots.Length; index++)
            {
                BattleComboActionSlot slot = slots[index];
                ComboActionDefinition action = GetComboAction(combo, slot);
                if (action == null)
                {
                    continue;
                }

                var button = new Button(() =>
                {
                    _reconfigurationSlot = slot;
                    _reconfigurationActionId = null;
                    _reconfigurationError.text = string.Empty;
                    RebuildReconfigurationDialog();
                })
                {
                    text = $"{slot}: {action.DisplayName}"
                };
                button.AddToClassList("combat-hud-reconfigure-choice");
                button.EnableInClassList(
                    "combat-hud-reconfigure-choice-selected",
                    slot == _reconfigurationSlot);
                _reconfigurationSlotList.Add(button);
            }
        }

        private void RebuildReconfigurationActionChoices(ComboConfiguration combo)
        {
            _reconfigurationActionList.Clear();
            ComboActionDefinition currentAction = GetComboAction(
                combo,
                _reconfigurationSlot);
            if (!_sandbox.TryGetFormalReconfigurationCandidates(
                    _reconfigurationSlot,
                    out IReadOnlyList<ComboActionDefinition> candidates,
                    out string validationError))
            {
                _reconfigurationError.text = validationError;
                return;
            }

            int availableCount = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                ComboActionDefinition candidate = candidates[index];
                if (currentAction != null
                    && string.Equals(
                        currentAction.ActionId,
                        candidate.ActionId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                availableCount++;
                string actionId = candidate.ActionId;
                var button = new Button(() =>
                {
                    _reconfigurationActionId = actionId;
                    _reconfigurationSelection.text =
                        $"Selected: {candidate.DisplayName}";
                    _reconfigurationError.text = string.Empty;
                    _applyReconfigurationButton.SetEnabled(true);
                })
                {
                    text = $"{candidate.DisplayName}\n"
                        + $"TIME {candidate.TimelineCost}   STA {candidate.StaminaCost}   "
                        + $"MANA {candidate.ManaCost}   CD {candidate.Cooldown}"
                };
                button.AddToClassList("combat-hud-reconfigure-action");
                _reconfigurationActionList.Add(button);
            }

            _reconfigurationSelection.text = availableCount > 0
                ? "No replacement selected."
                : "No other learned Action is available for this slot.";
        }

        private void ApplyReconfiguration()
        {
            if (string.IsNullOrWhiteSpace(_reconfigurationActionId))
            {
                return;
            }

            if (!_sandbox.TryReconfigureFormalCombo(
                    _reconfigurationComboIndex,
                    _reconfigurationSlot,
                    _reconfigurationActionId,
                    out string validationError))
            {
                _reconfigurationError.text = validationError;
                return;
            }

            CloseReconfiguration();
            RefreshPresentation();
        }

        private void HandleReconfigurationInput()
        {
            bool cancelPressed = (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null
                    && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (cancelPressed)
            {
                CloseReconfiguration();
            }
        }

        private static ComboActionDefinition GetComboAction(
            ComboConfiguration combo,
            BattleComboActionSlot slot)
        {
            switch (slot)
            {
                case BattleComboActionSlot.Opening:
                    return combo.OpeningAction;
                case BattleComboActionSlot.Main:
                    return combo.MainAction;
                case BattleComboActionSlot.Closing:
                    return combo.ClosingAction;
                case BattleComboActionSlot.Linked:
                    return combo.LinkedAction;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot));
            }
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

        private bool RefreshQtePresentation()
        {
            bool hasTiming = _sandbox != null && _sandbox.HasFormalTimingQte;
            bool hasCharge = _sandbox != null && _sandbox.HasFormalChargeQte;
            if (!hasTiming && !hasCharge)
            {
                HideQte();
                return false;
            }

            if (!_isQteVisible)
            {
                _isQteVisible = true;
                _qteOverlay.style.display = DisplayStyle.Flex;
                CloseStatusDetails();
            }

            string signature = hasTiming
                ? $"timing:{_sandbox.FormalQteDisplayName}:"
                    + $"{_sandbox.FormalTimingMinimumTimeStep}:"
                    + $"{_sandbox.FormalTimingMaximumTimeStep}"
                : $"charge:{_sandbox.FormalQteDisplayName}:"
                    + $"{_sandbox.FormalChargeMaximumLevel}";
            if (signature != _qteLayoutSignature)
            {
                BuildQteOptions(hasTiming);
                _qteLayoutSignature = signature;
            }

            int currentOption = hasTiming
                ? _sandbox.FormalTimingCandidate
                    - _sandbox.FormalTimingMinimumTimeStep
                : _sandbox.FormalChargeCandidateLevel;
            for (int index = 0; index < _qteOptionLabels.Count; index++)
            {
                bool chargeLevelReached = hasCharge
                    && _sandbox.IsFormalChargeEngaged
                    && index < currentOption;
                _qteOptionLabels[index].EnableInClassList(
                    "combat-hud-qte-option-reached",
                    chargeLevelReached);
                _qteOptionLabels[index].EnableInClassList(
                    "combat-hud-qte-option-current",
                    index == currentOption);
            }

            _qteActionName.text = _sandbox.FormalQteDisplayName;
            _qteTimeRemaining.lowValue = 0f;
            _qteTimeRemaining.highValue = 1f;
            _qteTimeRemaining.value = _sandbox.FormalQteNormalizedTimeRemaining;
            _qteTimeRemaining.title =
                $"{_sandbox.FormalQteSecondsRemaining:0.0}s remaining";

            if (hasTiming)
            {
                int candidate = _sandbox.FormalTimingCandidate;
                _qteType.text = "RESOLUTION TIMING";
                if (!_sandbox.IsFormalTimingReady)
                {
                    _qtePrompt.text =
                        "Get ready. The timing marker will move shortly.";
                    _qteActionButton.text = "GET READY";
                    _qteActionButton.SetEnabled(false);
                    _qteInputHint.text =
                        $"Input unlocks in "
                        + $"{_sandbox.FormalTimingPreparationSecondsRemaining:0.0}s";
                }
                else
                {
                    _qtePrompt.text =
                        $"The timing marker is moving. Lock this effect to T{candidate}.";
                    _qteActionButton.text = $"LOCK T{candidate}";
                    _qteActionButton.SetEnabled(true);
                    _qteInputHint.text =
                        "F / Space / Enter / A / Left Click: Lock timing";
                }
            }
            else if (_sandbox.IsFormalChargeEngaged)
            {
                int level = _sandbox.FormalChargeCandidateLevel;
                _qteTimeRemaining.title =
                    $"Hold limit: {_sandbox.FormalQteSecondsRemaining:0.0}s";
                _qteActionButton.SetEnabled(true);
                _qteType.text = "CHARGE";
                _qtePrompt.text =
                    $"Charging at Level {level}. Release on the level you want.";
                _qteActionButton.text = $"RELEASE AT LEVEL {level}";
                _qteInputHint.text =
                    "Release F / Space / A / Left Mouse: Confirm charge level";
            }
            else
            {
                _qteTimeRemaining.title =
                    $"Start within: {_sandbox.FormalQteSecondsRemaining:0.0}s";
                _qteActionButton.SetEnabled(true);
                _qteType.text = "CHARGE";
                _qtePrompt.text =
                    "Begin charging, then release on the level you want.";
                _qteActionButton.text = "HOLD TO CHARGE";
                _qteInputHint.text =
                    "Hold F / Space / A / Left Mouse: Begin charging";
            }

            return true;
        }

        private void BuildQteOptions(bool timing)
        {
            _qteOptions.Clear();
            _qteOptionLabels.Clear();
            int first = timing ? _sandbox.FormalTimingMinimumTimeStep : 0;
            int last = timing
                ? _sandbox.FormalTimingMaximumTimeStep
                : _sandbox.FormalChargeMaximumLevel;
            for (int value = first; value <= last; value++)
            {
                var label = new Label(timing ? $"T{value}" : $"LEVEL {value}");
                label.AddToClassList("combat-hud-qte-option");
                _qteOptionLabels.Add(label);
                _qteOptions.Add(label);
            }
        }

        private void HideQte()
        {
            if (!_isQteVisible)
            {
                return;
            }

            _isQteVisible = false;
            _qteLayoutSignature = null;
            _qteOptionLabels.Clear();
            if (_qteOptions != null)
            {
                _qteOptions.Clear();
            }

            if (_qteOverlay != null)
            {
                _qteOverlay.style.display = DisplayStyle.None;
            }
        }

        private void HandleQteInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool timingPressed = (keyboard != null
                    && (keyboard.fKey.wasPressedThisFrame
                        || keyboard.spaceKey.wasPressedThisFrame
                        || keyboard.enterKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            bool chargePressed = (keyboard != null
                    && (keyboard.fKey.wasPressedThisFrame
                        || keyboard.spaceKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            bool released = (keyboard != null
                    && (keyboard.fKey.wasReleasedThisFrame
                        || keyboard.spaceKey.wasReleasedThisFrame))
                || (gamepad != null && gamepad.buttonSouth.wasReleasedThisFrame);

            if (_sandbox.HasFormalTimingQte)
            {
                if (timingPressed)
                {
                    _sandbox.TrySelectFormalTimingCandidate();
                }

                return;
            }

            if (!_sandbox.HasFormalChargeQte)
            {
                return;
            }

            if (!_sandbox.IsFormalChargeEngaged && chargePressed)
            {
                _sandbox.TryBeginFormalCharge();
            }
            else if (_sandbox.IsFormalChargeEngaged && released)
            {
                _sandbox.TryReleaseFormalCharge();
            }
        }

        private void OnQteActionClicked()
        {
            if (_sandbox == null)
            {
                return;
            }

            if (_sandbox.HasFormalTimingQte)
            {
                _sandbox.TrySelectFormalTimingCandidate();
            }

            // Charge uses pointer down/up so a mouse press behaves like a real
            // hold. Keyboard and gamepad charge input is handled in Update.
            // Handling Button.clicked here as well would run after PointerUp
            // and could immediately start a second charge.
        }

        private void OnQtePointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0
                && _sandbox != null
                && _sandbox.HasFormalChargeQte
                && !_sandbox.IsFormalChargeEngaged)
            {
                _sandbox.TryBeginFormalCharge();
            }
        }

        private void OnQtePointerUp(PointerUpEvent evt)
        {
            if (evt.button == 0
                && _sandbox != null
                && _sandbox.HasFormalChargeQte
                && _sandbox.IsFormalChargeEngaged)
            {
                _sandbox.TryReleaseFormalCharge();
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

        private void HandleStatusDetailsShortcuts()
        {
            if (_sandbox.IsTimelinePlaying)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    OpenStatusDetails(TimelineActorSide.Player);
                    return;
                }

                if (keyboard.eKey.wasPressedThisFrame)
                {
                    OpenStatusDetails(TimelineActorSide.Enemy);
                    return;
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return;
            }

            if (gamepad.leftShoulder.wasPressedThisFrame)
            {
                OpenStatusDetails(TimelineActorSide.Player);
            }
            else if (gamepad.rightShoulder.wasPressedThisFrame)
            {
                OpenStatusDetails(TimelineActorSide.Enemy);
            }
        }

        private void HandleEnemyIntentDetailsShortcut()
        {
            if (_sandbox.IsTimelinePlaying || _isStatusDetailsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if ((keyboard != null && keyboard.iKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.rightStickButton.wasPressedThisFrame))
            {
                OpenEnemyIntentDetails();
            }
        }

        private void HandleEnemyIntentDetailsInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (mouse != null && mouse.rightButton.wasPressedThisFrame)
                || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
            {
                CloseEnemyIntentDetails();
                return;
            }

            float scrollDirection = 0f;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    scrollDirection -= 1f;
                }
                else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    scrollDirection += 1f;
                }
            }

            if (gamepad != null)
            {
                scrollDirection -= gamepad.rightStick.y.ReadValue();
            }

            if (Mathf.Abs(scrollDirection) < 0.1f)
            {
                return;
            }

            Vector2 offset = _enemyIntentDetailsScrollView.scrollOffset;
            offset.y = Mathf.Max(
                0f,
                offset.y + scrollDirection * 480f * Time.unscaledDeltaTime);
            _enemyIntentDetailsScrollView.scrollOffset = offset;
        }

        private void OpenEnemyIntentDetails()
        {
            SoloComboBattleState battle = _sandbox?.CurrentBattle;
            if (battle == null || _sandbox.IsTimelinePlaying)
            {
                return;
            }

            CloseStatusDetails();
            _isEnemyIntentDetailsOpen = true;
            _enemyIntentDetailsOverlay.style.display = DisplayStyle.Flex;
            _enemyIntentDetailsScrollView.scrollOffset = Vector2.zero;
            RebuildEnemyIntentDetails(battle);
            _enemyIntentDetailsCloseButton.Focus();
        }

        private void CloseEnemyIntentDetails()
        {
            _isEnemyIntentDetailsOpen = false;
            if (_enemyIntentDetailsOverlay != null)
            {
                _enemyIntentDetailsOverlay.style.display = DisplayStyle.None;
            }
        }

        private void RebuildEnemyIntentDetails(SoloComboBattleState battle)
        {
            InitialTimelineSchedule intent = battle.EnemyIntent;
            _enemyIntentDetailsTitle.text =
                $"{battle.Enemy.DisplayName} — Enemy Intent";
            _enemyIntentDetailsList.Clear();

            int actionCount = intent?.Actions.Count ?? 0;
            string sequenceName = battle.EnemySequence?.DisplayName ?? "Locked Sequence";
            _enemyIntentDetailsSummary.text = actionCount == 1
                ? $"{sequenceName}  •  1 locked Action"
                : $"{sequenceName}  •  {actionCount} locked Actions";
            _enemyIntentDetailsEmpty.style.display = actionCount == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            for (int index = 0; index < actionCount; index++)
            {
                _enemyIntentDetailsList.Add(CreateEnemyIntentEntry(
                    intent.Actions[index],
                    index));
            }
        }

        private static VisualElement CreateEnemyIntentEntry(
            ScheduledTimelineAction action,
            int actionIndex)
        {
            var entry = new VisualElement();
            entry.AddToClassList("combat-hud-intent-entry");

            var header = new VisualElement();
            header.AddToClassList("combat-hud-intent-entry-header");
            var name = new Label($"{actionIndex + 1}. {action.DisplayName}");
            name.AddToClassList("combat-hud-intent-entry-name");
            var time = new Label(
                $"Global T{action.StartTime + 1}–T{action.EndTime}");
            time.AddToClassList("combat-hud-intent-entry-time");
            header.Add(name);
            header.Add(time);
            entry.Add(header);

            for (int index = 0; index < action.ResolutionEvents.Count; index++)
            {
                entry.Add(CreateEnemyIntentEvent(action, action.ResolutionEvents[index]));
            }

            for (int index = 0; index < action.ActiveWindows.Count; index++)
            {
                ScheduledActiveWindow window = action.ActiveWindows[index];
                var windowBlock = new VisualElement();
                windowBlock.AddToClassList("combat-hud-intent-event");
                var windowTitle = new Label(
                    $"T{window.StartTime}–T{window.EndTime} — Active Window");
                windowTitle.AddToClassList("combat-hud-intent-event-title");
                var windowDetail = new Label(FormatActiveWindow(window));
                windowDetail.AddToClassList("combat-hud-intent-event-detail");
                windowBlock.Add(windowTitle);
                windowBlock.Add(windowDetail);
                entry.Add(windowBlock);
            }

            return entry;
        }

        private static VisualElement CreateEnemyIntentEvent(
            ScheduledTimelineAction action,
            ScheduledResolutionEvent resolution)
        {
            var eventBlock = new VisualElement();
            eventBlock.AddToClassList("combat-hud-intent-event");
            int localTime = resolution.Time - action.StartTime;
            var title = new Label(
                $"T{resolution.Time} (Local T{localTime}) — "
                + FormatIntentEffectName(resolution.Effect));
            title.AddToClassList("combat-hud-intent-event-title");
            var detail = new Label(FormatIntentEventDetails(resolution));
            detail.AddToClassList("combat-hud-intent-event-detail");
            eventBlock.Add(title);
            eventBlock.Add(detail);
            return eventBlock;
        }

        private static string FormatIntentEffectName(ActionResolutionEffect effect)
        {
            switch (effect)
            {
                case ActionResolutionEffect.ApplyBuff:
                    return "Apply Buff";
                case ActionResolutionEffect.ApplyDebuff:
                    return "Apply Debuff";
                case ActionResolutionEffect.RestoreStamina:
                    return "Restore Stamina";
                default:
                    return effect.ToString();
            }
        }

        private static string FormatIntentEventDetails(
            ScheduledResolutionEvent resolution)
        {
            var details = new List<string>
            {
                $"Target: {FormatIntentTarget(resolution)}"
            };

            if (resolution.DamageEffect != null)
            {
                details.Add($"Damage: {FormatDamageTypes(resolution.DamageEffect)}");
            }

            if (resolution.DamageTags != DamageResolutionTag.None)
            {
                details.Add($"Tags: {resolution.DamageTags}");
            }

            if (resolution.StatusEffect != null)
            {
                details.Add($"Status: {resolution.StatusEffect.DisplayName}");
            }

            if (resolution.OnEvadedDisruption != null)
            {
                details.Add(
                    "If Evaded: self-"
                    + $"{resolution.OnEvadedDisruption.Type} for "
                    + $"{resolution.OnEvadedDisruption.Duration} Time Steps");
            }

            return string.Join("  •  ", details);
        }

        private static string FormatIntentTarget(ScheduledResolutionEvent resolution)
        {
            switch (resolution.Effect)
            {
                case ActionResolutionEffect.Damage:
                case ActionResolutionEffect.ApplyDebuff:
                    return resolution.StatusEffect != null
                        && resolution.StatusTarget == ActionStatusTarget.Self
                            ? "Enemy"
                            : "Player";
                case ActionResolutionEffect.ApplyBuff:
                    return resolution.StatusTarget == ActionStatusTarget.Opponent
                        ? "Player"
                        : "Enemy";
                default:
                    return "Enemy";
            }
        }

        private static string FormatDamageTypes(DamageEffectDefinition damage)
        {
            var types = new List<string>();
            for (int index = 0; index < damage.Components.Count; index++)
            {
                string type = damage.Components[index].Type.ToString();
                if (!types.Contains(type))
                {
                    types.Add(type);
                }
            }

            return string.Join(", ", types);
        }

        private static string FormatActiveWindow(ScheduledActiveWindow window)
        {
            if (window.Effect == ActiveWindowEffect.SuperArmor)
            {
                return $"{window.DisplayName}: Super Armor Lv.{window.SuperArmorLevel}";
            }

            return $"{window.DisplayName}: Damage taken -"
                + $"{window.DamageReduction * 100d:0.#}%";
        }

        private void HandleStatusDetailsInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (mouse != null && mouse.rightButton.wasPressedThisFrame)
                || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
            {
                CloseStatusDetails();
                return;
            }

            float scrollDirection = 0f;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    scrollDirection -= 1f;
                }
                else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    scrollDirection += 1f;
                }
            }

            if (gamepad != null)
            {
                scrollDirection -= gamepad.rightStick.y.ReadValue();
            }

            if (Mathf.Abs(scrollDirection) < 0.1f)
            {
                return;
            }

            Vector2 offset = _statusDetailsScrollView.scrollOffset;
            offset.y = Mathf.Max(
                0f,
                offset.y + scrollDirection * 480f * Time.unscaledDeltaTime);
            _statusDetailsScrollView.scrollOffset = offset;
        }

        private void OnPlayerStatusClicked(ClickEvent evt)
        {
            OpenStatusDetails(TimelineActorSide.Player);
            evt.StopPropagation();
        }

        private void OnEnemyStatusClicked(ClickEvent evt)
        {
            OpenStatusDetails(TimelineActorSide.Enemy);
            evt.StopPropagation();
        }

        private void OpenStatusDetails(TimelineActorSide side)
        {
            if (_sandbox == null
                || _sandbox.CurrentBattle == null
                || _sandbox.IsTimelinePlaying)
            {
                return;
            }

            CloseEnemyIntentDetails();
            _statusDetailsSide = side;
            _isStatusDetailsOpen = true;
            _statusDetailsSignature = null;
            _statusDetailsOverlay.style.display = DisplayStyle.Flex;
            _statusDetailsScrollView.scrollOffset = Vector2.zero;
            RefreshStatusDetails();
            _statusDetailsCloseButton.Focus();
        }

        private void CloseStatusDetails()
        {
            if (!_isStatusDetailsOpen && _statusDetailsOverlay == null)
            {
                return;
            }

            _isStatusDetailsOpen = false;
            _statusDetailsSignature = null;
            if (_statusDetailsOverlay != null)
            {
                _statusDetailsOverlay.style.display = DisplayStyle.None;
            }
        }

        private void RefreshStatusDetails()
        {
            SoloComboBattleState battle = _sandbox?.CurrentBattle;
            if (!_isStatusDetailsOpen || battle == null)
            {
                return;
            }

            CombatantState combatant = _statusDetailsSide == TimelineActorSide.Player
                ? battle.Player
                : battle.Enemy;
            string signature = $"{_statusDetailsSide}:"
                + BuildStatusSignature(combatant.ActiveStatuses);
            if (signature == _statusDetailsSignature)
            {
                return;
            }

            _statusDetailsSignature = signature;
            _statusDetailsTitle.text = $"{combatant.DisplayName} — Status Details";
            int statusCount = combatant.ActiveStatuses.Count;
            _statusDetailsSummary.text = statusCount == 1
                ? "1 active status"
                : $"{statusCount} active statuses";
            _statusDetailsList.Clear();
            _statusDetailsEmpty.style.display = statusCount == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            for (int index = 0; index < statusCount; index++)
            {
                _statusDetailsList.Add(CreateStatusEntry(
                    combatant.ActiveStatuses[index]));
            }
        }

        private static VisualElement CreateStatusEntry(CombatStatusInstance status)
        {
            CombatStatusDefinition definition = status.Definition;
            var entry = new VisualElement();
            entry.AddToClassList("combat-hud-status-entry");

            var header = new VisualElement();
            header.AddToClassList("combat-hud-status-entry-header");
            var name = new Label(definition.DisplayName);
            name.AddToClassList("combat-hud-status-entry-name");
            var duration = new Label(FormatRemainingDuration(status));
            duration.AddToClassList("combat-hud-status-entry-duration");
            header.Add(name);
            header.Add(duration);
            entry.Add(header);

            var effect = new Label(FormatStatusEffect(definition));
            effect.AddToClassList("combat-hud-status-entry-effect");
            entry.Add(effect);

            string stackingText = definition.StackingPolicy
                == CombatStatusStackingPolicy.IndependentInstances
                ? "Independent stacking instance"
                : "Reapplying this status refreshes its duration";
            var stacking = new Label(stackingText);
            stacking.AddToClassList("combat-hud-status-entry-stacking");
            entry.Add(stacking);
            return entry;
        }

        private static string FormatRemainingDuration(CombatStatusInstance status)
        {
            string unit = status.Definition.DurationType
                == CombatStatusDurationType.TimeSteps
                ? (status.RemainingDuration == 1 ? "Time Step" : "Time Steps")
                : (status.RemainingDuration == 1 ? "Round" : "Rounds");
            return $"{status.RemainingDuration} {unit} remaining";
        }

        private static string FormatStatusEffect(CombatStatusDefinition definition)
        {
            var effects = new List<string>();
            if (Math.Abs(definition.Modifier.Amount) > double.Epsilon)
            {
                string direction = definition.ModifierDirection
                    == CombatStatusModifierDirection.DamageDealt
                    ? "Damage dealt"
                    : "Damage taken";
                string target = FormatModifierTarget(definition.Modifier);
                string sign = definition.Modifier.Amount > 0d ? "+" : string.Empty;
                effects.Add(
                    $"{direction} ({target}) {sign}"
                    + $"{definition.Modifier.Amount * 100d:0.#}%");
            }

            if (definition.StaminaRecoveryMultiplierBonus > double.Epsilon)
            {
                effects.Add(
                    "Base Stamina recovery +"
                    + $"{definition.StaminaRecoveryMultiplierBonus * 100d:0.#}%");
            }

            return effects.Count > 0
                ? string.Join("\n", effects)
                : "No additional passive modifier.";
        }

        private static string FormatModifierTarget(DamageModifier modifier)
        {
            switch (modifier.Scope)
            {
                case DamageModifierScope.All:
                    return "all damage";
                case DamageModifierScope.Family:
                    return $"{modifier.Family} damage";
                case DamageModifierScope.Type:
                    return $"{modifier.Type} damage";
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
