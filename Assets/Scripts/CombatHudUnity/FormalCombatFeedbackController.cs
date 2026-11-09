using System.Collections.Generic;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatPresentation;
using SER494.Gameplay.CombatSandbox;
using UnityEngine;
using UnityEngine.UIElements;

namespace SER494.Gameplay.CombatHud.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FormalCombatFeedbackController : MonoBehaviour
    {
        private const int MaximumVisibleCuesPerSide = 5;
        private const long CueLifetimeMilliseconds = 1100;
        private const long ImpactLifetimeMilliseconds = 160;

        private readonly Queue<VisualElement> _playerCues =
            new Queue<VisualElement>();
        private readonly Queue<VisualElement> _enemyCues =
            new Queue<VisualElement>();

        private VisualElement _feedbackOverlay;
        private VisualElement _playerFeedbackLane;
        private VisualElement _enemyFeedbackLane;
        private VisualElement _impactFlash;
        private CombatSandboxController _sandbox;
        private int _impactVersion;
        private bool _wasVisible;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _feedbackOverlay = root.Q<VisualElement>("combatFeedbackOverlay");
            _playerFeedbackLane = root.Q<VisualElement>("playerFeedbackLane");
            _enemyFeedbackLane = root.Q<VisualElement>("enemyFeedbackLane");
            _impactFlash = root.Q<VisualElement>("combatImpactFlash");
        }

        private void Update()
        {
            if (_sandbox == null)
            {
                BindSandbox(
                    UnityEngine.Object.FindAnyObjectByType<CombatSandboxController>());
            }

            bool visible = _sandbox != null && _sandbox.ShouldShowFormalBattleHud;
            if (_feedbackOverlay != null)
            {
                _feedbackOverlay.style.display = visible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_wasVisible && !visible)
            {
                ClearCues();
            }

            _wasVisible = visible;
        }

        private void OnDestroy()
        {
            BindSandbox(null);
        }

        private void BindSandbox(CombatSandboxController sandbox)
        {
            if (_sandbox != null)
            {
                _sandbox.TimelineResolutionPresented -= OnResolutionPresented;
            }

            _sandbox = sandbox;
            if (_sandbox != null)
            {
                _sandbox.TimelineResolutionPresented += OnResolutionPresented;
            }

            ClearCues();
        }

        private void OnResolutionPresented(SoloComboActionResolution resolution)
        {
            if (_sandbox == null || !_sandbox.ShouldShowFormalBattleHud)
            {
                return;
            }

            IReadOnlyList<CombatFeedbackCue> cues =
                CombatFeedbackPresentation.Build(resolution);
            for (int index = 0; index < cues.Count; index++)
            {
                ShowCue(cues[index]);
            }
        }

        private void ShowCue(CombatFeedbackCue cue)
        {
            VisualElement lane = cue.TargetSide == TimelineActorSide.Player
                ? _playerFeedbackLane
                : _enemyFeedbackLane;
            Queue<VisualElement> visibleCues = cue.TargetSide
                == TimelineActorSide.Player
                    ? _playerCues
                    : _enemyCues;
            if (lane == null)
            {
                return;
            }

            var entry = new VisualElement();
            entry.pickingMode = PickingMode.Ignore;
            entry.AddToClassList("combat-feedback-cue");
            entry.AddToClassList(ToneClass(cue.Tone));

            var symbol = new Label(SymbolFor(cue.Tone));
            symbol.pickingMode = PickingMode.Ignore;
            symbol.AddToClassList("combat-feedback-symbol");
            entry.Add(symbol);

            var copy = new VisualElement();
            copy.pickingMode = PickingMode.Ignore;
            copy.AddToClassList("combat-feedback-copy");
            var primary = new Label(cue.PrimaryText);
            primary.pickingMode = PickingMode.Ignore;
            primary.AddToClassList("combat-feedback-primary");
            copy.Add(primary);
            if (!string.IsNullOrWhiteSpace(cue.DetailText))
            {
                var detail = new Label(cue.DetailText);
                detail.pickingMode = PickingMode.Ignore;
                detail.AddToClassList("combat-feedback-detail");
                copy.Add(detail);
            }

            entry.Add(copy);
            lane.Add(entry);
            visibleCues.Enqueue(entry);
            while (visibleCues.Count > MaximumVisibleCuesPerSide)
            {
                RemoveCue(visibleCues.Dequeue());
            }

            entry.schedule.Execute(() =>
            {
                RemoveCue(entry);
                RemoveFromQueue(visibleCues, entry);
            }).ExecuteLater(CueLifetimeMilliseconds);

            ShowImpact(cue.Tone);
        }

        private void ShowImpact(CombatFeedbackTone tone)
        {
            if (_impactFlash == null || !HasImpactFlash(tone))
            {
                return;
            }

            int version = ++_impactVersion;
            _impactFlash.ClearClassList();
            _impactFlash.AddToClassList("combat-feedback-impact");
            _impactFlash.AddToClassList(ToneClass(tone));
            _impactFlash.style.display = DisplayStyle.Flex;
            _impactFlash.schedule.Execute(() =>
            {
                if (version == _impactVersion)
                {
                    _impactFlash.style.display = DisplayStyle.None;
                }
            }).ExecuteLater(ImpactLifetimeMilliseconds);
        }

        private void ClearCues()
        {
            _playerFeedbackLane?.Clear();
            _enemyFeedbackLane?.Clear();
            _playerCues.Clear();
            _enemyCues.Clear();
            if (_impactFlash != null)
            {
                _impactFlash.style.display = DisplayStyle.None;
            }
        }

        private static void RemoveCue(VisualElement cue)
        {
            cue?.RemoveFromHierarchy();
        }

        private static void RemoveFromQueue(
            Queue<VisualElement> queue,
            VisualElement removed)
        {
            if (queue.Count == 0)
            {
                return;
            }

            int count = queue.Count;
            for (int index = 0; index < count; index++)
            {
                VisualElement candidate = queue.Dequeue();
                if (!ReferenceEquals(candidate, removed))
                {
                    queue.Enqueue(candidate);
                }
            }
        }

        private static bool HasImpactFlash(CombatFeedbackTone tone)
        {
            return tone == CombatFeedbackTone.Damage
                || tone == CombatFeedbackTone.Guard
                || tone == CombatFeedbackTone.Evade
                || tone == CombatFeedbackTone.Clash
                || tone == CombatFeedbackTone.Heal
                || tone == CombatFeedbackTone.Knockdown;
        }

        private static string ToneClass(CombatFeedbackTone tone)
        {
            return "combat-feedback--" + tone.ToString().ToLowerInvariant();
        }

        private static string SymbolFor(CombatFeedbackTone tone)
        {
            switch (tone)
            {
                case CombatFeedbackTone.Damage:
                    return "◆";
                case CombatFeedbackTone.Guard:
                    return "◇";
                case CombatFeedbackTone.Evade:
                    return "○";
                case CombatFeedbackTone.Clash:
                    return "✕";
                case CombatFeedbackTone.Heal:
                    return "+";
                case CombatFeedbackTone.Buff:
                    return "↑";
                case CombatFeedbackTone.Debuff:
                    return "↓";
                case CombatFeedbackTone.Stamina:
                    return "+";
                case CombatFeedbackTone.Flinch:
                    return "!";
                case CombatFeedbackTone.Stagger:
                    return "!!";
                case CombatFeedbackTone.Knockdown:
                    return "▼";
                case CombatFeedbackTone.Canceled:
                    return "×";
                default:
                    return "•";
            }
        }
    }
}
