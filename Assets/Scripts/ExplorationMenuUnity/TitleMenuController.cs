using System;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    public sealed class TitleMenuController : MonoBehaviour
    {
        private const string MainSelectedClass = "title-selected";
        private const string SlotSelectedClass = "load-slot-selected";

        private enum Page { Main, SaveSlots, Settings, DeleteSlots, DeleteConfirmation }

        private readonly Button[] _mainButtons = new Button[4];
        private readonly Button[] _slotButtons = new Button[PlayerSaveStorage.SlotCount];
        private readonly Button[] _deleteSlotButtons = new Button[PlayerSaveStorage.SlotCount];
        private readonly Button[] _confirmationButtons = new Button[2];
        private VisualElement _mainPage;
        private VisualElement _loadPage;
        private VisualElement _settingsPage;
        private VisualElement _deleteSavePage;
        private VisualElement _deleteConfirmationOverlay;
        private Label _message;
        private Label _deleteMessage;
        private Label _slotPageHeading;
        private Label _deleteConfirmationTitle;
        private Label _deleteConfirmationMessage;
        private Button _deleteSaveButton;
        private Page _page;
        private int _mainIndex;
        private int _slotIndex;
        private int _deleteSlotIndex;
        private int _confirmationIndex;
        private int _deleteConfirmationStage;
        private int _pendingDeleteSlot;
        private bool _startingNewGame;
        private int _overwriteConfirmationSlot = -1;
        private bool _stickLatched;
        private Vector2 _lastMousePosition;
        private bool _hasMousePosition;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _mainPage = root.Q<VisualElement>("mainMenuPage");
            _loadPage = root.Q<VisualElement>("loadGamePage");
            _settingsPage = root.Q<VisualElement>("settingsPage");
            _deleteSavePage = root.Q<VisualElement>("deleteSavePage");
            _deleteConfirmationOverlay = root.Q<VisualElement>("deleteConfirmationOverlay");
            _message = root.Q<Label>("loadMessage");
            _deleteMessage = root.Q<Label>("deleteMessage");
            _slotPageHeading = root.Q<Label>("slotPageHeading");
            _deleteConfirmationTitle = root.Q<Label>("deleteConfirmationTitle");
            _deleteConfirmationMessage = root.Q<Label>("deleteConfirmationMessage");
            _deleteSaveButton = root.Q<Button>("deleteSaveDataButton");

            _mainButtons[0] = root.Q<Button>("newGameButton");
            _mainButtons[1] = root.Q<Button>("loadGameButton");
            _mainButtons[2] = root.Q<Button>("settingsButton");
            _mainButtons[3] = root.Q<Button>("quitGameButton");

            for (int index = 0; index < PlayerSaveStorage.SlotCount; index++)
            {
                int captured = index;
                _slotButtons[index] = root.Q<Button>($"loadSlot{index + 1}");
                _slotButtons[index].clicked += () => ChooseSlot(captured);
                RegisterPointerSelection(_slotButtons[index], () => SelectSlot(captured));
                _deleteSlotButtons[index] = root.Q<Button>($"deleteSlot{index + 1}");
                _deleteSlotButtons[index].clicked += () => ChooseDeleteSlot(captured);
                RegisterPointerSelection(_deleteSlotButtons[index], () => SelectDeleteSlot(captured));
            }

            _confirmationButtons[0] = root.Q<Button>("confirmDeleteButton");
            _confirmationButtons[1] = root.Q<Button>("cancelDeleteButton");
            _mainButtons[0].clicked += ShowNewGameSlots;
            _mainButtons[1].clicked += ShowLoadPage;
            _mainButtons[2].clicked += ShowSettingsPage;
            _mainButtons[3].clicked += QuitGame;
            root.Q<Button>("backToMainButton").clicked += ShowMainPage;
            root.Q<Button>("backFromSettingsButton").clicked += ShowMainPage;
            root.Q<Button>("backFromDeleteButton").clicked += ShowSettingsPage;
            _deleteSaveButton.clicked += ShowDeleteSavePage;
            _confirmationButtons[0].clicked += ConfirmDeleteStage;
            _confirmationButtons[1].clicked += CloseDeleteConfirmation;

            for (int index = 0; index < _mainButtons.Length; index++)
            {
                int captured = index;
                RegisterPointerSelection(_mainButtons[index], () => SelectMain(captured));
            }
            RegisterPointerSelection(_deleteSaveButton, SelectSettings);
            RegisterPointerSelection(_confirmationButtons[0], () => SelectConfirmation(0));
            RegisterPointerSelection(_confirmationButtons[1], () => SelectConfirmation(1));

            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            ShowMainPage();
        }

        private void Update()
        {
            UpdatePointerMode();
            if (WasBackPressed())
            {
                NavigateBack();
                return;
            }

            int movement = ReadNavigation();
            if (movement != 0)
            {
                UseNavigationMode();
                MoveSelection(movement);
            }

            if (WasConfirmPressed())
            {
                UseNavigationMode();
                ActivateSelection();
            }
        }

        private void MoveSelection(int movement)
        {
            switch (_page)
            {
                case Page.Main: SelectMain(Wrap(_mainIndex + movement, _mainButtons.Length)); break;
                case Page.SaveSlots: SelectSlot(Wrap(_slotIndex + movement, _slotButtons.Length)); break;
                case Page.DeleteSlots: SelectDeleteSlot(Wrap(_deleteSlotIndex + movement, _deleteSlotButtons.Length)); break;
                case Page.DeleteConfirmation: SelectConfirmation(Wrap(_confirmationIndex + movement, 2)); break;
            }
        }

        private void ActivateSelection()
        {
            switch (_page)
            {
                case Page.Main: ActivateMainSelection(); break;
                case Page.SaveSlots: ChooseSlot(_slotIndex); break;
                case Page.Settings: ShowDeleteSavePage(); break;
                case Page.DeleteSlots: ChooseDeleteSlot(_deleteSlotIndex); break;
                case Page.DeleteConfirmation:
                    if (_confirmationIndex == 0) ConfirmDeleteStage();
                    else CloseDeleteConfirmation();
                    break;
            }
        }

        private void NavigateBack()
        {
            switch (_page)
            {
                case Page.DeleteConfirmation: CloseDeleteConfirmation(); break;
                case Page.DeleteSlots: ShowSettingsPage(); break;
                case Page.Settings:
                case Page.SaveSlots: ShowMainPage(); break;
            }
        }

        private void HideAllPages()
        {
            _mainPage.style.display = DisplayStyle.None;
            _loadPage.style.display = DisplayStyle.None;
            _settingsPage.style.display = DisplayStyle.None;
            _deleteSavePage.style.display = DisplayStyle.None;
            _deleteConfirmationOverlay.style.display = DisplayStyle.None;
        }

        private void ShowMainPage()
        {
            HideAllPages();
            _page = Page.Main;
            _mainPage.style.display = DisplayStyle.Flex;
            SelectMain(0);
        }

        private void ShowSettingsPage()
        {
            HideAllPages();
            _page = Page.Settings;
            _settingsPage.style.display = DisplayStyle.Flex;
            SelectSettings();
        }

        private void ShowDeleteSavePage()
        {
            HideAllPages();
            _page = Page.DeleteSlots;
            _deleteSavePage.style.display = DisplayStyle.Flex;
            _deleteMessage.text = "Choose the save data to delete.";
            RefreshSlotButtons(_deleteSlotButtons);
            SelectDeleteSlot(0);
        }

        private void ShowLoadPage() => ShowSlotPage(false);
        private void ShowNewGameSlots() => ShowSlotPage(true);

        private void ShowSlotPage(bool startingNewGame)
        {
            HideAllPages();
            _page = Page.SaveSlots;
            _startingNewGame = startingNewGame;
            _overwriteConfirmationSlot = -1;
            _loadPage.style.display = DisplayStyle.Flex;
            _slotPageHeading.text = startingNewGame ? "New Game" : "Load Game";
            _message.text = startingNewGame
                ? "Choose the save slot for this new game."
                : "Choose a valid save slot. Empty or damaged slots cannot be loaded.";
            RefreshSlotButtons(_slotButtons);
            SelectSlot(0);
        }

        private void ChooseSlot(int index)
        {
            SelectSlot(index);
            if (!_startingNewGame)
            {
                LoadSlot(index);
                return;
            }

            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
            {
                _message.text = "Save service is unavailable.";
                return;
            }

            int slotNumber = index + 1;
            SaveSlotInfo info = saveService.InspectSlot(slotNumber);
            if (info.State != SaveSlotState.Empty && _overwriteConfirmationSlot != slotNumber)
            {
                _overwriteConfirmationSlot = slotNumber;
                _message.text = $"Slot {slotNumber} already contains progress. Confirm again to overwrite it.";
                return;
            }
            StartNewGame(slotNumber);
        }

        private void StartNewGame(int slotNumber)
        {
            try
            {
                if (!PlayerStateService.TryGetActive(out PlayerStateService stateService))
                    throw new InvalidOperationException("Player state service is unavailable.");
                stateService.StartNewGame();
                if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
                    throw new InvalidOperationException("Save service is unavailable.");
                saveService.BeginNewGameInSlot(slotNumber, TitleMenuBootstrap.ExplorationSceneName);
                SceneManager.LoadScene(TitleMenuBootstrap.ExplorationSceneName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void LoadSlot(int index)
        {
            try
            {
                if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
                    throw new InvalidOperationException("Save service is unavailable.");
                SaveSlotInfo info = saveService.InspectSlot(index + 1);
                if (!info.CanLoad)
                {
                    _message.text = GetUnavailableMessage(info);
                    return;
                }
                LoadedPlayerSave loaded = saveService.LoadSlot(index + 1);
                string sceneName = string.IsNullOrWhiteSpace(loaded.Exploration.SceneName)
                    ? TitleMenuBootstrap.ExplorationSceneName
                    : loaded.Exploration.SceneName;
                SceneManager.LoadScene(sceneName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _message.text = $"Load failed: {exception.Message}";
                RefreshSlotButtons(_slotButtons);
            }
        }

        private void ChooseDeleteSlot(int index)
        {
            SelectDeleteSlot(index);
            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
            {
                _deleteMessage.text = "Save service is unavailable.";
                return;
            }
            SaveSlotInfo info = saveService.InspectSlot(index + 1);
            if (info.State == SaveSlotState.Empty)
            {
                _deleteMessage.text = $"Save Slot {index + 1} is already empty.";
                return;
            }
            _pendingDeleteSlot = index + 1;
            _deleteConfirmationStage = 1;
            _page = Page.DeleteConfirmation;
            _deleteConfirmationOverlay.style.display = DisplayStyle.Flex;
            UpdateDeleteConfirmationCopy();
            SelectConfirmation(1);
        }

        private void UpdateDeleteConfirmationCopy()
        {
            bool finalStage = _deleteConfirmationStage == 2;
            _deleteConfirmationTitle.text = finalStage ? "Final Confirmation" : "Delete Save Data?";
            _deleteConfirmationMessage.text = finalStage
                ? $"Permanently delete Save Slot {_pendingDeleteSlot}? This cannot be undone."
                : $"Delete Save Slot {_pendingDeleteSlot}? All saved data in this slot will be removed.";
            _confirmationButtons[0].text = finalStage ? "Delete Permanently" : "Continue";
        }

        private void ConfirmDeleteStage()
        {
            if (_deleteConfirmationStage == 1)
            {
                _deleteConfirmationStage = 2;
                UpdateDeleteConfirmationCopy();
                SelectConfirmation(1);
                return;
            }
            try
            {
                if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
                    throw new InvalidOperationException("Save service is unavailable.");
                int deletedSlot = _pendingDeleteSlot;
                saveService.DeleteSlot(deletedSlot);
                CloseDeleteConfirmation();
                _deleteMessage.text = $"Save Slot {deletedSlot} was deleted.";
                RefreshSlotButtons(_deleteSlotButtons);
                SelectDeleteSlot(deletedSlot - 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CloseDeleteConfirmation();
                _deleteMessage.text = $"Delete failed: {exception.Message}";
                RefreshSlotButtons(_deleteSlotButtons);
            }
        }

        private void CloseDeleteConfirmation()
        {
            _deleteConfirmationOverlay.style.display = DisplayStyle.None;
            _page = Page.DeleteSlots;
            _deleteConfirmationStage = 0;
            _pendingDeleteSlot = 0;
            SelectDeleteSlot(_deleteSlotIndex);
        }

        private static void RefreshSlotButtons(Button[] buttons)
        {
            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
            {
                for (int index = 0; index < buttons.Length; index++)
                    buttons[index].text = $"Save Slot {index + 1}\nUnavailable";
                return;
            }
            for (int index = 0; index < buttons.Length; index++)
                buttons[index].text = FormatSlot(saveService.InspectSlot(index + 1));
        }

        private static string FormatSlot(SaveSlotInfo info)
        {
            string heading = $"Save Slot {info.SlotNumber}";
            switch (info.State)
            {
                case SaveSlotState.Empty: return heading + "\nEmpty";
                case SaveSlotState.Valid:
                    string timestamp = info.SavedAtUtc.HasValue
                        ? info.SavedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd  HH:mm")
                        : "Unknown time";
                    return $"{heading}\n{timestamp}{(info.UsesBackup ? "  •  Recovered backup" : string.Empty)}";
                case SaveSlotState.Corrupted: return heading + "\nCorrupted save";
                case SaveSlotState.UnsupportedVersion: return heading + "\nUnsupported version";
                default: return heading + "\nUnavailable";
            }
        }

        private static string GetUnavailableMessage(SaveSlotInfo info)
        {
            switch (info.State)
            {
                case SaveSlotState.Empty: return $"Save Slot {info.SlotNumber} is empty.";
                case SaveSlotState.Corrupted: return $"Save Slot {info.SlotNumber} is corrupted and cannot be loaded.";
                case SaveSlotState.UnsupportedVersion: return $"Save Slot {info.SlotNumber} was created by an unsupported save version.";
                default: return $"Save Slot {info.SlotNumber} is unavailable.";
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit Game is available in a player build. Play Mode remains active in the Editor.");
#else
            Application.Quit();
#endif
        }

        private void ActivateMainSelection()
        {
            switch (_mainIndex)
            {
                case 0: ShowNewGameSlots(); break;
                case 1: ShowLoadPage(); break;
                case 2: ShowSettingsPage(); break;
                case 3: QuitGame(); break;
            }
        }

        private void SelectMain(int index)
        {
            _mainIndex = index;
            SelectButton(_mainButtons, index, MainSelectedClass);
        }

        private void SelectSettings()
        {
            _deleteSaveButton.EnableInClassList(MainSelectedClass, true);
        }

        private void SelectSlot(int index)
        {
            if (_slotIndex != index) _overwriteConfirmationSlot = -1;
            _slotIndex = index;
            SelectButton(_slotButtons, index, SlotSelectedClass);
        }

        private void SelectDeleteSlot(int index)
        {
            _deleteSlotIndex = index;
            SelectButton(_deleteSlotButtons, index, SlotSelectedClass);
        }

        private void SelectConfirmation(int index)
        {
            _confirmationIndex = index;
            SelectButton(_confirmationButtons, index, MainSelectedClass);
        }

        private static void SelectButton(Button[] buttons, int index, string className)
        {
            for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
                buttons[buttonIndex].EnableInClassList(className, buttonIndex == index);
        }

        private static void RegisterPointerSelection(Button button, Action select)
        {
            button.RegisterCallback<PointerEnterEvent>(_ => select());
        }

        private void UpdatePointerMode()
        {
            if (Mouse.current == null) return;
            Vector2 current = Mouse.current.position.ReadValue();
            if (!_hasMousePosition)
            {
                _lastMousePosition = current;
                _hasMousePosition = true;
                return;
            }
            if ((current - _lastMousePosition).sqrMagnitude > 1f) UnityEngine.Cursor.visible = true;
            _lastMousePosition = current;
        }

        private static void UseNavigationMode()
        {
            if (Gamepad.current != null) UnityEngine.Cursor.visible = false;
        }

        private int ReadNavigation()
        {
            if (Keyboard.current != null)
            {
                if (_page == Page.DeleteConfirmation)
                {
                    if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) return -1;
                    if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) return 1;
                }
                if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) return -1;
                if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) return 1;
            }
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) return 0;
            if (_page == Page.DeleteConfirmation)
            {
                if (gamepad.dpad.left.wasPressedThisFrame) return -1;
                if (gamepad.dpad.right.wasPressedThisFrame) return 1;
            }
            if (gamepad.dpad.up.wasPressedThisFrame) return -1;
            if (gamepad.dpad.down.wasPressedThisFrame) return 1;
            Vector2 stick = gamepad.leftStick.ReadValue();
            float axis = _page == Page.DeleteConfirmation && Mathf.Abs(stick.x) > Mathf.Abs(stick.y)
                ? stick.x
                : stick.y;
            if (Mathf.Abs(axis) < 0.25f)
            {
                _stickLatched = false;
                return 0;
            }
            if (_stickLatched || Mathf.Abs(axis) < 0.65f) return 0;
            _stickLatched = true;
            if (_page == Page.DeleteConfirmation && Mathf.Abs(stick.x) > Mathf.Abs(stick.y))
                return axis < 0f ? -1 : 1;
            return axis > 0f ? -1 : 1;
        }

        private static bool WasConfirmPressed()
        {
            return (Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame
                        || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                        || Keyboard.current.fKey.wasPressedThisFrame))
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        private static bool WasBackPressed()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        }

        private static int Wrap(int value, int length) => (value % length + length) % length;
    }
}
