using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.Weapons;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    [DisallowMultipleComponent]
    public sealed class ProcessingShopController : MonoBehaviour
    {
        private const string SelectedClass = "processing-selected";
        private const string SelectedTabClass = "processing-tab-selected";
        private const string UnavailableClass = "processing-unavailable";

        private enum BrowseDepth
        {
            Root,
            Equipment
        }

        private static ProcessingShopController _active;

        private static readonly EquipmentCategory[] Categories =
        {
            EquipmentCategory.Weapon,
            EquipmentCategory.Armor,
            EquipmentCategory.Accessory
        };

        private static readonly WeaponType[] WeaponTypes =
        {
            WeaponType.Greatsword,
            WeaponType.SwordAndShield,
            WeaponType.GlaiveAndShield
        };

        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<Button> _navigationButtons = new List<Button>();
        private readonly List<ProcessingRecipe> _visibleRecipes = new List<ProcessingRecipe>();
        private readonly int[] _rootSelectionByTab = new int[3];
        private readonly int[] _equipmentSelectionByTab = new int[3];
        private readonly BrowseDepth[] _depthByTab =
        {
            BrowseDepth.Root,
            BrowseDepth.Root,
            BrowseDepth.Equipment
        };

        private VisualElement _root;
        private VisualElement _navigationContainer;
        private ScrollView _navigationScroll;
        private Label _navigationTitle;
        private Label _navigationHint;
        private Button _navigationBackButton;
        private Label _goldLabel;
        private Label _recipeName;
        private Label _recipeStats;
        private Label _recipeEffect;
        private Label _recipeCosts;
        private Label _message;
        private Button _craftButton;
        private VisualElement _confirmOverlay;
        private Label _confirmPrompt;
        private Button _confirmButton;
        private Button _cancelButton;
        private PlayerEquipmentCatalogAsset _equipmentCatalog;
        private int _selectedTabIndex;
        private bool _confirmingCraft;
        private bool _confirmChoice;
        private bool _isOpen;
        private bool _stickLatched;
        private float _previousTimeScale;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private bool _previousInputGate;
        private ProcessingRecipe _pendingRecipe;

        private EquipmentCategory ActiveCategory => Categories[_selectedTabIndex];

        private BrowseDepth ActiveDepth
        {
            get => _depthByTab[_selectedTabIndex];
            set => _depthByTab[_selectedTabIndex] = value;
        }

        public static bool TryOpen()
        {
            if (_active == null || _active._isOpen || ExplorationMenuInputGate.IsOpen)
            {
                return false;
            }

            if (!PlayerStateService.TryGetActive(out PlayerStateService service)
                || service.State == null)
            {
                return false;
            }

            _active.Open();
            return true;
        }

        private void Awake()
        {
            if (_active != null && !ReferenceEquals(_active, this))
            {
                Destroy(gameObject);
                return;
            }

            _active = this;
            _equipmentCatalog = Resources.Load<PlayerEquipmentCatalogAsset>(
                "PlayerState/PlayerEquipmentCatalog");

            UIDocument document = GetComponent<UIDocument>();
            _root = document.rootVisualElement.Q<VisualElement>("processingShopOverlay");
            _navigationContainer = _root.Q<VisualElement>("navigationContainer");
            _navigationScroll = _root.Q<ScrollView>("navigationScroll");
            _navigationTitle = _root.Q<Label>("navigationTitle");
            _navigationHint = _root.Q<Label>("navigationHint");
            _navigationBackButton = _root.Q<Button>("navigationBackButton");
            _goldLabel = _root.Q<Label>("goldLabel");
            _recipeName = _root.Q<Label>("recipeName");
            _recipeStats = _root.Q<Label>("recipeStats");
            _recipeEffect = _root.Q<Label>("recipeEffect");
            _recipeCosts = _root.Q<Label>("recipeCosts");
            _message = _root.Q<Label>("processingMessage");
            _craftButton = _root.Q<Button>("craftButton");
            _confirmOverlay = _root.Q<VisualElement>("craftConfirmOverlay");
            _confirmPrompt = _root.Q<Label>("craftConfirmPrompt");
            _confirmButton = _root.Q<Button>("confirmCraftButton");
            _cancelButton = _root.Q<Button>("cancelCraftButton");

            _tabButtons.Add(_root.Q<Button>("weaponsTab"));
            _tabButtons.Add(_root.Q<Button>("armorTab"));
            _tabButtons.Add(_root.Q<Button>("accessoriesTab"));
            for (int index = 0; index < _tabButtons.Count; index++)
            {
                int captured = index;
                _tabButtons[index].clicked += () => SelectTab(captured);
            }

            _root.Q<Button>("closeButton").clicked += Close;
            _navigationBackButton.clicked += NavigateBackOrClose;
            _craftButton.clicked += BeginCraftConfirmation;
            _confirmButton.clicked += ConfirmCraft;
            _cancelButton.clicked += CancelCraft;
            _root.style.display = DisplayStyle.None;
            _confirmOverlay.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (_isOpen)
            {
                Close();
            }

            if (ReferenceEquals(_active, this))
            {
                _active = null;
            }
        }

        private void Update()
        {
            if (!_isOpen)
            {
                return;
            }

            bool backPressed = (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                || (Gamepad.current != null
                    && (Gamepad.current.buttonEast.wasPressedThisFrame
                        || Gamepad.current.startButton.wasPressedThisFrame));
            bool confirmPressed = (Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame
                        || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                        || Keyboard.current.fKey.wasPressedThisFrame))
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (_confirmingCraft)
            {
                if (backPressed)
                {
                    CancelCraft();
                    return;
                }

                if (ReadConfirmationNavigation())
                {
                    _confirmChoice = !_confirmChoice;
                    RefreshConfirmSelection();
                }

                if (confirmPressed)
                {
                    if (_confirmChoice) ConfirmCraft();
                    else CancelCraft();
                }
                return;
            }

            int tabDirection = ReadTabNavigation();
            if (tabDirection != 0)
            {
                SelectTab(_selectedTabIndex + tabDirection);
                return;
            }

            if (backPressed)
            {
                NavigateBackOrClose();
                return;
            }

            int vertical = ReadVerticalNavigation();
            if (vertical != 0 && _navigationButtons.Count > 0)
            {
                int selection = ActiveDepth == BrowseDepth.Root
                    ? _rootSelectionByTab[_selectedTabIndex]
                    : _equipmentSelectionByTab[_selectedTabIndex];
                SelectNavigation(selection + vertical, true);
            }

            if (confirmPressed)
            {
                ConfirmCurrentSelection();
            }
        }

        private void Open()
        {
            _previousTimeScale = Time.timeScale;
            _previousCursorLockMode = UnityEngine.Cursor.lockState;
            _previousCursorVisible = UnityEngine.Cursor.visible;
            _previousInputGate = ExplorationMenuInputGate.IsOpen;
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            ExplorationMenuInputGate.IsOpen = true;
            _isOpen = true;
            _selectedTabIndex = 0;
            Array.Clear(_rootSelectionByTab, 0, _rootSelectionByTab.Length);
            Array.Clear(_equipmentSelectionByTab, 0, _equipmentSelectionByTab.Length);
            _depthByTab[0] = BrowseDepth.Root;
            _depthByTab[1] = BrowseDepth.Root;
            _depthByTab[2] = BrowseDepth.Equipment;
            _stickLatched = false;
            _confirmingCraft = false;
            _confirmChoice = false;
            _pendingRecipe = null;
            _confirmOverlay.style.display = DisplayStyle.None;
            _message.text = "Choose equipment to process.";
            _root.style.display = DisplayStyle.Flex;
            SelectTab(0);
        }

        private void Close()
        {
            _confirmingCraft = false;
            _confirmOverlay.style.display = DisplayStyle.None;
            _root.style.display = DisplayStyle.None;
            _isOpen = false;
            ExplorationMenuInputGate.IsOpen = _previousInputGate;
            Time.timeScale = _previousTimeScale;
            UnityEngine.Cursor.lockState = _previousCursorLockMode;
            UnityEngine.Cursor.visible = _previousCursorVisible;
        }

        private void SelectTab(int index)
        {
            _selectedTabIndex = Wrap(index, Categories.Length);
            if (ActiveCategory == EquipmentCategory.Accessory)
            {
                ActiveDepth = BrowseDepth.Equipment;
            }
            BuildNavigation();
            RefreshTabClasses();
            FocusSelectedButton();
        }

        private void BuildNavigation()
        {
            _navigationButtons.Clear();
            _visibleRecipes.Clear();
            _navigationContainer.Clear();

            bool hasBack = ActiveDepth == BrowseDepth.Equipment
                && ActiveCategory != EquipmentCategory.Accessory;
            _navigationBackButton.style.display = hasBack
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (ActiveDepth == BrowseDepth.Root)
            {
                BuildRootNavigation();
                ClearDetails();
            }
            else
            {
                BuildEquipmentNavigation();
            }

            RefreshSelectionClasses();
            RefreshGold();
        }

        private void BuildRootNavigation()
        {
            if (ActiveCategory == EquipmentCategory.Weapon)
            {
                _navigationTitle.text = "Weapon Type";
                _navigationHint.text = "Choose a weapon type to view its available series.";
                for (int index = 0; index < WeaponTypes.Length; index++)
                {
                    int captured = index;
                    AddNavigationButton(FormatWeaponType(WeaponTypes[index]), () =>
                    {
                        _rootSelectionByTab[_selectedTabIndex] = captured;
                        EnterSelectedGroup();
                    });
                }
            }
            else
            {
                _navigationTitle.text = "Armor Series";
                _navigationHint.text = "Choose a series to view its head, upper, and lower armor.";
                IReadOnlyList<string> seriesNames = GetArmorSeriesNames();
                for (int index = 0; index < seriesNames.Count; index++)
                {
                    int captured = index;
                    AddNavigationButton(seriesNames[index], () =>
                    {
                        _rootSelectionByTab[_selectedTabIndex] = captured;
                        EnterSelectedGroup();
                    });
                }
            }

            NormalizeSelection(_rootSelectionByTab, _navigationButtons.Count);
        }

        private void BuildEquipmentNavigation()
        {
            PopulateVisibleRecipes();
            switch (ActiveCategory)
            {
                case EquipmentCategory.Weapon:
                    WeaponType weaponType = WeaponTypes[Wrap(
                        _rootSelectionByTab[_selectedTabIndex],
                        WeaponTypes.Length)];
                    _navigationTitle.text = FormatWeaponType(weaponType);
                    _navigationHint.text = "Choose a series weapon to inspect its recipe.";
                    break;
                case EquipmentCategory.Armor:
                    IReadOnlyList<string> seriesNames = GetArmorSeriesNames();
                    _navigationTitle.text = seriesNames.Count > 0
                        ? seriesNames[Wrap(_rootSelectionByTab[_selectedTabIndex], seriesNames.Count)]
                        : "Armor";
                    _navigationHint.text = "Choose an armor part to inspect its recipe.";
                    break;
                default:
                    _navigationTitle.text = "Accessories";
                    _navigationHint.text = "Choose an accessory to inspect its recipe.";
                    break;
            }

            for (int index = 0; index < _visibleRecipes.Count; index++)
            {
                int captured = index;
                Button button = AddNavigationButton(string.Empty, () => SelectNavigation(captured, true));
                button.AddToClassList("processing-equipment-button");
            }

            NormalizeSelection(_equipmentSelectionByTab, _navigationButtons.Count);
            RefreshRecipeCards();
            if (_visibleRecipes.Count > 0)
            {
                RefreshDetails(_visibleRecipes[_equipmentSelectionByTab[_selectedTabIndex]]);
            }
            else
            {
                ClearDetails();
            }
        }

        private Button AddNavigationButton(string text, Action clicked)
        {
            var button = new Button(clicked) { text = text };
            button.AddToClassList("processing-navigation-button");
            _navigationContainer.Add(button);
            _navigationButtons.Add(button);
            return button;
        }

        private void PopulateVisibleRecipes()
        {
            IReadOnlyList<string> armorSeries = ActiveCategory == EquipmentCategory.Armor
                ? GetArmorSeriesNames()
                : Array.Empty<string>();
            string selectedArmorSeries = armorSeries.Count > 0
                ? armorSeries[Wrap(_rootSelectionByTab[_selectedTabIndex], armorSeries.Count)]
                : string.Empty;
            WeaponType selectedWeaponType = WeaponTypes[Wrap(
                _rootSelectionByTab[_selectedTabIndex],
                WeaponTypes.Length)];

            foreach (ProcessingRecipe recipe in ProcessingRecipeCatalog.All)
            {
                if (recipe.Category != ActiveCategory)
                {
                    continue;
                }

                if (ActiveCategory == EquipmentCategory.Weapon)
                {
                    if (TryGetWeapon(recipe, out WeaponDefinitionAsset weapon)
                        && weapon.WeaponType == selectedWeaponType)
                    {
                        _visibleRecipes.Add(recipe);
                    }
                }
                else if (ActiveCategory == EquipmentCategory.Armor)
                {
                    if (string.Equals(
                        recipe.SeriesDisplayName,
                        selectedArmorSeries,
                        StringComparison.Ordinal))
                    {
                        _visibleRecipes.Add(recipe);
                    }
                }
                else
                {
                    _visibleRecipes.Add(recipe);
                }
            }
        }

        private IReadOnlyList<string> GetArmorSeriesNames()
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ProcessingRecipe recipe in ProcessingRecipeCatalog.All)
            {
                if (recipe.Category == EquipmentCategory.Armor
                    && seen.Add(recipe.SeriesDisplayName))
                {
                    names.Add(recipe.SeriesDisplayName);
                }
            }
            return names;
        }

        private void EnterSelectedGroup()
        {
            if (ActiveCategory == EquipmentCategory.Accessory)
            {
                return;
            }

            ActiveDepth = BrowseDepth.Equipment;
            BuildNavigation();
            FocusSelectedButton();
        }

        private void NavigateBackOrClose()
        {
            if (ActiveDepth == BrowseDepth.Equipment
                && ActiveCategory != EquipmentCategory.Accessory)
            {
                ActiveDepth = BrowseDepth.Root;
                BuildNavigation();
                FocusSelectedButton();
                return;
            }

            Close();
        }

        private void ConfirmCurrentSelection()
        {
            if (ActiveDepth == BrowseDepth.Root)
            {
                EnterSelectedGroup();
            }
            else
            {
                BeginCraftConfirmation();
            }
        }

        private void SelectNavigation(int index, bool focus)
        {
            if (_navigationButtons.Count == 0)
            {
                return;
            }

            int wrapped = Wrap(index, _navigationButtons.Count);
            if (ActiveDepth == BrowseDepth.Root)
            {
                _rootSelectionByTab[_selectedTabIndex] = wrapped;
            }
            else
            {
                _equipmentSelectionByTab[_selectedTabIndex] = wrapped;
                RefreshRecipeCards();
                RefreshDetails(_visibleRecipes[wrapped]);
            }

            RefreshSelectionClasses();
            if (focus)
            {
                FocusSelectedButton();
            }
        }

        private void RefreshRecipeCards()
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            for (int index = 0; index < _visibleRecipes.Count; index++)
            {
                ProcessingRecipe recipe = _visibleRecipes[index];
                bool owned = service.State.Equipment.Owns(
                    recipe.Category,
                    recipe.EquipmentId);
                _navigationButtons[index].text = owned
                    ? recipe.DisplayName + "\nOWNED"
                    : recipe.DisplayName + $"\n{recipe.GoldCost} Gold";
                _navigationButtons[index].EnableInClassList(UnavailableClass, owned);
            }
        }

        private void RefreshDetails(ProcessingRecipe recipe)
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            PersistentPlayerState state = service.State;
            bool owned = state.Equipment.Owns(recipe.Category, recipe.EquipmentId);
            _goldLabel.text = $"Gold  {state.Currencies.Gold}";
            _recipeName.text = recipe.DisplayName;
            PopulateEquipmentDetails(recipe);
            _recipeCosts.text = FormatCosts(state, recipe);
            _craftButton.text = owned ? "Already Owned" : $"Process {FormatCategory(recipe.Category)}";
            _craftButton.SetEnabled(!owned && CanAfford(state, recipe));
            RefreshRecipeCards();
        }

        private void ClearDetails()
        {
            RefreshGold();
            _recipeName.text = ActiveCategory == EquipmentCategory.Weapon
                ? "Select a Weapon Type"
                : "Select an Armor Series";
            _recipeStats.text = string.Empty;
            _recipeEffect.text = "Choose a category on the left to view equipment details.";
            _recipeCosts.text = string.Empty;
            _craftButton.text = "Process Equipment";
            _craftButton.SetEnabled(false);
        }

        private void RefreshGold()
        {
            if (PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                _goldLabel.text = $"Gold  {service.State.Currencies.Gold}";
            }
        }

        private bool TryGetArmor(ProcessingRecipe recipe, out ArmorDefinitionAsset armor)
        {
            armor = null;
            return _equipmentCatalog != null
                && _equipmentCatalog.TryGetArmor(recipe.EquipmentId, out armor, out _);
        }

        private bool TryGetWeapon(ProcessingRecipe recipe, out WeaponDefinitionAsset weapon)
        {
            weapon = null;
            if (_equipmentCatalog == null)
            {
                return false;
            }

            foreach (WeaponDefinitionAsset candidate in _equipmentCatalog.Weapons)
            {
                if (candidate != null
                    && string.Equals(candidate.WeaponId, recipe.EquipmentId, StringComparison.Ordinal))
                {
                    weapon = candidate;
                    return true;
                }
            }
            return false;
        }

        private bool TryGetAccessory(ProcessingRecipe recipe, out AccessoryDefinitionAsset accessory)
        {
            accessory = null;
            return _equipmentCatalog != null
                && _equipmentCatalog.TryGetAccessory(recipe.EquipmentId, out accessory, out _);
        }

        private void PopulateEquipmentDetails(ProcessingRecipe recipe)
        {
            switch (recipe.Category)
            {
                case EquipmentCategory.Weapon:
                    if (TryGetWeapon(recipe, out WeaponDefinitionAsset weapon))
                    {
                        _recipeStats.text = EquipmentPresentationText.FormatWeaponStats(weapon);
                        _recipeEffect.text = "Weapon attack power and compatible action type.";
                    }
                    else
                    {
                        _recipeStats.text = "Weapon data unavailable.";
                        _recipeEffect.text = string.Empty;
                    }
                    break;
                case EquipmentCategory.Accessory:
                    _recipeStats.text = "Slot  Accessory";
                    _recipeEffect.text = TryGetAccessory(recipe, out AccessoryDefinitionAsset accessory)
                        ? EquipmentPresentationText.FormatAccessoryEffect(accessory)
                        : "Accessory effect data unavailable.";
                    break;
                default:
                    _recipeStats.text = TryGetArmor(recipe, out ArmorDefinitionAsset armor)
                        ? $"DEF  {armor.Defense}     Slot  {FormatSlot(recipe.SlotId)}"
                        : $"Slot  {FormatSlot(recipe.SlotId)}";
                    _recipeEffect.text = TryGetArmor(recipe, out armor)
                        ? EquipmentPresentationText.FormatArmorEffect(armor)
                        : "Armor effect data unavailable.";
                    break;
            }
        }

        private static string FormatCosts(PersistentPlayerState state, ProcessingRecipe recipe)
        {
            var lines = new List<string>
            {
                $"Gold: {state.Currencies.Gold} / {recipe.GoldCost}"
            };
            foreach (ProcessingMaterialCost cost in recipe.MaterialCosts)
            {
                lines.Add($"{cost.DisplayName}: {state.Inventory.GetCount(cost.ItemId)} / {cost.Amount}");
            }
            return string.Join("\n", lines);
        }

        private void BeginCraftConfirmation()
        {
            if (ActiveDepth != BrowseDepth.Equipment
                || _visibleRecipes.Count == 0
                || !PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            int selected = _equipmentSelectionByTab[_selectedTabIndex];
            ProcessingRecipe recipe = _visibleRecipes[selected];
            if (!CanAfford(service.State, recipe)
                || service.State.Equipment.Owns(recipe.Category, recipe.EquipmentId))
            {
                _message.text = "This equipment cannot currently be processed.";
                return;
            }

            _pendingRecipe = recipe;
            _confirmingCraft = true;
            _confirmChoice = false;
            _confirmPrompt.text = $"Process {recipe.DisplayName}?\n\n{FormatCosts(service.State, recipe)}";
            _confirmOverlay.style.display = DisplayStyle.Flex;
            RefreshConfirmSelection();
        }

        private void ConfirmCraft()
        {
            _confirmingCraft = false;
            _confirmOverlay.style.display = DisplayStyle.None;
            if (_pendingRecipe == null
                || !PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            if (ProcessingShopService.TryCraft(service.State, _pendingRecipe, out ProcessingCraftFailure failure))
            {
                _message.text = $"Processed {_pendingRecipe.DisplayName}. It is now available in Equipment.";
            }
            else
            {
                _message.text = FormatFailure(failure);
            }

            RefreshDetails(_pendingRecipe);
            _pendingRecipe = null;
        }

        private void CancelCraft()
        {
            _confirmingCraft = false;
            _confirmOverlay.style.display = DisplayStyle.None;
            _pendingRecipe = null;
            _message.text = "Processing cancelled.";
        }

        private void RefreshConfirmSelection()
        {
            _confirmButton.EnableInClassList(SelectedClass, _confirmChoice);
            _cancelButton.EnableInClassList(SelectedClass, !_confirmChoice);
        }

        private void RefreshTabClasses()
        {
            for (int index = 0; index < _tabButtons.Count; index++)
            {
                _tabButtons[index].EnableInClassList(
                    SelectedTabClass,
                    index == _selectedTabIndex);
            }
        }

        private void RefreshSelectionClasses()
        {
            int selected = ActiveDepth == BrowseDepth.Root
                ? _rootSelectionByTab[_selectedTabIndex]
                : _equipmentSelectionByTab[_selectedTabIndex];
            for (int index = 0; index < _navigationButtons.Count; index++)
            {
                _navigationButtons[index].EnableInClassList(
                    SelectedClass,
                    index == selected);
            }
        }

        private void FocusSelectedButton()
        {
            if (_navigationButtons.Count == 0)
            {
                return;
            }

            int selected = ActiveDepth == BrowseDepth.Root
                ? _rootSelectionByTab[_selectedTabIndex]
                : _equipmentSelectionByTab[_selectedTabIndex];
            Button button = _navigationButtons[Wrap(selected, _navigationButtons.Count)];
            button.Focus();
            _navigationScroll.schedule.Execute(() => _navigationScroll.ScrollTo(button)).ExecuteLater(1);
        }

        private void NormalizeSelection(int[] selections, int count)
        {
            selections[_selectedTabIndex] = count > 0
                ? Wrap(selections[_selectedTabIndex], count)
                : 0;
        }

        private static bool CanAfford(PersistentPlayerState state, ProcessingRecipe recipe)
        {
            if (state.Currencies.Gold < recipe.GoldCost)
            {
                return false;
            }
            foreach (ProcessingMaterialCost cost in recipe.MaterialCosts)
            {
                if (state.Inventory.GetCount(cost.ItemId) < cost.Amount)
                {
                    return false;
                }
            }
            return true;
        }

        private static string FormatFailure(ProcessingCraftFailure failure)
        {
            switch (failure)
            {
                case ProcessingCraftFailure.AlreadyOwned:
                    return "That equipment is already owned.";
                case ProcessingCraftFailure.InsufficientGold:
                    return "Not enough Gold.";
                case ProcessingCraftFailure.InsufficientMaterials:
                    return "Required materials are missing.";
                default:
                    return "Processing failed.";
            }
        }

        private static string FormatSlot(string slotId)
        {
            switch (slotId)
            {
                case "armor.head": return "Head";
                case "armor.upper-body": return "Upper Body";
                case "armor.lower-body": return "Lower Body";
                default: return slotId;
            }
        }

        private static string FormatCategory(EquipmentCategory category)
        {
            switch (category)
            {
                case EquipmentCategory.Armor: return "Armor";
                case EquipmentCategory.Weapon: return "Weapon";
                case EquipmentCategory.Accessory: return "Accessory";
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

        private static string FormatWeaponType(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Greatsword: return "Greatsword";
                case WeaponType.SwordAndShield: return "Sword and Shield";
                case WeaponType.GlaiveAndShield: return "Glaive and Shield";
                default: return weaponType.ToString();
            }
        }

        private int ReadTabNavigation()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame) return -1;
                if (Keyboard.current.eKey.wasPressedThisFrame) return 1;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.leftShoulder.wasPressedThisFrame) return -1;
                if (gamepad.rightShoulder.wasPressedThisFrame) return 1;
            }
            return 0;
        }

        private int ReadVerticalNavigation()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame
                    || Keyboard.current.wKey.wasPressedThisFrame) return -1;
                if (Keyboard.current.downArrowKey.wasPressedThisFrame
                    || Keyboard.current.sKey.wasPressedThisFrame) return 1;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) return 0;
            if (gamepad.dpad.up.wasPressedThisFrame) return -1;
            if (gamepad.dpad.down.wasPressedThisFrame) return 1;

            float primary = -gamepad.leftStick.ReadValue().y;
            if (Mathf.Abs(primary) < 0.25f)
            {
                _stickLatched = false;
                return 0;
            }
            if (_stickLatched || Mathf.Abs(primary) < 0.65f) return 0;
            _stickLatched = true;
            return primary < 0f ? -1 : 1;
        }

        private bool ReadConfirmationNavigation()
        {
            if (Keyboard.current != null
                && (Keyboard.current.upArrowKey.wasPressedThisFrame
                    || Keyboard.current.downArrowKey.wasPressedThisFrame
                    || Keyboard.current.leftArrowKey.wasPressedThisFrame
                    || Keyboard.current.rightArrowKey.wasPressedThisFrame
                    || Keyboard.current.wKey.wasPressedThisFrame
                    || Keyboard.current.sKey.wasPressedThisFrame
                    || Keyboard.current.aKey.wasPressedThisFrame
                    || Keyboard.current.dKey.wasPressedThisFrame))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return false;
            }
            if (gamepad.dpad.up.wasPressedThisFrame
                || gamepad.dpad.down.wasPressedThisFrame
                || gamepad.dpad.left.wasPressedThisFrame
                || gamepad.dpad.right.wasPressedThisFrame)
            {
                return true;
            }

            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude < 0.25f)
            {
                _stickLatched = false;
                return false;
            }
            if (_stickLatched || stick.magnitude < 0.65f)
            {
                return false;
            }
            _stickLatched = true;
            return true;
        }

        private static int Wrap(int value, int length)
        {
            return (value % length + length) % length;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = null;
        }
    }
}
