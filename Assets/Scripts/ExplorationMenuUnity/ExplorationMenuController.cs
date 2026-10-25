using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ExplorationMenuController : MonoBehaviour
    {
        private const string AccessorySlotId = "accessory.primary";
        private const string CastIronGreatswordId = "weapon.greatsword.cast-iron-greatsword";
        private const string LeatherHeadpieceId = "armor.leather-headpiece";
        private const string LeatherShirtId = "armor.leather-shirt";
        private const string LeatherHoseId = "armor.leather-hose";
        private const string RustRingId = "accessory.rust-ring";
        private const string RubyRingId = "accessory.ruby-ring";

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _content;
        private Label _pageTitle;
        private Button _statusButton;
        private Button _comboButton;
        private Button _inventoryButton;
        private Button _equipmentButton;
        private Button _returnToTitleButton;
        private bool _isOpen;
        private float _previousTimeScale;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private MenuPage _currentPage = MenuPage.None;
        private EquipmentSlot _editingEquipmentSlot = EquipmentSlot.None;
        private bool _isInsideEquipmentPage;
        private VisualElement _equipmentWarehouse;
        private readonly Dictionary<EquipmentSlot, Button> _equipmentSlotButtons =
            new Dictionary<EquipmentSlot, Button>();
        private readonly List<Button> _sidebarButtons = new List<Button>();
        private readonly List<Button> _equipmentSlotButtonOrder = new List<Button>();
        private readonly List<Button> _equipmentWarehouseButtons = new List<Button>();
        private readonly List<Button> _comboPageButtons = new List<Button>();
        private readonly Dictionary<Button, Action> _buttonActions =
            new Dictionary<Button, Action>();
        private ComboMenuLevel _comboMenuLevel;
        private PlayerComboLoadoutState _selectedLoadout;
        private int _selectedComboIndex = -1;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            _content = _root.Q<VisualElement>("pageContent");
            _pageTitle = _root.Q<Label>("pageTitle");
            _statusButton = _root.Q<Button>("statusButton");
            _comboButton = _root.Q<Button>("comboButton");
            _inventoryButton = _root.Q<Button>("inventoryButton");
            _equipmentButton = _root.Q<Button>("equipmentButton");
            _returnToTitleButton = _root.Q<Button>("returnToTitleButton");

            if (_content == null || _pageTitle == null || _statusButton == null
                || _comboButton == null || _inventoryButton == null
                || _equipmentButton == null || _returnToTitleButton == null)
            {
                Debug.LogError("ExplorationMenu.uxml is missing required named elements.", this);
                enabled = false;
                return;
            }

            ApplyStaticText();
            _sidebarButtons.Clear();
            _sidebarButtons.Add(_statusButton);
            _sidebarButtons.Add(_comboButton);
            _sidebarButtons.Add(_inventoryButton);
            _sidebarButtons.Add(_equipmentButton);
            _sidebarButtons.Add(_returnToTitleButton);
            RegisterButtonAction(_statusButton, OnStatusClicked);
            RegisterButtonAction(_comboButton, OnCombosClicked);
            RegisterButtonAction(_inventoryButton, OnInventoryClicked);
            RegisterButtonAction(_equipmentButton, OnEquipmentClicked);
            RegisterButtonAction(_returnToTitleButton, OnReturnToTitleClicked);
            _statusButton.clicked += OnStatusClicked;
            _comboButton.clicked += OnCombosClicked;
            _inventoryButton.clicked += OnInventoryClicked;
            _equipmentButton.clicked += OnEquipmentClicked;
            _returnToTitleButton.clicked += OnReturnToTitleClicked;
            _root.RegisterCallback<WheelEvent>(OnWheelNavigation, TrickleDown.TrickleDown);
            _root.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            if (_statusButton != null) _statusButton.clicked -= OnStatusClicked;
            if (_comboButton != null) _comboButton.clicked -= OnCombosClicked;
            if (_inventoryButton != null) _inventoryButton.clicked -= OnInventoryClicked;
            if (_equipmentButton != null) _equipmentButton.clicked -= OnEquipmentClicked;
            if (_returnToTitleButton != null)
            {
                _returnToTitleButton.clicked -= OnReturnToTitleClicked;
            }
            _root?.UnregisterCallback<WheelEvent>(OnWheelNavigation, TrickleDown.TrickleDown);
            _buttonActions.Clear();

            if (_isOpen)
            {
                CloseMenu();
            }
        }

        private void Update()
        {
            bool togglePressed = Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame;
            togglePressed |= Gamepad.current != null
                && Gamepad.current.startButton.wasPressedThisFrame;

            if (togglePressed)
            {
                if (_isOpen) HandleBack();
                else TryOpenMenu();
                return;
            }

            bool backPressed = _isOpen && Gamepad.current != null
                && Gamepad.current.buttonEast.wasPressedThisFrame;
            backPressed |= _isOpen && Mouse.current != null
                && Mouse.current.rightButton.wasPressedThisFrame;
            if (backPressed)
            {
                HandleBack();
                return;
            }

            if (_isOpen)
            {
                HandleAdditionalMenuInput();
            }
        }

        private void TryOpenMenu()
        {
            if (!CanOpenInCurrentScene() || !PlayerStateService.TryGetActive(out _))
            {
                return;
            }

            _previousTimeScale = Time.timeScale;
            _previousCursorLockMode = UnityEngine.Cursor.lockState;
            _previousCursorVisible = UnityEngine.Cursor.visible;
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            ExplorationMenuInputGate.IsOpen = true;
            _isOpen = true;
            _root.style.display = DisplayStyle.Flex;
            _currentPage = MenuPage.None;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            _comboMenuLevel = ComboMenuLevel.None;
            _selectedLoadout = null;
            _selectedComboIndex = -1;

            GrantEditorTestEquipment();
            ShowStatus();
            _root.schedule.Execute(() => _statusButton.Focus()).ExecuteLater(1);
        }

        private void CloseMenu()
        {
            _root.style.display = DisplayStyle.None;
            _isOpen = false;
            _isInsideEquipmentPage = false;
            ExplorationMenuInputGate.IsOpen = false;
            Time.timeScale = _previousTimeScale;
            UnityEngine.Cursor.lockState = _previousCursorLockMode;
            UnityEngine.Cursor.visible = _previousCursorVisible;
        }

        private void HandleBack()
        {
            if (_currentPage == MenuPage.Combos)
            {
                if (_comboMenuLevel == ComboMenuLevel.ComboDetails && _selectedLoadout != null)
                {
                    ShowLoadoutDetails(_selectedLoadout);
                    return;
                }

                if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails)
                {
                    ShowCombos();
                    return;
                }
            }

            if (_currentPage == MenuPage.Equipment
                && _editingEquipmentSlot != EquipmentSlot.None)
            {
                CancelEquipmentChange();
                return;
            }

            if (_currentPage == MenuPage.Equipment && _isInsideEquipmentPage)
            {
                _isInsideEquipmentPage = false;
                _equipmentButton.Focus();
                return;
            }

            CloseMenu();
        }

        private void HandleAdditionalMenuInput()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                ActivateFocusedButton();
                return;
            }

            if (Gamepad.current == null)
            {
                return;
            }

            if (Gamepad.current.dpad.up.wasPressedThisFrame)
            {
                MoveFocus(-1);
            }
            else if (Gamepad.current.dpad.down.wasPressedThisFrame)
            {
                MoveFocus(1);
            }
        }

        private void ShowStatus()
        {
            if (!TryGetContext(out PersistentPlayerState state, out PlayerEquipmentCatalogAsset catalog))
            {
                return;
            }

            ApplyDerivedStats(state, catalog);
            _currentPage = MenuPage.Status;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            ClearComboControls();
            _pageTitle.text = ExplorationMenuText.Get("status.heading");
            _content.Clear();
            AddValueRow(ExplorationMenuText.Get("status.hp"),
                $"{state.CurrentHealth} / {state.MaximumHealth}");

            string weaponName = state.Equipment.EquippedWeaponId ?? "—";
            if (catalog.TryGetWeapon(
                    state.Equipment.EquippedWeaponId,
                    out var weapon,
                    out _))
            {
                weaponName =
                    $"{ExplorationMenuText.EquipmentName(weapon.WeaponId, weapon.DisplayName)}"
                    + $"  |  ATK {weapon.WeaponAttack}";
            }

            AddValueRow(ExplorationMenuText.Get("status.weapon"), weaponName);
            string defenseText = catalog.TryGetTotalDefense(
                state.Equipment.EquippedArmorBySlot,
                out int defense,
                out _) ? defense.ToString() : "—";
            AddValueRow(ExplorationMenuText.Get("status.defense"), defenseText);
            AddValueRow(
                ExplorationMenuText.Get("status.loadout"),
                state.ComboLoadouts.ActiveLoadout?.Name ?? "—");
            SetSelectedNavigation(_statusButton);
        }

        private void ShowCombos()
        {
            if (!TryGetContext(out PersistentPlayerState state, out _))
            {
                return;
            }

            _pageTitle.text = ExplorationMenuText.Get("combos.heading");
            _currentPage = MenuPage.Combos;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            _comboMenuLevel = ComboMenuLevel.LoadoutList;
            _selectedLoadout = null;
            _selectedComboIndex = -1;
            ClearComboControls();
            _content.Clear();
            AddBodyText(ExplorationMenuText.Get("combos.note"), "page-note");
            ComboActionCatalogAsset actionCatalog = ComboActionCatalogAsset.LoadDefault();
            foreach (PlayerComboLoadoutState loadout in state.ComboLoadouts.Loadouts)
            {
                PlayerComboLoadoutState capturedLoadout = loadout;
                var card = new Button(() => ShowLoadoutDetails(capturedLoadout));
                card.AddToClassList("loadout-card-button");
                RegisterButtonAction(card, () => ShowLoadoutDetails(capturedLoadout));
                _comboPageButtons.Add(card);

                var heading = new Label(loadout.Name);
                heading.AddToClassList("loadout-card-title");
                card.Add(heading);

                bool built = PlayerStateComboConfigurationBuilder.TryBuildLoadout(
                    loadout,
                    state,
                    actionCatalog,
                    out IReadOnlyList<ComboConfiguration> configurations,
                    out _);
                for (int index = 0; index < loadout.Combos.Count; index++)
                {
                    var previewRow = new VisualElement();
                    previewRow.AddToClassList("loadout-combo-preview-row");
                    var comboName = new Label($"{index + 1}. {loadout.Combos[index].Name}");
                    comboName.AddToClassList("loadout-combo-name");
                    previewRow.Add(comboName);
                    if (built)
                    {
                        var timeline = new Label(FormatCompactTimeline(configurations[index]));
                        timeline.AddToClassList("loadout-combo-timeline");
                        previewRow.Add(timeline);
                    }

                    card.Add(previewRow);
                }

                if (loadout.LoadoutId == state.ComboLoadouts.ActiveLoadoutId)
                {
                    var active = new Label("ACTIVE");
                    active.AddToClassList("active-badge");
                    card.Add(active);
                }

                _content.Add(card);
            }

            SetSelectedNavigation(_comboButton);
            FocusFirstComboPageButton();
        }

        private void ShowLoadoutDetails(PlayerComboLoadoutState loadout)
        {
            if (loadout == null
                || !TryGetContext(out PersistentPlayerState state, out _))
            {
                return;
            }

            _currentPage = MenuPage.Combos;
            _comboMenuLevel = ComboMenuLevel.LoadoutDetails;
            _selectedLoadout = loadout;
            _selectedComboIndex = -1;
            _pageTitle.text =
                $"{ExplorationMenuText.Get("combos.single-heading")} / {loadout.Name}";
            ClearComboControls();
            _content.Clear();

            AddComboBackButton(
                ExplorationMenuText.Get("combos.back-loadouts"),
                ShowCombos);

            bool isActive = loadout.LoadoutId == state.ComboLoadouts.ActiveLoadoutId;
            var status = new Label(isActive
                ? ExplorationMenuText.Get("combos.active")
                : ExplorationMenuText.Get("combos.inactive"));
            status.AddToClassList(isActive ? "combo-status-valid" : "combo-status-neutral");
            _content.Add(status);

            ComboActionCatalogAsset actionCatalog = ComboActionCatalogAsset.LoadDefault();
            bool built = PlayerStateComboConfigurationBuilder.TryBuildLoadout(
                loadout,
                state,
                actionCatalog,
                out IReadOnlyList<ComboConfiguration> configurations,
                out string buildError);

            if (!built)
            {
                AddComboError(buildError);
            }

            for (int index = 0; index < loadout.Combos.Count; index++)
            {
                int capturedIndex = index;
                PreparedComboState prepared = loadout.Combos[index];
                ComboConfiguration configuration = built ? configurations[index] : null;
                var button = new Button(() => ShowComboDetails(loadout, capturedIndex));
                button.AddToClassList("combo-card-button");
                RegisterButtonAction(button, () => ShowComboDetails(loadout, capturedIndex));
                _comboPageButtons.Add(button);

                var heading = new Label($"{index + 1}. {prepared.Name}");
                heading.AddToClassList("combo-card-title");
                button.Add(heading);

                if (configuration != null)
                {
                    var sequence = new Label(FormatActionSequence(configuration));
                    sequence.AddToClassList("combo-card-sequence");
                    button.Add(sequence);

                    var costs = new Label(
                        $"Timeline {configuration.TotalTimelineCost} / {PlayerStateLoadoutSessionBuilder.MaximumTimelineCost}"
                        + $"    Stamina {configuration.TotalStaminaCost} / {state.MaximumCombatStamina}"
                        + $"    Mana {configuration.TotalManaCost} / {state.MaximumMana}");
                    costs.AddToClassList("combo-card-costs");
                    button.Add(costs);
                }

                _content.Add(button);
            }

            if (built)
            {
                ComboValidationResult validation = ComboLoadoutValidator.Validate(
                    configurations,
                    new ComboValidationLimits(
                        PlayerStateLoadoutSessionBuilder.MaximumTimelineCost,
                        state.MaximumMana,
                        state.MaximumCombatStamina));
                var validationLabel = new Label(validation.IsValid
                    ? ExplorationMenuText.Get("combos.valid")
                    : $"{ExplorationMenuText.Get("combos.invalid")} {FormatFirstIssue(validation)}");
                validationLabel.AddToClassList(
                    validation.IsValid ? "combo-status-valid" : "combo-status-invalid");
                _content.Add(validationLabel);
            }

            SetSelectedNavigation(_comboButton);
            FocusFirstComboPageButton();
        }

        private void ShowComboDetails(PlayerComboLoadoutState loadout, int comboIndex)
        {
            if (loadout == null
                || comboIndex < 0
                || comboIndex >= loadout.Combos.Count
                || !TryGetContext(out PersistentPlayerState state, out _))
            {
                return;
            }

            _currentPage = MenuPage.Combos;
            _comboMenuLevel = ComboMenuLevel.ComboDetails;
            _selectedLoadout = loadout;
            _selectedComboIndex = comboIndex;
            ClearComboControls();
            _content.Clear();

            PreparedComboState prepared = loadout.Combos[comboIndex];
            _pageTitle.text =
                $"{ExplorationMenuText.Get("combos.actions-heading")} / {prepared.Name}";
            AddComboBackButton(
                ExplorationMenuText.Get("combos.back-combos"),
                () => ShowLoadoutDetails(loadout));

            ComboActionCatalogAsset actionCatalog = ComboActionCatalogAsset.LoadDefault();
            if (!PlayerStateComboConfigurationBuilder.TryBuildCombo(
                    prepared,
                    state,
                    actionCatalog,
                    out ComboConfiguration configuration,
                    out string validationError))
            {
                AddComboError(validationError);
                SetSelectedNavigation(_comboButton);
                FocusFirstComboPageButton();
                return;
            }

            AddSectionHeading(ExplorationMenuText.Get("combos.timeline"));
            AddTimelinePreview(configuration);
            AddTimelineLegend();
            AddSectionHeading(ExplorationMenuText.Get("combos.actions"));

            IReadOnlyList<ComboActionDefinition> executionOrder = configuration.BuildExecutionOrder();
            for (int index = 0; index < executionOrder.Count; index++)
            {
                AddActionDetail(executionOrder[index]);
            }

            AddBodyText(ExplorationMenuText.Get("combos.editor-next"), "page-note");
            SetSelectedNavigation(_comboButton);
            FocusFirstComboPageButton();
        }

        private void AddComboBackButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("combo-back-button");
            _content.Add(button);
            _comboPageButtons.Add(button);
            RegisterButtonAction(button, action);
        }

        private void AddTimelinePreview(ComboConfiguration configuration)
        {
            var scaleRow = new VisualElement();
            scaleRow.AddToClassList("timeline-scale-row");
            var scaleSpacer = new VisualElement();
            scaleSpacer.AddToClassList("timeline-action-spacer");
            scaleRow.Add(scaleSpacer);

            var scale = new VisualElement();
            scale.AddToClassList("timeline-scale");
            for (int timeStep = 1;
                 timeStep <= PlayerStateLoadoutSessionBuilder.MaximumTimelineCost;
                 timeStep++)
            {
                var tick = new Label(timeStep % 5 == 0
                    ? timeStep.ToString()
                    : string.Empty);
                tick.AddToClassList("timeline-scale-label");
                scale.Add(tick);
            }

            scaleRow.Add(scale);
            _content.Add(scaleRow);

            int actionStart = 0;
            IReadOnlyList<ComboActionDefinition> actions = configuration.BuildExecutionOrder();
            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                ComboActionDefinition action = actions[actionIndex];
                var row = new VisualElement();
                row.AddToClassList("timeline-row");
                var name = new Label(action.DisplayName);
                name.AddToClassList("timeline-action-name");
                row.Add(name);

                var track = new VisualElement();
                track.AddToClassList("timeline-track");
                for (int timeStep = 1;
                     timeStep <= PlayerStateLoadoutSessionBuilder.MaximumTimelineCost;
                     timeStep++)
                {
                    var cell = new TimelineResolutionCell();
                    if (timeStep > actionStart
                        && timeStep <= actionStart + action.TimelineCost)
                    {
                        cell.AddToClassList("timeline-cell-active");
                    }

                    ApplyResolutionMarker(cell, action, timeStep - actionStart);
                    track.Add(cell);
                }

                row.Add(track);
                _content.Add(row);
                actionStart += action.TimelineCost;
            }
        }

        private static void ApplyResolutionMarker(
            TimelineResolutionCell cell,
            ComboActionDefinition action,
            int localTime)
        {
            string combinedMarkerText = string.Empty;
            string firstMarkerClass = null;
            Color? windowStripeColor = null;
            bool isChargeWindow = IsChargeResolutionPoint(action, localTime);
            if (isChargeWindow)
            {
                combinedMarkerText = ResolutionMarkerText(action.ResolutionEffect);
                windowStripeColor = ResolutionMarkerColor(action.ResolutionEffect);
            }

            for (int index = 0; index < action.ResolutionEvents.Count; index++)
            {
                ActionResolutionDefinition resolution = action.ResolutionEvents[index];
                if (isChargeWindow && resolution.Offset == action.ResolutionOffset)
                {
                    continue;
                }

                if (resolution.HasResolutionWindow
                    && localTime >= resolution.ResolutionWindowStart.Value
                    && localTime <= resolution.ResolutionWindowEnd.Value)
                {
                    combinedMarkerText += ResolutionMarkerText(resolution.Effect);
                    windowStripeColor ??= ResolutionMarkerColor(resolution.Effect);
                    continue;
                }

                if (resolution.Offset != localTime)
                {
                    continue;
                }

                combinedMarkerText += ResolutionMarkerText(resolution.Effect);
                firstMarkerClass ??= ResolutionMarkerClass(resolution.Effect);
            }

            if (windowStripeColor.HasValue)
            {
                cell.SetResolutionWindow(windowStripeColor.Value);
            }

            if (combinedMarkerText.Length > 0)
            {
                cell.SetMarkerText(combinedMarkerText);
                if (!windowStripeColor.HasValue && firstMarkerClass != null)
                {
                    cell.AddToClassList(firstMarkerClass);
                }
            }
        }

        private static bool IsChargeResolutionPoint(
            ComboActionDefinition action,
            int localTime)
        {
            if (!action.IsChargeable)
            {
                return false;
            }

            if (localTime == action.ResolutionOffset)
            {
                return true;
            }

            for (int index = 0; index < action.ChargeLevels.Count; index++)
            {
                if (localTime == action.ResolutionOffset
                    + action.ChargeLevels[index].AdditionalTimelineCost)
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolutionMarkerText(ActionResolutionEffect effect)
        {
            switch (effect)
            {
                case ActionResolutionEffect.Damage: return "D";
                case ActionResolutionEffect.Guard: return "G";
                case ActionResolutionEffect.Evade: return "E";
                case ActionResolutionEffect.Heal: return "H";
                case ActionResolutionEffect.RestoreStamina: return "S";
                case ActionResolutionEffect.ApplyBuff:
                case ActionResolutionEffect.ApplyDebuff: return "B";
                default: return "•";
            }
        }

        private static string ResolutionMarkerClass(ActionResolutionEffect effect)
        {
            switch (effect)
            {
                case ActionResolutionEffect.Damage: return "timeline-marker-damage";
                case ActionResolutionEffect.Guard: return "timeline-marker-guard";
                case ActionResolutionEffect.Evade: return "timeline-marker-evade";
                case ActionResolutionEffect.Heal: return "timeline-marker-heal";
                case ActionResolutionEffect.RestoreStamina: return "timeline-marker-stamina";
                case ActionResolutionEffect.ApplyBuff:
                case ActionResolutionEffect.ApplyDebuff: return "timeline-marker-status";
                default: return "timeline-marker-other";
            }
        }

        private static Color ResolutionMarkerColor(ActionResolutionEffect effect)
        {
            switch (effect)
            {
                case ActionResolutionEffect.Damage: return new Color32(206, 76, 62, 230);
                case ActionResolutionEffect.Guard: return new Color32(80, 137, 207, 230);
                case ActionResolutionEffect.Evade: return new Color32(76, 190, 196, 230);
                case ActionResolutionEffect.Heal: return new Color32(83, 174, 108, 230);
                case ActionResolutionEffect.RestoreStamina: return new Color32(211, 170, 61, 230);
                case ActionResolutionEffect.ApplyBuff:
                case ActionResolutionEffect.ApplyDebuff:
                    return new Color32(170, 104, 207, 230);
                default: return new Color32(155, 143, 118, 230);
            }
        }

        private void AddTimelineLegend()
        {
            var legend = new Label(ExplorationMenuText.Get("combos.timeline-legend"));
            legend.AddToClassList("timeline-legend");
            _content.Add(legend);
        }

        private void AddActionDetail(ComboActionDefinition action)
        {
            var card = new VisualElement();
            card.AddToClassList("action-detail-card");
            var title = new Label(action.DisplayName);
            title.AddToClassList("action-detail-title");
            card.Add(title);

            var costs = new Label(
                $"Timeline {action.TimelineCost}    Stamina {action.StaminaCost}"
                + $"    Mana {action.ManaCost}    Cooldown {action.Cooldown}");
            costs.AddToClassList("action-detail-costs");
            card.Add(costs);

            for (int index = 0; index < action.ResolutionEvents.Count; index++)
            {
                ActionResolutionDefinition resolution = action.ResolutionEvents[index];
                string timing = $"T{resolution.Offset}";
                if (action.IsChargeable && resolution.Offset == action.ResolutionOffset)
                {
                    timing = $"T{resolution.Offset}";
                    for (int levelIndex = 0;
                         levelIndex < action.ChargeLevels.Count;
                         levelIndex++)
                    {
                        timing += $" / T{resolution.Offset + action.ChargeLevels[levelIndex].AdditionalTimelineCost}";
                    }

                    timing += " Charge Window";
                }
                else if (resolution.HasResolutionWindow)
                {
                    timing = $"T{resolution.ResolutionWindowStart.Value}–T{resolution.ResolutionWindowEnd.Value} Resolution Window";
                }

                string detail = $"{timing}: {resolution.Effect}";
                if (resolution.DamageEffect != null)
                {
                    var components = resolution.DamageEffect.Components;
                    for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
                    {
                        detail += componentIndex == 0 ? " — " : ", ";
                        detail += $"{components[componentIndex].Type} "
                            + $"{components[componentIndex].ActionMultiplier * 100d:0}%";
                    }
                }

                var resolutionLabel = new Label(detail);
                resolutionLabel.AddToClassList("action-resolution-line");
                card.Add(resolutionLabel);
            }

            _content.Add(card);
        }

        private void AddComboError(string message)
        {
            var error = new Label(message);
            error.AddToClassList("combo-status-invalid");
            _content.Add(error);
        }

        private static string FormatActionSequence(ComboConfiguration configuration)
        {
            IReadOnlyList<ComboActionDefinition> actions = configuration.BuildExecutionOrder();
            string result = string.Empty;
            for (int index = 0; index < actions.Count; index++)
            {
                if (index > 0)
                {
                    result += "  →  ";
                }

                result += actions[index].DisplayName;
            }

            return result;
        }

        private static string FormatCompactTimeline(ComboConfiguration configuration)
        {
            IReadOnlyList<ComboActionDefinition> actions = configuration.BuildExecutionOrder();
            int start = 1;
            string result = string.Empty;
            for (int index = 0; index < actions.Count; index++)
            {
                if (index > 0)
                {
                    result += "  →  ";
                }

                int end = start + actions[index].TimelineCost - 1;
                result += $"T{start}–{end}  {actions[index].DisplayName}";
                start = end + 1;
            }

            return result;
        }

        private static string FormatFirstIssue(ComboValidationResult validation)
        {
            if (validation == null || validation.IsValid || validation.Issues.Count == 0)
            {
                return string.Empty;
            }

            ComboValidationIssue issue = validation.Issues[0];
            return $"{issue.Code} in {issue.ComboName}.";
        }

        private void ShowInventory()
        {
            if (!TryGetContext(out PersistentPlayerState state, out _))
            {
                return;
            }

            _pageTitle.text = ExplorationMenuText.Get("inventory.heading");
            _currentPage = MenuPage.Inventory;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            ClearComboControls();
            _content.Clear();
            AddSectionHeading(ExplorationMenuText.Get("inventory.consumables"));
            AddInventoryRow(state, PlayerStateInitialValues.HealingPotionId, "Healing Potion");
            AddInventoryRow(state, PlayerStateInitialValues.GreaterHealingPotionId,
                "Greater Healing Potion");
            AddInventoryRow(state, PlayerStateInitialValues.VitalityTonicId, "Vitality Tonic");
            AddSectionHeading(ExplorationMenuText.Get("inventory.materials"));
            AddBodyText(ExplorationMenuText.Get("inventory.empty"), "muted-text");
            SetSelectedNavigation(_inventoryButton);
        }

        private void ShowEquipment()
        {
            if (!TryGetContext(out PersistentPlayerState state, out PlayerEquipmentCatalogAsset catalog))
            {
                return;
            }

            _pageTitle.text = ExplorationMenuText.Get("equipment.heading");
            _currentPage = MenuPage.Equipment;
            _editingEquipmentSlot = EquipmentSlot.None;
            ClearComboControls();
            _content.Clear();
            ClearEquipmentControls();

            var layout = new VisualElement();
            layout.AddToClassList("equipment-layout");
            var equippedPanel = new VisualElement();
            equippedPanel.AddToClassList("equipped-slots-panel");
            var equippedTitle = new Label(ExplorationMenuText.Get("equipment.current"));
            equippedTitle.AddToClassList("equipment-pane-title");
            equippedPanel.Add(equippedTitle);

            AddSlotGroupHeading(equippedPanel, ExplorationMenuText.Get("equipment.weapon"));
            AddEquipmentSlotButton(
                equippedPanel,
                EquipmentSlot.Weapon,
                ExplorationMenuText.Get("equipment.weapon"),
                GetEquippedWeaponText(state, catalog));
            AddSlotGroupHeading(equippedPanel, ExplorationMenuText.Get("equipment.armor"));
            AddEquipmentSlotButton(
                equippedPanel,
                EquipmentSlot.Head,
                ExplorationMenuText.Get("equipment.head"),
                GetEquippedArmorText(state, catalog, PlayerStateInitialValues.HeadArmorSlotId));
            AddEquipmentSlotButton(
                equippedPanel,
                EquipmentSlot.UpperBody,
                ExplorationMenuText.Get("equipment.upper"),
                GetEquippedArmorText(state, catalog, PlayerStateInitialValues.UpperBodyArmorSlotId));
            AddEquipmentSlotButton(
                equippedPanel,
                EquipmentSlot.LowerBody,
                ExplorationMenuText.Get("equipment.lower"),
                GetEquippedArmorText(state, catalog, PlayerStateInitialValues.LowerBodyArmorSlotId));
            AddSlotGroupHeading(equippedPanel, ExplorationMenuText.Get("equipment.accessories"));
            AddEquipmentSlotButton(
                equippedPanel,
                EquipmentSlot.Accessory,
                ExplorationMenuText.Get("equipment.accessory"),
                GetEquippedAccessoryText(state, catalog));

            _equipmentWarehouse = new VisualElement();
            _equipmentWarehouse.AddToClassList("warehouse-panel");
            layout.Add(equippedPanel);
            layout.Add(_equipmentWarehouse);
            _content.Add(layout);
            PopulateEquipmentSummary(state, catalog);
            SetSelectedNavigation(_equipmentButton);
        }

        private void AddEquipmentSlotButton(
            VisualElement parent,
            EquipmentSlot slot,
            string slotName,
            string equipmentText)
        {
            var button = new Button(() => BeginEquipmentChange(slot))
            {
                text = $"{slotName}\n{equipmentText}"
            };
            button.AddToClassList("equipment-slot-button");
            parent.Add(button);
            _equipmentSlotButtons.Add(slot, button);
            _equipmentSlotButtonOrder.Add(button);
            RegisterButtonAction(button, () => BeginEquipmentChange(slot));
        }

        private static void AddSlotGroupHeading(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.AddToClassList("slot-group-title");
            parent.Add(label);
        }

        private void BeginEquipmentChange(EquipmentSlot slot)
        {
            if (!TryGetContext(out PersistentPlayerState state, out PlayerEquipmentCatalogAsset catalog))
            {
                return;
            }

            _editingEquipmentSlot = slot;
            _isInsideEquipmentPage = true;
            foreach (KeyValuePair<EquipmentSlot, Button> entry in _equipmentSlotButtons)
            {
                entry.Value.EnableInClassList("equipment-slot-selected", entry.Key == slot);
            }

            PopulateEquipmentCandidates(state, catalog, slot);
        }

        private void PopulateEquipmentCandidates(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog,
            EquipmentSlot slot)
        {
            ClearWarehouseControls();
            _equipmentWarehouse.Clear();
            var title = new Label(
                string.Format(
                    ExplorationMenuText.Get("equipment.choose"),
                    GetSlotName(slot)));
            title.AddToClassList("equipment-pane-title");
            _equipmentWarehouse.Add(title);

            var cancel = new Button(CancelEquipmentChange)
            {
                text = ExplorationMenuText.Get("equipment.cancel")
            };
            cancel.AddToClassList("equipment-cancel-button");
            _equipmentWarehouse.Add(cancel);
            _equipmentWarehouseButtons.Add(cancel);
            RegisterButtonAction(cancel, CancelEquipmentChange);

            int candidateCount = 0;
            if (slot == EquipmentSlot.Weapon)
            {
                foreach (WeaponDefinitionAsset asset in catalog.Weapons)
                {
                    if (asset == null
                        || !state.Equipment.Owns(EquipmentCategory.Weapon, asset.WeaponId))
                    {
                        continue;
                    }

                    bool equipped = state.Equipment.EquippedWeaponId == asset.WeaponId;
                    AddEquipmentCandidate(
                        ExplorationMenuText.EquipmentName(asset.WeaponId, asset.DisplayName),
                        $"ATK {asset.WeaponAttack}",
                        equipped,
                        () => EquipWeapon(state, catalog, asset.WeaponId));
                    candidateCount++;
                }
            }
            else if (slot == EquipmentSlot.Accessory)
            {
                foreach (AccessoryDefinitionAsset asset in catalog.Accessories)
                {
                    if (asset == null
                        || !state.Equipment.Owns(EquipmentCategory.Accessory, asset.AccessoryId))
                    {
                        continue;
                    }

                    bool equipped = state.Equipment.EquippedAccessoryBySlot.TryGetValue(
                        AccessorySlotId,
                        out string equippedId) && equippedId == asset.AccessoryId;
                    AddEquipmentCandidate(
                        ExplorationMenuText.EquipmentName(asset.AccessoryId, asset.DisplayName),
                        FormatAccessoryEffect(asset),
                        equipped,
                        () => EquipAccessory(state, catalog, asset.AccessoryId));
                    candidateCount++;
                }
            }
            else
            {
                string slotId = GetArmorSlotId(slot);
                foreach (ArmorDefinitionAsset asset in catalog.Armor)
                {
                    if (asset == null
                        || asset.SlotId != slotId
                        || !state.Equipment.Owns(EquipmentCategory.Armor, asset.ArmorId))
                    {
                        continue;
                    }

                    bool equipped = state.Equipment.EquippedArmorBySlot.TryGetValue(
                        slotId,
                        out string equippedId) && equippedId == asset.ArmorId;
                    AddEquipmentCandidate(
                        ExplorationMenuText.EquipmentName(asset.ArmorId, asset.DisplayName),
                        $"DEF {asset.Defense}",
                        equipped,
                        () => EquipArmor(state, catalog, slotId, asset.ArmorId));
                    candidateCount++;
                }
            }

            if (candidateCount == 0)
            {
                var empty = new Label(ExplorationMenuText.Get("equipment.none"));
                empty.AddToClassList("muted-text");
                _equipmentWarehouse.Add(empty);
            }

            _root.schedule.Execute(() => cancel.Focus()).ExecuteLater(1);
        }

        private void AddEquipmentCandidate(
            string heading,
            string detail,
            bool equipped,
            Action equip)
        {
            string status = equipped
                ? $"  •  {ExplorationMenuText.Get("equipment.equipped")}"
                : string.Empty;
            var button = new Button(equip)
            {
                text = $"{heading}{status}\n{detail}"
            };
            button.AddToClassList("equipment-candidate-button");
            button.SetEnabled(!equipped);
            _equipmentWarehouse.Add(button);
            if (!equipped)
            {
                _equipmentWarehouseButtons.Add(button);
                RegisterButtonAction(button, equip);
            }
        }

        private void PopulateEquipmentSummary(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog)
        {
            ClearWarehouseControls();
            _equipmentWarehouse.Clear();
            var title = new Label(ExplorationMenuText.Get("equipment.summary"));
            title.AddToClassList("equipment-pane-title");
            _equipmentWarehouse.Add(title);

            string attack = catalog.TryGetWeapon(
                state.Equipment.EquippedWeaponId,
                out var weapon,
                out _) ? weapon.WeaponAttack.ToString() : "—";
            string defense = catalog.TryGetTotalDefense(
                state.Equipment.EquippedArmorBySlot,
                out int totalDefense,
                out _) ? totalDefense.ToString() : "—";
            AddSummaryRow(ExplorationMenuText.Get("equipment.attack"), attack);
            AddSummaryRow(ExplorationMenuText.Get("equipment.defense"), defense);

            var effectsTitle = new Label(ExplorationMenuText.Get("equipment.effects"));
            effectsTitle.AddToClassList("summary-effects-title");
            _equipmentWarehouse.Add(effectsTitle);
            int effectCount = 0;
            foreach (string accessoryId in state.Equipment.EquippedAccessoryBySlot.Values)
            {
                if (!catalog.TryGetAccessory(accessoryId, out AccessoryDefinitionAsset asset, out _))
                {
                    continue;
                }

                var effect = new Label($"• {FormatAccessoryEffect(asset)}");
                effect.AddToClassList("summary-effect");
                _equipmentWarehouse.Add(effect);
                effectCount++;
            }

            if (effectCount == 0)
            {
                var empty = new Label(ExplorationMenuText.Get("equipment.no-effects"));
                empty.AddToClassList("muted-text");
                _equipmentWarehouse.Add(empty);
            }
        }

        private void AddSummaryRow(string name, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("summary-stat-row");
            row.Add(new Label(name));
            var valueLabel = new Label(value);
            valueLabel.AddToClassList("summary-stat-value");
            row.Add(valueLabel);
            _equipmentWarehouse.Add(row);
        }

        private void CancelEquipmentChange()
        {
            EquipmentSlot previousSlot = _editingEquipmentSlot;
            _editingEquipmentSlot = EquipmentSlot.None;
            if (!TryGetContext(out PersistentPlayerState state, out PlayerEquipmentCatalogAsset catalog))
            {
                return;
            }

            foreach (Button button in _equipmentSlotButtons.Values)
            {
                button.RemoveFromClassList("equipment-slot-selected");
            }

            PopulateEquipmentSummary(state, catalog);
            if (_equipmentSlotButtons.TryGetValue(previousSlot, out Button slotButton))
            {
                _root.schedule.Execute(() => slotButton.Focus()).ExecuteLater(1);
            }
        }

        private void EquipWeapon(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog,
            string weaponId)
        {
            state.Equipment.EquipWeapon(weaponId);
            FinishEquipmentChange(state, catalog, EquipmentSlot.Weapon);
        }

        private void EquipArmor(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog,
            string slotId,
            string armorId)
        {
            EquipmentSlot slot = _editingEquipmentSlot;
            state.Equipment.EquipArmor(slotId, armorId);
            FinishEquipmentChange(state, catalog, slot);
        }

        private void EquipAccessory(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog,
            string accessoryId)
        {
            state.Equipment.EquipAccessory(AccessorySlotId, accessoryId);
            FinishEquipmentChange(state, catalog, EquipmentSlot.Accessory);
        }

        private void FinishEquipmentChange(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog,
            EquipmentSlot changedSlot)
        {
            ApplyDerivedStats(state, catalog);
            ShowEquipment();
            if (_equipmentSlotButtons.TryGetValue(changedSlot, out Button slotButton))
            {
                _root.schedule.Execute(() => slotButton.Focus()).ExecuteLater(1);
            }
        }

        private void ShowUnavailableTitleMessage()
        {
            _pageTitle.text = ExplorationMenuText.Get("nav.title");
            _currentPage = MenuPage.ReturnToTitle;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            ClearComboControls();
            _content.Clear();
            AddBodyText(ExplorationMenuText.Get("title.unavailable"), "page-note");
            SetSelectedNavigation(_returnToTitleButton);
        }

        private void ApplyStaticText()
        {
            _root.Q<Label>("menuTitle").text = ExplorationMenuText.Get("menu.title");
            _root.Q<Label>("footerLabel").text = ExplorationMenuText.Get("footer.controls");
            _statusButton.text = ExplorationMenuText.Get("nav.status");
            _comboButton.text = ExplorationMenuText.Get("nav.combos");
            _inventoryButton.text = ExplorationMenuText.Get("nav.inventory");
            _equipmentButton.text = ExplorationMenuText.Get("nav.equipment");
            _returnToTitleButton.text = ExplorationMenuText.Get("nav.title");
        }

        private void SetSelectedNavigation(Button selected)
        {
            _statusButton.EnableInClassList("nav-selected", selected == _statusButton);
            _comboButton.EnableInClassList("nav-selected", selected == _comboButton);
            _inventoryButton.EnableInClassList("nav-selected", selected == _inventoryButton);
            _equipmentButton.EnableInClassList("nav-selected", selected == _equipmentButton);
            _returnToTitleButton.EnableInClassList(
                "nav-selected",
                selected == _returnToTitleButton);
        }

        private void OnStatusClicked() => ShowPageWhenChanged(MenuPage.Status);

        private void OnCombosClicked()
        {
            if (_isOpen && _currentPage == MenuPage.Combos)
            {
                ShowCombos();
                return;
            }

            ShowPageWhenChanged(MenuPage.Combos);
        }

        private void OnInventoryClicked() => ShowPageWhenChanged(MenuPage.Inventory);

        private void OnEquipmentClicked()
        {
            ShowPageWhenChanged(MenuPage.Equipment);
            _isInsideEquipmentPage = true;
            if (_equipmentSlotButtons.TryGetValue(EquipmentSlot.Weapon, out Button weaponButton))
            {
                _root.schedule.Execute(() => weaponButton.Focus()).ExecuteLater(1);
            }
        }

        private void OnReturnToTitleClicked() => ShowPageWhenChanged(MenuPage.ReturnToTitle);

        private void ShowPageWhenChanged(MenuPage page)
        {
            if (!_isOpen || _currentPage == page)
            {
                return;
            }

            switch (page)
            {
                case MenuPage.Status: ShowStatus(); break;
                case MenuPage.Combos: ShowCombos(); break;
                case MenuPage.Inventory: ShowInventory(); break;
                case MenuPage.Equipment: ShowEquipment(); break;
                case MenuPage.ReturnToTitle: ShowUnavailableTitleMessage(); break;
            }
        }

        private void RegisterButtonAction(Button button, Action action)
        {
            if (button != null && action != null)
            {
                _buttonActions[button] = action;
            }
        }

        private void ActivateFocusedButton()
        {
            Button focusedButton = GetFocusedButton();
            if (focusedButton != null
                && focusedButton.enabledInHierarchy
                && _buttonActions.TryGetValue(focusedButton, out Action action))
            {
                action();
            }
        }

        private void OnWheelNavigation(WheelEvent evt)
        {
            if (!_isOpen || Mathf.Abs(evt.delta.y) < 0.01f)
            {
                return;
            }

            MoveFocus(evt.delta.y > 0f ? 1 : -1);
            evt.StopPropagation();
        }

        private void MoveFocus(int direction)
        {
            Button focusedButton = GetFocusedButton();
            List<Button> scope = GetFocusScope(focusedButton);
            if (scope.Count == 0)
            {
                return;
            }

            int currentIndex = focusedButton == null ? -1 : scope.IndexOf(focusedButton);
            int nextIndex;
            if (currentIndex < 0)
            {
                nextIndex = direction > 0 ? 0 : scope.Count - 1;
            }
            else
            {
                nextIndex = Mathf.Clamp(currentIndex + direction, 0, scope.Count - 1);
            }

            scope[nextIndex].Focus();
        }

        private List<Button> GetFocusScope(Button focusedButton)
        {
            if (focusedButton != null && _comboPageButtons.Contains(focusedButton))
            {
                return _comboPageButtons;
            }

            if (focusedButton != null && _equipmentWarehouseButtons.Contains(focusedButton))
            {
                return _equipmentWarehouseButtons;
            }

            if (focusedButton != null && _equipmentSlotButtonOrder.Contains(focusedButton))
            {
                return _equipmentSlotButtonOrder;
            }

            return _sidebarButtons;
        }

        private Button GetFocusedButton()
        {
            return _root?.panel?.focusController?.focusedElement as Button;
        }

        private void ClearEquipmentControls()
        {
            foreach (Button button in _equipmentSlotButtonOrder)
            {
                _buttonActions.Remove(button);
            }

            ClearWarehouseControls();
            _equipmentSlotButtons.Clear();
            _equipmentSlotButtonOrder.Clear();
        }

        private void ClearWarehouseControls()
        {
            foreach (Button button in _equipmentWarehouseButtons)
            {
                _buttonActions.Remove(button);
            }

            _equipmentWarehouseButtons.Clear();
        }

        private void ClearComboControls()
        {
            foreach (Button button in _comboPageButtons)
            {
                _buttonActions.Remove(button);
            }

            _comboPageButtons.Clear();
            if (_currentPage != MenuPage.Combos)
            {
                _comboMenuLevel = ComboMenuLevel.None;
                _selectedLoadout = null;
                _selectedComboIndex = -1;
            }
        }

        private void FocusFirstComboPageButton()
        {
            if (_comboPageButtons.Count > 0)
            {
                _root.schedule.Execute(() => _comboPageButtons[0].Focus()).ExecuteLater(1);
            }
        }

        private void AddValueRow(string key, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("value-row");
            var keyLabel = new Label(key);
            keyLabel.AddToClassList("value-key");
            var valueLabel = new Label(value);
            valueLabel.AddToClassList("value-value");
            row.Add(keyLabel);
            row.Add(valueLabel);
            _content.Add(row);
        }

        private void AddInventoryRow(PersistentPlayerState state, string itemId, string displayName)
        {
            AddValueRow(displayName, state.Inventory.GetCount(itemId).ToString());
        }

        private void AddSectionHeading(string text)
        {
            var heading = new Label(text);
            heading.AddToClassList("section-heading");
            _content.Add(heading);
        }

        private void AddBodyText(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            _content.Add(label);
        }

        private static bool TryGetContext(
            out PersistentPlayerState state,
            out PlayerEquipmentCatalogAsset catalog)
        {
            state = null;
            catalog = PlayerEquipmentCatalogAsset.LoadDefault();
            if (!PlayerStateService.TryGetActive(out PlayerStateService service)
                || service.State == null || catalog == null)
            {
                return false;
            }

            state = service.State;
            return true;
        }

        private static void ApplyDerivedStats(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog)
        {
            if (PlayerEquipmentDerivedStats.TryResolve(
                    state,
                    catalog,
                    out PlayerEquipmentDerivedStats stats,
                    out string error))
            {
                stats.ApplyPersistentMaximumHealth(state);
            }
            else
            {
                Debug.LogWarning($"Could not refresh equipment stats: {error}");
            }
        }

        private static bool CanOpenInCurrentScene()
        {
            if (BattleSessionService.TryGetActive(out BattleSessionService battles)
                && battles.HasActiveSession)
            {
                return false;
            }

            try
            {
                return GameObject.FindGameObjectWithTag("Player") != null;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private static string FormatSlot(string slotId)
        {
            switch (slotId)
            {
                case PlayerStateInitialValues.HeadArmorSlotId: return "Head";
                case PlayerStateInitialValues.UpperBodyArmorSlotId: return "Upper Body";
                case PlayerStateInitialValues.LowerBodyArmorSlotId: return "Lower Body";
                default: return slotId;
            }
        }

        private static string FormatAccessoryEffect(AccessoryDefinitionAsset accessory)
        {
            return accessory.EffectType == AccessoryEffectType.MaximumHealthFlat
                ? $"Maximum HP +{accessory.EffectAmount:0}"
                : $"All Damage +{accessory.EffectAmount * 100f:0}%";
        }

        private static string GetEquippedWeaponText(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog)
        {
            return catalog.TryGetWeapon(state.Equipment.EquippedWeaponId, out var weapon, out _)
                ? $"{ExplorationMenuText.EquipmentName(weapon.WeaponId, weapon.DisplayName)}  |  ATK {weapon.WeaponAttack}"
                : "—";
        }

        private static string GetEquippedArmorText(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog,
            string slotId)
        {
            if (!state.Equipment.EquippedArmorBySlot.TryGetValue(slotId, out string armorId)
                || !catalog.TryGetArmor(armorId, out ArmorDefinitionAsset armor, out _))
            {
                return "—";
            }

            return $"{ExplorationMenuText.EquipmentName(armor.ArmorId, armor.DisplayName)}  |  DEF {armor.Defense}";
        }

        private static string GetEquippedAccessoryText(
            PersistentPlayerState state,
            PlayerEquipmentCatalogAsset catalog)
        {
            if (!state.Equipment.EquippedAccessoryBySlot.TryGetValue(
                    AccessorySlotId,
                    out string accessoryId)
                || !catalog.TryGetAccessory(
                    accessoryId,
                    out AccessoryDefinitionAsset accessory,
                    out _))
            {
                return "—";
            }

            return $"{ExplorationMenuText.EquipmentName(accessory.AccessoryId, accessory.DisplayName)}  |  {FormatAccessoryEffect(accessory)}";
        }

        private static string GetSlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return ExplorationMenuText.Get("equipment.weapon");
                case EquipmentSlot.Head: return ExplorationMenuText.Get("equipment.head");
                case EquipmentSlot.UpperBody: return ExplorationMenuText.Get("equipment.upper");
                case EquipmentSlot.LowerBody: return ExplorationMenuText.Get("equipment.lower");
                case EquipmentSlot.Accessory: return ExplorationMenuText.Get("equipment.accessory");
                default: return string.Empty;
            }
        }

        private static string GetArmorSlotId(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Head: return PlayerStateInitialValues.HeadArmorSlotId;
                case EquipmentSlot.UpperBody: return PlayerStateInitialValues.UpperBodyArmorSlotId;
                case EquipmentSlot.LowerBody: return PlayerStateInitialValues.LowerBodyArmorSlotId;
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void GrantEditorTestEquipment()
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            PlayerEquipmentState equipment = service.State.Equipment;
            equipment.Grant(EquipmentCategory.Weapon, CastIronGreatswordId);
            equipment.Grant(EquipmentCategory.Armor, LeatherHeadpieceId);
            equipment.Grant(EquipmentCategory.Armor, LeatherShirtId);
            equipment.Grant(EquipmentCategory.Armor, LeatherHoseId);
            equipment.Grant(EquipmentCategory.Accessory, RustRingId);
            equipment.Grant(EquipmentCategory.Accessory, RubyRingId);
        }

        private sealed class TimelineResolutionCell : VisualElement
        {
            private readonly Label _marker;
            private Color _stripeColor;
            private bool _hasResolutionWindow;

            public TimelineResolutionCell()
            {
                AddToClassList("timeline-cell");
                _marker = new Label();
                _marker.AddToClassList("timeline-cell-marker");
                Add(_marker);
                generateVisualContent += DrawResolutionWindow;
            }

            public void SetMarkerText(string text)
            {
                _marker.text = text;
            }

            public void SetResolutionWindow(Color stripeColor)
            {
                _stripeColor = stripeColor;
                _hasResolutionWindow = true;
                AddToClassList("timeline-resolution-window");
                MarkDirtyRepaint();
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
        }

        private enum MenuPage
        {
            None,
            Status,
            Combos,
            Inventory,
            Equipment,
            ReturnToTitle
        }

        private enum EquipmentSlot
        {
            None,
            Weapon,
            Head,
            UpperBody,
            LowerBody,
            Accessory
        }

        private enum ComboMenuLevel
        {
            None,
            LoadoutList,
            LoadoutDetails,
            ComboDetails
        }
    }
}
