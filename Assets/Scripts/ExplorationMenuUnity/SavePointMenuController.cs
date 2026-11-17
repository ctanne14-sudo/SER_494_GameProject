using System;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    [DisallowMultipleComponent]
    public sealed class SavePointMenuController : MonoBehaviour
    {
        private const string SelectedClass = "save-slot-selected";
        private const string ConfirmSelectedClass = "save-confirm-selected";

        private static SavePointMenuController _active;

        private readonly Button[] _slotButtons = new Button[PlayerSaveStorage.SlotCount];
        private VisualElement _root;
        private Label _message;
        private VisualElement _confirmationOverlay;
        private Label _confirmationPrompt;
        private Button _confirmOverwriteButton;
        private Button _cancelOverwriteButton;
        private bool _isOpen;
        private bool _confirmingOverwrite;
        private bool _confirmChoice;
        private int _selectedSlotIndex;
        private float _previousTimeScale;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private bool _previousInputGate;
        private bool _stickLatched;
        private int _openedFrame;
        private SavePointContext _checkpoint;

        public static bool TryOpen()
        {
            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService)
                || !saveService.Exploration.HasCheckpoint)
            {
                return false;
            }

            ExplorationSaveSnapshot saved = saveService.Exploration;
            return TryOpen(new SavePointContext(
                saved.SceneName,
                saved.SavePointId,
                saved.Position,
                saved.Rotation));
        }

        public static bool TryOpen(SavePointContext checkpoint)
        {
            if (_active == null || _active._isOpen || checkpoint == null)
            {
                return false;
            }

            if (ExplorationMenuInputGate.IsOpen
                || !PlayerSaveService.TryGetActive(out _))
            {
                return false;
            }

            _active.Open(checkpoint);
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
            UIDocument document = GetComponent<UIDocument>();
            _root = document.rootVisualElement.Q<VisualElement>("savePointOverlay");
            _message = _root.Q<Label>("saveMessage");
            _confirmationOverlay = _root.Q<VisualElement>("overwriteOverlay");
            _confirmationPrompt = _root.Q<Label>("overwritePrompt");
            _confirmOverwriteButton = _root.Q<Button>("confirmOverwriteButton");
            _cancelOverwriteButton = _root.Q<Button>("cancelOverwriteButton");

            for (int index = 0; index < _slotButtons.Length; index++)
            {
                int capturedIndex = index;
                _slotButtons[index] = _root.Q<Button>($"saveSlot{index + 1}");
                _slotButtons[index].clicked += () => ChooseSlot(capturedIndex);
            }

            _confirmOverwriteButton.clicked += ConfirmOverwrite;
            _cancelOverwriteButton.clicked += CancelOverwrite;
            _root.style.display = DisplayStyle.None;
            _confirmationOverlay.style.display = DisplayStyle.None;
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
                if (PlayerSaveService.TryGetActive(out PlayerSaveService saveService)
                    && saveService.TryConsumeCheckpointInteractionRequest(
                        out SavePointContext pendingCheckpoint))
                {
                    TryOpen(pendingCheckpoint);
                }

                return;
            }

            bool backPressed = (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Mouse.current != null
                    && Mouse.current.rightButton.wasPressedThisFrame)
                || (Gamepad.current != null
                    && (Gamepad.current.buttonEast.wasPressedThisFrame
                        || Gamepad.current.startButton.wasPressedThisFrame));
            if (backPressed)
            {
                if (_confirmingOverwrite)
                {
                    CancelOverwrite();
                }
                else
                {
                    Close();
                }

                return;
            }

            int movement = ReadVerticalNavigation();
            bool horizontalPrevious = IsPreviousPressed();
            bool horizontalNext = IsNextPressed();
            bool confirmPressed = (Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame
                        || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                        || Keyboard.current.fKey.wasPressedThisFrame))
                || (Gamepad.current != null
                    && Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (_confirmingOverwrite)
            {
                if (movement != 0 || horizontalPrevious || horizontalNext)
                {
                    _confirmChoice = !_confirmChoice;
                    RefreshConfirmationSelection();
                }

                if (confirmPressed)
                {
                    if (_confirmChoice) ConfirmOverwrite();
                    else CancelOverwrite();
                }

                return;
            }

            if (movement != 0)
            {
                _selectedSlotIndex = Wrap(
                    _selectedSlotIndex + movement,
                    _slotButtons.Length);
                RefreshSlotSelection();
            }

            if (confirmPressed && Time.frameCount != _openedFrame)
            {
                ChooseSlot(_selectedSlotIndex);
            }
        }

        private void Open(SavePointContext checkpoint)
        {
            _checkpoint = checkpoint;
            _previousTimeScale = Time.timeScale;
            _previousCursorLockMode = UnityEngine.Cursor.lockState;
            _previousCursorVisible = UnityEngine.Cursor.visible;
            _previousInputGate = ExplorationMenuInputGate.IsOpen;

            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            ExplorationMenuInputGate.IsOpen = true;
            _isOpen = true;
            _openedFrame = Time.frameCount;
            PlayerSaveService.TryGetActive(out PlayerSaveService saveService);
            _selectedSlotIndex = saveService != null && saveService.HasActiveSlot
                ? saveService.ActiveSlotNumber - 1
                : 0;
            _message.text = saveService != null && saveService.HasActiveSlot
                ? $"Save and recover all resources in Slot {saveService.ActiveSlotNumber}."
                : "No active save slot is available.";
            _root.style.display = DisplayStyle.Flex;
            RefreshSlots();
            RefreshSlotSelection();
        }

        private void Close()
        {
            _confirmingOverwrite = false;
            _confirmationOverlay.style.display = DisplayStyle.None;
            _root.style.display = DisplayStyle.None;
            _isOpen = false;
            ExplorationMenuInputGate.IsOpen = _previousInputGate;
            Time.timeScale = _previousTimeScale;
            UnityEngine.Cursor.lockState = _previousCursorLockMode;
            UnityEngine.Cursor.visible = _previousCursorVisible;
        }

        private void ChooseSlot(int index)
        {
            _selectedSlotIndex = index;
            RefreshSlotSelection();

            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
            {
                _message.text = "Save service is unavailable.";
                return;
            }

            if (!saveService.HasActiveSlot || index + 1 != saveService.ActiveSlotNumber)
            {
                _message.text = "This run can only save to its active slot.";
                return;
            }

            SaveSlotInfo info = saveService.InspectSlot(index + 1);
            if (info.State == SaveSlotState.Empty)
            {
                SaveSelectedSlot();
                return;
            }

            _confirmingOverwrite = true;
            _confirmChoice = false;
            _confirmationPrompt.text =
                $"Overwrite Save Slot {index + 1}? The previous save will be replaced.";
            _confirmationOverlay.style.display = DisplayStyle.Flex;
            RefreshConfirmationSelection();
        }

        private void ConfirmOverwrite()
        {
            _confirmingOverwrite = false;
            _confirmationOverlay.style.display = DisplayStyle.None;
            SaveSelectedSlot();
        }

        private void CancelOverwrite()
        {
            _confirmingOverwrite = false;
            _confirmationOverlay.style.display = DisplayStyle.None;
            _message.text = "Save cancelled. Choose a slot or go back.";
            RefreshSlotSelection();
        }

        private void SaveSelectedSlot()
        {
            try
            {
                if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
                {
                    throw new InvalidOperationException("Save service is unavailable.");
                }

                int slotNumber = saveService.ActiveSlotNumber;
                saveService.SaveAtCheckpoint(_checkpoint);
                RefreshSlots();
                _message.text = $"Progress saved to Slot {slotNumber}.";
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _message.text = $"Save failed: {exception.Message}";
            }
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
                int slotNumber = index + 1;
                SaveSlotInfo info = saveService.InspectSlot(slotNumber);
                _slotButtons[index].text = FormatSlot(info);
                _slotButtons[index].SetEnabled(
                    saveService.HasActiveSlot
                    && slotNumber == saveService.ActiveSlotNumber);
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
                    return heading + "\nCorrupted save — may be overwritten";
                case SaveSlotState.UnsupportedVersion:
                    return heading + "\nUnsupported version — may be overwritten";
                default:
                    return heading + "\nUnknown";
            }
        }

        private void RefreshSlotSelection()
        {
            for (int index = 0; index < _slotButtons.Length; index++)
            {
                _slotButtons[index].EnableInClassList(
                    SelectedClass,
                    index == _selectedSlotIndex);
            }
        }

        private void RefreshConfirmationSelection()
        {
            _confirmOverwriteButton.EnableInClassList(
                ConfirmSelectedClass,
                _confirmChoice);
            _cancelOverwriteButton.EnableInClassList(
                ConfirmSelectedClass,
                !_confirmChoice);
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

        private static bool IsPreviousPressed()
        {
            return (Keyboard.current != null
                    && (Keyboard.current.leftArrowKey.wasPressedThisFrame
                        || Keyboard.current.aKey.wasPressedThisFrame))
                || (Gamepad.current != null
                    && Gamepad.current.dpad.left.wasPressedThisFrame);
        }

        private static bool IsNextPressed()
        {
            return (Keyboard.current != null
                    && (Keyboard.current.rightArrowKey.wasPressedThisFrame
                        || Keyboard.current.dKey.wasPressedThisFrame))
                || (Gamepad.current != null
                    && Gamepad.current.dpad.right.wasPressedThisFrame);
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
