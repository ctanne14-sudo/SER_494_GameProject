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
        private VisualElement _comboList;
        private Button _clearPreviewButton;
        private CombatSandboxController _sandbox;
        private CombatSandboxController _builtForSandbox;
        private bool _isVisible;
        private int _lastTimelineMarker = -1;
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
            _comboList = documentRoot.Q<VisualElement>("comboList");
            _clearPreviewButton = documentRoot.Q<Button>("clearPreviewButton");
            _clearPreviewButton.clicked += ClearPreview;

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
            int marker = _sandbox.IsTimelinePlaying
                ? Mathf.Clamp(_sandbox.PlaybackTimeStep, 1, TimelineCellCount)
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

            _timelinePageLabel.text = "T1–T30";
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
                var cell = new VisualElement();
                cell.AddToClassList("combat-hud-timeline-cell");
                if ((index + 1) % 5 == 0)
                {
                    cell.AddToClassList("combat-hud-timeline-major-cell");
                    cell.tooltip = $"T{index + 1}";
                }

                container.Add(cell);
                cells.Add(cell);
            }
        }
    }
}
