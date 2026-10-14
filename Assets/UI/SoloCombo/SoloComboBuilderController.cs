using System;
using System.Collections.Generic;
using System.Text;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SER494.Gameplay.Combat.UI.SoloCombo
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SoloComboBuilderController : MonoBehaviour
    {
        private const int ComboCount = 4;
        private const string EmptyOption = "— None —";
        private const string CombatSceneName = "CombatSandbox";

        [Header("Runtime Handoff")]
        [SerializeField]
        private SoloComboLoadoutSession loadoutSession = null;

        [Header("Available Actions")]
        [SerializeField]
        private ComboActionAsset[] availableActions = Array.Empty<ComboActionAsset>();

        [Header("Character Limits")]
        [SerializeField]
        [Min(0)]
        private int maximumLoad = 30;

        [SerializeField]
        [Min(0)]
        private int maximumMana = 30;

        [SerializeField]
        [Min(0)]
        private int maximumStamina = 30;

        private readonly ComboDraft[] _drafts = new ComboDraft[ComboCount];

        private UIDocument _document;
        private VisualElement _root;
        private Button[] _comboButtons;
        private TextField _comboNameField;
        private DropdownField _openingActionField;
        private DropdownField _mainActionField;
        private DropdownField _closingActionField;
        private DropdownField _linkedActionField;
        private DropdownField _linkedPlacementField;
        private Label _loadCostLabel;
        private Label _manaCostLabel;
        private Label _staminaCostLabel;
        private Label _cooldownLabel;
        private Label _currentComboStatusLabel;
        private Label _loadoutStatusLabel;
        private Label _actionLibraryLabel;
        private Button _resetComboButton;
        private Button _fillSampleLoadoutButton;
        private Button _startBattleButton;

        private List<ComboActionAsset> _openingOptions;
        private List<ComboActionAsset> _mainOptions;
        private List<ComboActionAsset> _closingOptions;
        private List<ComboActionAsset> _linkedOptions;
        private int _selectedComboIndex;
        private bool _isBound;

        public SoloComboLoadoutSession LoadoutSession
        {
            get => loadoutSession;
            set => loadoutSession = value;
        }

        private void Awake()
        {
            for (int index = 0; index < _drafts.Length; index++)
            {
                _drafts[index] = ComboDraft.CreateDefault(index);
            }

            if (loadoutSession != null)
            {
                loadoutSession.Clear();
            }
        }

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            _root.schedule.Execute(BindUi).ExecuteLater(0);
        }

        private void OnDisable()
        {
            UnbindUi();
        }

        private void OnValidate()
        {
            maximumLoad = Mathf.Max(0, maximumLoad);
            maximumMana = Mathf.Max(0, maximumMana);
            maximumStamina = Mathf.Max(0, maximumStamina);
        }

        private void BindUi()
        {
            if (!isActiveAndEnabled || _isBound)
            {
                return;
            }

            _comboButtons = new[]
            {
                _root.Q<Button>("combo1Button"),
                _root.Q<Button>("combo2Button"),
                _root.Q<Button>("combo3Button"),
                _root.Q<Button>("combo4Button")
            };
            _comboNameField = _root.Q<TextField>("comboNameField");
            _openingActionField = _root.Q<DropdownField>("openingActionField");
            _mainActionField = _root.Q<DropdownField>("mainActionField");
            _closingActionField = _root.Q<DropdownField>("closingActionField");
            _linkedActionField = _root.Q<DropdownField>("linkedActionField");
            _linkedPlacementField = _root.Q<DropdownField>("linkedPlacementField");
            _loadCostLabel = _root.Q<Label>("loadCostLabel");
            _manaCostLabel = _root.Q<Label>("manaCostLabel");
            _staminaCostLabel = _root.Q<Label>("staminaCostLabel");
            _cooldownLabel = _root.Q<Label>("cooldownLabel");
            _currentComboStatusLabel = _root.Q<Label>("currentComboStatusLabel");
            _loadoutStatusLabel = _root.Q<Label>("loadoutStatusLabel");
            _actionLibraryLabel = _root.Q<Label>("actionLibraryLabel");
            _resetComboButton = _root.Q<Button>("resetComboButton");
            _fillSampleLoadoutButton = _root.Q<Button>("fillSampleLoadoutButton");
            _startBattleButton = _root.Q<Button>("startBattleButton");

            if (!HasRequiredElements())
            {
                Debug.LogError("SoloComboBuilder.uxml is missing one or more required named elements.", this);
                return;
            }

            BuildActionOptions();
            RegisterCallbacks();
            _isBound = true;
            SelectCombo(0);
            RefreshActionLibrary();
        }

        private void UnbindUi()
        {
            if (!_isBound)
            {
                return;
            }

            _comboButtons[0].clicked -= SelectCombo1;
            _comboButtons[1].clicked -= SelectCombo2;
            _comboButtons[2].clicked -= SelectCombo3;
            _comboButtons[3].clicked -= SelectCombo4;
            _comboNameField.UnregisterValueChangedCallback(OnComboNameChanged);
            _openingActionField.UnregisterValueChangedCallback(OnOpeningActionChanged);
            _mainActionField.UnregisterValueChangedCallback(OnMainActionChanged);
            _closingActionField.UnregisterValueChangedCallback(OnClosingActionChanged);
            _linkedActionField.UnregisterValueChangedCallback(OnLinkedActionChanged);
            _linkedPlacementField.UnregisterValueChangedCallback(OnLinkedPlacementChanged);
            _resetComboButton.clicked -= ResetCurrentCombo;
            _fillSampleLoadoutButton.clicked -= FillSampleLoadout;
            _startBattleButton.clicked -= StartTestBattle;
            _isBound = false;
        }

        private bool HasRequiredElements()
        {
            for (int index = 0; index < _comboButtons.Length; index++)
            {
                if (_comboButtons[index] == null)
                {
                    return false;
                }
            }

            return _comboNameField != null
                && _openingActionField != null
                && _mainActionField != null
                && _closingActionField != null
                && _linkedActionField != null
                && _linkedPlacementField != null
                && _loadCostLabel != null
                && _manaCostLabel != null
                && _staminaCostLabel != null
                && _cooldownLabel != null
                && _currentComboStatusLabel != null
                && _loadoutStatusLabel != null
                && _actionLibraryLabel != null
                && _resetComboButton != null
                && _fillSampleLoadoutButton != null
                && _startBattleButton != null;
        }

        private void BuildActionOptions()
        {
            _openingOptions = BuildOptions(ComboActionCategory.Opening);
            _mainOptions = BuildOptions(ComboActionCategory.Main);
            _closingOptions = BuildOptions(ComboActionCategory.Closing);
            _linkedOptions = BuildOptions(ComboActionCategory.Linked);

            _openingActionField.choices = BuildLabels(_openingOptions);
            _mainActionField.choices = BuildLabels(_mainOptions);
            _closingActionField.choices = BuildLabels(_closingOptions);
            _linkedActionField.choices = BuildLabels(_linkedOptions);
            _linkedPlacementField.choices = new List<string>
            {
                PlacementLabel(LinkedActionPlacement.AfterOpening),
                PlacementLabel(LinkedActionPlacement.AfterMain),
                PlacementLabel(LinkedActionPlacement.AfterClosing)
            };
        }

        private List<ComboActionAsset> BuildOptions(ComboActionCategory category)
        {
            var options = new List<ComboActionAsset> { null };

            for (int index = 0; index < availableActions.Length; index++)
            {
                ComboActionAsset action = availableActions[index];
                if (action != null && action.Category == category)
                {
                    options.Add(action);
                }
            }

            return options;
        }

        private void RegisterCallbacks()
        {
            _comboButtons[0].clicked += SelectCombo1;
            _comboButtons[1].clicked += SelectCombo2;
            _comboButtons[2].clicked += SelectCombo3;
            _comboButtons[3].clicked += SelectCombo4;
            _comboNameField.RegisterValueChangedCallback(OnComboNameChanged);
            _openingActionField.RegisterValueChangedCallback(OnOpeningActionChanged);
            _mainActionField.RegisterValueChangedCallback(OnMainActionChanged);
            _closingActionField.RegisterValueChangedCallback(OnClosingActionChanged);
            _linkedActionField.RegisterValueChangedCallback(OnLinkedActionChanged);
            _linkedPlacementField.RegisterValueChangedCallback(OnLinkedPlacementChanged);
            _resetComboButton.clicked += ResetCurrentCombo;
            _fillSampleLoadoutButton.clicked += FillSampleLoadout;
            _startBattleButton.clicked += StartTestBattle;
        }

        private void SelectCombo1() => SelectCombo(0);

        private void SelectCombo2() => SelectCombo(1);

        private void SelectCombo3() => SelectCombo(2);

        private void SelectCombo4() => SelectCombo(3);

        private void SelectCombo(int comboIndex)
        {
            _selectedComboIndex = comboIndex;
            RefreshTabs();
            RefreshEditorFields();
            RefreshSummaryAndValidation();
        }

        private void OnComboNameChanged(ChangeEvent<string> changeEvent)
        {
            _drafts[_selectedComboIndex].Name = changeEvent.newValue;
            RefreshTabs();
            RefreshSummaryAndValidation();
        }

        private void OnOpeningActionChanged(ChangeEvent<string> changeEvent)
        {
            _drafts[_selectedComboIndex].OpeningAction = FindOption(_openingOptions, changeEvent.newValue);
            RefreshSummaryAndValidation();
        }

        private void OnMainActionChanged(ChangeEvent<string> changeEvent)
        {
            _drafts[_selectedComboIndex].MainAction = FindOption(_mainOptions, changeEvent.newValue);
            RefreshSummaryAndValidation();
        }

        private void OnClosingActionChanged(ChangeEvent<string> changeEvent)
        {
            _drafts[_selectedComboIndex].ClosingAction = FindOption(_closingOptions, changeEvent.newValue);
            RefreshSummaryAndValidation();
        }

        private void OnLinkedActionChanged(ChangeEvent<string> changeEvent)
        {
            _drafts[_selectedComboIndex].LinkedAction = FindOption(_linkedOptions, changeEvent.newValue);
            RefreshEditorFields();
            RefreshSummaryAndValidation();
        }

        private void OnLinkedPlacementChanged(ChangeEvent<string> changeEvent)
        {
            _drafts[_selectedComboIndex].LinkedPlacement = ParsePlacement(changeEvent.newValue);
            RefreshSummaryAndValidation();
        }

        private void ResetCurrentCombo()
        {
            _drafts[_selectedComboIndex] = ComboDraft.CreateDefault(_selectedComboIndex);
            RefreshTabs();
            RefreshEditorFields();
            RefreshSummaryAndValidation();
        }

        private void FillSampleLoadout()
        {
            string[,] presetActionIds =
            {
                { "sample.focus-stance", "sample.arcane-slash", "sample.backstep", "sample.flame-echo" },
                { "sample.quick-guard", "sample.guard-breaker", "sample.guarded-retreat", "sample.ward-echo" },
                { "sample.sidestep", "sample.ice-lance", "sample.parting-slash", "sample.vanish-step" },
                { "sample.ember-primer", "sample.precise-shot", "sample.steady-finish", "sample.spark-echo" }
            };
            var presetActions = new ComboActionAsset[ComboCount, 4];
            var linkedPlacements = new[]
            {
                LinkedActionPlacement.AfterMain,
                LinkedActionPlacement.AfterMain,
                LinkedActionPlacement.AfterOpening,
                LinkedActionPlacement.AfterMain
            };

            for (int comboIndex = 0; comboIndex < ComboCount; comboIndex++)
            {
                for (int slotIndex = 0; slotIndex < 4; slotIndex++)
                {
                    string actionId = presetActionIds[comboIndex, slotIndex];
                    if (string.IsNullOrEmpty(actionId))
                    {
                        presetActions[comboIndex, slotIndex] = null;
                        continue;
                    }

                    presetActions[comboIndex, slotIndex] = FindAvailableAction(actionId);
                    if (presetActions[comboIndex, slotIndex] == null)
                    {
                        string message = $"Quick Fill requires the sample Action '{actionId}'. Repair the Combo Builder prototype scene.";
                        SetStatus(_loadoutStatusLabel, $"Loadout: {message}", isValid: false);
                        _startBattleButton.SetEnabled(false);
                        Debug.LogError(message, this);
                        return;
                    }
                }
            }

            for (int comboIndex = 0; comboIndex < ComboCount; comboIndex++)
            {
                _drafts[comboIndex] = ComboDraft.CreatePreset(
                    comboIndex,
                    presetActions[comboIndex, 0],
                    presetActions[comboIndex, 1],
                    presetActions[comboIndex, 2],
                    presetActions[comboIndex, 3],
                    linkedPlacements[comboIndex]);
            }

            RefreshTabs();
            RefreshEditorFields();
            RefreshSummaryAndValidation();
        }

        private void StartTestBattle()
        {
            if (loadoutSession == null)
            {
                Debug.LogError("Solo Combo Builder has no runtime loadout session assigned.", this);
                return;
            }

            if (!TryBuildLoadout(out List<ComboConfiguration> configurations, out string error))
            {
                Debug.LogError($"Cannot start the test battle: {error}", this);
                return;
            }

            var limits = new ComboValidationLimits(maximumLoad, maximumMana, maximumStamina);
            ComboValidationResult result = ComboLoadoutValidator.Validate(configurations, limits);
            if (!result.IsValid)
            {
                Debug.LogError("Cannot start the test battle while the Combo loadout is invalid.", this);
                return;
            }

            loadoutSession.Prepare(configurations);
            SceneManager.LoadScene(CombatSceneName);
        }

        private void RefreshTabs()
        {
            for (int index = 0; index < _comboButtons.Length; index++)
            {
                string comboName = string.IsNullOrWhiteSpace(_drafts[index].Name)
                    ? $"combo{index + 1}"
                    : _drafts[index].Name;
                _comboButtons[index].text = comboName;
                _comboButtons[index].EnableInClassList("combo-tab-selected", index == _selectedComboIndex);
            }
        }

        private void RefreshEditorFields()
        {
            ComboDraft draft = _drafts[_selectedComboIndex];
            _comboNameField.SetValueWithoutNotify(draft.Name);
            SetDropdownValue(_openingActionField, draft.OpeningAction);
            SetDropdownValue(_mainActionField, draft.MainAction);
            SetDropdownValue(_closingActionField, draft.ClosingAction);
            SetDropdownValue(_linkedActionField, draft.LinkedAction);
            _linkedPlacementField.SetValueWithoutNotify(PlacementLabel(draft.LinkedPlacement));
            _linkedPlacementField.SetEnabled(draft.LinkedAction != null);
        }

        private void RefreshSummaryAndValidation()
        {
            ComboDraft draft = _drafts[_selectedComboIndex];
            _loadCostLabel.text = $"Load: {draft.TotalLoadCost} / {maximumLoad}";
            _manaCostLabel.text = $"Mana: {draft.TotalManaCost} / {maximumMana}";
            _staminaCostLabel.text = $"Stamina: {draft.TotalStaminaCost} / {maximumStamina}";
            _cooldownLabel.text = $"Cooldown: {draft.TotalCooldown}";

            var limits = new ComboValidationLimits(maximumLoad, maximumMana, maximumStamina);

            if (!TryBuildConfiguration(draft, out ComboConfiguration currentCombo, out string authoringError))
            {
                SetStatus(_currentComboStatusLabel, $"Current Combo: {authoringError}", isValid: false);
            }
            else
            {
                ComboValidationResult currentResult = ComboValidator.Validate(currentCombo, limits);
                SetStatus(
                    _currentComboStatusLabel,
                    currentResult.IsValid
                        ? "Current Combo: Valid"
                        : $"Current Combo:\n{FormatIssues(currentResult.Issues, includeComboName: false)}",
                    currentResult.IsValid);
            }

            if (!TryBuildLoadout(out List<ComboConfiguration> configurations, out string loadoutAuthoringError))
            {
                SetStatus(_loadoutStatusLabel, $"Loadout: {loadoutAuthoringError}", isValid: false);
                _startBattleButton.SetEnabled(false);
                return;
            }

            ComboValidationResult loadoutResult = ComboLoadoutValidator.Validate(configurations, limits);
            if (loadoutSession == null)
            {
                SetStatus(
                    _loadoutStatusLabel,
                    "Loadout: Runtime session is not assigned. Run the Combo Builder scene setup tool.",
                    isValid: false);
                _startBattleButton.SetEnabled(false);
                return;
            }

            SetStatus(
                _loadoutStatusLabel,
                loadoutResult.IsValid
                    ? "Loadout: All four Combos are valid"
                    : $"Loadout:\n{FormatIssues(loadoutResult.Issues, includeComboName: true)}",
                loadoutResult.IsValid);
            _startBattleButton.SetEnabled(loadoutResult.IsValid);
        }

        private bool TryBuildLoadout(
            out List<ComboConfiguration> configurations,
            out string validationError)
        {
            configurations = new List<ComboConfiguration>(ComboCount);

            for (int index = 0; index < _drafts.Length; index++)
            {
                if (!TryBuildConfiguration(
                        _drafts[index],
                        out ComboConfiguration configuration,
                        out string error))
                {
                    validationError = $"combo{index + 1}: {error}";
                    return false;
                }

                configurations.Add(configuration);
            }

            validationError = null;
            return true;
        }

        private bool TryBuildConfiguration(
            ComboDraft draft,
            out ComboConfiguration configuration,
            out string validationError)
        {
            configuration = null;

            if (string.IsNullOrWhiteSpace(draft.Name))
            {
                validationError = "Combo Name cannot be empty.";
                return false;
            }

            if (!TryCreateDefinition(draft.OpeningAction, out ComboActionDefinition opening, out validationError)
                || !TryCreateDefinition(draft.MainAction, out ComboActionDefinition main, out validationError)
                || !TryCreateDefinition(draft.ClosingAction, out ComboActionDefinition closing, out validationError)
                || !TryCreateDefinition(draft.LinkedAction, out ComboActionDefinition linked, out validationError))
            {
                return false;
            }

            configuration = new ComboConfiguration(
                draft.Name.Trim(),
                opening,
                main,
                closing,
                linked,
                linked != null ? draft.LinkedPlacement : (LinkedActionPlacement?)null);
            validationError = null;
            return true;
        }

        private static bool TryCreateDefinition(
            ComboActionAsset asset,
            out ComboActionDefinition definition,
            out string validationError)
        {
            if (asset == null)
            {
                definition = null;
                validationError = null;
                return true;
            }

            return asset.TryCreateDefinition(out definition, out validationError);
        }

        private void RefreshActionLibrary()
        {
            var builder = new StringBuilder();

            for (int index = 0; index < availableActions.Length; index++)
            {
                ComboActionAsset action = availableActions[index];
                if (action == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(action.DisplayName)
                    .Append(" — ")
                    .Append(action.Category)
                    .Append(" | Load ")
                    .Append(action.LoadCost)
                    .Append(" | Mana ")
                    .Append(action.ManaCost)
                    .Append(" | Stamina ")
                    .Append(action.StaminaCost)
                    .Append(" | CD ")
                    .Append(action.Cooldown)
                    .Append(action.UsesResolutionWindow
                        ? " | Resolution Window — Local T+"
                        : " | Resolution Point — Local T+")
                    .Append(action.UsesResolutionWindow
                        ? $"{action.ResolutionWindowStart}–{action.ResolutionWindowEnd}"
                        : action.ResolutionOffset.ToString())
                    .Append(": ")
                    .Append(action.ResolutionEffect);
                if (action.ResolutionEffect == ActionResolutionEffect.Damage)
                {
                    builder.Append(" (")
                        .Append(action.DamageType)
                        .Append(" x")
                        .Append(action.DamageMultiplier.ToString("0.00"));
                    if (action.DamageTags != DamageResolutionTag.None)
                    {
                        builder.Append(" | Tags: ")
                            .Append(action.DamageTags);
                    }

                    builder.Append(')');
                    if (action.AppliesOnHitDisruption)
                    {
                        builder.Append(" | On Hit: ")
                            .Append(action.OnHitDisruptionType)
                            .Append(" for ")
                            .Append(action.OnHitDisruptionDuration)
                            .Append(" Time Steps");
                    }

                    if (action.AppliesOnGuardedDisruption)
                    {
                        builder.Append(" | On Guarded: self-")
                            .Append(action.OnGuardedDisruptionType)
                            .Append(" for ")
                            .Append(action.OnGuardedDisruptionDuration)
                            .Append(" Time Steps");
                    }

                    if (action.AppliesOnEvadedDisruption)
                    {
                        builder.Append(" | On Evaded: self-")
                            .Append(action.OnEvadedDisruptionType)
                            .Append(" for ")
                            .Append(action.OnEvadedDisruptionDuration)
                            .Append(" Time Steps");
                    }

                    if ((action.DamageTags & DamageResolutionTag.Clash) != 0)
                    {
                        builder.Append(" | Clash: -")
                            .Append((action.ClashDamageReduction * 100f).ToString("0"))
                            .Append("% incoming Damage");
                        if (action.ClashSuperArmorLevel > 0)
                        {
                            builder.Append(" | Super Armor Lv")
                                .Append(action.ClashSuperArmorLevel);
                        }
                    }
                }
                else if (action.ResolutionEffect == ActionResolutionEffect.None)
                {
                    builder.Append(" (no Damage / Guard / Evade prototype effect)");
                }

                if (action.ChargeLevelCount > 0)
                {
                    builder.Append(" | Charge: ");
                    for (int level = 1; level <= action.ChargeLevelCount; level++)
                    {
                        if (level > 1)
                        {
                            builder.Append("; ");
                        }

                        builder.Append('L')
                            .Append(level)
                            .Append(" +")
                            .Append(action.GetChargeAdditionalLoad(level))
                            .Append(" Load x")
                            .Append(action.GetChargeDamageMultiplierScale(level).ToString("0.00"));
                    }
                }

                for (int windowIndex = 0;
                    windowIndex < action.ActiveWindowCount;
                    windowIndex++)
                {
                    builder.Append(" | Active: ")
                        .Append(action.GetActiveWindowDisplayName(windowIndex))
                        .Append(" Local T+")
                        .Append(action.GetActiveWindowStartOffset(windowIndex))
                        .Append('–')
                        .Append(action.GetActiveWindowEndOffset(windowIndex))
                        .Append(" (-")
                        .Append((action.GetActiveWindowDamageReduction(windowIndex) * 100f)
                            .ToString("0"))
                        .Append("% Damage Taken)");
                }
            }

            _actionLibraryLabel.text = builder.Length == 0
                ? "No Actions assigned to the builder."
                : builder.ToString();
        }

        private static List<string> BuildLabels(IReadOnlyList<ComboActionAsset> options)
        {
            var labels = new List<string>(options.Count);
            for (int index = 0; index < options.Count; index++)
            {
                labels.Add(OptionLabel(options[index]));
            }

            return labels;
        }

        private static ComboActionAsset FindOption(
            IReadOnlyList<ComboActionAsset> options,
            string selectedLabel)
        {
            for (int index = 0; index < options.Count; index++)
            {
                if (OptionLabel(options[index]) == selectedLabel)
                {
                    return options[index];
                }
            }

            return null;
        }

        private ComboActionAsset FindAvailableAction(string actionId)
        {
            for (int index = 0; index < availableActions.Length; index++)
            {
                ComboActionAsset action = availableActions[index];
                if (action != null
                    && string.Equals(action.ActionId, actionId, StringComparison.Ordinal))
                {
                    return action;
                }
            }

            return null;
        }

        private static string OptionLabel(ComboActionAsset action)
        {
            return action == null
                ? EmptyOption
                : $"{action.DisplayName} [{action.ActionId}]";
        }

        private static void SetDropdownValue(DropdownField field, ComboActionAsset action)
        {
            field.SetValueWithoutNotify(OptionLabel(action));
        }

        private static string PlacementLabel(LinkedActionPlacement placement)
        {
            switch (placement)
            {
                case LinkedActionPlacement.AfterOpening:
                    return "After Opening";
                case LinkedActionPlacement.AfterClosing:
                    return "After Closing";
                default:
                    return "After Main";
            }
        }

        private static LinkedActionPlacement ParsePlacement(string label)
        {
            switch (label)
            {
                case "After Opening":
                    return LinkedActionPlacement.AfterOpening;
                case "After Closing":
                    return LinkedActionPlacement.AfterClosing;
                default:
                    return LinkedActionPlacement.AfterMain;
            }
        }

        private static string FormatIssues(
            IReadOnlyList<ComboValidationIssue> issues,
            bool includeComboName)
        {
            const int maximumDisplayedIssues = 5;
            var builder = new StringBuilder();
            int displayedIssues = Mathf.Min(issues.Count, maximumDisplayedIssues);

            for (int index = 0; index < displayedIssues; index++)
            {
                ComboValidationIssue issue = issues[index];
                if (index > 0)
                {
                    builder.AppendLine();
                }

                builder.Append("• ");
                if (includeComboName && !string.IsNullOrWhiteSpace(issue.ComboName))
                {
                    builder.Append(issue.ComboName).Append(": ");
                }

                builder.Append(IssueText(issue.Code));
            }

            if (issues.Count > maximumDisplayedIssues)
            {
                builder.AppendLine()
                    .Append("• ")
                    .Append(issues.Count - maximumDisplayedIssues)
                    .Append(" more issue(s)");
            }

            return builder.ToString();
        }

        private static string IssueText(ComboValidationErrorCode code)
        {
            switch (code)
            {
                case ComboValidationErrorCode.MissingMainAction:
                    return "Main Action is required.";
                case ComboValidationErrorCode.InvalidOpeningActionCategory:
                    return "Opening slot contains the wrong Action category.";
                case ComboValidationErrorCode.InvalidMainActionCategory:
                    return "Main slot contains the wrong Action category.";
                case ComboValidationErrorCode.InvalidClosingActionCategory:
                    return "Closing slot contains the wrong Action category.";
                case ComboValidationErrorCode.InvalidLinkedActionCategory:
                    return "Linked slot contains the wrong Action category.";
                case ComboValidationErrorCode.MissingLinkedActionPlacement:
                    return "Linked Action needs a placement.";
                case ComboValidationErrorCode.LinkedActionPlacementWithoutLinkedAction:
                    return "Linked placement exists without a Linked Action.";
                case ComboValidationErrorCode.LinkedActionPlacementHasNoAction:
                    return "Linked Action must follow an occupied slot.";
                case ComboValidationErrorCode.LoadCostExceeded:
                    return "Load Cost exceeds the character limit.";
                case ComboValidationErrorCode.ManaCostExceeded:
                    return "Mana Cost exceeds Max Mana.";
                case ComboValidationErrorCode.StaminaCostExceeded:
                    return "Stamina Cost exceeds Max Stamina.";
                case ComboValidationErrorCode.MutuallyExclusiveResolutionEffectsOverlap:
                    return "Damage, Guard, and Evade Resolution Points cannot overlap.";
                case ComboValidationErrorCode.ActionAssignedMoreThanOnce:
                    return "The same Action is assigned more than once.";
                case ComboValidationErrorCode.EmptyLoadout:
                    return "The loadout has no Combos.";
                case ComboValidationErrorCode.NullCombo:
                    return "The loadout contains a missing Combo.";
                default:
                    return code.ToString();
            }
        }

        private static void SetStatus(Label label, string text, bool isValid)
        {
            label.text = text;
            label.EnableInClassList("status-valid", isValid);
            label.EnableInClassList("status-invalid", !isValid);
        }

        private sealed class ComboDraft
        {
            public string Name;
            public ComboActionAsset OpeningAction;
            public ComboActionAsset MainAction;
            public ComboActionAsset ClosingAction;
            public ComboActionAsset LinkedAction;
            public LinkedActionPlacement LinkedPlacement = LinkedActionPlacement.AfterMain;

            public int TotalLoadCost => Sum(action => action.LoadCost);

            public int TotalManaCost => Sum(action => action.ManaCost);

            public int TotalStaminaCost => Sum(action => action.StaminaCost);

            public int TotalCooldown => Sum(action => action.Cooldown);

            public static ComboDraft CreateDefault(int zeroBasedIndex)
            {
                return new ComboDraft
                {
                    Name = $"combo{zeroBasedIndex + 1}"
                };
            }

            public static ComboDraft CreatePreset(
                int zeroBasedIndex,
                ComboActionAsset opening,
                ComboActionAsset main,
                ComboActionAsset closing,
                ComboActionAsset linked,
                LinkedActionPlacement linkedPlacement)
            {
                return new ComboDraft
                {
                    Name = $"combo{zeroBasedIndex + 1}",
                    OpeningAction = opening,
                    MainAction = main,
                    ClosingAction = closing,
                    LinkedAction = linked,
                    LinkedPlacement = linkedPlacement
                };
            }

            private int Sum(Func<ComboActionAsset, int> selector)
            {
                int total = 0;
                AddIfPresent(OpeningAction, selector, ref total);
                AddIfPresent(MainAction, selector, ref total);
                AddIfPresent(ClosingAction, selector, ref total);
                AddIfPresent(LinkedAction, selector, ref total);
                return total;
            }

            private static void AddIfPresent(
                ComboActionAsset action,
                Func<ComboActionAsset, int> selector,
                ref int total)
            {
                if (action != null)
                {
                    total += selector(action);
                }
            }
        }
    }
}
