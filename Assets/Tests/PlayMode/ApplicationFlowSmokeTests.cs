using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SER494.Gameplay.BattleResult.Unity;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.ExplorationMenu.Unity;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SER494.Gameplay.PlayModeTests
{
    [Category("PlayModeSmoke")]
    public sealed class ApplicationFlowSmokeTests
    {
        private const string ExplorationSceneName = "Floor1_Cell_A";
        private const string CombatSceneName = "CombatSandbox";

        private SoloComboLoadoutSession _runtimeLoadoutSession;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;

            if (BattleSessionService.TryGetActive(out BattleSessionService service)
                && service.HasActiveSession)
            {
                service.ClearCurrentSession();
            }

            if (_runtimeLoadoutSession != null)
            {
                Object.Destroy(_runtimeLoadoutSession);
                _runtimeLoadoutSession = null;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FormalBattleFlow_ShowsExclusiveResultAndReturnsToExploration()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            Assert.That(BattleSessionService.TryGetActive(out BattleSessionService battleService),
                Is.True);
            if (battleService.HasActiveSession)
            {
                battleService.ClearCurrentSession();
            }

            stateService.StartNewGame();
            ConfigureStarterCombo(stateService.State);
            _runtimeLoadoutSession =
                ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
            Assert.That(
                PlayerStateLoadoutSessionBuilder.TryPrepare(
                    stateService.State,
                    _runtimeLoadoutSession,
                    SoloComboTestScenario.GreatswordTrial,
                    out string loadoutError),
                Is.True,
                loadoutError);
            Assert.That(
                SoloComboEncounterLibrary.TryLoad(
                    SoloComboTestScenario.GreatswordTrial,
                    out SoloComboEncounterDefinitionAsset encounter,
                    out string encounterError),
                Is.True,
                encounterError);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(
                battleService.TryBeginBattle(
                    encounter,
                    _runtimeLoadoutSession,
                    "play-mode-round-trip.enemy",
                    ExplorationSceneName,
                    CombatSceneName,
                    player.transform.position,
                    player.transform.rotation,
                    out string beginError),
                Is.True,
                beginError);
            Assert.That(battleService.FlowPhase, Is.EqualTo(BattleFlowPhase.Combat));

            AsyncOperation combatLoad = SceneManager.LoadSceneAsync(
                CombatSceneName,
                LoadSceneMode.Additive);
            Assert.That(combatLoad, Is.Not.Null);
            while (!combatLoad.isDone)
            {
                yield return null;
            }

            yield return null;
            BattleResultScreenController resultController =
                Object.FindAnyObjectByType<BattleResultScreenController>();
            Assert.That(resultController, Is.Not.Null,
                "The formal battle result screen was not bootstrapped.");
            Assert.That(
                battleService.TryCompleteActiveBattle(
                    BattleOutcome.PlayerVictory,
                    BattleRewardSummary.None,
                    out string completionError),
                Is.True,
                completionError);

            yield return null;
            Assert.That(battleService.FlowPhase, Is.EqualTo(BattleFlowPhase.Result));
            Assert.That(resultController.IsOpen, Is.True,
                "A completed formal battle must open the result screen.");

            UIDocument resultDocument = resultController.GetComponent<UIDocument>();
            VisualElement resultOverlay =
                resultDocument.rootVisualElement.Q<VisualElement>("battleResultOverlay");
            Assert.That(resultOverlay, Is.Not.Null);
            Assert.That(resultOverlay.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            GameObject formalHudObject = GameObject.Find("Formal Combat HUD");
            Assert.That(formalHudObject, Is.Not.Null,
                "The formal combat HUD was not bootstrapped.");
            UIDocument formalHudDocument = formalHudObject.GetComponent<UIDocument>();
            VisualElement formalHudRoot =
                formalHudDocument.rootVisualElement.Q<VisualElement>("combatHudRoot");
            Assert.That(formalHudRoot, Is.Not.Null);
            Assert.That(formalHudRoot.style.display.value, Is.EqualTo(DisplayStyle.None),
                "The combat HUD must be hidden while the result screen owns presentation.");

            Assert.That(
                battleService.TryReturnToSourceScene(out string returnError),
                Is.True,
                returnError);

            float timeout = Time.realtimeSinceStartup + 10f;
            while (battleService.HasActiveSession
                && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            yield return null;
            Assert.That(battleService.HasActiveSession, Is.False,
                "Returning to exploration did not clear the completed Battle Session.");
            Assert.That(battleService.FlowPhase, Is.EqualTo(BattleFlowPhase.Idle));
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(ExplorationSceneName));
            Assert.That(resultController.IsOpen, Is.False,
                "The result screen remained open after the Battle Session ended.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(GameObject.FindGameObjectWithTag("Player"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator TitleMenu_LoadsRequiredPresentationAndServices()
        {
            yield return LoadScene(TitleMenuBootstrap.TitleSceneName);

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(TitleMenuBootstrap.TitleSceneName));
            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True,
                "Title Menu requires an active PlayerStateService.");
            Assert.That(stateService.State, Is.Not.Null);
            Assert.That(PlayerSaveService.TryGetActive(out PlayerSaveService saveService),
                Is.True,
                "Title Menu requires an active PlayerSaveService.");
            Assert.That(saveService.IsAuthority, Is.True);

            TitleMenuController controller =
                Object.FindAnyObjectByType<TitleMenuController>();
            Assert.That(controller, Is.Not.Null,
                "Title Menu bootstrap did not create its controller.");
            Assert.That(controller.isActiveAndEnabled, Is.True);

            UIDocument document = controller.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("newGameButton"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("loadGameButton"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("settingsButton"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("quitGameButton"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("deleteSaveDataButton"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("deleteSlot1"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<VisualElement>("deleteConfirmationOverlay"), Is.Not.Null);

            GameObject titleCamera = GameObject.Find("Title Camera");
            Assert.That(titleCamera, Is.Not.Null,
                "Title Menu bootstrap did not create the title camera.");
            Assert.That(titleCamera.GetComponent<Camera>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ExplorationScene_LoadsRequiredPlayerAndIntegrationObjects()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(ExplorationSceneName));
            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            Assert.That(stateService.State, Is.Not.Null);
            Assert.That(BattleSessionService.TryGetActive(out BattleSessionService battleService),
                Is.True);
            Assert.That(battleService.IsAuthority, Is.True);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null,
                "Exploration scene must contain an active Player object.");
            SkinnedMeshRenderer[] playerRenderers =
                player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(playerRenderers, Is.Not.Empty,
                "Player must contain a visible skinned mesh.");
            Bounds playerBounds = playerRenderers[0].bounds;
            for (int index = 1; index < playerRenderers.Length; index++)
            {
                playerBounds.Encapsulate(playerRenderers[index].bounds);
            }
            Vector2 rootPosition = new Vector2(
                player.transform.position.x,
                player.transform.position.z);
            Vector2 visiblePosition = new Vector2(
                playerBounds.center.x,
                playerBounds.center.z);
            Assert.That(Vector2.Distance(rootPosition, visiblePosition), Is.LessThan(0.5f),
                "Player gameplay origin must remain aligned with the visible character.");
            Assert.That(Camera.main, Is.Not.Null,
                "Exploration scene must contain an enabled Main Camera.");
            Assert.That(Object.FindAnyObjectByType<ExplorationMenuController>(), Is.Not.Null,
                "Exploration Menu bootstrap did not create its controller.");
            Assert.That(Object.FindAnyObjectByType<ExplorationBattleTrigger>(), Is.Not.Null,
                "Exploration scene must contain a configured battle trigger.");
            Assert.That(GameObject.Find("Save Point"), Is.Not.Null,
                "Exploration scene must contain its interactive Save Point.");
        }

        [UnityTest]
        public IEnumerator ExplorationMenu_OpensComboLoadoutsWithoutGrantingPreviewContent()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            stateService.StartNewGame();
            string[] expectedStartingActions =
            {
                "greatsword.opening.sweeping-slash",
                "greatsword.opening.draw-slash",
                "greatsword.opening.vertical-slash",
                "common.opening.prepare",
                "greatsword.main.charged-slash",
                "greatsword.main.clash-slash",
                "greatsword.main.rising-strong-strike",
                "common.main.use-healing-potion",
                "greatsword.closing.empowered-sheathe",
                "greatsword.closing.sidestep-sheathe",
                "greatsword.closing.double-sweep",
                "common.closing.breather-1",
                "greatsword.linked.shoulder-tackle",
                "greatsword.linked.guarding-charge",
                "greatsword.linked.vertical-slash",
                "common.linked.use-vitality-tonic"
            };
            CollectionAssert.AreEquivalent(
                expectedStartingActions,
                stateService.State.ActionLibrary.OwnedActionIds);
            Assert.That(
                stateService.State.ComboLoadouts.ActiveLoadout.Combos,
                Has.All.Matches<SER494.Gameplay.PlayerState.PreparedComboState>(
                    combo => combo.GetAssignedActionIds().Count == 0));

            ExplorationMenuController controller =
                Object.FindAnyObjectByType<ExplorationMenuController>();
            Assert.That(controller, Is.Not.Null);
            controller.gameObject.SendMessage(
                "TryOpenMenu",
                false,
                SendMessageOptions.RequireReceiver);
            controller.gameObject.SendMessage(
                "OnCombosClicked",
                SendMessageOptions.RequireReceiver);
            yield return null;

            Assert.That(stateService.State.ActionLibrary.OwnedActionIds,
                Has.Count.EqualTo(16),
                "Opening the Exploration Menu must not grant Editor preview Actions.");
            CollectionAssert.AreEquivalent(
                expectedStartingActions,
                stateService.State.ActionLibrary.OwnedActionIds);
            Assert.That(stateService.State.ComboLoadouts.UnlockedSlotCount,
                Is.EqualTo(1),
                "Opening the Exploration Menu must not unlock preview Loadouts.");

            UIDocument document = controller.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            List<Button> loadoutCards = document.rootVisualElement
                .Query<Button>(className: "loadout-card-button")
                .ToList();
            Assert.That(loadoutCards, Is.Not.Empty,
                "The Combo Loadouts page did not render its Loadout cards.");
            for (int index = 0; index < loadoutCards.Count; index++)
            {
                Assert.That(
                    loadoutCards[index]
                        .Query<VisualElement>(className: "loadout-combo-preview-row")
                        .ToList(),
                    Has.Count.EqualTo(4),
                    $"Loadout card {index + 1} must render all four Combo rows.");
            }
        }

        [UnityTest]
        public IEnumerator ExplorationMenu_RendersOwnedActionManualWithoutLearningItAutomatically()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            stateService.StartNewGame();
            const string actionId = "sword-and-shield.opening.horizontal-slash";
            string manualId = ActionManualLearningService.CreateManualItemId(actionId);
            stateService.State.Inventory.Add(manualId, 1);

            ExplorationMenuController controller =
                Object.FindAnyObjectByType<ExplorationMenuController>();
            Assert.That(controller, Is.Not.Null);
            controller.gameObject.SendMessage(
                "TryOpenMenu",
                false,
                SendMessageOptions.RequireReceiver);
            controller.gameObject.SendMessage(
                "ShowActionLearning",
                SendMessageOptions.RequireReceiver);
            yield return null;

            UIDocument document = controller.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            List<Button> manualCards = document.rootVisualElement
                .Query<Button>(className: "learning-action-card")
                .ToList();
            Assert.That(manualCards, Has.Count.EqualTo(1));
            Assert.That(
                manualCards[0].Query<Label>(className: "learning-action-title").First().text,
                Does.Contain("Horizontal Slash [O]").And.Contain("×1"));
            Assert.That(manualCards[0].enabledInHierarchy, Is.True);
            Assert.That(stateService.State.ActionLibrary.Owns(actionId), Is.False,
                "Opening the learning page must not consume a Manual or learn its Action.");
            Assert.That(stateService.State.Inventory.GetCount(manualId), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ExplorationMenu_AppliesSelectedComboWithoutValidatingEmptySiblings()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            stateService.StartNewGame();

            SER494.Gameplay.PlayerState.PlayerComboLoadoutState loadout =
                stateService.State.ComboLoadouts.ActiveLoadout;
            var draft = new SER494.Gameplay.PlayerState.PlayerComboLoadoutDraft(loadout);
            draft.Combos[0].SetAction(
                SER494.Gameplay.PlayerState.PreparedComboActionSlot.Opening,
                "greatsword.opening.sweeping-slash");
            draft.Combos[0].SetAction(
                SER494.Gameplay.PlayerState.PreparedComboActionSlot.Main,
                "greatsword.main.charged-slash");
            draft.Combos[0].SetAction(
                SER494.Gameplay.PlayerState.PreparedComboActionSlot.Closing,
                "greatsword.closing.empowered-sheathe");
            draft.Combos[0].SetAction(
                SER494.Gameplay.PlayerState.PreparedComboActionSlot.Linked,
                "greatsword.linked.shoulder-tackle");

            Assert.That(draft.Combos[1].CreatePreparedState().GetAssignedActionIds(),
                Is.Empty,
                "The regression requires an invalid empty sibling Combo.");

            ExplorationMenuController controller =
                Object.FindAnyObjectByType<ExplorationMenuController>();
            Assert.That(controller, Is.Not.Null);
            SetPrivateField(controller, "_selectedLoadout", loadout);
            SetPrivateField(controller, "_selectedLoadoutDraft", draft);
            SetPrivateField(controller, "_selectedComboIndex", 0);

            MethodInfo applyMethod = typeof(ExplorationMenuController).GetMethod(
                "TryApplyCurrentCombo",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(applyMethod, Is.Not.Null);
            object[] arguments = { null };
            bool applied = (bool)applyMethod.Invoke(controller, arguments);

            Assert.That(applied, Is.True, arguments[0] as string);
            Assert.That(loadout.Combos[0].MainActionId,
                Is.EqualTo("greatsword.main.charged-slash"));
            Assert.That(loadout.Combos[1].GetAssignedActionIds(), Is.Empty,
                "Applying Combo 1 must not modify or require Combo 2.");
        }

        [UnityTest]
        public IEnumerator CombatScene_ConsumesPreparedBattleSession()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            Assert.That(BattleSessionService.TryGetActive(out BattleSessionService battleService),
                Is.True);
            if (battleService.HasActiveSession)
            {
                battleService.ClearCurrentSession();
            }

            stateService.StartNewGame();
            ConfigureStarterCombo(stateService.State);
            _runtimeLoadoutSession =
                ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
            Assert.That(
                PlayerStateLoadoutSessionBuilder.TryPrepare(
                    stateService.State,
                    _runtimeLoadoutSession,
                    SoloComboTestScenario.GreatswordTrial,
                    out string loadoutError),
                Is.True,
                loadoutError);
            Assert.That(
                SoloComboEncounterLibrary.TryLoad(
                    SoloComboTestScenario.GreatswordTrial,
                    out SoloComboEncounterDefinitionAsset encounter,
                    out string encounterError),
                Is.True,
                encounterError);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(
                battleService.TryBeginBattle(
                    encounter,
                    _runtimeLoadoutSession,
                    "play-mode-smoke.enemy",
                    ExplorationSceneName,
                    CombatSceneName,
                    player.transform.position,
                    player.transform.rotation,
                    out string beginError),
                Is.True,
                beginError);
            BattleSession expectedSession = battleService.CurrentSession;

            yield return LoadScene(CombatSceneName);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(CombatSceneName));
            Assert.That(battleService.HasActiveSession, Is.True);
            Assert.That(battleService.CurrentSession, Is.SameAs(expectedSession));
            Assert.That(battleService.CurrentSession.LoadoutSession,
                Is.SameAs(_runtimeLoadoutSession));
            Assert.That(battleService.CurrentSession.Encounter, Is.SameAs(encounter));

            GameObject sandbox = GameObject.Find("CombatSandbox");
            Assert.That(sandbox, Is.Not.Null,
                "Combat scene must contain the CombatSandbox root object.");
            Component controller = sandbox.GetComponent("CombatSandboxController");
            Assert.That(controller, Is.Not.Null,
                "CombatSandbox root must contain its runtime controller.");
            Assert.That(((Behaviour)controller).isActiveAndEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ExplorationEncounter_UsesExplorationWorldForCombatPresentation()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            Assert.That(BattleSessionService.TryGetActive(out BattleSessionService battleService),
                Is.True);
            if (battleService.HasActiveSession)
            {
                battleService.ClearCurrentSession();
            }

            stateService.StartNewGame();
            ConfigureStarterCombo(stateService.State);
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Collider playerCollider = player.GetComponentInChildren<Collider>();
            Assert.That(playerCollider, Is.Not.Null);

            ExplorationBattleTrigger[] triggers =
                Object.FindObjectsByType<ExplorationBattleTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Assert.That(triggers, Is.Not.Empty);
            ExplorationBattleTrigger trigger = triggers[0];
            trigger.gameObject.SetActive(true);
            trigger.enabled = true;

            Camera explorationCamera = player.GetComponentInChildren<Camera>(true);
            Assert.That(explorationCamera, Is.Not.Null);
            trigger.gameObject.SendMessage(
                "OnTriggerEnter",
                playerCollider,
                SendMessageOptions.RequireReceiver);

            float timeout = Time.realtimeSinceStartup + 10f;
            Scene combatScene = SceneManager.GetSceneByName(CombatSceneName);
            while ((!combatScene.IsValid() || !combatScene.isLoaded)
                && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                combatScene = SceneManager.GetSceneByName(CombatSceneName);
            }

            Assert.That(combatScene.IsValid() && combatScene.isLoaded, Is.True,
                "Exploration encounter did not load the combat scene additively.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(ExplorationSceneName),
                "The exploration scene must remain the active world during combat.");
            Assert.That(explorationCamera.enabled, Is.True,
                "The exploration Camera must render the in-world combat presentation.");
            Assert.That(battleService.HasActiveSession, Is.True);

            GameObject sandbox = null;
            foreach (GameObject root in combatScene.GetRootGameObjects())
            {
                if (root.name == "CombatSandbox")
                {
                    sandbox = root;
                }

                foreach (Camera combatCamera in root.GetComponentsInChildren<Camera>(true))
                {
                    Assert.That(combatCamera.enabled, Is.False,
                        "The additive CombatSandbox Camera must not cover the exploration world.");
                }
            }

            Assert.That(sandbox, Is.Not.Null);
            Component controller = sandbox.GetComponent("CombatSandboxController");
            Assert.That(controller, Is.Not.Null);
            Assert.That(((Behaviour)controller).isActiveAndEnabled, Is.True);

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(combatScene);
            Assert.That(unloadOperation, Is.Not.Null);
            while (!unloadOperation.isDone)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DungeonGuardEncounter_StartsItsAuthoredBattleSession()
        {
            yield return VerifyAuthoredExplorationEncounter(
                "Dungeon Guard Encounter",
                "encounter.dungeon-guard",
                "floor1.dungeon-guard.01",
                "Dungeon Guard");
        }

        [UnityTest]
        public IEnumerator ApeBeastEncounter_StartsItsAuthoredBattleSession()
        {
            yield return VerifyAuthoredExplorationEncounter(
                "Ape Beast Encounter",
                "encounter.ape-beast",
                "floor1.ape-beast.01",
                "Ape Beast");
        }

        [UnityTest]
        public IEnumerator ArmoredGuardEncounter_StartsItsAuthoredBattleSession()
        {
            yield return VerifyAuthoredExplorationEncounter(
                "Armored Guard Encounter",
                "encounter.armored-guard",
                "floor1.armored-guard.01",
                "Armored Guard");
        }

        private IEnumerator VerifyAuthoredExplorationEncounter(
            string triggerObjectName,
            string expectedEncounterId,
            string expectedEncounterInstanceId,
            string expectedEnemyName)
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            Assert.That(BattleSessionService.TryGetActive(out BattleSessionService battleService),
                Is.True);
            if (battleService.HasActiveSession)
            {
                battleService.ClearCurrentSession();
            }

            stateService.StartNewGame();
            ConfigureStarterCombo(stateService.State);
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Collider playerCollider = player.GetComponentInChildren<Collider>();
            Assert.That(playerCollider, Is.Not.Null);

            GameObject triggerObject = GameObject.Find(triggerObjectName);
            Assert.That(triggerObject, Is.Not.Null,
                $"Exploration scene is missing '{triggerObjectName}'.");
            ExplorationBattleTrigger trigger =
                triggerObject.GetComponent<ExplorationBattleTrigger>();
            Assert.That(trigger, Is.Not.Null,
                $"'{triggerObjectName}' must contain ExplorationBattleTrigger.");
            triggerObject.SetActive(true);
            trigger.enabled = true;
            triggerObject.SendMessage(
                "OnTriggerEnter",
                playerCollider,
                SendMessageOptions.RequireReceiver);

            float timeout = Time.realtimeSinceStartup + 10f;
            Scene combatScene = SceneManager.GetSceneByName(CombatSceneName);
            while ((!combatScene.IsValid()
                    || !combatScene.isLoaded
                    || !battleService.HasActiveSession)
                && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                combatScene = SceneManager.GetSceneByName(CombatSceneName);
            }

            Assert.That(combatScene.IsValid() && combatScene.isLoaded, Is.True,
                $"'{triggerObjectName}' did not load the combat scene.");
            Assert.That(battleService.HasActiveSession, Is.True,
                $"'{triggerObjectName}' did not create a Battle Session.");
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(ExplorationSceneName),
                "Formal combat must retain the exploration scene as its active world.");

            BattleSession session = battleService.CurrentSession;
            Assert.That(session.EncounterInstanceId,
                Is.EqualTo(expectedEncounterInstanceId));
            Assert.That(session.Encounter.EncounterId,
                Is.EqualTo(expectedEncounterId));
            Assert.That(
                session.Encounter.TryCreateDefinition(
                    out SoloComboEncounterDefinition definition,
                    out string encounterError),
                Is.True,
                encounterError);
            Assert.That(definition.EnemyDisplayName, Is.EqualTo(expectedEnemyName));

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(combatScene);
            Assert.That(unloadOperation, Is.Not.Null);
            while (!unloadOperation.isDone)
            {
                yield return null;
            }

            battleService.ClearCurrentSession();
            yield return null;
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            Assert.That(Application.CanStreamedLevelBeLoaded(sceneName), Is.True,
                $"Scene '{sceneName}' must be enabled in Build Settings.");
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            // Allow scene-loaded bootstraps and component Start methods to run.
            yield return null;
        }

        private static void ConfigureStarterCombo(
            SER494.Gameplay.PlayerState.PlayerState state)
        {
            state.ComboLoadouts.ActiveLoadout.SetCombo(
                0,
                new SER494.Gameplay.PlayerState.PreparedComboState(
                    "Smoke Test Combo",
                    "greatsword.opening.sweeping-slash",
                    "greatsword.main.charged-slash",
                    "greatsword.closing.empowered-sheathe",
                    "greatsword.linked.shoulder-tackle",
                    SER494.Gameplay.PlayerState.PreparedLinkedActionPlacement.AfterMain));
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Private field '{name}' was not found.");
            field.SetValue(target, value);
        }
    }
}
