using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
        private VisualElement _menuFrame;
        private VisualElement _menuNavigation;
        private VisualElement _content;
        private Label _pageTitle;
        private Label _pageHeaderStatus;
        private VisualElement _pageHeaderActions;
        private ScrollView _pageScroll;
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
        private readonly List<Button> _comboEditorButtons = new List<Button>();
        private readonly List<Button> _pageHeaderButtons = new List<Button>();
        private readonly List<Button> _loadoutComboButtons = new List<Button>();
        private readonly List<Button> _assignedActionButtons = new List<Button>();
        private readonly List<Button> _linkedPlacementButtons = new List<Button>();
        private readonly List<Button> _actionLibraryFilterButtons = new List<Button>();
        private readonly List<Button> _actionLibraryCandidateButtons = new List<Button>();
        private readonly List<Button> _chargeLevelButtons = new List<Button>();
        private readonly List<Button> _unsavedDialogButtons = new List<Button>();
        private readonly List<Button> _timelineGuideButtons = new List<Button>();
        private readonly Dictionary<Button, Action> _buttonActions =
            new Dictionary<Button, Action>();
        private readonly Dictionary<string, int> _previewChargeLevels =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private ComboMenuLevel _comboMenuLevel;
        private PlayerComboLoadoutState _selectedLoadout;
        private PlayerComboLoadoutDraft _selectedLoadoutDraft;
        private int _selectedComboIndex = -1;
        private PreparedComboActionSlot? _editingActionSlot;
        private Label _comboEditorMessage;
        private Button _firstActionLibraryButton;
        private Button _selectedActionLibraryFilterButton;
        private ScrollView _timelineScroll;
        private VisualElement _chargeLevelPanel;
        private Button _chargeActionButton;
        private VisualElement _comboActionHeadingActions;
        private VisualElement _renameEditor;
        private VisualElement _unsavedDialog;
        private VisualElement _timelineGuideDialog;
        private CompatibleWeaponTypes _actionLibraryFilter = CompatibleWeaponTypes.All;
        private bool _actionLibraryFilterInitialized;
        private bool _usesNavigationInput;
        private Vector2 _lastMousePosition;
        private bool _hasLastMousePosition;
        private Button _lastNavigationTarget;
        private int _contentRevision;
        private bool _comboSidebarUnlocked;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            _menuFrame = _root.Q<VisualElement>("menuFrame");
            _menuNavigation = _root.Q<VisualElement>("menuNavigation");
            _content = _root.Q<VisualElement>("pageContent");
            _pageTitle = _root.Q<Label>("pageTitle");
            _pageHeaderStatus = _root.Q<Label>("pageHeaderStatus");
            _pageHeaderActions = _root.Q<VisualElement>("pageHeaderActions");
            _pageScroll = _root.Q<ScrollView>("pageScroll");
            _statusButton = _root.Q<Button>("statusButton");
            _comboButton = _root.Q<Button>("comboButton");
            _inventoryButton = _root.Q<Button>("inventoryButton");
            _equipmentButton = _root.Q<Button>("equipmentButton");
            _returnToTitleButton = _root.Q<Button>("returnToTitleButton");

            if (_menuFrame == null || _menuNavigation == null || _content == null
                || _pageTitle == null || _pageHeaderStatus == null
                || _pageHeaderActions == null
                || _pageScroll == null || _statusButton == null
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
            _root.RegisterCallback<FocusInEvent>(OnFocusEntered, TrickleDown.TrickleDown);
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
            _root?.UnregisterCallback<FocusInEvent>(OnFocusEntered, TrickleDown.TrickleDown);
            _buttonActions.Clear();

            if (_isOpen)
            {
                CloseMenu();
            }
        }

        private void Update()
        {
            bool keyboardTogglePressed = Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool gamepadTogglePressed = Gamepad.current != null
                && Gamepad.current.startButton.wasPressedThisFrame;

            if (keyboardTogglePressed || gamepadTogglePressed)
            {
                if (_isOpen) HandleBack();
                else if (!ExplorationMenuInputGate.IsOpen)
                {
                    TryOpenMenu(gamepadTogglePressed);
                }
                return;
            }

            if (_isOpen)
            {
                UpdateInputMode();
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
                HandleControllerScroll();
            }
        }

        private void TryOpenMenu(bool openedWithNavigationInput)
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
            _selectedLoadoutDraft = null;
            _selectedComboIndex = -1;
            _editingActionSlot = null;
            _previewChargeLevels.Clear();
            _actionLibraryFilterInitialized = false;
            _lastNavigationTarget = null;
            RecordCurrentMousePosition();
            SetNavigationInputMode(openedWithNavigationInput);

            GrantEditorTestEquipment();
            GrantEditorComboPreviewLoadouts();
            ShowStatus();
            if (_usesNavigationInput)
            {
                _root.schedule.Execute(() => FocusNavigationTarget(_statusButton))
                    .ExecuteLater(1);
            }
        }

        private void CloseMenu()
        {
            _contentRevision++;
            CloseUnsavedChangesDialog();
            CloseTimelineGuideDialog();
            ReleaseMenuFocus();
            _root.style.display = DisplayStyle.None;
            _isOpen = false;
            _isInsideEquipmentPage = false;
            ExplorationMenuInputGate.IsOpen = false;
            Time.timeScale = _previousTimeScale;
            UnityEngine.Cursor.lockState = _previousCursorLockMode;
            UnityEngine.Cursor.visible = _previousCursorVisible;
            _hasLastMousePosition = false;
            _lastNavigationTarget = null;
        }

        private void ReleaseMenuFocus()
        {
            // A hidden UI Toolkit TextField can otherwise retain keyboard focus.
            // On Windows, pressing Shift during exploration may then be handled by
            // the active IME instead of behaving like a gameplay-only input.
            _root?.panel?.focusController?.focusedElement?.Blur();
        }

        private void UpdateInputMode()
        {
            bool usedPointer = false;
            if (Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                if (_hasLastMousePosition)
                {
                    usedPointer = (mousePosition - _lastMousePosition).sqrMagnitude >= 16f;
                }

                _lastMousePosition = mousePosition;
                _hasLastMousePosition = true;
                usedPointer |= Mouse.current.leftButton.wasPressedThisFrame
                    || Mouse.current.middleButton.wasPressedThisFrame
                    || Mouse.current.rightButton.wasPressedThisFrame
                    || Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.01f;
            }

            bool editingText = _root?.panel?.focusController?.focusedElement is TextField;
            bool usedNavigation = !editingText
                && (UsedKeyboardNavigationThisFrame()
                    || UsedGamepadNavigationThisFrame());
            if (usedPointer)
            {
                SetNavigationInputMode(false);
            }
            else if (usedNavigation)
            {
                SetNavigationInputMode(true);
            }

            if (!_usesNavigationInput)
            {
                BlurFocusedButton();
            }
        }

        private static bool UsedKeyboardNavigationThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                && (keyboard.upArrowKey.wasPressedThisFrame
                    || keyboard.downArrowKey.wasPressedThisFrame
                    || keyboard.leftArrowKey.wasPressedThisFrame
                    || keyboard.rightArrowKey.wasPressedThisFrame
                    || keyboard.wKey.wasPressedThisFrame
                    || keyboard.aKey.wasPressedThisFrame
                    || keyboard.sKey.wasPressedThisFrame
                    || keyboard.dKey.wasPressedThisFrame
                    || keyboard.fKey.wasPressedThisFrame
                    || keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame
                    || keyboard.tabKey.wasPressedThisFrame
                    || keyboard.qKey.wasPressedThisFrame
                    || keyboard.eKey.wasPressedThisFrame
                    || keyboard.xKey.wasPressedThisFrame
                    || keyboard.yKey.wasPressedThisFrame);
        }

        private static bool UsedGamepadNavigationThisFrame()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return false;
            }

            return gamepad.dpad.up.wasPressedThisFrame
                || gamepad.dpad.down.wasPressedThisFrame
                || gamepad.dpad.left.wasPressedThisFrame
                || gamepad.dpad.right.wasPressedThisFrame
                || gamepad.buttonSouth.wasPressedThisFrame
                || gamepad.buttonEast.wasPressedThisFrame
                || gamepad.buttonWest.wasPressedThisFrame
                || gamepad.buttonNorth.wasPressedThisFrame
                || gamepad.leftShoulder.wasPressedThisFrame
                || gamepad.rightShoulder.wasPressedThisFrame
                || gamepad.leftStick.ReadValue().sqrMagnitude >= 0.36f;
        }

        private void SetNavigationInputMode(bool useNavigationInput)
        {
            if (_usesNavigationInput == useNavigationInput && _isOpen)
            {
                UnityEngine.Cursor.visible = !useNavigationInput;
                if (useNavigationInput && GetFocusedButton() == null)
                {
                    FocusNavigationTarget(DefaultNavigationTarget());
                }

                return;
            }

            _usesNavigationInput = useNavigationInput;
            ApplyButtonPickingMode();
            UnityEngine.Cursor.visible = !useNavigationInput;
            if (useNavigationInput)
            {
                FocusNavigationTarget(DefaultNavigationTarget());
            }
            else
            {
                BlurFocusedButton();
            }
        }

        private void ApplyButtonPickingMode()
        {
            if (_root == null)
            {
                return;
            }

            PickingMode pickingMode = _usesNavigationInput
                ? PickingMode.Ignore
                : PickingMode.Position;
            _root.Query<Button>().ForEach(button => button.pickingMode = pickingMode);
        }

        private void BlurFocusedButton()
        {
            if (GetFocusedButton() is Button focusedButton)
            {
                if (focusedButton.panel != null && focusedButton.enabledInHierarchy)
                {
                    _lastNavigationTarget = focusedButton;
                }

                focusedButton.Blur();
            }
        }

        private void FocusNavigationTarget(Button preferred)
        {
            Button target = preferred;
            if (target == null
                || target.panel == null
                || !target.enabledInHierarchy
                || !target.focusable)
            {
                target = IsNestedComboPage()
                    ? FirstNestedComboNavigationTarget()
                    : SelectedSidebarButton();
            }

            target?.Focus();
        }

        private void OnFocusEntered(FocusInEvent evt)
        {
            if (!(evt.target is Button focusedButton)
                || !_buttonActions.ContainsKey(focusedButton))
            {
                return;
            }

            int capturedRevision = _contentRevision;
            VisualElement focusedElement = focusedButton;
            VisualElement current = focusedElement.parent;
            while (current != null)
            {
                if (current is ScrollView scrollView)
                {
                    ScrollView capturedScroll = scrollView;
                    VisualElement capturedElement = focusedElement;
                    _root.schedule.Execute(() =>
                    {
                        if (!_isOpen
                            || capturedRevision != _contentRevision
                            || capturedScroll.panel == null
                            || capturedElement.panel == null
                            || capturedScroll.panel != capturedElement.panel
                            || !capturedScroll.Contains(capturedElement))
                        {
                            return;
                        }

                        capturedScroll.ScrollTo(capturedElement);
                    }).ExecuteLater(1);
                }

                current = current.parent;
            }
        }

        private void HandleControllerScroll()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return;
            }

            float vertical = gamepad.rightStick.ReadValue().y;
            if (Mathf.Abs(vertical) < 0.2f)
            {
                return;
            }

            ScrollView scrollView = FindFocusedScrollView() ?? _pageScroll;
            if (scrollView == null)
            {
                return;
            }

            Vector2 offset = scrollView.scrollOffset;
            offset.y -= vertical * 720f * Time.unscaledDeltaTime;
            scrollView.scrollOffset = offset;
        }

        private ScrollView FindFocusedScrollView()
        {
            VisualElement current = _root?.panel?.focusController?.focusedElement
                as VisualElement;
            while (current != null)
            {
                if (current is ScrollView scrollView)
                {
                    return scrollView;
                }

                current = current.parent;
            }

            return null;
        }

        private Button SelectedSidebarButton()
        {
            switch (_currentPage)
            {
                case MenuPage.Combos: return _comboButton;
                case MenuPage.Inventory: return _inventoryButton;
                case MenuPage.Equipment: return _equipmentButton;
                case MenuPage.ReturnToTitle: return _returnToTitleButton;
                default: return _statusButton;
            }
        }

        private void RecordCurrentMousePosition()
        {
            if (Mouse.current == null)
            {
                _hasLastMousePosition = false;
                return;
            }

            _lastMousePosition = Mouse.current.position.ReadValue();
            _hasLastMousePosition = true;
        }

        private void HandleBack()
        {
            if (_timelineGuideDialog != null)
            {
                CloseTimelineGuideDialog();
                return;
            }

            if (_unsavedDialog != null)
            {
                CloseUnsavedChangesDialog();
                return;
            }

            if (_renameEditor != null)
            {
                RefreshCurrentComboPage();
                return;
            }

            if (_currentPage == MenuPage.Combos)
            {
                if (_comboMenuLevel == ComboMenuLevel.ComboDetails
                    && _chargeLevelPanel != null
                    && !_chargeLevelPanel.ClassListContains("charge-level-panel-hidden"))
                {
                    HideChargeLevelChoices();
                    return;
                }

                if (_comboMenuLevel == ComboMenuLevel.ComboDetails && _selectedLoadout != null)
                {
                    if (_editingActionSlot.HasValue)
                    {
                        ExitActionReplacement();
                        return;
                    }

                    TryReturnToLoadoutDetails();
                    return;
                }

                if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails)
                {
                    TryLeaveLoadoutEditor();
                    return;
                }

                if (_comboMenuLevel == ComboMenuLevel.LoadoutList
                    && _usesNavigationInput
                    && !_sidebarButtons.Contains(GetFocusedButton()))
                {
                    _comboSidebarUnlocked = true;
                    SetSidebarCollapsed(false);
                    FocusNavigationTarget(_comboButton);
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
            if (_root?.panel?.focusController?.focusedElement is TextField)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                ActivateFocusedButton();
                return;
            }

            if (_unsavedDialog != null)
            {
                HandleUnsavedDialogDirectionalInput();
                return;
            }

            bool previousActionFilter = _editingActionSlot.HasValue
                && ((Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                    || (Gamepad.current != null
                        && Gamepad.current.leftShoulder.wasPressedThisFrame));
            bool nextActionFilter = _editingActionSlot.HasValue
                && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    || (Gamepad.current != null
                        && Gamepad.current.rightShoulder.wasPressedThisFrame));
            if (previousActionFilter || nextActionFilter)
            {
                CycleActionLibraryFilter(nextActionFilter ? 1 : -1);
                return;
            }


            bool shortcutAllowed = _timelineGuideDialog == null
                && _unsavedDialog == null
                && _renameEditor == null;
            if (shortcutAllowed && !_editingActionSlot.HasValue)
            {
                bool shortcutX = (Keyboard.current != null
                        && Keyboard.current.xKey.wasPressedThisFrame)
                    || (Gamepad.current != null
                        && Gamepad.current.buttonWest.wasPressedThisFrame);
                bool shortcutY = (Keyboard.current != null
                        && Keyboard.current.yKey.wasPressedThisFrame)
                    || (Gamepad.current != null
                        && Gamepad.current.buttonNorth.wasPressedThisFrame);
                if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails
                    && shortcutX)
                {
                    if (PlayerStateService.TryGetActive(out PlayerStateService service)
                        && _selectedLoadout != null
                        && _selectedLoadout.LoadoutId
                            != service.State.ComboLoadouts.ActiveLoadoutId)
                    {
                        ActivateSelectedLoadout();
                    }
                    return;
                }

                if (_comboMenuLevel == ComboMenuLevel.ComboDetails)
                {
                    bool hasComboChanges = _selectedLoadoutDraft != null
                        && _selectedComboIndex >= 0
                        && _selectedLoadoutDraft.HasComboChanges(_selectedComboIndex);
                    if (shortcutX)
                    {
                        if (hasComboChanges)
                        {
                            ApplyCurrentComboAndRefresh();
                        }
                        return;
                    }

                    if (shortcutY)
                    {
                        if (hasComboChanges)
                        {
                            RevertCurrentComboAndRefresh();
                        }
                        return;
                    }
                }
            }

            bool moveLeft = Keyboard.current != null
                && (Keyboard.current.leftArrowKey.wasPressedThisFrame
                    || Keyboard.current.aKey.wasPressedThisFrame);
            bool moveRight = Keyboard.current != null
                && (Keyboard.current.rightArrowKey.wasPressedThisFrame
                    || Keyboard.current.dKey.wasPressedThisFrame);
            moveLeft |= Gamepad.current != null
                && Gamepad.current.dpad.left.wasPressedThisFrame;
            moveRight |= Gamepad.current != null
                && Gamepad.current.dpad.right.wasPressedThisFrame;
            if (moveLeft || moveRight)
            {
                Button focused = GetFocusedButton();
                if (TryMoveComboFocusHorizontally(focused, moveRight ? 1 : -1))
                {
                    return;
                }

                if (focused != null && _chargeLevelButtons.Contains(focused))
                {
                    MoveFocus(moveRight ? 1 : -1);
                }
                else if (_comboMenuLevel == ComboMenuLevel.ComboDetails)
                {
                    PanTimeline(moveRight ? 1 : -1);
                }

                return;
            }

            if (Gamepad.current == null)
            {
                return;
            }

            if (Gamepad.current.dpad.up.wasPressedThisFrame)
            {
                if (!TryMoveComboFocusVertically(GetFocusedButton(), -1))
                {
                    MoveFocus(-1);
                }
            }
            else if (Gamepad.current.dpad.down.wasPressedThisFrame)
            {
                if (!TryMoveComboFocusVertically(GetFocusedButton(), 1))
                {
                    MoveFocus(1);
                }
            }
        }

        private void HandleUnsavedDialogDirectionalInput()
        {
            bool movePrevious = Keyboard.current != null
                && (Keyboard.current.leftArrowKey.wasPressedThisFrame
                    || Keyboard.current.upArrowKey.wasPressedThisFrame
                    || Keyboard.current.aKey.wasPressedThisFrame
                    || Keyboard.current.wKey.wasPressedThisFrame);
            bool moveNext = Keyboard.current != null
                && (Keyboard.current.rightArrowKey.wasPressedThisFrame
                    || Keyboard.current.downArrowKey.wasPressedThisFrame
                    || Keyboard.current.dKey.wasPressedThisFrame
                    || Keyboard.current.sKey.wasPressedThisFrame);

            if (Gamepad.current != null)
            {
                movePrevious |= Gamepad.current.dpad.left.wasPressedThisFrame
                    || Gamepad.current.dpad.up.wasPressedThisFrame;
                moveNext |= Gamepad.current.dpad.right.wasPressedThisFrame
                    || Gamepad.current.dpad.down.wasPressedThisFrame;
            }

            if (movePrevious != moveNext)
            {
                MoveFocus(moveNext ? 1 : -1);
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
            ConfigurePageHeader(ExplorationMenuText.Get("status.heading"));
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

            _currentPage = MenuPage.Combos;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            _comboMenuLevel = ComboMenuLevel.LoadoutList;
            _comboSidebarUnlocked = false;
            _selectedLoadout = null;
            _selectedLoadoutDraft = null;
            _selectedComboIndex = -1;
            _editingActionSlot = null;
            ClearComboControls();
            ConfigurePageHeader(ExplorationMenuText.Get("combos.heading"));
            _content.Clear();
            AddBodyText(ExplorationMenuText.Get("combos.note"), "page-note");
            AddBodyText(
                ExplorationMenuText.Get("combos.compact-timeline-legend"),
                "compact-timeline-legend");
            ComboActionCatalogAsset actionCatalog = ComboActionCatalogAsset.LoadDefault();
            var orderedLoadouts = new List<PlayerComboLoadoutState>();
            PlayerComboLoadoutState activeLoadout = state.ComboLoadouts.ActiveLoadout;
            if (activeLoadout != null)
            {
                orderedLoadouts.Add(activeLoadout);
            }

            foreach (PlayerComboLoadoutState loadout in state.ComboLoadouts.Loadouts)
            {
                if (activeLoadout == null
                    || !string.Equals(
                        loadout.LoadoutId,
                        activeLoadout.LoadoutId,
                        StringComparison.Ordinal))
                {
                    orderedLoadouts.Add(loadout);
                }
            }

            foreach (PlayerComboLoadoutState loadout in orderedLoadouts)
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
                        AddCompactTimelinePreview(previewRow, configurations[index]);
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
            if (_selectedLoadoutDraft == null
                || !string.Equals(
                    _selectedLoadoutDraft.LoadoutId,
                    loadout.LoadoutId,
                    StringComparison.Ordinal))
            {
                _selectedLoadoutDraft = new PlayerComboLoadoutDraft(loadout);
            }
            _selectedComboIndex = -1;
            _editingActionSlot = null;
            ClearComboControls();
            ConfigurePageHeader(_selectedLoadoutDraft.Name);
            _content.Clear();

            bool isActive = loadout.LoadoutId == state.ComboLoadouts.ActiveLoadoutId;
            if (isActive)
            {
                SetPageHeaderStatus(ExplorationMenuText.Get("combos.active"));
            }

            AddPageHeaderButton(
                ExplorationMenuText.Get("combos.rename-loadout"),
                ShowLoadoutRenameEditor,
                "page-header-rename-button");
            AddPageHeaderButton(
                ExplorationMenuText.Get("combos.back-loadouts"),
                TryLeaveLoadoutEditor,
                "page-header-back-button");

            ComboActionCatalogAsset actionCatalog = ComboActionCatalogAsset.LoadDefault();
            bool built = TryBuildDraftLoadout(
                _selectedLoadoutDraft,
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
                PreparedComboDraft prepared = _selectedLoadoutDraft.Combos[index];
                ComboConfiguration configuration = built ? configurations[index] : null;
                var button = new Button(() => ShowComboDetails(loadout, capturedIndex));
                button.AddToClassList("combo-card-button");
                RegisterButtonAction(button, () => ShowComboDetails(loadout, capturedIndex));
                _comboPageButtons.Add(button);
                _loadoutComboButtons.Add(button);

                var heading = new Label($"{index + 1}. {prepared.Name}");
                heading.AddToClassList("combo-card-title");
                button.Add(heading);

                if (configuration != null)
                {
                    var sequence = new Label(
                        ExplorationMenuActionTextFormatter.FormatActionSequence(configuration));
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
                    : $"{ExplorationMenuText.Get("combos.invalid")} "
                        + ExplorationMenuActionTextFormatter.FormatFirstIssue(validation));
                validationLabel.AddToClassList(
                    validation.IsValid ? "combo-status-valid" : "combo-status-invalid");
                _content.Add(validationLabel);
            }

            AddLoadoutActivationControl(built, configurations, state);

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

            bool changedCombo = _comboMenuLevel != ComboMenuLevel.ComboDetails
                || _selectedLoadout != loadout
                || _selectedComboIndex != comboIndex;
            if (changedCombo)
            {
                _previewChargeLevels.Clear();
                _actionLibraryFilterInitialized = false;
            }

            _currentPage = MenuPage.Combos;
            _comboMenuLevel = ComboMenuLevel.ComboDetails;
            _selectedLoadout = loadout;
            _selectedComboIndex = comboIndex;
            ClearComboControls();
            _content.Clear();

            if (_selectedLoadoutDraft == null
                || !string.Equals(
                    _selectedLoadoutDraft.LoadoutId,
                    loadout.LoadoutId,
                    StringComparison.Ordinal))
            {
                _selectedLoadoutDraft = new PlayerComboLoadoutDraft(loadout);
            }

            PreparedComboDraft draftCombo = _selectedLoadoutDraft.Combos[comboIndex];
            PreparedComboState prepared = draftCombo.CreatePreparedState();
            ConfigurePageHeader(draftCombo.Name);
            AddPageHeaderButton(
                ExplorationMenuText.Get("combos.timeline-guide"),
                ShowTimelineGuideDialog,
                "page-header-guide-button");
            AddPageHeaderButton(
                ExplorationMenuText.Get("combos.rename-combo"),
                ShowComboRenameEditor,
                "page-header-rename-button");
            AddPageHeaderButton(
                ExplorationMenuText.Get("combos.back-combos"),
                TryReturnToLoadoutDetails,
                "page-header-back-button");

            ComboActionCatalogAsset actionCatalog = ComboActionCatalogAsset.LoadDefault();
            if (!PlayerStateComboConfigurationBuilder.TryBuildCombo(
                    prepared,
                    state,
                    actionCatalog,
                    out ComboConfiguration configuration,
                    out string validationError))
            {
                AddComboError(validationError);
                AddDraftEditorControls(state);
                SetSelectedNavigation(_comboButton);
                FocusFirstComboPageButton();
                return;
            }

            AddTimelinePreview(configuration);
            AddChargeLevelPanel();
            AddComboActionEditor(draftCombo, configuration, state);
            AddDraftEditorControls(state);
            SetSelectedNavigation(_comboButton);
            FocusFirstComboPageButton();
        }

        private void AddLoadoutActivationControl(
            bool built,
            IReadOnlyList<ComboConfiguration> configurations,
            PersistentPlayerState state)
        {
            ComboValidationResult validation = built
                ? ComboLoadoutValidator.Validate(
                    configurations,
                    new ComboValidationLimits(
                        PlayerStateLoadoutSessionBuilder.MaximumTimelineCost,
                        state.MaximumMana,
                        state.MaximumCombatStamina))
                : null;
            bool isValid = validation != null && validation.IsValid;

            var controls = new VisualElement();
            controls.AddToClassList("combo-editor-controls");

            if (_selectedLoadout.LoadoutId != state.ComboLoadouts.ActiveLoadoutId)
            {
                var activate = new Button(ActivateSelectedLoadout)
                {
                    text = ExplorationMenuText.Get("combos.activate")
                };
                activate.AddToClassList("combo-editor-control-button");
                activate.focusable = false;
                activate.SetEnabled(isValid);
                controls.Add(activate);
                _comboEditorButtons.Add(activate);
                RegisterButtonAction(activate, ActivateSelectedLoadout);
            }

            if (controls.childCount > 0)
            {
                _content.Add(controls);
            }
        }

        private void AddDraftEditorControls(PersistentPlayerState state)
        {
            bool built = TryBuildDraftLoadout(
                _selectedLoadoutDraft,
                state,
                ComboActionCatalogAsset.LoadDefault(),
                out IReadOnlyList<ComboConfiguration> configurations,
                out _);
            ComboValidationResult validation = built
                ? ComboLoadoutValidator.Validate(
                    configurations,
                    new ComboValidationLimits(
                        PlayerStateLoadoutSessionBuilder.MaximumTimelineCost,
                        state.MaximumMana,
                        state.MaximumCombatStamina))
                : null;
            bool isValid = validation != null && validation.IsValid;

            bool hasComboChanges = _selectedLoadoutDraft.HasComboChanges(_selectedComboIndex);
            EnsureComboActionHeading();
            _comboActionHeadingActions.Clear();

            var status = new Label(ExplorationMenuText.Get(
                hasComboChanges ? "combos.unsaved-status" : "combos.saved-status"));
            status.AddToClassList("combo-action-heading-status");
            status.EnableInClassList("combo-action-heading-status-dirty", hasComboChanges);
            _comboActionHeadingActions.Add(status);

            var apply = new Button(ApplyCurrentComboAndRefresh)
            {
                text = ExplorationMenuText.Get("combos.apply")
            };
            apply.AddToClassList("combo-action-heading-button");
            apply.focusable = false;
            apply.SetEnabled(isValid && hasComboChanges);
            _comboActionHeadingActions.Add(apply);
            _comboEditorButtons.Add(apply);
            RegisterButtonAction(apply, ApplyCurrentComboAndRefresh);

            var revert = new Button(RevertCurrentComboAndRefresh)
            {
                text = ExplorationMenuText.Get("combos.revert")
            };
            revert.AddToClassList("combo-action-heading-button");
            revert.focusable = false;
            revert.SetEnabled(hasComboChanges);
            _comboActionHeadingActions.Add(revert);
            _comboEditorButtons.Add(revert);
            RegisterButtonAction(revert, RevertCurrentComboAndRefresh);
        }

        private void AddComboActionEditor(
            PreparedComboDraft draftCombo,
            ComboConfiguration configuration,
            PersistentPlayerState state)
        {
            EnsureComboActionHeading();
            var layout = new VisualElement();
            layout.AddToClassList("combo-action-editor-layout");

            var assigned = new VisualElement();
            assigned.AddToClassList("combo-assigned-actions");
            var assignedHeading = new Label(ExplorationMenuText.Get("combos.assigned-actions"));
            assignedHeading.AddToClassList("combo-editor-pane-title");
            assigned.Add(assignedHeading);

            AddAssignedActionButton(assigned, draftCombo, PreparedComboActionSlot.Opening);
            AddAssignedActionButton(assigned, draftCombo, PreparedComboActionSlot.Main);
            AddAssignedActionButton(assigned, draftCombo, PreparedComboActionSlot.Closing);
            AddAssignedActionButton(assigned, draftCombo, PreparedComboActionSlot.Linked);
            AddLinkedPlacementControls(assigned, draftCombo);

            var library = new VisualElement();
            library.AddToClassList("combo-action-library");
            var libraryScroll = new ScrollView(ScrollViewMode.Vertical);
            libraryScroll.AddToClassList("combo-action-library-scroll");
            layout.Add(assigned);
            layout.Add(library);
            _content.Add(layout);

            if (_editingActionSlot.HasValue)
            {
                PopulateActionLibrary(
                    library,
                    libraryScroll,
                    draftCombo,
                    state,
                    _editingActionSlot.Value);
            }
            else
            {
                var heading = new Label(ExplorationMenuText.Get("combos.action-details"));
                heading.AddToClassList("combo-editor-pane-title");
                library.Add(heading);
                library.Add(libraryScroll);
                IReadOnlyList<ComboActionDefinition> executionOrder =
                    configuration.BuildExecutionOrder();
                for (int index = 0; index < executionOrder.Count; index++)
                {
                    ComboActionDefinition action = executionOrder[index];
                    AddActionDetail(libraryScroll, action, GetPreviewChargeLevel(action));
                }
            }

            _comboEditorMessage = new Label();
            _comboEditorMessage.AddToClassList("combo-editor-message");
            _content.Add(_comboEditorMessage);
        }

        private void AddAssignedActionButton(
            VisualElement parent,
            PreparedComboDraft draftCombo,
            PreparedComboActionSlot slot)
        {
            string actionId = draftCombo.GetActionId(slot);
            string displayName = ExplorationMenuText.Get("combos.empty-slot");
            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            if (actionId != null
                && catalog != null
                && catalog.TryCreateDefinition(actionId, out ComboActionDefinition action, out _))
            {
                displayName = action.DisplayName;
            }

            var button = new Button(() => BeginActionReplacement(slot))
            {
                text = $"{ExplorationMenuActionTextFormatter.FormatActionSlot(slot)}\n{displayName}"
            };
            button.AddToClassList("combo-assigned-action-button");
            button.EnableInClassList("combo-assigned-action-selected", _editingActionSlot == slot);
            parent.Add(button);
            _comboEditorButtons.Add(button);
            _comboPageButtons.Add(button);
            _assignedActionButtons.Add(button);
            RegisterButtonAction(button, () => BeginActionReplacement(slot));
        }

        private void AddLinkedPlacementControls(
            VisualElement parent,
            PreparedComboDraft draftCombo)
        {
            if (draftCombo.LinkedActionId == null)
            {
                return;
            }

            var label = new Label(ExplorationMenuText.Get("combos.linked-placement"));
            label.AddToClassList("combo-linked-placement-label");
            parent.Add(label);
            var row = new VisualElement();
            row.AddToClassList("combo-linked-placement-row");
            AddLinkedPlacementButton(
                row,
                draftCombo,
                PreparedLinkedActionPlacement.AfterOpening,
                draftCombo.OpeningActionId != null);
            AddLinkedPlacementButton(
                row,
                draftCombo,
                PreparedLinkedActionPlacement.AfterMain,
                draftCombo.MainActionId != null);
            AddLinkedPlacementButton(
                row,
                draftCombo,
                PreparedLinkedActionPlacement.AfterClosing,
                draftCombo.ClosingActionId != null);
            parent.Add(row);
        }

        private void AddLinkedPlacementButton(
            VisualElement parent,
            PreparedComboDraft draftCombo,
            PreparedLinkedActionPlacement placement,
            bool enabled)
        {
            var button = new Button(() => SetLinkedPlacement(draftCombo, placement))
            {
                text = ExplorationMenuActionTextFormatter.ShortPlacementName(placement)
            };
            button.AddToClassList("combo-linked-placement-button");
            button.EnableInClassList(
                "combo-linked-placement-selected",
                draftCombo.LinkedPlacement == placement);
            button.SetEnabled(enabled);
            parent.Add(button);
            _comboEditorButtons.Add(button);
            _linkedPlacementButtons.Add(button);
            RegisterButtonAction(button, () => SetLinkedPlacement(draftCombo, placement));
        }

        private void PopulateActionLibrary(
            VisualElement library,
            VisualElement libraryBody,
            PreparedComboDraft draftCombo,
            PersistentPlayerState state,
            PreparedComboActionSlot slot)
        {
            var heading = new Label(
                $"{ExplorationMenuText.Get("combos.action-library")} — "
                    + ExplorationMenuActionTextFormatter.FormatActionSlot(slot));
            heading.AddToClassList("combo-editor-pane-title");
            library.Add(heading);

            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            string catalogError = null;
            if (catalog == null
                || !catalog.TryCreateDefinitions(
                    state.ActionLibrary.OwnedActionIds,
                    out IReadOnlyList<ComboActionDefinition> definitions,
                    out catalogError))
            {
                var error = new Label(catalogError ?? "The Action Catalog is unavailable.");
                error.AddToClassList("combo-status-invalid");
                libraryBody.Add(error);
                return;
            }

            ComboActionCategory category = ToActionCategory(slot);
            AddActionLibraryFilters(library, definitions, category, state);
            library.Add(libraryBody);

            Button clearButton = null;
            if (slot != PreparedComboActionSlot.Main)
            {
                clearButton = new Button(() => ReplaceDraftAction(slot, null))
                {
                    text = ExplorationMenuText.Get("combos.clear-slot")
                };
                clearButton.AddToClassList("combo-action-candidate-button");
            }

            bool found = false;
            for (int index = 0; index < definitions.Count; index++)
            {
                ComboActionDefinition definition = definitions[index];
                if (definition.Category != category
                    || !MatchesActionLibraryFilter(definition, _actionLibraryFilter))
                {
                    continue;
                }

                found = true;
                bool usedElsewhere = IsActionAssignedElsewhere(
                    definition.ActionId,
                    _selectedComboIndex,
                    slot);
                string current = string.Equals(
                    draftCombo.GetActionId(slot),
                    definition.ActionId,
                    StringComparison.Ordinal)
                    ? $"  • {ExplorationMenuText.Get("combos.current")}" : string.Empty;
                var button = new Button(
                    () => ReplaceDraftAction(slot, definition.ActionId));
                button.AddToClassList("combo-action-candidate-button");
                button.SetEnabled(!usedElsewhere);

                var name = new Label($"{definition.DisplayName}{current}");
                name.AddToClassList("combo-action-candidate-name");
                button.Add(name);

                var costs = new Label(
                    $"Timeline {definition.TimelineCost}  |  Stamina {definition.StaminaCost}"
                    + $"  |  Mana {definition.ManaCost}");
                costs.AddToClassList("combo-action-candidate-costs");
                button.Add(costs);

                var effects = new Label(
                    ExplorationMenuActionTextFormatter.FormatActionLibrarySummary(definition));
                effects.AddToClassList("combo-action-candidate-effects");
                button.Add(effects);

                if (usedElsewhere)
                {
                    var unavailable = new Label(
                        ExplorationMenuText.Get("combos.used-elsewhere"));
                    unavailable.AddToClassList("combo-action-candidate-warning");
                    button.Add(unavailable);
                }

                libraryBody.Add(button);
                if (!usedElsewhere)
                {
                    _firstActionLibraryButton ??= button;
                }
                _comboEditorButtons.Add(button);
                _actionLibraryCandidateButtons.Add(button);
                RegisterButtonAction(
                    button,
                    () => ReplaceDraftAction(slot, definition.ActionId));
            }

            if (!found)
            {
                var empty = new Label(ExplorationMenuText.Get("combos.no-owned-actions"));
                empty.AddToClassList("muted-text");
                libraryBody.Add(empty);
            }

            if (clearButton != null)
            {
                libraryBody.Add(clearButton);
                _comboEditorButtons.Add(clearButton);
                _actionLibraryCandidateButtons.Add(clearButton);
                RegisterButtonAction(
                    clearButton,
                    () => ReplaceDraftAction(slot, null));
            }

            if (_firstActionLibraryButton == null
                && _actionLibraryCandidateButtons.Count > 0)
            {
                _firstActionLibraryButton = _actionLibraryCandidateButtons[0];
            }
        }

        private void EnsureComboActionHeading()
        {
            if (_comboActionHeadingActions != null)
            {
                return;
            }

            var row = new VisualElement();
            row.AddToClassList("combo-action-heading-row");

            var heading = new Label(ExplorationMenuText.Get("combos.configure-actions"));
            heading.AddToClassList("section-heading");
            heading.AddToClassList("combo-action-heading-title");
            row.Add(heading);

            var spacer = new VisualElement();
            spacer.AddToClassList("combo-action-heading-spacer");
            row.Add(spacer);

            _comboActionHeadingActions = new VisualElement();
            _comboActionHeadingActions.AddToClassList("combo-action-heading-actions");
            row.Add(_comboActionHeadingActions);
            _content.Add(row);
        }

        private void AddActionLibraryFilters(
            VisualElement parent,
            IReadOnlyList<ComboActionDefinition> definitions,
            ComboActionCategory category,
            PersistentPlayerState state)
        {
            var available = new List<CompatibleWeaponTypes>();
            AddAvailableActionFilter(
                available,
                definitions,
                category,
                CompatibleWeaponTypes.All);
            AddAvailableActionFilter(
                available,
                definitions,
                category,
                CompatibleWeaponTypes.Greatsword);
            AddAvailableActionFilter(
                available,
                definitions,
                category,
                CompatibleWeaponTypes.SwordAndShield);
            AddAvailableActionFilter(
                available,
                definitions,
                category,
                CompatibleWeaponTypes.GlaiveAndShield);
            AddAvailableActionFilter(
                available,
                definitions,
                category,
                CompatibleWeaponTypes.Bow);
            AddAvailableActionFilter(
                available,
                definitions,
                category,
                CompatibleWeaponTypes.Bladeguard);

            CompatibleWeaponTypes equippedFilter = CompatibleWeaponTypes.None;
            if (TryGetEquippedWeaponType(state, out WeaponType equippedWeaponType))
            {
                equippedFilter = WeaponCompatibility.ToCompatibilityFlag(equippedWeaponType);
            }

            if (!_actionLibraryFilterInitialized)
            {
                _actionLibraryFilter = available.Contains(equippedFilter)
                    ? equippedFilter
                    : available.Count > 0
                        ? available[0]
                        : CompatibleWeaponTypes.All;
                _actionLibraryFilterInitialized = true;
            }
            else if (!available.Contains(_actionLibraryFilter) && available.Count > 0)
            {
                _actionLibraryFilter = available[0];
            }

            var row = new VisualElement();
            row.AddToClassList("combo-action-filter-row");
            var shortcutHint = new Label(
                ExplorationMenuText.Get("combos.action-filter-shortcut"));
            shortcutHint.AddToClassList("combo-action-filter-hint");
            parent.Add(shortcutHint);
            for (int index = 0; index < available.Count; index++)
            {
                CompatibleWeaponTypes filter = available[index];
                string equipped = filter == equippedFilter
                    ? $" • {ExplorationMenuText.Get("combos.equipped-filter")}"
                    : string.Empty;
                var button = new Button(() => SetActionLibraryFilter(filter))
                {
                    text = ExplorationMenuActionTextFormatter.FormatActionLibraryFilter(filter)
                        + equipped
                };
                button.AddToClassList("combo-action-filter-button");
                button.focusable = false;
                button.EnableInClassList(
                    "combo-action-filter-selected",
                    filter == _actionLibraryFilter);
                row.Add(button);
                _comboEditorButtons.Add(button);
                _actionLibraryFilterButtons.Add(button);
                if (filter == _actionLibraryFilter)
                {
                    _selectedActionLibraryFilterButton = button;
                }
                RegisterButtonAction(button, () => SetActionLibraryFilter(filter));
            }

            parent.Add(row);
        }

        private static void AddAvailableActionFilter(
            List<CompatibleWeaponTypes> available,
            IReadOnlyList<ComboActionDefinition> definitions,
            ComboActionCategory category,
            CompatibleWeaponTypes filter)
        {
            for (int index = 0; index < definitions.Count; index++)
            {
                ComboActionDefinition definition = definitions[index];
                if (definition.Category == category
                    && MatchesActionLibraryFilter(definition, filter))
                {
                    available.Add(filter);
                    return;
                }
            }
        }

        private static bool MatchesActionLibraryFilter(
            ComboActionDefinition definition,
            CompatibleWeaponTypes filter)
        {
            if (filter == CompatibleWeaponTypes.All)
            {
                return definition.CompatibleWeaponTypes == CompatibleWeaponTypes.All;
            }

            return definition.CompatibleWeaponTypes != CompatibleWeaponTypes.All
                && (definition.CompatibleWeaponTypes & filter) != 0;
        }

        private void SetActionLibraryFilter(CompatibleWeaponTypes filter)
        {
            _actionLibraryFilter = filter;
            _actionLibraryFilterInitialized = true;
            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
        }

        private void CycleActionLibraryFilter(int direction)
        {
            if (_actionLibraryFilterButtons.Count == 0)
            {
                return;
            }

            int currentIndex = _actionLibraryFilterButtons.IndexOf(
                _selectedActionLibraryFilterButton);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = (currentIndex + direction) % _actionLibraryFilterButtons.Count;
            if (nextIndex < 0)
            {
                nextIndex += _actionLibraryFilterButtons.Count;
            }

            Button nextFilter = _actionLibraryFilterButtons[nextIndex];
            if (_buttonActions.TryGetValue(nextFilter, out Action changeFilter))
            {
                changeFilter();
            }
        }

        private void BeginActionReplacement(PreparedComboActionSlot slot)
        {
            _editingActionSlot = slot;
            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
            if (_usesNavigationInput && _firstActionLibraryButton != null)
            {
                Button firstCandidate = _firstActionLibraryButton;
                _root.schedule.Execute(() => FocusNavigationTarget(firstCandidate)).ExecuteLater(1);
            }
        }

        private void ExitActionReplacement()
        {
            PreparedComboActionSlot slot = _editingActionSlot.Value;
            _editingActionSlot = null;
            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
            if (_usesNavigationInput && (int)slot < _assignedActionButtons.Count)
            {
                Button assignedAction = _assignedActionButtons[(int)slot];
                _root.schedule.Execute(() => FocusNavigationTarget(assignedAction))
                    .ExecuteLater(1);
            }
        }

        private void ReplaceDraftAction(PreparedComboActionSlot slot, string actionId)
        {
            _selectedLoadoutDraft.Combos[_selectedComboIndex].SetAction(slot, actionId);
            _previewChargeLevels.Clear();
            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
        }

        private void SetLinkedPlacement(
            PreparedComboDraft draftCombo,
            PreparedLinkedActionPlacement placement)
        {
            if (!draftCombo.TrySetLinkedPlacement(placement, out string error))
            {
                ShowComboEditorMessage(error, isError: true);
                return;
            }

            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
        }

        private void TryReturnToLoadoutDetails()
        {
            TryExitCurrentCombo(() =>
            {
                _editingActionSlot = null;
                ShowLoadoutDetails(_selectedLoadout);
            });
        }

        private void TryLeaveLoadoutEditor()
        {
            TryLeaveLoadoutEditor(MenuPage.None);
        }

        private void TryLeaveLoadoutEditor(MenuPage destinationPage)
        {
            if (destinationPage == MenuPage.None || destinationPage == MenuPage.Combos)
            {
                ShowCombos();
            }
            else
            {
                ShowPageDirect(destinationPage);
            }
        }

        private void ApplyCurrentComboAndRefresh()
        {
            if (!TryApplyCurrentCombo(out string validationError))
            {
                ShowComboEditorMessage(validationError, isError: true);
                return;
            }

            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
        }

        private void RevertCurrentComboAndRefresh()
        {
            _selectedLoadoutDraft.RevertCombo(_selectedComboIndex);
            _previewChargeLevels.Clear();
            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
        }

        private bool TryApplyCurrentCombo(out string validationError)
        {
            if (!TryValidateSelectedDraft(out _, out validationError))
            {
                return false;
            }

            _selectedLoadoutDraft.ApplyCombo(_selectedComboIndex);
            return true;
        }

        private void TryExitCurrentCombo(Action continuation)
        {
            if (_selectedLoadoutDraft == null
                || _selectedComboIndex < 0
                || !_selectedLoadoutDraft.HasComboChanges(_selectedComboIndex))
            {
                continuation();
                return;
            }

            ShowUnsavedChangesDialog(continuation);
        }

        private void ShowUnsavedChangesDialog(Action continuation)
        {
            CloseUnsavedChangesDialog();

            var overlay = new VisualElement();
            overlay.AddToClassList("unsaved-dialog-overlay");
            var dialog = new VisualElement();
            dialog.AddToClassList("unsaved-dialog");
            overlay.Add(dialog);

            var heading = new Label(ExplorationMenuText.Get("combos.unsaved-heading"));
            heading.AddToClassList("unsaved-dialog-heading");
            dialog.Add(heading);
            var prompt = new Label(ExplorationMenuText.Get("combos.unsaved-prompt"));
            prompt.AddToClassList("unsaved-dialog-prompt");
            dialog.Add(prompt);

            var actions = new VisualElement();
            actions.AddToClassList("unsaved-dialog-actions");
            dialog.Add(actions);

            void Finish(Action result)
            {
                CloseUnsavedChangesDialog();
                result();
            }

            void Save()
            {
                if (!TryApplyCurrentCombo(out string validationError))
                {
                    prompt.text = validationError;
                    prompt.AddToClassList("unsaved-dialog-error");
                    return;
                }

                Finish(continuation);
            }

            var save = new Button(Save)
            {
                text = ExplorationMenuText.Get("combos.unsaved-save")
            };
            save.AddToClassList("unsaved-dialog-button");
            actions.Add(save);
            RegisterUnsavedDialogButton(save, Save);

            void Discard()
            {
                _selectedLoadoutDraft.RevertCombo(_selectedComboIndex);
                _previewChargeLevels.Clear();
                Finish(continuation);
            }

            var discard = new Button(Discard)
            {
                text = ExplorationMenuText.Get("combos.unsaved-discard")
            };
            discard.AddToClassList("unsaved-dialog-button");
            actions.Add(discard);
            RegisterUnsavedDialogButton(discard, Discard);

            var cancel = new Button(CloseUnsavedChangesDialog)
            {
                text = ExplorationMenuText.Get("combos.unsaved-cancel")
            };
            cancel.AddToClassList("unsaved-dialog-button");
            actions.Add(cancel);
            RegisterUnsavedDialogButton(cancel, CloseUnsavedChangesDialog);

            _unsavedDialog = overlay;
            _menuFrame.Add(overlay);
            _root.schedule.Execute(() => save.Focus()).ExecuteLater(1);
        }

        private void RegisterUnsavedDialogButton(Button button, Action action)
        {
            _unsavedDialogButtons.Add(button);
            RegisterButtonAction(button, action);
        }

        private void CloseUnsavedChangesDialog()
        {
            foreach (Button button in _unsavedDialogButtons)
            {
                _buttonActions.Remove(button);
            }

            _unsavedDialogButtons.Clear();
            _unsavedDialog?.RemoveFromHierarchy();
            _unsavedDialog = null;
        }

        private void ShowTimelineGuideDialog()
        {
            CloseTimelineGuideDialog();

            var overlay = new VisualElement();
            overlay.AddToClassList("unsaved-dialog-overlay");
            var dialog = new VisualElement();
            dialog.AddToClassList("unsaved-dialog");
            dialog.AddToClassList("timeline-guide-dialog");
            overlay.Add(dialog);

            var heading = new Label(
                ExplorationMenuText.Get("combos.timeline-guide-heading"));
            heading.AddToClassList("unsaved-dialog-heading");
            dialog.Add(heading);

            var introduction = new Label(
                ExplorationMenuText.Get("combos.timeline-guide-base"));
            introduction.AddToClassList("timeline-guide-body");
            dialog.Add(introduction);

            AddTimelineGuideEntry(
                dialog,
                "D",
                "timeline-marker-damage",
                ExplorationMenuText.Get("combos.timeline-guide-damage"));
            AddTimelineGuideEntry(
                dialog,
                "G",
                "timeline-marker-guard",
                ExplorationMenuText.Get("combos.timeline-guide-guard"));
            AddTimelineGuideEntry(
                dialog,
                "E",
                "timeline-marker-evade",
                ExplorationMenuText.Get("combos.timeline-guide-evade"));
            AddTimelineGuideEntry(
                dialog,
                "B",
                "timeline-marker-buff",
                ExplorationMenuText.Get("combos.timeline-guide-status"));
            AddTimelineGuideEntry(
                dialog,
                "H",
                "timeline-marker-heal",
                ExplorationMenuText.Get("combos.timeline-guide-heal"));
            AddTimelineGuideEntry(
                dialog,
                "S",
                "timeline-marker-stamina",
                ExplorationMenuText.Get("combos.timeline-guide-stamina"));

            var windowRow = new VisualElement();
            windowRow.AddToClassList("timeline-guide-row");
            var windowCell = new TimelineResolutionCell();
            windowCell.AddToClassList("timeline-guide-swatch");
            windowCell.SetMarkerText("G");
            windowCell.SetResolutionWindow(
                ResolutionMarkerColor(ActionResolutionEffect.Guard));
            windowRow.Add(windowCell);
            var windowDescription = new Label(
                ExplorationMenuText.Get("combos.timeline-guide-window"));
            windowDescription.AddToClassList("timeline-guide-description");
            windowRow.Add(windowDescription);
            dialog.Add(windowRow);

            var charge = new Label(
                ExplorationMenuText.Get("combos.timeline-guide-charge"));
            charge.AddToClassList("timeline-guide-body");
            dialog.Add(charge);

            var actions = new VisualElement();
            actions.AddToClassList("unsaved-dialog-actions");
            dialog.Add(actions);
            var close = new Button(CloseTimelineGuideDialog)
            {
                text = ExplorationMenuText.Get("combos.timeline-guide-close")
            };
            close.AddToClassList("unsaved-dialog-button");
            actions.Add(close);
            _timelineGuideButtons.Add(close);
            RegisterButtonAction(close, CloseTimelineGuideDialog);

            _timelineGuideDialog = overlay;
            _menuFrame.Add(overlay);
            _root.schedule.Execute(() => close.Focus()).ExecuteLater(1);
        }

        private static void AddTimelineGuideEntry(
            VisualElement parent,
            string marker,
            string markerClass,
            string description)
        {
            var row = new VisualElement();
            row.AddToClassList("timeline-guide-row");
            var cell = new TimelineResolutionCell();
            cell.AddToClassList("timeline-guide-swatch");
            cell.AddToClassList(markerClass);
            cell.SetMarkerText(marker);
            row.Add(cell);
            var label = new Label(description);
            label.AddToClassList("timeline-guide-description");
            row.Add(label);
            parent.Add(row);
        }

        private void CloseTimelineGuideDialog()
        {
            foreach (Button button in _timelineGuideButtons)
            {
                _buttonActions.Remove(button);
            }

            _timelineGuideButtons.Clear();
            _timelineGuideDialog?.RemoveFromHierarchy();
            _timelineGuideDialog = null;
        }

        private void ActivateSelectedLoadout()
        {
            if (!TryValidateSelectedDraft(out _, out string validationError))
            {
                ShowComboEditorMessage(validationError, isError: true);
                return;
            }

            if (PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                service.State.ComboLoadouts.SetActive(_selectedLoadout.LoadoutId);
            }

            _selectedLoadoutDraft = new PlayerComboLoadoutDraft(_selectedLoadout);
            ShowLoadoutDetails(_selectedLoadout);
        }

        private bool TryValidateSelectedDraft(
            out ComboValidationResult validation,
            out string validationError)
        {
            validation = null;
            validationError = null;
            if (_selectedLoadoutDraft == null
                || !TryGetContext(out PersistentPlayerState state, out _)
                || !TryBuildDraftLoadout(
                    _selectedLoadoutDraft,
                    state,
                    ComboActionCatalogAsset.LoadDefault(),
                    out IReadOnlyList<ComboConfiguration> configurations,
                    out validationError))
            {
                return false;
            }

            validation = ComboLoadoutValidator.Validate(
                configurations,
                new ComboValidationLimits(
                    PlayerStateLoadoutSessionBuilder.MaximumTimelineCost,
                    state.MaximumMana,
                    state.MaximumCombatStamina));
            if (validation.IsValid)
            {
                return true;
            }

            validationError = ExplorationMenuActionTextFormatter.FormatFirstIssue(validation);
            return false;
        }

        private static bool TryBuildDraftLoadout(
            PlayerComboLoadoutDraft draft,
            PersistentPlayerState state,
            ComboActionCatalogAsset catalog,
            out IReadOnlyList<ComboConfiguration> configurations,
            out string validationError)
        {
            configurations = null;
            if (draft == null)
            {
                validationError = "A Combo Loadout draft is required.";
                return false;
            }

            var created = new List<ComboConfiguration>(draft.Combos.Count);
            for (int index = 0; index < draft.Combos.Count; index++)
            {
                PreparedComboState prepared;
                try
                {
                    prepared = draft.Combos[index].CreatePreparedState();
                }
                catch (ArgumentException exception)
                {
                    validationError = $"Combo {index + 1}: {exception.Message}";
                    return false;
                }

                if (!PlayerStateComboConfigurationBuilder.TryBuildCombo(
                        prepared,
                        state,
                        catalog,
                        out ComboConfiguration configuration,
                        out validationError))
                {
                    validationError = $"Combo {index + 1}: {validationError}";
                    return false;
                }

                created.Add(configuration);
            }

            configurations = created.AsReadOnly();
            validationError = null;
            return true;
        }

        private bool IsActionAssignedElsewhere(
            string actionId,
            int currentComboIndex,
            PreparedComboActionSlot currentSlot)
        {
            for (int comboIndex = 0;
                comboIndex < _selectedLoadoutDraft.Combos.Count;
                comboIndex++)
            {
                PreparedComboDraft combo = _selectedLoadoutDraft.Combos[comboIndex];
                foreach (PreparedComboActionSlot slot in Enum.GetValues(typeof(PreparedComboActionSlot)))
                {
                    if (comboIndex == currentComboIndex && slot == currentSlot)
                    {
                        continue;
                    }

                    if (string.Equals(combo.GetActionId(slot), actionId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetEquippedWeaponType(
            PersistentPlayerState state,
            out WeaponType weaponType)
        {
            PlayerEquipmentCatalogAsset catalog = PlayerEquipmentCatalogAsset.LoadDefault();
            if (catalog != null
                && catalog.TryGetWeapon(
                    state.Equipment.EquippedWeaponId,
                    out WeaponDefinition weapon,
                    out _))
            {
                weaponType = weapon.WeaponType;
                return true;
            }

            weaponType = default;
            return false;
        }

        private void ShowComboEditorMessage(string message, bool isError)
        {
            if (_comboEditorMessage == null)
            {
                AddComboError(message);
                return;
            }

            _comboEditorMessage.text = message;
            _comboEditorMessage.EnableInClassList("combo-editor-message-error", isError);
        }

        private static ComboActionCategory ToActionCategory(PreparedComboActionSlot slot)
        {
            return (ComboActionCategory)slot;
        }

        private void ConfigurePageHeader(string title)
        {
            _contentRevision++;
            _pageTitle.text = title;
            _pageHeaderStatus.text = string.Empty;
            _pageHeaderStatus.RemoveFromClassList("page-header-status-active");
            _pageHeaderActions.Clear();
            _renameEditor = null;
            SetSidebarCollapsed(IsNestedComboPage());
        }

        private bool IsNestedComboPage()
        {
            return _currentPage == MenuPage.Combos
                && (_comboMenuLevel == ComboMenuLevel.LoadoutDetails
                    || _comboMenuLevel == ComboMenuLevel.ComboDetails);
        }

        private void SetSidebarCollapsed(bool collapsed)
        {
            _menuNavigation.EnableInClassList("menu-navigation-collapsed", collapsed);
            bool allowSidebarFocus = !collapsed
                && (_currentPage != MenuPage.Combos
                    || _comboMenuLevel != ComboMenuLevel.LoadoutList
                    || _comboSidebarUnlocked);
            for (int index = 0; index < _sidebarButtons.Count; index++)
            {
                _sidebarButtons[index].focusable = allowSidebarFocus;
            }
        }

        private Button DefaultNavigationTarget()
        {
            if (_currentPage == MenuPage.Combos)
            {
                if (_comboMenuLevel == ComboMenuLevel.LoadoutList
                    && !_comboSidebarUnlocked)
                {
                    return FirstEnabled(_comboPageButtons);
                }

                if (IsNestedComboPage())
                {
                    return FirstNestedComboNavigationTarget();
                }
            }

            return _lastNavigationTarget;
        }

        private Button FirstNestedComboNavigationTarget()
        {
            if (_editingActionSlot.HasValue)
            {
                return FirstEnabled(_actionLibraryCandidateButtons);
            }

            if (_comboMenuLevel == ComboMenuLevel.ComboDetails)
            {
                return FirstEnabled(_assignedActionButtons);
            }

            if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails)
            {
                return FirstEnabled(_loadoutComboButtons);
            }

            return null;
        }

        private void SetPageHeaderStatus(string text)
        {
            _pageHeaderStatus.text = text;
            _pageHeaderStatus.AddToClassList("page-header-status-active");
        }

        private void AddPageHeaderButton(string text, Action action, string className)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("page-header-button");
            button.AddToClassList(className);
            _pageHeaderActions.Add(button);
            _comboPageButtons.Add(button);
            _pageHeaderButtons.Add(button);
            RegisterButtonAction(button, action);
        }

        private void ShowLoadoutRenameEditor()
        {
            ShowRenameEditor(
                ExplorationMenuText.Get("combos.rename-loadout"),
                _selectedLoadoutDraft.Name,
                CommitLoadoutRename);
        }

        private void ShowComboRenameEditor()
        {
            ShowRenameEditor(
                ExplorationMenuText.Get("combos.rename-combo"),
                _selectedLoadoutDraft.Combos[_selectedComboIndex].Name,
                CommitComboRename);
        }

        private void ShowRenameEditor(
            string heading,
            string currentName,
            Action<string> commit)
        {
            if (_renameEditor != null)
            {
                return;
            }

            var editor = new VisualElement();
            editor.AddToClassList("combo-rename-editor");
            var label = new Label(heading);
            label.AddToClassList("combo-rename-label");
            editor.Add(label);

            var field = new TextField { value = currentName };
            field.AddToClassList("combo-rename-field");
            editor.Add(field);

            var error = new Label();
            error.AddToClassList("combo-rename-error");
            editor.Add(error);

            void Save()
            {
                if (string.IsNullOrWhiteSpace(field.value))
                {
                    error.text = ExplorationMenuText.Get("combos.rename-required");
                    return;
                }

                commit(field.value.Trim());
            }

            var save = new Button(Save)
            {
                text = ExplorationMenuText.Get("combos.rename-save")
            };
            save.AddToClassList("combo-rename-button");
            editor.Add(save);
            _comboEditorButtons.Add(save);
            RegisterButtonAction(save, Save);

            var cancel = new Button(RefreshCurrentComboPage)
            {
                text = ExplorationMenuText.Get("combos.rename-cancel")
            };
            cancel.AddToClassList("combo-rename-button");
            editor.Add(cancel);
            _comboEditorButtons.Add(cancel);
            RegisterButtonAction(cancel, RefreshCurrentComboPage);

            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    Save();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    RefreshCurrentComboPage();
                    evt.StopPropagation();
                }
            });

            _renameEditor = editor;
            _content.Insert(0, editor);
            _root.schedule.Execute(() => field.Focus()).ExecuteLater(1);
        }

        private void CommitLoadoutRename(string name)
        {
            _selectedLoadout.Rename(name);
            _selectedLoadoutDraft.Rename(name);
            ShowLoadoutDetails(_selectedLoadout);
        }

        private void CommitComboRename(string name)
        {
            _selectedLoadout.RenameCombo(_selectedComboIndex, name);
            _selectedLoadoutDraft.Combos[_selectedComboIndex].Rename(name);
            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
        }

        private void RefreshCurrentComboPage()
        {
            if (_comboMenuLevel == ComboMenuLevel.ComboDetails && _selectedComboIndex >= 0)
            {
                ShowComboDetails(_selectedLoadout, _selectedComboIndex);
            }
            else if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails)
            {
                ShowLoadoutDetails(_selectedLoadout);
            }
        }

        private void AddTimelinePreview(ComboConfiguration configuration)
        {
            IReadOnlyList<ComboActionDefinition> actions = configuration.BuildExecutionOrder();
            int totalTimelineCost = 0;
            for (int index = 0; index < actions.Count; index++)
            {
                totalTimelineCost += GetPreviewTimelineCost(
                    actions[index],
                    GetPreviewChargeLevel(actions[index]));
            }

            int displayedTimeSteps = Mathf.Max(
                PlayerStateLoadoutSessionBuilder.MaximumTimelineCost,
                totalTimelineCost);
            bool overflowsBaseTimeline = displayedTimeSteps
                > PlayerStateLoadoutSessionBuilder.MaximumTimelineCost;

            _timelineScroll = new ScrollView(ScrollViewMode.Horizontal);
            _timelineScroll.AddToClassList("timeline-scroll");
            _timelineScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            _timelineScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            var canvas = new VisualElement();
            canvas.AddToClassList("timeline-canvas");
            if (overflowsBaseTimeline)
            {
                canvas.AddToClassList("timeline-canvas-overflow");
                const float actionNameWidth = 190f;
                const float overflowCellWidthWithMargin = 43f;
                canvas.style.width = actionNameWidth
                    + displayedTimeSteps * overflowCellWidthWithMargin;
            }

            var scaleRow = new VisualElement();
            scaleRow.AddToClassList("timeline-scale-row");
            var scaleSpacer = new VisualElement();
            scaleSpacer.AddToClassList("timeline-action-spacer");
            scaleRow.Add(scaleSpacer);

            var scale = new VisualElement();
            scale.AddToClassList("timeline-scale");
            for (int timeStep = 1;
                 timeStep <= displayedTimeSteps;
                 timeStep++)
            {
                var tick = new Label(timeStep % 5 == 0
                    ? timeStep.ToString()
                    : string.Empty);
                tick.AddToClassList("timeline-scale-label");
                if (overflowsBaseTimeline)
                {
                    tick.AddToClassList("timeline-cell-overflow");
                }
                scale.Add(tick);
            }

            scaleRow.Add(scale);
            canvas.Add(scaleRow);

            int actionStart = 0;
            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                ComboActionDefinition action = actions[actionIndex];
                int chargeLevel = GetPreviewChargeLevel(action);
                int previewTimelineCost = GetPreviewTimelineCost(action, chargeLevel);
                var row = new VisualElement();
                row.AddToClassList("timeline-row");
                if (action.IsChargeable)
                {
                    var actionButton = new Button(
                        () => ShowChargeLevelChoices(action));
                    actionButton.text = $"{action.DisplayName}  |  Charge Lv.{chargeLevel}";
                    actionButton.AddToClassList("timeline-action-name");
                    actionButton.AddToClassList("timeline-action-button");
                    row.Add(actionButton);
                    _comboPageButtons.Add(actionButton);
                    RegisterButtonAction(
                        actionButton,
                        () => ShowChargeLevelChoices(action));
                }
                else
                {
                    var name = new Label(action.DisplayName);
                    name.AddToClassList("timeline-action-name");
                    row.Add(name);
                }

                var track = new VisualElement();
                track.AddToClassList("timeline-track");
                for (int timeStep = 1;
                     timeStep <= displayedTimeSteps;
                     timeStep++)
                {
                    var cell = new TimelineResolutionCell();
                    if (timeStep > actionStart
                        && timeStep <= actionStart + previewTimelineCost)
                    {
                        cell.AddToClassList("timeline-cell-active");
                    }

                    if (overflowsBaseTimeline)
                    {
                        cell.AddToClassList("timeline-cell-overflow");
                    }

                    ApplyResolutionMarker(
                        cell,
                        action,
                        timeStep - actionStart,
                        chargeLevel);
                    track.Add(cell);
                }

                row.Add(track);
                canvas.Add(row);
                actionStart += previewTimelineCost;
            }

            _timelineScroll.Add(canvas);
            _content.Add(_timelineScroll);
        }

        private static void ApplyResolutionMarker(
            TimelineResolutionCell cell,
            ComboActionDefinition action,
            int localTime,
            int chargeLevel)
        {
            string combinedMarkerText = string.Empty;
            string firstMarkerClass = null;
            Color? windowStripeColor = null;
            int chargeTimelineExtension = GetChargeTimelineExtension(action, chargeLevel);
            int chargeResolutionOffset = action.ResolutionOffset + chargeTimelineExtension;

            bool isInsideActionWindow = action.HasResolutionWindow
                && localTime >= action.ResolutionWindowStart.Value
                && localTime <= action.ResolutionWindowEnd.Value;
            if (isInsideActionWindow)
            {
                combinedMarkerText = ResolutionMarkerText(action.ResolutionEffect);
                windowStripeColor = ResolutionMarkerColor(action.ResolutionEffect);
            }

            for (int index = 0; index < action.ResolutionEvents.Count; index++)
            {
                ActionResolutionDefinition resolution = action.ResolutionEvents[index];
                if (isInsideActionWindow && index == 0)
                {
                    continue;
                }

                int resolutionOffset = action.IsChargeable && index == 0
                    ? chargeResolutionOffset
                    : resolution.Offset;

                if (resolution.HasResolutionWindow
                    && localTime >= resolution.ResolutionWindowStart.Value
                    && localTime <= resolution.ResolutionWindowEnd.Value)
                {
                    combinedMarkerText += ResolutionMarkerText(resolution.Effect);
                    windowStripeColor ??= ResolutionMarkerColor(resolution.Effect);
                    continue;
                }

                if (resolutionOffset != localTime)
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

        private int GetPreviewChargeLevel(ComboActionDefinition action)
        {
            if (action == null || !action.IsChargeable)
            {
                return 0;
            }

            return _previewChargeLevels.TryGetValue(action.ActionId, out int level)
                ? Mathf.Clamp(level, 0, action.ChargeLevels.Count)
                : 0;
        }

        private static int GetPreviewTimelineCost(
            ComboActionDefinition action,
            int chargeLevel)
        {
            return action.TimelineCost + GetChargeTimelineExtension(action, chargeLevel);
        }

        private static int GetChargeTimelineExtension(
            ComboActionDefinition action,
            int chargeLevel)
        {
            return action != null && action.IsChargeable && chargeLevel > 0
                ? action.ChargeLevels[chargeLevel - 1].AdditionalTimelineCost
                : 0;
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

        private void AddActionDetail(ComboActionDefinition action, int chargeLevel)
        {
            AddActionDetail(_content, action, chargeLevel);
        }

        private static void AddActionDetail(
            VisualElement parent,
            ComboActionDefinition action,
            int chargeLevel)
        {
            var card = new VisualElement();
            card.AddToClassList("action-detail-card");
            var title = new Label(action.DisplayName);
            title.AddToClassList("action-detail-title");
            card.Add(title);

            int previewTimelineCost = GetPreviewTimelineCost(action, chargeLevel);
            var costs = new Label(
                $"Timeline {previewTimelineCost}    Stamina {action.StaminaCost}"
                + $"    Mana {action.ManaCost}    Cooldown {action.Cooldown}");
            costs.AddToClassList("action-detail-costs");
            card.Add(costs);

            for (int index = 0; index < action.ResolutionEvents.Count; index++)
            {
                ActionResolutionDefinition resolution = action.ResolutionEvents[index];
                int resolutionOffset = action.IsChargeable && index == 0
                    ? action.ResolutionOffset + GetChargeTimelineExtension(action, chargeLevel)
                    : resolution.Offset;
                string timing = $"T{resolutionOffset}";
                if (action.IsChargeable && resolution.Offset == action.ResolutionOffset)
                {
                    timing += $" (Charge Lv.{chargeLevel} Preview)";
                }
                else if (index == 0 && action.HasResolutionWindow)
                {
                    timing = $"T{action.ResolutionWindowStart.Value}–T{action.ResolutionWindowEnd.Value} Resolution Window";
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
                        double multiplier = components[componentIndex].ActionMultiplier;
                        if (action.IsChargeable && index == 0 && chargeLevel > 0)
                        {
                            multiplier *= action.ChargeLevels[chargeLevel - 1]
                                .DamageMultiplierScale;
                        }

                        detail += $"{components[componentIndex].Type} "
                            + $"{multiplier * 100d:0}%";
                    }
                }

                var resolutionLabel = new Label(detail);
                resolutionLabel.AddToClassList("action-resolution-line");
                card.Add(resolutionLabel);
            }

            parent.Add(card);
        }

        private void AddChargeLevelPanel()
        {
            _chargeLevelPanel = new VisualElement();
            _chargeLevelPanel.AddToClassList("charge-level-panel");
            _chargeLevelPanel.AddToClassList("charge-level-panel-hidden");
            _content.Add(_chargeLevelPanel);
        }

        private void ShowChargeLevelChoices(ComboActionDefinition action)
        {
            if (action == null || !action.IsChargeable || _chargeLevelPanel == null)
            {
                return;
            }

            foreach (Button button in _chargeLevelButtons)
            {
                _buttonActions.Remove(button);
            }

            _chargeLevelButtons.Clear();
            _chargeLevelPanel.Clear();
            _chargeLevelPanel.RemoveFromClassList("charge-level-panel-hidden");
            _chargeActionButton = GetFocusedButton();

            var title = new Label(
                $"{ExplorationMenuText.Get("combos.charge-level")} — {action.DisplayName}");
            title.AddToClassList("charge-level-title");
            _chargeLevelPanel.Add(title);

            var choices = new VisualElement();
            choices.AddToClassList("charge-level-choices");
            int selectedLevel = GetPreviewChargeLevel(action);
            for (int level = 0; level <= action.ChargeLevels.Count; level++)
            {
                int capturedLevel = level;
                var button = new Button(
                    () => SelectPreviewChargeLevel(action, capturedLevel))
                {
                    text = $"Lv.{level}"
                };
                button.AddToClassList("charge-level-button");
                button.EnableInClassList("charge-level-selected", level == selectedLevel);
                choices.Add(button);
                _chargeLevelButtons.Add(button);
                RegisterButtonAction(
                    button,
                    () => SelectPreviewChargeLevel(action, capturedLevel));
            }

            _chargeLevelPanel.Add(choices);
            _root.schedule.Execute(
                () => _chargeLevelButtons[selectedLevel].Focus()).ExecuteLater(1);
        }

        private void HideChargeLevelChoices()
        {
            foreach (Button button in _chargeLevelButtons)
            {
                _buttonActions.Remove(button);
            }

            _chargeLevelButtons.Clear();
            _chargeLevelPanel.Clear();
            _chargeLevelPanel.AddToClassList("charge-level-panel-hidden");
            Button actionButton = _chargeActionButton;
            _chargeActionButton = null;
            if (actionButton != null)
            {
                _root.schedule.Execute(() => actionButton.Focus()).ExecuteLater(1);
            }
        }

        private void SelectPreviewChargeLevel(
            ComboActionDefinition action,
            int chargeLevel)
        {
            _previewChargeLevels[action.ActionId] = chargeLevel;
            ShowComboDetails(_selectedLoadout, _selectedComboIndex);
        }

        private void PanTimeline(int direction)
        {
            if (_timelineScroll == null || direction == 0)
            {
                return;
            }

            Vector2 offset = _timelineScroll.scrollOffset;
            offset.x = Mathf.Max(0f, offset.x + direction * 180f);
            _timelineScroll.scrollOffset = offset;
        }

        private void AddComboError(string message)
        {
            var error = new Label(message);
            error.AddToClassList("combo-status-invalid");
            _content.Add(error);
        }

        private static void AddCompactTimelinePreview(
            VisualElement parent,
            ComboConfiguration configuration)
        {
            IReadOnlyList<ComboActionDefinition> actions = configuration.BuildExecutionOrder();
            var track = new VisualElement();
            track.AddToClassList("compact-timeline-track");
            var cells = new TimelineResolutionCell[
                PlayerStateLoadoutSessionBuilder.MaximumTimelineCost];
            for (int index = 0; index < cells.Length; index++)
            {
                cells[index] = new TimelineResolutionCell();
                cells[index].AddToClassList("compact-timeline-cell");
                track.Add(cells[index]);
            }

            int actionStart = 0;
            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                ComboActionDefinition action = actions[actionIndex];
                for (int localTime = 1; localTime <= action.TimelineCost; localTime++)
                {
                    int globalIndex = actionStart + localTime - 1;
                    if (globalIndex < 0 || globalIndex >= cells.Length)
                    {
                        continue;
                    }

                    cells[globalIndex].AddToClassList("compact-timeline-cell-active");
                    ApplyCompactResolutionMarker(cells[globalIndex], action, localTime);
                }

                actionStart += action.TimelineCost;
            }

            parent.Add(track);
        }

        private static void ApplyCompactResolutionMarker(
            TimelineResolutionCell cell,
            ComboActionDefinition action,
            int localTime)
        {
            Color? windowStripeColor = null;
            string markerClass = null;
            bool chargeDependentPoint = false;
            bool isInsideActionWindow = action.HasResolutionWindow
                && localTime >= action.ResolutionWindowStart.Value
                && localTime <= action.ResolutionWindowEnd.Value;
            if (isInsideActionWindow)
            {
                windowStripeColor = ResolutionMarkerColor(action.ResolutionEffect);
            }

            for (int index = 0; index < action.ResolutionEvents.Count; index++)
            {
                ActionResolutionDefinition resolution = action.ResolutionEvents[index];
                if (isInsideActionWindow && index == 0)
                {
                    continue;
                }

                if (resolution.HasResolutionWindow
                    && localTime >= resolution.ResolutionWindowStart.Value
                    && localTime <= resolution.ResolutionWindowEnd.Value)
                {
                    windowStripeColor ??= ResolutionMarkerColor(resolution.Effect);
                    continue;
                }

                if (resolution.Offset != localTime)
                {
                    continue;
                }

                chargeDependentPoint |= action.IsChargeable
                    && resolution.Offset == action.ResolutionOffset;
                markerClass ??= ResolutionMarkerClass(resolution.Effect);
            }

            if (windowStripeColor.HasValue)
            {
                cell.SetResolutionWindow(windowStripeColor.Value);
            }

            if (chargeDependentPoint)
            {
                cell.AddToClassList("compact-timeline-marker-charge");
            }
            else if (markerClass != null)
            {
                cell.AddToClassList(markerClass);
            }
        }

        private void ShowInventory()
        {
            if (!TryGetContext(out PersistentPlayerState state, out _))
            {
                return;
            }

            _currentPage = MenuPage.Inventory;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            ClearComboControls();
            ConfigurePageHeader(ExplorationMenuText.Get("inventory.heading"));
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

            _currentPage = MenuPage.Equipment;
            _editingEquipmentSlot = EquipmentSlot.None;
            ClearComboControls();
            ConfigurePageHeader(ExplorationMenuText.Get("equipment.heading"));
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
            _currentPage = MenuPage.ReturnToTitle;
            _editingEquipmentSlot = EquipmentSlot.None;
            _isInsideEquipmentPage = false;
            ClearComboControls();
            ConfigurePageHeader(ExplorationMenuText.Get("nav.title"));
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
                if (_comboMenuLevel == ComboMenuLevel.ComboDetails)
                {
                    TryReturnToLoadoutDetails();
                }
                else if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails)
                {
                    TryLeaveLoadoutEditor();
                }
                else
                {
                    ShowCombos();
                }

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

        private void OnReturnToTitleClicked()
        {
            if (!_isOpen)
            {
                return;
            }

            CloseMenu();
            SceneManager.LoadScene(TitleMenuBootstrap.TitleSceneName);
        }

        private void ShowPageWhenChanged(MenuPage page)
        {
            if (!_isOpen || _currentPage == page)
            {
                return;
            }

            if (_currentPage == MenuPage.Combos
                && _selectedLoadoutDraft != null
                && _comboMenuLevel == ComboMenuLevel.ComboDetails)
            {
                TryExitCurrentCombo(() => ShowPageDirect(page));
                return;
            }

            if (_currentPage == MenuPage.Combos
                && _selectedLoadoutDraft != null
                && _comboMenuLevel == ComboMenuLevel.LoadoutDetails)
            {
                TryLeaveLoadoutEditor(page);
                return;
            }

            ShowPageDirect(page);
        }

        private void ShowPageDirect(MenuPage page)
        {
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
                button.pickingMode = _usesNavigationInput
                    ? PickingMode.Ignore
                    : PickingMode.Position;
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

            FocusNavigationTarget(scope[nextIndex]);
        }

        private bool TryMoveComboFocusHorizontally(Button focusedButton, int direction)
        {
            if (_currentPage != MenuPage.Combos || focusedButton == null)
            {
                return false;
            }

            if (_editingActionSlot.HasValue)
            {
                if (_actionLibraryFilterButtons.Contains(focusedButton))
                {
                    return true;
                }

                if (_actionLibraryCandidateButtons.Contains(focusedButton))
                {
                    return true;
                }
            }

            if (_pageHeaderButtons.Contains(focusedButton))
            {
                FocusAdjacent(_pageHeaderButtons, focusedButton, direction);
                return true;
            }

            if (_linkedPlacementButtons.Contains(focusedButton))
            {
                FocusAdjacent(_linkedPlacementButtons, focusedButton, direction);
                return true;
            }

            return false;
        }

        private bool TryMoveComboFocusVertically(Button focusedButton, int direction)
        {
            if (_currentPage != MenuPage.Combos || focusedButton == null)
            {
                return false;
            }

            if (_editingActionSlot.HasValue)
            {
                if (_actionLibraryFilterButtons.Contains(focusedButton))
                {
                    if (direction > 0)
                    {
                        FocusFirstEnabled(_actionLibraryCandidateButtons);
                    }

                    return true;
                }

                int candidateIndex = _actionLibraryCandidateButtons.IndexOf(focusedButton);
                if (candidateIndex >= 0)
                {
                    FocusAdjacentEnabled(
                        _actionLibraryCandidateButtons,
                        candidateIndex,
                        direction);

                    return true;
                }

                FocusNavigationTarget(_firstActionLibraryButton);
                return true;
            }

            if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails)
            {
                if (_pageHeaderButtons.Contains(focusedButton))
                {
                    if (direction > 0)
                    {
                        FocusFirstEnabled(_loadoutComboButtons);
                    }

                    return true;
                }

                int comboIndex = _loadoutComboButtons.IndexOf(focusedButton);
                if (comboIndex >= 0)
                {
                    if (direction < 0 && comboIndex == 0)
                    {
                        FocusNavigationTarget(_pageHeaderButtons.Count > 0
                            ? _pageHeaderButtons[0]
                            : null);
                    }
                    else
                    {
                        FocusAdjacentEnabled(_loadoutComboButtons, comboIndex, direction);
                    }

                    return true;
                }

            }

            if (_comboMenuLevel == ComboMenuLevel.ComboDetails)
            {
                if (_pageHeaderButtons.Contains(focusedButton))
                {
                    if (direction > 0)
                    {
                        FocusFirstEnabled(_assignedActionButtons);
                    }

                    return true;
                }

                int assignedIndex = _assignedActionButtons.IndexOf(focusedButton);
                if (assignedIndex >= 0)
                {
                    if (direction < 0 && assignedIndex == 0)
                    {
                        FocusNavigationTarget(_pageHeaderButtons.Count > 0
                            ? _pageHeaderButtons[0]
                            : null);
                    }
                    else if (direction > 0
                        && assignedIndex == _assignedActionButtons.Count - 1
                        && _linkedPlacementButtons.Count > 0)
                    {
                        FocusFirstEnabled(_linkedPlacementButtons);
                    }
                    else
                    {
                        FocusAdjacentEnabled(
                            _assignedActionButtons,
                            assignedIndex,
                            direction);
                    }

                    return true;
                }

                if (_linkedPlacementButtons.Contains(focusedButton))
                {
                    if (direction < 0)
                    {
                        FocusLastEnabled(_assignedActionButtons);
                    }

                    return true;
                }
            }

            return false;
        }

        private void FocusAdjacent(List<Button> buttons, Button current, int direction)
        {
            int index = buttons.IndexOf(current);
            if (index < 0 || buttons.Count == 0)
            {
                return;
            }

            int next = Mathf.Clamp(index + direction, 0, buttons.Count - 1);
            FocusNavigationTarget(buttons[next]);
        }

        private void FocusAdjacentEnabled(List<Button> buttons, int index, int direction)
        {
            int next = index + direction;
            while (next >= 0 && next < buttons.Count)
            {
                if (buttons[next].enabledInHierarchy)
                {
                    FocusNavigationTarget(buttons[next]);
                    return;
                }

                next += direction;
            }
        }

        private void FocusFirstEnabled(List<Button> buttons)
        {
            Button target = FirstEnabled(buttons);
            if (target != null)
            {
                FocusNavigationTarget(target);
            }
        }

        private void FocusLastEnabled(List<Button> buttons)
        {
            for (int index = buttons.Count - 1; index >= 0; index--)
            {
                if (buttons[index].enabledInHierarchy)
                {
                    FocusNavigationTarget(buttons[index]);
                    return;
                }
            }
        }

        private List<Button> GetFocusScope(Button focusedButton)
        {
            if (_timelineGuideDialog != null)
            {
                return _timelineGuideButtons;
            }

            if (_unsavedDialog != null)
            {
                return _unsavedDialogButtons;
            }

            if (focusedButton != null && _chargeLevelButtons.Contains(focusedButton))
            {
                return _chargeLevelButtons;
            }

            if (focusedButton != null && _comboPageButtons.Contains(focusedButton))
            {
                return _comboPageButtons;
            }

            if (focusedButton != null && _comboEditorButtons.Contains(focusedButton))
            {
                return _comboEditorButtons;
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
            _contentRevision++;
            foreach (Button button in _equipmentWarehouseButtons)
            {
                _buttonActions.Remove(button);
            }

            _equipmentWarehouseButtons.Clear();
        }

        private void ClearComboControls()
        {
            _contentRevision++;
            foreach (Button button in _comboPageButtons)
            {
                _buttonActions.Remove(button);
            }

            _comboPageButtons.Clear();
            foreach (Button button in _comboEditorButtons)
            {
                _buttonActions.Remove(button);
            }

            _comboEditorButtons.Clear();
            _pageHeaderButtons.Clear();
            _loadoutComboButtons.Clear();
            _assignedActionButtons.Clear();
            _linkedPlacementButtons.Clear();
            _actionLibraryFilterButtons.Clear();
            _actionLibraryCandidateButtons.Clear();
            foreach (Button button in _chargeLevelButtons)
            {
                _buttonActions.Remove(button);
            }

            _chargeLevelButtons.Clear();
            _timelineScroll = null;
            _chargeLevelPanel = null;
            _chargeActionButton = null;
            _comboActionHeadingActions = null;
            _comboEditorMessage = null;
            _firstActionLibraryButton = null;
            _selectedActionLibraryFilterButton = null;
            if (_currentPage != MenuPage.Combos)
            {
                _comboMenuLevel = ComboMenuLevel.None;
                _selectedLoadout = null;
                _selectedLoadoutDraft = null;
                _selectedComboIndex = -1;
                _editingActionSlot = null;
            }
        }

        private void FocusFirstComboPageButton()
        {
            if (!_usesNavigationInput)
            {
                return;
            }

            Button target = null;
            if (_comboMenuLevel == ComboMenuLevel.LoadoutDetails)
            {
                target = FirstEnabled(_loadoutComboButtons);
            }
            else if (_comboMenuLevel == ComboMenuLevel.ComboDetails)
            {
                target = _editingActionSlot.HasValue
                    ? _firstActionLibraryButton
                    : FirstEnabled(_assignedActionButtons);
            }

            target ??= FirstEnabled(_comboPageButtons);
            if (target != null)
            {
                _root.schedule.Execute(() => FocusNavigationTarget(target)).ExecuteLater(1);
            }
        }

        private static Button FirstEnabled(List<Button> buttons)
        {
            for (int index = 0; index < buttons.Count; index++)
            {
                if (buttons[index].enabledInHierarchy)
                {
                    return buttons[index];
                }
            }

            return null;
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void GrantEditorComboPreviewLoadouts()
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            PersistentPlayerState state = service.State;
            PlayerComboLoadoutState glaiveLoadout = GetOrUnlockPreviewLoadout(
                state,
                2,
                "Glaive Preview Loadout");
            LearnAndConfigureGlaivePreview(state, glaiveLoadout);

            PlayerComboLoadoutState commonLoadout = GetOrUnlockPreviewLoadout(
                state,
                3,
                "Common Preview Loadout");
            LearnAndConfigureCommonPreview(state, commonLoadout);
        }

        private static PlayerComboLoadoutState GetOrUnlockPreviewLoadout(
            PersistentPlayerState state,
            int oneBasedSlot,
            string name)
        {
            while (state.ComboLoadouts.UnlockedSlotCount < oneBasedSlot)
            {
                state.ComboLoadouts.UnlockNext();
            }

            PlayerComboLoadoutState loadout = state.ComboLoadouts.Loadouts[oneBasedSlot - 1];
            loadout.Rename(name);
            return loadout;
        }

        private static void LearnAndConfigureGlaivePreview(
            PersistentPlayerState state,
            PlayerComboLoadoutState loadout)
        {
            string[] actionIds =
            {
                "glaive-and-shield.opening.restraining-slash",
                "glaive-and-shield.main.diagonal-slash",
                "glaive-and-shield.closing.turning-back-thrust",
                "glaive-and-shield.linked.counter-thrust",
                "glaive-and-shield.opening.guarded-thrust",
                "glaive-and-shield.main.flying-round-slash",
                "glaive-and-shield.closing.flying-round-slash",
                "glaive-and-shield.linked.bash",
                "glaive-and-shield.opening.leaping-slash",
                "glaive-and-shield.main.rising-slash-chain",
                "glaive-and-shield.closing.rising-slash-chain",
                "glaive-and-shield.linked.double-slash",
                "glaive-and-shield.opening.bash",
                "glaive-and-shield.main.guarded-chain-slash",
                "glaive-and-shield.closing.empowered-double-slash",
                "glaive-and-shield.linked.restraining-slash"
            };
            LearnActions(state, actionIds);

            loadout.SetCombo(0, CreatePreviewCombo(
                "Counter Resolution Window",
                actionIds[0], actionIds[1], actionIds[2], actionIds[3]));
            loadout.SetCombo(1, CreatePreviewCombo(
                "Guarded Advance",
                actionIds[4], actionIds[5], actionIds[6], actionIds[7]));
            loadout.SetCombo(2, CreatePreviewCombo(
                "Rising Chain",
                actionIds[8], actionIds[9], actionIds[10], actionIds[11]));
            loadout.SetCombo(3, CreatePreviewCombo(
                "Guarded Chain",
                actionIds[12], actionIds[13], actionIds[14], actionIds[15]));
        }

        private static void LearnAndConfigureCommonPreview(
            PersistentPlayerState state,
            PlayerComboLoadoutState loadout)
        {
            string[] actionIds =
            {
                "common.opening.roll",
                "common.main.focused-guard-1",
                "common.closing.breather-2",
                "common.linked.focused-guard",
                "common.opening.defensive-stance",
                "common.main.focused-guard-2",
                "common.closing.roll",
                "common.linked.jump",
                "common.opening.use-healing-potion",
                "common.main.use-healing-potion",
                "common.closing.use-healing-potion",
                "common.linked.breather-1",
                "common.opening.use-vitality-tonic",
                "common.main.use-vitality-tonic",
                "common.closing.use-vitality-tonic",
                "common.linked.use-vitality-tonic"
            };
            LearnActions(state, actionIds);

            loadout.SetCombo(0, CreatePreviewCombo(
                "Double Guard Window",
                actionIds[0], actionIds[1], actionIds[2], actionIds[3]));
            loadout.SetCombo(1, CreatePreviewCombo(
                "Shifted Guard Window",
                actionIds[4], actionIds[5], actionIds[6], actionIds[7]));
            loadout.SetCombo(2, CreatePreviewCombo(
                "Healing Sequence",
                actionIds[8], actionIds[9], actionIds[10], actionIds[11]));
            loadout.SetCombo(3, CreatePreviewCombo(
                "Vitality Sequence",
                actionIds[12], actionIds[13], actionIds[14], actionIds[15]));
        }

        private static PreparedComboState CreatePreviewCombo(
            string name,
            string openingActionId,
            string mainActionId,
            string closingActionId,
            string linkedActionId)
        {
            return new PreparedComboState(
                name,
                openingActionId,
                mainActionId,
                closingActionId,
                linkedActionId,
                PreparedLinkedActionPlacement.AfterMain);
        }

        private static void LearnActions(PersistentPlayerState state, string[] actionIds)
        {
            for (int index = 0; index < actionIds.Length; index++)
            {
                state.ActionLibrary.Learn(actionIds[index]);
            }
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
