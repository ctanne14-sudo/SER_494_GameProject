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

        private readonly Button[] _mainButtons = new Button[3];
        private readonly Button[] _slotButtons = new Button[PlayerSaveStorage.SlotCount];
        private VisualElement _mainPage;
        private VisualElement _loadPage;
        private Label _message;
        private int _mainIndex;
        private int _slotIndex;
        private bool _showingLoadPage;
        private bool _stickLatched;
        private Vector2 _lastMousePosition;
        private bool _hasMousePosition;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _mainPage = root.Q<VisualElement>("mainMenuPage");
            _loadPage = root.Q<VisualElement>("loadGamePage");
            _message = root.Q<Label>("loadMessage");

            _mainButtons[0] = root.Q<Button>("newGameButton");
            _mainButtons[1] = root.Q<Button>("loadGameButton");
            _mainButtons[2] = root.Q<Button>("quitGameButton");

            for (int index = 0; index < _slotButtons.Length; index++)
            {
                int capturedIndex = index;
                _slotButtons[index] = root.Q<Button>($"loadSlot{index + 1}");
                _slotButtons[index].clicked += () => LoadSlot(capturedIndex);
                RegisterPointerSelection(_slotButtons[index], () => SelectSlot(capturedIndex));
            }

            Button backButton = root.Q<Button>("backToMainButton");
            _mainButtons[0].clicked += StartNewGame;
            _mainButtons[1].clicked += ShowLoadPage;
            _mainButtons[2].clicked += QuitGame;
            backButton.clicked += ShowMainPage;

            for (int index = 0; index < _mainButtons.Length; index++)
            {
                int capturedIndex = index;
                RegisterPointerSelection(_mainButtons[index], () => SelectMain(capturedIndex));
            }

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
                if (_showingLoadPage)
                {
                    ShowMainPage();
                }

                return;
            }

            int movement = ReadVerticalNavigation();
            if (movement != 0)
            {
                UseNavigationMode();
                if (_showingLoadPage)
                {
                    SelectSlot(Wrap(_slotIndex + movement, _slotButtons.Length));
                }
                else
                {
                    SelectMain(Wrap(_mainIndex + movement, _mainButtons.Length));
                }
            }

            if (!WasConfirmPressed())
            {
                return;
            }

            UseNavigationMode();
            if (_showingLoadPage)
            {
                LoadSlot(_slotIndex);
            }
            else
            {
                ActivateMainSelection();
            }
        }

        private void ShowMainPage()
        {
            _showingLoadPage = false;
            _loadPage.style.display = DisplayStyle.None;
            _mainPage.style.display = DisplayStyle.Flex;
            SelectMain(0);
        }

        private void ShowLoadPage()
        {
            _showingLoadPage = true;
            _mainPage.style.display = DisplayStyle.None;
            _loadPage.style.display = DisplayStyle.Flex;
            _message.text = "Choose a valid save slot. Empty or damaged slots cannot be loaded.";
            RefreshSlots();
            SelectSlot(0);
        }

        private void StartNewGame()
        {
            try
            {
                if (!PlayerStateService.TryGetActive(out PlayerStateService stateService))
                {
                    throw new InvalidOperationException("Player state service is unavailable.");
                }

                stateService.StartNewGame();
                SceneManager.LoadScene(TitleMenuBootstrap.ExplorationSceneName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void LoadSlot(int index)
        {
            _slotIndex = index;
            RefreshSlotSelection();

            try
            {
                if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
                {
                    throw new InvalidOperationException("Save service is unavailable.");
                }

                SaveSlotInfo info = saveService.InspectSlot(index + 1);
                if (!info.CanLoad)
                {
                    _message.text = GetUnavailableMessage(info);
                    return;
                }

                saveService.LoadSlot(index + 1);
                SceneManager.LoadScene(TitleMenuBootstrap.ExplorationSceneName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _message.text = $"Load failed: {exception.Message}";
                RefreshSlots();
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

        private void RefreshSlots()
        {
            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
            {
                for (int index = 0; index < _slotButtons.Length; index++)
                {
                    _slotButtons[index].text = $"Save Slot {index + 1}\nUnavailable";
                }

                return;
            }

            for (int index = 0; index < _slotButtons.Length; index++)
            {
                _slotButtons[index].text = FormatSlot(saveService.InspectSlot(index + 1));
            }
        }

        private static string FormatSlot(SaveSlotInfo info)
        {
            string heading = $"Save Slot {info.SlotNumber}";
            switch (info.State)
            {
                case SaveSlotState.Empty:
                    return heading + "\nEmpty";
                case SaveSlotState.Valid:
                    string timestamp = info.SavedAtUtc.HasValue
                        ? info.SavedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd  HH:mm")
                        : "Unknown time";
                    string backup = info.UsesBackup ? "  •  Recovered backup" : string.Empty;
                    return $"{heading}\n{timestamp}{backup}";
                case SaveSlotState.Corrupted:
                    return heading + "\nCorrupted save";
                case SaveSlotState.UnsupportedVersion:
                    return heading + "\nUnsupported version";
                default:
                    return heading + "\nUnavailable";
            }
        }

        private static string GetUnavailableMessage(SaveSlotInfo info)
        {
            switch (info.State)
            {
                case SaveSlotState.Empty:
                    return $"Save Slot {info.SlotNumber} is empty.";
                case SaveSlotState.Corrupted:
                    return $"Save Slot {info.SlotNumber} is corrupted and cannot be loaded.";
                case SaveSlotState.UnsupportedVersion:
                    return $"Save Slot {info.SlotNumber} was created by an unsupported save version.";
                default:
                    return $"Save Slot {info.SlotNumber} is unavailable.";
            }
        }

        private void ActivateMainSelection()
        {
            switch (_mainIndex)
            {
                case 0: StartNewGame(); break;
                case 1: ShowLoadPage(); break;
                case 2: QuitGame(); break;
            }
        }

        private void SelectMain(int index)
        {
            _mainIndex = index;
            for (int buttonIndex = 0; buttonIndex < _mainButtons.Length; buttonIndex++)
            {
                _mainButtons[buttonIndex].EnableInClassList(
                    MainSelectedClass,
                    buttonIndex == index);
            }
        }

        private void SelectSlot(int index)
        {
            _slotIndex = index;
            RefreshSlotSelection();
        }

        private void RefreshSlotSelection()
        {
            for (int index = 0; index < _slotButtons.Length; index++)
            {
                _slotButtons[index].EnableInClassList(
                    SlotSelectedClass,
                    index == _slotIndex);
            }
        }

        private static void RegisterPointerSelection(Button button, Action select)
        {
            button.RegisterCallback<PointerEnterEvent>(_ => select());
        }

        private void UpdatePointerMode()
        {
            if (Mouse.current == null)
            {
                return;
            }

            Vector2 current = Mouse.current.position.ReadValue();
            if (!_hasMousePosition)
            {
                _lastMousePosition = current;
                _hasMousePosition = true;
                return;
            }

            if ((current - _lastMousePosition).sqrMagnitude > 1f)
            {
                UnityEngine.Cursor.visible = true;
            }

            _lastMousePosition = current;
        }

        private static void UseNavigationMode()
        {
            if (Gamepad.current != null)
            {
                UnityEngine.Cursor.visible = false;
            }
        }

        private int ReadVerticalNavigation()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame
                    || Keyboard.current.wKey.wasPressedThisFrame)
                {
                    return -1;
                }

                if (Keyboard.current.downArrowKey.wasPressedThisFrame
                    || Keyboard.current.sKey.wasPressedThisFrame)
                {
                    return 1;
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return 0;
            }

            if (gamepad.dpad.up.wasPressedThisFrame) return -1;
            if (gamepad.dpad.down.wasPressedThisFrame) return 1;

            float stickY = gamepad.leftStick.ReadValue().y;
            if (Mathf.Abs(stickY) < 0.25f)
            {
                _stickLatched = false;
                return 0;
            }

            if (_stickLatched || Mathf.Abs(stickY) < 0.65f)
            {
                return 0;
            }

            _stickLatched = true;
            return stickY > 0f ? -1 : 1;
        }

        private static bool WasConfirmPressed()
        {
            return (Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame
                        || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                        || Keyboard.current.fKey.wasPressedThisFrame))
                || (Gamepad.current != null
                    && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        private static bool WasBackPressed()
        {
            return (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Mouse.current != null
                    && Mouse.current.rightButton.wasPressedThisFrame)
                || (Gamepad.current != null
                    && Gamepad.current.buttonEast.wasPressedThisFrame);
        }

        private static int Wrap(int value, int length)
        {
            return (value % length + length) % length;
        }
    }
}
