using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Authoring.Weapons;
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
        private readonly Dictionary<Button, Action> _buttonActions =
            new Dictionary<Button, Action>();

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
            _content.Clear();
            AddBodyText(ExplorationMenuText.Get("combos.note"), "page-note");
            foreach (PlayerComboLoadoutState loadout in state.ComboLoadouts.Loadouts)
            {
                var card = new VisualElement();
                card.AddToClassList("loadout-card");
                var heading = new Label(loadout.Name);
                heading.AddToClassList("card-title");
                card.Add(heading);
                for (int index = 0; index < loadout.Combos.Count; index++)
                {
                    card.Add(new Label($"{index + 1}. {loadout.Combos[index].Name}"));
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

        private void OnCombosClicked() => ShowPageWhenChanged(MenuPage.Combos);

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
    }
}
