using SER494.Gameplay.CombatIntegration.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SER494.Gameplay.BattleResult.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class BattleResultScreenController : MonoBehaviour
    {
        private static readonly string[] OutcomeStyleClasses =
        {
            "battle-result-victory",
            "battle-result-defeat",
            "battle-result-mutual"
        };

        private VisualElement _overlay;
        private VisualElement _frame;
        private Label _outcomeTitle;
        private Label _enemyName;
        private Label _rewardHeading;
        private VisualElement _goldRow;
        private Label _goldValue;
        private VisualElement _materialList;
        private Label _emptyRewards;
        private Label _consequenceHeading;
        private Label _consequenceBody;
        private Label _errorMessage;
        private Button _continueButton;
        private BattleSessionService _boundService;
        private BattleSession _displayedSession;
        private bool _isOpen;
        private int _openedFrame;
        private float _previousTimeScale;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _overlay = root.Q<VisualElement>("battleResultOverlay");
            _frame = root.Q<VisualElement>("battleResultFrame");
            _outcomeTitle = root.Q<Label>("outcomeTitle");
            _enemyName = root.Q<Label>("enemyName");
            _rewardHeading = root.Q<Label>("rewardHeading");
            _goldRow = root.Q<VisualElement>("goldRow");
            _goldValue = root.Q<Label>("goldValue");
            _materialList = root.Q<VisualElement>("materialList");
            _emptyRewards = root.Q<Label>("emptyRewards");
            _consequenceHeading = root.Q<Label>("consequenceHeading");
            _consequenceBody = root.Q<Label>("consequenceBody");
            _errorMessage = root.Q<Label>("errorMessage");
            _continueButton = root.Q<Button>("continueButton");
            _continueButton.clicked += ContinueFromBattle;
            _overlay.style.display = DisplayStyle.None;
        }

        private void OnEnable()
        {
            TryBindService();
        }

        private void OnDisable()
        {
            UnbindService();
            CloseAndRestoreInteraction();
        }

        private void Update()
        {
            TryBindService();

            if (!_isOpen)
            {
                BattleSession session = _boundService?.CurrentSession;
                if (session != null && session.IsComplete)
                {
                    Show(session);
                }

                return;
            }

            if (Time.frameCount == _openedFrame)
            {
                return;
            }

            bool confirmPressed = (Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame
                        || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                        || Keyboard.current.fKey.wasPressedThisFrame
                        || Keyboard.current.spaceKey.wasPressedThisFrame))
                || (Gamepad.current != null
                    && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (confirmPressed)
            {
                ContinueFromBattle();
            }
        }

        private void TryBindService()
        {
            BattleSessionService active = BattleSessionService.Active;
            if (ReferenceEquals(active, _boundService))
            {
                return;
            }

            UnbindService();
            _boundService = active;
            if (_boundService != null)
            {
                _boundService.SessionStarted += HandleSessionStarted;
                _boundService.SessionCompleted += HandleSessionCompleted;
            }
        }

        private void UnbindService()
        {
            if (_boundService == null)
            {
                return;
            }

            _boundService.SessionStarted -= HandleSessionStarted;
            _boundService.SessionCompleted -= HandleSessionCompleted;
            _boundService = null;
        }

        private void HandleSessionStarted(BattleSession session)
        {
            if (_isOpen)
            {
                CloseAndRestoreInteraction();
            }

            _displayedSession = null;
        }

        private void HandleSessionCompleted(BattleSession session)
        {
            Show(session);
        }

        private void Show(BattleSession session)
        {
            if (session == null || !session.IsComplete || ReferenceEquals(session, _displayedSession))
            {
                return;
            }

            BattleResultPresentation presentation = BattleResultPresentationFactory.Create(session);
            _displayedSession = session;
            _outcomeTitle.text = presentation.OutcomeTitle;
            _enemyName.text = presentation.EnemyName;
            _rewardHeading.text = presentation.RewardHeading;
            _goldValue.text = presentation.Gold.ToString();
            _consequenceHeading.text = presentation.ConsequenceHeading;
            _consequenceBody.text = presentation.ConsequenceBody;
            _continueButton.text = presentation.ContinueLabel;
            _errorMessage.text = string.Empty;
            _errorMessage.style.display = DisplayStyle.None;

            for (int index = 0; index < OutcomeStyleClasses.Length; index++)
            {
                _frame.RemoveFromClassList(OutcomeStyleClasses[index]);
            }

            _frame.AddToClassList(presentation.OutcomeStyleClass);
            PopulateMaterials(presentation);

            _previousTimeScale = Time.timeScale;
            _previousCursorLockMode = UnityEngine.Cursor.lockState;
            _previousCursorVisible = UnityEngine.Cursor.visible;
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            _openedFrame = Time.frameCount;
            _isOpen = true;
            _overlay.style.display = DisplayStyle.Flex;
            _continueButton.Focus();
        }

        private void PopulateMaterials(BattleResultPresentation presentation)
        {
            _materialList.Clear();
            _goldRow.style.display = presentation.Gold > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            for (int index = 0; index < presentation.Materials.Count; index++)
            {
                BattleResultMaterialLine material = presentation.Materials[index];
                var row = new VisualElement();
                row.AddToClassList("battle-result-material-row");

                var name = new Label(material.DisplayName);
                name.AddToClassList("battle-result-material-name");
                row.Add(name);

                var quantity = new Label($"×{material.Quantity}");
                quantity.AddToClassList("battle-result-material-quantity");
                row.Add(quantity);
                _materialList.Add(row);
            }

            _emptyRewards.style.display = presentation.HasRewards
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _materialList.style.display = presentation.Materials.Count > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void ContinueFromBattle()
        {
            if (!_isOpen || _boundService == null || _boundService.IsReturningToSource)
            {
                return;
            }

            if (!_boundService.TryReturnToSourceScene(out string validationError))
            {
                _errorMessage.text = validationError;
                _errorMessage.style.display = DisplayStyle.Flex;
                return;
            }

            CloseAndRestoreInteraction();
        }

        private void CloseAndRestoreInteraction()
        {
            if (!_isOpen)
            {
                return;
            }

            _overlay.style.display = DisplayStyle.None;
            _isOpen = false;
            Time.timeScale = _previousTimeScale;
            UnityEngine.Cursor.lockState = _previousCursorLockMode;
            UnityEngine.Cursor.visible = _previousCursorVisible;
        }
    }
}
