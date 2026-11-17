using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Equipment;
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
        private const string UnavailableClass = "processing-unavailable";

        private static ProcessingShopController _active;

        private readonly List<string> _seriesNames = new List<string>();
        private readonly List<Button> _seriesButtons = new List<Button>();
        private readonly List<ProcessingRecipe> _visibleRecipes = new List<ProcessingRecipe>();
        private readonly List<Button> _recipeButtons = new List<Button>();

        private VisualElement _root;
        private VisualElement _seriesContainer;
        private VisualElement _recipeContainer;
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
        private int _selectedSeriesIndex;
        private int _selectedRecipeIndex;
        private bool _recipeColumnActive;
        private bool _confirmingCraft;
        private bool _confirmChoice;
        private bool _isOpen;
        private bool _stickLatched;
        private float _previousTimeScale;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private bool _previousInputGate;
        private ProcessingRecipe _pendingRecipe;

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
            _seriesContainer = _root.Q<VisualElement>("seriesContainer");
            _recipeContainer = _root.Q<VisualElement>("recipeContainer");
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

            _root.Q<Button>("closeButton").clicked += Close;
            _craftButton.clicked += BeginCraftConfirmation;
            _confirmButton.clicked += ConfirmCraft;
            _cancelButton.clicked += CancelCraft;
            _root.style.display = DisplayStyle.None;
            _confirmOverlay.style.display = DisplayStyle.None;
            BuildSeriesButtons();
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
            if (backPressed)
            {
                if (_confirmingCraft)
                {
                    CancelCraft();
                }
                else
                {
                    Close();
                }
                return;
            }

            int vertical = ReadVerticalNavigation();
            int horizontal = ReadHorizontalNavigation();
            bool confirmPressed = (Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame
                        || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                        || Keyboard.current.fKey.wasPressedThisFrame))
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (_confirmingCraft)
            {
                if (vertical != 0 || horizontal != 0)
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

            if (horizontal < 0)
            {
                _recipeColumnActive = false;
            }
            else if (horizontal > 0 && _visibleRecipes.Count > 0)
            {
                _recipeColumnActive = true;
            }

            if (vertical != 0)
            {
                if (_recipeColumnActive && _visibleRecipes.Count > 0)
                {
                    _selectedRecipeIndex = Wrap(
                        _selectedRecipeIndex + vertical,
                        _visibleRecipes.Count);
                    SelectRecipe(_selectedRecipeIndex);
                }
                else if (_seriesNames.Count > 0)
                {
                    _selectedSeriesIndex = Wrap(
                        _selectedSeriesIndex + vertical,
                        _seriesNames.Count);
                    SelectSeries(_selectedSeriesIndex);
                }
            }

            if (confirmPressed)
            {
                if (_recipeColumnActive)
                {
                    BeginCraftConfirmation();
                }
                else if (_visibleRecipes.Count > 0)
                {
                    _recipeColumnActive = true;
                }
            }

            RefreshSelectionClasses();
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
            _recipeColumnActive = false;
            _selectedSeriesIndex = 0;
            _selectedRecipeIndex = 0;
            _message.text = "Choose a series, then select one armor piece to process.";
            _root.style.display = DisplayStyle.Flex;
            SelectSeries(0);
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

        private void BuildSeriesButtons()
        {
            _seriesNames.Clear();
            _seriesButtons.Clear();
            _seriesContainer.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ProcessingRecipe recipe in ProcessingRecipeCatalog.All)
            {
                if (seen.Add(recipe.SeriesDisplayName))
                {
                    _seriesNames.Add(recipe.SeriesDisplayName);
                }
            }

            for (int index = 0; index < _seriesNames.Count; index++)
            {
                int captured = index;
                var button = new Button(() => SelectSeries(captured))
                {
                    text = _seriesNames[index]
                };
                button.AddToClassList("processing-series-button");
                _seriesContainer.Add(button);
                _seriesButtons.Add(button);
            }
        }

        private void SelectSeries(int index)
        {
            if (_seriesNames.Count == 0)
            {
                return;
            }

            _selectedSeriesIndex = Wrap(index, _seriesNames.Count);
            _selectedRecipeIndex = 0;
            _visibleRecipes.Clear();
            _recipeButtons.Clear();
            _recipeContainer.Clear();
            string series = _seriesNames[_selectedSeriesIndex];
            foreach (ProcessingRecipe recipe in ProcessingRecipeCatalog.All)
            {
                if (string.Equals(recipe.SeriesDisplayName, series, StringComparison.Ordinal))
                {
                    _visibleRecipes.Add(recipe);
                }
            }

            for (int recipeIndex = 0; recipeIndex < _visibleRecipes.Count; recipeIndex++)
            {
                int captured = recipeIndex;
                ProcessingRecipe recipe = _visibleRecipes[recipeIndex];
                var button = new Button(() =>
                {
                    _recipeColumnActive = true;
                    SelectRecipe(captured);
                });
                button.AddToClassList("processing-recipe-button");
                _recipeContainer.Add(button);
                _recipeButtons.Add(button);
            }

            if (_visibleRecipes.Count > 0)
            {
                SelectRecipe(0);
            }
            RefreshSelectionClasses();
        }

        private void SelectRecipe(int index)
        {
            if (_visibleRecipes.Count == 0)
            {
                return;
            }

            _selectedRecipeIndex = Wrap(index, _visibleRecipes.Count);
            RefreshRecipeCards();
            RefreshDetails(_visibleRecipes[_selectedRecipeIndex]);
            RefreshSelectionClasses();
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
                    EquipmentCategory.Armor,
                    recipe.EquipmentId);
                _recipeButtons[index].text = owned
                    ? recipe.DisplayName + "\nOWNED"
                    : recipe.DisplayName + $"\n{recipe.GoldCost} Gold";
                _recipeButtons[index].EnableInClassList(UnavailableClass, owned);
            }
        }

        private void RefreshDetails(ProcessingRecipe recipe)
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            PersistentPlayerState state = service.State;
            bool owned = state.Equipment.Owns(EquipmentCategory.Armor, recipe.EquipmentId);
            _goldLabel.text = $"Gold  {state.Currencies.Gold}";
            _recipeName.text = recipe.DisplayName;
            _recipeStats.text = TryGetArmor(recipe, out ArmorDefinitionAsset armor)
                ? $"DEF  {armor.Defense}     Slot  {FormatSlot(recipe.SlotId)}"
                : $"Slot  {FormatSlot(recipe.SlotId)}";
            _recipeEffect.text = TryGetArmor(recipe, out armor)
                ? FormatEffect(armor)
                : "Armor effect data will be available after content generation.";
            _recipeCosts.text = FormatCosts(state, recipe);
            _craftButton.text = owned ? "Already Owned" : "Process Armor";
            _craftButton.SetEnabled(!owned && CanAfford(state, recipe));
            RefreshRecipeCards();
        }

        private bool TryGetArmor(ProcessingRecipe recipe, out ArmorDefinitionAsset armor)
        {
            armor = null;
            return _equipmentCatalog != null
                && _equipmentCatalog.TryGetArmor(recipe.EquipmentId, out armor, out _);
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

        private static string FormatEffect(ArmorDefinitionAsset armor)
        {
            int percent = (int)Math.Round(armor.EffectAmount * 100f, MidpointRounding.AwayFromZero);
            switch (armor.EffectType)
            {
                case ArmorEffectType.None:
                    switch (armor.SeriesId)
                    {
                        case "armor-series.yellow-silk":
                            return "Full set: Gold earned after battle +20%.";
                        case "armor-series.red-silk":
                            return "Full set: one additional material reward roll.";
                        default:
                            return "No individual armor effect.";
                    }
                case ArmorEffectType.DamageTakenMultiplier:
                    return $"{armor.EffectDamageType} damage taken -{percent}%."
                        + (armor.SeriesId == "armor-series.pure-gold"
                            ? "\nFull set: successful Clash damage +50%."
                            : string.Empty);
                case ArmorEffectType.MaximumHealthMultiplier:
                    return $"Maximum HP +{percent}%.";
                case ArmorEffectType.HealingPotionMultiplier:
                    return $"Healing and Greater Healing Potion recovery +{percent}%.";
                case ArmorEffectType.GuardCounterPiercingMultiplier:
                    return $"Successful Guard deals an extra {percent}% Piercing damage.";
                case ArmorEffectType.AllDamageDealtMultiplier:
                    return $"All damage dealt +{percent}%."
                        + "\nFull set: all damage dealt +10%.";
                default:
                    return "Unknown armor effect.";
            }
        }

        private void BeginCraftConfirmation()
        {
            if (_visibleRecipes.Count == 0
                || !PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            ProcessingRecipe recipe = _visibleRecipes[_selectedRecipeIndex];
            if (!CanAfford(service.State, recipe)
                || service.State.Equipment.Owns(EquipmentCategory.Armor, recipe.EquipmentId))
            {
                _message.text = "This armor cannot currently be processed.";
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

        private void RefreshSelectionClasses()
        {
            for (int index = 0; index < _seriesButtons.Count; index++)
            {
                _seriesButtons[index].EnableInClassList(
                    SelectedClass,
                    !_recipeColumnActive && index == _selectedSeriesIndex);
            }
            for (int index = 0; index < _recipeButtons.Count; index++)
            {
                _recipeButtons[index].EnableInClassList(
                    SelectedClass,
                    _recipeColumnActive && index == _selectedRecipeIndex);
            }
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
                    return "That armor is already owned.";
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

        private int ReadVerticalNavigation()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) return -1;
                if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) return 1;
            }
            return ReadGamepadAxis(true);
        }

        private int ReadHorizontalNavigation()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) return -1;
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) return 1;
            }
            return ReadGamepadAxis(false);
        }

        private int ReadGamepadAxis(bool vertical)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) return 0;
            if (vertical)
            {
                if (gamepad.dpad.up.wasPressedThisFrame) return -1;
                if (gamepad.dpad.down.wasPressedThisFrame) return 1;
            }
            else
            {
                if (gamepad.dpad.left.wasPressedThisFrame) return -1;
                if (gamepad.dpad.right.wasPressedThisFrame) return 1;
            }

            Vector2 stick = gamepad.leftStick.ReadValue();
            float primary = vertical ? -stick.y : stick.x;
            if (Mathf.Abs(primary) < 0.25f)
            {
                _stickLatched = false;
                return 0;
            }
            if (_stickLatched || Mathf.Abs(primary) < 0.65f) return 0;
            _stickLatched = true;
            return primary < 0f ? -1 : 1;
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
